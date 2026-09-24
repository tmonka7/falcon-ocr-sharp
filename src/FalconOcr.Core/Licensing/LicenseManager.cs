using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FalconOcr.Licensing
{
    /// <summary>
    /// Verifies license keys against the public key built into the application and stores the activated key.
    /// Only the KeyGen (which holds the private key) can produce keys that pass this check.
    /// </summary>
    public static class LicenseManager
    {
        private const string FileName = "license.key";

        /// <summary>Where the activated key is saved (per user, always writable).</summary>
        public static string UserLicensePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FalconOCR", FileName);

        /// <summary>Keys deployed by an administrator: next to the executable, or machine-wide.</summary>
        private static string[] SearchPaths => new[]
        {
            UserLicensePath,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "FalconOCR", FileName)
        };

        private static string StatePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FalconOCR", "license.state");

        /// <summary>Validates a key for this computer and today's date.</summary>
        public static LicenseCheck Validate(string key) => Validate(key, LicensePublicKey.Blob, MachineIdentity.Hash, DateTime.UtcNow, true);

        /// <summary>Full validation with explicit inputs (used by the KeyGen's verifier and tests).</summary>
        public static LicenseCheck Validate(string key, string publicKeyBlob, byte[] machineHash, DateTime utcNow, bool checkClock)
        {
            var result = new LicenseCheck { Key = key };
            if (string.IsNullOrWhiteSpace(key))
            {
                result.Status = LicenseStatus.Missing;
                return result;
            }
            if (!LicenseCodec.TryParse(key, out var payload, out var signature))
            {
                result.Status = LicenseStatus.Malformed;
                return result;
            }
            if (!VerifySignature(payload, signature, publicKeyBlob))
            {
                result.Status = LicenseStatus.InvalidSignature;
                return result;
            }
            try { result.Info = LicenseCodec.DecodePayload(payload); }
            catch (Exception)
            {
                result.Status = LicenseStatus.Malformed;
                return result;
            }

            var info = result.Info;
            if (!info.AnyMachine && (machineHash == null || !info.MachineHash.SequenceEqual(machineHash)))
                result.Status = LicenseStatus.WrongMachine;
            else if (info.Expires.HasValue && utcNow.Date > info.Expires.Value.Date)
                result.Status = LicenseStatus.Expired;
            else if (checkClock && info.Expires.HasValue && ClockRolledBack(utcNow))
                result.Status = LicenseStatus.ClockTampered;
            else
                result.Status = LicenseStatus.Valid;
            return result;
        }

        public static bool VerifySignature(byte[] payload, byte[] signature, string publicKeyBlob)
        {
            if (string.IsNullOrEmpty(publicKeyBlob)) return false;
            try
            {
                using (var key = CngKey.Import(Convert.FromBase64String(publicKeyBlob), CngKeyBlobFormat.EccPublicBlob))
                using (var ecdsa = new ECDsaCng(key))
                    return ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256);
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>Validates the first stored key found (user profile, application folder, ProgramData).</summary>
        public static LicenseCheck CheckInstalled()
        {
            LicenseCheck first = null;
            foreach (var path in SearchPaths)
            {
                string key;
                try
                {
                    if (!File.Exists(path)) continue;
                    key = File.ReadAllText(path).Trim();
                }
                catch { continue; }
                var check = Validate(key);
                if (check.IsValid)
                {
                    RecordUse();
                    return check;
                }
                if (first == null) first = check;
            }
            return first ?? new LicenseCheck { Status = LicenseStatus.Missing };
        }

        /// <summary>Validates and, when valid, saves the key for this user.</summary>
        public static LicenseCheck Activate(string key)
        {
            var check = Validate(key);
            if (!check.IsValid) return check;
            Directory.CreateDirectory(Path.GetDirectoryName(UserLicensePath));
            File.WriteAllText(UserLicensePath, Normalize(key));
            RecordUse();
            return check;
        }

        public static void Deactivate()
        {
            if (File.Exists(UserLicensePath)) File.Delete(UserLicensePath);
        }

        /// <summary>Accepts keys pasted with line breaks/spaces or read from a .lic file.</summary>
        public static string Normalize(string key)
        {
            if (!LicenseCodec.TryParse(key, out var p, out var s)) return (key ?? "").Trim();
            return LicenseCodec.Format(p, s);
        }

        /// <summary>Reads a key from a license file (the first line that starts with "FOCR", or the whole text).</summary>
        public static string ReadKeyFile(string path)
        {
            var text = File.ReadAllText(path);
            var start = text.IndexOf(LicenseCodec.Prefix + "-", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return text.Trim();
            var rest = text.Substring(start);
            // A key may be wrapped over several lines; stop at the first blank line.
            var end = rest.IndexOf("\n\n", StringComparison.Ordinal);
            if (end < 0) end = rest.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            return (end > 0 ? rest.Substring(0, end) : rest).Trim();
        }

        // ------------------------------------------------------------ clock rollback guard (time-limited keys)

        private static bool ClockRolledBack(DateTime utcNow)
        {
            try
            {
                if (!File.Exists(StatePath)) return false;
                var last = new DateTime(long.Parse(File.ReadAllText(StatePath).Trim()), DateTimeKind.Utc);
                return utcNow < last.AddDays(-1);
            }
            catch { return false; }
        }

        private static void RecordUse()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
                long prev = 0;
                if (File.Exists(StatePath)) long.TryParse(File.ReadAllText(StatePath).Trim(), out prev);
                File.WriteAllText(StatePath, Math.Max(prev, DateTime.UtcNow.Ticks).ToString());
            }
            catch { }
        }
    }
}
