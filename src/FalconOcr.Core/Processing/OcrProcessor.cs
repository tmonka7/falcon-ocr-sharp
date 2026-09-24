using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using FalconOcr.Engine;
using FalconOcr.Imaging;
using FalconOcr.Input;
using FalconOcr.Layout;
using FalconOcr.Model;

namespace FalconOcr.Processing
{
    public sealed class PageProgress
    {
        public OcrDocument Document;
        public DocumentPage Page;
        public int Done;
        public int Total;
        public string Message;
    }

    /// <summary>Page → (PDF text layer | PaddleOCR) → layout reconstruction.</summary>
    public sealed class OcrProcessor : IDisposable
    {
        private readonly PaddleOcrEngine _engine;

        public OcrProcessor(string modelDirectory = null)
        {
            _engine = new PaddleOcrEngine(modelDirectory);
        }

        public PaddleOcrEngine Engine => _engine;

        public OcrPage ProcessPage(DocumentPage page, OcrOptions o, CancellationToken ct = default(CancellationToken))
        {
            var sw = Stopwatch.StartNew();
            using (var bmp = page.GetProcessedImage(o.PdfDpi))
            {
                var img = RgbImage.FromBitmap(bmp);
                float dpi = page.EffectiveDpi(bmp, o.PdfDpi);
                var lang = LanguageCatalog.Get(o.Language);

                List<TextLine> lines = null;
                bool fromText = false;
                if (page.IsPdf && o.PdfText == PdfTextMode.Auto && page.Document.Pdf.CountTextChars(page.PdfPageIndex) >= 16)
                {
                    lines = page.Document.Pdf.ExtractLines(page.PdfPageIndex, o.PdfDpi);
                    if (page.Rotation != 0 || page.Crop.HasValue) lines = MapLines(lines, page, o.PdfDpi, img.Width, img.Height);
                    fromText = lines.Count > 0;
                }
                ct.ThrowIfCancellationRequested();
                if (!fromText) lines = _engine.Recognize(img, o, ct);
                ct.ThrowIfCancellationRequested();

                var result = LayoutAnalyzer.Analyze(img, lines, dpi, o, fromText, o.EffectiveFont(lang));
                result.Elapsed = sw.Elapsed;
                return result;
            }
        }

        /// <summary>Recognizes every page (or only pages without results) of a document.</summary>
        public void ProcessDocument(OcrDocument doc, OcrOptions o, bool onlyMissing, IProgress<PageProgress> progress, CancellationToken ct)
        {
            var pages = doc.Pages.Where(p => !onlyMissing || p.Result == null).ToList();
            for (int i = 0; i < pages.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(new PageProgress { Document = doc, Page = pages[i], Done = i, Total = pages.Count, Message = "Recognizing " + doc.Name + " — " + pages[i].Label });
                pages[i].Result = ProcessPage(pages[i], o, ct);
            }
            progress?.Report(new PageProgress { Document = doc, Done = pages.Count, Total = pages.Count, Message = "Recognition completed" });
        }

        /// <summary>Re-maps PDF text-layer lines (unrotated page pixels) into the rotated/cropped page image.</summary>
        private static List<TextLine> MapLines(List<TextLine> lines, DocumentPage page, int dpi, int outW, int outH)
        {
            var size = page.Document.Pdf.GetPageSize(page.PdfPageIndex);
            int ow = (int)Math.Round(size.Width * dpi / 72f), oh = (int)Math.Round(size.Height * dpi / 72f);
            var result = new List<TextLine>();
            var clip = new RectangleF(0, 0, outW, outH);
            foreach (var l in lines)
            {
                var pts = l.Polygon.Select(p => page.MapFromOriginal(p, ow, oh)).ToArray();
                var b = Geometry.BoundsOf(pts);
                if (Geometry.Coverage(b, clip) < 0.5f) continue;
                bool vertical = page.Rotation == 90 || page.Rotation == 270;
                l.Bounds = b;
                l.InkBounds = b;
                // Reorder so the quad starts at the visual top-left.
                l.Polygon = new[] { new PointF(b.Left, b.Top), new PointF(b.Right, b.Top), new PointF(b.Right, b.Bottom), new PointF(b.Left, b.Bottom) };
                if (vertical || page.Rotation == 180)
                {
                    // Character extents no longer run along x; drop them.
                    l.CharLeft = null;
                    l.CharRight = null;
                }
                else if (page.Crop.HasValue && l.CharLeft != null)
                {
                    float dx = -page.MapFromOriginal(PointF.Empty, ow, oh).X; // crop offset in pixels
                    l.CharLeft = l.CharLeft.Select(x => x - dx).ToArray();
                    l.CharRight = l.CharRight.Select(x => x - dx).ToArray();
                }
                result.Add(l);
            }
            return result;
        }

        public void Dispose() => _engine.Dispose();
    }
}
