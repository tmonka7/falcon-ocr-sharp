using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FalconOcr.Licensing;

namespace FalconOcr.KeyGen
{
    /// <summary>License key generator for the Falcon OCR vendor.</summary>
    internal sealed class KeyGenForm : Form
    {
        private static readonly Color Accent = Color.FromArgb(19, 134, 92);
        private static readonly Color Header = Color.FromArgb(14, 110, 76);

        private SigningKey _signer;
        private readonly Label _signerInfo;
        private readonly TextBox _licensee;
        private readonly TextBox _machine;
        private readonly CheckBox _anyMachine;
        private readonly CheckBox _perpetual;
        private readonly DateTimePicker _expires;
        private readonly NumericUpDown _serial;
        private readonly TextBox _note;
        private readonly TextBox _key;
        private readonly TextBox _verifyBox;
        private readonly Label _verifyResult;

        public KeyGenForm()
        {
            Text = "Falcon OCR — License Key Generator";
            Font = new Font("Segoe UI", 9.75f);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(780, 900);
            MinimumSize = new Size(700, 700);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Header };
            header.Controls.Add(new Label { Text = "License Key Generator", ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 16f), AutoSize = true, Location = new Point(18, 8) });
            header.Controls.Add(new Label { Text = "Vendor tool — keep this program and its signing key private.", ForeColor = Color.FromArgb(215, 240, 228), AutoSize = true, Location = new Point(20, 40) });

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(18, 12, 18, 12), AutoScroll = true };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ---- signing key
            _signerInfo = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Margin = new Padding(3, 6, 3, 3) };
            var keyButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            var newKey = Btn("New signing key…");
            var importKey = Btn("Import backup…");
            var backupKey = Btn("Backup key…");
            var writeSource = Btn("Write public key to app…", 190);
            keyButtons.Controls.AddRange(new Control[] { newKey, importKey, backupKey, writeSource });

            // ---- license fields
            _licensee = new TextBox { Dock = DockStyle.Fill };
            _machine = new TextBox { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper };
            _anyMachine = new CheckBox { Text = "Any computer (not bound to a machine code)", AutoSize = true };
            _perpetual = new CheckBox { Text = "Perpetual (never expires)", AutoSize = true, Checked = true };
            _expires = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(1), Enabled = false, Width = 140 };
            var expiryRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            var days30 = Btn("30 days", 80);
            var year1 = Btn("1 year", 80);
            expiryRow.Controls.AddRange(new Control[] { _perpetual, _expires, days30, year1 });
            _serial = new NumericUpDown { Minimum = 1, Maximum = uint.MaxValue, Width = 140, Value = SigningKey.NextSerial() };
            _note = new TextBox { Dock = DockStyle.Fill };

            var generate = new Button { Text = "Generate License Key", Height = 42, Width = 240, BackColor = Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 11f) };
            generate.FlatAppearance.BorderSize = 0;
            _key = new TextBox { Multiline = true, ReadOnly = true, Height = 92, Dock = DockStyle.Fill, Font = new Font("Consolas", 10.5f), BackColor = Color.FromArgb(244, 246, 248), ScrollBars = ScrollBars.Vertical };
            var keyActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            var copy = Btn("Copy key");
            var save = Btn("Save as .lic file…");
            var openLog = Btn("Open issued log");
            keyActions.Controls.AddRange(new Control[] { copy, save, openLog });

            // ---- verify
            _verifyBox = new TextBox { Multiline = true, Height = 60, Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5f), ScrollBars = ScrollBars.Vertical };
            var verify = Btn("Verify");
            _verifyResult = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Margin = new Padding(3, 6, 3, 3) };

            int row = 0;
            void Section(string t)
            {
                var l = new Label { Text = t, Font = new Font("Segoe UI Semibold", 11.5f), ForeColor = Accent, AutoSize = true, Margin = new Padding(0, row == 0 ? 0 : 14, 0, 4) };
                body.Controls.Add(l, 0, row);
                body.SetColumnSpan(l, 2);
                row++;
            }
            void Row(string caption, Control c)
            {
                body.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, 0, row);
                body.Controls.Add(c, 1, row);
                row++;
            }

            Section("Signing key");
            Row("Status:", _signerInfo);
            Row("", keyButtons);
            Section("License");
            Row("Licensee (customer):", _licensee);
            Row("Machine code:", _machine);
            Row("", _anyMachine);
            Row("Expiry:", expiryRow);
            Row("Serial number:", _serial);
            Row("Note (log only):", _note);
            Row("", generate);
            Row("License key:", _key);
            Row("", keyActions);
            Section("Verify a key");
            Row("Key:", _verifyBox);
            Row("", verify);
            Row("Result:", _verifyResult);

            Controls.Add(body);
            Controls.Add(header);

            _anyMachine.CheckedChanged += (s, e) => _machine.Enabled = !_anyMachine.Checked;
            _perpetual.CheckedChanged += (s, e) => _expires.Enabled = !_perpetual.Checked;
            days30.Click += (s, e) => { _perpetual.Checked = false; _expires.Value = DateTime.Today.AddDays(30); };
            year1.Click += (s, e) => { _perpetual.Checked = false; _expires.Value = DateTime.Today.AddYears(1); };
            generate.Click += (s, e) => Generate();
            copy.Click += (s, e) => { if (_key.TextLength > 0) Clipboard.SetText(_key.Text); };
            save.Click += (s, e) => SaveLicenseFile();
            openLog.Click += (s, e) => { if (File.Exists(SigningKey.IssuedLogPath)) Process.Start(new ProcessStartInfo(SigningKey.IssuedLogPath) { UseShellExecute = true }); };
            verify.Click += (s, e) => Verify();
            newKey.Click += (s, e) => NewSigningKey();
            importKey.Click += (s, e) => ImportKey();
            backupKey.Click += (s, e) => BackupKey();
            writeSource.Click += (s, e) => WritePublicKey(true);

            Load += (s, e) => LoadSigner();
        }

        private static Button Btn(string text, int width = 150) => new Button { Text = text, Width = width, Height = 30, FlatStyle = FlatStyle.System, Margin = new Padding(0, 2, 8, 2) };

        private void LoadSigner()
        {
            _signer?.Dispose();
            _signer = null;
            if (SigningKey.Exists)
            {
                try { _signer = SigningKey.Load(); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "The stored signing key cannot be opened (it is encrypted for the Windows user that created it):\n\n" + ex.Message +
                        "\n\nImport a backup of the key instead.", "Signing key", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            UpdateSignerInfo();
            if (_signer == null && !SigningKey.Exists &&
                MessageBox.Show(this, "No signing key exists yet.\n\nCreate a new signing key now? (Or choose No and import a backup.)", "Signing key", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                NewSigningKey();
        }

        private void UpdateSignerInfo()
        {
            if (_signer == null)
            {
                _signerInfo.Text = "No signing key loaded.";
                _signerInfo.ForeColor = Color.Firebrick;
                return;
            }
            bool match = _signer.MatchesBuiltInPublicKey;
            _signerInfo.Text = "Fingerprint " + _signer.Fingerprint + "\n" +
                (match
                    ? "✓ Matches the public key compiled into this Falcon OCR build."
                    : "⚠ Does not match the public key compiled into this build (" + SigningKey.FingerprintOf(LicensePublicKey.Blob) +
                      "). Use \"Write public key to app…\" and rebuild, otherwise the app rejects these keys.");
            _signerInfo.ForeColor = match ? Color.FromArgb(20, 110, 60) : Color.DarkOrange;
        }

        private void NewSigningKey()
        {
            if (_signer != null && MessageBox.Show(this,
                    "Replacing the signing key invalidates EVERY license issued so far once the app is rebuilt with the new public key.\n\nContinue?",
                    "New signing key", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
            var k = SigningKey.CreateNew();
            k.Save();
            _signer?.Dispose();
            _signer = k;
            UpdateSignerInfo();
            BackupKey(firstTime: true);
            WritePublicKey(false);
        }

        private void ImportKey()
        {
            using (var dlg = new OpenFileDialog { Filter = "Signing key backup|*.key;*.txt|All files|*.*" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var k = SigningKey.FromBackup(dlg.FileName);
                    k.Save();
                    _signer?.Dispose();
                    _signer = k;
                    UpdateSignerInfo();
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Import", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private void BackupKey(bool firstTime = false)
        {
            if (_signer == null) return;
            if (firstTime)
                MessageBox.Show(this, "Save a backup of the new signing key now and keep it somewhere safe (offline).\nWithout it you cannot issue keys for this build on another PC or Windows account.",
                    "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            using (var dlg = new SaveFileDialog { Filter = "Signing key backup|*.key", FileName = "falcon-ocr-signing-" + _signer.Fingerprint + ".key" })
                if (dlg.ShowDialog(this) == DialogResult.OK) _signer.ExportBackup(dlg.FileName);
        }

        private void WritePublicKey(bool ask)
        {
            if (_signer == null) return;
            var path = SigningKey.FindPublicKeySource();
            if (path == null || ask)
            {
                using (var dlg = new SaveFileDialog { Filter = "C# source|LicensePublicKey.cs", FileName = "LicensePublicKey.cs", InitialDirectory = path == null ? null : Path.GetDirectoryName(path) })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    path = dlg.FileName;
                }
            }
            _signer.WritePublicKeySource(path);
            MessageBox.Show(this, "Public key written to:\n" + path + "\n\nRebuild Falcon OCR (build.ps1) so the application accepts keys from this signing key.",
                "Public key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Generate()
        {
            if (_signer == null)
            {
                MessageBox.Show(this, "Create or import a signing key first.", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var info = new LicenseInfo
            {
                Serial = (uint)_serial.Value,
                Licensee = _licensee.Text.Trim(),
                Issued = DateTime.UtcNow.Date,
                Expires = _perpetual.Checked ? (DateTime?)null : _expires.Value.Date
            };
            if (info.Licensee.Length == 0)
            {
                MessageBox.Show(this, "Enter the licensee (customer name).", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!_anyMachine.Checked)
            {
                try { info.MachineHash = MachineIdentity.ParseCode(_machine.Text); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Machine code: " + ex.Message + "\n\nThe customer finds it in the activation window of Falcon OCR.\nTick \"Any computer\" for an unbound key.",
                        "Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            try
            {
                var key = _signer.Issue(info);
                _key.Text = key;
                SigningKey.LogIssued(info, key, _note.Text.Trim());
                SigningKey.CommitSerial(info.Serial);
                _serial.Value = SigningKey.NextSerial();
                _verifyBox.Text = key;
                Verify();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Generate", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void SaveLicenseFile()
        {
            if (_key.TextLength == 0) return;
            using (var dlg = new SaveFileDialog { Filter = "Falcon OCR license|*.lic", FileName = MakeFileName(_licensee.Text) + ".lic" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                File.WriteAllText(dlg.FileName,
                    "Falcon OCR license" + Environment.NewLine +
                    "Licensee: " + _licensee.Text.Trim() + Environment.NewLine +
                    "Issued: " + DateTime.Today.ToString("yyyy-MM-dd") + Environment.NewLine + Environment.NewLine +
                    _key.Text + Environment.NewLine);
            }
        }

        private static string MakeFileName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return string.IsNullOrWhiteSpace(s) ? "license" : s.Trim();
        }

        private void Verify()
        {
            var blob = _signer?.PublicBlob ?? LicensePublicKey.Blob;
            var check = LicenseManager.Validate(_verifyBox.Text, blob, null, DateTime.UtcNow, false);
            if (check.Info != null && (check.Status == LicenseStatus.Valid || check.Status == LicenseStatus.WrongMachine || check.Status == LicenseStatus.Expired))
            {
                var i = check.Info;
                _verifyResult.Text = (check.Status == LicenseStatus.Expired ? "Signature OK — EXPIRED\n" : "Signature OK\n") +
                    $"Licensee: {i.Licensee}\nSerial: {i.Serial}\nIssued: {i.Issued:yyyy-MM-dd}\nExpires: {(i.Expires.HasValue ? i.Expires.Value.ToString("yyyy-MM-dd") : "never")}\n" +
                    $"Machine: {(i.AnyMachine ? "any computer" : MachineIdentity.FormatCode(i.MachineHash))}";
                _verifyResult.ForeColor = check.Status == LicenseStatus.Expired ? Color.DarkOrange : Color.FromArgb(20, 110, 60);
            }
            else
            {
                _verifyResult.Text = check.Message;
                _verifyResult.ForeColor = Color.Firebrick;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _signer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
