using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using FalconOcr.Engine;
using FalconOcr.Model;
using SD = System.Drawing;

namespace FalconOcr.Export
{
    /// <summary>Self-contained HTML export (images embedded) — semantic flow or exact positioned copy.</summary>
    internal static class HtmlExporter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Write(List<OcrPage> pages, string path, string title, ExportOptions opt)
        {
            var lang = LanguageCatalog.Get(opt.Language);
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>\n<html lang=\"").Append(lang.Culture.Substring(0, 2)).Append("\">\n<head>\n<meta charset=\"utf-8\">\n");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            sb.Append("<meta name=\"generator\" content=\"Falcon OCR\">\n<title>").Append(Enc(title)).Append("</title>\n<style>\n");
            sb.Append(Css(lang));
            sb.Append("</style>\n</head>\n<body>\n");
            for (int i = 0; i < pages.Count; i++)
            {
                switch (opt.Mode)
                {
                    case DocumentMode.ExactCopy: ExactPage(sb, pages[i], i, opt); break;
                    case DocumentMode.PlainText: PlainPage(sb, pages[i], i); break;
                    default: FlowPage(sb, pages[i], i, opt); break;
                }
            }
            sb.Append("</body>\n</html>\n");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static string Css(LanguageModel lang)
        {
            return
                "html{background:#eceff1}\n" +
                "body{margin:0;padding:24px 12px;font-family:'" + lang.DefaultFont + "',Calibri,'Segoe UI',Arial,sans-serif;color:#000}\n" +
                ".page{background:#fff;margin:0 auto 24px;box-shadow:0 1px 4px rgba(0,0,0,.18);box-sizing:border-box;position:relative;max-width:100%;overflow:hidden}\n" +
                ".page p,.page h1,.page h2,.page h3,.page ul,.page ol,.page figure{margin:0;padding:0}\n" +
                ".page ul,.page ol{list-style-position:outside}\n" +
                ".page ol.lit{list-style:none}\n" +
                ".page ol.lit li>.m{display:inline-block;margin-left:-1.6em;width:1.6em}\n" +
                ".cols{display:grid;align-items:start}\n" +
                ".page table{border-collapse:collapse;table-layout:fixed}\n" +
                ".page td{padding:1px 4px;vertical-align:middle;overflow-wrap:anywhere}\n" +
                ".page img{display:block;max-width:100%;height:auto}\n" +
                ".exact .l{position:absolute;white-space:pre;transform-origin:0 50%;line-height:1.2}\n" +
                ".exact .a{position:absolute}\n" +
                ".plain{white-space:pre-wrap;font-family:Consolas,monospace;padding:24px}\n" +
                "@media print{html{background:none}body{padding:0}.page{box-shadow:none;margin:0;page-break-after:always}}\n";
        }

        // ------------------------------------------------------------ flow

        private static void FlowPage(StringBuilder sb, OcrPage page, int index, ExportOptions opt)
        {
            float k = 96f / page.Dpi; // page px → CSS px
            var content = Geometry.Union(page.Sections.Select(s => s.Bounds));
            if (content.IsEmpty) content = new SD.RectangleF(0, 0, page.Width, page.Height);
            string bg = opt.KeepColors && page.Background.ToArgb() != SD.Color.White.ToArgb() ? ";background:" + Units.Css(page.Background) : "";
            sb.AppendFormat(Inv, "<article class=\"page\" id=\"page-{0}\" style=\"width:{1:0}px;min-height:{2:0}px;padding:{3:0}px {4:0}px {5:0}px {6:0}px{7}\">\n",
                index + 1, page.Width * k, page.Height * k, content.Top * k, (page.Width - content.Right) * k, Math.Min(48, (page.Height - content.Bottom) * k), content.Left * k, bg);

            bool first = true;
            foreach (var s in page.Sections)
            {
                float top = first ? 0 : s.SpaceBefore;
                first = false;
                if (s.Columns.Count > 1)
                {
                    var tpl = string.Join(" ", s.Columns.Select(c => (c.Width * k).ToString("0", Inv) + "px"));
                    float gap = s.Columns.Count > 1 ? (s.Columns[1].Left - s.Columns[0].Right) * k : 0;
                    sb.AppendFormat(Inv, "<section class=\"cols\" style=\"grid-template-columns:{0};column-gap:{1:0}px;margin-top:{2:0}px;margin-left:{3:0}px\">\n",
                        tpl, Math.Max(8, gap), top * k, (s.Columns[0].Left - content.Left) * k);
                    for (int c = 0; c < s.Columns.Count; c++)
                    {
                        sb.Append("<div>\n");
                        var col = s.Columns[c];
                        bool firstInCol = true;
                        EmitBlocks(sb, s.Blocks.Where(b => b.Column == c).ToList(), col.Left, k, opt, b =>
                        {
                            float before = firstInCol ? b.Bounds.Top - s.Bounds.Top : b.SpaceBefore;
                            firstInCol = false;
                            return before;
                        });
                        sb.Append("</div>\n");
                    }
                    sb.Append("</section>\n");
                }
                else
                {
                    sb.AppendFormat(Inv, "<section style=\"margin-top:{0:0}px\">\n", top * k);
                    bool firstInSection = true;
                    EmitBlocks(sb, s.Blocks, content.Left, k, opt, b =>
                    {
                        float before = firstInSection ? b.Bounds.Top - s.Bounds.Top : b.SpaceBefore;
                        firstInSection = false;
                        return before;
                    });
                    sb.Append("</section>\n");
                }
            }
            sb.Append("</article>\n");
        }

