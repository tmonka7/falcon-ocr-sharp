using System;
using FalconOcr.Localization;
using System.Drawing;
using System.Windows.Forms;
using FalconOcr.App.UI;
using FalconOcr.Licensing;

namespace FalconOcr.App.Pages
{
    /// <summary>
    /// License activation: shows the machine code and accepts a key (typed, pasted or from a .lic file).
    /// During the 7-day trial it also offers "Continue Trial" (DialogResult.Ignore).
    /// </summary>
    internal sealed class ActivationForm : Form
    {
        private readonly TextBox _key;
        private readonly Label _status;

        public LicenseCheck Result { get; private set; }

        public ActivationForm(LicenseCheck current, bool startup, TrialStatus trial = null)
        {
            bool trialOpen = trial != null && !trial.Expired;
            Text = L.T("Activate Falcon OCR");
            Font = Theme.Base;
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = startup ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
            ShowInTaskbar = startup;
            ClientSize = new Size(620, 530);
            Icon = Theme.AppIcon(32);
            var logo = Theme.AppIconBitmap(48);

            string subtitle = trial == null ? L.T("Enter your license key.")
                : trialOpen ? L.F("You are using the trial version — {0} of {1} days left.", trial.DaysLeft, trial.TotalDays)
                : L.T("Your trial period has ended. A license key is required to continue.");
            var header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Theme.Header };
            header.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(logo, new Rectangle(16, 16, 44, 44));
                TextRenderer.DrawText(e.Graphics, L.T("Activate Falcon OCR"), Theme.Semibold(16f), new Point(70, 12), Color.White);
                TextRenderer.DrawText(e.Graphics, subtitle, Theme.Base, new Point(72, 46), Color.FromArgb(220, 243, 233));
            };

            // Trial banner.
            var banner = new Panel { Location = new Point(20, 90), Size = new Size(580, 50), BackColor = trialOpen ? Theme.AccentLight : Color.FromArgb(253, 236, 234) };
            var bannerText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                ForeColor = trialOpen ? Theme.AccentDark : Theme.Danger,
                Text = trial == null ? "" : trial.Message + (trialOpen ? "  " + L.T("All features are available during the trial.") : "")
            };
            banner.Controls.Add(bannerText);
            banner.Visible = trial != null;
            int y = trial != null ? 154 : 94;

            var step1 = new Label { Text = L.T("1. Send this machine code to your vendor (only needed for computer-bound licenses):"), Location = new Point(20, y), AutoSize = true };
            var machine = new TextBox { Text = MachineIdentity.Code, ReadOnly = true, Location = new Point(22, y + 26), Width = 300, Font = new Font("Consolas", 13f), BackColor = Theme.Window };
            var copyMachine = new Button { Text = L.T("Copy"), Location = new Point(332, y + 25), Width = 80, Height = 30 };
            copyMachine.Click += (s, e) => { Clipboard.SetText(MachineIdentity.Code); _status.ForeColor = Theme.Accent; _status.Text = L.T("Machine code copied to the clipboard."); };

            var step2 = new Label { Text = L.T("2. Enter or paste the license key:"), Location = new Point(20, y + 76), AutoSize = true };
            _key = new TextBox { Multiline = true, Location = new Point(22, y + 102), Size = new Size(576, 110), Font = new Font("Consolas", 10.5f), ScrollBars = ScrollBars.Vertical };
            if (current?.Key != null) _key.Text = current.Key;

            _status = new Label { Location = new Point(20, y + 220), Size = new Size(580, 40), ForeColor = Theme.Danger };
            if (current != null && current.Status != LicenseStatus.Missing) _status.Text = current.Message;

            int by = ClientSize.Height - 54;
            var loadFile = new Button { Text = L.T("Load license file…"), Location = new Point(20, by), Width = 150, Height = 34 };
            bool offerTrial = trialOpen && startup;
            var activate = new Button { Text = L.T("Activate"), Location = new Point(offerTrial ? 266 : 388, by), Width = 104, Height = 34, BackColor = Theme.Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = Theme.Bold };
            activate.FlatAppearance.BorderSize = 0;
            var cancel = new Button { Text = startup ? L.T("Exit") : L.T("Cancel"), Location = new Point(496, by), Width = 104, Height = 34, DialogResult = DialogResult.Cancel };
            var continueTrial = new Button { Text = L.T("Continue Trial"), Location = new Point(378, by), Width = 112, Height = 34, DialogResult = DialogResult.Ignore, Visible = offerTrial };

            loadFile.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = L.T("License file|*.lic;*.key;*.txt|All files|*.*") })
                    if (dlg.ShowDialog(this) == DialogResult.OK) _key.Text = LicenseManager.ReadKeyFile(dlg.FileName);
            };
            activate.Click += (s, e) => Activate();
            _key.TextChanged += (s, e) => { if (_status.ForeColor == Theme.Danger) _status.Text = ""; };

            Controls.AddRange(new Control[] { header, banner, step1, machine, copyMachine, step2, _key, _status, loadFile, activate, continueTrial, cancel });
            AcceptButton = activate;
            CancelButton = cancel;
        }

        private new void Activate()
        {
            var check = LicenseManager.Activate(_key.Text);
            if (!check.IsValid)
            {
                _status.ForeColor = Theme.Danger;
                _status.Text = check.Message;
                return;
            }
            Result = check;
            MessageBox.Show(this, L.T("Thank you — Falcon OCR is activated.\n\n") + check.Message, L.T("Activated"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
