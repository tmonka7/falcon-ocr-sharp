using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using FalconOcr.Engine;
using FalconOcr.Imaging;
using FalconOcr.Model;

namespace FalconOcr.Layout
{
    /// <summary>
    /// Rebuilds the logical structure of a page from positioned text lines: tables, figures, column
    /// sections, reading order, paragraphs, headings, list items, alignment, spacing and styles.
    /// </summary>
    public static class LayoutAnalyzer
    {
        private static readonly Regex BulletPrefix = new Regex(@"^([•·●○◦■□▪▫‣⁃➢➤►✓✔✗❖◆◇\-–—*])\s*(?=\S)", RegexOptions.Compiled);
        private static readonly Regex OrderedPrefix = new Regex(@"^(\(?(\d{1,3}|[a-zA-Z]|[ivxIVX]{1,5})[.)])\s+(?=\S)", RegexOptions.Compiled);
        private const string LooseBulletChars = "•·●○◦■□▪▫‣⁃➢➤►✓✔✗❖◆◇-–—*oeO°»>";

        /// <summary>Graphic bullets found left of lines on the page being analyzed (per thread).</summary>
        [ThreadStatic] private static Dictionary<TextLine, RectangleF> _bullets;

        private sealed class Item
        {
            public RectangleF R;
            public TextLine Line;
            public TableRegion Table;
            public FigureRegion Figure;
            public bool IsSpecial => Table != null || Figure != null;
            public float CenterX => R.X + R.Width / 2;
        }

        private sealed class VLine
        {
            public LayoutLine Line;
            public Item Special;
            public RectangleF R;
            public TextStyle Style => Line?.Style;
        }

        public static OcrPage Analyze(RgbImage img, List<TextLine> lines, float dpi, OcrOptions o, bool fromTextLayer, string defaultFont)
        {
            var page = new OcrPage { Width = img.Width, Height = img.Height, Dpi = dpi, FromTextLayer = fromTextLayer };
            page.Background = StyleEstimator.EstimateBackground(img);

            if (!fromTextLayer)
            {
                StyleEstimator.Estimate(img, lines, dpi, page.Background, defaultFont);
                NormalizeSizes(lines);
            }
            else
            {
                foreach (var l in lines)
                {
                    if (l.Style.FontFamily == null) l.Style.FontFamily = defaultFont;
                    if (l.InkBounds.IsEmpty) l.InkBounds = l.Bounds;
                }
            }

            var textLines = lines.Where(l => l.Text.Trim().Length > 0).ToList();
            float hMed = Median(textLines.Select(l => l.Bounds.Height), dpi / 6f);

            if (o.Layout == LayoutMode.LinesOnly)
            {
                page.Lines.AddRange(textLines);
                BuildLinesOnly(page, textLines);
                Renumber(page);
                return page;
            }

            bool auto = o.Layout == LayoutMode.Automatic;
            var tables = new List<TableRegion>();
            var rules = new List<RectangleF>();
            var consumed = new List<TextLine>();
            var ink = Binarize(img);
            if (auto && (o.DetectTables || o.DetectFigures))
            {
                var tr = TableDetector.Detect(img, ink, textLines, dpi);
                rules = tr.Rules;
                if (o.DetectTables)
                {
                    foreach (var t in tr.Tables)
                    {
                        TableDetector.AssignLines(t, textLines.Except(consumed).ToList(), img, page.Background, consumed);
                        tables.Add(t);
                    }
                }
            }

            // Lines split across table cells are replaced by their pieces.
            page.Lines.AddRange(textLines.Except(consumed));
            foreach (var t in tables) foreach (var c in t.Cells) page.Lines.AddRange(c.Lines);

            var figures = new List<FigureRegion>();
            if (auto && o.DetectFigures)
            {
                figures = FigureDetector.Detect(img, page.Background, page.Lines, rules, tables, dpi);
                foreach (var f in figures)
                {
                    foreach (var l in page.Lines)
                        if (!consumed.Contains(l) && Geometry.Coverage(l.Bounds, f.Bounds) > 0.7f) l.InFigure = true;
                    f.Png = img.ToPng(Rectangle.Round(f.Bounds));
                }
            }

            var inTables = new HashSet<TextLine>(tables.SelectMany(t => t.Cells.SelectMany(c => c.Lines)));
            var items = page.Lines.Where(l => !l.InFigure && !inTables.Contains(l)).Select(l => new Item { R = l.Bounds, Line = l }).ToList();
            items.AddRange(tables.Select(t => new Item { R = t.Bounds, Table = t }));
            items.AddRange(figures.Select(f => new Item { R = f.Bounds, Figure = f }));
            if (items.Count == 0)
            {
                Renumber(page);
                return page;
            }

            var content = Geometry.Union(items.Select(i => i.R));
            float colGap = Math.Max(hMed * 1.1f, page.Width * 0.012f);
            var flowLines = items.Where(i => i.Line != null).Select(i => i.Line).ToList();
            _bullets = FindBulletGlyphs(ink, img.Width, img.Height, flowLines);
            var sections = Segment(items, content, colGap, hMed, auto, o.DetectTables);

            float prevBottom = content.Top;
            foreach (var s in sections)
            {
                s.SpaceBefore = Math.Max(0, s.Bounds.Top - prevBottom);
                prevBottom = s.Bounds.Bottom;
                page.Sections.Add(s);
                page.Blocks.AddRange(s.Blocks);
            }
            ClassifyHeadings(page);
            Renumber(page);
            return page;
        }

