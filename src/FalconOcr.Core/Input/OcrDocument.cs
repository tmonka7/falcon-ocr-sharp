using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FalconOcr.Model;
using FalconOcr.Pdf;

namespace FalconOcr.Input
{
    public enum SourceKind
    {
        Image,
        MultiPageImage,
        ImageSequence,
        Pdf,
        Clipboard,
        Scan
    }

    /// <summary>A document in the workspace: a PDF, an image, a multi-page TIFF or a sequence of images.</summary>
    public sealed class OcrDocument : IDisposable
    {
        public static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".jpe", ".bmp", ".dib", ".gif", ".tif", ".tiff", ".ico", ".emf", ".wmf" };

        public string Name { get; set; }
        public string SourcePath { get; private set; }
        public SourceKind Kind { get; private set; }
        public List<DocumentPage> Pages { get; } = new List<DocumentPage>();
        public DateTime Added { get; } = DateTime.Now;

        internal PdfDocument Pdf { get; private set; }

        public bool IsRecognized => Pages.Count > 0 && Pages.All(p => p.Result != null);
        public int RecognizedCount => Pages.Count(p => p.Result != null);

        private OcrDocument() { }

        public static bool IsSupported(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".pdf" || ImageExtensions.Contains(ext);
        }

        public static bool IsImage(string path) => ImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

        public static OcrDocument Open(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".pdf") return OpenPdf(path);
            if (!ImageExtensions.Contains(ext)) throw new NotSupportedException("Unsupported file type: " + ext);

