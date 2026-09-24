using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FalconOcr.Model;

namespace FalconOcr.Pdf
{
    /// <summary>A PDF opened with PDFium. Rendering and text extraction are serialized internally.</summary>
    public sealed class PdfDocument : IDisposable
    {
        private IntPtr _doc;
        private GCHandle _pin;
        private readonly byte[] _bytes;

        public int PageCount { get; }
        public string Path { get; }

        public PdfDocument(string path, string password = null)
        {
            PdfiumNative.EnsureInitialized();
            Path = path;
            _bytes = File.ReadAllBytes(path);
            _pin = GCHandle.Alloc(_bytes, GCHandleType.Pinned); // PDFium reads lazily from this buffer
            lock (PdfiumNative.Sync)
            {
                _doc = PdfiumNative.FPDF_LoadMemDocument64(_pin.AddrOfPinnedObject(), (UIntPtr)_bytes.Length, password);
                if (_doc == IntPtr.Zero)
                {
                    uint err = PdfiumNative.FPDF_GetLastError();
                    _pin.Free();
                    throw new InvalidDataException(err == 4 ? "The PDF is password protected." : "Cannot open PDF (PDFium error " + err + ").");
                }
                PageCount = PdfiumNative.FPDF_GetPageCount(_doc);
            }
        }

        /// <summary>Page size in points (1/72 inch), rotation applied.</summary>
        public SizeF GetPageSize(int index)
        {
            lock (PdfiumNative.Sync)
            {
                var page = PdfiumNative.FPDF_LoadPage(_doc, index);
                if (page == IntPtr.Zero) throw new InvalidDataException("Cannot load page " + (index + 1));
                try { return new SizeF(PdfiumNative.FPDF_GetPageWidthF(page), PdfiumNative.FPDF_GetPageHeightF(page)); }
                finally { PdfiumNative.FPDF_ClosePage(page); }
            }
        }

        public Bitmap Render(int index, float dpi)
        {
            lock (PdfiumNative.Sync)
            {
                var page = PdfiumNative.FPDF_LoadPage(_doc, index);
                if (page == IntPtr.Zero) throw new InvalidDataException("Cannot load page " + (index + 1));
                try
                {
                    int w = Math.Max(1, (int)Math.Round(PdfiumNative.FPDF_GetPageWidthF(page) * dpi / 72f));
                    int h = Math.Max(1, (int)Math.Round(PdfiumNative.FPDF_GetPageHeightF(page) * dpi / 72f));
                    // Guard against absurd page sizes (posters) exhausting memory.
                    const int maxPixels = 12000 * 12000;
                    if ((long)w * h > maxPixels)
                    {
                        float k = (float)Math.Sqrt((double)maxPixels / ((long)w * h));
                        w = (int)(w * k);
                        h = (int)(h * k);
                    }
                    var bmp = PdfiumNative.FPDFBitmap_Create(w, h, 0);
                    if (bmp == IntPtr.Zero) throw new OutOfMemoryException("PDFium could not allocate a page bitmap.");
                    try
                    {
                        PdfiumNative.FPDFBitmap_FillRect(bmp, 0, 0, w, h, 0xFFFFFFFF);
                        PdfiumNative.FPDF_RenderPageBitmap(bmp, page, 0, 0, w, h, 0, PdfiumNative.FPDF_ANNOT | PdfiumNative.FPDF_PRINTING);
                        var buffer = PdfiumNative.FPDFBitmap_GetBuffer(bmp);
                        int stride = PdfiumNative.FPDFBitmap_GetStride(bmp);
                        var result = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                        result.SetResolution(dpi, dpi);
                        var bd = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                        try
                        {
                            var row = new byte[stride];
                            var outRow = new byte[w * 3];
                            for (int y = 0; y < h; y++)
                            {
                                Marshal.Copy(buffer + y * stride, row, 0, stride);
                                for (int x = 0, s = 0, d = 0; x < w; x++, s += 4, d += 3)
                                {
                                    outRow[d] = row[s];
                                    outRow[d + 1] = row[s + 1];
                                    outRow[d + 2] = row[s + 2];
                                }
                                Marshal.Copy(outRow, 0, bd.Scan0 + y * bd.Stride, outRow.Length);
                            }
                        }
                        finally { result.UnlockBits(bd); }
                        return result;
                    }
                    finally { PdfiumNative.FPDFBitmap_Destroy(bmp); }
                }
                finally { PdfiumNative.FPDF_ClosePage(page); }
            }
        }