        // ---------------------------------------------------------------- segmentation

        private static List<LayoutSection> Segment(List<Item> items, RectangleF region, float colGap, float hMed, bool allowColumns, bool borderless)
        {
            // 1) Bands: maximal runs of items whose vertical extents overlap substantially.
            var sorted = items.OrderBy(i => i.R.Top).ThenBy(i => i.R.Left).ToList();
            var bands = new List<List<Item>>();
            float bandBottom = float.MinValue, bandMinH = 0;
            foreach (var it in sorted)
            {
                float h = Math.Min(it.R.Height, bandMinH <= 0 ? it.R.Height : bandMinH);
                if (bands.Count > 0 && it.R.Top < bandBottom - h * 0.4f)
                {
                    bands[bands.Count - 1].Add(it);
                    bandBottom = Math.Max(bandBottom, it.R.Bottom);
                    if (!it.IsSpecial) bandMinH = Math.Min(bandMinH, it.R.Height);
                }
                else
                {
                    bands.Add(new List<Item> { it });
                    bandBottom = it.R.Bottom;
                    bandMinH = it.IsSpecial ? hMed : it.R.Height;
                }
            }

            // 2) Group bands into sections with a stable set of column gutters.
            var groups = new List<(List<List<Item>> Bands, List<(float A, float B)> Common, bool HasCols)>();
            foreach (var band in bands)
            {
                var free = Subtract(region.Left, region.Right, band.Select(i => (i.R.Left, i.R.Right)));
                bool bandHas = allowColumns && HasGutter(free, region, colGap);
                if (groups.Count == 0)
                {
                    groups.Add((new List<List<Item>> { band }, free, bandHas));
                    continue;
                }
                var g = groups[groups.Count - 1];
                var common = Intersect(g.Common, free);
                bool newHas = allowColumns && HasGutter(common, region, colGap);
                if ((g.HasCols && !newHas) || (!g.HasCols && bandHas && !newHas))
                {
                    groups.Add((new List<List<Item>> { band }, free, bandHas));
                }
                else
                {
                    g.Bands.Add(band);
                    groups[groups.Count - 1] = (g.Bands, common, newHas);
                }
            }

            // 3) Columns → blocks.
            var sections = new List<LayoutSection>();
            foreach (var g in groups)
            {
                var all = g.Bands.SelectMany(b => b).ToList();
                var section = new LayoutSection { Bounds = Geometry.Union(all.Select(i => i.R)) };
                var gutters = g.HasCols && g.Bands.Count >= 2
                    ? g.Common.Where(c => IsInterior(c, region) && c.B - c.A >= colGap).ToList()
                    : new List<(float A, float B)>();

                if (gutters.Count > 0 && borderless && LooksLikeBorderlessTable(g.Bands, gutters))
                {
                    var table = BuildBorderlessTable(g.Bands, gutters, section.Bounds);
                    section.Columns.Add(section.Bounds);
                    var blk = new LayoutBlock { Kind = BlockKind.Table, Table = table, Bounds = table.Bounds, Section = section };
                    section.Blocks.Add(blk);
                    sections.Add(section);
                    continue;
                }

                var edges = new List<float> { float.MinValue };
                edges.AddRange(gutters.Select(c => (c.A + c.B) / 2));
                edges.Add(float.MaxValue);
                for (int c = 0; c < edges.Count - 1; c++)
                {
                    var colItems = all.Where(i => i.CenterX >= edges[c] && i.CenterX < edges[c + 1]).ToList();
                    if (colItems.Count == 0) continue;
                    var cb = Geometry.Union(colItems.Select(i => i.R));
                    var colRect = RectangleF.FromLTRB(cb.Left, section.Bounds.Top, cb.Right, section.Bounds.Bottom);
                    int colIndex = section.Columns.Count;
                    section.Columns.Add(colRect);
                    foreach (var b in BuildBlocks(colItems, colRect, hMed))
                    {
                        b.Section = section;
                        b.Column = colIndex;
                        section.Blocks.Add(b);
                    }
                }
                sections.Add(section);
            }
            return sections;
        }

