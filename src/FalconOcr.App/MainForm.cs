using System;
using FalconOcr.Localization;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FalconOcr.App.Pages;
using FalconOcr.App.Services;
using FalconOcr.App.UI;
using FalconOcr.Export;

namespace FalconOcr.App
{
    /// <summary>Services and navigation shared by all pages.</summary>
    internal interface IShell
    {
        AppSettings Settings { get; }
        HistoryStore History { get; }
        OcrService Ocr { get; }
        void SetStatus(string text, int progressPercent = -1);
        void SetCounters(int totalFiles, int selected, ExportFormat format);
        void Navigate(string page);
        WorkspacePage Workspace { get; }
    }

    internal sealed class MainForm : Form, IShell
    {
        private const int WM_NCHITTEST = 0x84, WM_NCLBUTTONDOWN = 0xA1, HTCAPTION = 2;
        private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private readonly Panel _titleBar;
        private readonly Panel _sidebar;
        private readonly Panel _host;
        private readonly Panel _statusBar;
        private readonly Label _status;
        private readonly Label _counters;
        private readonly ProgressBar _progress;
        private readonly IconButton _maxButton;
        private readonly Dictionary<string, NavButton> _nav = new Dictionary<string, NavButton>();
        private readonly Dictionary<string, Control> _pages = new Dictionary<string, Control>();
        private readonly SidebarToggle _sidebarToggle;
        private readonly ToolTip _navTips = new ToolTip();
        private bool _sidebarCollapsed;

        public AppSettings Settings { get; }
        public HistoryStore History { get; }
        public OcrService Ocr { get; }
        public WorkspacePage Workspace { get; }

        public MainForm(string[] startupFiles)
        {
            Settings = AppSettings.Load();
            History = HistoryStore.Load();
            Ocr = new OcrService();

            Text = "Falcon OCR";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Window;
            Font = Theme.Base;
            MinimumSize = new Size(Scale(1100), Scale(700));
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(Scale(1536), Scale(1000));
            DoubleBuffered = true;
            Icon = MakeIcon();
            KeyPreview = true;
            Padding = new Padding(1);

            // ---- title bar
            _titleBar = new Panel { Dock = DockStyle.Top, Height = Scale(52), BackColor = Theme.Header };
            _titleBar.Paint += PaintTitle;
            _titleBar.MouseDown += DragWindow;
            _titleBar.DoubleClick += (s, e) => ToggleMaximize();
            var close = WindowButton(IconKind.Close, (s, e) => Close());
            _maxButton = WindowButton(IconKind.Maximize, (s, e) => ToggleMaximize());
            var min = WindowButton(IconKind.Minimize, (s, e) => WindowState = FormWindowState.Minimized);
            if (Program.IsTrial)
            {
                var badge = new TrialBadge(Program.Trial) { Dock = DockStyle.Right };
                badge.Click += (s, e) => ShowActivation();
                _titleBar.Controls.Add(badge);
            }
            _titleBar.Controls.Add(min);
            _titleBar.Controls.Add(_maxButton);
            _titleBar.Controls.Add(close);

            // ---- sidebar
            _sidebar = new Panel { Dock = DockStyle.Left, Width = Scale(184), BackColor = Theme.SidebarTop };
            _sidebar.Paint += PaintSidebar;
            var items = new[]
            {
                ("home", L.T("Home"), IconKind.Home),
                ("ocr", L.T("OCR"), IconKind.Ocr),
                ("batch", L.T("Batch Process"), IconKind.Batch),
                ("history", L.T("History"), IconKind.History),
                ("settings", L.T("Settings"), IconKind.Settings)
            };
            foreach (var it in items.Reverse())
            {
                var b = new NavButton(it.Item2, it.Item3) { BackColor = Color.Transparent };
                string key = it.Item1;
                b.Click += (s, e) => Navigate(key);
                _nav[key] = b;
                _sidebar.Controls.Add(b);
            }
            _sidebar.Controls.Add(new Panel { Dock = DockStyle.Top, Height = Scale(10), BackColor = Color.Transparent });
            _sidebar.Controls[_sidebar.Controls.Count - 1].SendToBack();
            // Collapse / expand button at the bottom of the sidebar.
            _sidebarToggle = new SidebarToggle { Dock = DockStyle.Bottom, Height = Scale(52) };
            _sidebarToggle.Click += (s, e) => SetSidebarCollapsed(!_sidebarCollapsed);
            _sidebar.Controls.Add(_sidebarToggle);

            // ---- status bar
            _statusBar = new BorderPanel { Dock = DockStyle.Bottom, Height = Scale(36), BorderLeft = false, BorderRight = false, BorderBottom = false, Padding = new Padding(0, 1, 0, 0) };
            _status = new Label { Text = L.T("Ready"), Dock = DockStyle.Left, Width = Scale(520), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(Scale(16), 0, 0, 0), ForeColor = Theme.Text };
            _progress = new ProgressBar { Dock = DockStyle.Left, Width = Scale(180), Visible = false, Style = ProgressBarStyle.Continuous };
            var progressHost = new Panel { Dock = DockStyle.Left, Width = Scale(200), Padding = new Padding(Scale(8), Scale(11), Scale(12), Scale(11)) };
            progressHost.Controls.Add(_progress);
            _progress.Dock = DockStyle.Fill;
            _counters = new Label { Dock = DockStyle.Right, Width = Scale(420), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, Scale(16), 0), ForeColor = Theme.SubText, Font = Theme.Small };
            _statusBar.Controls.Add(progressHost);
            _statusBar.Controls.Add(_status);
            _statusBar.Controls.Add(_counters);

