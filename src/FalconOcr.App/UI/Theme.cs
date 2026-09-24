using System.Drawing;

namespace FalconOcr.App.UI
{
    /// <summary>Colors and fonts of the application (green ABBYY-like theme).</summary>
    internal static class Theme
    {
        public static readonly Color Header = Color.FromArgb(14, 110, 76);
        public static readonly Color HeaderText = Color.White;
        public static readonly Color SidebarTop = Color.FromArgb(16, 112, 78);
        public static readonly Color SidebarBottom = Color.FromArgb(10, 88, 62);
        public static readonly Color SidebarSelected = Color.FromArgb(58, 150, 112);
        public static readonly Color SidebarHover = Color.FromArgb(34, 130, 94);
        public static readonly Color Accent = Color.FromArgb(19, 134, 92);
        public static readonly Color AccentDark = Color.FromArgb(12, 104, 71);
        public static readonly Color AccentLight = Color.FromArgb(225, 243, 235);
        public static readonly Color AccentHover = Color.FromArgb(238, 248, 243);
        public static readonly Color Window = Color.FromArgb(244, 246, 248);
        public static readonly Color Panel = Color.White;
        public static readonly Color Border = Color.FromArgb(222, 227, 232);
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color SubText = Color.FromArgb(100, 112, 126);
        public static readonly Color Canvas = Color.FromArgb(226, 230, 234);
        public static readonly Color Selection = Color.FromArgb(70, 19, 134, 92);
        public static readonly Color LowConfidence = Color.FromArgb(90, 255, 214, 0);
        public static readonly Color Danger = Color.FromArgb(200, 40, 40);

        public static readonly Color PdfRed = Color.FromArgb(222, 49, 41);
        public static readonly Color ImageGreen = Color.FromArgb(46, 160, 90);
        public static readonly Color WordBlue = Color.FromArgb(24, 90, 189);
        public static readonly Color ExcelGreen = Color.FromArgb(16, 124, 65);
        public static readonly Color HtmlOrange = Color.FromArgb(228, 77, 38);

        public static readonly Font Base = new Font("Segoe UI", 9.75f);
        public static readonly Font Small = new Font("Segoe UI", 8.5f);
        public static readonly Font Bold = new Font("Segoe UI Semibold", 10f);
        public static readonly Font Title = new Font("Segoe UI Semibold", 15f);
        public static readonly Font Heading = new Font("Segoe UI Semibold", 12f);
        public static readonly Font Nav = new Font("Segoe UI", 11f);
    }
}
