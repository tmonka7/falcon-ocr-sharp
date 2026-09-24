using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using FalconOcr.Engine;
using FalconOcr.Export;
using ExportOptions = FalconOcr.Export.ExportOptions;

namespace FalconOcr.App.Services
{
    internal static class AppPaths
    {
        public static string DataDir
        {
            get
            {
                var d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FalconOCR");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static string DefaultOutput => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Falcon OCR Output");
    }

    internal static class Json
    {
        public static T Load<T>(string path) where T : class, new()
        {
            try
            {
                if (!File.Exists(path)) return new T();
                using (var fs = File.OpenRead(path))
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(fs) ?? new T();
            }
            catch
            {
                return new T(); // corrupt settings must never prevent the app from starting
            }
        }

        public static void Save<T>(string path, T value)
        {
            var tmp = path + ".tmp";
            using (var fs = File.Create(tmp))
            using (var w = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, true, true))
                new DataContractJsonSerializer(typeof(T)).WriteObject(w, value);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }

    [DataContract]
    internal sealed class AppSettings
    {
        private static string FilePath => Path.Combine(AppPaths.DataDir, "settings.json");

        [DataMember] public OcrOptions Ocr { get; set; } = new OcrOptions();
        [DataMember] public ExportFormat Format { get; set; } = ExportFormat.Docx;
        [DataMember] public DocumentMode Mode { get; set; } = DocumentMode.Editable;
        [DataMember] public string OutputFolder { get; set; }
        [DataMember] public bool OpenAfterExport { get; set; } = true;
        [DataMember] public bool KeepColors { get; set; } = true;
        [DataMember] public bool IncludeImages { get; set; } = true;
        [DataMember] public bool AutoRecognize { get; set; }
        [DataMember] public bool ShowConfidence { get; set; } = true;
        [DataMember] public bool Maximized { get; set; } = true;
        [DataMember] public Rectangle Bounds { get; set; }
        [DataMember] public List<string> RecentFiles { get; set; } = new List<string>();

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Ocr = new OcrOptions();
            Format = ExportFormat.Docx;
            Mode = DocumentMode.Editable;
            OpenAfterExport = true;
            KeepColors = true;
            IncludeImages = true;
            ShowConfidence = true;
            Maximized = true;
            RecentFiles = new List<string>();
        }

        public static AppSettings Load()
        {
            var s = Json.Load<AppSettings>(FilePath);
            if (s.Ocr == null) s.Ocr = new OcrOptions();
            if (s.RecentFiles == null) s.RecentFiles = new List<string>();
            if (string.IsNullOrEmpty(s.OutputFolder)) s.OutputFolder = AppPaths.DefaultOutput;
            return s;
        }

        public void Save()
        {
            try { Json.Save(FilePath, this); } catch { /* read-only profile: settings are optional */ }
        }

        public void AddRecent(string path)
        {
            RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            RecentFiles.Insert(0, path);
            if (RecentFiles.Count > 15) RecentFiles.RemoveRange(15, RecentFiles.Count - 15);
        }

        public ExportOptions ExportOptions() => new ExportOptions { Mode = Mode, Language = Ocr.Language, KeepColors = KeepColors, IncludeImages = IncludeImages };
    }

    [DataContract]
    internal sealed class HistoryEntry
    {
        [DataMember] public DateTime Time { get; set; }
        [DataMember] public string Source { get; set; }
        [DataMember] public string Output { get; set; }
        [DataMember] public string Format { get; set; }
        [DataMember] public int Pages { get; set; }
        [DataMember] public string Language { get; set; }
        [DataMember] public double Seconds { get; set; }
    }

    [DataContract]
    internal sealed class HistoryStore
    {
        private static string FilePath => Path.Combine(AppPaths.DataDir, "history.json");

        [DataMember] public List<HistoryEntry> Entries { get; set; } = new List<HistoryEntry>();

        public event EventHandler Changed;

        public static HistoryStore Load()
        {
            var h = Json.Load<HistoryStore>(FilePath);
            if (h.Entries == null) h.Entries = new List<HistoryEntry>();
            return h;
        }

        public void Add(HistoryEntry e)
        {
            Entries.Insert(0, e);
            if (Entries.Count > 500) Entries.RemoveRange(500, Entries.Count - 500);
            Save();
        }

        public void Clear()
        {
            Entries.Clear();
            Save();
        }

        private void Save()
        {
            try { Json.Save(FilePath, this); } catch { }
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Windows Image Acquisition (WIA) scanning through late-bound COM — no interop assembly needed.</summary>
    internal static class Scanner
    {
        private const string FormatPng = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";

        /// <summary>Shows the WIA acquisition dialog and returns the scanned image file (null if cancelled).</summary>
        public static string Acquire()
        {
            var type = Type.GetTypeFromProgID("WIA.CommonDialog");
            if (type == null) throw new InvalidOperationException("Windows Image Acquisition (WIA) is not available on this computer.");
            dynamic dialog = Activator.CreateInstance(type);
            dynamic image;
            try
            {
                // DeviceType 1 = scanner, Intent 1 = color, Bias 131072 = max quality.
                image = dialog.ShowAcquireImage(1, 1, 131072, FormatPng, false, true, false);
            }
            catch (System.Runtime.InteropServices.COMException ex) when ((uint)ex.ErrorCode == 0x80210015)
            {
                throw new InvalidOperationException("No scanner was found. Connect a WIA-compatible scanner and try again.");
            }
            if (image == null) return null;
            var dir = Path.Combine(AppPaths.DataDir, "scans");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "Scan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            image.SaveFile(path);
            return path;
        }
    }
}
