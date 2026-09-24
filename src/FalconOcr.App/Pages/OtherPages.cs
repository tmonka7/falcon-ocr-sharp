using System;
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

        public QuickOcrPage(IShell shell) : base(shell, "Quick OCR", "Paste (Ctrl+V), drop or open an image to get its text instantly — nothing is saved.")
        {
            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = S(12), BackColor = Theme.Panel };
            _image = new ImageViewer { Dock = DockStyle.Fill, Placeholder = "Drop an image here or press Ctrl+V", AllowDrop = true, ShowOverlay = true };
            _text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Segoe UI", 11f), BorderStyle = BorderStyle.FixedSingle };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = S(46) };
            var open = FlatButton("Open image…");
            var paste = FlatButton("Paste image");
            var copy = FlatButton("Copy text");
            var send = FlatButton("Open in workspace", 160);
            buttons.Controls.AddRange(new Control[] { open, paste, copy, send });
            _info = new Label { Dock = DockStyle.Bottom, Height = S(28), ForeColor = Theme.SubText, TextAlign = ContentAlignment.MiddleLeft };
            split.Panel1.Controls.Add(_image);
            split.Panel2.Controls.Add(_text);
            Body.Controls.Add(split);
            Body.Controls.Add(_info);
            Body.Controls.Add(buttons);

            open.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = "Images and PDF|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.pdf" })
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
            _info.Text = "Recognizing…";
            try
            {
                var sw = Stopwatch.StartNew();
                var r = await Shell.Ocr.RecognizePageAsync(page, o, CancellationToken.None);
                if (_doc != doc) return;
                _image.Result = r;
                _image.Invalidate();
                _text.Text = r.GetPlainText().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                _info.Text = $"{r.Lines.Count} lines · {r.MeanConfidence:P0} confidence · {sw.Elapsed.TotalSeconds:0.0}s · {LanguageCatalog.Get(o.Language).DisplayName}";
            }
            catch (Exception ex)
            {
                _info.Text = "Recognition failed: " + ex.Message;
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

        public BatchPage(IShell shell) : base(shell, "Batch Process", "Recognize and export many documents at once. Folders can be treated as one multi-page document.")
        {
            var inputButtons = new FlowLayoutPanel { Height = S(44) };
            var addFiles = FlatButton("Add files…");
            var addFolder = FlatButton("Add folder…");
            var clear = FlatButton("Clear list", 110);
            inputButtons.Controls.AddRange(new Control[] { addFiles, addFolder, clear });

            _inputs = new ListView { View = View.Details, FullRowSelect = true, Height = S(220), AllowDrop = true, BorderStyle = BorderStyle.FixedSingle };
            _inputs.Columns.Add("Input", S(520));
            _inputs.Columns.Add("Type", S(140));
            _inputs.Columns.Add("Status", S(260));
            _foldersAsSequence = new CheckBox { Text = "Treat each folder as one document (image sequence)", Checked = true, Height = S(34) };

            var options = new TableLayoutPanel { ColumnCount = 4, Height = S(84) };
            for (int i = 0; i < 4; i++) options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _format = Field.Combo("Microsoft Word (.docx)", "Microsoft Excel (.xlsx)", "HTML (.html)", "Plain text (.txt)");
            _mode = Field.Combo("Editable Document", "Exact Copy", "Plain Text");
            _language = Field.Combo(LanguageCatalog.All.Select(l => (object)l.DisplayName).ToArray());
            _output = new TextBox { Dock = DockStyle.Top };
            options.Controls.Add(Labeled("Output format", _format), 0, 0);
            options.Controls.Add(Labeled("Document type", _mode), 1, 0);
            options.Controls.Add(Labeled("Language", _language), 2, 0);
            var outBox = Labeled("Output folder (double-click to browse)", _output);
            options.Controls.Add(outBox, 3, 0);

            var run = new FlowLayoutPanel { Height = S(50) };
            _start = new Button { Text = "▶  Start", Width = S(140), Height = S(38), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Font = Theme.Bold };
            _start.FlatAppearance.BorderSize = 0;
            _progress = new ProgressBar { Width = S(420), Height = S(20), Margin = new Padding(S(16), S(10), 0, 0) };
            run.Controls.AddRange(new Control[] { _start, _progress });

            _log = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false, HorizontalScrollbar = true };
            Body.Controls.Add(_log);
            Stack(Body, Heading("1. Inputs"), inputButtons, _inputs, _foldersAsSequence, Heading("2. Options"), options, Heading("3. Run"), run);

            addFiles.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Multiselect = true, Filter = "PDF and images|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff" })
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
                        _inputs.Items.Add(new ListViewItem(new[] { p, "Image sequence", "Waiting" }) { Tag = p });
                    foreach (var f in Directory.GetFiles(p).Where(f => OcrDocument.IsSupported(f) && (!_foldersAsSequence.Checked || !OcrDocument.IsImage(f))))
                        _inputs.Items.Add(new ListViewItem(new[] { f, Path.GetExtension(f).TrimStart('.').ToUpperInvariant(), "Waiting" }) { Tag = f });
                }
                else if (OcrDocument.IsSupported(p))
                    _inputs.Items.Add(new ListViewItem(new[] { p, Path.GetExtension(p).TrimStart('.').ToUpperInvariant(), "Waiting" }) { Tag = p });
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
            var items = _inputs.Items.Cast<ListViewItem>().Where(i => i.SubItems[2].Text != "Done").ToList();
            if (items.Count == 0)
            {
                MessageBox.Show(this, "Add files or folders first.", "Batch Process", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Output folder", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            _cts = new CancellationTokenSource();
            _start.Text = "■  Stop";
            _start.BackColor = Theme.Danger;
            _progress.Value = 0;
            int ok = 0, failed = 0;
            var total = Stopwatch.StartNew();
            Log($"Started: {items.Count} document(s) → {format.ToString().ToUpperInvariant()} in {outDir}");
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var item = items[i];
                    var path = (string)item.Tag;
                    item.SubItems[2].Text = "Recognizing…";
                    item.EnsureVisible();
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        using (var doc = Directory.Exists(path) ? OcrDocument.OpenImageSequence(Directory.GetFiles(path), Path.GetFileName(path)) : OcrDocument.Open(path))
                        {
                            var progress = new Progress<PageProgress>(p => { if (p.Total > 0) item.SubItems[2].Text = $"Page {Math.Min(p.Done + 1, p.Total)}/{p.Total}"; });
                            await Shell.Ocr.RecognizeAsync(doc.Pages, o, progress, null, _cts.Token);
                            var target = Exporter.UniquePath(outDir, Path.GetFileNameWithoutExtension(doc.Name), Exporter.Extension(format));
                            await Task.Run(() => Exporter.Export(doc, format, target, exportOptions));
                            item.SubItems[2].Text = "Done";
                            Log($"✓ {doc.Name} ({doc.Pages.Count} p., {sw.Elapsed.TotalSeconds:0.0}s) → {Path.GetFileName(target)}");
                            Shell.History.Add(new HistoryEntry { Time = DateTime.Now, Source = path, Output = target, Format = format.ToString().ToUpperInvariant(), Pages = doc.Pages.Count, Language = o.Language.ToString(), Seconds = sw.Elapsed.TotalSeconds });
                            ok++;
                        }
                    }
                    catch (OperationCanceledException) { item.SubItems[2].Text = "Stopped"; throw; }
                    catch (Exception ex)
                    {
                        item.SubItems[2].Text = "Failed";
                        Log($"✗ {path}: {ex.Message}");
                        failed++;
                    }
                    _progress.Value = (int)((i + 1) * 100L / items.Count);
                    Shell.SetStatus($"Batch: {i + 1}/{items.Count} documents", _progress.Value);
                }
                Log($"Finished: {ok} succeeded, {failed} failed, {total.Elapsed.TotalSeconds:0}s.");
            }
            catch (OperationCanceledException)
            {
                Log("Stopped by user.");
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _start.Text = "▶  Start";
                _start.BackColor = Theme.Accent;
                Shell.SetStatus($"Batch finished: {ok} succeeded, {failed} failed.");
            }
            if (ok > 0 && MessageBox.Show(this, $"{ok} document(s) exported. Open the output folder?", "Batch Process", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                Process.Start("explorer.exe", "\"" + outDir + "\"");
        }
    }

    // ==================================================================== History

    internal sealed class HistoryPage : PageBase, IActivatable
    {
        private readonly ListView _list;

        public HistoryPage(IShell shell) : base(shell, "History", "Recent recognitions and exports.")
        {
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = S(46) };
            var open = FlatButton("Open output");
            var folder = FlatButton("Show in folder");
            var reopen = FlatButton("Open source in workspace", 190);
            var clear = FlatButton("Clear history");
            buttons.Controls.AddRange(new Control[] { open, folder, reopen, clear });
            _list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BorderStyle = BorderStyle.None };
            foreach (var c in new[] { ("Time", 150), ("Source", 380), ("Output", 380), ("Format", 100), ("Pages", 60), ("Language", 90), ("Duration", 80) })
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
                if (MessageBox.Show(this, "Clear the whole history?", "History", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) Shell.History.Clear();
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
                    h.Format ?? "",
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
        private readonly ComboBox _language, _layout, _mode, _format;
        private readonly CheckBox _tables, _keepColors, _images, _openAfter, _autoRecognize, _confidence;
        private readonly TextBox _output;
        private readonly OcrOptionsEditor _advanced;
        private readonly Label _models;
        private Label _license;

        private void ShowLicense()
        {
            var l = Program.License;
            string state = Program.IsTrial ? "⏳ " + Program.Trial.Message
                : l == null ? "" : (l.IsValid ? "✓ " : "✗ ") + l.Message;
            _license.Text = state + Environment.NewLine + "Machine code: " + Licensing.MachineIdentity.Code;
        }

        public SettingsPage(IShell shell) : base(shell, "Settings", "Defaults for recognition and export. Everything runs offline with the bundled PaddleOCR models.")
        {
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Location = new Point(0, 0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(240)));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(420)));

            _language = Combo(LanguageCatalog.All.Select(l => (object)l.DisplayName).ToArray());
            _layout = Combo("Automatic (columns, tables, figures)", "Single column", "Text lines only");
            _mode = Combo("Editable Document (Recommended)", "Exact Copy (keep positions)", "Plain Text");
            _format = Combo("Microsoft Word (.docx)", "Microsoft Excel (.xlsx)", "HTML (.html)", "Plain text (.txt)");
            _tables = Check("Detect tables and columns");
            _keepColors = Check("Keep text, fill and page colors");
            _images = Check("Include pictures in exported documents");
            _openAfter = Check("Open the document after export");
            _autoRecognize = Check("Recognize automatically when files are added");
            _confidence = Check("Highlight uncertain characters in the results view");
            _output = new TextBox { Width = S(400) };
            _advanced = new OcrOptionsEditor();
            _models = new Label { AutoSize = true, ForeColor = Theme.SubText, MaximumSize = new Size(S(660), 0) };

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

            Section("Recognition");
            Row("Default language:", _language);
            Row("Layout analysis:", _layout);
            Row("", _tables);
            Row("", _autoRecognize);
            Row("", _confidence);
            Section("Export");
            Row("Default output format:", _format);
            Row("Document type:", _mode);
            Row("Output folder:", _output);
            Row("", _keepColors);
            Row("", _images);
            Row("", _openAfter);
            Section("Advanced recognition");
            Span(_advanced);
            Section("OCR models (offline)");
            Span(_models);
            Section("License");
            _license = new Label { AutoSize = true, MaximumSize = new Size(S(660), 0), ForeColor = Theme.Text };
            var licenseButtons = new FlowLayoutPanel { AutoSize = true };
            var changeKey = FlatButton("Change license key…", 170);
            var machineCode = FlatButton("Copy machine code", 170);
            licenseButtons.Controls.AddRange(new Control[] { changeKey, machineCode });
            Span(_license);
            Span(licenseButtons);
            changeKey.Click += (s, e) =>
            {
                ((MainForm)FindForm()).ShowActivation();
                ShowLicense();
            };
            machineCode.Click += (s, e) => { Clipboard.SetText(Licensing.MachineIdentity.Code); Shell.SetStatus("Machine code copied: " + Licensing.MachineIdentity.Code); };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = S(50), FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, S(8), 0, 0) };
            var save = new Button { Text = "Save", Width = S(120), Height = S(36), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Font = Theme.Bold };
            save.FlatAppearance.BorderSize = 0;
            var reset = FlatButton("Restore defaults", 150);
            var dataFolder = FlatButton("Open data folder", 150);
            buttons.Controls.AddRange(new Control[] { save, reset, dataFolder });

            scroll.Controls.Add(grid);
            Body.Controls.Add(scroll);
            Body.Controls.Add(buttons);

            save.Click += (s, e) => Save();
            reset.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Restore all settings to their defaults?", "Settings", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
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
            var dir = LanguageCatalog.DefaultModelDirectory;
            var lines = new List<string> { "Location: " + dir };
            foreach (var f in new[] { LanguageCatalog.DetModel, LanguageCatalog.ClsModel }.Concat(LanguageCatalog.All.Select(l => l.RecModel)).Distinct())
            {
                var p = Path.Combine(dir, f);
                lines.Add((File.Exists(p) ? "✓ " : "✗ missing  ") + f + (File.Exists(p) ? $"  ({new FileInfo(p).Length / 1048576.0:0.0} MB)" : ""));
            }
            lines.Add("Languages: " + string.Join(", ", LanguageCatalog.All.Select(l => l.DisplayName)));
            _models.Text = string.Join(Environment.NewLine, lines);
            ShowLicense();
        }

        private void LoadFrom(AppSettings s)
        {
            _language.SelectedIndex = Math.Max(0, LanguageCatalog.All.ToList().FindIndex(l => l.Language == s.Ocr.Language));
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
            s.Save();
            Shell.Workspace.LoadSettingsIntoControls();
            Shell.SetStatus("Settings saved.");
        }
    }
}
