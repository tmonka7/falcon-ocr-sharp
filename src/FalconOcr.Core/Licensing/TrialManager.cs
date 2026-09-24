using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FalconOcr.Localization;
using Microsoft.Win32;

namespace FalconOcr.Licensing
{
    public sealed class TrialStatus
    {
        public DateTime StartedUtc { get; set; }
        public int TotalDays { get; set; } = TrialManager.TrialDays;
        /// <summary>Whole days left including today (7 on the first day, 1 on the last day, 0 when over).</summary>
        public int DaysLeft { get; set; }
        public bool Expired { get; set; }
        /// <summary>Trial data was modified or the clock was turned back: the trial is treated as over.</summary>
        public bool Tampered { get; set; }
        public DateTime EndsUtc => StartedUtc.AddDays(TotalDays);

        public string Message =>
            Tampered ? L.T("The trial information on this computer is invalid (modified data or system clock set back).")
            : Expired ? L.F("The {0}-day trial ended on {1}.", TotalDays, EndsUtc.ToLocalTime().ToString("yyyy-MM-dd"))
            : L.F("Trial version — {0} of {1} days remaining (ends {2}).", DaysLeft, TotalDays, EndsUtc.ToLocalTime().ToString("yyyy-MM-dd"));
    }

    /// <summary>
    /// 7-day evaluation period. The start date is written on first use to two places (registry and a file in the
    /// user profile), each protected by an HMAC bound to this computer; the earliest valid start wins, edited
    /// records end the trial, and turning the clock back before the last recorded use ends the trial.
    /// </summary>
    public static class TrialManager
    {
        public const int TrialDays = 7;
        private const string RegistryPath = @"Software\FalconOCR";
        private const string RegistryValue = "EvaluationState";

        private static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FalconOCR", "evaluation.dat");

        /// <summary>Reads (and on the very first run starts) the trial, recording this use.</summary>
        public static TrialStatus Check() => Check(DateTime.UtcNow);

        public static TrialStatus Check(DateTime utcNow)
        {
            var records = ReadAll(out bool tampered);
            DateTime start, lastSeen;
            if (records.Count == 0 && !tampered)
            {
                start = lastSeen = utcNow;
            }
            else if (records.Count == 0)
            {
                // Only corrupted records exist: someone edited them.
                return new TrialStatus { StartedUtc = utcNow.AddDays(-TrialDays), Expired = true, Tampered = true };
            }
            else
            {
                start = records.Min(r => r.Start);
                lastSeen = records.Max(r => r.LastSeen);
            }

            var status = new TrialStatus { StartedUtc = start, Tampered = tampered };
            if (utcNow < lastSeen.AddHours(-24)) status.Tampered = true; // clock turned back
            double used = (utcNow - start).TotalDays;
            status.Expired = status.Tampered || used >= TrialDays || used < -1;
            status.DaysLeft = status.Expired ? 0 : Math.Max(1, TrialDays - (int)Math.Floor(used));

            WriteAll(new Record { Start = start, LastSeen = utcNow > lastSeen ? utcNow : lastSeen });
            return status;
        }

        // ------------------------------------------------------------ storage

        private struct Record
        {
            public DateTime Start;
            public DateTime LastSeen;
        }

        private static List<Record> ReadAll(out bool tampered)
        {
            tampered = false;
            var result = new List<Record>();
            foreach (var text in new[] { ReadRegistry(), ReadFile() })
            {
                if (text == null) continue;
                if (TryDecode(text, out var r)) result.Add(r);
                else tampered = true;
            }
            return result;
        }

        private static void WriteAll(Record r)
        {
            var text = Encode(r);
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(RegistryPath))
                    k?.SetValue(RegistryValue, text);
            }
            catch { }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                if (File.Exists(FilePath)) File.SetAttributes(FilePath, FileAttributes.Normal);
                File.WriteAllText(FilePath, text);
                File.SetAttributes(FilePath, FileAttributes.Hidden);
            }
            catch { }
        }

        private static string ReadRegistry()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RegistryPath))
                    return k?.GetValue(RegistryValue) as string;
            }
            catch { return null; }
        }

        private static string ReadFile()
        {
            try { return File.Exists(FilePath) ? File.ReadAllText(FilePath).Trim() : null; }
            catch { return null; }
        }

        private static string Encode(Record r)
        {
            var body = "1|" + r.Start.Ticks + "|" + r.LastSeen.Ticks;
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(body)) + "." + Mac(body);
        }

        private static bool TryDecode(string text, out Record r)
        {
            r = default(Record);
            try
            {
                var parts = text.Split('.');
                if (parts.Length != 2) return false;
                var body = Encoding.ASCII.GetString(Convert.FromBase64String(parts[0]));
                if (!FixedEquals(Mac(body), parts[1])) return false;
                var f = body.Split('|');
                if (f.Length != 3 || f[0] != "1") return false;
                r.Start = new DateTime(long.Parse(f[1]), DateTimeKind.Utc);
                r.LastSeen = new DateTime(long.Parse(f[2]), DateTimeKind.Utc);
                return true;
            }
            catch { return false; }
        }

        /// <summary>HMAC keyed with this computer's identity: records cannot be edited or copied to another PC.</summary>
        private static string Mac(string body)
        {
            var key = Encoding.UTF8.GetBytes("FalconOCR/evaluation/v1/").Concat(MachineIdentity.Hash).ToArray();
            using (var h = new HMACSHA256(key))
                return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(body)));
        }

        private static bool FixedEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
