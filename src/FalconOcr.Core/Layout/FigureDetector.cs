using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FalconOcr.Engine;
using FalconOcr.Imaging;
using FalconOcr.Model;

namespace FalconOcr.Layout
{
    /// <summary>
    /// Finds non-text graphics (photos, logos, diagrams, decorative shapes): everything that differs from the
    /// page background once text lines and table rules are masked out.
    /// </summary>
    internal static class FigureDetector
    {
        public static List<FigureRegion> Detect(RgbImage img, Color background, IList<TextLine> lines, IEnumerable<RectangleF> rules, IEnumerable<TableRegion> tables, float dpi)
        {
            int cell = Math.Max(2, (int)Math.Round(dpi / 50f));
            int gw = (img.Width + cell - 1) / cell, gh = (img.Height + cell - 1) / cell;
            var grid = new bool[gw * gh];

            int br = background.R, bg = background.G, bb = background.B;
            for (int gy = 0; gy < gh; gy++)
            {
                for (int gx = 0; gx < gw; gx++)
                {
                    int x1 = Math.Min(img.Width, (gx + 1) * cell), y1 = Math.Min(img.Height, (gy + 1) * cell);
                    int hits = 0;
                    for (int y = gy * cell; y < y1; y += 1)
                    {
                        for (int x = gx * cell; x < x1; x += 1)
                        {
                            int i = (y * img.Width + x) * 3;
                            int d = Math.Abs(img.Data[i] - bb) + Math.Abs(img.Data[i + 1] - bg) + Math.Abs(img.Data[i + 2] - br);
                            if (d > 60) hits++;
                        }
                    }
                    grid[gy * gw + gx] = hits * 4 >= cell * cell; // at least a quarter of the cell
                }
            }

            void Clear(RectangleF r, float pad)
            {
                int x0 = Math.Max(0, (int)((r.Left - pad) / cell)), x1 = Math.Min(gw - 1, (int)((r.Right + pad) / cell));
                int y0 = Math.Max(0, (int)((r.Top - pad) / cell)), y1 = Math.Min(gh - 1, (int)((r.Bottom + pad) / cell));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        grid[y * gw + x] = false;
            }

            // Text on a plain background is not a figure: mask text (with anti-aliasing margin), rules and tables.
            var textOnFill = new List<TextLine>();
            foreach (var l in lines)
            {
                if (!l.Style.BackColor.IsEmpty) { textOnFill.Add(l); continue; }
                Clear(l.Bounds, l.Bounds.Height * 0.15f + cell);
            }
            foreach (var r in rules) Clear(r, cell);
            foreach (var t in tables) Clear(t.Bounds, cell * 2);

            // Close small gaps (dilate by 2 cells) then label 8-connected components.
            var dil = Dilate(grid, gw, gh, 2);
            var labels = new int[gw * gh];
            var comps = new List<Rectangle>();
            var compCells = new List<int>();
            var stack = new Stack<int>();
            for (int s = 0; s < dil.Length; s++)
            {
                if (!dil[s] || labels[s] != 0) continue;
                int id = comps.Count + 1;
                labels[s] = id;
                stack.Push(s);
                int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1, count = 0;
                while (stack.Count > 0)
                {
                    int p = stack.Pop();
                    int px = p % gw, py = p / gw;
                    if (grid[p]) count++;
                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = px + dx, ny = py + dy;
                            if (nx < 0 || ny < 0 || nx >= gw || ny >= gh) continue;
                            int q = ny * gw + nx;
                            if (dil[q] && labels[q] == 0) { labels[q] = id; stack.Push(q); }
                        }
                    }
                }
                // Undo the dilation margin (2 cells) on the bounding box.
                comps.Add(Rectangle.FromLTRB(Math.Min(minX + 2, maxX), Math.Min(minY + 2, maxY), Math.Max(maxX - 1, minX + 1), Math.Max(maxY - 1, minY + 1)));
                compCells.Add(count);
            }

