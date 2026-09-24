using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using SD = System.Drawing;
using FalconOcr.Engine;
using FalconOcr.Input;
using FalconOcr.Model;

namespace FalconOcr.Export
{
    public enum ExportFormat
    {
        Docx,
        Xlsx,
        Html,
        Text
    }

    public enum DocumentMode
    {
        /// <summary>Flowing, editable document that keeps columns, headings, lists, tables, images and styles.</summary>
        Editable,
        /// <summary>Every line/table/image at its original position (visual copy of the page).</summary>
        ExactCopy,
        /// <summary>Text only, in reading order.</summary>
        PlainText
    }

    public sealed class ExportOptions
    {
        public DocumentMode Mode { get; set; } = DocumentMode.Editable;
        public OcrLanguage Language { get; set; } = OcrLanguage.English;
        public bool IncludeImages { get; set; } = true;
        /// <summary>Document default font (Word Normal style, HTML body, Excel); null = the language default.</summary>
        public string DefaultFont { get; set; }
        /// <summary>Keep the original page background/fill colors.</summary>
        public bool KeepColors { get; set; } = true;
        /// <summary>Only these pages (null = all recognized pages).</summary>
        public IList<DocumentPage> Pages { get; set; }
    }

    public static class Exporter
    {
        public static string Extension(ExportFormat f)
        {
            switch (f)
            {
                case ExportFormat.Xlsx: return ".xlsx";
                case ExportFormat.Html: return ".html";
                case ExportFormat.Text: return ".txt";
                default: return ".docx";
            }
        }

        public static string FilterFor(ExportFormat f)
        {
            switch (f)
            {
                case ExportFormat.Xlsx: return "Excel Workbook (*.xlsx)|*.xlsx";
                case ExportFormat.Html: return "Web Page (*.html)|*.html";
                case ExportFormat.Text: return "Text (*.txt)|*.txt";
                default: return "Word Document (*.docx)|*.docx";
            }
        }

        public static void Export(OcrDocument doc, ExportFormat format, string path, ExportOptions options)
        {
            var pages = (options.Pages ?? doc.Pages).Where(p => p.Result != null).Select(p => p.Result).ToList();
            if (pages.Count == 0) throw new InvalidOperationException("The document has no recognized pages.");
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var title = Path.GetFileNameWithoutExtension(doc.Name);

            // Write to a temp file first so a failed export never leaves a truncated document behind.
            var tmp = path + ".tmp";
            try
            {
                switch (format)
                {
                    case ExportFormat.Docx: DocxExporter.Write(pages, tmp, title, options); break;
                    case ExportFormat.Xlsx: XlsxExporter.Write(pages, tmp, title, options); break;
                    case ExportFormat.Html: HtmlExporter.Write(pages, tmp, title, options); break;
                    default: File.WriteAllText(tmp, string.Join("\f" + Environment.NewLine, pages.Select(p => p.GetPlainText())), new UTF8Encoding(true)); break;
                }
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        /// <summary>A file name that does not exist yet: "name.docx", "name (2).docx", ...</summary>
        public static string UniquePath(string dir, string baseName, string ext)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) baseName = baseName.Replace(c, '_');
            var p = Path.Combine(dir, baseName + ext);
            for (int i = 2; File.Exists(p); i++) p = Path.Combine(dir, baseName + " (" + i + ")" + ext);
            return p;
        }
    }

    public static class ExportHelpers
    {
        /// <summary>
        /// Text lines drawn at their original position in Exact Copy: recognized lines outside tables/figures,
        /// plus a "•" for every bullet that was drawn as a graphic (not recognized as text).
        /// </summary>
        public static IEnumerable<TextLine> PositionedLines(OcrPage page)
        {
            var inTable = new HashSet<TextLine>(page.Blocks.Where(b => b.Kind == BlockKind.Table).SelectMany(b => b.AllTextLines()));
            var lines = page.Lines.Where(l => !l.InFigure && !inTable.Contains(l)).ToList();
            foreach (var b in page.Blocks.Where(b => !b.MarkerGlyph.IsEmpty))
            {
                var g = b.MarkerGlyph;
                var style = b.Style.Clone();
                style.Bold = false;
                style.BackColor = SD.Color.Empty;
                lines.Add(new TextLine { Text = "•", Bounds = g, InkBounds = g, Style = style, Polygon = new[] { g.Location, new SD.PointF(g.Right, g.Top), new SD.PointF(g.Right, g.Bottom), new SD.PointF(g.Left, g.Bottom) } });
            }
            return lines.OrderBy(l => l.Bounds.Top).ThenBy(l => l.Bounds.Left);
        }
    }

    internal static class Units
    {
        public static int Twips(float px, float dpi) => (int)Math.Round(px * 1440f / dpi);
        public static long Emu(float px, float dpi) => (long)Math.Round(px * 914400.0 / dpi);
        public static int HalfPoints(float pt) => Math.Max(2, (int)Math.Round(pt * 2));
        public static string Hex(Color c) => c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        public static string Css(Color c) => "#" + Hex(c).ToLowerInvariant();
    }

    /// <summary>Measures text with GDI+ to fit reconstructed text into its original box (Exact Copy).</summary>
    public static class TextMeasure
    {
        private static readonly object Sync = new object();
        private static readonly Bitmap Canvas = new Bitmap(1, 1);
        private static readonly Graphics G = CreateGraphics();
        private static HashSet<string> _installed;

        private static Graphics CreateGraphics()
        {
            var g = Graphics.FromImage(Canvas);
            g.PageUnit = GraphicsUnit.Point;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            return g;
        }

        public static bool IsInstalled(string family)
        {
            lock (Sync)
            {
                if (_installed == null)
                {
                    using (var fc = new InstalledFontCollection())
                        _installed = new HashSet<string>(fc.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
                }
                return family != null && _installed.Contains(family);
            }
        }

        /// <summary>Width in points of <paramref name="text"/> at <paramref name="sizePt"/>.</summary>
        public static float WidthPt(string text, string family, float sizePt, bool bold, bool italic)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            lock (Sync)
            {
                var style = (bold ? FontStyle.Bold : FontStyle.Regular) | (italic ? FontStyle.Italic : FontStyle.Regular);
                using (var f = new Font(IsInstalled(family) ? family : "Arial", Math.Max(1, sizePt), style, GraphicsUnit.Point))
                {
                    return G.MeasureString(text, f, int.MaxValue, StringFormat.GenericTypographic).Width;
                }
            }
        }

        /// <summary>Horizontal scale (percent) that makes the text exactly as wide as the source line.</summary>
        public static int FitScale(string text, TextStyle s, float boxWidthPt)
        {
            float w = WidthPt(text, s.FontFamily, s.FontSizePt, s.Bold, s.Italic);
            if (w <= 0) return 100;
            int pct = (int)Math.Round(boxWidthPt / w * 100);
            return Math.Max(40, Math.Min(250, pct));
        }
    }
}
