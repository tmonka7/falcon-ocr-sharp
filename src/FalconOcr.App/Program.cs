using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

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
            Application.Run(new MainForm(args));
        }

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