        private static bool LooksLikeBorderlessTable(List<List<Item>> bands, List<(float A, float B)> gutters)
        {
            if (bands.Count < 3) return false;
            var items = bands.SelectMany(b => b).ToList();
            if (items.Any(i => i.IsSpecial)) return false;
            var edges = gutters.Select(g => (g.A + g.B) / 2).ToList();
            int Col(Item i) => edges.Count(e => i.CenterX > e);
            int multi = bands.Count(b => b.Select(Col).Distinct().Count() >= 2);
            var lens = items.Select(i => i.Line.Text.Length).OrderBy(x => x).ToList();
            float medLen = lens[lens.Count / 2];
            int cols = gutters.Count + 1;
            return multi >= bands.Count * 0.6f && medLen <= 30 && (cols >= 3 || medLen <= 16);
        }

        private static TableRegion BuildBorderlessTable(List<List<Item>> bands, List<(float A, float B)> gutters, RectangleF bounds)
        {
            var t = new TableRegion { Bounds = bounds, HasBorders = false };
            t.ColumnEdges.Add(bounds.Left);
            t.ColumnEdges.AddRange(gutters.Select(g => (g.A + g.B) / 2));
            t.ColumnEdges.Add(bounds.Right);
            var rects = bands.Select(b => Geometry.Union(b.Select(i => i.R))).ToList();
            t.RowEdges.Add(bounds.Top);
            for (int i = 1; i < rects.Count; i++) t.RowEdges.Add((rects[i - 1].Bottom + rects[i].Top) / 2);
            t.RowEdges.Add(bounds.Bottom);
            for (int r = 0; r < bands.Count; r++)
            {
                for (int c = 0; c < t.ColumnCount; c++)
                {
                    var cell = new TableCell { Row = r, Col = c, Bounds = RectangleF.FromLTRB(t.ColumnEdges[c], t.RowEdges[r], t.ColumnEdges[c + 1], t.RowEdges[r + 1]) };
                    cell.Lines.AddRange(bands[r].Where(i => i.CenterX >= t.ColumnEdges[c] && (i.CenterX < t.ColumnEdges[c + 1] || c == t.ColumnCount - 1)).Select(i => i.Line));
                    t.Cells.Add(cell);
                }
            }
            TableDetector.NormalizeStyles(t);
            return t;
        }

