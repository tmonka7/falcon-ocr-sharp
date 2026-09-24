using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FalconOcr.Engine;
using FalconOcr.Model;
using SD = System.Drawing;

namespace FalconOcr.Export
{
    /// <summary>
    /// Excel (.xlsx) export: one worksheet per page. Tables keep their grid (merged cells, borders, fills);
    /// other text is placed on a column grid derived from the page so side-by-side content stays side by side.
    /// </summary>
    internal static class XlsxExporter
    {
        private static readonly Regex NumberPattern = new Regex(@"^[-+]?(\d{1,3}(,\d{3})+|\d+)(\.\d+)?%?$", RegexOptions.Compiled);

        private sealed class Entry
        {
            public float Top, Bottom;
            public bool TableRow;
            public bool Wrap;
            public List<(int Col, int Span, int RowSpan, string Text, TextStyle Style, SD.Color Fill, TextAlign Align, bool Border, SD.Color BorderColor)> Cells =
                new List<(int, int, int, string, TextStyle, SD.Color, TextAlign, bool, SD.Color)>();
            public float HeightPt;
        }

        public static void Write(List<OcrPage> pages, string path, string title, ExportOptions opt)
        {
            using (var doc = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook))
            {
                var wbPart = doc.AddWorkbookPart();
                wbPart.Workbook = new Workbook();
                var sheets = wbPart.Workbook.AppendChild(new Sheets());
                var sst = new SharedStrings();
                var styles = new StyleRegistry(LanguageCatalog.Get(opt.Language).DefaultFont);

                for (int i = 0; i < pages.Count; i++)
                {
                    var wsPart = wbPart.AddNewPart<WorksheetPart>();
                    wsPart.Worksheet = BuildSheet(pages[i], sst, styles, opt);
                    sheets.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = (uint)(i + 1), Name = "Page " + (i + 1) });
                }