        /// <summary>Number of non-whitespace characters in the page's text layer.</summary>
        public int CountTextChars(int index)
        {
            lock (PdfiumNative.Sync)
            {
                var page = PdfiumNative.FPDF_LoadPage(_doc, index);
                if (page == IntPtr.Zero) return 0;
                var tp = PdfiumNative.FPDFText_LoadPage(page);
                try
                {
                    if (tp == IntPtr.Zero) return 0;
                    int n = PdfiumNative.FPDFText_CountChars(tp), count = 0;
                    for (int i = 0; i < n; i++)
                    {
                        uint u = PdfiumNative.FPDFText_GetUnicode(tp, i);
                        if (u > 32 && u != 0xFFFE && u != 0xFFFD) count++;
                    }
                    return count;
                }
                finally
                {
                    if (tp != IntPtr.Zero) PdfiumNative.FPDFText_ClosePage(tp);
                    PdfiumNative.FPDF_ClosePage(page);
                }
            }
        }

        private struct PdfChar
        {
            public string Text;
            public RectangleF Box;   // device pixels
            public float FontPt;
            public bool Bold, Italic;
            public Color Color;
            public string Font;
            public bool Break;       // hard line break (\r or \n) before this char
        }

        /// <summary>
        /// Extracts the text layer as <see cref="TextLine"/>s in device pixels of a rendering at <paramref name="dpi"/>,
        /// with the real font size, weight, slant, family and fill color of each line.
        /// </summary>
        public List<TextLine> ExtractLines(int index, float dpi)
        {
            var chars = new List<PdfChar>();
            lock (PdfiumNative.Sync)
            {
                var page = PdfiumNative.FPDF_LoadPage(_doc, index);
                if (page == IntPtr.Zero) return new List<TextLine>();
                var tp = PdfiumNative.FPDFText_LoadPage(page);
                try
                {
                    if (tp == IntPtr.Zero) return new List<TextLine>();
                    int w = (int)Math.Round(PdfiumNative.FPDF_GetPageWidthF(page) * dpi / 72f);
                    int h = (int)Math.Round(PdfiumNative.FPDF_GetPageHeightF(page) * dpi / 72f);
                    int n = PdfiumNative.FPDFText_CountChars(tp);
                    var nameBuf = new byte[256];
                    bool pendingBreak = false;
                    for (int i = 0; i < n; i++)
                    {
                        uint u = PdfiumNative.FPDFText_GetUnicode(tp, i);
                        if (u == '\r' || u == '\n') { pendingBreak = true; continue; }
                        if (u == 0 || u == 0xFFFE || u == 2) continue;
                        if (PdfiumNative.FPDFText_GetLooseCharBox(tp, i, out var rc) == 0) continue;
                        PdfiumNative.FPDF_PageToDevice(page, 0, 0, w, h, 0, rc.Left, rc.Top, out int ax, out int ay);
                        PdfiumNative.FPDF_PageToDevice(page, 0, 0, w, h, 0, rc.Right, rc.Bottom, out int bx, out int by);
                        var box = RectangleF.FromLTRB(Math.Min(ax, bx), Math.Min(ay, by), Math.Max(ax, bx), Math.Max(ay, by));

                        var c = new PdfChar
                        {
                            Text = char.ConvertFromUtf32((int)Math.Min(u, 0x10FFFF)),
                            Box = box,
                            FontPt = (float)PdfiumNative.FPDFText_GetFontSize(tp, i),
                            Break = pendingBreak
                        };
                        pendingBreak = false;
                        uint len = PdfiumNative.FPDFText_GetFontInfo(tp, i, nameBuf, (uint)nameBuf.Length, out int flags);
                        string font = len > 1 ? Encoding.UTF8.GetString(nameBuf, 0, (int)Math.Min(len - 1, nameBuf.Length)) : "";
                        int weight = PdfiumNative.FPDFText_GetFontWeight(tp, i);
                        c.Bold = weight >= 600 || Regex.IsMatch(font, "Bold|Black|Heavy|Semibold|Demi", RegexOptions.IgnoreCase);
                        c.Italic = (flags & PdfiumNative.FPDF_FONT_ITALIC) != 0 || Regex.IsMatch(font, "Italic|Oblique", RegexOptions.IgnoreCase);
                        c.Font = CleanFontName(font);
                        c.Color = PdfiumNative.FPDFText_GetFillColor(tp, i, out uint r, out uint g, out uint b, out uint _) != 0
                            ? Color.FromArgb((int)r, (int)g, (int)b) : Color.Black;
                        chars.Add(c);
                    }
                }
                finally
                {
                    if (tp != IntPtr.Zero) PdfiumNative.FPDFText_ClosePage(tp);
                    PdfiumNative.FPDF_ClosePage(page);
                }
            }
            return BuildLines(chars, dpi);
        }

