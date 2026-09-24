using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FalconOcr.Engine;
using FalconOcr.Imaging;
using FalconOcr.Model;

namespace FalconOcr.Layout
{
    /// <summary>Finds ruled tables: long horizontal/vertical strokes that intersect into a grid.</summary>
    internal static class TableDetector
    {
        internal sealed class Rule
        {
            public bool Horizontal;
            public float Pos;      // y for horizontal, x for vertical (center of the stroke)
            public float Start, End;
            public float Thickness;
            public RectangleF Rect => Horizontal
                ? RectangleF.FromLTRB(Start, Pos - Thickness / 2, End, Pos + Thickness / 2)
                : RectangleF.FromLTRB(Pos - Thickness / 2, Start, Pos + Thickness / 2, End);
        }

        public sealed class Result
        {
            public List<TableRegion> Tables = new List<TableRegion>();
            /// <summary>All ruling strokes (tables or decorative separators) — excluded from figure detection.</summary>
            public List<RectangleF> Rules = new List<RectangleF>();
        }

        public static Result Detect(RgbImage img, bool[] ink, IList<TextLine> lines, float dpi)
        {
            int w = img.Width, h = img.Height;
            int minLen = (int)Math.Max(dpi * 0.25f, Math.Min(w, h) / 40f);
            int maxThick = Math.Max(3, (int)(dpi / 25f));
            int gapTol = Math.Max(2, (int)(dpi / 100f));

            var textRects = lines.Select(l => Shrink(l.Bounds, 0.15f)).ToList();
            var hRules = Merge(FindRuns(ink, w, h, true, minLen, gapTol), true, maxThick, gapTol);
            var vRules = Merge(FindRuns(ink, w, h, false, minLen, gapTol), false, maxThick, gapTol);
            // A rule is a thin stroke with background on both sides; edges of filled areas are not.
            hRules.RemoveAll(r => !ClearOnBothSides(r, ink, w, h));
            vRules.RemoveAll(r => !ClearOnBothSides(r, ink, w, h));
            // Strokes inside text lines are letters (underscores, long dashes), not rules.
            hRules.RemoveAll(r => textRects.Any(t => Geometry.Coverage(r.Rect, t) > 0.6f));
            vRules.RemoveAll(r => textRects.Any(t => Geometry.Coverage(r.Rect, t) > 0.6f));

            var result = new Result();
            result.Rules.AddRange(hRules.Select(r => r.Rect));
            result.Rules.AddRange(vRules.Select(r => r.Rect));

            float tol = Math.Max(4, dpi / 30f);
            var all = hRules.Concat(vRules).ToList();
            var parent = Enumerable.Range(0, all.Count).ToArray();
            int Find(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    var a = all[i];
                    var b = all[j];
                    if (a.Horizontal == b.Horizontal) continue;
                    var hr = a.Horizontal ? a : b;
                    var vr = a.Horizontal ? b : a;
                    if (vr.Pos >= hr.Start - tol && vr.Pos <= hr.End + tol && hr.Pos >= vr.Start - tol && hr.Pos <= vr.End + tol)
                        parent[Find(i)] = Find(j);
                }
            }

            foreach (var grp in Enumerable.Range(0, all.Count).GroupBy(Find))
            {
                var rules = grp.Select(i => all[i]).ToList();
                var hs = rules.Where(r => r.Horizontal).ToList();
                var vs = rules.Where(r => !r.Horizontal).ToList();
                if (hs.Count < 2 || vs.Count < 2) continue;
                var table = BuildGrid(hs, vs, tol, img);
                if (table != null) result.Tables.Add(table);
            }
            return result;
        }

