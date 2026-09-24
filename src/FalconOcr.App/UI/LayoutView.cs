using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FalconOcr.Export;
using FalconOcr.Model;

namespace FalconOcr.App.UI
{
    /// <summary>
    /// The recognized page redrawn at the positions of the original: text lines with their estimated font,
    /// size, weight and color (horizontally fitted to the source line), tables with borders/fills, figures,
    /// bullets. Lines can be selected (synchronised with the image) and edited in place (double-click).
    /// </summary>
    internal sealed class LayoutView : PageCanvas
    {
        private OcrPage _page;
        private readonly Dictionary<FigureRegion, Bitmap> _figures = new Dictionary<FigureRegion, Bitmap>();
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private TextBox _editor;
        private TextLine _editing;
        private TextLine _hover;

        public LayoutView()
        {
            BackColor = Theme.Canvas;
        }

        public event EventHandler<TextLine> LineSelected;
        public event EventHandler<TextLine> LineEdited;

        public TextLine SelectedLine { get; private set; }

        /// <summary>Highlight characters with low recognition confidence.</summary>
        public bool ShowConfidence { get; set; } = true;

        public string Placeholder { get; set; } = "Click Recognize to convert this page";

        public OcrPage Page
        {
            get => _page;
            set
            {
                CommitEdit(false);
                _page = value;
                foreach (var b in _figures.Values) b.Dispose();
                _figures.Clear();
                SelectedLine = null;
                if (value != null)
                {
                    PageSize = new Size(value.Width, value.Height);
                    PageDpi = value.Dpi;
                }
                Invalidate();
            }
        }

        public void Select(TextLine line, bool scroll)
        {
            SelectedLine = line;
            if (line != null && scroll) EnsureVisible(line.Bounds);
            Invalidate();
        }

        public LayoutBlock BlockOf(TextLine line)
        {
            if (_page == null || line == null) return null;
            return _page.Blocks.FirstOrDefault(b => b.AllTextLines().Contains(line));
        }