        private static bool HasGutter(List<(float A, float B)> free, RectangleF region, float minWidth)
            => free.Any(f => IsInterior(f, region) && f.B - f.A >= minWidth);

        private static bool IsInterior((float A, float B) f, RectangleF region)
            => f.A > region.Left + 1 && f.B < region.Right - 1;

        private static List<(float A, float B)> Subtract(float left, float right, IEnumerable<(float A, float B)> covered)
        {
            var free = new List<(float A, float B)> { (left, right) };
            foreach (var c in covered)
            {
                var next = new List<(float A, float B)>();
                foreach (var f in free)
                {
                    if (c.B <= f.A || c.A >= f.B) { next.Add(f); continue; }
                    if (c.A > f.A) next.Add((f.A, c.A));
                    if (c.B < f.B) next.Add((c.B, f.B));
                }
                free = next;
            }
            return free;
        }

        private static List<(float A, float B)> Intersect(List<(float A, float B)> a, List<(float A, float B)> b)
        {
            var r = new List<(float A, float B)>();
            foreach (var x in a)
                foreach (var y in b)
                {
                    float s = Math.Max(x.A, y.A), e = Math.Min(x.B, y.B);
                    if (e > s) r.Add((s, e));
                }
            return r;
        }

        // ---------------------------------------------------------------- blocks

        private static List<LayoutBlock> BuildBlocks(List<Item> items, RectangleF col, float hMed)
        {
            // Visual lines: segments sharing a baseline.
            var vlines = new List<VLine>();
            foreach (var it in items.OrderBy(i => i.R.Top).ThenBy(i => i.R.Left))
            {
                if (it.IsSpecial)
                {
                    vlines.Add(new VLine { Special = it, R = it.R });
                    continue;
                }
                var target = vlines.LastOrDefault(v => v.Line != null && Geometry.OverlapRatio(v.R.Top, v.R.Bottom, it.R.Top, it.R.Bottom) > 0.55f);
                if (target == null)
                {
                    target = new VLine { Line = new LayoutLine(), R = it.R };
                    vlines.Add(target);
                }
                target.Line.Segments.Add(it.Line);
                target.R = RectangleF.Union(target.R, it.R);
            }
            vlines = vlines.OrderBy(v => v.R.Top).ToList();
            foreach (var v in vlines.Where(v => v.Line != null))
            {
                v.Line.Segments.Sort((a, b) => a.Bounds.Left.CompareTo(b.Bounds.Left));
                v.Line.Bounds = v.R;
            }

            var gaps = new List<float>();
            for (int i = 1; i < vlines.Count; i++)
                if (vlines[i].Line != null && vlines[i - 1].Line != null)
                    gaps.Add(vlines[i].R.Top - vlines[i - 1].R.Bottom);
            float medGap = Median(gaps, hMed * 0.3f);

            var blocks = new List<LayoutBlock>();
            LayoutBlock cur = null;
            VLine prev = null;
            float colW = Math.Max(1, col.Width);
            foreach (var v in vlines)
            {
                if (v.Special != null)
                {
                    var sb = new LayoutBlock
                    {
                        Kind = v.Special.Table != null ? BlockKind.Table : BlockKind.Figure,
                        Table = v.Special.Table,
                        Figure = v.Special.Figure,
                        Bounds = v.R
                    };
                    blocks.Add(sb);
                    cur = null;
                    prev = v;
                    continue;
                }

                float h = v.R.Height;
                bool isMarker = TryListMarker(v.Line, hMed, out var marker, out bool ordered, out int prefixLen, out float textLeft, out var glyphBounds);
                bool newBlock = cur == null || prev == null || prev.Line == null;
                if (!newBlock)
                {
                    float lh = Math.Min(prev.R.Height, h);
                    float gap = v.R.Top - prev.R.Bottom;
                    if (gap > medGap + Math.Max(lh * 0.4f, 4f)) newBlock = true;
                    else if (!v.Style.SameAs(prev.Style, 0.16f)) newBlock = true;
                    else if (isMarker) newBlock = true;
                    else if (cur.Kind == BlockKind.ListItem)
                    {
                        // Continuation lines of a list item align with the item text, not the marker.
                        if (Math.Abs(v.R.Left - cur.TextLeft) > Math.Max(lh, hMed) * 1.0f) newBlock = true;
                    }
                    else
                    {
                        bool prevShort = prev.R.Right < col.Right - Math.Max(3 * lh, colW * 0.12f);
                        bool indented = v.R.Left > prev.R.Left + lh * 1.2f && cur.Lines.Count >= 2;
                        bool outdentOk = cur.Lines.Count == 1 && v.R.Left < prev.R.Left - lh * 0.8f; // first-line indent
                        bool centered = IsCentered(prev.R, col) && IsCentered(v.R, col);
                        if (prevShort && !centered) newBlock = true;
                        else if (indented) newBlock = true;
                        else if (!outdentOk && Math.Abs(v.R.Left - prev.R.Left) > lh * 1.2f && !centered) newBlock = true;
                    }
                }

                if (newBlock)
                {
                    cur = new LayoutBlock { Kind = isMarker ? BlockKind.ListItem : BlockKind.Paragraph };
                    if (isMarker)
                    {
                        cur.ListMarker = marker;
                        cur.OrderedList = ordered;
                        cur.MarkerPrefixLength = prefixLen;
                        cur.TextLeft = textLeft;
                        cur.MarkerGlyph = glyphBounds;
                    }
                    blocks.Add(cur);
                }
                cur.Lines.Add(v.Line);
                prev = v;
            }

            float prevBottom = col.Top;
            foreach (var b in blocks)
            {
                if (b.Kind != BlockKind.Table && b.Kind != BlockKind.Figure) FinishTextBlock(b, col);
                b.SpaceBefore = Math.Max(0, b.Bounds.Top - prevBottom);
                prevBottom = b.Bounds.Bottom;
            }
            return blocks;
        }