        private static TableRegion BuildGrid(List<Rule> hs, List<Rule> vs, float tol, RgbImage img)
        {
            float left = Math.Min(hs.Min(r => r.Start), vs.Min(r => r.Pos));
            float right = Math.Max(hs.Max(r => r.End), vs.Max(r => r.Pos));
            float top = Math.Min(vs.Min(r => r.Start), hs.Min(r => r.Pos));
            float bottom = Math.Max(vs.Max(r => r.End), hs.Max(r => r.Pos));

            var rows = Cluster(hs.Select(r => r.Pos).Concat(new[] { top, bottom }), tol);
            var cols = Cluster(vs.Select(r => r.Pos).Concat(new[] { left, right }), tol);
            if (rows.Count < 2 || cols.Count < 2) return null;
            if (right - left < 40 || bottom - top < 20) return null;

            var table = new TableRegion { Bounds = RectangleF.FromLTRB(left, top, right, bottom), BorderColor = RuleColor(hs.Concat(vs), img) };
            table.RowEdges.AddRange(rows);
            table.ColumnEdges.AddRange(cols);
            int nr = rows.Count - 1, nc = cols.Count - 1;

            // A border exists between two grid cells if a rule covers most of their shared edge.
            bool VBorder(int r, int c) // between column c-1 and c, row r
            {
                float x = cols[c], y0 = rows[r], y1 = rows[r + 1];
                return vs.Any(v => Math.Abs(v.Pos - x) <= tol && Geometry.OverlapRatio(v.Start, v.End, y0, y1) > 0.5f);
            }
            bool HBorder(int r, int c) // between row r-1 and r, column c
            {
                float y = rows[r], x0 = cols[c], x1 = cols[c + 1];
                return hs.Any(hh => Math.Abs(hh.Pos - y) <= tol && Geometry.OverlapRatio(hh.Start, hh.End, x0, x1) > 0.5f);
            }

            var parent = Enumerable.Range(0, nr * nc).ToArray();
            int Find(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
            void Union(int a, int b) { parent[Find(a)] = Find(b); }
            for (int r = 0; r < nr; r++)
            {
                for (int c = 0; c < nc; c++)
                {
                    if (c + 1 < nc && !VBorder(r, c + 1)) Union(r * nc + c, r * nc + c + 1);
                    if (r + 1 < nr && !HBorder(r + 1, c)) Union(r * nc + c, (r + 1) * nc + c);
                }
            }

            // Make every merged group rectangular (Word/Excel spans must be rectangles).
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var g in Enumerable.Range(0, nr * nc).GroupBy(Find).ToList())
                {
                    int r0 = g.Min(i => i / nc), r1 = g.Max(i => i / nc), c0 = g.Min(i => i % nc), c1 = g.Max(i => i % nc);
                    for (int r = r0; r <= r1; r++)
                        for (int c = c0; c <= c1; c++)
                            if (Find(r * nc + c) != Find(g.Key)) { Union(r * nc + c, g.Key); changed = true; }
                }
            }