        protected override void DrawPage(Graphics g)
        {
            if (_page == null)
            {
                g.Clear(Color.White);
                return;
            }
            g.Clear(_page.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            float px = 1f / ViewScale;

            foreach (var b in _page.Blocks)
            {
                if (b.Kind == BlockKind.Figure && b.Figure?.Png != null)
                {
                    if (!_figures.TryGetValue(b.Figure, out var bmp))
                    {
                        using (var ms = new MemoryStream(b.Figure.Png)) bmp = new Bitmap(ms);
                        _figures[b.Figure] = bmp;
                    }
                    g.DrawImage(bmp, b.Figure.Bounds);
                }
                else if (b.Kind == BlockKind.Table) DrawTable(g, b.Table, px);
                else if (!b.Style.BackColor.IsEmpty)
                    using (var fill = new SolidBrush(b.Style.BackColor))
                        g.FillRectangle(fill, RectangleF.Inflate(b.Bounds, 4 * px, 2 * px));
            }

            foreach (var line in ExportHelpers.PositionedLines(_page).Concat(TableLines()))
                DrawLine(g, line, px);

            if (_hover != null && _hover != SelectedLine)
                using (var p = new Pen(Color.FromArgb(120, Theme.Accent), px)) g.DrawRectangle(p, _hover.Bounds.X, _hover.Bounds.Y, _hover.Bounds.Width, _hover.Bounds.Height);
            if (SelectedLine != null)
            {
                using (var b = new SolidBrush(Color.FromArgb(40, Theme.Accent))) g.FillRectangle(b, SelectedLine.Bounds);
                using (var p = new Pen(Theme.Accent, 2 * px)) g.DrawRectangle(p, SelectedLine.Bounds.X, SelectedLine.Bounds.Y, SelectedLine.Bounds.Width, SelectedLine.Bounds.Height);
            }
        }

        private IEnumerable<TextLine> TableLines() => _page.Blocks.Where(b => b.Kind == BlockKind.Table).SelectMany(b => b.AllTextLines());

        private void DrawTable(Graphics g, TableRegion t, float px)
        {
            foreach (var c in t.Cells.Where(c => !c.Fill.IsEmpty))
                using (var b = new SolidBrush(c.Fill)) g.FillRectangle(b, c.Bounds);
            if (!t.HasBorders)
            {
                using (var p = new Pen(Color.FromArgb(90, 24, 90, 189), px) { DashStyle = DashStyle.Dot })
                    foreach (var c in t.Cells) g.DrawRectangle(p, c.Bounds.X, c.Bounds.Y, c.Bounds.Width, c.Bounds.Height);
                return;
            }
            using (var p = new Pen(t.BorderColor, Math.Max(px, 1.5f)))
                foreach (var c in t.Cells) g.DrawRectangle(p, c.Bounds.X, c.Bounds.Y, c.Bounds.Width, c.Bounds.Height);
        }

        private void DrawLine(Graphics g, TextLine line, float px)
        {
            var s = line.Style;
            float fontPx = Math.Max(2, s.FontSizePt * _page.Dpi / 72f);
            var ink = line.InkBounds.IsEmpty ? line.Bounds : line.InkBounds;
            var font = GetFont(s, fontPx);

            if (!s.BackColor.IsEmpty)
                using (var bb = new SolidBrush(s.BackColor)) g.FillRectangle(bb, line.Bounds);

            if (ShowConfidence && line.CharConfidence != null && line.CharLeft != null && !line.Edited && line.CharConfidence.Length == line.CharLeft.Length)
            {
                using (var hl = new SolidBrush(Theme.LowConfidence))
                    for (int i = 0; i < line.CharConfidence.Length; i++)
                        if (line.CharConfidence[i] < 0.75f && !char.IsWhiteSpace(line.Text[i]))
                            g.FillRectangle(hl, line.CharLeft[i], line.Bounds.Top, Math.Max(px * 2, line.CharRight[i] - line.CharLeft[i]), line.Bounds.Height);
            }
            else if (ShowConfidence && line.Confidence < 0.75f)
                using (var hl = new SolidBrush(Theme.LowConfidence)) g.FillRectangle(hl, line.Bounds);

            var size = g.MeasureString(line.Text, font, PointF.Empty, StringFormat.GenericTypographic);
            if (size.Width <= 0) return;
            float sx = Math.Max(0.4f, Math.Min(2.5f, ink.Width / size.Width));
            float lineH = font.GetHeight(g);
            float top = ink.Top + ink.Height / 2 - lineH * 0.55f;
            var state = g.Save();
            g.TranslateTransform(ink.Left, top);
            g.ScaleTransform(sx, 1);
            using (var b = new SolidBrush(s.Color)) g.DrawString(line.Text, font, b, 0, 0, StringFormat.GenericTypographic);
            g.Restore(state);
            if (line.Edited)
                using (var p = new Pen(Color.FromArgb(160, 24, 90, 189), px) { DashStyle = DashStyle.Dash })
                    g.DrawLine(p, line.Bounds.Left, line.Bounds.Bottom, line.Bounds.Right, line.Bounds.Bottom);
        }

        private Font GetFont(TextStyle s, float px)
        {
            string family = TextMeasure.IsInstalled(s.FontFamily) ? s.FontFamily : "Segoe UI";
            var style = (s.Bold ? FontStyle.Bold : 0) | (s.Italic ? FontStyle.Italic : 0) | (s.Underline ? FontStyle.Underline : 0);
            string key = family + "|" + Math.Round(px, 1) + "|" + (int)style;
            if (!_fonts.TryGetValue(key, out var f))
            {
                f = new Font(family, px, style, GraphicsUnit.Pixel);
                _fonts[key] = f;
            }
            return f;
        }

        protected override void DrawOverlay(Graphics g)
        {
            if (_page == null)
                TextRenderer.DrawText(g, Placeholder, Theme.Heading, ClientRectangle, Theme.SubText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private TextLine HitTest(Point client)
        {
            if (_page == null) return null;
            var p = ClientToPage(client);
            return _page.Lines.Concat(TableLines()).Where(l => !l.InFigure).FirstOrDefault(l => RectangleF.Inflate(l.Bounds, 2, 2).Contains(p));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (PanTool) return;
            var h = HitTest(e.Location);
            if (h != _hover)
            {
                _hover = h;
                Cursor = h != null ? Cursors.IBeam : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || PanTool) return;
            CommitEdit(true);
            SelectedLine = HitTest(e.Location);
            Invalidate();
            LineSelected?.Invoke(this, SelectedLine);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            var hit = HitTest(e.Location);
            if (hit != null) BeginEdit(hit);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_editor == null && SelectedLine != null && (keyData == Keys.F2 || keyData == Keys.Enter))
            {
                BeginEdit(SelectedLine);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void BeginEdit(TextLine line)
        {
            CommitEdit(true);
            _editing = line;
            SelectedLine = line;
            var r = PageToClient(line.Bounds);
            float fontPt = Math.Max(8, Math.Min(28, line.Style.FontSizePt * Zoom));
            _editor = new TextBox
            {
                Text = line.Text,
                Font = new Font(TextMeasure.IsInstalled(line.Style.FontFamily) ? line.Style.FontFamily : "Segoe UI", fontPt, line.Style.Bold ? FontStyle.Bold : FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point((int)r.X, (int)r.Y),
                Width = Math.Max((int)r.Width + 40, 160)
            };
            _editor.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitEdit(true); }
                else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; CommitEdit(false); }
            };
            _editor.LostFocus += (s, e) => CommitEdit(true);
            Controls.Add(_editor);
            _editor.Focus();
            _editor.SelectAll();
            Invalidate();
        }

        private void CommitEdit(bool save)
        {
            if (_editor == null) return;
            var editor = _editor;
            var line = _editing;
            _editor = null;
            _editing = null;
            if (save && line != null && editor.Text != line.Text)
            {
                line.Text = editor.Text;
                line.Edited = true;
                line.CharLeft = null;
                line.CharRight = null;
                line.CharConfidence = null;
                line.Confidence = 1f;
                LineEdited?.Invoke(this, line);
            }
            Controls.Remove(editor);
            editor.Dispose();
            Invalidate();
            Focus();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            CommitEdit(true);
            base.OnScroll(se);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var b in _figures.Values) b.Dispose();
                foreach (var f in _fonts.Values) f.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