            var doc = new OcrDocument { Name = Path.GetFileName(path), SourcePath = path, Kind = SourceKind.Image };
            int frames = CountFrames(path);
            if (frames > 1) doc.Kind = SourceKind.MultiPageImage;
            for (int i = 0; i < frames; i++)
                doc.Pages.Add(new DocumentPage(doc, i) { ImagePath = path, FrameIndex = i, Label = frames > 1 ? "Page " + (i + 1) : Path.GetFileName(path) });
            return doc;
        }

        public static OcrDocument OpenPdf(string path)
        {
            var pdf = new PdfDocument(path);
            var doc = new OcrDocument { Name = Path.GetFileName(path), SourcePath = path, Kind = SourceKind.Pdf, Pdf = pdf };
            for (int i = 0; i < pdf.PageCount; i++)
                doc.Pages.Add(new DocumentPage(doc, i) { PdfPageIndex = i, Label = "Page " + (i + 1) });
            return doc;
        }

        /// <summary>Several images that form one document (e.g. a scanned book), ordered naturally by file name.</summary>
        public static OcrDocument OpenImageSequence(IEnumerable<string> files, string name = null)
        {
            var list = files.Where(IsImage).OrderBy(f => f, NaturalComparer.Instance).ToList();
            if (list.Count == 0) throw new InvalidOperationException("No supported images in the sequence.");
            var doc = new OcrDocument
            {
                Name = name ?? (Path.GetFileName(Path.GetDirectoryName(list[0])) ?? "Image sequence"),
                SourcePath = Path.GetDirectoryName(list[0]),
                Kind = SourceKind.ImageSequence
            };
            foreach (var f in list)
            {
                int frames = CountFrames(f);
                for (int i = 0; i < frames; i++)
                    doc.Pages.Add(new DocumentPage(doc, doc.Pages.Count) { ImagePath = f, FrameIndex = i, Label = frames > 1 ? Path.GetFileName(f) + " #" + (i + 1) : Path.GetFileName(f) });
            }
            return doc;
        }

        public static OcrDocument FromBitmap(Bitmap bmp, string name, SourceKind kind = SourceKind.Clipboard)
        {
            var doc = new OcrDocument { Name = name, Kind = kind };
            doc.Pages.Add(new DocumentPage(doc, 0) { InMemory = new Bitmap(bmp), Label = name });
            return doc;
        }

        public void RemovePage(DocumentPage page)
        {
            Pages.Remove(page);
            PageImageCache.Invalidate(page);
            for (int i = 0; i < Pages.Count; i++) Pages[i].Index = i;
        }

        private static int CountFrames(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".tif" && ext != ".tiff") return 1;
            try
            {
                using (var img = Image.FromFile(path))
                    return Math.Max(1, img.GetFrameCount(FrameDimension.Page));
            }
            catch { return 1; }
        }

        public void Dispose()
        {
            foreach (var p in Pages) PageImageCache.Invalidate(p);
            foreach (var p in Pages) p.InMemory?.Dispose();
            Pdf?.Dispose();
            Pdf = null;
        }

        public override string ToString() => Name;
    }

    public sealed class DocumentPage
    {
        private int _rotation;
        private RectangleF? _crop;

        internal DocumentPage(OcrDocument owner, int index)
        {
            Document = owner;
            Index = index;
        }

        public OcrDocument Document { get; }
        public int Index { get; internal set; }
        public string Label { get; set; }

        public string ImagePath { get; internal set; }
        public int FrameIndex { get; internal set; }
        public int PdfPageIndex { get; internal set; } = -1;
        internal Bitmap InMemory { get; set; }

        public bool IsPdf => PdfPageIndex >= 0 && Document.Pdf != null;

        /// <summary>Clockwise rotation (0, 90, 180, 270) applied before recognition.</summary>
        public int Rotation
        {
            get => _rotation;
            set
            {
                int v = ((value % 360) + 360) % 360;
                if (v == _rotation) return;
                _rotation = v;
                _crop = null; // crop is expressed in rotated coordinates
                Changed();
            }
        }

        /// <summary>
        /// Crop as fractions (0..1) of the rotated page, or null for the full page. Normalized so that it stays
        /// valid for any PDF rendering resolution (thumbnails, preview, OCR).
        /// </summary>
        public RectangleF? Crop
        {
            get => _crop;
            set
            {
                _crop = value;
                Changed();
            }
        }

        /// <summary>Crops further to <paramref name="r"/>, given in pixels of the current processed image.</summary>
        public void ApplyCrop(Rectangle r, Size processedSize)
        {
            var cur = _crop ?? new RectangleF(0, 0, 1, 1);
            float fx = (float)r.X / processedSize.Width, fy = (float)r.Y / processedSize.Height;
            float fw = (float)r.Width / processedSize.Width, fh = (float)r.Height / processedSize.Height;
            Crop = new RectangleF(cur.X + fx * cur.Width, cur.Y + fy * cur.Height, fw * cur.Width, fh * cur.Height);
        }

        private Rectangle CropPixels(int w, int h)
        {
            var c = _crop.Value;
            return Rectangle.Round(new RectangleF(c.X * w, c.Y * h, c.Width * w, c.Height * h));
        }

        /// <summary>Recognition result (null until recognized; reset by rotate/crop).</summary>
        public OcrPage Result { get; set; }

        /// <summary>Increments with every edit that changes the processed image.</summary>
        public int Version { get; private set; }

        private void Changed()
        {
            Version++;
            Result = null;
            PageImageCache.Invalidate(this);
        }

        /// <summary>Loads the unmodified page image (PDF pages rendered at <paramref name="pdfDpi"/>).</summary>
        public Bitmap LoadOriginal(int pdfDpi)
        {
            if (IsPdf) return Document.Pdf.Render(PdfPageIndex, pdfDpi);
            if (InMemory != null) return new Bitmap(InMemory);
            // Read through a stream copy so the file is not locked while the app runs.
            var bytes = File.ReadAllBytes(ImagePath);
            using (var ms = new MemoryStream(bytes))
            using (var img = Image.FromStream(ms))
            {
                if (FrameIndex > 0) img.SelectActiveFrame(FrameDimension.Page, FrameIndex);
                var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
                bmp.SetResolution(img.HorizontalResolution, img.VerticalResolution);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
                }
                ApplyExifOrientation(img, bmp);
                return bmp;
            }
        }

        /// <summary>The image that is recognized: original → rotation → crop. Callers own (dispose) the result.</summary>
        public Bitmap GetProcessedImage(int pdfDpi)
        {
            return PageImageCache.Get(this, pdfDpi, () =>
            {
                var bmp = LoadOriginal(pdfDpi);
                float dpiX = bmp.HorizontalResolution, dpiY = bmp.VerticalResolution;
                switch (Rotation)
                {
                    case 90: bmp.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                    case 180: bmp.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                    case 270: bmp.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
                }
                if (Crop.HasValue)
                {
                    var r = Rectangle.Intersect(CropPixels(bmp.Width, bmp.Height), new Rectangle(0, 0, bmp.Width, bmp.Height));
                    if (r.Width > 8 && r.Height > 8)
                    {
                        var c = bmp.Clone(r, PixelFormat.Format24bppRgb);
                        bmp.Dispose();
                        bmp = c;
                    }
                }
                bmp.SetResolution(dpiX, dpiY);
                return bmp;
            });
        }

        /// <summary>Effective resolution used to convert pixels into physical font sizes.</summary>
        public float EffectiveDpi(Bitmap processed, int pdfDpi)
        {
            if (IsPdf) return pdfDpi;
            float dpi = processed.HorizontalResolution;
            if (dpi >= 150 && dpi <= 1200) return dpi;
            // Scans are often tagged 72/96 dpi regardless of scan resolution: estimate from an A4/Letter page.
            int longSide = Math.Max(processed.Width, processed.Height);
            if (longSide >= 1600 && Crop == null)
                return Math.Max(96, Math.Min(600, longSide / 11.3f));
            return dpi > 0 ? dpi : 96;
        }

        /// <summary>Maps a point of the unrotated original (e.g. PDF text layer) into processed-image coordinates.</summary>
        public PointF MapFromOriginal(PointF p, int origW, int origH)
        {
            PointF r;
            switch (Rotation)
            {
                case 90: r = new PointF(origH - p.Y, p.X); break;
                case 180: r = new PointF(origW - p.X, origH - p.Y); break;
                case 270: r = new PointF(p.Y, origW - p.X); break;
                default: r = p; break;
            }
            if (Crop.HasValue)
            {
                bool swap = Rotation == 90 || Rotation == 270;
                var c = CropPixels(swap ? origH : origW, swap ? origW : origH);
                r = new PointF(r.X - c.X, r.Y - c.Y);
            }
            return r;
        }

        private static void ApplyExifOrientation(Image src, Bitmap dst)
        {
            const int orientationId = 0x0112;
            if (!src.PropertyIdList.Contains(orientationId)) return;
            var prop = src.GetPropertyItem(orientationId);
            if (prop?.Value == null || prop.Value.Length < 2) return;
            switch (BitConverter.ToUInt16(prop.Value, 0))
            {
                case 3: dst.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                case 6: dst.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                case 8: dst.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
            }
        }

        public override string ToString() => Label;
    }

    /// <summary>Small LRU of processed page images (page renders are expensive, especially PDFs).</summary>
    public static class PageImageCache
    {
        private const int Capacity = 6;
        private static readonly LinkedList<Entry> Entries = new LinkedList<Entry>();

        private sealed class Entry
        {
            public DocumentPage Page;
            public int Version;
            public int Dpi;
            public Bitmap Bitmap;
        }

        public static Bitmap Get(DocumentPage page, int dpi, Func<Bitmap> factory)
        {
            lock (Entries)
            {
                for (var n = Entries.First; n != null; n = n.Next)
                {
                    var e = n.Value;
                    if (e.Page == page && e.Version == page.Version && (e.Dpi == dpi || !page.IsPdf))
                    {
                        Entries.Remove(n);
                        Entries.AddFirst(n);
                        return Clone(e.Bitmap);
                    }
                }
            }
            var bmp = factory();
            lock (Entries)
            {
                Entries.AddFirst(new Entry { Page = page, Version = page.Version, Dpi = dpi, Bitmap = bmp });
                while (Entries.Count > Capacity)
                {
                    Entries.Last.Value.Bitmap.Dispose();
                    Entries.RemoveLast();
                }
                return Clone(bmp);
            }
        }

        private static Bitmap Clone(Bitmap b)
        {
            var c = new Bitmap(b);
            c.SetResolution(b.HorizontalResolution, b.VerticalResolution);
            return c;
        }

        public static void Invalidate(DocumentPage page)
        {
            lock (Entries)
            {
                var n = Entries.First;
                while (n != null)
                {
                    var next = n.Next;
                    if (n.Value.Page == page)
                    {
                        n.Value.Bitmap.Dispose();
                        Entries.Remove(n);
                    }
                    n = next;
                }
            }
        }
    }

    /// <summary>"page2" &lt; "page10".</summary>
    public sealed class NaturalComparer : IComparer<string>
    {
        public static readonly NaturalComparer Instance = new NaturalComparer();
        private static readonly Regex Chunk = new Regex(@"\d+|\D+", RegexOptions.Compiled);

        public int Compare(string a, string b)
        {
            if (a == null || b == null) return string.Compare(a, b, StringComparison.Ordinal);
            var ma = Chunk.Matches(a);
            var mb = Chunk.Matches(b);
            for (int i = 0; i < Math.Min(ma.Count, mb.Count); i++)
            {
                string x = ma[i].Value, y = mb[i].Value;
                int c;
                if (char.IsDigit(x[0]) && char.IsDigit(y[0]))
                {
                    x = x.TrimStart('0');
                    y = y.TrimStart('0');
                    c = x.Length != y.Length ? x.Length.CompareTo(y.Length) : string.CompareOrdinal(x, y);
                }
                else c = string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
            }
            return ma.Count.CompareTo(mb.Count);
        }
    }
}
