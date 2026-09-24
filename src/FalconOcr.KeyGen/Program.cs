using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FalconOcr.Licensing;

namespace FalconOcr.KeyGen
{
    /// <summary>
    /// GUI when started without arguments; command line otherwise:
    ///   FalconOcrKeyGen --init [--force]                      create a signing key and write LicensePublicKey.cs
    ///   FalconOcrKeyGen --backup file.key | --import file.key
    ///   FalconOcrKeyGen --generate --name "Customer" [--machine XXXX-XXXX-XXXX-XXXX]
    ///                   [--days 365 | --expires 2027-12-31] [--serial N] [--note text]
    ///   FalconOcrKeyGen --verify KEY
    ///   FalconOcrKeyGen --machine-code                        print this computer's code
    /// </summary>
    internal static class Program
    {
        [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new KeyGenForm());
                return 0;
            }
            AttachConsole(-1); // WinExe: write to the calling console
            Console.WriteLine();
            try { return RunCommand(args); }
            catch (Exception ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
        }

        private static int RunCommand(string[] args)
        {
            string Arg(string name)
            {
                int i = Array.IndexOf(args, name);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            }
            bool Has(string name) => Array.IndexOf(args, name) >= 0;

            if (Has("--machine-code"))
            {
                Console.WriteLine(MachineIdentity.Code);
                return 0;
            }
            if (Has("--init"))
            {
                if (SigningKey.Exists && !Has("--force"))
                {
                    Console.Error.WriteLine("A signing key already exists (" + SigningKey.StorePath + "). Use --force to replace it — this invalidates all issued keys.");
                    return 2;
                }
                using (var k = SigningKey.CreateNew())
                {
                    k.Save();
                    var src = Arg("--public-cs") ?? SigningKey.FindPublicKeySource();
                    if (src != null) k.WritePublicKeySource(src);
                    Console.WriteLine("Signing key created: " + SigningKey.StorePath);
                    Console.WriteLine("Fingerprint:        " + k.Fingerprint);
                    Console.WriteLine(src != null ? "Public key written: " + src + "  (rebuild Falcon OCR)" : "Public key file not found; use --public-cs <path>.");
                    Console.WriteLine("Back it up now:     FalconOcrKeyGen --backup <file>");
                }
                return 0;
            }
            if (Arg("--import") != null)
            {
                using (var k = SigningKey.FromBackup(Arg("--import")))
                {
                    k.Save();
                    Console.WriteLine("Imported signing key " + k.Fingerprint + (k.MatchesBuiltInPublicKey ? " (matches this build)" : " (does NOT match this build's public key)"));
                }
                return 0;
            }
            if (Arg("--backup") != null)
            {
                using (var k = SigningKey.Load())
                {
                    k.ExportBackup(Arg("--backup"));
                    Console.WriteLine("Backup written: " + Arg("--backup") + "  — keep it secret.");
                }
                return 0;
            }
            if (Has("--generate"))
            {
                var info = new LicenseInfo
                {
                    Licensee = Arg("--name") ?? throw new ArgumentException("--name is required"),
                    Serial = Arg("--serial") != null ? uint.Parse(Arg("--serial")) : SigningKey.NextSerial(),
                    Issued = DateTime.UtcNow.Date
                };
                if (Arg("--machine") != null) info.MachineHash = MachineIdentity.ParseCode(Arg("--machine"));
                if (Arg("--days") != null) info.Expires = DateTime.UtcNow.Date.AddDays(int.Parse(Arg("--days")));
                if (Arg("--expires") != null) info.Expires = DateTime.ParseExact(Arg("--expires"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                using (var k = SigningKey.Load())
                {
                    if (!k.MatchesBuiltInPublicKey) Console.Error.WriteLine("warning: signing key " + k.Fingerprint + " does not match the public key of this build.");
                    var key = k.Issue(info);
                    SigningKey.LogIssued(info, key, Arg("--note"));
                    SigningKey.CommitSerial(info.Serial);
                    Console.WriteLine(key);
                }
                return 0;
            }
            if (Arg("--verify") != null)
            {
                string blob = SigningKey.Exists ? null : LicensePublicKey.Blob;
                if (blob == null) using (var k = SigningKey.Load()) blob = k.PublicBlob;
                var check = LicenseManager.Validate(Arg("--verify"), blob, MachineIdentity.Hash, DateTime.UtcNow, false);
                Console.WriteLine(check.Status + ": " + check.Message);
                return check.IsValid ? 0 : 3;
            }
            Console.Error.WriteLine("usage: FalconOcrKeyGen [--init [--force]] [--generate --name N [--machine CODE] [--days N|--expires yyyy-MM-dd]] [--verify KEY] [--backup F|--import F] [--machine-code]");
            return 1;
        }
    }
}
