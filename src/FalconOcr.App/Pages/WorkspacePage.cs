using System;
using FalconOcr.Localization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FalconOcr.App.Services;
using FalconOcr.App.UI;
using FalconOcr.Engine;
using FalconOcr.Export;
using FalconOcr.Input;
using FalconOcr.Model;
using FalconOcr.Processing;

namespace FalconOcr.App.Pages
{
    /// <summary>
    /// Main workspace (Home): documents on the left, the source page and its recognized reconstruction side by
    /// side (synchronised zoom/scroll/selection), OCR and output settings on the right.
    /// </summary>
    internal sealed class WorkspacePage : UserControl, IActivatable
    {
        private readonly IShell _shell;
        private readonly List<OcrDocument> _docs = new List<OcrDocument>();

        private readonly TabStrip _leftTabs;
        private readonly ListBox _fileList;
        private readonly ListView _thumbs;
        private readonly ImageList _thumbImages;
        private readonly ImageViewer _viewer;
        private readonly ImageViewer _overlay;
        private readonly LayoutView _layout;
        private readonly TabStrip _resultTabs;
        private readonly ComboBox _zoomCombo;
        private readonly Label _pageLabel;
        private readonly IconButton _handButton;
        private readonly Label _resultStatus;
        private readonly Label _resultPage;
        private readonly Label _imageInfo;
        private readonly Panel _resultStatusIcon;
        private readonly ToolButton _recognizeButton;
        private readonly ComboBox _styleCombo;
        private readonly ComboBox _fontCombo, _sizeCombo;
        private Panel _trialBanner;
        private Control _rightColumn;
        private Panel _leftPanel, _leftBody;
        private PanelToggle _filesToggle;
        private bool _filesCollapsed;
        private readonly ToolTip _panelTips = new ToolTip();
        private readonly IconButton _boldButton, _italicButton, _underlineButton, _bulletButton, _numberButton;

        private ComboBox _docType, _language, _layoutMode;
        private CheckBox _detectTables;
        private TextBox _outputFolder;
        private readonly Dictionary<ExportFormat, IconRadio> _formatRadios = new Dictionary<ExportFormat, IconRadio>();

        private int _pageIndex;
        private int _loadToken;
        private int _thumbToken;
        private bool _syncing;
        private bool _loadingSettings;
        private CancellationTokenSource _cts;

        public WorkspacePage(IShell shell)
        {
            _shell = shell;
            BackColor = Theme.Window;
            Font = Theme.Base;
            AllowDrop = true;
            DoubleBuffered = true;

            // ---------------------------------------------------------------- toolbar
            var toolbar = new BorderPanel { Dock = DockStyle.Top, Height = S(92), BorderTop = false, BorderLeft = false, BorderRight = false };
            var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(S(16), S(8), 0, 0), WrapContents = false, BackColor = Color.Transparent };
            var addButton = new ToolButton(L.T("Add Files"), IconKind.AddCircle, Theme.Accent);
            var scanButton = new ToolButton(L.T("Scan"), IconKind.Scanner);
            var clipButton = new ToolButton(L.T("From Clipboard"), IconKind.Clipboard) { Width = S(116) };
            var rotateButton = new ToolButton(L.T("Rotate"), IconKind.Rotate);
            var cropButton = new ToolButton(L.T("Crop"), IconKind.Crop);
            var deleteButton = new ToolButton(L.T("Delete"), IconKind.Trash);
            _recognizeButton = new ToolButton(L.T("Recognize"), IconKind.Play, Theme.Accent) { Width = S(96) };
            var exportButton = new ToolButton(L.T("Export"), IconKind.Export, Theme.Accent);
            tools.Controls.AddRange(new Control[] { addButton, scanButton, clipButton, Separator(), rotateButton, cropButton, deleteButton, Separator(), _recognizeButton, Separator(), exportButton });

            var topRight = new FlowLayoutPanel { Dock = DockStyle.Right, Width = S(250), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, S(18), S(16), 0), BackColor = Color.Transparent };
            var helpLink = LinkButton(L.T("Help"), IconKind.Help);
            var settingsLink = LinkButton(L.T("Settings"), IconKind.Settings);
            topRight.Controls.Add(helpLink);
            topRight.Controls.Add(settingsLink);
            toolbar.Controls.Add(tools);
            toolbar.Controls.Add(topRight);

            var addMenu = new ContextMenuStrip();
            addMenu.Items.Add(L.T("Add files (PDF, images)…\tCtrl+O"), null, (s, e) => AddFilesDialog());
            addMenu.Items.Add(L.T("Add images as one document (image sequence)…"), null, (s, e) => AddSequenceDialog());
            addMenu.Items.Add(L.T("Add folder as image sequence…"), null, (s, e) => AddFolderDialog());
            addButton.Click += (s, e) => addMenu.Show(addButton, new Point(0, addButton.Height));
            scanButton.Click += (s, e) => Scan();
            clipButton.Click += (s, e) => PasteFromClipboard();
            rotateButton.Click += (s, e) => RotateCurrent();
            cropButton.Click += (s, e) => ToggleCrop();
            deleteButton.Click += (s, e) => DeleteCurrent();
            _recognizeButton.Click += async (s, e) => await RecognizeCurrentDocumentAsync();
            exportButton.Click += async (s, e) => await ExportCurrentAsync();
            settingsLink.Click += (s, e) => _shell.Navigate("settings");
            helpLink.Click += (s, e) => ShowHelp();