                var ssPart = wbPart.AddNewPart<SharedStringTablePart>();
                ssPart.SharedStringTable = sst.Build();
                var stPart = wbPart.AddNewPart<WorkbookStylesPart>();
                stPart.Stylesheet = styles.Build();
                doc.PackageProperties.Title = title;
                doc.PackageProperties.Creator = "Falcon OCR";
                wbPart.Workbook.Save();
            }
        }

        private static Worksheet BuildSheet(OcrPage page, SharedStrings sst, StyleRegistry styles, ExportOptions opt)
        {
            float dpi = page.Dpi;
            var content = Geometry.Union(page.Blocks.Select(b => b.Bounds));
            if (content.IsEmpty) content = new SD.RectangleF(0, 0, page.Width, page.Height);

            // Column grid: table columns, text columns and the content box.
            var edges = new List<float> { content.Left, content.Right };
            foreach (var b in page.Blocks.Where(b => b.Kind == BlockKind.Table)) edges.AddRange(b.Table.ColumnEdges);
            foreach (var s in page.Sections.Where(s => s.Columns.Count > 1)) edges.AddRange(s.Columns.Select(c => c.Left));
            edges = Cluster(edges, Math.Max(8, page.Width * 0.012f));
            int Col(float x)
            {
                int best = 0;
                for (int i = 0; i < edges.Count - 1; i++) if (x >= edges[i] - 4) best = i;
                return best;
            }
            int Edge(float x)
            {
                int best = 0;
                for (int i = 1; i < edges.Count; i++) if (Math.Abs(edges[i] - x) < Math.Abs(edges[best] - x)) best = i;
                return best;
            }

            var entries = new List<Entry>();
            foreach (var b in page.Blocks)
            {
                if (b.Kind == BlockKind.Figure) continue;
                if (b.Kind == BlockKind.Table)
                {
                    var t = b.Table;
                    for (int r = 0; r < t.RowCount; r++)
                    {
                        var e = new Entry { Top = t.RowEdges[r], Bottom = t.RowEdges[r + 1], TableRow = true };
                        e.HeightPt = Math.Max(15, (t.RowEdges[r + 1] - t.RowEdges[r]) * 72f / dpi);
                        foreach (var cell in t.Cells.Where(c => c.Row == r))
                        {
                            int c0 = Edge(t.ColumnEdges[cell.Col]), c1 = Math.Max(c0 + 1, Edge(t.ColumnEdges[cell.Col + cell.ColSpan]));
                            e.Cells.Add((c0, c1 - c0, cell.RowSpan, cell.Text, cell.Style, cell.Fill, cell.Align, t.HasBorders, t.BorderColor));
                        }
                        entries.Add(e);
                    }
                    continue;
                }
                var text = (b.ListMarker != null ? b.ListMarker + " " : "") + b.GetFlowText();
                if (text.Length == 0) continue;
                // Paragraphs span the grid columns they cover and wrap, so text stays inside the page width.
                int startCol = Col(b.Bounds.Left);
                int span = Math.Max(1, Edge(b.Bounds.Right) - startCol);
                bool wrap = b.Lines.Count > 1;
                var en = new Entry
                {
                    Top = b.Bounds.Top,
                    Bottom = b.Bounds.Bottom,
                    HeightPt = wrap ? Math.Max(15, b.Lines.Count * b.Style.FontSizePt * 1.3f + 2) : Math.Max(15, b.Style.FontSizePt * 1.35f),
                    Wrap = wrap
                };
                en.Cells.Add((startCol, span, 1, text, b.Style, b.Style.BackColor, b.Align == TextAlign.Justify ? TextAlign.Left : b.Align, false, SD.Color.Empty));
                entries.Add(en);
            }

            // Rows: text entries that sit side by side (different columns, overlapping y) share a row.
            var rows = new List<List<Entry>>();
            foreach (var e in entries.OrderBy(e => e.Top))
            {
                var row = rows.LastOrDefault();
                bool join = row != null && !e.TableRow && !row[0].TableRow
                            && Geometry.OverlapRatio(row[0].Top, row[0].Bottom, e.Top, e.Bottom) > 0.3f
                            && !row.SelectMany(x => x.Cells).Any(c => e.Cells.Any(n => n.Col < c.Col + c.Span && c.Col < n.Col + n.Span));
                if (join) row.Add(e);
                else rows.Add(new List<Entry> { e });
            }

            var ws = new Worksheet();
            var cols = new Columns();
            for (int i = 0; i < edges.Count - 1; i++)
            {
                double widthChars = Math.Max(2, (edges[i + 1] - edges[i]) * 96f / dpi / 7.0);
                cols.Append(new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = Math.Round(widthChars, 2), CustomWidth = true });
            }
            ws.Append(new SheetViews(new SheetView { WorkbookViewId = 0U, ShowGridLines = false }));
            ws.Append(cols);
            var data = new SheetData();
            var merges = new List<string>();
            uint rowIndex = 1;
            foreach (var row in rows)
            {
                var xr = new Row { RowIndex = rowIndex, Height = row.Max(e => e.HeightPt), CustomHeight = true };
                bool rowWrap = row.Any(e => e.Wrap);
                foreach (var c in row.SelectMany(e => e.Cells).OrderBy(c => c.Col))
                {
                    string refA = Ref(c.Col, rowIndex);
                    uint styleIdx = styles.Get(c.Style, opt.KeepColors ? c.Fill : SD.Color.Empty, c.Align, c.Border, c.BorderColor, opt.KeepColors, wrap: rowWrap || (row[0].TableRow && c.Text.Contains("\n")));
                    xr.Append(MakeCell(refA, c.Text, styleIdx, sst, styles, c.Style, c.Fill, c.Align, c.Border, c.BorderColor, opt.KeepColors));
                    if (c.Span > 1 || c.RowSpan > 1)
                    {
                        merges.Add(refA + ":" + Ref(c.Col + c.Span - 1, rowIndex + (uint)c.RowSpan - 1));
                        // Border/fill on the covered cells so merged regions draw completely.
                        if (c.Border || !c.Fill.IsEmpty)
                        {
                            for (int k = 1; k < c.Span; k++)
                                xr.Append(new Cell { CellReference = Ref(c.Col + k, rowIndex), StyleIndex = styleIdx });
                        }
                    }
                }
                // Cells within a row must be ordered by column.
                var ordered = xr.Elements<Cell>().OrderBy(x => ColIndex(x.CellReference)).ToList();
                xr.RemoveAllChildren<Cell>();
                foreach (var oc in ordered.GroupBy(x => x.CellReference.Value).Select(g => g.First())) xr.Append(oc);
                data.Append(xr);
                rowIndex++;
            }
            ws.Append(data);
            if (merges.Count > 0)
            {
                var mc = new MergeCells();
                foreach (var m in merges.Distinct()) mc.Append(new MergeCell { Reference = m });
                ws.Append(mc);
            }
            ws.Append(new PageMargins { Left = 0.5, Right = 0.5, Top = 0.5, Bottom = 0.5, Header = 0.3, Footer = 0.3 });
            return ws;
        }

        private static Cell MakeCell(string reference, string text, uint styleIdx, SharedStrings sst, StyleRegistry styles, TextStyle s, SD.Color fill, TextAlign align, bool border, SD.Color borderColor, bool keepColors)
        {
            var t = (text ?? "").Trim();
            if (NumberPattern.IsMatch(t))
            {
                bool pct = t.EndsWith("%");
                var raw = t.TrimEnd('%').Replace(",", "");
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                {
                    if (pct) v /= 100;
                    int decimals = raw.Contains(".") ? raw.Length - raw.IndexOf('.') - 1 : 0;
                    uint numFmt = pct ? (decimals > 0 ? 10U : 9U) : t.Contains(",") ? (decimals > 0 ? 4U : 3U) : (decimals > 0 ? 2U : 1U);
                    if (!pct && !t.Contains(",") && decimals > 2) numFmt = 0;
                    uint st = styles.Get(s, keepColors ? fill : SD.Color.Empty, align == TextAlign.Left ? TextAlign.Right : align, border, borderColor, keepColors, false, numFmt);
                    return new Cell { CellReference = reference, StyleIndex = st, CellValue = new CellValue(v.ToString("R", CultureInfo.InvariantCulture)) };
                }
            }
            return new Cell { CellReference = reference, StyleIndex = styleIdx, DataType = CellValues.SharedString, CellValue = new CellValue(sst.Index(t).ToString()) };
        }

        private static string Ref(int col, uint row) => ColName(col) + row;

        private static string ColName(int col)
        {
            string s = "";
            col++;
            while (col > 0)
            {
                int m = (col - 1) % 26;
                s = (char)('A' + m) + s;
                col = (col - 1) / 26;
            }
            return s;
        }

        private static int ColIndex(string reference)
        {
            int n = 0;
            foreach (var ch in reference)
            {
                if (!char.IsLetter(ch)) break;
                n = n * 26 + (ch - 'A' + 1);
            }
            return n - 1;
        }

        private static List<float> Cluster(List<float> values, float tol)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var result = new List<float>();
            foreach (var v in sorted)
            {
                if (result.Count > 0 && v - result[result.Count - 1] <= tol) continue;
                result.Add(v);
            }
            return result;
        }

        private sealed class SharedStrings
        {
            private readonly Dictionary<string, int> _index = new Dictionary<string, int>();
            private readonly List<string> _items = new List<string>();

            public int Index(string s)
            {
                if (!_index.TryGetValue(s, out int i))
                {
                    i = _items.Count;
                    _items.Add(s);
                    _index[s] = i;
                }
                return i;
            }

            public SharedStringTable Build()
            {
                var t = new SharedStringTable { Count = (uint)_items.Count, UniqueCount = (uint)_items.Count };
                foreach (var s in _items) t.Append(new SharedStringItem(new Text(s) { Space = SpaceProcessingModeValues.Preserve }));
                return t;
            }
        }

        /// <summary>Deduplicating registry of fonts, fills, borders and cell formats.</summary>
        private sealed class StyleRegistry
        {
            private readonly string _defaultFont;
            private readonly List<string> _fonts = new List<string>();
            private readonly List<Font> _fontEls = new List<Font>();
            private readonly List<string> _fills = new List<string> { "none", "gray125" };
            private readonly List<Fill> _fillEls = new List<Fill>
            {
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 })
            };
            private readonly List<string> _borders = new List<string> { "none" };
            private readonly List<Border> _borderEls = new List<Border> { new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder()) };
            private readonly List<string> _xfs = new List<string>();
            private readonly List<CellFormat> _xfEls = new List<CellFormat>();

            public StyleRegistry(string defaultFont)
            {
                _defaultFont = defaultFont;
                Get(new TextStyle { FontSizePt = 11, FontFamily = defaultFont }, SD.Color.Empty, TextAlign.Left, false, SD.Color.Empty, true);
            }

            public uint Get(TextStyle s, SD.Color fill, TextAlign align, bool border, SD.Color borderColor, bool keepColors, bool wrap = false, uint numFmt = 0)
            {
                int font = FontId(s, keepColors);
                int fillId = FillId(fill);
                int borderId = border ? BorderId(borderColor) : 0;
                string key = font + "|" + fillId + "|" + borderId + "|" + align + "|" + wrap + "|" + numFmt;
                int i = _xfs.IndexOf(key);
                if (i >= 0) return (uint)i;
                var h = align == TextAlign.Center ? HorizontalAlignmentValues.Center : align == TextAlign.Right ? HorizontalAlignmentValues.Right : HorizontalAlignmentValues.Left;
                var xf = new CellFormat(new Alignment { Horizontal = h, Vertical = VerticalAlignmentValues.Center, WrapText = wrap })
                {
                    NumberFormatId = numFmt,
                    FontId = (uint)font,
                    FillId = (uint)fillId,
                    BorderId = (uint)borderId,
                    FormatId = 0U,
                    ApplyFont = true,
                    ApplyFill = fillId > 1,
                    ApplyBorder = borderId > 0,
                    ApplyAlignment = true,
                    ApplyNumberFormat = numFmt != 0
                };
                _xfs.Add(key);
                _xfEls.Add(xf);
                return (uint)(_xfs.Count - 1);
            }

            private int FontId(TextStyle s, bool keepColors)
            {
                string name = s.FontFamily ?? _defaultFont;
                double size = Math.Max(6, Math.Round(s.FontSizePt * 2) / 2);
                string color = keepColors ? Units.Hex(s.Color) : "000000";
                string key = name + "|" + size + "|" + s.Bold + "|" + s.Italic + "|" + color;
                int i = _fonts.IndexOf(key);
                if (i >= 0) return i;
                var f = new Font();
                if (s.Bold) f.Append(new Bold());
                if (s.Italic) f.Append(new Italic());
                f.Append(new FontSize { Val = size });
                f.Append(new Color { Rgb = "FF" + color });
                f.Append(new FontName { Val = name });
                _fonts.Add(key);
                _fontEls.Add(f);
                return _fonts.Count - 1;
            }

            private int FillId(SD.Color c)
            {
                if (c.IsEmpty) return 0;
                string key = Units.Hex(c);
                int i = _fills.IndexOf(key);
                if (i >= 0) return i;
                _fills.Add(key);
                _fillEls.Add(new Fill(new PatternFill(new ForegroundColor { Rgb = "FF" + key }, new BackgroundColor { Indexed = 64U }) { PatternType = PatternValues.Solid }));
                return _fills.Count - 1;
            }

            private int BorderId(SD.Color c)
            {
                string key = c.IsEmpty ? "000000" : Units.Hex(c);
                int i = _borders.IndexOf(key);
                if (i >= 0) return i;
                _borders.Add(key);
                Color Col() => new Color { Rgb = "FF" + key };
                _borderEls.Add(new Border(
                    new LeftBorder(Col()) { Style = BorderStyleValues.Thin },
                    new RightBorder(Col()) { Style = BorderStyleValues.Thin },
                    new TopBorder(Col()) { Style = BorderStyleValues.Thin },
                    new BottomBorder(Col()) { Style = BorderStyleValues.Thin },
                    new DiagonalBorder()));
                return _borders.Count - 1;
            }

            public Stylesheet Build()
            {
                return new Stylesheet(
                    new Fonts(_fontEls.Select(f => (OpenXmlElement)f.CloneNode(true))) { Count = (uint)_fontEls.Count },
                    new Fills(_fillEls.Select(f => (OpenXmlElement)f.CloneNode(true))) { Count = (uint)_fillEls.Count },
                    new Borders(_borderEls.Select(b => (OpenXmlElement)b.CloneNode(true))) { Count = (uint)_borderEls.Count },
                    new CellStyleFormats(new CellFormat { NumberFormatId = 0U, FontId = 0U, FillId = 0U, BorderId = 0U }) { Count = 1U },
                    new CellFormats(_xfEls.Select(x => (OpenXmlElement)x.CloneNode(true))) { Count = (uint)_xfEls.Count },
                    new CellStyles(new CellStyle { Name = "Normal", FormatId = 0U, BuiltinId = 0U }) { Count = 1U });
            }
        }
    }
}