        private static void FinishTextBlock(LayoutBlock b, RectangleF col)
        {
            b.Bounds = Geometry.Union(b.Lines.Select(l => l.Bounds));
            // Dominant style by character count; the whole paragraph gets one size.
            var segs = b.Lines.SelectMany(l => l.Segments).ToList();
            var main = segs.OrderByDescending(s => s.Text.Length).First().Style;
            b.Style = main.Clone();
            b.Style.Bold = segs.Sum(s => s.Style.Bold ? s.Text.Length : 0) * 2 > segs.Sum(s => s.Text.Length);

            if (b.Lines.Count > 1)
            {
                var tops = b.Lines.Select(l => l.Bounds.Top).ToList();
                b.LineSpacing = Median(Enumerable.Range(1, tops.Count - 1).Select(i => tops[i] - tops[i - 1]), b.Lines[0].Bounds.Height * 1.2f);
            }
            else b.LineSpacing = b.Lines[0].Bounds.Height * 1.2f;

            if (b.Kind == BlockKind.ListItem)
            {
                b.FirstLineIndent = b.Bounds.Left - b.TextLeft;
                b.Bounds = RectangleF.FromLTRB(b.TextLeft, b.Bounds.Top, b.Bounds.Right, b.Bounds.Bottom);
            }
            else if (b.Lines.Count > 1)
            {
                float rest = b.Lines.Skip(1).Min(l => l.Bounds.Left);
                b.FirstLineIndent = b.Lines[0].Bounds.Left - rest;
                b.Bounds = RectangleF.FromLTRB(rest, b.Bounds.Top, b.Bounds.Right, b.Bounds.Bottom);
            }
            b.Align = DetectAlign(b, col);
        }

