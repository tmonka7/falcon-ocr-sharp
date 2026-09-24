using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using FalconOcr.Model;

namespace FalconOcr.App.UI
{
    /// <summary>Shows the (processed) page image; optional recognition overlay and crop selection.</summary>
    internal sealed class ImageViewer : PageCanvas
    {
        private Bitmap _image;
        private bool _cropMode;
        private Point? _dragStart;
        private Rectangle _dragRect;

        public event EventHandler<Rectangle> CropSelected;
        public event EventHandler<TextLine> LineClicked;

        public Bitmap Image
        {
            get => _image;
            set
            {
                _image = value;
                if (value != null)
                {
                    PageSize = value.Size;
                    PageDpi = Result?.Dpi ?? (value.HorizontalResolution > 0 ? value.HorizontalResolution : 96);
                }
                Invalidate();
            }
        }

        /// <summary>Recognition result used for the overlay and selection highlight.</summary>
        public OcrPage Result { get; set; }

        /// <summary>Draw detected text lines / tables / figures on top of the image.</summary>
        public bool ShowOverlay { get; set; }

        public TextLine SelectedLine { get; set; }

        public string Placeholder { get; set; } = "Add files to get started";

        public bool CropMode
        {
            get => _cropMode;
            set
            {
                _cropMode = value;
                _dragStart = null;
                Cursor = value ? Cursors.Cross : PanTool ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void DrawPage(Graphics g)
        {
            if (_image == null)
            {
                g.Clear(Color.White);
                return;
            }
            g.InterpolationMode = ViewScale < 1 ? InterpolationMode.HighQualityBicubic : InterpolationMode.Bilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_image, new Rectangle(0, 0, _image.Width, _image.Height));
            g.PixelOffsetMode = PixelOffsetMode.Default;

            var page = Result;
            if (page == null) return;
            float px = 1f / ViewScale; // one screen pixel in page units
            if (ShowOverlay)
            {
                using (var text = new Pen(Color.FromArgb(200, 19, 134, 92), 1.5f * px))
                using (var fill = new SolidBrush(Color.FromArgb(28, 19, 134, 92)))
                {
                    foreach (var l in page.Lines.Where(l => !l.InFigure))
                    {
                        g.FillPolygon(fill, l.Polygon);
                        g.DrawPolygon(text, l.Polygon);
                    }
                }
                using (var tp = new Pen(Color.FromArgb(220, 24, 90, 189), 2.5f * px))
                using (var fp = new Pen(Color.FromArgb(220, 228, 120, 20), 2.5f * px) { DashStyle = DashStyle.Dash })
                using (var hp = new Pen(Color.FromArgb(150, 90, 90, 90), 1f * px) { DashStyle = DashStyle.Dot })
                {
                    foreach (var b in page.Blocks)
                    {
                        if (b.Kind == BlockKind.Table) g.DrawRectangle(tp, b.Bounds.X, b.Bounds.Y, b.Bounds.Width, b.Bounds.Height);
                        else if (b.Kind == BlockKind.Figure) g.DrawRectangle(fp, b.Bounds.X, b.Bounds.Y, b.Bounds.Width, b.Bounds.Height);
                        else g.DrawRectangle(hp, b.Bounds.X - 3 * px, b.Bounds.Y - 3 * px, b.Bounds.Width + 6 * px, b.Bounds.Height + 6 * px);
                    }
                }
            }
            if (SelectedLine != null)
            {
                using (var b = new SolidBrush(Theme.Selection)) g.FillPolygon(b, SelectedLine.Polygon);
                using (var p = new Pen(Theme.Accent, 2 * px)) g.DrawPolygon(p, SelectedLine.Polygon);
            }
        }

        protected override void DrawOverlay(Graphics g)
        {
            if (_image == null)
            {
                TextRenderer.DrawText(g, Placeholder, Theme.Heading, ClientRectangle, Theme.SubText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            if (_cropMode && _dragStart.HasValue && _dragRect.Width > 0)
            {
                // Dim everything outside the selection.
                using (var region = new Region(ClientRectangle))
                {
                    region.Exclude(_dragRect);
                    using (var b = new SolidBrush(Color.FromArgb(110, 0, 0, 0))) g.FillRegion(b, region);
                }
                using (var p = new Pen(Color.White, 1.5f) { DashStyle = DashStyle.Dash }) g.DrawRectangle(p, _dragRect);
            }
            else if (_cropMode)
            {
                var r = new Rectangle(0, 0, ClientSize.Width, (int)(30 * DeviceDpi / 96f));
                using (var b = new SolidBrush(Color.FromArgb(220, 31, 41, 55))) g.FillRectangle(b, r);
                TextRenderer.DrawText(g, "Drag to select the area to keep — Esc to cancel", Theme.Base, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_cropMode && e.Button == MouseButtons.Left && _image != null)
            {
                Focus();
                _dragStart = e.Location;
                _dragRect = Rectangle.Empty;
                return;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_cropMode && _dragStart.HasValue)
            {
                var s = _dragStart.Value;
                _dragRect = Rectangle.FromLTRB(Math.Min(s.X, e.X), Math.Min(s.Y, e.Y), Math.Max(s.X, e.X), Math.Max(s.Y, e.Y));
                Invalidate();
                return;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_cropMode && _dragStart.HasValue)
            {
                var a = ClientToPage(_dragRect.Location);
                var b = ClientToPage(new Point(_dragRect.Right, _dragRect.Bottom));
                _dragStart = null;
                var r = Rectangle.Round(RectangleF.FromLTRB(Math.Max(0, a.X), Math.Max(0, a.Y), Math.Min(PageSize.Width, b.X), Math.Min(PageSize.Height, b.Y)));
                Invalidate();
                if (r.Width > 16 && r.Height > 16) CropSelected?.Invoke(this, r);
                return;
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_cropMode || Result == null || e.Button != MouseButtons.Left || PanTool) return;
            var p = ClientToPage(e.Location);
            var hit = Result.Lines.FirstOrDefault(l => l.Bounds.Contains(p));
            LineClicked?.Invoke(this, hit);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && _cropMode)
            {
                CropMode = false;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