            // ---------------------------------------------------------------- left: files / thumbnails
            var left = new BorderPanel { Dock = DockStyle.Left, Width = S(214), BorderTop = false, BorderLeft = false };
            _leftTabs = new TabStrip(L.T("Files"), L.T("Thumbnails")) { Dock = DockStyle.Top };
            _fileList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = S(64),
                IntegralHeight = false,
                AllowDrop = true
            };
            _fileList.DrawItem += DrawFileItem;
            _fileList.SelectedIndexChanged += (s, e) => { _pageIndex = 0; ShowCurrent(); };
            _fileList.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                int i = _fileList.IndexFromPoint(e.Location);
                if (i >= 0) _fileList.SelectedIndex = i;
            };
            _fileList.ContextMenuStrip = FileMenu();
            _thumbImages = new ImageList { ImageSize = new Size(S(96), S(128)), ColorDepth = ColorDepth.Depth32Bit };
            _thumbs = new ListView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                View = View.LargeIcon,
                LargeImageList = _thumbImages,
                MultiSelect = false,
                Visible = false,
                HideSelection = false
            };
            _thumbs.SelectedIndexChanged += (s, e) =>
            {
                if (_thumbs.SelectedIndices.Count == 0) return;
                _pageIndex = _thumbs.SelectedIndices[0];
                ShowPage();
            };
            _leftTabs.SelectedIndexChanged += (s, e) =>
            {
                _fileList.Visible = _leftTabs.SelectedIndex == 0;
                _thumbs.Visible = _leftTabs.SelectedIndex == 1;
                if (_thumbs.Visible) BuildThumbnails();
            };
            var leftBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(S(6)) };
            leftBody.Controls.Add(_fileList);
            leftBody.Controls.Add(_thumbs);
            left.Controls.Add(leftBody);
            left.Controls.Add(_leftTabs);
            // Collapse / expand button at the bottom of the Files / Thumbnails panel.
            _leftPanel = left;
            _leftBody = leftBody;
            _filesToggle = new PanelToggle { Dock = DockStyle.Bottom, Height = S(40) };
            _filesToggle.Click += (s, e) => SetFilesPanelCollapsed(!_filesCollapsed);
            left.Controls.Add(_filesToggle);

            // ---------------------------------------------------------------- right: settings
            var right = BuildSettingsColumn();

            // ---------------------------------------------------------------- center: viewer | results
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = S(8), BackColor = Theme.Window, FixedPanel = FixedPanel.None };

            // viewer
            var viewerHost = new BorderPanel { Dock = DockStyle.Fill };
            var viewerBar = new Panel { Dock = DockStyle.Top, Height = S(46), Padding = new Padding(S(6), S(7), S(6), S(7)) };
            _viewer = new ImageViewer { Dock = DockStyle.Fill, AllowDrop = true };
            var zoomOut = new IconButton(IconKind.ZoomOut, L.T("Zoom out"));
            var zoomIn = new IconButton(IconKind.ZoomIn, L.T("Zoom in"));
            _zoomCombo = new ComboBox { Width = S(78), DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.Base };
            _zoomCombo.Items.AddRange(new object[] { L.T("Fit width"), L.T("Fit page"), "50%", "75%", "100%", "125%", "150%", "200%", "300%" });
            var fitWidth = new IconButton(IconKind.FitWidth, L.T("Fit width"));
            var fitPage = new IconButton(IconKind.FitPage, L.T("Fit page"));
            var rotateSmall = new IconButton(IconKind.Rotate, L.T("Rotate page 90°"));
            _handButton = new IconButton(IconKind.Hand, L.T("Pan (hand tool)")) { Toggle = true };
            var prev = new IconButton(IconKind.ChevronLeft, L.T("Previous page (PgUp)"));
            var next = new IconButton(IconKind.ChevronRight, L.T("Next page (PgDn)"));
            _pageLabel = new Label { AutoSize = false, Width = S(70), Height = S(30), TextAlign = ContentAlignment.MiddleCenter, Text = "0 / 0", BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            var viewerFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            viewerFlow.Controls.AddRange(new Control[] { zoomOut, zoomIn, Pad(_zoomCombo, 2), fitWidth, fitPage, Separator(30), rotateSmall, _handButton, Separator(30), prev, Pad(_pageLabel, 0), next });
            viewerBar.Controls.Add(viewerFlow);
            // Caption + footer mirror the results panel (tabs / status) so both canvases get identical
            // geometry and every page pixel appears at the same screen position on both sides.
            var viewerCaption = new TabStrip(L.T("Source Page")) { Dock = DockStyle.Top };
            var viewerFooter = new BorderPanel { Dock = DockStyle.Bottom, Height = S(40), BorderLeft = false, BorderRight = false, BorderBottom = false };
            _imageInfo = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.SubText, Padding = new Padding(S(12), 0, 0, 0) };
            viewerFooter.Controls.Add(_imageInfo);
            viewerHost.Controls.Add(_viewer);
            viewerHost.Controls.Add(viewerFooter);
            viewerHost.Controls.Add(viewerBar);
            viewerHost.Controls.Add(viewerCaption);
            split.Panel1.Controls.Add(viewerHost);

            var tips = new ToolTip();
            foreach (var b in new[] { zoomOut, zoomIn, fitWidth, fitPage, rotateSmall, _handButton, prev, next }) tips.SetToolTip(b, b.TooltipText);
            zoomOut.Click += (s, e) => _viewer.Zoom = _viewer.Zoom / 1.2f;
            zoomIn.Click += (s, e) => _viewer.Zoom = _viewer.Zoom * 1.2f;
            fitWidth.Click += (s, e) => _viewer.ZoomMode = ZoomMode.FitWidth;
            fitPage.Click += (s, e) => _viewer.ZoomMode = ZoomMode.FitPage;
            rotateSmall.Click += (s, e) => RotateCurrent();
            _handButton.Click += (s, e) =>
            {
                foreach (var c in Canvases()) { c.PanTool = _handButton.Checked; c.Cursor = _handButton.Checked ? Cursors.Hand : Cursors.Default; }
            };
            prev.Click += (s, e) => GoToPage(_pageIndex - 1);
            next.Click += (s, e) => GoToPage(_pageIndex + 1);
            _zoomCombo.SelectionChangeCommitted += (s, e) => ApplyZoomText(_zoomCombo.SelectedItem as string);
            _zoomCombo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { ApplyZoomText(_zoomCombo.Text); e.SuppressKeyPress = true; } };

            // results
            var resultHost = new BorderPanel { Dock = DockStyle.Fill };
            _resultTabs = new TabStrip(L.T("Recognized Text"), L.T("Original Image")) { Dock = DockStyle.Top, ReservedRight = S(20) };
            var formatBar = new Panel { Dock = DockStyle.Top, Height = S(46), Padding = new Padding(S(8), S(7), S(6), S(7)) };
            _styleCombo = Field.Combo(L.T("Normal"), L.T("Heading 1"), L.T("Heading 2"), L.T("Heading 3"), L.T("List item"));
            _styleCombo.Dock = DockStyle.None;
            _styleCombo.Width = S(100);
            // Font family / size of the selected paragraph (typed or picked; applied on selection or Enter).
            _fontCombo = new ComboBox { Width = S(136), DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.Base, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, MaxDropDownItems = 20 };
            _fontCombo.Items.AddRange(FontFamilies());
            _sizeCombo = new ComboBox { Width = S(54), DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.Base, MaxDropDownItems = 16 };
            _sizeCombo.Items.AddRange(new object[] { "8", "9", "10", "10.5", "11", "12", "14", "16", "18", "20", "22", "24", "28", "32", "36", "48", "72" });
            tips.SetToolTip(_fontCombo, L.T("Font"));
            tips.SetToolTip(_sizeCombo, L.T("Font size"));
            _boldButton = new IconButton(IconKind.None, L.T("Bold"), "B", FontStyle.Bold);
            _italicButton = new IconButton(IconKind.None, L.T("Italic"), "I", FontStyle.Italic);
            _underlineButton = new IconButton(IconKind.None, L.T("Underline"), "U", FontStyle.Underline);
            _bulletButton = new IconButton(IconKind.List, L.T("Bulleted list"));
            _numberButton = new IconButton(IconKind.NumberedList, L.T("Numbered list"));
            var more = new IconButton(IconKind.More, L.T("More"));
            var formatFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            formatFlow.Controls.AddRange(new Control[] { Pad(_styleCombo, 2), Pad(_fontCombo, 2), Pad(_sizeCombo, 2), Separator(30), _boldButton, _italicButton, _underlineButton, Separator(30), _bulletButton, _numberButton, Separator(30), more });
            formatBar.Controls.Add(formatFlow);
            foreach (var b in new[] { _boldButton, _italicButton, _underlineButton, _bulletButton, _numberButton, more }) tips.SetToolTip(b, b.TooltipText);

            _layout = new LayoutView { Dock = DockStyle.Fill, ShowConfidence = _shell.Settings.ShowConfidence };
            _overlay = new ImageViewer { Dock = DockStyle.Fill, ShowOverlay = true, Visible = false, Placeholder = "" };
            var resultStatus = new BorderPanel { Dock = DockStyle.Bottom, Height = S(40), BorderLeft = false, BorderRight = false, BorderBottom = false };
            _resultStatusIcon = new Panel { Dock = DockStyle.Left, Width = S(40), Visible = false };
            _resultStatusIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int d = S(20);
                var r = new Rectangle(S(12), (resultStatus.Height - d) / 2, d, d);
                using (var b = new SolidBrush(Theme.Accent)) e.Graphics.FillEllipse(b, r);
                Icons.Draw(e.Graphics, IconKind.Check, RectangleF.Inflate(r, -S(4), -S(4)), Color.White, 2);
            };
            _resultStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.SubText, Padding = new Padding(S(8), 0, 0, 0) };
            _resultPage = new Label { Dock = DockStyle.Right, Width = S(80), TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.SubText, Padding = new Padding(0, 0, S(12), 0) };
            resultStatus.Controls.Add(_resultStatus);
            resultStatus.Controls.Add(_resultStatusIcon);
            resultStatus.Controls.Add(_resultPage);
            var resultBody = new Panel { Dock = DockStyle.Fill };
            resultBody.Controls.Add(_layout);
            resultBody.Controls.Add(_overlay);
            resultHost.Controls.Add(resultBody);
            resultHost.Controls.Add(resultStatus);
            resultHost.Controls.Add(formatBar);
            resultHost.Controls.Add(_resultTabs);
            split.Panel2.Controls.Add(resultHost);
            _resultTabs.SelectedIndexChanged += (s, e) =>
            {
                _layout.Visible = _resultTabs.SelectedIndex == 0;
                _overlay.Visible = _resultTabs.SelectedIndex == 1;
                formatBar.Enabled = _resultTabs.SelectedIndex == 0;
                SyncFrom(_viewer);
            };

            // formatting actions
            _styleCombo.SelectionChangeCommitted += (s, e) => ApplyBlockStyle(_styleCombo.SelectedIndex);
            _boldButton.Click += (s, e) => ToggleLineStyle(st => st.Bold = !st.Bold);
            _italicButton.Click += (s, e) => ToggleLineStyle(st => st.Italic = !st.Italic);
            _underlineButton.Click += (s, e) => ToggleLineStyle(st => st.Underline = !st.Underline);
            _fontCombo.SelectionChangeCommitted += (s, e) => ApplyFont(_fontCombo.SelectedItem as string, null);
            _fontCombo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ApplyFont(_fontCombo.Text.Trim(), null); } };
            _sizeCombo.SelectionChangeCommitted += (s, e) => ApplyFontSize(_sizeCombo.SelectedItem as string);
            _sizeCombo.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ApplyFontSize(_sizeCombo.Text); } };
            _bulletButton.Click += (s, e) => ApplyBlockStyle(4);
            _numberButton.Click += (s, e) => ApplyBlockStyle(5);
            var moreMenu = new ContextMenuStrip();
            var confItem = new ToolStripMenuItem(L.T("Highlight uncertain characters")) { Checked = _layout.ShowConfidence, CheckOnClick = true };
            confItem.CheckedChanged += (s, e) => { _layout.ShowConfidence = confItem.Checked; _shell.Settings.ShowConfidence = confItem.Checked; _layout.Invalidate(); };
            moreMenu.Items.Add(confItem);
            moreMenu.Items.Add(new ToolStripSeparator());
            moreMenu.Items.Add(L.T("Copy page text"), null, (s, e) => CopyText(false));
            moreMenu.Items.Add(L.T("Copy document text"), null, (s, e) => CopyText(true));
            moreMenu.Items.Add(new ToolStripSeparator());
            moreMenu.Items.Add(L.T("Recognize this page again"), null, async (s, e) => { if (CurrentPage != null) await RecognizePagesAsync(new[] { CurrentPage }); });
            more.Click += (s, e) => moreMenu.Show(more, new Point(0, more.Height));

            // sync + selection
            foreach (var c in Canvases())
            {
                var src = c;
                c.ZoomChanged += (s, e) => SyncFrom(src);
                c.ViewScrolled += (s, e) => SyncFrom(src);
            }
            _layout.LineSelected += (s, line) => SelectLine(line, _layout);
            _viewer.LineClicked += (s, line) => SelectLine(line, _viewer);
            _overlay.LineClicked += (s, line) => SelectLine(line, _overlay);
            _layout.LineEdited += (s, line) => { _shell.SetStatus(L.T("Edited: ") + line.Text); SelectLine(line, _layout); };
            _viewer.CropSelected += (s, r) => ApplyCrop(r);

            // ---------------------------------------------------------------- compose
            var center = new Panel { Dock = DockStyle.Fill, Padding = new Padding(S(8), S(8), S(8), S(8)) };
            center.Controls.Add(split);

            // The right settings column can be resized by dragging its left edge; the width is remembered.
            _rightColumn = right;
            if (Settings.RightPanelWidth > 0) right.Width = Math.Max(S(280), Math.Min(S(640), S(Settings.RightPanelWidth)));
            var rightSplitter = new GripSplitter { Dock = DockStyle.Right, Width = S(6), MinSize = S(280), MinExtra = S(560) };
            rightSplitter.SplitterMoved += (s, e) =>
            {
                Settings.RightPanelWidth = (int)Math.Round(right.Width * 96f / DeviceDpi);
                Settings.Save();
            };

            // Docking order: the last added control is docked first (outermost).
            Controls.Add(center);
            Controls.Add(rightSplitter);
            Controls.Add(right);
            Controls.Add(left);
            if (Program.IsTrial)
            {
                _trialBanner = BuildTrialBanner();
                Controls.Add(_trialBanner);
            }
            Controls.Add(toolbar);
            SetFilesPanelCollapsed(Settings.FilesPanelCollapsed, save: false);

            foreach (Control c in new Control[] { this, _fileList, _viewer, _layout, _thumbs })
            {
                c.AllowDrop = true;
                c.DragEnter += OnDragEnter;
                c.DragDrop += OnDragDrop;
            }

            Load += (s, e) =>
            {
                split.SplitterDistance = Math.Max(S(200), (split.Width - split.SplitterWidth) / 2);
                _zoomCombo.Text = L.T("Fit width");
            };
            UpdateResultStatus();
        }

        // ================================================================ state

        private OcrDocument CurrentDoc => _fileList.SelectedItem as OcrDocument;

        private DocumentPage CurrentPage
        {
            get
            {
                var d = CurrentDoc;
                if (d == null || d.Pages.Count == 0) return null;
                return d.Pages[Math.Max(0, Math.Min(_pageIndex, d.Pages.Count - 1))];
            }
        }

        private IEnumerable<PageCanvas> Canvases()
        {
            yield return _viewer;
            yield return _layout;
            yield return _overlay;
        }

        private AppSettings Settings => _shell.Settings;

        public void OnActivated() => UpdateCounters();

        // ================================================================ adding documents

        public void AddPaths(IEnumerable<string> paths)
        {
            var errors = new List<string>();
            OcrDocument last = null;
            var files = new List<string>();
            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    var images = Directory.GetFiles(p).Where(OcrDocument.IsImage).ToList();
                    var pdfs = Directory.GetFiles(p, "*.pdf");
                    try
                    {
                        if (images.Count > 0) last = Add(OcrDocument.OpenImageSequence(images, Path.GetFileName(p.TrimEnd('\\', '/'))));
                    }
                    catch (Exception ex) { errors.Add(p + ": " + ex.Message); }
                    files.AddRange(pdfs);
                }
                else files.Add(p);
            }
            foreach (var f in files)
            {
                if (!OcrDocument.IsSupported(f))
                {
                    errors.Add(Path.GetFileName(f) + ": " + L.T("unsupported file type"));
                    continue;
                }
                try
                {
                    last = Add(OcrDocument.Open(f));
                    Settings.AddRecent(f);
                }
                catch (Exception ex) { errors.Add(Path.GetFileName(f) + ": " + ex.Message); }
            }
            if (last != null) _fileList.SelectedItem = last;
            if (errors.Count > 0)
                MessageBox.Show(this, L.T("Some files could not be opened:\n\n") + string.Join("\n", errors), L.T("Add Files"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (last != null && Settings.AutoRecognize) BeginInvoke(new Action(async () => await RecognizeCurrentDocumentAsync()));
        }

        private OcrDocument Add(OcrDocument doc)
        {
            _docs.Add(doc);
            _fileList.Items.Add(doc);
            UpdateCounters();
            return doc;
        }

        private const string OpenFilter = "Supported files|*.pdf;*.png;*.jpg;*.jpeg;*.jpe;*.bmp;*.dib;*.gif;*.tif;*.tiff|PDF documents|*.pdf|Images|*.png;*.jpg;*.jpeg;*.jpe;*.bmp;*.dib;*.gif;*.tif;*.tiff|All files|*.*";

        private void AddFilesDialog()
        {
            using (var dlg = new OpenFileDialog { Multiselect = true, Filter = OpenFilter, Title = L.T("Add Files") })
                if (dlg.ShowDialog(this) == DialogResult.OK) AddPaths(dlg.FileNames);
        }

        private void AddSequenceDialog()
        {
            using (var dlg = new OpenFileDialog { Multiselect = true, Filter = L.T("Images|*.png;*.jpg;*.jpeg;*.jpe;*.bmp;*.gif;*.tif;*.tiff"), Title = L.T("Select the images of one document") })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var name = L.F("{0} ({1} images)", Path.GetFileName(Path.GetDirectoryName(dlg.FileNames[0])), dlg.FileNames.Length);
                    _fileList.SelectedItem = Add(OcrDocument.OpenImageSequence(dlg.FileNames, name));
                }
                catch (Exception ex) { Error(L.T("Add image sequence"), ex); }
            }
        }

        private void AddFolderDialog()
        {
            using (var dlg = new FolderBrowserDialog { Description = L.T("Select a folder of page images (sorted by file name)") })
                if (dlg.ShowDialog(this) == DialogResult.OK) AddPaths(new[] { dlg.SelectedPath });
        }

        private void PasteFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsFileDropList())
                {
                    AddPaths(Clipboard.GetFileDropList().Cast<string>());
                    return;
                }
                if (!Clipboard.ContainsImage())
                {
                    MessageBox.Show(this, L.T("The clipboard does not contain an image."), L.T("From Clipboard"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using (var img = Clipboard.GetImage())
                using (var bmp = new Bitmap(img))
                {
                    int n = _docs.Count(d => d.Kind == SourceKind.Clipboard) + 1;
                    _fileList.SelectedItem = Add(OcrDocument.FromBitmap(bmp, "Clipboard_" + n.ToString("00")));
                }
            }
            catch (Exception ex) { Error(L.T("From Clipboard"), ex); }
        }

        private void Scan()
        {
            try
            {
                var path = Scanner.Acquire();
                if (path != null) AddPaths(new[] { path });
            }
            catch (Exception ex) { Error(L.T("Scan"), ex); }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files) AddPaths(files);
        }

        // ================================================================ page display

        private void ShowCurrent()
        {
            UpdateCounters();
            _viewer.CropMode = false;
            if (_thumbs.Visible) BuildThumbnails();
            ShowPage();
        }

        private void GoToPage(int index)
        {
            var d = CurrentDoc;
            if (d == null || d.Pages.Count == 0) return;
            index = Math.Max(0, Math.Min(d.Pages.Count - 1, index));
            if (index == _pageIndex) return;
            _pageIndex = index;
            ShowPage();
            if (_thumbs.Visible && _thumbs.Items.Count > index)
            {
                _thumbs.SelectedIndices.Clear();
                _thumbs.Items[index].Selected = true;
                _thumbs.EnsureVisible(index);
            }
        }

        private async void ShowPage()
        {
            var page = CurrentPage;
            int token = ++_loadToken;
            var d = CurrentDoc;
            _pageLabel.Text = d == null ? "0 / 0" : $"{_pageIndex + 1} / {d.Pages.Count}";
            if (page == null)
            {
                SetImage(null, null, 96);
                UpdateResultStatus();
                return;
            }
            _shell.SetStatus(L.F("Loading {0}…", page.Label));
            Bitmap bmp;
            float dpi;
            try
            {
                int pdfDpi = Settings.Ocr.PdfDpi;
                bmp = await Task.Run(() => page.GetProcessedImage(pdfDpi));
                dpi = page.Result?.Dpi ?? page.EffectiveDpi(bmp, pdfDpi);
            }
            catch (Exception ex)
            {
                if (token == _loadToken) _shell.SetStatus(L.T("Cannot display page: ") + ex.Message);
                return;
            }
            if (token != _loadToken)
            {
                bmp.Dispose();
                return;
            }
            SetImage(bmp, page.Result, dpi);
            _shell.SetStatus(L.T("Ready"));
            UpdateResultStatus();
        }

        private void SetImage(Bitmap bmp, OcrPage result, float dpi)
        {
            var old = _viewer.Image;
            foreach (var v in new[] { _viewer, _overlay })
            {
                v.Result = result;
                v.SelectedLine = null;
                v.Image = bmp;
                v.PageDpi = dpi;
            }
            _layout.Page = result;
            if (result == null && bmp != null)
            {
                _layout.PageSize = bmp.Size;
                _layout.PageDpi = dpi;
            }
            if (old != null && old != bmp) old.Dispose();
            var page = CurrentPage;
            _imageInfo.Text = bmp == null || page == null ? "" :
                L.F("{0}  ·  {1} × {2} px  ·  {3:0} dpi", page.Label, bmp.Width, bmp.Height, dpi) + (page.Rotation != 0 ? L.F("  ·  rotated {0}°", page.Rotation) : "") + (page.Crop.HasValue ? "  ·  " + L.T("cropped") : "");
            SyncFrom(_viewer);
        }

        private void RefreshResult()
        {
            var page = CurrentPage;
            if (page == null) return;
            // Rotation/crop changes invalidate the image; a new result may have a different DPI.
            if (_viewer.Image == null || page.Result == null || _viewer.Image.Size != new Size(page.Result.Width, page.Result.Height))
            {
                ShowPage();
                return;
            }
            _viewer.Result = page.Result;
            _overlay.Result = page.Result;
            foreach (var v in new[] { _viewer, _overlay }) { v.PageDpi = page.Result.Dpi; v.Invalidate(); }
            _layout.Page = page.Result;
            SyncFrom(_viewer);
            UpdateResultStatus();
        }

        private void SyncFrom(PageCanvas source)
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                foreach (var c in Canvases())
                {
                    if (c == source) continue;
                    if (source.ZoomMode != ZoomMode.Custom) c.ZoomMode = source.ZoomMode;
                    else if (Math.Abs(c.Zoom - source.Zoom) > 0.0001f || c.ZoomMode != ZoomMode.Custom) c.Zoom = source.Zoom;
                    c.ScrollFraction = source.ScrollFraction;
                }
                if (!_zoomCombo.Focused)
                    _zoomCombo.Text = source.ZoomMode == ZoomMode.FitWidth ? L.T("Fit width") : source.ZoomMode == ZoomMode.FitPage ? L.T("Fit page") : Math.Round(source.Zoom * 100) + "%";
            }
            finally { _syncing = false; }
        }

        private void ApplyZoomText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (text == L.T("Fit width") || text.StartsWith("Fit w", StringComparison.OrdinalIgnoreCase)) _viewer.ZoomMode = ZoomMode.FitWidth;
            else if (text == L.T("Fit page") || text.StartsWith("Fit p", StringComparison.OrdinalIgnoreCase)) _viewer.ZoomMode = ZoomMode.FitPage;
            else if (float.TryParse(text.Trim().TrimEnd('%'), out float pct) && pct > 0) _viewer.Zoom = pct / 100f;
            SyncFrom(_viewer);
        }

        private void SelectLine(TextLine line, PageCanvas origin)
        {
            _layout.Select(line, origin != _layout);
            foreach (var v in new[] { _viewer, _overlay })
            {
                v.SelectedLine = line;
                if (line != null && v != origin && v.Visible) v.EnsureVisible(line.Bounds);
                v.Invalidate();
            }
            UpdateFormatState();
        }

        private void UpdateResultStatus()
        {
            var d = CurrentDoc;
            var p = CurrentPage;
            _resultPage.Text = d == null ? "" : $"{_pageIndex + 1} / {d.Pages.Count}";
            if (p?.Result != null)
            {
                var r = p.Result;
                _resultStatusIcon.Visible = true;
                _resultStatus.Text = r.FromTextLayer
                    ? L.F("Text layer read from PDF — {0} lines, {1} blocks.", r.Lines.Count, r.Blocks.Count)
                    : L.F("Text recognition completed — {0} lines, {1} blocks, {2:P0} confidence ({3:0.0}s).", r.Lines.Count, r.Blocks.Count, r.MeanConfidence, r.Elapsed.TotalSeconds);
            }
            else
            {
                _resultStatusIcon.Visible = false;
                _resultStatus.Text = p == null ? L.T("No document selected.") : L.T("This page has not been recognized yet.");
            }
            UpdateFormatState();
        }

        private void UpdateCounters()
        {
            _shell.SetCounters(_docs.Count, _fileList.SelectedIndex >= 0 ? 1 : 0, Settings.Format);
            _fileList.Invalidate();
        }

        // ================================================================ thumbnails

        private async void BuildThumbnails()
        {
            int token = ++_thumbToken;
            _thumbs.BeginUpdate();
            _thumbs.Items.Clear();
            foreach (Image i in _thumbImages.Images) i.Dispose();
            _thumbImages.Images.Clear();
            var d = CurrentDoc;
            if (d != null)
                for (int i = 0; i < d.Pages.Count; i++)
                    _thumbs.Items.Add(new ListViewItem((i + 1) + (d.Pages[i].Result != null ? " ✓" : "")) { ImageIndex = -1 });
            _thumbs.EndUpdate();
            if (d == null) return;
            var size = _thumbImages.ImageSize;
            for (int i = 0; i < d.Pages.Count; i++)
            {
                var page = d.Pages[i];
                Bitmap thumb;
                try
                {
                    thumb = await Task.Run(() => MakeThumb(page, size));
                }
                catch { continue; }
                if (token != _thumbToken || i >= _thumbs.Items.Count)
                {
                    thumb.Dispose();
                    return;
                }
                _thumbImages.Images.Add(thumb);
                _thumbs.Items[i].ImageIndex = _thumbImages.Images.Count - 1;
            }
            if (_pageIndex < _thumbs.Items.Count) _thumbs.Items[_pageIndex].Selected = true;
        }

        private static Bitmap MakeThumb(DocumentPage page, Size box)
        {
            using (var src = page.GetProcessedImage(40))
            {
                var bmp = new Bitmap(box.Width, box.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    float k = Math.Min((box.Width - 6f) / src.Width, (box.Height - 6f) / src.Height);
                    int w = (int)(src.Width * k), h = (int)(src.Height * k);
                    var r = new Rectangle((box.Width - w) / 2, (box.Height - h) / 2, w, h);
                    using (var shadow = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillRectangle(shadow, r.X + 2, r.Y + 2, r.Width, r.Height);
                    g.DrawImage(src, r);
                    g.DrawRectangle(Pens.Silver, r);
                }
                return bmp;
            }
        }

        // ================================================================ file list drawing / menu

        private void DrawFileItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var doc = (OcrDocument)_fileList.Items[e.Index];
            var g = e.Graphics;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            g.FillRectangle(Brushes.White, e.Bounds);
            if (sel)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = Icons.Rounded(new RectangleF(e.Bounds.X + 1, e.Bounds.Y + 2, e.Bounds.Width - 3, e.Bounds.Height - 4), S(6)))
                using (var b = new SolidBrush(Theme.AccentLight))
                    g.FillPath(b, path);
            }
            var icon = doc.Kind == SourceKind.Pdf ? IconKind.Pdf : doc.Pages.Count > 1 ? IconKind.Images : IconKind.Image;
            int s = S(30);
            Icons.Draw(g, icon, new RectangleF(e.Bounds.X + S(10), e.Bounds.Y + (e.Bounds.Height - s) / 2f, s, s), Theme.Text);
            var nameRect = new Rectangle(e.Bounds.X + S(50), e.Bounds.Y + S(12), e.Bounds.Width - S(56), S(22));
            TextRenderer.DrawText(g, doc.Name, Theme.Base, nameRect, Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.Left);
            
            string sub = doc.Pages.Count == 1 ? L.T("1 page") : doc.Kind == SourceKind.ImageSequence ? L.F("{0} images", doc.Pages.Count) : L.F("{0} pages", doc.Pages.Count);
            if (doc.RecognizedCount > 0) sub += "  •  " + (doc.IsRecognized ? L.T("recognized") : L.F("{0}/{1} done", doc.RecognizedCount, doc.Pages.Count));
            TextRenderer.DrawText(g, sub, Theme.Small, new Rectangle(nameRect.X, nameRect.Bottom + S(2), nameRect.Width, S(18)), doc.IsRecognized ? Theme.Accent : Theme.SubText, TextFormatFlags.EndEllipsis | TextFormatFlags.Left);
        }

        private ContextMenuStrip FileMenu()
        {
            var m = new ContextMenuStrip();
            m.Items.Add(L.T("Recognize"), null, async (s, e) => await RecognizeCurrentDocumentAsync());
            m.Items.Add(L.T("Recognize all documents"), null, async (s, e) => await RecognizeAllAsync());
            m.Items.Add(L.T("Export"), null, async (s, e) => await ExportCurrentAsync());
            m.Items.Add(L.T("Export as…"), null, async (s, e) => await ExportCurrentAsync(true));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(L.T("Rename…"), null, (s, e) => RenameCurrent());
            m.Items.Add(L.T("Open containing folder"), null, (s, e) =>
            {
                var p = CurrentDoc?.SourcePath;
                if (p == null) return;
                if (File.Exists(p)) Process.Start("explorer.exe", "/select,\"" + p + "\"");
                else if (Directory.Exists(p)) Process.Start("explorer.exe", "\"" + p + "\"");
            });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(L.T("Remove"), null, (s, e) => DeleteCurrent());
            m.Items.Add(L.T("Remove all"), null, (s, e) => RemoveAll());
            return m;
        }

        private void RenameCurrent()
        {
            var d = CurrentDoc;
            if (d == null) return;
            var name = Prompt.Show(this, L.T("Rename document"), L.T("Name:"), d.Name);
            if (string.IsNullOrWhiteSpace(name)) return;
            d.Name = name.Trim();
            _fileList.Invalidate();
        }

        // ================================================================ edit commands

        private void RotateCurrent()
        {
            var p = CurrentPage;
            if (p == null) return;
            p.Rotation += 90;
            _shell.SetStatus(L.F("{0} rotated to {1}° — recognize again to update the text.", p.Label, p.Rotation));
            ShowPage();
            _fileList.Invalidate();
            if (_thumbs.Visible) BuildThumbnails();
        }

        private void ToggleCrop()
        {
            if (CurrentPage == null) return;
            if (CurrentPage.Crop.HasValue && !_viewer.CropMode)
            {
                var r = MessageBox.Show(this, L.T("This page is already cropped.\n\nYes = crop further, No = restore the full page."), L.T("Crop"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) return;
                if (r == DialogResult.No)
                {
                    CurrentPage.Crop = null;
                    ShowPage();
                    return;
                }
            }
            _resultTabs.SelectedIndex = 0;
            _viewer.CropMode = !_viewer.CropMode;
            _viewer.Focus();
        }

        private void ApplyCrop(Rectangle r)
        {
            var p = CurrentPage;
            if (p == null || _viewer.Image == null) return;
            p.ApplyCrop(r, _viewer.Image.Size);
            _viewer.CropMode = false;
            _shell.SetStatus(L.F("{0} cropped — recognize again to update the text.", p.Label));
            ShowPage();
            if (_thumbs.Visible) BuildThumbnails();
        }

        private void DeleteCurrent()
        {
            var d = CurrentDoc;
            if (d == null) return;
            if (_thumbs.Visible && d.Pages.Count > 1 && CurrentPage != null)
            {
                if (MessageBox.Show(this, L.F("Remove {0} from \"{1}\"?", CurrentPage.Label, d.Name), L.T("Delete"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                d.RemovePage(CurrentPage);
                _pageIndex = Math.Min(_pageIndex, d.Pages.Count - 1);
                ShowCurrent();
                return;
            }
            if (d.RecognizedCount > 0 && MessageBox.Show(this, L.F("Remove \"{0}\" and its recognition results?", d.Name), L.T("Delete"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            int idx = _fileList.SelectedIndex;
            _fileList.Items.Remove(d);
            _docs.Remove(d);
            d.Dispose();
            if (_fileList.Items.Count > 0) _fileList.SelectedIndex = Math.Min(idx, _fileList.Items.Count - 1);
            else ShowCurrent();
            UpdateCounters();
        }

        private void RemoveAll()
        {
            if (_docs.Count == 0) return;
            if (MessageBox.Show(this, L.T("Remove all documents from the workspace?"), L.T("Remove all"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            _fileList.Items.Clear();
            DisposeDocuments();
            ShowCurrent();
        }

        private void ApplyBlockStyle(int styleIndex)
        {
            var line = _layout.SelectedLine;
            var block = _layout.BlockOf(line);
            if (block == null || block.Kind == BlockKind.Table || block.Kind == BlockKind.Figure) return;
            switch (styleIndex)
            {
                case 0:
                    block.Kind = BlockKind.Paragraph;
                    block.ListMarker = null;
                    block.MarkerPrefixLength = 0;
                    break;
                case 1:
                case 2:
                case 3:
                    block.Kind = BlockKind.Heading;
                    block.HeadingLevel = styleIndex;
                    block.ListMarker = null;
                    block.MarkerPrefixLength = 0;
                    break;
                case 4: // bullet
                    block.Kind = BlockKind.ListItem;
                    block.OrderedList = false;
                    block.ListMarker = "•";
                    if (block.TextLeft <= 0) block.TextLeft = block.Bounds.Left;
                    break;
                case 5: // numbered
                    var siblings = CurrentPage.Result.Blocks;
                    int n = 1;
                    for (int i = siblings.IndexOf(block) - 1; i >= 0 && siblings[i].Kind == BlockKind.ListItem && siblings[i].OrderedList; i--) n++;
                    block.Kind = BlockKind.ListItem;
                    block.OrderedList = true;
                    block.ListMarker = n + ".";
                    if (block.TextLeft <= 0) block.TextLeft = block.Bounds.Left;
                    break;
            }
            _layout.Invalidate();
            UpdateFormatState();
            _shell.SetStatus(L.F("Paragraph style changed to {0}.", styleIndex <= 4 ? _styleCombo.Items[Math.Min(styleIndex, 4)] : L.T("Numbered list")));
        }

        private void ToggleLineStyle(Action<TextStyle> change)
        {
            var line = _layout.SelectedLine;
            if (line == null) return;
            var block = _layout.BlockOf(line);
            var targets = block != null && block.Kind != BlockKind.Table ? block.AllTextLines().ToList() : new List<TextLine> { line };
            // Apply to the whole paragraph so exports stay consistent.
            var probe = line.Style.Clone();
            change(probe);
            foreach (var t in targets)
            {
                t.Style.Bold = probe.Bold;
                t.Style.Italic = probe.Italic;
                t.Style.Underline = probe.Underline;
            }
            if (block != null)
            {
                block.Style.Bold = probe.Bold;
                block.Style.Italic = probe.Italic;
                block.Style.Underline = probe.Underline;
            }
            _layout.Invalidate();
            UpdateFormatState();
        }

        // ================================================================ files panel

        /// <summary>Collapses the Files / Thumbnails panel to a narrow strip (or expands it); the state is remembered.</summary>
        private void SetFilesPanelCollapsed(bool collapsed, bool save = true)
        {
            _filesCollapsed = collapsed;
            _leftTabs.Visible = _leftBody.Visible = !collapsed;
            _leftPanel.Width = S(collapsed ? 40 : 214);
            _filesToggle.Collapsed = collapsed;
            _filesToggle.Dock = collapsed ? DockStyle.Fill : DockStyle.Bottom;
            _panelTips.SetToolTip(_filesToggle, collapsed ? L.T("Show files and thumbnails") : L.T("Hide files and thumbnails"));
            if (save)
            {
                Settings.FilesPanelCollapsed = collapsed;
                Settings.Save();
            }
        }

        /// <summary>Snapshot tour: shows the files panel collapsed / expanded without remembering it.</summary>
        internal void SnapshotFilesPanel(bool collapsed) => SetFilesPanelCollapsed(collapsed, save: false);

        /// <summary>
        /// « Hide panel button at the bottom of the files panel; when collapsed it fills the narrow strip,
        /// shows the panel name vertically and » at the bottom.
        /// </summary>
        private sealed class PanelToggle : Control
        {
            private bool _hover, _collapsed;

            public PanelToggle()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Theme.Panel;
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
                g.Clear(_hover ? Theme.AccentHover : Theme.Panel);
                float s = 18 * k;
                if (_collapsed)
                {
                    // Vertical caption, read bottom-to-top like a tab.
                    var state = g.Save();
                    g.TranslateTransform(Width / 2f, 16 * k);
                    g.RotateTransform(90);
                    // TextRenderer ignores transforms, so the rotated caption is drawn with GDI+.
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    using (var br = new SolidBrush(Theme.Text))
                    using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
                        g.DrawString(L.T("Files") + "  /  " + L.T("Thumbnails"), Font, br, 0, 0, sf);
                    g.Restore(state);
                    Icons.Draw(g, IconKind.ChevronRight, new RectangleF((Width - s) / 2, Height - s - 12 * k, s, s), Theme.Accent, 2 * k);
                    return;
                }
                using (var p = new Pen(Theme.Border)) g.DrawLine(p, 0, 0, Width, 0);
                Icons.Draw(g, IconKind.ChevronLeft, new RectangleF(12 * k, (Height - s) / 2, s, s), Theme.Accent, 2 * k);
                TextRenderer.DrawText(g, L.T("Hide panel"), Font, new Rectangle((int)(38 * k), 0, Width - (int)(38 * k), Height), Theme.SubText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }
        }

        // ================================================================ trial banner

        /// <summary>Amber notice under the toolbar while the application runs as a trial.</summary>
        private Panel BuildTrialBanner()
        {
            var trial = Program.Trial;
            var banner = new Panel { Dock = DockStyle.Top, Height = S(40), BackColor = Color.FromArgb(255, 244, 214), Padding = new Padding(S(12), S(6), S(10), S(6)) };
            banner.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Color.FromArgb(242, 210, 122))) e.Graphics.DrawLine(p, 0, banner.Height - 1, banner.Width, banner.Height - 1);
                // Clock icon.
                int d = S(20), y = (banner.Height - d) / 2;
                using (var b = new SolidBrush(Color.FromArgb(255, 179, 0))) e.Graphics.FillEllipse(b, S(14), y, d, d);
                using (var p = new Pen(Color.White, S(2))) { e.Graphics.DrawLine(p, S(14) + d / 2, y + S(5), S(14) + d / 2, y + d / 2); e.Graphics.DrawLine(p, S(14) + d / 2, y + d / 2, S(14) + d / 2 + S(4), y + d / 2 + S(3)); }
            };
            var text = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(102, 71, 0),
                Padding = new Padding(S(30), 0, 0, 0),
                Text = L.F("You are using the trial version of Falcon OCR — {0} of {1} days left. All features are available during the trial.", trial.DaysLeft, trial.TotalDays)
            };
            var activate = new Button { Text = L.T("Activate now"), Dock = DockStyle.Right, Width = S(130), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Font = Theme.Bold, Cursor = Cursors.Hand };
            activate.FlatAppearance.BorderSize = 0;
            activate.Click += (s, e) => (FindForm() as MainForm)?.ShowActivation();
            var close = new Button { Text = "✕", Dock = DockStyle.Right, Width = S(34), FlatStyle = FlatStyle.Flat, BackColor = banner.BackColor, ForeColor = Color.FromArgb(102, 71, 0), Cursor = Cursors.Hand };
            close.FlatAppearance.BorderSize = 0;
            new ToolTip().SetToolTip(close, L.T("Hide until the next start"));
            close.Click += (s, e) => HideTrialBanner();
            banner.Controls.Add(text);
            banner.Controls.Add(new Panel { Dock = DockStyle.Right, Width = S(8), BackColor = banner.BackColor });
            banner.Controls.Add(activate);
            banner.Controls.Add(close);
            return banner;
        }

        /// <summary>Removes the trial notice (after activation, or when the user closes it).</summary>
        public void HideTrialBanner()
        {
            if (_trialBanner == null) return;
            Controls.Remove(_trialBanner);
            _trialBanner.Dispose();
            _trialBanner = null;
        }

        /// <summary>Splitter with a centred grip, highlighted on hover.</summary>
        private sealed class GripSplitter : Splitter
        {
            private bool _hover;

            public GripSplitter()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Theme.Window;
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(_hover ? Theme.AccentLight : BackColor);
                int cx = Width / 2, cy = Height / 2;
                using (var b = new SolidBrush(_hover ? Theme.Accent : Color.FromArgb(170, 178, 186)))
                    for (int i = -2; i <= 2; i++) e.Graphics.FillEllipse(b, cx - 1.5f, cy + i * 7 - 1.5f, 3, 3);
            }
        }

        /// <summary>Installed font families, the fonts the OCR uses by default first.</summary>
        internal static object[] FontFamilies()
        {
            var preferred = new[] { "Calibri", "Arial", "Times New Roman", "Segoe UI", "Cambria", "Georgia", "Verdana", "Courier New", "Microsoft YaHei", "SimSun", "Yu Gothic", "MS Mincho", "Malgun Gothic" };
            var installed = new List<string>();
            using (var fc = new System.Drawing.Text.InstalledFontCollection())
                installed.AddRange(fc.Families.Select(f => f.Name).Where(n => !n.StartsWith("@")));
            return preferred.Where(p => installed.Contains(p)).Concat(installed.Where(n => !preferred.Contains(n)).OrderBy(n => n)).Cast<object>().ToArray();
        }

        /// <summary>Lines of the paragraph containing the selected line (or the line alone inside tables).</summary>
        private List<TextLine> ParagraphLines(out LayoutBlock block)
        {
            var line = _layout.SelectedLine;
            block = _layout.BlockOf(line);
            if (line == null) return new List<TextLine>();
            return block != null && block.Kind != BlockKind.Table ? block.AllTextLines().ToList() : new List<TextLine> { line };
        }

        private void ApplyFont(string family, float? size)
        {
            var targets = ParagraphLines(out var block);
            if (targets.Count == 0 || (string.IsNullOrWhiteSpace(family) && size == null)) return;
            foreach (var t in targets)
            {
                if (!string.IsNullOrWhiteSpace(family)) t.Style.FontFamily = family;
                if (size.HasValue) t.Style.FontSizePt = size.Value;
            }
            if (block != null && block.Kind != BlockKind.Table)
            {
                if (!string.IsNullOrWhiteSpace(family)) block.Style.FontFamily = family;
                if (size.HasValue) block.Style.FontSizePt = size.Value;
            }
            _layout.Invalidate();
            UpdateFormatState();
            _shell.SetStatus(size.HasValue ? L.F("Font size set to {0} pt.", size.Value) : L.F("Font set to {0}.", family));
        }

        private void ApplyFontSize(string text)
        {
            if (float.TryParse((text ?? "").Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float pt) && pt >= 4 && pt <= 200)
                ApplyFont(null, pt);
            else UpdateFormatState();
        }

        private void UpdateFormatState()
        {
            var line = _layout.SelectedLine;
            var block = _layout.BlockOf(line);
            bool on = line != null;
            foreach (var b in new[] { _boldButton, _italicButton, _underlineButton, _bulletButton, _numberButton }) b.Enabled = on;
            _fontCombo.Enabled = _sizeCombo.Enabled = on;
            if (on && !_fontCombo.Focused) _fontCombo.Text = line.Style.FontFamily ?? Settings.Ocr.EffectiveFont(LanguageCatalog.Get(Settings.Ocr.Language));
            if (on && !_sizeCombo.Focused) _sizeCombo.Text = line.Style.FontSizePt.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            if (!on) { _fontCombo.Text = ""; _sizeCombo.Text = ""; }
            _styleCombo.Enabled = block != null && block.Kind != BlockKind.Table && block.Kind != BlockKind.Figure;
            _boldButton.Checked = on && line.Style.Bold;
            _italicButton.Checked = on && line.Style.Italic;
            _underlineButton.Checked = on && line.Style.Underline;
            _bulletButton.Checked = block != null && block.Kind == BlockKind.ListItem && !block.OrderedList;
            _numberButton.Checked = block != null && block.Kind == BlockKind.ListItem && block.OrderedList;
            if (block != null)
                _styleCombo.SelectedIndex = block.Kind == BlockKind.Heading ? Math.Max(1, Math.Min(3, block.HeadingLevel)) : block.Kind == BlockKind.ListItem ? 4 : 0;
        }

        private void CopyText(bool wholeDocument)
        {
            var d = CurrentDoc;
            if (d == null) return;
            var pages = wholeDocument ? d.Pages : new List<DocumentPage> { CurrentPage };
            var sb = new StringBuilder();
            foreach (var p in pages.Where(p => p?.Result != null)) sb.AppendLine(p.Result.GetPlainText());
            if (sb.Length == 0)
            {
                _shell.SetStatus(L.T("Nothing to copy — recognize the document first."));
                return;
            }
            Clipboard.SetText(sb.ToString());
            _shell.SetStatus(L.T("Text copied to the clipboard."));
        }

        // ================================================================ recognition

        private async Task RecognizeCurrentDocumentAsync()
        {
            if (_cts != null)
            {
                _cts.Cancel(); // the button acts as "Stop" while running
                return;
            }
            var d = CurrentDoc;
            if (d == null)
            {
                AddFilesDialog();
                return;
            }
            var pages = d.Pages.Where(p => p.Result == null).ToList();
            if (pages.Count == 0)
            {
                if (MessageBox.Show(this, L.F("\"{0}\" is already recognized. Recognize it again with the current settings?", d.Name), L.T("Recognize"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                pages = d.Pages.ToList();
            }
            // Current page first so the user sees a result as early as possible.
            var cur = CurrentPage;
            if (cur != null && pages.Remove(cur)) pages.Insert(0, cur);
            await RecognizePagesAsync(pages);
        }

        /// <summary>Recognizes the current document without prompts (diagnostic snapshot tour).</summary>
        internal Task SnapshotRecognizeAsync()
        {
            var d = CurrentDoc;
            return d == null ? Task.CompletedTask : RecognizePagesAsync(d.Pages.Where(p => p.Result == null).ToList());
        }

        /// <summary>Diagnostic tour: select the n-th recognized line / switch the results tab.</summary>
        internal void SnapshotSelect(int lineIndex, int resultTab)
        {
            var lines = CurrentPage?.Result?.Lines;
            if (lines != null && lines.Count > 0) SelectLine(lines.OrderBy(l => l.Bounds.Top).ElementAt(Math.Min(lineIndex, lines.Count - 1)), _viewer);
            _resultTabs.SelectedIndex = resultTab;
        }

        private async Task RecognizeAllAsync()
        {
            var pages = _docs.SelectMany(d => d.Pages).Where(p => p.Result == null).ToList();
            if (pages.Count == 0) return;
            await RecognizePagesAsync(pages);
        }

        /// <summary>Returns true when all pages were recognized.</summary>
        private async Task<bool> RecognizePagesAsync(IList<DocumentPage> pages)
        {
            if (_cts != null) return false;
            var missing = OcrService.MissingModels(Settings.Ocr.Language);
            if (missing.Count > 0)
            {
                MessageBox.Show(this, L.T("OCR models are missing:\n") + string.Join("\n", missing), L.T("Recognize"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            _cts = new CancellationTokenSource();
            _recognizeButton.Text = L.T("Stop");
            _recognizeButton.Icon = IconKind.Stop;
            _recognizeButton.IconColor = Theme.Danger;
            var sw = Stopwatch.StartNew();
            var progress = new Progress<PageProgress>(p => _shell.SetStatus(p.Message, p.Total == 0 ? -1 : (int)(p.Done * 100L / p.Total)));
            try
            {
                await _shell.Ocr.RecognizeAsync(pages, Settings.Ocr, progress, page => BeginInvoke(new Action(() => OnPageRecognized(page))), _cts.Token);
                _shell.SetStatus(L.F("Text recognition completed — {0} page(s) in {1:0.0}s.", pages.Count, sw.Elapsed.TotalSeconds));
                foreach (var doc in pages.Select(p => p.Document).Distinct())
                    _shell.History.Add(new HistoryEntry { Time = DateTime.Now, Source = doc.SourcePath ?? doc.Name, Format = "Recognition", Pages = pages.Count(p => p.Document == doc), Language = Settings.Ocr.Language.ToString(), Seconds = sw.Elapsed.TotalSeconds });
                return true;
            }
            catch (OperationCanceledException)
            {
                _shell.SetStatus(L.T("Recognition stopped."));
                return false;
            }
            catch (Exception ex)
            {
                _shell.SetStatus(L.T("Recognition failed."));
                Error(L.T("Recognize"), ex);
                return false;
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _recognizeButton.Text = L.T("Recognize");
                _recognizeButton.Icon = IconKind.Play;
                _recognizeButton.IconColor = Theme.Accent;
                _fileList.Invalidate();
            }
        }

        private void OnPageRecognized(DocumentPage page)
        {
            _fileList.Invalidate();
            if (page == CurrentPage) RefreshResult();
            if (_thumbs.Visible && page.Document == CurrentDoc && page.Index < _thumbs.Items.Count)
                _thumbs.Items[page.Index].Text = (page.Index + 1) + " ✓";
        }

        // ================================================================ export

        private async Task ExportCurrentAsync(bool askPath = false)
        {
            var d = CurrentDoc;
            if (d == null)
            {
                MessageBox.Show(this, L.T("Add and recognize a document first."), L.T("Export"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var missing = d.Pages.Where(p => p.Result == null).ToList();
            if (missing.Count > 0)
            {
                var answer = missing.Count == d.Pages.Count
                    ? (MessageBox.Show(this, L.F("\"{0}\" has not been recognized yet. Recognize it now?", d.Name), L.T("Export"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK ? DialogResult.Yes : DialogResult.Cancel)
                    : MessageBox.Show(this, L.F("{0} of {1} pages are not recognized.\n\nYes = recognize them first\nNo = export only the recognized pages", missing.Count, d.Pages.Count), L.T("Export"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel) return;
                if (answer == DialogResult.Yes && !await RecognizePagesAsync(missing)) return;
            }

            var format = Settings.Format;
            string path;
            if (askPath)
            {
                using (var dlg = new SaveFileDialog { Filter = Exporter.FilterFor(format), FileName = Path.GetFileNameWithoutExtension(d.Name) + Exporter.Extension(format), InitialDirectory = Settings.OutputFolder })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    path = dlg.FileName;
                }
            }
            else
            {
                try { Directory.CreateDirectory(Settings.OutputFolder); }
                catch (Exception ex) { Error(L.T("Export"), new IOException(L.T("Cannot create the output folder: ") + ex.Message)); return; }
                path = Exporter.UniquePath(Settings.OutputFolder, Path.GetFileNameWithoutExtension(d.Name), Exporter.Extension(format));
            }

            _shell.SetStatus(L.F("Exporting {0}…", Path.GetFileName(path)));
            var opts = Settings.ExportOptions();
            var sw = Stopwatch.StartNew();
            try
            {
                await Task.Run(() => Exporter.Export(d, format, path, opts));
            }
            catch (Exception ex)
            {
                _shell.SetStatus(L.T("Export failed."));
                Error(L.T("Export"), ex);
                return;
            }
            _shell.History.Add(new HistoryEntry { Time = DateTime.Now, Source = d.SourcePath ?? d.Name, Output = path, Format = format.ToString().ToUpperInvariant(), Pages = d.RecognizedCount, Language = Settings.Ocr.Language.ToString(), Seconds = sw.Elapsed.TotalSeconds });
            _shell.SetStatus(L.F("Exported to {0}", path));
            if (Settings.OpenAfterExport)
            {
                try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
                catch { Process.Start("explorer.exe", "/select,\"" + path + "\""); }
            }
        }

        // ================================================================ settings column

        private Control BuildSettingsColumn()
        {
            var col = new Panel { Dock = DockStyle.Right, Width = S(340), Padding = new Padding(0, S(8), S(10), S(8)), BackColor = Theme.Window };

            var ocrCard = new CardPanel(L.T("OCR Settings"), IconKind.Settings) { Dock = DockStyle.Top, Height = S(390) };
            _docType = Field.Combo(L.T("Editable Document (Recommended)"), L.T("Exact Copy (keep positions)"), L.T("Plain Text"));
            _language = Field.Combo(LanguageCatalog.All.Select(l => (object)L.T(l.DisplayName)).ToArray());
            _layoutMode = Field.Combo(L.T("Automatic"), L.T("Single column"), L.T("Text lines only"));
            _detectTables = new CheckBox { Text = L.T("Detect tables and columns"), Dock = DockStyle.Top, Height = S(40), FlatStyle = FlatStyle.System };
            var advanced = new Button { Text = L.T("⚙  Advanced Settings…"), Dock = DockStyle.Top, Height = S(38), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.Text };
            advanced.FlatAppearance.BorderColor = Theme.Border;
            advanced.Click += (s, e) => { using (var dlg = new AdvancedSettingsDialog(Settings)) if (dlg.ShowDialog(this) == DialogResult.OK) Settings.Save(); };
            Stack(ocrCard, Field.Caption(L.T("Document Type:")), _docType, Field.Caption(L.T("Language:")), _language, Field.Caption(L.T("Layout Analysis:")), _layoutMode, Field.Spacer(S(6)), _detectTables, Field.Spacer(S(6)), advanced);

            var outCard = new CardPanel(L.T("Output Format"), IconKind.Export) { Dock = DockStyle.Top, Height = S(290) };
            var word = new IconRadio(L.T("Microsoft Word (.docx)"), IconKind.Word);
            var excel = new IconRadio(L.T("Microsoft Excel (.xlsx)"), IconKind.Excel);
            var html = new IconRadio(L.T("HTML (.html)"), IconKind.Html);
            _formatRadios[ExportFormat.Docx] = word;
            _formatRadios[ExportFormat.Xlsx] = excel;
            _formatRadios[ExportFormat.Html] = html;
            var radios = new Panel { Dock = DockStyle.Top, Height = S(136) };
            radios.Controls.Add(html);
            radios.Controls.Add(excel);
            radios.Controls.Add(word);
            _outputFolder = new TextBox { Dock = DockStyle.Fill, Font = Theme.Base };
            var browse = new Button { Text = "…", Dock = DockStyle.Right, Width = S(36), FlatStyle = FlatStyle.System };
            var folderRow = new Panel { Dock = DockStyle.Top, Height = S(30) };
            folderRow.Controls.Add(_outputFolder);
            folderRow.Controls.Add(browse);
            Stack(outCard, radios, Field.Caption(L.T("Output Folder:")), folderRow);

            var export = new AccentButton(L.T("Export"), IconKind.Export) { Dock = DockStyle.Top, Height = S(52) };
            export.Click += async (s, e) => await ExportCurrentAsync();

            col.Controls.Add(export);
            col.Controls.Add(Field.Spacer(S(10)));
            col.Controls.Add(outCard);
            col.Controls.Add(Field.Spacer(S(10)));
            col.Controls.Add(ocrCard);

            LoadSettingsIntoControls();
            _docType.SelectedIndexChanged += (s, e) => { if (!_loadingSettings) { Settings.Mode = (DocumentMode)_docType.SelectedIndex; Settings.Save(); } };
            _language.SelectedIndexChanged += (s, e) =>
            {
                if (_loadingSettings) return;
                Settings.Ocr.Language = LanguageCatalog.All[_language.SelectedIndex].Language;
                Settings.Save();
                _shell.Ocr.WarmupAsync(Settings.Ocr);
            };
            _layoutMode.SelectedIndexChanged += (s, e) => { if (!_loadingSettings) { Settings.Ocr.Layout = (LayoutMode)_layoutMode.SelectedIndex; Settings.Save(); } };
            _detectTables.CheckedChanged += (s, e) => { if (!_loadingSettings) { Settings.Ocr.DetectTables = _detectTables.Checked; Settings.Save(); } };
            foreach (var kv in _formatRadios)
            {
                var f = kv.Key;
                kv.Value.CheckedChanged += (s, e) =>
                {
                    if (_loadingSettings || !((IconRadio)s).Checked) return;
                    Settings.Format = f;
                    Settings.Save();
                    UpdateCounters();
                };
            }
            _outputFolder.Leave += (s, e) => { Settings.OutputFolder = _outputFolder.Text.Trim(); Settings.Save(); };
            browse.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog { SelectedPath = Settings.OutputFolder, Description = L.T("Output folder for exported documents") })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    _outputFolder.Text = dlg.SelectedPath;
                    Settings.OutputFolder = dlg.SelectedPath;
                    Settings.Save();
                }
            };
            return col;
        }

        /// <summary>Reloads the right-hand controls from the settings (after the Settings page changed them).</summary>
        public void LoadSettingsIntoControls()
        {
            _loadingSettings = true;
            try
            {
                _docType.SelectedIndex = (int)Settings.Mode;
                _language.SelectedIndex = Math.Max(0, LanguageCatalog.All.ToList().FindIndex(l => l.Language == Settings.Ocr.Language));
                _layoutMode.SelectedIndex = (int)Settings.Ocr.Layout;
                _detectTables.Checked = Settings.Ocr.DetectTables;
                if (_formatRadios.TryGetValue(Settings.Format, out var r)) r.Checked = true;
                else _formatRadios[ExportFormat.Docx].Checked = true;
                _outputFolder.Text = Settings.OutputFolder;
            }
            finally { _loadingSettings = false; }
            UpdateCounters();
        }

        // ================================================================ keyboard

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!Visible) return base.ProcessCmdKey(ref msg, keyData);
            bool textFocus = TextInputFocused();
            switch (keyData)
            {
                case Keys.Control | Keys.O: AddFilesDialog(); return true;
                case Keys.F5: _ = RecognizeCurrentDocumentAsync(); return true;
                case Keys.Control | Keys.E: _ = ExportCurrentAsync(); return true;
                case Keys.Control | Keys.R: RotateCurrent(); return true;
                case Keys.PageDown: GoToPage(_pageIndex + 1); return true;
                case Keys.PageUp: GoToPage(_pageIndex - 1); return true;
                case Keys.Control | Keys.Oemplus:
                case Keys.Control | Keys.Add: _viewer.Zoom *= 1.2f; return true;
                case Keys.Control | Keys.OemMinus:
                case Keys.Control | Keys.Subtract: _viewer.Zoom /= 1.2f; return true;
                case Keys.Control | Keys.V:
                    if (textFocus) break;
                    PasteFromClipboard();
                    return true;
                case Keys.Delete:
                    if (textFocus) break;
                    DeleteCurrent();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowHelp()
        {
            MessageBox.Show(this,
                L.T("Falcon OCR — offline OCR with PaddleOCR (PP-OCRv5)\n\n" +
                    "1. Add PDFs, images, image sequences, paste or scan a page.\n" +
                    "2. Choose the language and document type on the right.\n" +
                    "3. Recognize (F5). The recognized page is rebuilt next to the original;\n" +
                    "    click a line to find it in the image, double-click (or F2) to correct it.\n" +
                    "4. Export to Word, Excel or HTML (Ctrl+E).\n\n" +
                    "Shortcuts: Ctrl+O add files · Ctrl+V paste image · Ctrl+R rotate · Del remove\n" +
                    "PgUp/PgDn pages · Ctrl+wheel zoom · middle mouse pans") + "\n\n" +
                (Program.IsTrial ? Program.Trial.Message : Program.License?.IsValid == true ? Program.License.Message : L.T("Unlicensed")),
                L.T("Help"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public bool ConfirmClose()
        {
            if (_cts == null) return true;
            if (MessageBox.Show(this, L.T("Recognition is still running. Stop it and exit?"), "Falcon OCR", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return false;
            _cts.Cancel();
            return true;
        }

        public void DisposeDocuments()
        {
            foreach (var d in _docs) d.Dispose();
            _docs.Clear();
            UpdateCounters();
        }

        // ================================================================ helpers

        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        private Control Separator(int height = 64)
        {
            var p = new Panel { Width = S(17), Height = S(height), Margin = new Padding(0, S(4), 0, 0) };
            p.Paint += (s, e) => { using (var pen = new Pen(Theme.Border)) e.Graphics.DrawLine(pen, p.Width / 2, 4, p.Width / 2, p.Height - 4); };
            return p;
        }

        private Control Pad(Control c, int top)
        {
            c.Margin = new Padding(S(3), S(top), S(3), 0);
            return c;
        }

        private static Control LinkButton(string text, IconKind icon) => new InlineLink(text, icon);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        /// <summary>True while the user types in a text field (shortcuts like Del/Ctrl+V must not steal keys).</summary>
        private static bool TextInputFocused()
        {
            var c = FromHandle(GetFocus());
            return c is TextBoxBase || c is ComboBox;
        }

        /// <summary>Icon + text on one line (header links: Settings, Help).</summary>
        private sealed class InlineLink : Control
        {
            private readonly IconKind _icon;
            private bool _hover;

            public InlineLink(string text, IconKind icon)
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Text = text;
                _icon = icon;
                Font = Theme.Base;
                Cursor = Cursors.Hand;
                Size = new Size((int)(104 * DeviceDpi / 96f), (int)(34 * DeviceDpi / 96f));
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(_hover ? Theme.AccentHover : Theme.Panel);
                int s = (int)(20 * DeviceDpi / 96f);
                Icons.Draw(g, _icon, new RectangleF(6, (Height - s) / 2f, s, s), Theme.Text);
                TextRenderer.DrawText(g, Text, Font, new Rectangle(s + 14, 0, Width - s - 14, Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }

        /// <summary>Adds controls to a Dock=Top stack in visual (top-to-bottom) order.</summary>
        private static void Stack(Control parent, params Control[] topDown)
        {
            for (int i = topDown.Length - 1; i >= 0; i--)
            {
                topDown[i].Dock = DockStyle.Top;
                parent.Controls.Add(topDown[i]);
            }
            // Keep a CardPanel's header above the stacked fields.
            if (parent is CardPanel card) card.Header.SendToBack();
        }

        private void Error(string title, Exception ex)
        {
            MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
