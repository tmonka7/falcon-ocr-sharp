using System;
using FalconOcr.Localization;
using System.Drawing;
using System.Windows.Forms;
using FalconOcr.App.Services;
using FalconOcr.App.UI;
using FalconOcr.Engine;

namespace FalconOcr.App.Pages
{
    /// <summary>Editor for the advanced recognition parameters of <see cref="OcrOptions"/>.</summary>
    internal sealed class OcrOptionsEditor : TableLayoutPanel
    {
        private readonly ComboBox _pdfText = Field.Combo(L.T("Use the PDF text layer when present (fast, exact)"), L.T("Always run OCR (ignore embedded text)"));
        private readonly NumericUpDown _pdfDpi = Num(100, 600, 200, 25);
        private readonly NumericUpDown _detMax = Num(960, 6000, 2560, 160);
        private readonly NumericUpDown _detThr = Num(0.05m, 0.9m, 0.3m, 0.05m, 2);
        private readonly NumericUpDown _boxThr = Num(0.1m, 0.95m, 0.6m, 0.05m, 2);
        private readonly NumericUpDown _unclip = Num(1.0m, 3.0m, 1.5m, 0.1m, 1);
        private readonly NumericUpDown _minConf = Num(0m, 0.95m, 0.5m, 0.05m, 2);
        private readonly NumericUpDown _threads = Num(0, 64, 0, 1);
        private readonly CheckBox _cls = new CheckBox { Text = L.T("Correct upside-down text lines (orientation classifier)"), AutoSize = true };
        private readonly CheckBox _figures = new CheckBox { Text = L.T("Keep pictures and graphics as images"), AutoSize = true };

        public OcrOptionsEditor()
        {
            ColumnCount = 2;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
            Font = Theme.Base;
            _pdfText.Dock = DockStyle.None;
            _pdfText.Width = 360;
            Row(L.T("PDF input:"), _pdfText);
            Row(L.T("PDF rendering resolution (dpi):"), _pdfDpi);
            Row(L.T("Detector max. image side (px):"), _detMax);
            Row(L.T("Text pixel threshold:"), _detThr);
            Row(L.T("Text box threshold:"), _boxThr);
            Row(L.T("Box expansion (unclip ratio):"), _unclip);
            Row(L.T("Minimum line confidence:"), _minConf);
            Row(L.T("CPU threads (0 = automatic):"), _threads);
            Row("", _cls);
            Row("", _figures);
        }

        private void Row(string caption, Control c)
        {
            c.Margin = new Padding(3, 5, 3, 5);
            Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.Text, Margin = new Padding(3, 9, 3, 3) });
            Controls.Add(c);
        }

        private static NumericUpDown Num(decimal min, decimal max, decimal value, decimal step, int decimals = 0)
            => new NumericUpDown { Minimum = min, Maximum = max, Value = value, Increment = step, DecimalPlaces = decimals, Width = 100 };

        public void LoadFrom(OcrOptions o)
        {
            _pdfText.SelectedIndex = (int)o.PdfText;
            _pdfDpi.Value = Clamp(_pdfDpi, o.PdfDpi);
            _detMax.Value = Clamp(_detMax, o.DetMaxSide);
            _detThr.Value = Clamp(_detThr, (decimal)o.DetThreshold);
            _boxThr.Value = Clamp(_boxThr, (decimal)o.BoxThreshold);
            _unclip.Value = Clamp(_unclip, (decimal)o.UnclipRatio);
            _minConf.Value = Clamp(_minConf, (decimal)o.MinConfidence);
            _threads.Value = Clamp(_threads, o.Threads);
            _cls.Checked = o.UseAngleClassifier;
            _figures.Checked = o.DetectFigures;
        }

        public void SaveTo(OcrOptions o)
        {
            o.PdfText = (PdfTextMode)_pdfText.SelectedIndex;
            o.PdfDpi = (int)_pdfDpi.Value;
            o.DetMaxSide = (int)_detMax.Value;
            o.DetThreshold = (float)_detThr.Value;
            o.BoxThreshold = (float)_boxThr.Value;
            o.UnclipRatio = (float)_unclip.Value;
            o.MinConfidence = (float)_minConf.Value;
            o.Threads = (int)_threads.Value;
            o.UseAngleClassifier = _cls.Checked;
            o.DetectFigures = _figures.Checked;
        }

        private static decimal Clamp(NumericUpDown n, decimal v) => Math.Max(n.Minimum, Math.Min(n.Maximum, v));
    }

    internal sealed class AdvancedSettingsDialog : Form
    {
        public AdvancedSettingsDialog(AppSettings settings)
        {
            Text = L.T("Advanced OCR Settings");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            Font = Theme.Base;
            BackColor = Color.White;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);

            var editor = new OcrOptionsEditor { Dock = DockStyle.Top };
            editor.LoadFrom(settings.Ocr);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 12, 0, 0) };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90, Height = 30 };
            var cancel = new Button { Text = L.T("Cancel"), DialogResult = DialogResult.Cancel, Width = 90, Height = 30 };
            var defaults = new Button { Text = L.T("Defaults"), Width = 90, Height = 30 };
            defaults.Click += (s, e) => editor.LoadFrom(new OcrOptions());
            buttons.Controls.AddRange(new Control[] { cancel, ok, defaults });
            Controls.Add(buttons);
            Controls.Add(editor);
            AcceptButton = ok;
            CancelButton = cancel;
            ok.Click += (s, e) => editor.SaveTo(settings.Ocr);
        }
    }

    /// <summary>Single-line text input dialog.</summary>
    internal static class Prompt
    {
        public static string Show(IWin32Window owner, string title, string caption, string value)
        {
            using (var f = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(420, 120),
                ShowInTaskbar = false,
                Font = Theme.Base
            })
            {
                var label = new Label { Text = caption, Location = new Point(14, 14), AutoSize = true };
                var box = new TextBox { Text = value, Location = new Point(14, 38), Width = 390 };
                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(228, 78), Width = 85 };
                var cancel = new Button { Text = L.T("Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(319, 78), Width = 85 };
                f.Controls.AddRange(new Control[] { label, box, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                return f.ShowDialog(owner) == DialogResult.OK ? box.Text : null;
            }
        }
    }
}
