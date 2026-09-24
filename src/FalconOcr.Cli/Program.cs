using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FalconOcr.Engine;
using FalconOcr.Export;
using FalconOcr.Input;
using FalconOcr.Model;
using FalconOcr.Processing;

namespace FalconOcr.Cli
{
    /// <summary>
    /// Command-line front end (batch conversion / automation / testing).
    ///   falcon-ocr input.pdf [more files] [-l en|zh|ja|ko|ru] [-f docx|xlsx|html|txt] [-o outDir]
    ///              [--exact] [--seq] [--no-tables] [--ocr-only] [--dpi 200] [--dump]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
            {
                Console.WriteLine("usage: falcon-ocr <files...> [-l en|zh|ja|ko|ru] [-f docx|xlsx|html|txt] [-o outDir] [--exact] [--seq] [--no-tables] [--ocr-only] [--dpi N] [--dump]");
                return 1;
            }

            var inputs = new List<string>();
            var o = new OcrOptions();
            var formats = new List<ExportFormat>();
            string outDir = null;
            bool dump = false, seq = false;
            var mode = DocumentMode.Editable;
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                switch (a)
                {
                    case "-l": o.Language = ParseLang(args[++i]); break;
                    case "-f": formats.AddRange(args[++i].Split(',').Select(ParseFormat)); break;
                    case "-o": outDir = args[++i]; break;
                    case "--exact": mode = DocumentMode.ExactCopy; break;
                    case "--plain": mode = DocumentMode.PlainText; break;
                    case "--seq": seq = true; break;
                    case "--no-tables": o.DetectTables = false; break;
                    case "--ocr-only": o.PdfText = PdfTextMode.AlwaysOcr; break;
                    case "--dpi": o.PdfDpi = int.Parse(args[++i]); break;
                    case "--cls": o.UseAngleClassifier = true; break;
                    case "--dump": dump = true; break;
                    default:
                        if (Directory.Exists(a)) inputs.AddRange(Directory.GetFiles(a).Where(OcrDocument.IsSupported));
                        else inputs.Add(a);
                        break;
                }
            }
            if (formats.Count == 0 && !dump) formats.Add(ExportFormat.Docx);

            var missing = PaddleOcrEngine.FindMissingModels(LanguageCatalog.DefaultModelDirectory, o.Language);
            if (missing.Count > 0)
            {
                Console.Error.WriteLine("Missing model files: " + string.Join(", ", missing));
                return 2;
            }

            var docs = seq
                ? new List<OcrDocument> { OcrDocument.OpenImageSequence(inputs) }
                : inputs.Select(OcrDocument.Open).ToList();

            using (var proc = new OcrProcessor())
            {
                foreach (var doc in docs)
                {
                    var sw = Stopwatch.StartNew();
                    proc.ProcessDocument(doc, o, false, new Progress<PageProgress>(p => { if (p.Page != null) Console.Error.WriteLine(p.Message); }), CancellationToken.None);
                    Console.Error.WriteLine($"{doc.Name}: {doc.Pages.Count} page(s) in {sw.Elapsed.TotalSeconds:0.0}s");
                    if (dump) Dump(doc);
                    foreach (var f in formats)
                    {
                        var dir = outDir ?? Path.GetDirectoryName(Path.GetFullPath(doc.SourcePath ?? "."));
                        Directory.CreateDirectory(dir);
                        var path = Path.Combine(dir, Path.GetFileNameWithoutExtension(doc.Name) + Exporter.Extension(f));
                        Exporter.Export(doc, f, path, new ExportOptions { Mode = mode, Language = o.Language });
                        Console.WriteLine(path);
                    }
                    doc.Dispose();
                }
            }
            return 0;
        }

        private static void Dump(OcrDocument doc)
        {
            foreach (var p in doc.Pages)
            {
                var r = p.Result;
                Console.WriteLine($"=== {p.Label}  {r.Width}x{r.Height} @{r.Dpi:0}dpi  lines={r.Lines.Count} blocks={r.Blocks.Count} sections={r.Sections.Count} textLayer={r.FromTextLayer} {r.Elapsed.TotalSeconds:0.00}s");
                if (Environment.GetEnvironmentVariable("FALCON_DUMP_LINES") == "1")
                    foreach (var l in r.Lines)
                        Console.WriteLine($"  line box={l.Bounds.Left:0},{l.Bounds.Top:0},{l.Bounds.Width:0}x{l.Bounds.Height:0} ink={l.InkBounds.Top:0}-{l.InkBounds.Bottom:0} ({l.InkBounds.Height:0}) {l.Style.FontSizePt}pt{(l.Style.Bold ? " B" : "")} conf={l.Confidence:0.00} \"{l.Text}\"");
                foreach (var s in r.Sections)
                {
                    Console.WriteLine($"--- section cols={s.Columns.Count} y={s.Bounds.Top:0}-{s.Bounds.Bottom:0}");
                    foreach (var b in s.Blocks)
                    {
                        var st = b.Style;
                        string head = $"[{b.Kind}{(b.Kind == BlockKind.Heading ? b.HeadingLevel.ToString() : "")} c{b.Column} {b.Align} {st.FontSizePt}pt{(st.Bold ? " B" : "")} #{st.Color.R:X2}{st.Color.G:X2}{st.Color.B:X2} @{b.Bounds.Left:0},{b.Bounds.Top:0} {b.Bounds.Width:0}x{b.Bounds.Height:0}]";
                        if (b.Kind == BlockKind.Table)
                            Console.WriteLine(head + $" {b.Table.RowCount}x{b.Table.ColumnCount} borders={b.Table.HasBorders}\n" + b.Table.ToText());
                        else if (b.Kind == BlockKind.Figure)
                            Console.WriteLine(head);
                        else
                            Console.WriteLine(head + " " + (b.ListMarker != null ? b.ListMarker + " " : "") + b.GetFlowText());
                    }
                }
            }
        }

        private static OcrLanguage ParseLang(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "zh": case "ch": case "chinese": return OcrLanguage.Chinese;
                case "ja": case "jp": case "japanese": return OcrLanguage.Japanese;
                case "ko": case "kr": case "korean": return OcrLanguage.Korean;
                case "ru": case "russian": return OcrLanguage.Russian;
                default: return OcrLanguage.English;
            }
        }

        private static ExportFormat ParseFormat(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "xlsx": case "excel": return ExportFormat.Xlsx;
                case "html": case "htm": return ExportFormat.Html;
                case "txt": case "text": return ExportFormat.Text;
                default: return ExportFormat.Docx;
            }
        }
    }
}
