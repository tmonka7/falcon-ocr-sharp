using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FalconOcr.App.UI
{
    internal enum ZoomMode
    {
        Custom,
        FitWidth,
        FitPage
    }

    /// <summary>
    /// Scrollable, zoomable surface that shows one page in page-pixel coordinates. Two canvases with the same
    /// page size/DPI and zoom map every page pixel to the same screen offset, which keeps the original image
    /// and the recognized layout aligned side by side.
    /// </summary>
    internal abstract class PageCanvas : Panel
    {
        private float _zoom = 1f;
        private ZoomMode _mode = ZoomMode.FitWidth;
        private Size _pageSize = new Size(850, 1100);
        private float _pageDpi = 96f;
        private bool _panning;
        private Point _panStart;
        private Point _scrollStart;

        protected PageCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            AutoScroll = true;
            BackColor = Theme.Canvas;
            TabStop = true;
        }

        public event EventHandler ZoomChanged;
        public event EventHandler ViewScrolled;

        public const float MinZoom = 0.1f, MaxZoom = 6f;
        public int Margin2 => (int)(16 * DeviceDpi / 96f);

        /// <summary>Hand tool: left-drag pans (middle-drag always pans).</summary>
        public bool PanTool { get; set; }

        public Size PageSize
        {
            get => _pageSize;
            set { _pageSize = value.Width > 0 && value.Height > 0 ? value : new Size(850, 1100); UpdateLayoutSize(); }
        }

        public float PageDpi
        {
            get => _pageDpi;
            set { _pageDpi = value > 0 ? value : 96f; UpdateLayoutSize(); }
        }

        public ZoomMode ZoomMode
        {
            get => _mode;
            set { _mode = value; UpdateLayoutSize(); ZoomChanged?.Invoke(this, EventArgs.Empty); }
        }

        /// <summary>1.0 = physical size (a 200 dpi page appears at real-world size on screen).</summary>
        public float Zoom
        {
            get => _zoom;
            set
            {
                float z = Math.Max(MinZoom, Math.Min(MaxZoom, value));
                _mode = ZoomMode.Custom;
                if (Math.Abs(z - _zoom) < 0.0001f) return;
                // Keep the view center stable while zooming.
                var center = ClientToPage(new Point(ClientSize.Width / 2, ClientSize.Height / 2));
                _zoom = z;
                UpdateLayoutSize();
                CenterOn(center);
                ZoomChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Screen pixels per page pixel.</summary>
        public float ViewScale => _zoom * (DeviceDpi / 96f) * 96f / _pageDpi;

        public Rectangle PageRectOnCanvas
        {
            get
            {
                int w = (int)Math.Round(_pageSize.Width * ViewScale), h = (int)Math.Round(_pageSize.Height * ViewScale);
                int x = Math.Max(Margin2, (ClientSize.Width - w) / 2);
                int y = Margin2;
                return new Rectangle(x, y, w, h);
            }
        }

        private void UpdateLayoutSize()
        {
            if (_mode != ZoomMode.Custom && ClientSize.Width > 0)
            {
                float unit = (DeviceDpi / 96f) * 96f / _pageDpi;
                float availW = Math.Max(50, (Parent == null ? ClientSize.Width : Width - SystemInformation.VerticalScrollBarWidth) - 2 * Margin2);
                float availH = Math.Max(50, Height - 2 * Margin2);
                float fitW = availW / (_pageSize.Width * unit);
                float fitP = Math.Min(fitW, availH / (_pageSize.Height * unit));
                _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, _mode == ZoomMode.FitWidth ? fitW : fitP));
            }
            var r = PageRectOnCanvas;
            AutoScrollMinSize = new Size(r.Width + 2 * Margin2, r.Height + 2 * Margin2);
            Invalidate();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (_mode != ZoomMode.Custom)
            {
                UpdateLayoutSize();
                ZoomChanged?.Invoke(this, EventArgs.Empty);
            }
            else UpdateLayoutSize();
        }

        public PointF ClientToPage(Point p)
        {
            var r = PageRectOnCanvas;
            var s = AutoScrollPosition;
            return new PointF((p.X - s.X - r.X) / ViewScale, (p.Y - s.Y - r.Y) / ViewScale);
        }

        public RectangleF PageToClient(RectangleF pr)
        {
            var r = PageRectOnCanvas;
            var s = AutoScrollPosition;
            return new RectangleF(pr.X * ViewScale + r.X + s.X, pr.Y * ViewScale + r.Y + s.Y, pr.Width * ViewScale, pr.Height * ViewScale);
        }

        public void CenterOn(PointF pagePoint)
        {
            var r = PageRectOnCanvas;
            int x = (int)(pagePoint.X * ViewScale + r.X - ClientSize.Width / 2f);
            int y = (int)(pagePoint.Y * ViewScale + r.Y - ClientSize.Height / 2f);
            AutoScrollPosition = new Point(Math.Max(0, x), Math.Max(0, y));
            Invalidate();
        }

        /// <summary>Scroll position as a fraction of the scrollable range (for synchronizing views).</summary>
        public PointF ScrollFraction
        {
            get
            {
                var s = AutoScrollPosition;
                int maxX = Math.Max(1, AutoScrollMinSize.Width - ClientSize.Width), maxY = Math.Max(1, AutoScrollMinSize.Height - ClientSize.Height);
                return new PointF(-s.X / (float)maxX, -s.Y / (float)maxY);
            }
            set
            {
                int maxX = Math.Max(0, AutoScrollMinSize.Width - ClientSize.Width), maxY = Math.Max(0, AutoScrollMinSize.Height - ClientSize.Height);
                AutoScrollPosition = new Point((int)(value.X * maxX), (int)(value.Y * maxY));
                Invalidate();
            }
        }

        /// <summary>Scrolls so that the page rectangle is visible.</summary>
        public void EnsureVisible(RectangleF pageRect)
        {
            var c = PageToClient(pageRect);
            if (c.Top >= 0 && c.Bottom <= ClientSize.Height && c.Left >= 0 && c.Right <= ClientSize.Width) return;
            CenterOn(new PointF(pageRect.X + pageRect.Width / 2, pageRect.Y + pageRect.Height / 2));
            ViewScrolled?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
            ViewScrolled?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) != 0)
            {
                Zoom = Zoom * (e.Delta > 0 ? 1.15f : 1 / 1.15f);
                return;
            }
            base.OnMouseWheel(e);
            Invalidate();
            ViewScrolled?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && PanTool))
            {
                _panning = true;
                _panStart = e.Location;
                _scrollStart = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y);
                Cursor = Cursors.SizeAll;
                return;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_panning)
            {
                AutoScrollPosition = new Point(_scrollStart.X - (e.X - _panStart.X), _scrollStart.Y - (e.Y - _panStart.Y));
                Invalidate();
                ViewScrolled?.Invoke(this, EventArgs.Empty);
                return;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_panning)
            {
                _panning = false;
                Cursor = PanTool ? Cursors.Hand : Cursors.Default;
                return;
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            var r = PageRectOnCanvas;
            r.Offset(AutoScrollPosition);
            // Drop shadow.
            using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0))) g.FillRectangle(shadow, r.X + 2, r.Y + 3, r.Width, r.Height);
            var state = g.Save();
            g.SetClip(r);
            g.TranslateTransform(r.X, r.Y);
            g.ScaleTransform(ViewScale, ViewScale);
            DrawPage(g);
            g.Restore(state);
            DrawOverlay(g);
        }

        /// <summary>Draws the page; the graphics is in page-pixel coordinates.</summary>
        protected abstract void DrawPage(Graphics g);

        /// <summary>Screen-space adornments (selection rubber band etc.).</summary>
        protected virtual void DrawOverlay(Graphics g) { }
    }
}