        private static TextAlign DetectAlign(LayoutBlock b, RectangleF col)
        {
            float w = Math.Max(1, col.Width);
            float h = b.Lines.Average(l => l.Bounds.Height);
            if (b.Kind == BlockKind.ListItem) return TextAlign.Left;
            if (b.Lines.Count == 1)
            {
                var r = b.Lines[0].Bounds;
                float lm = r.Left - col.Left, rm = col.Right - r.Right;
                if (lm > w * 0.1f && Math.Abs(lm - rm) < Math.Max(w * 0.06f, h)) return TextAlign.Center;
                if (lm > w * 0.3f && rm < Math.Max(w * 0.03f, h * 0.5f)) return TextAlign.Right;
                return TextAlign.Left;
            }
            var lefts = b.Lines.Skip(1).Select(l => l.Bounds.Left).ToList();
            var rights = b.Lines.Take(b.Lines.Count - 1).Select(l => l.Bounds.Right).ToList();
            var centers = b.Lines.Select(l => l.Bounds.Left + l.Bounds.Width / 2).ToList();
            float sl = Spread(lefts), sr = Spread(rights), sc = Spread(centers);
            if (sc < h * 0.8f && sl > h * 1.2f) return TextAlign.Center;
            if (sr < h * 0.5f && sl > h * 1.5f) return TextAlign.Right;
            if (sr < h * 0.6f && b.Lines.Count >= 3 && rights.Average() > col.Right - h * 1.5f) return TextAlign.Justify;
            return TextAlign.Left;
        }

        private static bool IsCentered(RectangleF r, RectangleF col)
        {
            float lm = r.Left - col.Left, rm = col.Right - r.Right;
            return lm > col.Width * 0.08f && Math.Abs(lm - rm) < Math.Max(col.Width * 0.05f, r.Height);
        }

        private static bool TryListMarker(LayoutLine line, float hMed, out string marker, out bool ordered, out int prefixLen, out float textLeft, out RectangleF glyphBounds)
        {
            glyphBounds = RectangleF.Empty;
            marker = null;
            ordered = false;
            prefixLen = 0;
            textLeft = line.Bounds.Left;
            if (line.Segments.Count == 0) return false;
            var first = line.Segments[0];
            var t = first.Text.Trim();

            // Drawn bullet (dot/square) that the recognizer did not return as text.
            if (_bullets != null && _bullets.TryGetValue(first, out var glyph))
            {
                marker = "•";
                glyphBounds = glyph;
                textLeft = first.Bounds.Left;
                line.Bounds = RectangleF.FromLTRB(glyph.Left, line.Bounds.Top, line.Bounds.Right, line.Bounds.Bottom);
                return true;
            }

            // Bullet detected as its own tiny box (typical for DB detectors).
            if (line.Segments.Count >= 2 && first.Bounds.Width < Math.Max(first.Bounds.Height, hMed) * 1.6f)
            {
                bool bullet = t.Length == 1 && LooseBulletChars.IndexOf(t[0]) >= 0;
                bool num = OrderedPrefix.IsMatch(t + " x");
                if (bullet || num)
                {
                    marker = bullet ? "•" : t;
                    ordered = num && !bullet;
                    line.Segments.RemoveAt(0);
                    textLeft = line.Segments[0].Bounds.Left;
                    line.Bounds = RectangleF.FromLTRB(first.Bounds.Left, line.Bounds.Top, line.Bounds.Right, line.Bounds.Bottom);
                    return true;
                }
            }

            var m = BulletPrefix.Match(first.Text);
            if (m.Success && m.Groups[1].Value != "-" || (m.Success && first.Text.Length > 2 && first.Text[1] == ' '))
            {
                marker = "•";
                prefixLen = m.Length;
            }
            else
            {
                m = OrderedPrefix.Match(first.Text);
                if (!m.Success) return false;
                marker = m.Groups[1].Value;
                ordered = true;
                prefixLen = m.Length;
            }
            textLeft = first.CharLeft != null && prefixLen < first.CharLeft.Length
                ? first.CharLeft[prefixLen]
                : first.Bounds.Left + first.Bounds.Width * prefixLen / Math.Max(1, first.Text.Length);
            return true;
        }

