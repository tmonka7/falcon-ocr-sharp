using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FalconOcr.App.UI
{
    /// <summary>Base for owner-drawn clickable controls with hover/press tracking.</summary>
    internal abstract class HoverControl : Control
    {
        protected bool Hover, Pressed;

        protected HoverControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Cursor = Cursors.Hand;
            Font = Theme.Base;
        }

        protected override void OnMouseEnter(EventArgs e) { Hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { Hover = false; Pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Pressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { Pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

        protected static int Dpi(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96f);
    }

    /// <summary>Toolbar button: icon above a caption (ribbon style).</summary>
    internal sealed class ToolButton : HoverControl
    {
        private IconKind _icon;

        public ToolButton(string text, IconKind icon, Color? iconColor = null)
        {
            Text = text;
            _icon = icon;
            IconColor = iconColor ?? Theme.Text;
            Size = new Size(Dpi(this, 84), Dpi(this, 72));
            BackColor = Color.Transparent;
        }

        public Color IconColor { get; set; }

        public IconKind Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(2, 2, Width - 4, Height - 4);
            if (Enabled && (Hover || Pressed))
                using (var path = Icons.Rounded(r, 6))
                using (var b = new SolidBrush(Pressed ? Theme.AccentLight : Theme.AccentHover))
                    g.FillPath(b, path);
            int icon = Dpi(this, 28);
            var ic = Enabled ? IconColor : Color.FromArgb(170, 176, 184);
            Icons.Draw(g, _icon, new RectangleF((Width - icon) / 2f, Dpi(this, 9), icon, icon), ic);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, Dpi(this, 44), Width, Dpi(this, 22)), Enabled ? Theme.Text : Color.Gray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Sidebar navigation entry.</summary>
    internal sealed class NavButton : HoverControl
    {
        private bool _selected;
        private readonly IconKind _icon;

        public NavButton(string text, IconKind icon)
        {
            Text = text;
            _icon = icon;
            Font = Theme.Nav;
            Height = Dpi(this, 56);
            Dock = DockStyle.Top;
        }

        public bool Selected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_selected || Hover)
            {
                var r = new Rectangle(0, 2, Width - Dpi(this, 6), Height - 4);
                using (var path = RightRounded(r, Dpi(this, 8)))
                using (var b = new SolidBrush(_selected ? Theme.SidebarSelected : Theme.SidebarHover))
                    g.FillPath(b, path);
            }
            int icon = Dpi(this, 26);
            Icons.Draw(g, _icon, new RectangleF(Dpi(this, 22), (Height - icon) / 2f, icon, icon), Color.White, Dpi(this, 2));
            TextRenderer.DrawText(g, Text, Font, new Rectangle(Dpi(this, 62), 0, Width - Dpi(this, 62), Height), Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath RightRounded(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddLine(r.Left, r.Top, r.Right - d, r.Top);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddLine(r.Right - d, r.Bottom, r.Left, r.Bottom);
            p.CloseFigure();
            return p;
        }
    }

    /// <summary>Flat tabs with a green underline on the selected tab.</summary>
    internal sealed class TabStrip : Control
    {
        private readonly List<string> _tabs = new List<string>();
        private int _selected;
        private int _hover = -1;

        public TabStrip(params string[] tabs)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            _tabs.AddRange(tabs);
            Font = Theme.Base;
            Height = (int)(38 * DeviceDpi / 96f);
            BackColor = Theme.Panel;
            Cursor = Cursors.Hand;
        }

        public event EventHandler SelectedIndexChanged;

        /// <summary>Extra width reserved at the right for other controls.</summary>
        public int ReservedRight { get; set; }

        public int SelectedIndex
        {
            get => _selected;
            set
            {
                if (value == _selected) return;
                _selected = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private Rectangle TabRect(int i)
        {
            int w = (Width - ReservedRight) / Math.Max(1, _tabs.Count);
            int max = (int)(150 * DeviceDpi / 96f);
            w = Math.Min(w, max);
            return new Rectangle(i * w, 0, w, Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = -1;
            for (int i = 0; i < _tabs.Count; i++) if (TabRect(i).Contains(e.Location)) h = i;
            if (h != _hover) { _hover = h; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            for (int i = 0; i < _tabs.Count; i++) if (TabRect(i).Contains(e.Location)) SelectedIndex = i;
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var line = new Pen(Theme.Border)) g.DrawLine(line, 0, Height - 1, Width, Height - 1);
            for (int i = 0; i < _tabs.Count; i++)
            {
                var r = TabRect(i);
                bool sel = i == _selected;
                if (i == _hover && !sel) using (var b = new SolidBrush(Theme.AccentHover)) g.FillRectangle(b, r);
                TextRenderer.DrawText(g, _tabs[i], sel ? Theme.Bold : Font, r, sel ? Theme.Accent : Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                if (sel) using (var b = new SolidBrush(Theme.Accent)) g.FillRectangle(b, r.X + 6, Height - 3, r.Width - 12, 3);
            }
        }
    }

    /// <summary>Filled green call-to-action button.</summary>
    internal sealed class AccentButton : HoverControl
    {
        public AccentButton(string text, IconKind icon)
        {
            Text = text;
            Icon = icon;
            Font = new Font("Segoe UI Semibold", 12f);
            Height = Dpi(this, 48);
        }

        public IconKind Icon { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var color = !Enabled ? Color.FromArgb(160, 190, 176) : Pressed ? Theme.AccentDark : Hover ? Color.FromArgb(22, 146, 101) : Theme.Accent;
            using (var path = Icons.Rounded(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Dpi(this, 5)))
            using (var b = new SolidBrush(color))
                g.FillPath(b, path);
            var size = TextRenderer.MeasureText(Text, Font);
            int icon = Icon == IconKind.None ? 0 : Dpi(this, 24);
            int gap = icon > 0 ? Dpi(this, 12) : 0;
            int total = icon + gap + size.Width;
            int x = (Width - total) / 2;
            if (icon > 0) Icons.Draw(g, Icon, new RectangleF(x, (Height - icon) / 2f, icon, icon), Color.White, Dpi(this, 2));
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x + icon + gap, 0, size.Width + 4, Height), Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    /// <summary>Small square icon button, optionally a toggle; draws a text glyph when no icon is set (B / I / U).</summary>
    internal class IconButton : HoverControl
    {
        private bool _checked;

        public IconButton(IconKind icon, string tooltip = null, string glyph = null, FontStyle glyphStyle = FontStyle.Regular)
        {
            Icon = icon;
            Glyph = glyph;
            GlyphFont = new Font("Segoe UI", 11f, glyphStyle);
            Size = new Size(Dpi(this, 30), Dpi(this, 30));
            TooltipText = tooltip;
        }

        public IconKind Icon { get; set; }
        public string Glyph { get; set; }
        public Font GlyphFont { get; set; }
        public string TooltipText { get; }
        public bool Toggle { get; set; }

        public bool Checked
        {
            get => _checked;
            set { _checked = value; Invalidate(); }
        }

        protected override void OnClick(EventArgs e)
        {
            if (Toggle) Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_checked || (Enabled && Hover))
                using (var path = Icons.Rounded(new RectangleF(1, 1, Width - 2, Height - 2), 4))
                using (var b = new SolidBrush(_checked ? Theme.AccentLight : Theme.AccentHover))
                    g.FillPath(b, path);
            var c = !Enabled ? Color.Silver : _checked ? Theme.Accent : Theme.Text;
            if (Glyph != null)
                TextRenderer.DrawText(g, Glyph, GlyphFont, ClientRectangle, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            else
            {
                int s = Math.Min(Width, Height) - Dpi(this, 10);
                Icons.Draw(g, Icon, new RectangleF((Width - s) / 2f, (Height - s) / 2f, s, s), c);
            }
        }
    }

    /// <summary>Radio option with a leading file-type icon (Word / Excel / HTML).</summary>
    internal sealed class IconRadio : HoverControl
    {
        private bool _checked;

        public IconRadio(string text, IconKind icon)
        {
            Text = text;
            Icon = icon;
            Height = Dpi(this, 44);
            Dock = DockStyle.Top;
        }

        public IconKind Icon { get; }
        public object Tag2 { get; set; }
        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (value && Parent != null)
                    foreach (Control c in Parent.Controls)
                        if (c is IconRadio r && r != this) r.Checked = false;
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = true;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Hover) using (var b = new SolidBrush(Theme.AccentHover)) g.FillRectangle(b, ClientRectangle);
            int d = Dpi(this, 18);
            var rc = new Rectangle(Dpi(this, 8), (Height - d) / 2, d, d);
            using (var pen = new Pen(_checked ? Theme.Accent : Color.FromArgb(150, 158, 168), 1.5f)) g.DrawEllipse(pen, rc);
            if (_checked) using (var b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, Rectangle.Inflate(rc, -Dpi(this, 4), -Dpi(this, 4)));
            int icon = Dpi(this, 26);
            Icons.Draw(g, Icon, new RectangleF(Dpi(this, 36), (Height - icon) / 2f, icon, icon), Theme.Text);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(Dpi(this, 74), 0, Width - Dpi(this, 74), Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    /// <summary>White card with a titled, collapsible header (right-hand settings panels).</summary>
    internal sealed class CardPanel : Panel
    {
        private readonly Label _title;
        private readonly IconButton _toggle;
        private bool _collapsed;
        private int _expandedHeight;

        public CardPanel(string title, IconKind icon)
        {
            BackColor = Theme.Panel;
            Padding = new Padding(Scale(14), Scale(48), Scale(14), Scale(12));
            DoubleBuffered = true;
            Header = new Panel { Dock = DockStyle.None, Height = Scale(44), BackColor = Theme.Panel };
            Header.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Icons.Draw(e.Graphics, icon, new RectangleF(Scale(14), Scale(13), Scale(18), Scale(18)), Theme.Accent);
            };
            _title = new Label { Text = title, Font = Theme.Heading, ForeColor = Theme.Text, AutoSize = false, Location = new Point(Scale(40), Scale(10)), Size = new Size(Scale(220), Scale(26)), TextAlign = ContentAlignment.MiddleLeft };
            _toggle = new IconButton(IconKind.ChevronUp) { Size = new Size(Scale(26), Scale(26)) };
            _toggle.Click += (s, e) => Collapsed = !Collapsed;
            Header.Controls.Add(_title);
            Header.Controls.Add(_toggle);
            Controls.Add(Header);
        }

        public Panel Header { get; }

        public bool Collapsed
        {
            get => _collapsed;
            set
            {
                if (_collapsed == value) return;
                if (value) _expandedHeight = Height;
                _collapsed = value;
                _toggle.Icon = value ? IconKind.ChevronDown : IconKind.ChevronUp;
                Height = value ? Header.Height + Scale(4) : Math.Max(_expandedHeight, Header.Height + Scale(40));
                foreach (Control c in Controls) if (c != Header) c.Visible = !value;
                _toggle.Invalidate();
            }
        }

        private int Scale(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (Header == null || _toggle == null) // layout during construction
            {
                base.OnLayout(levent);
                return;
            }
            Header.SetBounds(0, 0, Width, Header.Height);
            _toggle.Location = new Point(Width - _toggle.Width - Scale(12), Scale(9));
            _title.Width = Math.Max(10, Width - Scale(90));
            base.OnLayout(levent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Theme.Border)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }

    /// <summary>Panel drawing a 1px border (optionally only some sides).</summary>
    internal class BorderPanel : Panel
    {
        public BorderPanel()
        {
            DoubleBuffered = true;
            BackColor = Theme.Panel;
        }

        public bool BorderTop { get; set; } = true;
        public bool BorderBottom { get; set; } = true;
        public bool BorderLeft { get; set; } = true;
        public bool BorderRight { get; set; } = true;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Theme.Border))
            {
                if (BorderTop) e.Graphics.DrawLine(pen, 0, 0, Width, 0);
                if (BorderBottom) e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
                if (BorderLeft) e.Graphics.DrawLine(pen, 0, 0, 0, Height);
                if (BorderRight) e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
            }
        }
    }

    /// <summary>Labeled field helper for settings panels.</summary>
    internal static class Field
    {
        public static Label Caption(string text) => new Label
        {
            Text = text,
            AutoSize = false,
            Height = 26,
            Dock = DockStyle.Top,
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.BottomLeft,
            Font = Theme.Base
        };

        public static ComboBox Combo(params object[] items)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, FlatStyle = FlatStyle.System, Font = Theme.Base, IntegralHeight = false };
            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            return c;
        }

        public static Control Spacer(int h) => new Panel { Height = h, Dock = DockStyle.Top, BackColor = Color.Transparent };
    }
}
