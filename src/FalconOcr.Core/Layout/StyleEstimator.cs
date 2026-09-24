using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FalconOcr.Imaging;
using FalconOcr.Model;

namespace FalconOcr.Layout
{
    /// <summary>
    /// Recovers typographic attributes of OCR lines from pixels: font size (from ink height and the letters
    /// present), weight (stroke width), text color and background fill.
    /// </summary>
    internal static class StyleEstimator
    {
        private const string Ascenders = "bdfhklt";
        private const string Descenders = "gjpqy";

        public static Color EstimateBackground(RgbImage img)
        {
            // Mode of coarsely quantized colors on a sparse grid.
            var counts = new Dictionary<int, int>();
            int step = Math.Max(1, Math.Min(img.Width, img.Height) / 150);
            for (int y = 0; y < img.Height; y += step)
            {
                for (int x = 0; x < img.Width; x += step)
                {
                    int i = (y * img.Width + x) * 3;
                    int key = (img.Data[i] >> 4) | ((img.Data[i + 1] >> 4) << 4) | ((img.Data[i + 2] >> 4) << 8);
                    counts.TryGetValue(key, out int c);
                    counts[key] = c + 1;
                }
            }
            int best = counts.OrderByDescending(kv => kv.Value).First().Key;
            int b = (best & 15) * 16 + 8, g = ((best >> 4) & 15) * 16 + 8, r = ((best >> 8) & 15) * 16 + 8;
            var c0 = Color.FromArgb(r, g, b);
            // Near-white pages are exported as pure white.
            return c0.R > 225 && c0.G > 225 && c0.B > 225 ? Color.White : c0;
        }

        public static void Estimate(RgbImage img, IList<TextLine> lines, float dpi, Color pageBackground, string defaultFont)
        {
            var strokeRatios = new List<float>();
            var ratioByLine = new Dictionary<TextLine, float>();
            foreach (var line in lines)
            {
                var m = Measure(img, line, pageBackground);
                if (m == null) continue;
                line.InkBounds = m.Ink;
                // Anti-aliasing adds about one pixel to the measured ink extent.
                float fontPx = FontPixels(Math.Max(1, m.Ink.Height - 1), line.Text);
                if (fontPx <= 0) fontPx = line.Bounds.Height * 0.72f;
                line.Style.FontSizePt = RoundHalf(fontPx * 72f / dpi);
                line.Style.Color = SnapColor(m.Ink0);
                line.Style.BackColor = Distance(m.Back, pageBackground) > 45 ? m.Back : Color.Empty;
                if (line.Style.FontFamily == null) line.Style.FontFamily = defaultFont;
                if (m.StrokeWidth > 0 && fontPx > 0)
                {
                    float ratio = m.StrokeWidth / fontPx;
                    ratioByLine[line] = ratio;
                    if (line.Text.Length >= 4) strokeRatios.Add(ratio);
                }
            }

            if (strokeRatios.Count == 0) return;
            strokeRatios.Sort();
            float median = strokeRatios[strokeRatios.Count / 2];
            foreach (var kv in ratioByLine)
                kv.Key.Style.Bold = kv.Value > median * 1.25f;
        }

        /// <summary>
        /// Converts the measured ink height into an em size, accounting for which vertical zones the
        /// letters occupy (x-height only, ascenders/caps, descenders).
        /// </summary>
        private static float FontPixels(float inkHeight, string text)
        {
            if (inkHeight <= 0 || string.IsNullOrEmpty(text)) return 0;
            bool cjk = text.Any(LayoutBlock.IsCjk);
            if (cjk) return inkHeight / 0.88f;
            bool asc = text.Any(c => char.IsUpper(c) || char.IsDigit(c) || Ascenders.IndexOf(c) >= 0 || "ЁЙБДФ".IndexOf(c) >= 0);
            // Commas descend only ~0.1em; they must not count as full descenders (numbers like "1,250").
            bool desc = text.Any(c => Descenders.IndexOf(c) >= 0 || "дзруфцщ".IndexOf(c) >= 0);
            bool paren = text.IndexOf('(') >= 0 || text.IndexOf(')') >= 0 || text.IndexOf('[') >= 0;
            float span;
            if (paren) span = 0.95f;
            else if (asc && desc) span = 0.92f;
            else if (asc) span = 0.72f;
            else if (desc) span = 0.72f;
            else span = 0.52f; // x-height only ("was on a...")
            return inkHeight / span;
        }

        private sealed class Measurement
        {
            public RectangleF Ink;
            public Color Ink0;
            public Color Back;
            public float StrokeWidth;
        }

