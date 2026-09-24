using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FalconOcr.Engine;
using FalconOcr.Model;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using SD = System.Drawing;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;

namespace FalconOcr.Export
{
    /// <summary>Word (.docx) export: editable flow reconstruction or exact positional copy.</summary>
    internal static class DocxExporter
    {
        public static void Write(List<OcrPage> pages, string path, string title, ExportOptions opt)
        {
            using (var package = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var main = package.AddMainDocumentPart();
                main.Document = new Document(new Body());
                var w = new Writer(main, opt);
                w.AddStyles();
                w.AddNumbering();
                for (int i = 0; i < pages.Count; i++)
                {
                    bool last = i == pages.Count - 1;
                    switch (opt.Mode)
                    {
                        case DocumentMode.ExactCopy: w.WriteExactPage(pages[i], i == 0, last); break;
                        case DocumentMode.PlainText: w.WritePlainPage(pages[i], i == 0, last); break;
                        default: w.WriteEditablePage(pages[i], i == 0, last); break;
                    }
                }
                package.PackageProperties.Title = title;
                package.PackageProperties.Creator = "Falcon OCR";
                package.PackageProperties.Created = DateTime.Now;
                main.Document.Save();
            }
        }

        private sealed class Writer
        {
            private readonly MainDocumentPart _main;
            private readonly Body _body;
            private readonly ExportOptions _opt;
            private readonly LanguageModel _lang;
            private uint _drawingId = 1;

            public Writer(MainDocumentPart main, ExportOptions opt)
            {
                _main = main;
                _body = main.Document.Body;
                _opt = opt;
                _lang = LanguageCatalog.Get(opt.Language);
            }

            private bool IsCjk => _opt.Language == OcrLanguage.Chinese || _opt.Language == OcrLanguage.Japanese || _opt.Language == OcrLanguage.Korean;

            // ------------------------------------------------------------ styles & numbering