        private static List<TextLine> BuildLines(List<PdfChar> chars, float dpi)
        {
            var lines = new List<TextLine>();
            var cur = new List<PdfChar>();

            void Flush()
            {
                // Trim spaces at both ends.
                int s = 0, e = cur.Count;
                while (s < e && string.IsNullOrWhiteSpace(cur[s].Text)) s++;
                while (e > s && string.IsNullOrWhiteSpace(cur[e - 1].Text)) e--;
                if (e > s) lines.Add(MakeLine(cur.GetRange(s, e - s), dpi));
                cur.Clear();
            }

            foreach (var c in chars)
            {
                bool space = string.IsNullOrWhiteSpace(c.Text);
                if (cur.Count > 0)
                {
                    var last = cur.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.Text));
                    bool newLine = c.Break;
                    if (!space && last.Text != null)
                    {
                        float lh = Math.Max(1, Math.Min(last.Box.Height, c.Box.Height));
                        float dy = Math.Abs((last.Box.Top + last.Box.Bottom) / 2 - (c.Box.Top + c.Box.Bottom) / 2);
                        if (dy > lh * 0.6f) newLine = true;                       // different baseline
                        else if (c.Box.Left < last.Box.Left - lh) newLine = true; // went backwards
                        else if (c.Box.Left - last.Box.Right > Math.Max(lh, c.FontPt * dpi / 72f) * 1.6f) newLine = true; // column/cell gap
                    }
                    if (newLine) Flush();
                }
                if (space && cur.Count == 0) continue;
                cur.Add(c);
            }
            Flush();
            for (int i = 0; i < lines.Count; i++) lines[i].Id = i + 1;
            return lines;
        }

        private static TextLine MakeLine(List<PdfChar> cs, float dpi)
        {
            var sb = new StringBuilder();
            var left = new List<float>();
            var right = new List<float>();
            RectangleF bounds = RectangleF.Empty;
            foreach (var c in cs)
            {
                var box = c.Box;
                if (string.IsNullOrWhiteSpace(c.Text))
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] == ' ') continue;
                    sb.Append(' ');
                    float x = left.Count > 0 ? right[right.Count - 1] : box.Left;
                    left.Add(x);
                    right.Add(Math.Max(x, box.Right));
                    continue;
                }
                sb.Append(c.Text);
                for (int k = 0; k < c.Text.Length; k++) { left.Add(box.Left); right.Add(box.Right); }
                bounds = bounds.IsEmpty ? box : RectangleF.Union(bounds, box);
            }

            // Style of the dominant (most characters) run.
            var main = cs.Where(c => !string.IsNullOrWhiteSpace(c.Text))
                .GroupBy(c => new { c.Font, Size = (int)Math.Round(c.FontPt * 2), c.Bold, c.Italic, c.Color })
                .OrderByDescending(g => g.Count()).First().First();

            return new TextLine
            {
                Text = sb.ToString(),
                Bounds = bounds,
                InkBounds = bounds,
                Polygon = new[] { new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Top), new PointF(bounds.Right, bounds.Bottom), new PointF(bounds.Left, bounds.Bottom) },
                Confidence = 1f,
                CharLeft = left.ToArray(),
                CharRight = right.ToArray(),
                CharConfidence = Enumerable.Repeat(1f, sb.Length).ToArray(),
                Style = new TextStyle
                {
                    FontSizePt = main.FontPt > 0.5f ? main.FontPt : Math.Max(4, bounds.Height * 72f / dpi * 0.8f),
                    Bold = main.Bold,
                    Italic = main.Italic,
                    Color = main.Color,
                    FontFamily = string.IsNullOrEmpty(main.Font) ? null : main.Font
                }
            };
        }

        /// <summary>"ABCDEF+Arial-BoldMT" → "Arial".</summary>
        internal static string CleanFontName(string font)
        {
            if (string.IsNullOrEmpty(font)) return null;
            int plus = font.IndexOf('+');
            if (plus == 6) font = font.Substring(7);
            font = Regex.Replace(font, @"[-,](Bold|Italic|Oblique|Regular|Roman|Book|Medium|Light|Black|Semibold|SemiBold|Demi|Heavy|BoldItalic|BoldOblique)+.*$", "", RegexOptions.IgnoreCase);
            font = Regex.Replace(font, @"(MT|PS|PSMT)$", "");
            font = Regex.Replace(font, @"(?<=[a-z])(?=[A-Z])", " ").Trim(); // TimesNewRoman → Times New Roman
            return font.Length == 0 ? null : font;
        }

        public void Dispose()
        {
            lock (PdfiumNative.Sync)
            {
                if (_doc != IntPtr.Zero)
                {
                    PdfiumNative.FPDF_CloseDocument(_doc);
                    _doc = IntPtr.Zero;
                }
            }
            if (_pin.IsAllocated) _pin.Free();
        }
    }
}