            float pageArea = (float)img.Width * img.Height;
            float minSide = Math.Max(dpi * 0.2f, Math.Min(img.Width, img.Height) * 0.02f);
            var rects = new List<RectangleF>();
            for (int i = 0; i < comps.Count; i++)
            {
                var c = comps[i];
                var r = new RectangleF(c.X * cell, c.Y * cell, c.Width * cell, c.Height * cell);
                r.Intersect(new RectangleF(0, 0, img.Width, img.Height));
                if (r.Width < minSide || r.Height < minSide) continue;
                if (r.Width * r.Height < pageArea * 0.002f) continue;
                if (compCells[i] < 20) continue;
                // Scan shadows: thin slivers glued to the page edge.
                bool edge = r.Left <= cell || r.Top <= cell || r.Right >= img.Width - cell || r.Bottom >= img.Height - cell;
                float aspect = Math.Max(r.Width / r.Height, r.Height / r.Width);
                if (edge && aspect > 12) continue;
                // A region that is almost the whole page is a page background/scan tint, not a figure.
                if (r.Width * r.Height > pageArea * 0.85f) continue;
                rects.Add(r);
            }

            // Merge overlapping regions.
            bool merged = true;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < rects.Count && !merged; i++)
                {
                    for (int j = i + 1; j < rects.Count; j++)
                    {
                        if (!RectangleF.Inflate(rects[i], cell, cell).IntersectsWith(rects[j])) continue;
                        rects[i] = RectangleF.Union(rects[i], rects[j]);
                        rects.RemoveAt(j);
                        merged = true;
                        break;
                    }
                }
            }

            var figures = new List<FigureRegion>();
            foreach (var r in rects)
            {
                // Colored panels behind text stay as text with shading instead of becoming pictures.
                float textArea = textOnFill.Where(l => Geometry.Coverage(l.Bounds, r) > 0.7f).Sum(l => l.Bounds.Width * l.Bounds.Height);
                if (textArea > r.Width * r.Height * 0.2f && IsFlatFill(img, r, textOnFill)) continue;

                // Boxes/panels that frame a lot of text are containers, not pictures; the text must stay text.
                var inside = lines.Where(l => Geometry.Coverage(l.Bounds, r) > 0.7f).ToList();
                float insideArea = inside.Sum(l => l.Bounds.Width * l.Bounds.Height);
                if (inside.Count > 6 || insideArea > r.Width * r.Height * 0.25f) continue;

                figures.Add(new FigureRegion { Bounds = r });
            }
            return figures;
        }

        /// <summary>True when the region (outside text) is essentially one color.</summary>
        private static bool IsFlatFill(RgbImage img, RectangleF r, List<TextLine> text)
        {
            var counts = new Dictionary<int, int>();
            int total = 0;
            int step = Math.Max(2, (int)(Math.Min(r.Width, r.Height) / 20));
            for (float y = r.Top; y < r.Bottom; y += step)
            {
                for (float x = r.Left; x < r.Right; x += step)
                {
                    if (text.Any(l => l.Bounds.Contains(x, y))) continue;
                    var c = img.GetPixel((int)Math.Min(img.Width - 1, x), (int)Math.Min(img.Height - 1, y));
                    int key = (c.R >> 5) << 6 | (c.G >> 5) << 3 | (c.B >> 5);
                    counts.TryGetValue(key, out int n);
                    counts[key] = n + 1;
                    total++;
                }
            }
            return total > 0 && counts.Values.Max() > total * 0.8f;
        }

        private static bool[] Dilate(bool[] src, int w, int h, int radius)
        {
            // Separable box dilation.
            var tmp = new bool[src.Length];
            for (int y = 0; y < h; y++)
            {
                int last = -1000000;
                for (int x = 0; x < w; x++) if (src[y * w + x]) { last = x; } else if (x - last <= radius) tmp[y * w + x] = true;
                last = 1000000;
                for (int x = w - 1; x >= 0; x--) if (src[y * w + x]) { last = x; tmp[y * w + x] = true; } else if (last - x <= radius) tmp[y * w + x] = true;
            }
            var dst = new bool[src.Length];
            for (int x = 0; x < w; x++)
            {
                int last = -1000000;
                for (int y = 0; y < h; y++) if (tmp[y * w + x]) { last = y; } else if (y - last <= radius) dst[y * w + x] = true;
                last = 1000000;
                for (int y = h - 1; y >= 0; y--) if (tmp[y * w + x]) { last = y; dst[y * w + x] = true; } else if (last - y <= radius) dst[y * w + x] = true;
            }
            return dst;
        }
    }
}
