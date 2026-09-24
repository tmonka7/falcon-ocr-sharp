using System;
using System.Runtime.InteropServices;

namespace FalconOcr.Pdf
{
    /// <summary>Minimal P/Invoke surface of pdfium.dll (bblanchon/pdfium-binaries, x64).</summary>
    internal static class PdfiumNative
    {
        private const string Dll = "pdfium.dll";

        /// <summary>PDFium is not thread-safe: every call must hold this lock.</summary>
        public static readonly object Sync = new object();

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            lock (Sync)
            {
                if (_initialized) return;
                FPDF_InitLibrary();
                _initialized = true;
            }
        }

        public const int FPDF_ANNOT = 0x01;
        public const int FPDF_LCD_TEXT = 0x02;
        public const int FPDF_PRINTING = 0x800;
        public const int FPDFBitmap_BGRx = 3;
        public const int FPDF_FONT_ITALIC = 1 << 6;

        [StructLayout(LayoutKind.Sequential)]
        public struct FS_RECTF
        {
            public float Left, Top, Right, Bottom;
        }

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_InitLibrary();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadMemDocument64(IntPtr data, UIntPtr size, [MarshalAs(UnmanagedType.LPStr)] string password);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_CloseDocument(IntPtr doc);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint FPDF_GetLastError();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDF_GetPageCount(IntPtr doc);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadPage(IntPtr doc, int index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_ClosePage(IntPtr page);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern float FPDF_GetPageWidthF(IntPtr page);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern float FPDF_GetPageHeightF(IntPtr page);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFBitmap_GetStride(IntPtr bitmap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFBitmap_Destroy(IntPtr bitmap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page, int startX, int startY, int sizeX, int sizeY, int rotate, int flags);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDF_PageToDevice(IntPtr page, int startX, int startY, int sizeX, int sizeY, int rotate, double pageX, double pageY, out int deviceX, out int deviceY);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDFText_LoadPage(IntPtr page);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFText_ClosePage(IntPtr textPage);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFText_CountChars(IntPtr textPage);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint FPDFText_GetUnicode(IntPtr textPage, int index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFText_IsGenerated(IntPtr textPage, int index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern double FPDFText_GetFontSize(IntPtr textPage, int index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFText_GetFontWeight(IntPtr textPage, int index);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint FPDFText_GetFontInfo(IntPtr textPage, int index, byte[] buffer, uint buflen, out int flags);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFText_GetFillColor(IntPtr textPage, int index, out uint r, out uint g, out uint b, out uint a);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFText_GetLooseCharBox(IntPtr textPage, int index, out FS_RECTF rect);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFText_GetCharBox(IntPtr textPage, int index, out double left, out double right, out double bottom, out double top);
    }
}
