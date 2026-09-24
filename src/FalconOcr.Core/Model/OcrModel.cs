using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace FalconOcr.Model
{
    /// <summary>Recognition result for one page. All coordinates are pixels of the processed page image.</summary>
    public sealed class OcrPage
    {
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>Resolution of the page image; converts pixels to physical sizes (pt, twips, EMU).</summary>
        public float Dpi { get; set; } = 96f;

        public Color Background { get; set; } = Color.White;

        /// <summary>Every recognized text line (raw detector/recognizer output or PDF text layer).</summary>
        public List<TextLine> Lines { get; } = new List<TextLine>();

        /// <summary>Layout blocks in reading order (paragraphs, headings, list items, tables, figures).</summary>
        public List<LayoutBlock> Blocks { get; } = new List<LayoutBlock>();

        /// <summary>Column sections in reading order; each block belongs to exactly one section/column.</summary>
        public List<LayoutSection> Sections { get; } = new List<LayoutSection>();

        public bool FromTextLayer { get; set; }
        public TimeSpan Elapsed { get; set; }

        public float PxToPt(float px) => px * 72f / Dpi;

        public string GetPlainText()
        {
            var sb = new StringBuilder();
            foreach (var b in Blocks)
            {
                var t = b.GetText();
                if (t.Length == 0) continue;
                sb.AppendLine(t);
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        public float MeanConfidence => Lines.Count == 0 ? 0f : Lines.Average(l => l.Confidence);
    }

    public sealed class TextStyle
    {
        public float FontSizePt { get; set; } = 11f;
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public Color Color { get; set; } = Color.Black;
        /// <summary>Background fill behind the text, or <see cref="Color.Empty"/> when it is the page background.</summary>
        public Color BackColor { get; set; } = Color.Empty;
        public string FontFamily { get; set; }

        public TextStyle Clone() => (TextStyle)MemberwiseClone();

        public bool SameAs(TextStyle o, float sizeTolerance = 0.12f)
        {
            if (o == null) return false;
            float rel = Math.Abs(FontSizePt - o.FontSizePt) / Math.Max(1f, Math.Max(FontSizePt, o.FontSizePt));
            return rel <= sizeTolerance && Bold == o.Bold && Italic == o.Italic && ColorClose(Color, o.Color, 60);
        }

        public static bool ColorClose(Color a, Color b, int tol)
        {
            return Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) <= tol;
        }
    }

    /// <summary>A single detected text line (one detector box).</summary>
    public sealed class TextLine
    {
        public int Id { get; set; }

        /// <summary>Quadrilateral TL, TR, BR, BL in page pixels.</summary>
        public PointF[] Polygon { get; set; }

        public RectangleF Bounds { get; set; }

        public string Text { get; set; } = "";
        public float Confidence { get; set; } = 1f;

        /// <summary>Per-character horizontal extents (page pixels, along the line); same length as <see cref="Text"/> or null.</summary>
        public float[] CharLeft { get; set; }
        public float[] CharRight { get; set; }
        /// <summary>Per-character confidence; same length as <see cref="Text"/> or null.</summary>
        public float[] CharConfidence { get; set; }

        public TextStyle Style { get; set; } = new TextStyle();

        /// <summary>Tight ink extent measured on the image (used for font size and baseline).</summary>
        public RectangleF InkBounds { get; set; }

        /// <summary>True when the line was absorbed by a figure (its pixels are exported as part of the image).</summary>
        public bool InFigure { get; set; }

        /// <summary>User edited this line in the results view.</summary>
        public bool Edited { get; set; }

        public float CenterX => Bounds.X + Bounds.Width / 2f;
        public float CenterY => Bounds.Y + Bounds.Height / 2f;

        public override string ToString() => Text;
    }

    /// <summary>Lines that sit on the same visual baseline inside a column (detector may split a line into segments).</summary>
    public sealed class LayoutLine
    {
        public List<TextLine> Segments { get; } = new List<TextLine>();
        public RectangleF Bounds { get; set; }

        public string Text
        {
            get
            {
                if (Segments.Count == 1) return Segments[0].Text;
                var sb = new StringBuilder();
                for (int i = 0; i < Segments.Count; i++)
                {
                    if (i > 0)
                    {
                        // Wide gaps become tabs so that tabular text keeps its columns.
                        float gap = Segments[i].Bounds.Left - Segments[i - 1].Bounds.Right;
                        sb.Append(gap > Bounds.Height * 2.5f ? "\t" : " ");
                    }
                    sb.Append(Segments[i].Text);
                }
                return sb.ToString();
            }
        }

        public TextStyle Style => Segments.Count == 0 ? new TextStyle() : Segments.OrderByDescending(s => s.Text.Length).First().Style;
    }

    public enum BlockKind
    {
        Paragraph,
        Heading,
        ListItem,
        Table,
        Figure
    }

    public enum TextAlign
    {
        Left,
        Center,
        Right,
        Justify
    }

    public sealed class LayoutBlock
    {
        public BlockKind Kind { get; set; }
        public RectangleF Bounds { get; set; }
        public List<LayoutLine> Lines { get; } = new List<LayoutLine>();
        public TextAlign Align { get; set; }
        public int HeadingLevel { get; set; }

        /// <summary>Bullet/number marker of a list item ("•", "1.", "a)") — not repeated in the line text.</summary>
        public string ListMarker { get; set; }
        public bool OrderedList { get; set; }

        /// <summary>Characters of the first line that form the list marker (stripped from flowing text).</summary>
        public int MarkerPrefixLength { get; set; }

        /// <summary>Left edge of the list item's text (after the marker), px.</summary>
        public float TextLeft { get; set; }

        /// <summary>Position of a drawn (non-text) bullet glyph; empty when the marker is part of the text.</summary>
        public RectangleF MarkerGlyph { get; set; }

        /// <summary>Indent of the first line relative to <see cref="Bounds"/>.Left (px, may be negative for hanging).</summary>
        public float FirstLineIndent { get; set; }

        /// <summary>Vertical space before this block (px) measured on the page.</summary>
        public float SpaceBefore { get; set; }

        /// <summary>Median baseline-to-baseline distance (px) of the block's lines.</summary>
        public float LineSpacing { get; set; }

        public TextStyle Style { get; set; } = new TextStyle();

        public TableRegion Table { get; set; }
        public FigureRegion Figure { get; set; }

        public LayoutSection Section { get; set; }
        public int Column { get; set; }

        public IEnumerable<TextLine> AllTextLines()
        {
            if (Table != null) return Table.Cells.SelectMany(c => c.Lines);
            return Lines.SelectMany(l => l.Segments);
        }

        public string GetText()
        {
            switch (Kind)
            {
                case BlockKind.Table:
                    return Table == null ? "" : Table.ToText();
                case BlockKind.Figure:
                    return "";
                default:
                    var body = string.Join(Kind == BlockKind.Heading ? " " : Environment.NewLine, Lines.Select((l, i) => LineText(i)));
                    return ListMarker != null ? ListMarker + " " + body : body;
            }
        }

        /// <summary>Text of line <paramref name="i"/> without the list marker.</summary>
        public string LineText(int i)
        {
            var t = Lines[i].Text;
            if (i == 0 && MarkerPrefixLength > 0 && t.Length >= MarkerPrefixLength)
                t = t.Substring(MarkerPrefixLength).TrimStart();
            return t;
        }

        /// <summary>Paragraph text with soft line breaks joined (de-hyphenated) for flowing output.</summary>
        public string GetFlowText()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Lines.Count; i++)
            {
                var t = LineText(i).TrimEnd();
                if (i > 0 && sb.Length > 0)
                {
                    char last = sb[sb.Length - 1];
                    if (last == '-' && t.Length > 0 && char.IsLower(t[0]) && sb.Length > 1 && char.IsLetter(sb[sb.Length - 2]))
                        sb.Length--; // re-join hyphenated word
                    else if (!JoinsWithoutSpace(last) && !(t.Length > 0 && JoinsWithoutSpace(t[0])))
                        sb.Append(' ');
                }
                sb.Append(t);
            }
            return sb.ToString();
        }

        /// <summary>Chinese/Japanese text wraps without spaces; Korean (Hangul) uses spaces between words.</summary>
        internal static bool JoinsWithoutSpace(char c) => IsCjk(c) && !(c >= 0xAC00 && c <= 0xD7AF);

        internal static bool IsCjk(char c)
        {
            return (c >= 0x3040 && c <= 0x30FF) || (c >= 0x3400 && c <= 0x9FFF) || (c >= 0xAC00 && c <= 0xD7AF) || (c >= 0xFF00 && c <= 0xFFEF) || (c >= 0x3000 && c <= 0x303F);
        }
    }

    /// <summary>A horizontal band of the page with a fixed number of text columns.</summary>
    public sealed class LayoutSection
    {
        public RectangleF Bounds { get; set; }
        /// <summary>Column rectangles left to right; a single-column section has one entry.</summary>
        public List<RectangleF> Columns { get; } = new List<RectangleF>();
        public List<LayoutBlock> Blocks { get; } = new List<LayoutBlock>();
        /// <summary>Distance from the previous section's bottom (or the top of the content) in px.</summary>
        public float SpaceBefore { get; set; }
    }

    public sealed class TableRegion
    {
        public RectangleF Bounds { get; set; }
        /// <summary>Column boundaries (Count = columns + 1), page pixels.</summary>
        public List<float> ColumnEdges { get; } = new List<float>();
        /// <summary>Row boundaries (Count = rows + 1), page pixels.</summary>
        public List<float> RowEdges { get; } = new List<float>();
        public List<TableCell> Cells { get; } = new List<TableCell>();
        /// <summary>True when the table was found from ruling lines (borders are drawn).</summary>
        public bool HasBorders { get; set; } = true;
        public Color BorderColor { get; set; } = Color.Black;

        public int RowCount => Math.Max(0, RowEdges.Count - 1);
        public int ColumnCount => Math.Max(0, ColumnEdges.Count - 1);

        public TableCell CellAt(int row, int col)
        {
            foreach (var c in Cells)
                if (row >= c.Row && row < c.Row + c.RowSpan && col >= c.Col && col < c.Col + c.ColSpan)
                    return c;
            return null;
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < RowCount; r++)
            {
                var parts = new List<string>();
                for (int c = 0; c < ColumnCount; c++)
                {
                    var cell = CellAt(r, c);
                    parts.Add(cell != null && cell.Row == r && cell.Col == c ? cell.Text.Replace(Environment.NewLine, " ") : "");
                }
                sb.AppendLine(string.Join("\t", parts));
            }
            return sb.ToString().TrimEnd();
        }
    }

    public sealed class TableCell
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int RowSpan { get; set; } = 1;
        public int ColSpan { get; set; } = 1;
        public RectangleF Bounds { get; set; }
        public List<TextLine> Lines { get; } = new List<TextLine>();
        public Color Fill { get; set; } = Color.Empty;
        public TextAlign Align { get; set; }

        public string Text => string.Join(Environment.NewLine, GroupRows().Select(r => string.Join(" ", r.Select(l => l.Text))));

        public TextStyle Style => Lines.Count == 0 ? new TextStyle() : Lines.OrderByDescending(l => l.Text.Length).First().Style;

        /// <summary>Lines of the cell grouped into visual rows (top to bottom, left to right).</summary>
        public List<List<TextLine>> GroupRows()
        {
            var rows = new List<List<TextLine>>();
            foreach (var l in Lines.OrderBy(l => l.CenterY))
            {
                var row = rows.LastOrDefault();
                if (row != null && Math.Abs(row[0].CenterY - l.CenterY) < Math.Min(row[0].Bounds.Height, l.Bounds.Height) * 0.5f)
                    row.Add(l);
                else
                    rows.Add(new List<TextLine> { l });
            }
            foreach (var r in rows) r.Sort((a, b) => a.Bounds.X.CompareTo(b.Bounds.X));
            return rows;
        }
    }

    public sealed class FigureRegion
    {
        public RectangleF Bounds { get; set; }
        /// <summary>PNG bytes of the cropped region.</summary>
        public byte[] Png { get; set; }
    }
}