            foreach (var g in Enumerable.Range(0, nr * nc).GroupBy(Find))
            {
                int r0 = g.Min(i => i / nc), r1 = g.Max(i => i / nc), c0 = g.Min(i => i % nc), c1 = g.Max(i => i % nc);
                var cell = new TableCell
                {
                    Row = r0,
                    Col = c0,
                    RowSpan = r1 - r0 + 1,
                    ColSpan = c1 - c0 + 1,
                    Bounds = RectangleF.FromLTRB(cols[c0], rows[r0], cols[c1 + 1], rows[r1 + 1])
                };
                table.Cells.Add(cell);
            }
            // A "table" of one cell is a frame/box around content, not a table.
            if (table.Cells.Count < 2) return null;
            table.Cells.Sort((a, b) => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Col.CompareTo(b.Col));
            return table;
        }

        /// <summary>Fill cells with text lines, splitting lines that cross cell borders.</summary>
        public static void AssignLines(TableRegion table, List<TextLine> lines, RgbImage img, Color pageBackground, List<TextLine> consumed)
        {
            var incoming = lines.Where(l => Geometry.Coverage(l.Bounds, table.Bounds) > 0.5f).ToList();
            foreach (var line in incoming)
            {
                consumed.Add(line);
                foreach (var piece in SplitAcrossColumns(line, table))
                {
                    var cell = table.Cells.FirstOrDefault(c => c.Bounds.Contains(piece.CenterX, piece.CenterY))
                               ?? table.Cells.OrderBy(c => DistanceTo(c.Bounds, piece.CenterX, piece.CenterY)).First();
                    cell.Lines.Add(piece);
                }
            }

            foreach (var cell in table.Cells)
            {
                cell.Fill = CellFill(cell, img, pageBackground);
                if (cell.Lines.Count == 0) continue;
                var u = Geometry.Union(cell.Lines.Select(l => l.Bounds));
                float lm = u.Left - cell.Bounds.Left, rm = cell.Bounds.Right - u.Right;
                if (Math.Abs(lm - rm) < cell.Bounds.Width * 0.12f && lm > cell.Bounds.Width * 0.08f) cell.Align = TextAlign.Center;
                else if (rm < lm * 0.35f) cell.Align = TextAlign.Right;
                else cell.Align = TextAlign.Left;
            }
            NormalizeStyles(table);
        }

        /// <summary>
        /// Short cell texts give noisy size/weight measurements: snap sizes to the table's dominant size and
        /// keep bold only where a whole row or column is bold (header rows, label columns).
        /// </summary>
        internal static void NormalizeStyles(TableRegion table)
        {
            var lines = table.Cells.SelectMany(c => c.Lines).ToList();
            if (lines.Count == 0) return;
            float mode = lines.GroupBy(l => (float)Math.Round(l.Style.FontSizePt * 2) / 2)
                .OrderByDescending(g => g.Sum(l => l.Text.Length)).First().Key;
            foreach (var l in lines)
                if (Math.Abs(l.Style.FontSizePt - mode) / mode <= 0.3f) l.Style.FontSizePt = mode;

            bool Majority(IEnumerable<TableCell> cells)
            {
                var ls = cells.SelectMany(c => c.Lines).ToList();
                return ls.Count > 0 && ls.Count(l => l.Style.Bold) * 10 >= ls.Count * 6;
            }
            var boldRows = new HashSet<int>(Enumerable.Range(0, table.RowCount).Where(r => Majority(table.Cells.Where(c => c.Row == r))));
            var boldCols = new HashSet<int>(Enumerable.Range(0, table.ColumnCount).Where(c => table.RowCount > 2 && Majority(table.Cells.Where(x => x.Col == c && !boldRows.Contains(x.Row)))));
            foreach (var cell in table.Cells)
                foreach (var l in cell.Lines)
                    l.Style.Bold = boldRows.Contains(cell.Row) || boldCols.Contains(cell.Col);
        }

        /// <summary>Cuts a line at interior column borders using the per-character positions.</summary>
        internal static IEnumerable<TextLine> SplitAcrossColumns(TextLine line, TableRegion table)
        {
            var cuts = table.ColumnEdges.Skip(1).Take(table.ColumnEdges.Count - 2)
                .Where(x => x > line.Bounds.Left + 2 && x < line.Bounds.Right - 2).ToList();
            if (cuts.Count == 0 || line.CharLeft == null || line.CharLeft.Length != line.Text.Length)
            {
                yield return line;
                yield break;
            }
            int start = 0;
            foreach (var x in cuts.Concat(new[] { float.MaxValue }))
            {
                int end = start;
                while (end < line.Text.Length && (line.CharLeft[end] + line.CharRight[end]) / 2 < x) end++;
                var piece = Slice(line, start, end);
                if (piece != null) yield return piece;
                start = end;
            }
        }

        internal static TextLine Slice(TextLine line, int start, int end)
        {
            while (start < end && char.IsWhiteSpace(line.Text[start])) start++;
            while (end > start && char.IsWhiteSpace(line.Text[end - 1])) end--;
            if (end <= start) return null;
            if (start == 0 && end == line.Text.Length) return line;
            float l = line.CharLeft[start], r = line.CharRight[end - 1];
            var b = RectangleF.FromLTRB(l, line.Bounds.Top, Math.Max(l + 1, r), line.Bounds.Bottom);
            return new TextLine
            {
                Id = line.Id,
                Text = line.Text.Substring(start, end - start),
                Bounds = b,
                InkBounds = RectangleF.FromLTRB(l, line.InkBounds.Top, Math.Max(l + 1, r), line.InkBounds.Bottom),
                Polygon = new[] { new PointF(b.Left, b.Top), new PointF(b.Right, b.Top), new PointF(b.Right, b.Bottom), new PointF(b.Left, b.Bottom) },
                Confidence = line.Confidence,
                CharLeft = line.CharLeft.Skip(start).Take(end - start).ToArray(),
                CharRight = line.CharRight.Skip(start).Take(end - start).ToArray(),
                CharConfidence = line.CharConfidence?.Skip(start).Take(end - start).ToArray(),
                Style = line.Style.Clone()
            };
        }

        private static float DistanceTo(RectangleF r, float x, float y)
        {
            float dx = Math.Max(0, Math.Max(r.Left - x, x - r.Right));
            float dy = Math.Max(0, Math.Max(r.Top - y, y - r.Bottom));
            return dx + dy;
        }

        private static Color CellFill(TableCell cell, RgbImage img, Color pageBackground)
        {
            // Sample the cell interior away from text and borders; the most common color is the fill.
            var inner = RectangleF.Inflate(cell.Bounds, -Math.Min(6, cell.Bounds.Width / 6), -Math.Min(6, cell.Bounds.Height / 6));
            var counts = new Dictionary<int, int>();
            int step = Math.Max(2, (int)(Math.Min(inner.Width, inner.Height) / 12));
            for (float y = inner.Top; y < inner.Bottom; y += step)
            {
                for (float x = inner.Left; x < inner.Right; x += step)
                {
                    int ix = (int)x, iy = (int)y;
                    if (ix < 0 || iy < 0 || ix >= img.Width || iy >= img.Height) continue;
                    if (cell.Lines.Any(l => l.Bounds.Contains(x, y))) continue;
                    var c = img.GetPixel(ix, iy);
                    int key = (c.R >> 3) << 10 | (c.G >> 3) << 5 | (c.B >> 3);
                    counts.TryGetValue(key, out int n);
                    counts[key] = n + 1;
                }
            }
            if (counts.Count == 0) return Color.Empty;
            int k = counts.OrderByDescending(kv => kv.Value).First().Key;
            var fill = Color.FromArgb(((k >> 10) & 31) * 8 + 4, ((k >> 5) & 31) * 8 + 4, (k & 31) * 8 + 4);
            return StyleEstimator.Distance(fill, pageBackground) > 36 ? fill : Color.Empty;
        }

        private static Color RuleColor(IEnumerable<Rule> rules, RgbImage img)
        {
            long r = 0, g = 0, b = 0, n = 0;
            foreach (var rule in rules)
            {
                for (float t = rule.Start; t < rule.End; t += Math.Max(1, (rule.End - rule.Start) / 20))
                {
                    int x = (int)(rule.Horizontal ? t : rule.Pos), y = (int)(rule.Horizontal ? rule.Pos : t);
                    if (x < 0 || y < 0 || x >= img.Width || y >= img.Height) continue;
                    var c = img.GetPixel(x, y);
                    r += c.R; g += c.G; b += c.B; n++;
                }
            }
            return n == 0 ? Color.Black : StyleEstimator.SnapColor(Color.FromArgb((int)(r / n), (int)(g / n), (int)(b / n)));
        }

        private static List<float> Cluster(IEnumerable<float> values, float tol)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var result = new List<float>();
            var group = new List<float>();
            foreach (var v in sorted)
            {
                if (group.Count > 0 && v - group[group.Count - 1] > tol)
                {
                    result.Add(group.Average());
                    group.Clear();
                }
                group.Add(v);
            }
            if (group.Count > 0) result.Add(group.Average());
            return result;
        }

        private struct Run
        {
            public int Line, Start, End; // row (or column) index and extent along it
        }

        /// <summary>Long ink runs along rows (horizontal) or columns (vertical), tolerating small breaks.</summary>
        private static List<Run> FindRuns(bool[] ink, int w, int h, bool horizontal, int minLen, int gapTol)
        {
            var runs = new List<Run>();
            int lines = horizontal ? h : w, len = horizontal ? w : h;
            for (int l = 0; l < lines; l++)
            {
                int start = -1, lastInk = -1;
                for (int p = 0; p <= len; p++)
                {
                    bool on = p < len && (horizontal ? ink[l * w + p] : ink[p * w + l]);
                    if (on)
                    {
                        if (start < 0) start = p;
                        lastInk = p;
                    }
                    else if (start >= 0 && p - lastInk > gapTol)
                    {
                        if (lastInk - start + 1 >= minLen) runs.Add(new Run { Line = l, Start = start, End = lastInk + 1 });
                        start = -1;
                    }
                }
            }
            return runs;
        }

        /// <summary>Joins runs on adjacent rows/columns into strokes; thick strokes are filled areas, not rules.</summary>
        private static List<Rule> Merge(List<Run> runs, bool horizontal, int maxThick, int gapTol)
        {
            var open = new List<List<Run>>();
            var done = new List<List<Run>>();
            foreach (var run in runs.OrderBy(r => r.Line))
            {
                List<Run> target = null;
                foreach (var g in open)
                {
                    var last = g[g.Count - 1];
                    if (run.Line - last.Line <= 1 && run.Start < last.End && run.End > last.Start) { target = g; break; }
                }
                if (target != null) target.Add(run);
                else open.Add(new List<Run> { run });
                // Retire groups that can no longer grow.
                for (int i = open.Count - 1; i >= 0; i--)
                {
                    if (run.Line - open[i][open[i].Count - 1].Line > 1)
                    {
                        done.Add(open[i]);
                        open.RemoveAt(i);
                    }
                }
            }
            done.AddRange(open);

            var rules = new List<Rule>();
            foreach (var g in done)
            {
                int thick = g[g.Count - 1].Line - g[0].Line + 1;
                if (thick > maxThick) continue;
                rules.Add(new Rule
                {
                    Horizontal = horizontal,
                    Pos = (g[0].Line + g[g.Count - 1].Line + 1) / 2f,
                    Start = g.Min(r => r.Start),
                    End = g.Max(r => r.End),
                    Thickness = thick
                });
            }

            // Collinear pieces broken by crossings/scan noise become one rule.
            rules = rules.OrderBy(r => r.Pos).ThenBy(r => r.Start).ToList();
            var merged = new List<Rule>();
            foreach (var r in rules)
            {
                var m = merged.FirstOrDefault(x => Math.Abs(x.Pos - r.Pos) <= 2 && r.Start <= x.End + gapTol * 4 && r.End >= x.Start - gapTol * 4);
                if (m != null)
                {
                    m.Start = Math.Min(m.Start, r.Start);
                    m.End = Math.Max(m.End, r.End);
                    m.Thickness = Math.Max(m.Thickness, r.Thickness);
                }
                else merged.Add(r);
            }
            return merged;
        }

        private static bool ClearOnBothSides(Rule r, bool[] ink, int w, int h)
        {
            int off = (int)Math.Ceiling(r.Thickness / 2) + 2;
            int before = (int)r.Pos - off, after = (int)r.Pos + off;
            int limit = r.Horizontal ? h : w;
            if (before < 0 || after >= limit) return true; // at the image edge: only one side can be checked
            int n = 0, inkA = 0, inkB = 0;
            for (int t = (int)r.Start; t < (int)r.End; t += 2)
            {
                n++;
                if (r.Horizontal ? ink[before * w + t] : ink[t * w + before]) inkA++;
                if (r.Horizontal ? ink[after * w + t] : ink[t * w + after]) inkB++;
            }
            // Crossing rules and text touching the line are fine; a solid fill on one side is not.
            return n > 0 && inkA < n * 0.5f && inkB < n * 0.5f;
        }

        private static RectangleF Shrink(RectangleF r, float f) => RectangleF.Inflate(r, -r.Width * 0.02f, -r.Height * f);
    }
}