        private static void EmitBlocks(StringBuilder sb, List<LayoutBlock> blocks, float colLeft, float k, ExportOptions opt, Func<LayoutBlock, float> spaceBefore)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                float before = spaceBefore(b);
                switch (b.Kind)
                {
                    case BlockKind.Figure:
                        if (!opt.IncludeImages || b.Figure?.Png == null) break;
                        sb.AppendFormat(Inv, "<figure style=\"margin:{0:0}px 0 0 {1:0}px\"><img src=\"data:image/png;base64,{2}\" width=\"{3:0}\" height=\"{4:0}\" alt=\"Figure\"></figure>\n",
                            before * k, Math.Max(0, b.Bounds.Left - colLeft) * k, Convert.ToBase64String(b.Figure.Png), b.Bounds.Width * k, b.Bounds.Height * k);
                        break;
                    case BlockKind.Table:
                        sb.AppendFormat(Inv, "<div style=\"margin:{0:0}px 0 0 {1:0}px\">", before * k, Math.Max(0, b.Bounds.Left - colLeft) * k);
                        Table(sb, b.Table, k, opt, false);
                        sb.Append("</div>\n");
                        break;
                    case BlockKind.ListItem:
                        // Group consecutive items of the same kind into one list element.
                        int j = i;
                        while (j + 1 < blocks.Count && blocks[j + 1].Kind == BlockKind.ListItem && blocks[j + 1].OrderedList == b.OrderedList) j++;
                        string tag = b.OrderedList ? "ol" : "ul";
                        sb.AppendFormat(Inv, "<{0}{1} style=\"margin:{2:0}px 0 0 {3:0}px;padding-left:{4:0}px\">\n", tag, b.OrderedList ? " class=\"lit\"" : "",
                            before * k, Math.Max(0, b.TextLeft - colLeft + b.FirstLineIndent) * k, Math.Max(12, -b.FirstLineIndent) * k);
                        for (int n = i; n <= j; n++)
                        {
                            var li = blocks[n];
                            float liBefore = n == i ? 0 : spaceBefore(li);
                            sb.AppendFormat(Inv, "<li style=\"{0}{1}\">", TextCss(li.Style, opt), n == i ? "" : string.Format(Inv, ";margin-top:{0:0}px", liBefore * k));
                            if (li.OrderedList) sb.Append("<span class=\"m\">").Append(Enc(li.ListMarker)).Append("</span>");
                            Runs(sb, li, opt);
                            sb.Append("</li>\n");
                        }
                        sb.Append("</").Append(tag).Append(">\n");
                        i = j;
                        break;
                    default:
                        string t = b.Kind == BlockKind.Heading ? "h" + Math.Max(1, Math.Min(3, b.HeadingLevel)) : "p";
                        var css = new StringBuilder(TextCss(b.Style, opt));
                        css.AppendFormat(Inv, ";margin:{0:0}px 0 0 {1:0}px", before * k, Math.Max(0, b.Bounds.Left - colLeft) * k);
                        if (b.Align != TextAlign.Left) css.Append(";text-align:").Append(b.Align == TextAlign.Justify ? "justify" : b.Align.ToString().ToLowerInvariant());
                        if (Math.Abs(b.FirstLineIndent) > 1) css.AppendFormat(Inv, ";text-indent:{0:0}px", b.FirstLineIndent * k);
                        if (b.Lines.Count > 1 && b.LineSpacing > 0) css.AppendFormat(Inv, ";line-height:{0:0}px", b.LineSpacing * k);
                        if (opt.KeepColors && !b.Style.BackColor.IsEmpty) css.Append(";background:").Append(Units.Css(b.Style.BackColor));
                        sb.Append('<').Append(t).Append(" style=\"").Append(css).Append("\">");
                        Runs(sb, b, opt);
                        sb.Append("</").Append(t).Append(">\n");
                        break;
                }
            }
        }

        /// <summary>Paragraph text; segments whose style differs from the block get their own span.</summary>
        private static void Runs(StringBuilder sb, LayoutBlock b, ExportOptions opt)
        {
            bool keepBreaks = b.Lines.Count <= 2 && b.Lines.Any(l => l.Segments.Count > 1);
            for (int i = 0; i < b.Lines.Count; i++)
            {
                var line = b.Lines[i];
                if (i > 0)
                {
                    var prev = b.LineText(i - 1).TrimEnd();
                    var cur = line.Text;
                    if (keepBreaks) sb.Append("<br>");
                    else if (prev.Length > 0 && cur.Length > 0 && !LayoutBlock.JoinsWithoutSpace(prev[prev.Length - 1]) && !LayoutBlock.JoinsWithoutSpace(cur[0]) && !(prev.EndsWith("-") && char.IsLower(cur[0])))
                        sb.Append(' ');
                }
                for (int s = 0; s < line.Segments.Count; s++)
                {
                    var seg = line.Segments[s];
                    var text = seg.Text;
                    if (i == 0 && s == 0 && b.MarkerPrefixLength > 0 && text.Length >= b.MarkerPrefixLength) text = text.Substring(b.MarkerPrefixLength).TrimStart();
                    if (i < b.Lines.Count - 1 && s == line.Segments.Count - 1 && !keepBreaks && text.EndsWith("-") && text.Length > 2 && char.IsLetter(text[text.Length - 2])
                        && b.Lines[i + 1].Text.Length > 0 && char.IsLower(b.Lines[i + 1].Text[0]))
                        text = text.Substring(0, text.Length - 1);
                    if (s > 0) sb.Append(keepBreaks ? "&emsp;&emsp;" : " ");
                    bool differs = seg.Style.Bold != b.Style.Bold || seg.Style.Italic != b.Style.Italic || !TextStyle.ColorClose(seg.Style.Color, b.Style.Color, 60);
                    if (differs)
                    {
                        sb.Append("<span style=\"");
                        sb.Append("font-weight:").Append(seg.Style.Bold ? "700" : "400");
                        if (seg.Style.Italic) sb.Append(";font-style:italic");
                        if (opt.KeepColors) sb.Append(";color:").Append(Units.Css(seg.Style.Color));
                        sb.Append("\">").Append(Enc(text)).Append("</span>");
                    }
                    else sb.Append(Enc(text));
                }
            }
        }

        private static string TextCss(TextStyle s, ExportOptions opt)
        {
            var css = new StringBuilder();
            css.AppendFormat(Inv, "font-size:{0:0.#}pt;font-weight:{1}", s.FontSizePt, s.Bold ? 700 : 400);
            if (s.Italic) css.Append(";font-style:italic");
            if (!string.IsNullOrEmpty(s.FontFamily)) css.Append(";font-family:'").Append(s.FontFamily.Replace("'", "")).Append("',inherit");
            if (opt.KeepColors && s.Color.ToArgb() != SD.Color.Black.ToArgb()) css.Append(";color:").Append(Units.Css(s.Color));
            if (s.Underline) css.Append(";text-decoration:underline");
            return css.ToString();
        }

        private static void Table(StringBuilder sb, TableRegion t, float k, ExportOptions opt, bool absolute)
        {
            string border = t.HasBorders ? "1px solid " + Units.Css(t.BorderColor) : "none";
            sb.AppendFormat(Inv, "<table style=\"width:{0:0}px\"><colgroup>", t.Bounds.Width * k);
            for (int c = 0; c < t.ColumnCount; c++) sb.AppendFormat(Inv, "<col style=\"width:{0:0}px\">", (t.ColumnEdges[c + 1] - t.ColumnEdges[c]) * k);
            sb.Append("</colgroup>\n");
            for (int r = 0; r < t.RowCount; r++)
            {
                sb.AppendFormat(Inv, "<tr style=\"height:{0:0}px\">", (t.RowEdges[r + 1] - t.RowEdges[r]) * k);
                foreach (var cell in t.Cells.Where(c => c.Row == r).OrderBy(c => c.Col))
                {
                    var st = cell.Style;
                    sb.Append("<td");
                    if (cell.ColSpan > 1) sb.Append(" colspan=\"").Append(cell.ColSpan).Append('"');
                    if (cell.RowSpan > 1) sb.Append(" rowspan=\"").Append(cell.RowSpan).Append('"');
                    sb.Append(" style=\"border:").Append(border).Append(';').Append(TextCss(st, opt));
                    if (cell.Align != TextAlign.Left) sb.Append(";text-align:").Append(cell.Align.ToString().ToLowerInvariant());
                    if (opt.KeepColors && !cell.Fill.IsEmpty) sb.Append(";background:").Append(Units.Css(cell.Fill));
                    sb.Append("\">");
                    sb.Append(string.Join("<br>", cell.GroupRows().Select(row => Enc(string.Join(" ", row.Select(l => l.Text))))));
                    sb.Append("</td>");
                }
                sb.Append("</tr>\n");
            }
            sb.Append("</table>");
        }

        // ------------------------------------------------------------ exact

        private static void ExactPage(StringBuilder sb, OcrPage page, int index, ExportOptions opt)
        {
            float k = 96f / page.Dpi;
            string bg = opt.KeepColors ? ";background:" + Units.Css(page.Background) : "";
            sb.AppendFormat(Inv, "<article class=\"page exact\" id=\"page-{0}\" style=\"width:{1:0}px;height:{2:0}px{3}\">\n", index + 1, page.Width * k, page.Height * k, bg);
            foreach (var b in page.Blocks)
            {
                if (b.Kind == BlockKind.Figure && opt.IncludeImages && b.Figure?.Png != null)
                    sb.AppendFormat(Inv, "<img class=\"a\" style=\"left:{0:0}px;top:{1:0}px\" src=\"data:image/png;base64,{2}\" width=\"{3:0}\" height=\"{4:0}\" alt=\"Figure\">\n",
                        b.Bounds.Left * k, b.Bounds.Top * k, Convert.ToBase64String(b.Figure.Png), b.Bounds.Width * k, b.Bounds.Height * k);
                else if (b.Kind == BlockKind.Table)
                {
                    sb.AppendFormat(Inv, "<div class=\"a\" style=\"left:{0:0}px;top:{1:0}px\">", b.Bounds.Left * k, b.Bounds.Top * k);
                    Table(sb, b.Table, k, opt, true);
                    sb.Append("</div>\n");
                }
            }
            foreach (var l in ExportHelpers.PositionedLines(page))
            {
                var s = l.Style;
                var ink = l.InkBounds.IsEmpty ? l.Bounds : l.InkBounds;
                float fontPx = s.FontSizePt * page.Dpi / 72f;
                float lineH = fontPx * 1.2f;
                float top = ink.Top + ink.Height / 2 - lineH * 0.55f;
                float scale = TextMeasure.FitScale(l.Text, s, ink.Width * 72f / page.Dpi) / 100f;
                sb.AppendFormat(Inv, "<div class=\"l\" style=\"left:{0:0.#}px;top:{1:0.#}px;{2}{3}{4}\">{5}</div>\n",
                    ink.Left * k, top * k, TextCss(s, opt),
                    Math.Abs(scale - 1) > 0.02f ? string.Format(Inv, ";transform:scaleX({0:0.###})", scale) : "",
                    opt.KeepColors && !s.BackColor.IsEmpty ? ";background:" + Units.Css(s.BackColor) : "",
                    Enc(l.Text));
            }
            sb.Append("</article>\n");
        }

        private static void PlainPage(StringBuilder sb, OcrPage page, int index)
        {
            sb.AppendFormat(Inv, "<article class=\"page plain\" id=\"page-{0}\" style=\"width:{1:0}px\">", index + 1, page.Width * 96f / page.Dpi);
            sb.Append(Enc(page.GetPlainText()));
            sb.Append("</article>\n");
        }

        private static string Enc(string s) => WebUtility.HtmlEncode(s ?? "");
    }
}