        private static Measurement Measure(RgbImage img, TextLine line, Color pageBackground)
        {
            var r = Rectangle.Round(line.Bounds);
            r.Intersect(new Rectangle(0, 0, img.Width, img.Height));
            if (r.Width < 3 || r.Height < 3) return null;

            var hist = new int[256];
            for (int y = r.Top; y < r.Bottom; y++)
                for (int x = r.Left; x < r.Right; x++)
                    hist[img.Gray(x, y)]++;
            int t = ImageOps.Otsu(hist);
            long below = 0, sumBelow = 0, sumAbove = 0, total = (long)r.Width * r.Height;
            for (int i = 0; i < 256; i++)
            {
                if (i <= t) { below += hist[i]; sumBelow += (long)i * hist[i]; }
                else sumAbove += (long)i * hist[i];
            }
            // Polarity: the class that looks like the page background is the background. When neither does
            // (text on a colored panel) the majority class is the background — this supports light-on-dark text.
            float meanBelow = below == 0 ? 0 : (float)sumBelow / below, meanAbove = total == below ? 255 : (float)sumAbove / (total - below);
            int pageGray = (pageBackground.R * 77 + pageBackground.G * 150 + pageBackground.B * 29) >> 8;
            bool darkInk;
            if (Math.Abs(meanAbove - pageGray) < 40) darkInk = true;
            else if (Math.Abs(meanBelow - pageGray) < 40) darkInk = false;
            else darkInk = below <= total / 2;

            var mask = new bool[r.Width * r.Height];
            var rowInk = new int[r.Height];
            var colInk = new int[r.Width];
            long ir = 0, ig = 0, ib = 0, n = 0, br = 0, bg = 0, bb = 0, bn = 0, area = 0;
            // Contrast weighting: the strongest ink pixels give the true text color (edges are anti-aliased).
            int strong = darkInk ? Math.Max(0, t - (t - MinGray(hist)) / 2) : Math.Min(255, t + (MaxGray(hist) - t) / 2);
            for (int y = r.Top; y < r.Bottom; y++)
            {
                for (int x = r.Left; x < r.Right; x++)
                {
                    int g = img.Gray(x, y);
                    bool ink = darkInk ? g <= t : g > t;
                    int i = (y * img.Width + x) * 3;
                    if (ink)
                    {
                        mask[(y - r.Top) * r.Width + (x - r.Left)] = true;
                        area++;
                        rowInk[y - r.Top]++;
                        colInk[x - r.Left]++;
                        if (darkInk ? g <= strong : g >= strong)
                        {
                            ib += img.Data[i]; ig += img.Data[i + 1]; ir += img.Data[i + 2]; n++;
                        }
                    }
                    else
                    {
                        bb += img.Data[i]; bg += img.Data[i + 1]; br += img.Data[i + 2]; bn++;
                    }
                }
            }
            if (n == 0 || bn == 0) return null;

            // Stroke width ≈ 2·area / perimeter: independent of letter shapes (unlike run lengths).
            long edge = 0;
            for (int y = 0; y < r.Height; y++)
            {
                for (int x = 0; x < r.Width; x++)
                {
                    int p = y * r.Width + x;
                    if (!mask[p]) continue;
                    if (x > 0 && !mask[p - 1]) edge++;
                    if (x < r.Width - 1 && !mask[p + 1]) edge++;
                    if (y > 0 && !mask[p - r.Width]) edge++;
                    if (y < r.Height - 1 && !mask[p + r.Width]) edge++;
                }
            }

            // Text body = rows carrying substantial ink; then grow over ascender/descender rows as long as
            // they stay contiguous (a single "p" descender has very little ink, a neighbor line is separated by blank rows).
            int peak = rowInk.Max();
            int body = Math.Max(1, peak / 6);
            int top = 0, bottom = r.Height - 1;
            while (top < r.Height && rowInk[top] < body) top++;
            while (bottom > top && rowInk[bottom] < body) bottom--;
            if (top >= bottom) return null;
            int weak = Math.Max(1, r.Width / 500);
            while (top > 0 && rowInk[top - 1] >= weak) top--;
            while (bottom < r.Height - 1 && rowInk[bottom + 1] >= weak) bottom++;
            int left = 0, right = r.Width - 1;
            while (left < r.Width && colInk[left] == 0) left++;
            while (right > left && colInk[right] == 0) right--;
            if (top >= bottom) return null;

            return new Measurement
            {
                Ink = new RectangleF(r.Left + left, r.Top + top, right - left + 1, bottom - top + 1),
                Ink0 = Color.FromArgb((int)(ir / n), (int)(ig / n), (int)(ib / n)),
                Back = Color.FromArgb((int)(br / bn), (int)(bg / bn), (int)(bb / bn)),
                StrokeWidth = edge == 0 ? 0 : 2f * area / edge
            };
        }

        private static int MinGray(int[] h)
        {
            for (int i = 0; i < 256; i++) if (h[i] > 0) return i;
            return 0;
        }

        private static int MaxGray(int[] h)
        {
            for (int i = 255; i >= 0; i--) if (h[i] > 0) return i;
            return 255;
        }

        /// <summary>Snaps near-black/near-white/grayish ink to clean values so exports look intentional.</summary>
        public static Color SnapColor(Color c)
        {
            int max = Math.Max(c.R, Math.Max(c.G, c.B)), min = Math.Min(c.R, Math.Min(c.G, c.B));
            if (max < 80 && max - min < 30) return Color.Black;
            if (min > 225) return Color.White;
            if (max - min < 18)
            {
                int v = (c.R + c.G + c.B) / 3;
                v = (int)(Math.Round(v / 16.0) * 16);
                v = Math.Min(255, v);
                return Color.FromArgb(v, v, v);
            }
            return c;
        }

        public static int Distance(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

        private static float RoundHalf(float v) => (float)Math.Max(4, Math.Round(v * 2) / 2);
    }
}