        /// <summary>
        /// Looks for a small solid blob (●, ■, ◆) just left of each line, vertically centered on its text.
        /// </summary>
        private static Dictionary<TextLine, RectangleF> FindBulletGlyphs(bool[] ink, int w, int h, List<TextLine> lines)
        {
            var result = new Dictionary<TextLine, RectangleF>();
            foreach (var line in lines)
            {
                var textBox = line.InkBounds.IsEmpty ? line.Bounds : line.InkBounds;
                float th = textBox.Height;
                if (th < 4) continue;
                float cy = textBox.Top + th / 2;
                var region = RectangleF.FromLTRB(line.Bounds.Left - th * 2.5f, cy - th * 0.55f, line.Bounds.Left - 1, cy + th * 0.55f);
                if (region.Left < 0 || region.Width < 3) continue;
                if (lines.Any(o => o != line && o.Bounds.IntersectsWith(region))) continue;

                int x0 = (int)region.Left, x1 = (int)region.Right, y0 = Math.Max(0, (int)region.Top), y1 = Math.Min(h - 1, (int)region.Bottom);
                int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1, count = 0;
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!ink[y * w + x]) continue;
                        count++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
                if (count == 0 || minX <= x0 || minY <= y0 || maxY >= y1) continue; // touches the window: part of something bigger
                float bw = maxX - minX + 1, bh = maxY - minY + 1;
                if (bw < th * 0.12f || bh < th * 0.12f || bw > th * 0.7f || bh > th * 0.7f) continue;
                if (Math.Max(bw, bh) / Math.Min(bw, bh) > 1.8f) continue;
                if (count < bw * bh * 0.4f) continue;
                if (Math.Abs((minY + maxY) / 2f - cy) > th * 0.35f) continue;
                result[line] = new RectangleF(minX, minY, bw, bh);
            }
            return result;
        }

        private static void ClassifyHeadings(OcrPage page)
        {
            // Numbered headings ("1. Introduction") look like ordered list items; size/weight decides.
            var textBlocks = page.Blocks.Where(b => b.Kind == BlockKind.Paragraph || (b.Kind == BlockKind.ListItem && b.OrderedList && b.Lines.Count == 1)).ToList();
            if (textBlocks.Count == 0) return;
            // Body size = the size carrying the most characters on the page.
            float body = page.Blocks.Where(b => b.Lines.Count > 0)
                .SelectMany(b => b.Lines.SelectMany(l => l.Segments))
                .GroupBy(s => (float)Math.Round(s.Style.FontSizePt * 2) / 2)
                .OrderByDescending(g => g.Sum(s => s.Text.Length)).First().Key;

            var candidates = new List<LayoutBlock>();
            for (int i = 0; i < textBlocks.Count; i++)
            {
                var b = textBlocks[i];
                string text = b.GetFlowText();
                if (text.Length == 0 || text.Length > 160 || b.Lines.Count > 3) continue;
                bool larger = b.Style.FontSizePt >= body * 1.18f;
                bool boldShort = b.Style.Bold && b.Lines.Count == 1 && b.Style.FontSizePt >= body * 0.95f
                                 && text.Split(' ').Length <= 12 && !text.TrimEnd().EndsWith(".");
                if (b.Kind == BlockKind.ListItem && !(larger || b.Style.Bold)) continue;
                if (larger || boldShort) candidates.Add(b);
            }
            if (candidates.Count == 0) return;
            // A page where "headings" are the majority has no body text to contrast with.
            if (candidates.Count > textBlocks.Count * 0.6f && textBlocks.Count > 4) return;
            foreach (var c in candidates.Where(c => c.Kind == BlockKind.ListItem))
            {
                // The number stays part of the heading text.
                c.ListMarker = null;
                c.OrderedList = false;
                c.MarkerPrefixLength = 0;
                c.FirstLineIndent = 0;
                c.Bounds = Geometry.Union(c.Lines.Select(l => l.Bounds));
            }

            var sizes = candidates.Select(c => (float)Math.Round(c.Style.FontSizePt)).Distinct().OrderByDescending(s => s).ToList();
            foreach (var c in candidates)
            {
                c.Kind = BlockKind.Heading;
                int level = sizes.IndexOf((float)Math.Round(c.Style.FontSizePt)) + 1;
                if (c.Style.FontSizePt < body * 1.18f) level = Math.Max(level, Math.Min(3, sizes.Count(s => s >= body * 1.18f) + 1));
                c.HeadingLevel = Math.Max(1, Math.Min(3, level));
            }
        }