            // ---- pages
            _host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Window };
            Workspace = new WorkspacePage(this) { Dock = DockStyle.Fill };
            _pages["home"] = Workspace;
            _pages["ocr"] = new QuickOcrPage(this) { Dock = DockStyle.Fill };
            _pages["batch"] = new BatchPage(this) { Dock = DockStyle.Fill };
            _pages["history"] = new HistoryPage(this) { Dock = DockStyle.Fill };
            _pages["settings"] = new SettingsPage(this) { Dock = DockStyle.Fill };
            foreach (var p in _pages.Values)
            {
                p.Visible = false;
                _host.Controls.Add(p);
            }

            var right = new Panel { Dock = DockStyle.Fill };
            right.Controls.Add(_host);
            right.Controls.Add(_statusBar);
            Controls.Add(right);
            Controls.Add(_sidebar);
            Controls.Add(_titleBar);

            Navigate("home");
            SetCounters(0, 0, Settings.Format);
            SetSidebarCollapsed(Settings.SidebarCollapsed, save: false);

            if (!Settings.Bounds.IsEmpty && SystemInformation.VirtualScreen.IntersectsWith(Settings.Bounds))
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = Settings.Bounds;
            }
            UpdateMaximizedBounds();
            if (Settings.Maximized) WindowState = FormWindowState.Maximized;

            Shown += (s, e) =>
            {
                var missing = OcrService.MissingModels();
                if (missing.Count > 0)
                {
                    MessageBox.Show(this, L.F("Some OCR model files are missing from the installation:\n\n{0}\n\nRun tools\\fetch-dependencies.ps1 on a connected machine and rebuild.", string.Join("\n", missing)),
                        "Falcon OCR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else Ocr.WarmupAsync(Settings.Ocr);
                if (startupFiles != null && startupFiles.Length > 0) Workspace.AddPaths(startupFiles);
                var snapshotDir = Environment.GetEnvironmentVariable("FALCON_SNAPSHOT");
                if (!string.IsNullOrEmpty(snapshotDir)) RunSnapshotTour(snapshotDir);
            };
        }

        /// <summary>
        /// Diagnostics for headless environments (set FALCON_SNAPSHOT=&lt;dir&gt;): recognizes the loaded document,
        /// renders every page of the UI to PNG files and exits.
        /// </summary>
        private async void RunSnapshotTour(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            void Snap(string name)
            {
                Refresh();
                using (var bmp = new Bitmap(Width, Height))
                {
                    DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                    bmp.Save(System.IO.Path.Combine(dir, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            await System.Threading.Tasks.Task.Delay(1500);
            Snap("1-loaded");
            await Workspace.SnapshotRecognizeAsync();
            await System.Threading.Tasks.Task.Delay(800);
            Snap("2-recognized");
            Workspace.SnapshotSelect(12, 0);
            await System.Threading.Tasks.Task.Delay(300);
            Snap("2b-selected");
            Workspace.SnapshotSelect(12, 1);
            await System.Threading.Tasks.Task.Delay(300);
            Snap("2c-overlay");
            Workspace.SnapshotSelect(12, 0);
            foreach (var p in new[] { "ocr", "batch", "history", "settings" })
            {
                Navigate(p);
                await System.Threading.Tasks.Task.Delay(400);
                Snap("3-" + p);
            }
            Navigate("home");
            Close();
        }

        private int Scale(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        /// <summary>Collapsed sidebar shows icons only (names as tooltips); the state is remembered.</summary>
        private void SetSidebarCollapsed(bool collapsed, bool save = true)
        {
            _sidebarCollapsed = collapsed;
            _sidebar.Width = Scale(collapsed ? 64 : 184);
            foreach (var n in _nav.Values)
            {
                n.Compact = collapsed;
                _navTips.SetToolTip(n, collapsed ? n.Text : null);
            }
            _sidebarToggle.Collapsed = collapsed;
            _navTips.SetToolTip(_sidebarToggle, collapsed ? L.T("Expand sidebar") : L.T("Collapse sidebar"));
            if (save)
            {
                Settings.SidebarCollapsed = collapsed;
                Settings.Save();
            }
        }

        /// <summary>« Collapse / » button at the bottom of the sidebar.</summary>
        private sealed class SidebarToggle : Control
        {
            private bool _hover, _collapsed;

            public SidebarToggle()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
                BackColor = Color.Transparent;
                Cursor = Cursors.Hand;
                Font = Theme.Base;
            }

            public bool Collapsed
            {
                get => _collapsed;
                set { _collapsed = value; Invalidate(); }
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float k = DeviceDpi / 96f;
                using (var p = new Pen(Color.FromArgb(60, 255, 255, 255))) g.DrawLine(p, 10 * k, 0, Width - 10 * k, 0);
                var box = new RectangleF(10 * k, 8 * k, Width - 20 * k, Height - 16 * k);
                if (_hover) using (var path = Icons.Rounded(box, 8 * k)) using (var b = new SolidBrush(Theme.SidebarHover)) g.FillPath(b, path);
                float s = 20 * k;
                var icon = _collapsed ? IconKind.ChevronRight : IconKind.ChevronLeft;
                if (_collapsed)
                {
                    Icons.Draw(g, icon, new RectangleF((Width - s) / 2, (Height - s) / 2, s, s), Color.White, 2 * k);
                    return;
                }
                Icons.Draw(g, icon, new RectangleF(22 * k, (Height - s) / 2, s, s), Color.White, 2 * k);
                TextRenderer.DrawText(g, L.T("Collapse"), Font, new Rectangle((int)(62 * k), 0, Width - (int)(62 * k), Height), Color.FromArgb(225, 245, 236), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }

        /// <summary>Opens the activation window (trial badge, Settings); removes the badge once activated.</summary>
        public void ShowActivation()
        {
            using (var dlg = new ActivationForm(Program.License, startup: false, trial: Program.Trial))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                Program.License = dlg.Result;
                Program.Trial = null;
            }
            foreach (var b in _titleBar.Controls.OfType<TrialBadge>().ToList()) _titleBar.Controls.Remove(b);
            Workspace.HideTrialBanner();
            SetStatus(L.T("Activated — ") + Program.License.Message);
        }

        /// <summary>Amber "TRIAL · n days left" pill in the title bar; click to activate.</summary>
        private sealed class TrialBadge : Control
        {
            private bool _hover;

            public TrialBadge(Licensing.TrialStatus trial)
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
                Cursor = Cursors.Hand;
                Font = Theme.Bold;
                Text = trial.DaysLeft == 1 ? L.T("TRIAL · 1 day left — Activate now") : L.F("TRIAL · {0} days left — Activate now", trial.DaysLeft);
                Width = TextRenderer.MeasureText(Text, Font).Width + (int)(40 * DeviceDpi / 96f);
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int h = (int)(30 * DeviceDpi / 96f);
                var r = new RectangleF(6, (Height - h) / 2f, Width - 16, h);
                using (var path = Icons.Rounded(r, h / 2f))
                using (var b = new SolidBrush(_hover ? Color.FromArgb(255, 214, 102) : Color.FromArgb(255, 193, 7)))
                    g.FillPath(b, path);
                TextRenderer.DrawText(g, Text, Font, Rectangle.Round(r), Color.FromArgb(60, 40, 0), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        // ------------------------------------------------------------ IShell

        public void SetStatus(string text, int progressPercent = -1)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetStatus(text, progressPercent))); return; }
            _status.Text = text;
            _progress.Visible = progressPercent >= 0;
            if (progressPercent >= 0) _progress.Value = Math.Max(0, Math.Min(100, progressPercent));
        }

        public void SetCounters(int totalFiles, int selected, ExportFormat format)
        {
            _counters.Text = L.F("Total Files: {0}     |     Selected: {1}     |     Output: {2}", totalFiles, selected, format.ToString().ToUpperInvariant());
        }

        public void Navigate(string page)
        {
            foreach (var kv in _nav) kv.Value.Selected = kv.Key == page;
            foreach (var kv in _pages) kv.Value.Visible = kv.Key == page;
            _pages[page].BringToFront();
            (_pages[page] as IActivatable)?.OnActivated();
        }

        // ------------------------------------------------------------ window chrome

        private IconButton WindowButton(IconKind icon, EventHandler click) => new WhiteIconButton(icon, click, icon == IconKind.Close);

        /// <summary>Caption button drawn white on the green header.</summary>
        private sealed class WhiteIconButton : IconButton
        {
            private readonly bool _danger;

            public WhiteIconButton(IconKind icon, EventHandler click, bool danger) : base(icon)
            {
                _danger = danger;
                Dock = DockStyle.Right;
                Width = (int)(52 * DeviceDpi / 96f);
                Click += click;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (Hover) using (var b = new SolidBrush(_danger ? Color.FromArgb(196, 43, 28) : Color.FromArgb(40, 255, 255, 255))) e.Graphics.FillRectangle(b, ClientRectangle);
                int s = (int)(18 * DeviceDpi / 96f);
                Icons.Draw(e.Graphics, Icon, new RectangleF((Width - s) / 2f, (Height - s) / 2f, s, s), Color.White, 1.3f);
            }
        }

        private void PaintTitle(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int logo = Scale(34);
            Icons.Draw(g, IconKind.Logo, new RectangleF(Scale(18), (_titleBar.Height - logo) / 2f, logo, logo), Color.FromArgb(58, 190, 140));
            using (var f = Theme.Semibold(17f))
                TextRenderer.DrawText(g, "Falcon OCR", f, new Point(Scale(62), Scale(9)), Color.White);
            TextRenderer.DrawText(g, L.T("Convert Scans and Images into Editable Documents"), Theme.Base, new Rectangle(Scale(215), 0, Scale(520), _titleBar.Height), Color.FromArgb(225, 245, 236), TextFormatFlags.VerticalCenter);
        }

        private void PaintSidebar(object sender, PaintEventArgs e)
        {
            var r = _sidebar.ClientRectangle;
            if (r.Height <= 0) return;
            using (var b = new LinearGradientBrush(r, Theme.SidebarTop, Theme.SidebarBottom, LinearGradientMode.Vertical)) e.Graphics.FillRectangle(b, r);
            // Decorative curve at the bottom, as in the reference design.
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Color.FromArgb(22, 255, 255, 255)))
                e.Graphics.FillEllipse(b, -r.Width * 0.6f, r.Height - r.Width * 0.9f, r.Width * 1.9f, r.Width * 1.6f);
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.Clicks > 1) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        /// <summary>A borderless window would cover the taskbar when maximized: limit it to the monitor's work area.</summary>
        private void UpdateMaximizedBounds()
        {
            if (WindowState == FormWindowState.Maximized) return;
            var scr = IsHandleCreated ? Screen.FromHandle(Handle) : Screen.FromRectangle(Bounds);
            MaximizedBounds = new Rectangle(scr.WorkingArea.X - scr.Bounds.X, scr.WorkingArea.Y - scr.Bounds.Y, scr.WorkingArea.Width, scr.WorkingArea.Height);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateMaximizedBounds();
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            UpdateMaximizedBounds();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_maxButton != null)
            {
                foreach (Control c in _titleBar.Controls)
                    if (c is WhiteIconButton w && (w.Icon == IconKind.Maximize || w.Icon == IconKind.Restore))
                    {
                        w.Icon = WindowState == FormWindowState.Maximized ? IconKind.Restore : IconKind.Maximize;
                        w.Invalidate();
                    }
            }
            Padding = WindowState == FormWindowState.Maximized ? Padding.Empty : new Padding(1);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Theme.AccentDark)) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x00020000;      // WS_MINIMIZEBOX: minimize/restore from the taskbar
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                // Resize handles along the 6px border of the borderless window.
                var p = PointToClient(Cursor.Position);
                int g = Scale(6);
                bool l = p.X < g, r = p.X >= Width - g, t = p.Y < g, b = p.Y >= Height - g;
                if (t && l) m.Result = (IntPtr)HTTOPLEFT;
                else if (t && r) m.Result = (IntPtr)HTTOPRIGHT;
                else if (b && l) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (b && r) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (l) m.Result = (IntPtr)HTLEFT;
                else if (r) m.Result = (IntPtr)HTRIGHT;
                else if (t) m.Result = (IntPtr)HTTOP;
                else if (b) m.Result = (IntPtr)HTBOTTOM;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!Workspace.ConfirmClose())
            {
                e.Cancel = true;
                return;
            }
            Settings.Maximized = WindowState == FormWindowState.Maximized;
            Settings.Bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            Settings.Save();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Workspace.DisposeDocuments();
            Ocr.Dispose();
            base.OnFormClosed(e);
        }

        private static Icon MakeIcon()
        {
            using (var bmp = new Bitmap(64, 64))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Icons.Draw(g, IconKind.Logo, new RectangleF(0, 0, 64, 64), Theme.Accent);
                }
                return System.Drawing.Icon.FromHandle(bmp.GetHicon());
            }
        }
    }

    /// <summary>Pages that refresh when navigated to.</summary>
    internal interface IActivatable
    {
        void OnActivated();
    }
}
