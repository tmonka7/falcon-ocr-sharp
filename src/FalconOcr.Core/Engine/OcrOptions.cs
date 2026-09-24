using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace FalconOcr.Engine
{
    public enum OcrLanguage
    {
        English,
        Chinese,
        Japanese,
        Korean,
        Russian
    }

    /// <summary>Paths of the bundled PaddleOCR (PP-OCRv5, ONNX) models.</summary>
    public sealed class LanguageModel
    {
        public OcrLanguage Language { get; set; }
        public string DisplayName { get; set; }
        public string RecModel { get; set; }
        public string Dictionary { get; set; }
        /// <summary>Font used for reconstructed text of this language when the original font is unknown.</summary>
        public string DefaultFont { get; set; }
        /// <summary>BCP-47 tag written to exported documents.</summary>
        public string Culture { get; set; }
    }

    public static class LanguageCatalog
    {
        public const string DetModel = @"det\ch_PP-OCRv5_det_mobile.onnx";
        public const string ClsModel = @"cls\ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx";

        public static readonly IReadOnlyList<LanguageModel> All = new[]
        {
            new LanguageModel { Language = OcrLanguage.English, DisplayName = "English", RecModel = @"rec\en_PP-OCRv5_rec_mobile.onnx", Dictionary = @"rec\ppocrv5_en_dict.txt", DefaultFont = "Calibri", Culture = "en-US" },
            // PP-OCRv5's main recognizer covers Simplified/Traditional Chinese, Japanese and English in one model.
            new LanguageModel { Language = OcrLanguage.Chinese, DisplayName = "Chinese (中文)", RecModel = @"rec\ch_PP-OCRv5_rec_mobile.onnx", Dictionary = @"rec\ppocrv5_dict.txt", DefaultFont = "Microsoft YaHei", Culture = "zh-CN" },
            new LanguageModel { Language = OcrLanguage.Japanese, DisplayName = "Japanese (日本語)", RecModel = @"rec\ch_PP-OCRv5_rec_mobile.onnx", Dictionary = @"rec\ppocrv5_dict.txt", DefaultFont = "Yu Gothic", Culture = "ja-JP" },
            new LanguageModel { Language = OcrLanguage.Korean, DisplayName = "Korean (한국어)", RecModel = @"rec\korean_PP-OCRv5_rec_mobile.onnx", Dictionary = @"rec\ppocrv5_korean_dict.txt", DefaultFont = "Malgun Gothic", Culture = "ko-KR" },
            new LanguageModel { Language = OcrLanguage.Russian, DisplayName = "Russian (Русский)", RecModel = @"rec\eslav_PP-OCRv5_rec_mobile.onnx", Dictionary = @"rec\ppocrv5_eslav_dict.txt", DefaultFont = "Calibri", Culture = "ru-RU" },
        };

        public static LanguageModel Get(OcrLanguage lang) => All.First(l => l.Language == lang);

        /// <summary>Models directory next to the executable (models are copied there at build time).</summary>
        public static string DefaultModelDirectory
        {
            get
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, "models");
            }
        }
    }

    public enum PdfTextMode
    {
        /// <summary>Use the PDF's embedded text layer when a page has one, OCR otherwise.</summary>
        Auto,
        /// <summary>Always rasterize and OCR (for PDFs with broken text layers).</summary>
        AlwaysOcr
    }

    public enum LayoutMode
    {
        /// <summary>Columns, paragraphs, headings, lists, tables and figures.</summary>
        Automatic,
        /// <summary>Single column: paragraphs only, top-to-bottom.</summary>
        SingleColumn,
        /// <summary>One output line per detected text line.</summary>
        LinesOnly
    }

    [DataContract]
    public sealed class OcrOptions
    {
        [DataMember] public OcrLanguage Language { get; set; } = OcrLanguage.English;
        [DataMember] public LayoutMode Layout { get; set; } = LayoutMode.Automatic;
        [DataMember] public bool DetectTables { get; set; } = true;
        [DataMember] public bool DetectFigures { get; set; } = true;
        [DataMember] public PdfTextMode PdfText { get; set; } = PdfTextMode.Auto;

        /// <summary>Rendering resolution for PDF pages.</summary>
        [DataMember] public int PdfDpi { get; set; } = 200;

        /// <summary>Longest side fed to the detector (PP-OCRv5 works at native resolution up to this limit).</summary>
        [DataMember] public int DetMaxSide { get; set; } = 2560;
        [DataMember] public float DetThreshold { get; set; } = 0.3f;
        [DataMember] public float BoxThreshold { get; set; } = 0.6f;
        [DataMember] public float UnclipRatio { get; set; } = 1.5f;

        /// <summary>Run the text-line orientation classifier (fixes upside-down lines).</summary>
        [DataMember] public bool UseAngleClassifier { get; set; } = false;

        /// <summary>Discard lines whose mean recognition confidence is below this value.</summary>
        [DataMember] public float MinConfidence { get; set; } = 0.5f;

        [DataMember] public int Threads { get; set; } = 0;
        [DataMember] public int RecBatchSize { get; set; } = 8;

        public OcrOptions Clone() => (OcrOptions)MemberwiseClone();

        /// <summary>The serializer skips constructors: start from defaults so fields missing in old files stay sane.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            var d = new OcrOptions();
            Language = d.Language;
            Layout = d.Layout;
            DetectTables = d.DetectTables;
            DetectFigures = d.DetectFigures;
            PdfText = d.PdfText;
            PdfDpi = d.PdfDpi;
            DetMaxSide = d.DetMaxSide;
            DetThreshold = d.DetThreshold;
            BoxThreshold = d.BoxThreshold;
            UnclipRatio = d.UnclipRatio;
            UseAngleClassifier = d.UseAngleClassifier;
            MinConfidence = d.MinConfidence;
            Threads = d.Threads;
            RecBatchSize = d.RecBatchSize;
        }
    }
}