            public void AddStyles()
            {
                var part = _main.AddNewPart<StyleDefinitionsPart>();
                var fonts = new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = _lang.DefaultFont, ComplexScript = "Calibri" };
                var styles = new Styles(
                    new DocDefaults(
                        new RunPropertiesDefault(new RunPropertiesBaseStyle(fonts, new FontSize { Val = "22" }, new FontSizeComplexScript { Val = "22" },
                            new Languages { Val = _lang.Culture, EastAsia = IsCjk ? _lang.Culture : "zh-CN" })),
                        new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }))),
                    new Style(new StyleName { Val = "Normal" }, new PrimaryStyle()) { Type = StyleValues.Paragraph, StyleId = "Normal", Default = true });
                for (int lvl = 1; lvl <= 3; lvl++)
                {
                    styles.Append(new Style(
                        new StyleName { Val = "heading " + lvl },
                        new BasedOn { Val = "Normal" },
                        new NextParagraphStyle { Val = "Normal" },
                        new PrimaryStyle(),
                        new StyleParagraphProperties(new KeepNext(), new KeepLines(), new OutlineLevel { Val = lvl - 1 }),
                        new StyleRunProperties(new Bold(), new FontSize { Val = (36 - lvl * 6).ToString() }))
                    { Type = StyleValues.Paragraph, StyleId = "Heading" + lvl });
                }
                styles.Append(new Style(new StyleName { Val = "List Paragraph" }, new BasedOn { Val = "Normal" }, new PrimaryStyle())
                { Type = StyleValues.Paragraph, StyleId = "ListParagraph" });
                styles.Append(new Style(new StyleName { Val = "Table Grid" }, new BasedOn { Val = "TableNormal" },
                    new StyleTableProperties(new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 4 }, new LeftBorder { Val = BorderValues.Single, Size = 4 },
                        new BottomBorder { Val = BorderValues.Single, Size = 4 }, new RightBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 }, new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })))
                { Type = StyleValues.Table, StyleId = "TableGrid" });
                part.Styles = styles;
            }

            public void AddNumbering()
            {
                var part = _main.AddNewPart<NumberingDefinitionsPart>();
                part.Numbering = new Numbering(
                    new AbstractNum(
                        new MultiLevelType { Val = MultiLevelValues.SingleLevel },
                        new Level(
                            new StartNumberingValue { Val = 1 },
                            new NumberingFormat { Val = NumberFormatValues.Bullet },
                            new LevelText { Val = "•" },
                            new LevelJustification { Val = LevelJustificationValues.Left },
                            new PreviousParagraphProperties(new Indentation { Left = "720", Hanging = "360" }))
                        { LevelIndex = 0 })
                    { AbstractNumberId = 1 },
                    new NumberingInstance(new AbstractNumId { Val = 1 }) { NumberID = 1 });
            }

            // ------------------------------------------------------------ editable

            public void WriteEditablePage(OcrPage page, bool first, bool last)
            {
                var content = Geometry.Union(page.Sections.Select(s => s.Bounds));
                if (content.IsEmpty) content = new SD.RectangleF(page.Width * 0.1f, page.Height * 0.1f, page.Width * 0.8f, page.Height * 0.8f);
                var margins = Margins(page, content);

                // Consecutive single-column sections share one Word section; multi-column sections get their own.
                var groups = new List<List<LayoutSection>>();
                foreach (var s in page.Sections)
                {
                    bool multi = s.Columns.Count > 1;
                    if (groups.Count == 0 || multi || groups[groups.Count - 1][0].Columns.Count > 1) groups.Add(new List<LayoutSection>());
                    groups[groups.Count - 1].Add(s);
                }
                if (groups.Count == 0) groups.Add(new List<LayoutSection>());

                for (int g = 0; g < groups.Count; g++)
                {
                    var grp = groups[g];
                    bool multi = grp.Count == 1 && grp[0].Columns.Count > 1;
                    if (multi)
                    {
                        var s = grp[0];
                        var cols = s.Columns;
                        for (int c = 0; c < cols.Count; c++)
                        {
                            // Word column c starts at margin + (colRect_c.Left - colRect_0.Left).
                            float colLeft = content.Left + (cols[c].Left - cols[0].Left);
                            bool firstInCol = true;
                            foreach (var b in s.Blocks.Where(b => b.Column == c))
                            {
                                float before = firstInCol ? (c == 0 ? s.SpaceBefore : 0) + (b.Bounds.Top - s.Bounds.Top) : b.SpaceBefore;
                                WriteBlock(page, b, colLeft, before);
                                firstInCol = false;
                            }
                            if (c < cols.Count - 1) _body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" }), new Run(new Break { Type = BreakValues.Column })));
                        }
                    }
                    else
                    {
                        foreach (var s in grp)
                        {
                            bool firstInSection = true;
                            foreach (var b in s.Blocks)
                            {
                                float before = firstInSection ? s.SpaceBefore + (b.Bounds.Top - s.Bounds.Top) : b.SpaceBefore;
                                if (first && g == 0 && s == grp[0] && firstInSection) before = 0;
                                WriteBlock(page, b, content.Left, before);
                                firstInSection = false;
                            }
                        }
                    }

                    var type = g == 0 ? SectionMarkValues.NextPage : SectionMarkValues.Continuous;
                    var sp = SectionProps(page, margins, type, multi ? grp[0].Columns : null);
                    EndSection(sp, last && g == groups.Count - 1);
                }
            }

            private void WriteBlock(OcrPage page, LayoutBlock b, float colLeft, float spaceBeforePx)
            {
                float dpi = page.Dpi;
                int before = Math.Min(Units.Twips(spaceBeforePx, dpi), 2880);
                switch (b.Kind)
                {
                    case BlockKind.Table:
                        _body.Append(BuildTable(page, b.Table, colLeft, null));
                        return;
                    case BlockKind.Figure:
                        if (!_opt.IncludeImages || b.Figure?.Png == null) return;
                        var pp = new ParagraphProperties(
                            new SpacingBetweenLines { Before = before.ToString(), After = "0" },
                            new Indentation { Left = Math.Max(0, Units.Twips(b.Bounds.Left - colLeft, dpi)).ToString() });
                        _body.Append(new Paragraph(pp, new Run(InlineImage(b.Figure.Png, b.Bounds.Width, b.Bounds.Height, dpi))));
                        return;
                }

                var ppr = new ParagraphProperties();
                if (b.Kind == BlockKind.Heading) ppr.Append(new ParagraphStyleId { Val = "Heading" + Math.Max(1, Math.Min(3, b.HeadingLevel)) });
                else if (b.Kind == BlockKind.ListItem) ppr.Append(new ParagraphStyleId { Val = "ListParagraph" });
                bool bullet = b.Kind == BlockKind.ListItem && !b.OrderedList;
                if (bullet) ppr.Append(new NumberingProperties(new NumberingLevelReference { Val = 0 }, new NumberingId { Val = 1 }));
                if (_opt.KeepColors && !b.Style.BackColor.IsEmpty)
                    ppr.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = Units.Hex(b.Style.BackColor) });

                // Multi-segment lines (label ... value) keep their horizontal positions through tab stops.
                var tabLines = b.Lines.Where(l => l.Segments.Count > 1).ToList();
                if (tabLines.Count > 0 && b.Lines.Count <= 2)
                {
                    var tabs = new Tabs();
                    foreach (var seg in tabLines[0].Segments.Skip(1))
                        tabs.Append(new TabStop { Val = TabStopValues.Left, Position = Math.Max(0, Units.Twips(seg.Bounds.Left - b.Bounds.Left, dpi)) });
                    ppr.Append(tabs);
                }

                var spacing = new SpacingBetweenLines { Before = before.ToString(), After = "0" };
                if (b.Lines.Count > 1 && b.LineSpacing > 0)
                {
                    spacing.Line = Units.Twips(b.LineSpacing, dpi).ToString();
                    spacing.LineRule = LineSpacingRuleValues.AtLeast;
                }
                ppr.Append(spacing);

                int left = Math.Max(0, Units.Twips(b.Bounds.Left - colLeft, dpi));
                int first = Units.Twips(b.FirstLineIndent, dpi);
                var ind = new Indentation { Left = left.ToString() };
                if (first > 0) ind.FirstLine = first.ToString();
                else if (first < 0) ind.Hanging = Math.Min(-first, left).ToString();
                // Right indent keeps narrow centered/right-aligned blocks in place.
                ppr.Append(ind);
                ppr.Append(new Justification { Val = Jc(b.Align) });
                var para = new Paragraph(ppr);

                if (b.Kind == BlockKind.ListItem && b.OrderedList)
                {
                    para.Append(MakeRun(b.ListMarker, b.Style, b.Style.FontSizePt));
                    para.Append(new Run(new TabChar()));
                }

                bool keepLineBreaks = tabLines.Count > 0 && b.Lines.Count <= 2;
                for (int i = 0; i < b.Lines.Count; i++)
                {
                    var line = b.Lines[i];
                    if (i > 0)
                    {
                        if (keepLineBreaks) para.Append(new Run(new Break()));
                        else if (NeedsSpace(para, b, i)) para.Append(MakeRun(" ", b.Style, b.Style.FontSizePt));
                    }
                    for (int s = 0; s < line.Segments.Count; s++)
                    {
                        var seg = line.Segments[s];
                        string text = seg.Text;
                        if (i == 0 && s == 0 && b.MarkerPrefixLength > 0 && text.Length >= b.MarkerPrefixLength)
                            text = text.Substring(b.MarkerPrefixLength).TrimStart();
                        if (i < b.Lines.Count - 1 && s == line.Segments.Count - 1 && !keepLineBreaks && JoinsHyphen(text, b.Lines[i + 1]))
                            text = text.Substring(0, text.Length - 1);
                        if (s > 0) para.Append(keepLineBreaks ? new Run(new TabChar()) : MakeRun(" ", seg.Style, b.Style.FontSizePt));
                        float size = Math.Abs(seg.Style.FontSizePt - b.Style.FontSizePt) / b.Style.FontSizePt > 0.2f ? seg.Style.FontSizePt : b.Style.FontSizePt;
                        para.Append(MakeRun(text, seg.Style, size));
                    }
                }
                _body.Append(para);
            }

            private static bool JoinsHyphen(string text, LayoutLine next)
            {
                var nt = next.Text;
                return text.Length > 2 && text[text.Length - 1] == '-' && char.IsLetter(text[text.Length - 2]) && nt.Length > 0 && char.IsLower(nt[0]);
            }

            private static bool NeedsSpace(Paragraph p, LayoutBlock b, int lineIndex)
            {
                var prev = b.Lines[lineIndex - 1].Text.TrimEnd();
                var next = b.Lines[lineIndex].Text;
                if (prev.Length == 0 || next.Length == 0) return false;
                if (JoinsHyphen(prev, b.Lines[lineIndex])) return false;
                return !(LayoutBlock.JoinsWithoutSpace(prev[prev.Length - 1]) || LayoutBlock.JoinsWithoutSpace(next[0]));
            }

            // ------------------------------------------------------------ exact copy

            public void WriteExactPage(OcrPage page, bool first, bool last)
            {
                float dpi = page.Dpi;
                // Anchor paragraph for images (positioned relative to the page, behind text).
                var anchorPara = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0", Line = "20", LineRule = LineSpacingRuleValues.Exact }));
                if (_opt.IncludeImages)
                    foreach (var f in page.Blocks.Where(b => b.Kind == BlockKind.Figure && b.Figure?.Png != null))
                        anchorPara.Append(new Run(AnchoredImage(f.Figure.Png, f.Bounds, dpi)));
                _body.Append(anchorPara);

                foreach (var t in page.Blocks.Where(b => b.Kind == BlockKind.Table))
                {
                    _body.Append(BuildTable(page, t.Table, 0, t.Table.Bounds));
                    _body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0", Line = "20", LineRule = LineSpacingRuleValues.Exact })));
                }

                foreach (var line in ExportHelpers.PositionedLines(page))
                    _body.Append(FramedLine(line, dpi));

                var sp = SectionProps(page, new[] { 0, 0, 0, 0 }, SectionMarkValues.NextPage, null);
                EndSection(sp, last);
            }

            private Paragraph FramedLine(TextLine line, float dpi)
            {
                var s = line.Style;
                float fontPx = s.FontSizePt * dpi / 72f;
                var ink = line.InkBounds.IsEmpty ? line.Bounds : line.InkBounds;
                float lineH = fontPx * 1.22f;
                float top = ink.Top + ink.Height / 2 - lineH * 0.55f;
                float widthPt = line.Bounds.Width * 72f / dpi;
                int scale = TextMeasure.FitScale(line.Text, s, ink.Width * 72f / dpi);

                var ppr = new ParagraphProperties(
                    new FrameProperties
                    {
                        Width = Units.Twips(line.Bounds.Width * 1.25f + fontPx, dpi).ToString(),
                        X = Units.Twips(ink.Left, dpi).ToString(),
                        Y = Units.Twips(top, dpi).ToString(),
                        HorizontalPosition = HorizontalAnchorValues.Page,
                        VerticalPosition = VerticalAnchorValues.Page,
                        Wrap = TextWrappingValues.Around
                    },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = Units.Twips(lineH, dpi).ToString(), LineRule = LineSpacingRuleValues.Exact });
                if (_opt.KeepColors && !s.BackColor.IsEmpty)
                    ppr.InsertAfter(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = Units.Hex(s.BackColor) }, ppr.GetFirstChild<FrameProperties>());
                var run = MakeRun(line.Text, s, s.FontSizePt, scale);
                return new Paragraph(ppr, run);
            }

            // ------------------------------------------------------------ plain

            public void WritePlainPage(OcrPage page, bool first, bool last)
            {
                foreach (var b in page.Blocks)
                {
                    var t = b.Kind == BlockKind.Table ? b.Table.ToText() : b.Kind == BlockKind.Figure ? null : (b.ListMarker != null ? b.ListMarker + " " : "") + b.GetFlowText();
                    if (string.IsNullOrEmpty(t)) continue;
                    foreach (var line in t.Split('\n'))
                        _body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "120" }), new Run(new Text(line.TrimEnd('\r')) { Space = SpaceProcessingModeValues.Preserve })));
                }
                var content = Geometry.Union(page.Sections.Select(s => s.Bounds));
                EndSection(SectionProps(page, Margins(page, content.IsEmpty ? new SD.RectangleF(0, 0, page.Width, page.Height) : content), SectionMarkValues.NextPage, null), last);
            }

            // ------------------------------------------------------------ building blocks

            private Run MakeRun(string text, TextStyle s, float sizePt, int scalePct = 100)
            {
                var rpr = new RunProperties();
                string font = s.FontFamily ?? _lang.DefaultFont;
                rpr.Append(new RunFonts { Ascii = font, HighAnsi = font, ComplexScript = font, EastAsia = IsCjk ? _lang.DefaultFont : font });
                if (s.Bold) rpr.Append(new Bold());
                if (s.Italic) rpr.Append(new Italic());
                if (_opt.KeepColors && s.Color.ToArgb() != SD.Color.Black.ToArgb()) rpr.Append(new Color { Val = Units.Hex(s.Color) });
                if (scalePct != 100) rpr.Append(new CharacterScale { Val = scalePct });
                rpr.Append(new FontSize { Val = Units.HalfPoints(sizePt).ToString() });
                rpr.Append(new FontSizeComplexScript { Val = Units.HalfPoints(sizePt).ToString() });
                if (s.Underline) rpr.Append(new Underline { Val = UnderlineValues.Single });
                return new Run(rpr, new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            }

            private Table BuildTable(OcrPage page, TableRegion t, float colLeft, SD.RectangleF? absolute)
            {
                float dpi = page.Dpi;
                var tbl = new Table();
                var tp = new TableProperties();
                if (absolute.HasValue)
                {
                    tp.Append(new TablePositionProperties
                    {
                        LeftFromText = 0,
                        RightFromText = 0,
                        HorizontalAnchor = HorizontalAnchorValues.Page,
                        VerticalAnchor = VerticalAnchorValues.Page,
                        TablePositionX = Units.Twips(absolute.Value.Left, dpi),
                        TablePositionY = Units.Twips(absolute.Value.Top, dpi)
                    });
                    tp.Append(new TableOverlap { Val = TableOverlapValues.Overlap });
                }
                tp.Append(new TableWidth { Width = Units.Twips(t.Bounds.Width, dpi).ToString(), Type = TableWidthUnitValues.Dxa });
                if (!absolute.HasValue) tp.Append(new TableIndentation { Width = Math.Max(0, Units.Twips(t.Bounds.Left - colLeft, dpi)), Type = TableWidthUnitValues.Dxa });
                var border = t.HasBorders ? BorderValues.Single : BorderValues.None;
                string bc = Units.Hex(t.BorderColor);
                tp.Append(new TableBorders(
                    new TopBorder { Val = border, Size = 4, Color = bc }, new LeftBorder { Val = border, Size = 4, Color = bc },
                    new BottomBorder { Val = border, Size = 4, Color = bc }, new RightBorder { Val = border, Size = 4, Color = bc },
                    new InsideHorizontalBorder { Val = border, Size = 4, Color = bc }, new InsideVerticalBorder { Val = border, Size = 4, Color = bc }));
                tp.Append(new TableLayout { Type = TableLayoutValues.Fixed });
                tp.Append(new TableCellMarginDefault(
                    new TopMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                    new TableCellLeftMargin { Width = 60, Type = TableWidthValues.Dxa },
                    new BottomMargin { Width = "20", Type = TableWidthUnitValues.Dxa },
                    new TableCellRightMargin { Width = 60, Type = TableWidthValues.Dxa }));
                tbl.Append(tp);

                var grid = new TableGrid();
                var colW = new int[t.ColumnCount];
                for (int c = 0; c < t.ColumnCount; c++)
                {
                    colW[c] = Math.Max(120, Units.Twips(t.ColumnEdges[c + 1] - t.ColumnEdges[c], dpi));
                    grid.Append(new GridColumn { Width = colW[c].ToString() });
                }
                tbl.Append(grid);

                for (int r = 0; r < t.RowCount; r++)
                {
                    var tr = new TableRow(new TableRowProperties(new TableRowHeight
                    {
                        Val = (UInt32Value)(uint)Math.Max(120, Units.Twips(t.RowEdges[r + 1] - t.RowEdges[r], dpi)),
                        HeightType = HeightRuleValues.AtLeast
                    }));
                    for (int c = 0; c < t.ColumnCount;)
                    {
                        var cell = t.CellAt(r, c);
                        if (cell == null)
                        {
                            tr.Append(new TableCell(new TableCellProperties(new TableCellWidth { Width = colW[c].ToString(), Type = TableWidthUnitValues.Dxa }), new Paragraph()));
                            c++;
                            continue;
                        }
                        int span = Math.Min(cell.ColSpan, t.ColumnCount - c);
                        int width = colW.Skip(c).Take(span).Sum();
                        var tcp = new TableCellProperties(new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa });
                        if (span > 1) tcp.Append(new GridSpan { Val = span });
                        if (cell.RowSpan > 1) tcp.Append(new VerticalMerge { Val = cell.Row == r ? MergedCellValues.Restart : MergedCellValues.Continue });
                        if (_opt.KeepColors && !cell.Fill.IsEmpty) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = Units.Hex(cell.Fill) });
                        tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
                        var tc = new TableCell(tcp);
                        if (cell.Row == r)
                        {
                            var rows = cell.GroupRows();
                            foreach (var row in rows)
                            {
                                var p = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" }, new Justification { Val = Jc(cell.Align) }));
                                for (int k = 0; k < row.Count; k++)
                                {
                                    if (k > 0) p.Append(MakeRun(" ", row[k].Style, row[k].Style.FontSizePt));
                                    p.Append(MakeRun(row[k].Text, row[k].Style, row[k].Style.FontSizePt));
                                }
                                tc.Append(p);
                            }
                        }
                        if (!tc.Elements<Paragraph>().Any()) tc.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0" })));
                        tr.Append(tc);
                        c += span;
                    }
                    tbl.Append(tr);
                }
                return tbl;
            }

            private string AddImage(byte[] png)
            {
                var part = _main.AddImagePart(ImagePartType.Png);
                using (var ms = new MemoryStream(png)) part.FeedData(ms);
                return _main.GetIdOfPart(part);
            }

            private A.Graphic Graphic(string relId, long cx, long cy, uint id)
            {
                return new A.Graphic(new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = 0U, Name = "figure" + id + ".png" },
                            new PIC.NonVisualPictureDrawingProperties()),
                        new PIC.BlipFill(new A.Blip { Embed = relId }, new A.Stretch(new A.FillRectangle())),
                        new PIC.ShapeProperties(
                            new A.Transform2D(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = cx, Cy = cy }),
                            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" });
            }

            private Drawing InlineImage(byte[] png, float wPx, float hPx, float dpi)
            {
                var relId = AddImage(png);
                long cx = Units.Emu(wPx, dpi), cy = Units.Emu(hPx, dpi);
                uint id = _drawingId++;
                return new Drawing(new DW.Inline(
                    new DW.Extent { Cx = cx, Cy = cy },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = id, Name = "Figure " + id },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                    Graphic(relId, cx, cy, id))
                { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });
            }

            private Drawing AnchoredImage(byte[] png, SD.RectangleF r, float dpi)
            {
                var relId = AddImage(png);
                long cx = Units.Emu(r.Width, dpi), cy = Units.Emu(r.Height, dpi);
                uint id = _drawingId++;
                return new Drawing(new DW.Anchor(
                    new DW.SimplePosition { X = 0L, Y = 0L },
                    new DW.HorizontalPosition(new DW.PositionOffset(Units.Emu(r.Left, dpi).ToString())) { RelativeFrom = DW.HorizontalRelativePositionValues.Page },
                    new DW.VerticalPosition(new DW.PositionOffset(Units.Emu(r.Top, dpi).ToString())) { RelativeFrom = DW.VerticalRelativePositionValues.Page },
                    new DW.Extent { Cx = cx, Cy = cy },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.WrapNone(),
                    new DW.DocProperties { Id = id, Name = "Figure " + id },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                    Graphic(relId, cx, cy, id))
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U,
                    SimplePos = false,
                    RelativeHeight = id,
                    BehindDoc = true,
                    Locked = false,
                    LayoutInCell = true,
                    AllowOverlap = true
                });
            }

            // ------------------------------------------------------------ sections

            /// <summary>Margins [left, top, right, bottom] in twips derived from where content sits on the page.</summary>
            private static int[] Margins(OcrPage page, SD.RectangleF content)
            {
                float dpi = page.Dpi;
                int Clamp(float px, int max) => Math.Max(360, Math.Min(max, Units.Twips(px, dpi)));
                return new[]
                {
                    Clamp(content.Left, 2880),
                    Clamp(content.Top, 2880),
                    Clamp(page.Width - content.Right, 2880),
                    Math.Min(720, Clamp(page.Height - content.Bottom, 2880)) // small bottom margin absorbs font-metric drift
                };
            }

            private static SectionProperties SectionProps(OcrPage page, int[] m, SectionMarkValues type, List<SD.RectangleF> columns)
            {
                float dpi = page.Dpi;
                uint w = (uint)Units.Twips(page.Width, dpi), h = (uint)Units.Twips(page.Height, dpi);
                var ps = new PageSize { Width = w, Height = h };
                if (w > h) ps.Orient = PageOrientationValues.Landscape;
                var sp = new SectionProperties(
                    new SectionType { Val = type },
                    ps,
                    new PageMargin { Left = (uint)m[0], Top = m[1], Right = (uint)m[2], Bottom = m[3], Header = 360U, Footer = 360U, Gutter = 0U });
                if (columns != null && columns.Count > 1)
                {
                    var cols = new Columns { EqualWidth = false, ColumnCount = (Int16Value)(short)columns.Count };
                    for (int i = 0; i < columns.Count; i++)
                    {
                        var col = new Column { Width = Math.Max(720, Units.Twips(columns[i].Width, dpi)).ToString() };
                        if (i < columns.Count - 1) col.Space = Math.Max(144, Units.Twips(columns[i + 1].Left - columns[i].Right, dpi)).ToString();
                        cols.Append(col);
                    }
                    sp.Append(cols);
                }
                else sp.Append(new Columns { Space = "720" });
                return sp;
            }

            private void EndSection(SectionProperties sp, bool lastInDocument)
            {
                if (lastInDocument)
                {
                    _body.Append(sp);
                    return;
                }
                var lastPara = _body.LastChild as Paragraph;
                if (lastPara == null || lastPara.ParagraphProperties?.SectionProperties != null || lastPara.ParagraphProperties?.FrameProperties != null)
                {
                    lastPara = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0", Line = "20", LineRule = LineSpacingRuleValues.Exact }));
                    _body.Append(lastPara);
                }
                if (lastPara.ParagraphProperties == null) lastPara.PrependChild(new ParagraphProperties());
                lastPara.ParagraphProperties.Append(sp);
            }

            private static JustificationValues Jc(TextAlign a)
            {
                switch (a)
                {
                    case TextAlign.Center: return JustificationValues.Center;
                    case TextAlign.Right: return JustificationValues.Right;
                    case TextAlign.Justify: return JustificationValues.Both;
                    default: return JustificationValues.Left;
                }
            }
        }
    }
}
