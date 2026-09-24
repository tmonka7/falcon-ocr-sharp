using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FalconOcr.Localization;
using Microsoft.Win32;

namespace FalconOcr.Licensing
{
    /// <summary>Contents of a license key (the signed payload).</summary>
    public sealed class LicenseInfo
    {
        public const int MachineHashLength = 10;

        public byte Version { get; set; } = 1;
        public uint Serial { get; set; }
        public DateTime Issued { get; set; } = DateTime.UtcNow.Date;
        /// <summary>Last valid day (inclusive), or null for a perpetual license.</summary>
        public DateTime? Expires { get; set; }
        /// <summary>Machine binding (see <see cref="MachineIdentity"/>), or null for any machine.</summary>
        public byte[] MachineHash { get; set; }
        public string Licensee { get; set; } = "";

        public bool AnyMachine => MachineHash == null || MachineHash.All(b => b == 0);

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(L.F("Licensed to {0}", string.IsNullOrEmpty(Licensee) ? L.T("(unnamed)") : Licensee));
            sb.Append(" · ").Append(L.F("serial {0}", Serial));
            sb.Append(" · ").Append(Expires.HasValue ? L.F("valid until {0}", Expires.Value.ToString("yyyy-MM-dd")) : L.T("perpetual"));
            sb.Append(" · ").Append(AnyMachine ? L.T("any computer") : L.T("this computer only"));
            return sb.ToString();
        }
    }

    public enum LicenseStatus
    {
        Valid,
        Missing,
        Malformed,
        InvalidSignature,
        WrongMachine,
        Expired,
        ClockTampered
    }

    public sealed class LicenseCheck
    {
        public LicenseStatus Status { get; set; }
        public LicenseInfo Info { get; set; }
        public string Key { get; set; }
        public bool IsValid => Status == LicenseStatus.Valid;

        public string Message
        {
            get
            {
                switch (Status)
                {
                    case LicenseStatus.Valid: return Info.ToString();
                    case LicenseStatus.Missing: return L.T("No license key has been entered.");
                    case LicenseStatus.Malformed: return L.T("The license key is not in a valid format. Check that it was copied completely.");
                    case LicenseStatus.InvalidSignature: return L.T("The license key is not genuine.");
                    case LicenseStatus.WrongMachine: return L.T("This license key was issued for a different computer.");
                    case LicenseStatus.Expired: return L.F("The license expired on {0}.", Info.Expires.Value.ToString("yyyy-MM-dd"));
                    case LicenseStatus.ClockTampered: return L.T("The system clock is set earlier than the last use of the application.");
                    default: return Status.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Binary layout and text form of license keys.
    /// Payload: version(1) serial(4) issued(2) expires(2) machine(10) nameLength(1) name(UTF-8, ≤ 48),
    /// followed by a 64-byte ECDSA P-256 / SHA-256 signature. Text: "FOCR-" + Crockford Base32 in groups of five.
    /// </summary>
    public static class LicenseCodec
    {
        public const string Prefix = "FOCR";
        public const int SignatureLength = 64;
        public const int MaxNameBytes = 48;
        private static readonly DateTime Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static byte[] EncodePayload(LicenseInfo info)
        {
            var name = Encoding.UTF8.GetBytes(info.Licensee ?? "");
            if (name.Length > MaxNameBytes) name = TrimUtf8(info.Licensee, MaxNameBytes);
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(info.Version);
                w.Write(info.Serial);
                w.Write(Days(info.Issued));
                w.Write(info.Expires.HasValue ? Days(info.Expires.Value) : (ushort)0);
                var mh = new byte[LicenseInfo.MachineHashLength];
                if (info.MachineHash != null) Array.Copy(info.MachineHash, mh, Math.Min(mh.Length, info.MachineHash.Length));
                w.Write(mh);
                w.Write((byte)name.Length);
                w.Write(name);
                w.Flush();
                return ms.ToArray();
            }
        }

        public static LicenseInfo DecodePayload(byte[] payload)
        {
            using (var r = new BinaryReader(new MemoryStream(payload)))
            {
                var info = new LicenseInfo { Version = r.ReadByte() };
                if (info.Version != 1) throw new FormatException("Unsupported license version " + info.Version);
                info.Serial = r.ReadUInt32();
                info.Issued = Epoch.AddDays(r.ReadUInt16());
                ushort exp = r.ReadUInt16();
                info.Expires = exp == 0 ? (DateTime?)null : Epoch.AddDays(exp);
                info.MachineHash = r.ReadBytes(LicenseInfo.MachineHashLength);
                int n = r.ReadByte();
                var name = r.ReadBytes(n);
                if (name.Length != n || r.BaseStream.Position != payload.Length) throw new FormatException("Truncated license payload");
                info.Licensee = Encoding.UTF8.GetString(name);
                return info;
            }
        }

        public static string Format(byte[] payload, byte[] signature)
        {
            var all = payload.Concat(signature).ToArray();
            var b32 = Base32.Encode(all);
            var groups = Enumerable.Range(0, (b32.Length + 4) / 5).Select(i => b32.Substring(i * 5, Math.Min(5, b32.Length - i * 5)));
            return Prefix + "-" + string.Join("-", groups);
        }

        /// <summary>Splits a key into payload and signature (no signature check).</summary>
        public static bool TryParse(string key, out byte[] payload, out byte[] signature)
        {
            payload = signature = null;
            if (string.IsNullOrWhiteSpace(key)) return false;
            var s = new string(key.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();
            if (s.StartsWith(Prefix)) s = s.Substring(Prefix.Length);
            byte[] all;
            try { all = Base32.Decode(s); }
            catch (FormatException) { return false; }
            if (all.Length < 20 + SignatureLength) return false;
            payload = all.Take(all.Length - SignatureLength).ToArray();
            signature = all.Skip(all.Length - SignatureLength).ToArray();
            return true;
        }

        private static ushort Days(DateTime d) => (ushort)Math.Max(1, Math.Min(ushort.MaxValue, (d.Date - Epoch).TotalDays));

        private static byte[] TrimUtf8(string s, int maxBytes)
        {
            while (Encoding.UTF8.GetByteCount(s) > maxBytes) s = s.Substring(0, s.Length - 1);
            return Encoding.UTF8.GetBytes(s);
        }
    }

    /// <summary>Crockford Base32 (no I, L, O, U; decoding tolerates I/L → 1 and O → 0).</summary>
    public static class Base32
    {
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        public static string Encode(byte[] data)
        {
            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = 0, bits = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0) sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        public static byte[] Decode(string text)
        {
            var bytes = new System.Collections.Generic.List<byte>(text.Length * 5 / 8);
            int buffer = 0, bits = 0;
            foreach (var raw in text.ToUpperInvariant())
            {
                char c = raw == 'I' || raw == 'L' ? '1' : raw == 'O' ? '0' : raw;
                int v = Alphabet.IndexOf(c);
                if (v < 0) throw new FormatException("Invalid character '" + raw + "'");
                buffer = (buffer << 5) | v;
                bits += 5;
                if (bits >= 8)
                {
                    bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }
            return bytes.ToArray();
        }
    }

    /// <summary>Stable identifier of this Windows installation used to bind a key to one computer.</summary>
    public static class MachineIdentity
    {
        private static byte[] _hash;

        /// <summary>First 10 bytes of SHA-256 over the Windows MachineGuid.</summary>
        public static byte[] Hash
        {
            get
            {
                if (_hash != null) return _hash;
                string id = null;
                try
                {
                    using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    using (var k = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                        id = k?.GetValue("MachineGuid") as string;
                }
                catch { }
                if (string.IsNullOrEmpty(id)) id = Environment.MachineName;
                using (var sha = SHA256.Create())
                    _hash = sha.ComputeHash(Encoding.UTF8.GetBytes("FalconOCR|" + id.Trim().ToLowerInvariant())).Take(LicenseInfo.MachineHashLength).ToArray();
                return _hash;
            }
        }

        /// <summary>The code a customer sends to get a machine-bound key, e.g. "7K2Q-DM0X-9A4T-Z1BC".</summary>
        public static string Code => FormatCode(Hash);

        public static string FormatCode(byte[] hash)
        {
            var s = Base32.Encode(hash); // 10 bytes → 16 chars
            return string.Join("-", Enumerable.Range(0, 4).Select(i => s.Substring(i * 4, 4)));
        }

        public static byte[] ParseCode(string code)
        {
            var s = new string((code ?? "").Where(char.IsLetterOrDigit).ToArray());
            var b = Base32.Decode(s);
            if (b.Length != LicenseInfo.MachineHashLength) throw new FormatException("A machine code has 16 characters (4 groups of 4).");
            return b;
        }
    }
}