        private static void BuildLinesOnly(OcrPage page, List<TextLine> lines)
        {
            var section = new LayoutSection();
            if (lines.Count == 0) return;
            section.Bounds = Geometry.Union(lines.Select(l => l.Bounds));
            section.Columns.Add(section.Bounds);
            float prevBottom = section.Bounds.Top;
            var rows = new List<List<TextLine>>();
            foreach (var l in lines.OrderBy(l => l.Bounds.Top))
            {
                var row = rows.LastOrDefault(r => Geometry.OverlapRatio(r[0].Bounds.Top, r[0].Bounds.Bottom, l.Bounds.Top, l.Bounds.Bottom) > 0.55f);
                if (row == null) rows.Add(new List<TextLine> { l });
                else row.Add(l);
            }
            foreach (var row in rows)
            {
                var ll = new LayoutLine();
                ll.Segments.AddRange(row.OrderBy(s => s.Bounds.Left));
                ll.Bounds = Geometry.Union(row.Select(s => s.Bounds));
                var b = new LayoutBlock { Kind = BlockKind.Paragraph, Section = section, Bounds = ll.Bounds, Style = ll.Style.Clone(), LineSpacing = ll.Bounds.Height * 1.2f };
                b.Lines.Add(ll);
                b.SpaceBefore = Math.Max(0, ll.Bounds.Top - prevBottom);
                prevBottom = ll.Bounds.Bottom;
                section.Blocks.Add(b);
            }
            page.Sections.Add(section);
            page.Blocks.AddRange(section.Blocks);
        }

        /// <summary>Snaps sizes that are within measuring noise of a frequent size to that size.</summary>
        private static void NormalizeSizes(List<TextLine> lines)
        {
            var groups = lines.GroupBy(l => (float)Math.Round(l.Style.FontSizePt * 2) / 2)
                .Select(g => new { Size = g.Key, Weight = g.Sum(l => l.Text.Length) })
                .OrderByDescending(g => g.Weight).ToList();
            var anchors = new List<float>();
            foreach (var g in groups)
                if (!anchors.Any(a => Math.Abs(a - g.Size) / a <= 0.09f)) anchors.Add(g.Size);
            foreach (var l in lines)
            {
                float s = l.Style.FontSizePt;
                var a = anchors.OrderBy(x => Math.Abs(x - s)).First();
                if (Math.Abs(a - s) / a <= 0.09f) l.Style.FontSizePt = a;
            }
        }

        private static bool[] Binarize(RgbImage img)
        {
            var gray = img.ToGray();
            var hist = new int[256];
            foreach (var g in gray) hist[g]++;
            int t = Math.Min(200, ImageOps.Otsu(hist));
            var ink = new bool[gray.Length];
            for (int i = 0; i < gray.Length; i++) ink[i] = gray[i] <= t;
            return ink;
        }

        private static void Renumber(OcrPage page)
        {
            int id = 1;
            foreach (var l in page.Lines.OrderBy(l => l.Bounds.Top).ThenBy(l => l.Bounds.Left)) l.Id = id++;
        }

        private static float Median(IEnumerable<float> values, float fallback)
        {
            var v = values.OrderBy(x => x).ToList();
            return v.Count == 0 ? fallback : v[v.Count / 2];
        }

        private static float Spread(List<float> v)
        {
            if (v.Count < 2) return 0;
            return v.Max() - v.Min();
        }
    }
}
