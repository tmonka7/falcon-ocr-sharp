using System;
using FalconOcr.Localization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FalconOcr.App.Services;
using FalconOcr.App.UI;
using FalconOcr.Engine;
using FalconOcr.Export;
using FalconOcr.Input;
using FalconOcr.Processing;

namespace FalconOcr.App.Pages
{
    /// <summary>Common frame for the secondary pages: a title, a subtitle and a white body card.</summary>
    internal abstract class PageBase : UserControl
    {
        protected readonly IShell Shell;
        protected readonly Panel Body;

        protected PageBase(IShell shell, string title, string subtitle)
        {
            Shell = shell;
            BackColor = Theme.Window;
            Font = Theme.Base;
            Padding = new Padding(S(24), S(16), S(24), S(16));
            var header = new Panel { Dock = DockStyle.Top, Height = S(70) };
            header.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Top, Height = S(26), ForeColor = Theme.SubText });
            header.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = S(40), Font = Theme.Title, ForeColor = Theme.Text });
            Body = new BorderPanel { Dock = DockStyle.Fill, Padding = new Padding(S(20)) };
            Controls.Add(Body);
            Controls.Add(header);
        }

        protected int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        protected static Button FlatButton(string text, int width = 130)
        {
            var b = new Button { Text = text, Width = width, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.Text, Margin = new Padding(0, 0, 8, 0) };
            b.FlatAppearance.BorderColor = Theme.Border;
            return b;
        }

        protected static void Stack(Control parent, params Control[] topDown)
        {
            for (int i = topDown.Length - 1; i >= 0; i--)
            {
                topDown[i].Dock = DockStyle.Top;
                parent.Controls.Add(topDown[i]);
            }
        }
    }

    // ==================================================================== Quick OCR

    /// <summary>Drop / paste / open one image and get plain text immediately.</summary>
    internal sealed class QuickOcrPage : PageBase
    {
        private readonly ImageViewer _image;
        private readonly TextBox _text;
        private readonly Label _info;
        private OcrDocument _doc;

        public QuickOcrPage(IShell shell) : base(shell, L.T("Quick OCR"), L.T("Paste (Ctrl+V), drop or open an image to get its text instantly — nothing is saved."))
        {
            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = S(12), BackColor = Theme.Panel };
            _image = new ImageViewer { Dock = DockStyle.Fill, Placeholder = L.T("Drop an image here or press Ctrl+V"), AllowDrop = true, ShowOverlay = true };
            _text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font(Theme.Family, 11f), BorderStyle = BorderStyle.FixedSingle };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = S(46) };
            var open = FlatButton(L.T("Open image…"));
            var paste = FlatButton(L.T("Paste image"));
            var copy = FlatButton(L.T("Copy text"));
            var send = FlatButton(L.T("Open in workspace"), 160);
            buttons.Controls.AddRange(new Control[] { open, paste, copy, send });
            _info = new Label { Dock = DockStyle.Bottom, Height = S(28), ForeColor = Theme.SubText, TextAlign = ContentAlignment.MiddleLeft };
            split.Panel1.Controls.Add(_image);
            split.Panel2.Controls.Add(_text);
            Body.Controls.Add(split);
            Body.Controls.Add(_info);
            Body.Controls.Add(buttons);

            open.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = L.T("Images and PDF|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.pdf") })
                    if (dlg.ShowDialog(this) == DialogResult.OK) Run(OcrDocument.Open(dlg.FileName));
            };
            paste.Click += (s, e) => Paste();
            copy.Click += (s, e) => { if (_text.TextLength > 0) Clipboard.SetText(_text.Text); };
            send.Click += (s, e) =>
            {
                if (_doc == null) return;
                if (_doc.SourcePath != null) Shell.Workspace.AddPaths(new[] { _doc.SourcePath });
                Shell.Navigate("home");
            };
            _image.DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            _image.DragDrop += (s, e) =>
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] f && f.Length > 0 && OcrDocument.IsSupported(f[0])) Run(OcrDocument.Open(f[0]));
            };
            Load += (s, e) => split.SplitterDistance = split.Width / 2;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (Visible && keyData == (Keys.Control | Keys.V) && !_text.Focused)
            {
                Paste();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Paste()
        {
            if (Clipboard.ContainsImage())
                using (var img = Clipboard.GetImage())
                using (var bmp = new Bitmap(img))
                    Run(OcrDocument.FromBitmap(bmp, "Clipboard"));
            else if (Clipboard.ContainsFileDropList() && Clipboard.GetFileDropList().Count > 0)
                Run(OcrDocument.Open(Clipboard.GetFileDropList()[0]));
        }

        private async void Run(OcrDocument doc)
        {
            _doc?.Dispose();
            _doc = doc;
            var page = doc.Pages.FirstOrDefault();
            if (page == null) return;
            var o = Shell.Settings.Ocr.Clone();
            using (var bmp = await Task.Run(() => page.GetProcessedImage(o.PdfDpi)))
            {
                var old = _image.Image;
                _image.Result = null;
                _image.Image = new Bitmap(bmp);
                _image.PageDpi = page.EffectiveDpi(bmp, o.PdfDpi);
                old?.Dispose();
            }
            _text.Text = "";
            _info.Text = L.T("Recognizing…");
            try
            {
                var sw = Stopwatch.StartNew();
                var r = await Shell.Ocr.RecognizePageAsync(page, o, CancellationToken.None);
                if (_doc != doc) return;
                _image.Result = r;
                _image.Invalidate();
                _text.Text = r.GetPlainText().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                _info.Text = L.F("{0} lines · {1:P0} confidence · {2:0.0}s · {3}", r.Lines.Count, r.MeanConfidence, sw.Elapsed.TotalSeconds, L.T(LanguageCatalog.Get(o.Language).DisplayName));
            }
            catch (Exception ex)
            {
                _info.Text = L.T("Recognition failed: ") + ex.Message;
            }
        }
    }

    // ==================================================================== Batch

    /// <summary>Converts many files/folders unattended.</summary>
    internal sealed class BatchPage : PageBase
    {
        private readonly ListView _inputs;
        private readonly CheckBox _foldersAsSequence;
        private readonly ComboBox _format, _mode, _language;
        private readonly TextBox _output;
        private readonly Button _start;
        private readonly ProgressBar _progress;
        private readonly ListBox _log;
        private CancellationTokenSource _cts;

        public BatchPage(IShell shell) : base(shell, L.T("Batch Process"), L.T("Recognize and export many documents at once. Folders can be treated as one multi-page document."))
        {
            var inputButtons = new FlowLayoutPanel { Height = S(44) };
            var addFiles = FlatButton(L.T("Add files…"));
            var addFolder = FlatButton(L.T("Add folder…"));
            var clear = FlatButton(L.T("Clear list"), 110);
            inputButtons.Controls.AddRange(new Control[] { addFiles, addFolder, clear });

            _inputs = new ListView { View = View.Details, FullRowSelect = true, Height = S(220), AllowDrop = true, BorderStyle = BorderStyle.FixedSingle };
            _inputs.Columns.Add(L.T("Input"), S(520));
            _inputs.Columns.Add(L.T("Type"), S(140));
            _inputs.Columns.Add(L.T("Status"), S(260));
            _foldersAsSequence = new CheckBox { Text = L.T("Treat each folder as one document (image sequence)"), Checked = true, Height = S(34) };

            var options = new TableLayoutPanel { ColumnCount = 4, Height = S(84) };
            for (int i = 0; i < 4; i++) options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _format = Field.Combo(L.T("Microsoft Word (.docx)"), L.T("Microsoft Excel (.xlsx)"), L.T("HTML (.html)"), L.T("Plain text (.txt)"));
            _mode = Field.Combo(L.T("Editable Document"), L.T("Exact Copy"), L.T("Plain Text"));
            _language = Field.Combo(LanguageCatalog.All.Select(l => (object)L.T(l.DisplayName)).ToArray());
            _output = new TextBox { Dock = DockStyle.Top };
            options.Controls.Add(Labeled(L.T("Output format"), _format), 0, 0);
            options.Controls.Add(Labeled(L.T("Document type"), _mode), 1, 0);
            options.Controls.Add(Labeled(L.T("Language"), _language), 2, 0);
            var outBox = Labeled(L.T("Output folder (double-click to browse)"), _output);
            options.Controls.Add(outBox, 3, 0);

            var run = new FlowLayoutPanel { Height = S(50) };
            _start = new Button { Text = L.T("▶  Start"), Width = S(140), Height = S(38), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Font = Theme.Bold };
            _start.FlatAppearance.BorderSize = 0;
            _progress = new ProgressBar { Width = S(420), Height = S(20), Margin = new Padding(S(16), S(10), 0, 0) };
            run.Controls.AddRange(new Control[] { _start, _progress });

            _log = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, HorizontalScrollbar = true };
            Body.Controls.Add(_log);
            Stack(Body, Heading(L.T("1. Inputs")), inputButtons, _inputs, _foldersAsSequence, Heading(L.T("2. Options")), options, Heading(L.T("3. Run")), run);

            addFiles.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Multiselect = true, Filter = L.T("PDF and images|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff") })
                    if (dlg.ShowDialog(this) == DialogResult.OK) AddInputs(dlg.FileNames);
            };
            addFolder.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                    if (dlg.ShowDialog(this) == DialogResult.OK) AddInputs(new[] { dlg.SelectedPath });
            };
            clear.Click += (s, e) => { if (_cts == null) _inputs.Items.Clear(); };
            _inputs.DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            _inputs.DragDrop += (s, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] f) AddInputs(f); };
            _output.DoubleClick += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog { SelectedPath = _output.Text })
                    if (dlg.ShowDialog(this) == DialogResult.OK) _output.Text = dlg.SelectedPath;
            };
            _start.Click += async (s, e) => await StartAsync();
            VisibleChanged += (s, e) =>
            {
                if (!Visible || _cts != null) return;
                _format.SelectedIndex = (int)Shell.Settings.Format;
                _mode.SelectedIndex = (int)Shell.Settings.Mode;
                _language.SelectedIndex = Math.Max(0, LanguageCatalog.All.ToList().FindIndex(l => l.Language == Shell.Settings.Ocr.Language));
                if (string.IsNullOrEmpty(_output.Text)) _output.Text = Shell.Settings.OutputFolder;
            };
        }

        private Control Heading(string text) => new Label { Text = text, Height = S(34), Font = Theme.Heading, ForeColor = Theme.Text, TextAlign = ContentAlignment.BottomLeft };

        private Control Labeled(string caption, Control c)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, S(12), 0) };
            c.Dock = DockStyle.Top;
            p.Controls.Add(c);
            p.Controls.Add(new Label { Text = caption, Dock = DockStyle.Top, Height = S(26), TextAlign = ContentAlignment.BottomLeft, ForeColor = Theme.SubText });
            return p;
        }

        private void AddInputs(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    if (_foldersAsSequence.Checked && Directory.GetFiles(p).Any(OcrDocument.IsImage))
                        _inputs.Items.Add(new ListViewItem(new[] { p, L.T("Image sequence"), L.T("Waiting") }) { Tag = p });
                    foreach (var f in Directory.GetFiles(p).Where(f => OcrDocument.IsSupported(f) && (!_foldersAsSequence.Checked || !OcrDocument.IsImage(f))))
                        _inputs.Items.Add(new ListViewItem(new[] { f, Path.GetExtension(f).TrimStart('.').ToUpperInvariant(), L.T("Waiting") }) { Tag = f });
                }
                else if (OcrDocument.IsSupported(p))
                    _inputs.Items.Add(new ListViewItem(new[] { p, Path.GetExtension(p).TrimStart('.').ToUpperInvariant(), L.T("Waiting") }) { Tag = p });
            }
        }

        private void Log(string text)
        {
            _log.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + text);
            _log.TopIndex = Math.Max(0, _log.Items.Count - 1);
        }

        private async Task StartAsync()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                return;
            }
            var items = _inputs.Items.Cast<ListViewItem>().Where(i => i.SubItems[2].Text != L.T("Done")).ToList();
            if (items.Count == 0)
            {
                MessageBox.Show(this, L.T("Add files or folders first."), L.T("Batch Process"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var format = (ExportFormat)_format.SelectedIndex;
            var o = Shell.Settings.Ocr.Clone();
            o.Language = LanguageCatalog.All[_language.SelectedIndex].Language;
            var exportOptions = Shell.Settings.ExportOptions();
            exportOptions.Mode = (DocumentMode)_mode.SelectedIndex;
            exportOptions.Language = o.Language;
            string outDir = string.IsNullOrWhiteSpace(_output.Text) ? Shell.Settings.OutputFolder : _output.Text.Trim();
            try { Directory.CreateDirectory(outDir); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("Output folder"), MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            _cts = new CancellationTokenSource();
            _start.Text = L.T("■  Stop");
            _start.BackColor = Theme.Danger;
            _progress.Value = 0;
            int ok = 0, failed = 0;
            var total = Stopwatch.StartNew();
            Log(L.F("Started: {0} document(s) → {1} in {2}", items.Count, format.ToString().ToUpperInvariant(), outDir));
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var item = items[i];
                    var path = (string)item.Tag;
                    item.SubItems[2].Text = L.T("Recognizing…");
                    item.EnsureVisible();
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        using (var doc = Directory.Exists(path) ? OcrDocument.OpenImageSequence(Directory.GetFiles(path), Path.GetFileName(path)) : OcrDocument.Open(path))
                        {
                            var progress = new Progress<PageProgress>(p => { if (p.Total > 0) item.SubItems[2].Text = L.F("Page {0}/{1}", Math.Min(p.Done + 1, p.Total), p.Total); });
                            await Shell.Ocr.RecognizeAsync(doc.Pages, o, progress, null, _cts.Token);
                            var target = Exporter.UniquePath(outDir, Path.GetFileNameWithoutExtension(doc.Name), Exporter.Extension(format));
                            await Task.Run(() => Exporter.Export(doc, format, target, exportOptions));
                            item.SubItems[2].Text = L.T("Done");
                            Log($"✓ {doc.Name} ({doc.Pages.Count} p., {sw.Elapsed.TotalSeconds:0.0}s) → {Path.GetFileName(target)}");
                            Shell.History.Add(new HistoryEntry { Time = DateTime.Now, Source = path, Output = target, Format = format.ToString().ToUpperInvariant(), Pages = doc.Pages.Count, Language = o.Language.ToString(), Seconds = sw.Elapsed.TotalSeconds });
                            ok++;
                        }
                    }
                    catch (OperationCanceledException) { item.SubItems[2].Text = L.T("Stopped"); throw; }
                    catch (Exception ex)
                    {
                        item.SubItems[2].Text = L.T("Failed");
                        Log($"✗ {path}: {ex.Message}");
                        failed++;
                    }
                    _progress.Value = (int)((i + 1) * 100L / items.Count);
                    Shell.SetStatus(L.F("Batch: {0}/{1} documents", i + 1, items.Count), _progress.Value);
                }
                Log(L.F("Finished: {0} succeeded, {1} failed, {2:0}s.", ok, failed, total.Elapsed.TotalSeconds));
            }
            catch (OperationCanceledException)
            {
                Log(L.T("Stopped by user."));
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _start.Text = L.T("▶  Start");
                _start.BackColor = Theme.Accent;
                Shell.SetStatus(L.F("Batch finished: {0} succeeded, {1} failed.", ok, failed));
            }
            if (ok > 0 && MessageBox.Show(this, L.F("{0} document(s) exported. Open the output folder?", ok), L.T("Batch Process"), MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                Process.Start("explorer.exe", "\"" + outDir + "\"");
        }
    }

    // ==================================================================== History

    internal sealed class HistoryPage : PageBase, IActivatable
    {
        private readonly ListView _list;

        public HistoryPage(IShell shell) : base(shell, L.T("History"), L.T("Recent recognitions and exports."))
        {
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = S(46) };
            var open = FlatButton(L.T("Open output"));
            var folder = FlatButton(L.T("Show in folder"));
            var reopen = FlatButton(L.T("Open source in workspace"), 190);
            var clear = FlatButton(L.T("Clear history"));
            buttons.Controls.AddRange(new Control[] { open, folder, reopen, clear });
            _list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BorderStyle = BorderStyle.None };
            foreach (var c in new[] { (L.T("Time"), 150), (L.T("Source"), 380), (L.T("Output"), 380), (L.T("Format"), 100), (L.T("Pages"), 60), (L.T("Language"), 90), (L.T("Duration"), 80) })
                _list.Columns.Add(c.Item1, S(c.Item2));
            Body.Controls.Add(_list);
            Body.Controls.Add(buttons);

            open.Click += (s, e) => { var h = Selected(); if (h?.Output != null && File.Exists(h.Output)) Process.Start(new ProcessStartInfo(h.Output) { UseShellExecute = true }); };
            _list.DoubleClick += (s, e) => { var h = Selected(); if (h?.Output != null && File.Exists(h.Output)) Process.Start(new ProcessStartInfo(h.Output) { UseShellExecute = true }); };
            folder.Click += (s, e) => { var h = Selected(); if (h?.Output != null && File.Exists(h.Output)) Process.Start("explorer.exe", "/select,\"" + h.Output + "\""); };
            reopen.Click += (s, e) =>
            {
                var h = Selected();
                if (h?.Source == null || !(File.Exists(h.Source) || Directory.Exists(h.Source))) return;
                Shell.Workspace.AddPaths(new[] { h.Source });
                Shell.Navigate("home");
            };
            clear.Click += (s, e) =>
            {
                if (MessageBox.Show(this, L.T("Clear the whole history?"), L.T("History"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) Shell.History.Clear();
            };
            Shell.History.Changed += (s, e) => { if (Visible) Reload(); };
        }

        public void OnActivated() => Reload();

        private HistoryEntry Selected() => _list.SelectedItems.Count == 0 ? null : (HistoryEntry)_list.SelectedItems[0].Tag;

        private void Reload()
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var h in Shell.History.Entries)
            {
                _list.Items.Add(new ListViewItem(new[]
                {
                    h.Time.ToString("yyyy-MM-dd HH:mm"),
                    h.Source ?? "",
                    h.Output ?? "—",
                    h.Format == "Recognition" ? L.T("Recognition") : h.Format ?? "", // stored in English
                    h.Pages.ToString(),
                    h.Language ?? "",
                    h.Seconds.ToString("0.0") + "s"
                }) { Tag = h });
            }
            _list.EndUpdate();
        }
    }

    // ==================================================================== Settings

    internal sealed class SettingsPage : PageBase, IActivatable
    {
        private readonly ComboBox _language, _layout, _mode, _format, _uiLanguage, _defaultFont;
        private readonly CheckBox _tables, _keepColors, _images, _openAfter, _autoRecognize, _confidence;
        private readonly TextBox _output;
        private readonly OcrOptionsEditor _advanced;
        // OCR model information is hidden on the Settings screen (kept for diagnostics; re-enable by uncommenting).
        // private readonly Label _models;
        private Label _license;

        private void ShowLicense()
        {
            var l = Program.License;
            string state = Program.IsTrial ? "⏳ " + Program.Trial.Message
                : l == null ? "" : (l.IsValid ? "✓ " : "✗ ") + l.Message;
            _license.Text = state + Environment.NewLine + L.T("Machine code: ") + Licensing.MachineIdentity.Code;
        }

        public SettingsPage(IShell shell) : base(shell, L.T("Settings"), L.T("Defaults for recognition and export. Everything runs offline on this computer."))
        {
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Location = new Point(0, 0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(240)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(420)));

            _language = Combo(LanguageCatalog.All.Select(l => (object)L.T(l.DisplayName)).ToArray());
            _uiLanguage = Combo(L.Languages.Select(l => (object)l.NativeName).ToArray());
            // Default font: first entry = automatic (the font of the recognition language), then installed fonts.
            _defaultFont = Combo(new object[] { L.T("Automatic (by recognition language)") }.Concat(WorkspacePage.FontFamilies()).ToArray());
            _defaultFont.MaxDropDownItems = 20;
            _layout = Combo(L.T("Automatic (columns, tables, figures)"), L.T("Single column"), L.T("Text lines only"));
            _mode = Combo(L.T("Editable Document (Recommended)"), L.T("Exact Copy (keep positions)"), L.T("Plain Text"));
            _format = Combo(L.T("Microsoft Word (.docx)"), L.T("Microsoft Excel (.xlsx)"), L.T("HTML (.html)"), L.T("Plain text (.txt)"));
            _tables = Check(L.T("Detect tables and columns"));
            _keepColors = Check(L.T("Keep text, fill and page colors"));
            _images = Check(L.T("Include pictures in exported documents"));
            _openAfter = Check(L.T("Open the document after export"));
            _autoRecognize = Check(L.T("Recognize automatically when files are added"));
            _confidence = Check(L.T("Highlight uncertain characters in the results view"));
            _output = new TextBox { Width = S(400) };
            _advanced = new OcrOptionsEditor();
            // _models = new Label { AutoSize = true, ForeColor = Theme.SubText, MaximumSize = new Size(S(660), 0) };

            int row = 0;
            void Section(string title)
            {
                var l = new Label { Text = title, Font = Theme.Heading, AutoSize = true, ForeColor = Theme.Accent, Margin = new Padding(0, row == 0 ? 0 : S(18), 0, S(6)) };
                grid.Controls.Add(l, 0, row);
                grid.SetColumnSpan(l, 2);
                row++;
            }
            void Row(string caption, Control c)
            {
                c.Margin = new Padding(3, 4, 3, 4);
                grid.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, 0, row);
                grid.Controls.Add(c, 1, row);
                row++;
            }
            void Span(Control c)
            {
                grid.Controls.Add(c, 0, row);
                grid.SetColumnSpan(c, 2);
                row++;
            }

            Section(L.T("General"));
            Row(L.T("Interface language:"), _uiLanguage);
            Section(L.T("Recognition"));
            Row(L.T("Default language:"), _language);
            Row(L.T("Layout analysis:"), _layout);
            Row("", _tables);
            Row("", _autoRecognize);
            Row("", _confidence);
            Row(L.T("Default font:"), _defaultFont);
            Row("", new Label { Text = L.T("Used for recognized text when the original font is unknown (scans, images). Applies to pages recognized afterwards."), AutoSize = true, MaximumSize = new Size(S(420), 0), ForeColor = Theme.SubText });
            Section(L.T("Export"));
            Row(L.T("Default output format:"), _format);
            Row(L.T("Document type:"), _mode);
            Row(L.T("Output folder:"), _output);
            Row("", _keepColors);
            Row("", _images);
            Row("", _openAfter);
            Section(L.T("Advanced recognition"));
            Span(_advanced);
            // Section(L.T("OCR models (offline)"));   // hidden: OCR model information
            // Span(_models);
            Section("License");
            _license = new Label { AutoSize = true, MaximumSize = new Size(S(660), 0), ForeColor = Theme.Text };
            var licenseButtons = new FlowLayoutPanel { AutoSize = true };
            var changeKey = FlatButton(L.T("Change license key…"), 170);
            var machineCode = FlatButton(L.T("Copy machine code"), 170);
            licenseButtons.Controls.AddRange(new Control[] { changeKey, machineCode });
            Span(_license);
            Span(licenseButtons);
            changeKey.Click += (s, e) =>
            {
                ((MainForm)FindForm()).ShowActivation();
                ShowLicense();
            };
            machineCode.Click += (s, e) => { Clipboard.SetText(Licensing.MachineIdentity.Code); Shell.SetStatus(L.T("Machine code copied: ") + Licensing.MachineIdentity.Code); };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = S(50), FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, S(8), 0, 0) };
            var save = new Button { Text = L.T("Save"), Width = S(120), Height = S(36), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Font = Theme.Bold };
            save.FlatAppearance.BorderSize = 0;
            var reset = FlatButton(L.T("Restore defaults"), 150);
            var dataFolder = FlatButton(L.T("Open data folder"), 150);
            buttons.Controls.AddRange(new Control[] { save, reset, dataFolder });

            scroll.Controls.Add(grid);
            Body.Controls.Add(scroll);
            Body.Controls.Add(buttons);

            save.Click += (s, e) => Save();
            reset.Click += (s, e) =>
            {
                if (MessageBox.Show(this, L.T("Restore all settings to their defaults?"), L.T("Settings"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                var d = new AppSettings { OutputFolder = AppPaths.DefaultOutput };
                LoadFrom(d);
            };
            dataFolder.Click += (s, e) => Process.Start("explorer.exe", "\"" + AppPaths.DataDir + "\"");
            _output.DoubleClick += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog { SelectedPath = _output.Text })
                    if (dlg.ShowDialog(this) == DialogResult.OK) _output.Text = dlg.SelectedPath;
            };
        }

        private ComboBox Combo(params object[] items)
        {
            var c = Field.Combo(items);
            c.Dock = DockStyle.None;
            c.Width = S(300);
            return c;
        }

        private static CheckBox Check(string text) => new CheckBox { Text = text, AutoSize = true };

        public void OnActivated()
        {
            LoadFrom(Shell.Settings);
            // OCR model information (hidden):
            // var dir = LanguageCatalog.DefaultModelDirectory;
            // var lines = new List<string> { L.T("Location: ") + dir };
            // foreach (var f in new[] { LanguageCatalog.DetModel, LanguageCatalog.ClsModel }.Concat(LanguageCatalog.All.Select(l => l.RecModel)).Distinct())
            // {
            //     var p = Path.Combine(dir, f);
            //     lines.Add((File.Exists(p) ? "✓ " : L.T("✗ missing  ")) + f + (File.Exists(p) ? $"  ({new FileInfo(p).Length / 1048576.0:0.0} MB)" : ""));
            // }
            // lines.Add(L.T("Languages: ") + string.Join(", ", LanguageCatalog.All.Select(l => L.T(l.DisplayName))));
            // _models.Text = string.Join(Environment.NewLine, lines);
            ShowLicense();
        }

        private void LoadFrom(AppSettings s)
        {
            _language.SelectedIndex = Math.Max(0, LanguageCatalog.All.ToList().FindIndex(l => l.Language == s.Ocr.Language));
            _uiLanguage.SelectedIndex = Math.Max(0, L.Languages.ToList().FindIndex(l => l.Code == (s.UiLanguage ?? L.Current)));
            _layout.SelectedIndex = (int)s.Ocr.Layout;
            _tables.Checked = s.Ocr.DetectTables;
            _autoRecognize.Checked = s.AutoRecognize;
            _confidence.Checked = s.ShowConfidence;
            _format.SelectedIndex = (int)s.Format;
            _mode.SelectedIndex = (int)s.Mode;
            _output.Text = s.OutputFolder;
            _keepColors.Checked = s.KeepColors;
            _images.Checked = s.IncludeImages;
            _openAfter.Checked = s.OpenAfterExport;
            _advanced.LoadFrom(s.Ocr);
            int fi = string.IsNullOrWhiteSpace(s.Ocr.DefaultFont) ? -1 : _defaultFont.Items.IndexOf(s.Ocr.DefaultFont);
            _defaultFont.SelectedIndex = fi > 0 ? fi : 0;
        }

        private void Save()
        {
            var s = Shell.Settings;
            s.Ocr.Language = LanguageCatalog.All[_language.SelectedIndex].Language;
            s.Ocr.Layout = (LayoutMode)_layout.SelectedIndex;
            s.Ocr.DetectTables = _tables.Checked;
            s.AutoRecognize = _autoRecognize.Checked;
            s.ShowConfidence = _confidence.Checked;
            s.Format = (ExportFormat)_format.SelectedIndex;
            s.Mode = (DocumentMode)_mode.SelectedIndex;
            s.OutputFolder = string.IsNullOrWhiteSpace(_output.Text) ? AppPaths.DefaultOutput : _output.Text.Trim();
            s.KeepColors = _keepColors.Checked;
            s.IncludeImages = _images.Checked;
            s.OpenAfterExport = _openAfter.Checked;
            _advanced.SaveTo(s.Ocr);
            s.Ocr.DefaultFont = _defaultFont.SelectedIndex > 0 ? _defaultFont.SelectedItem as string : null;
            s.Save();
            Shell.Workspace.LoadSettingsIntoControls();
            Shell.SetStatus(L.T("Settings saved."));
            var ui = L.Languages[Math.Max(0, _uiLanguage.SelectedIndex)].Code;
            if (ui != L.Current)
            {
                s.UiLanguage = ui;
                s.Save();
                // Texts are created with the windows: switching languages needs a restart.
                if (MessageBox.Show(this, L.T("The interface language is changed after a restart. Restart Falcon OCR now?"), L.T("Interface language"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Application.Restart();
            }
        }
    }
}
