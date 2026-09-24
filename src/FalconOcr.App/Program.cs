using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FalconOcr.Licensing;

namespace FalconOcr.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Native libraries (onnxruntime, pdfium, VC++ runtime) live next to the executable.
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ReportCrash(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ReportCrash(e.ExceptionObject as Exception);

            if (!Environment.Is64BitProcess)
            {
                MessageBox.Show("Falcon OCR must run as a 64-bit process.", "Falcon OCR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Licensed with a key from FalconOcr.KeyGen, or within the 7-day trial. Without a license the
            // activation window is shown at every start; after the trial it cannot be skipped.
            License = LicenseManager.CheckInstalled();
            if (!License.IsValid)
            {
                Trial = TrialManager.Check();
                // Headless diagnostics may skip the prompt, but only while the trial is still valid.
                bool skipPrompt = !Trial.Expired && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FALCON_SNAPSHOT"));
                if (!skipPrompt)
                using (var activation = new Pages.ActivationForm(License, startup: true, trial: Trial))
                {
                    var answer = activation.ShowDialog();
                    if (answer == DialogResult.OK)
                    {
                        License = activation.Result;
                        Trial = null;
                    }
                    else if (answer != DialogResult.Ignore || Trial.Expired) return;
                }
            }
            Application.Run(new MainForm(args));
        }

        /// <summary>The validated license of this session (invalid while running as a trial).</summary>
        public static LicenseCheck License { get; set; }

        /// <summary>Trial state when running unlicensed; null once activated.</summary>
        public static TrialStatus Trial { get; set; }

        public static bool IsTrial => Trial != null && License?.IsValid != true;

        private static int _reporting;

        private static void ReportCrash(Exception ex)
        {
            if (ex == null || Interlocked.Exchange(ref _reporting, 1) == 1) return;
            try
            {
                var log = Path.Combine(Services.AppPaths.DataDir, "error.log");
                File.AppendAllText(log, DateTime.Now + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine);
                MessageBox.Show("An unexpected error occurred:\n\n" + ex.Message + "\n\nDetails were written to:\n" + log, "Falcon OCR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
            finally { Interlocked.Exchange(ref _reporting, 0); }
        }
    }
}
