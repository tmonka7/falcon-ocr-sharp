using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FalconOcr.App.UI
{
    internal enum IconKind
    {
        None,
        Logo,
        Home,
        Ocr,
        Batch,
        History,
        Settings,
        Help,
        AddCircle,
        Scanner,
        Clipboard,
        Rotate,
        Crop,
        Trash,
        Play,
        Export,
        ZoomIn,
        ZoomOut,
        FitWidth,
        FitPage,
        Hand,
        ChevronLeft,
        ChevronRight,
        ChevronUp,
        ChevronDown,
        Pdf,
        Image,
        Images,
        Word,
        Excel,
        Html,
        Text,
        Folder,
        Check,
        Close,
        Minimize,
        Maximize,
        Restore,
        Boxes,
        Copy,
        Stop,
        List,
        NumberedList,
        More
    }

    /// <summary>Resolution-independent line icons drawn with GDI+ (no bitmap assets).</summary>
    internal static class Icons
    {
        public static void Draw(Graphics g, IconKind kind, RectangleF r, Color color, float stroke = 0)
        {
            if (kind == IconKind.None) return;
            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(r.Width, r.Height);
            float x = r.X + (r.Width - s) / 2, y = r.Y + (r.Height - s) / 2;
            g.TranslateTransform(x, y);
            g.ScaleTransform(s / 24f, s / 24f); // icons are designed on a 24x24 grid
            float w = stroke > 0 ? stroke * 24f / s : 1.8f;
            using (var pen = new Pen(color, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (var brush = new SolidBrush(color))
            {
                DrawGlyph(g, kind, pen, brush, color);
            }
            g.Restore(state);
        }

        private static void DrawGlyph(Graphics g, IconKind kind, Pen p, SolidBrush b, Color c)
        {
            switch (kind)
            {
                case IconKind.Logo:
                    using (var path = Rounded(new RectangleF(2, 2, 20, 20), 5)) g.FillPath(b, path);
                    using (var white = new Pen(Color.White, 2.2f))
                    {
                        g.DrawEllipse(white, 6.5f, 6.5f, 10, 10);
                        g.DrawLine(white, 14.5f, 14.5f, 17.5f, 17.5f);
                    }
                    break;
                case IconKind.Home:
                    g.DrawLines(p, new[] { new PointF(3, 11), new PointF(12, 3.5f), new PointF(21, 11) });
                    g.DrawLines(p, new[] { new PointF(5.5f, 9.5f), new PointF(5.5f, 20.5f), new PointF(18.5f, 20.5f), new PointF(18.5f, 9.5f) });
                    g.DrawRectangle(p, 10, 14, 4, 6.5f);
                    break;
                case IconKind.Ocr:
                    Corners(g, p);
                    g.DrawEllipse(p, 8.5f, 8.5f, 7, 7);
                    g.FillEllipse(b, 11, 11, 2, 2);
                    break;
                case IconKind.Batch:
                    g.DrawRectangle(p, 4, 3, 13, 17);
                    g.DrawLine(p, 7.5f, 8, 13.5f, 8);
                    g.DrawLine(p, 7.5f, 12, 13.5f, 12);
                    g.DrawLine(p, 7.5f, 16, 10.5f, 16);
                    g.FillEllipse(Brushes.White, 13, 13, 9, 9);
                    g.DrawEllipse(p, 13.5f, 13.5f, 8, 8);
                    g.DrawLine(p, 17.5f, 15.5f, 17.5f, 17.5f);
                    g.DrawLine(p, 17.5f, 17.5f, 19, 19);
                    break;
                case IconKind.History:
                    g.DrawArc(p, 3.5f, 3.5f, 17, 17, 200, 300);
                    g.DrawLines(p, new[] { new PointF(3, 6.5f), new PointF(4.2f, 10.2f), new PointF(8, 9.5f) });
                    g.DrawLines(p, new[] { new PointF(12, 7.5f), new PointF(12, 12), new PointF(15, 14) });
                    break;
                case IconKind.Settings:
                    Gear(g, p);
                    break;
                case IconKind.Help:
                    g.DrawEllipse(p, 3, 3, 18, 18);
                    g.DrawArc(p, 9, 7, 6, 6, 180, 250);
                    g.DrawLine(p, 12, 13.3f, 12, 14.5f);
                    g.FillEllipse(b, 11, 16.5f, 2, 2);
                    break;
                case IconKind.AddCircle:
                    g.FillEllipse(b, 2, 2, 20, 20);
                    using (var white = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(white, 12, 7, 12, 17);
                        g.DrawLine(white, 7, 12, 17, 12);
                    }
                    break;
                case IconKind.Scanner:
                    g.DrawLines(p, new[] { new PointF(6.5f, 9), new PointF(6.5f, 3.5f), new PointF(17.5f, 3.5f), new PointF(17.5f, 9) });
                    using (var path = Rounded(new RectangleF(3, 9, 18, 8), 2)) g.DrawPath(p, path);
                    g.DrawRectangle(p, 6.5f, 17, 11, 4);
                    g.DrawLine(p, 6, 13, 9, 13);
                    break;
                case IconKind.Clipboard:
                    using (var path = Rounded(new RectangleF(4.5f, 4.5f, 15, 17), 2)) g.DrawPath(p, path);
                    g.DrawRectangle(p, 8.5f, 2.5f, 7, 4);
                    g.DrawLine(p, 8, 11, 16, 11);
                    g.DrawLine(p, 8, 14.5f, 16, 14.5f);
                    g.DrawLine(p, 8, 18, 13, 18);
                    break;
                case IconKind.Rotate:
                    g.DrawArc(p, 4, 4, 16, 16, -60, 300);
                    g.DrawLines(p, new[] { new PointF(15.5f, 2.5f), new PointF(16.3f, 6.6f), new PointF(12.2f, 7.3f) });
                    break;
                case IconKind.Crop:
                    g.DrawLines(p, new[] { new PointF(6.5f, 2.5f), new PointF(6.5f, 17.5f), new PointF(21.5f, 17.5f) });
                    g.DrawLines(p, new[] { new PointF(2.5f, 6.5f), new PointF(17.5f, 6.5f), new PointF(17.5f, 21.5f) });
                    break;
                case IconKind.Trash:
                    g.DrawLine(p, 3.5f, 6, 20.5f, 6);
                    g.DrawLines(p, new[] { new PointF(9, 6), new PointF(9, 3.5f), new PointF(15, 3.5f), new PointF(15, 6) });
                    g.DrawLines(p, new[] { new PointF(5.5f, 6), new PointF(6.5f, 20.5f), new PointF(17.5f, 20.5f), new PointF(18.5f, 6) });
                    g.DrawLine(p, 10, 10, 10, 17);
                    g.DrawLine(p, 14, 10, 14, 17);
                    break;
                case IconKind.Play:
                    g.FillPolygon(b, new[] { new PointF(6, 3), new PointF(20, 12), new PointF(6, 21) });
                    break;
                case IconKind.Stop:
                    g.FillRectangle(b, 5, 5, 14, 14);
                    break;
                case IconKind.Export:
                    g.DrawLines(p, new[] { new PointF(13, 3.5f), new PointF(4.5f, 3.5f), new PointF(4.5f, 20.5f), new PointF(16.5f, 20.5f), new PointF(16.5f, 16) });
                    g.DrawLines(p, new[] { new PointF(13, 3.5f), new PointF(16.5f, 7), new PointF(16.5f, 8.5f) });
                    g.DrawLine(p, 9.5f, 12.5f, 21, 12.5f);
                    g.DrawLines(p, new[] { new PointF(17.5f, 9), new PointF(21, 12.5f), new PointF(17.5f, 16) });
                    break;
                case IconKind.ZoomIn:
                    g.DrawLine(p, 5, 12, 19, 12);
                    g.DrawLine(p, 12, 5, 12, 19);
                    break;
                case IconKind.ZoomOut:
                    g.DrawLine(p, 5, 12, 19, 12);
                    break;
                case IconKind.FitWidth:
                    using (var path = Rounded(new RectangleF(3.5f, 3.5f, 17, 17), 2)) g.DrawPath(p, path);
                    g.DrawLine(p, 7, 12, 17, 12);
                    g.DrawLines(p, new[] { new PointF(9, 10), new PointF(7, 12), new PointF(9, 14) });
                    g.DrawLines(p, new[] { new PointF(15, 10), new PointF(17, 12), new PointF(15, 14) });
                    break;
                case IconKind.FitPage:
                    using (var path = Rounded(new RectangleF(3.5f, 3.5f, 17, 17), 2)) g.DrawPath(p, path);
                    g.DrawLine(p, 12, 7.5f, 12, 16.5f);
                    g.DrawLine(p, 7.5f, 12, 16.5f, 12);
                    break;
                case IconKind.Hand:
                    g.DrawLines(p, new[] { new PointF(8, 13), new PointF(8, 5.5f) });
                    g.DrawArc(p, 8, 3.5f, 3.5f, 3.5f, 180, 180);
                    g.DrawLines(p, new[] { new PointF(11.5f, 5.3f), new PointF(11.5f, 11) });
                    g.DrawArc(p, 11.5f, 4, 3.5f, 3.5f, 180, 180);
                    g.DrawLines(p, new[] { new PointF(15, 5.8f), new PointF(15, 11) });
                    g.DrawArc(p, 15, 6.5f, 3.5f, 3.5f, 180, 180);
                    g.DrawLines(p, new[] { new PointF(18.5f, 8.3f), new PointF(18.5f, 14), new PointF(17, 19), new PointF(14, 21), new PointF(10, 21), new PointF(7, 18), new PointF(4.5f, 13.5f), new PointF(5.5f, 12.3f), new PointF(8, 13.5f) });
                    break;
                case IconKind.ChevronLeft:
                    g.DrawLines(p, new[] { new PointF(15, 5), new PointF(8, 12), new PointF(15, 19) });
                    break;
                case IconKind.ChevronRight:
                    g.DrawLines(p, new[] { new PointF(9, 5), new PointF(16, 12), new PointF(9, 19) });
                    break;
                case IconKind.ChevronUp:
                    g.DrawLines(p, new[] { new PointF(5, 15), new PointF(12, 8), new PointF(19, 15) });
                    break;
                case IconKind.ChevronDown:
                    g.DrawLines(p, new[] { new PointF(5, 9), new PointF(12, 16), new PointF(19, 9) });
                    break;
                case IconKind.Pdf:
                    FileBadge(g, Theme.PdfRed, "PDF");
                    break;
                case IconKind.Image:
                    using (var path = Rounded(new RectangleF(3, 3, 18, 18), 2.5f)) g.FillPath(new SolidBrush(Theme.ImageGreen), path);
                    g.FillEllipse(Brushes.White, 6.5f, 6.5f, 4, 4);
                    g.FillPolygon(Brushes.White, new[] { new PointF(5, 18.5f), new PointF(10, 12), new PointF(13, 15.5f), new PointF(15.5f, 13), new PointF(19, 18.5f) });
                    break;
                case IconKind.Images:
                    using (var back = Rounded(new RectangleF(6, 2, 16, 16), 2.5f)) g.FillPath(new SolidBrush(ColorUtil.Light(Theme.ImageGreen)), back);
                    using (var path = Rounded(new RectangleF(2, 6, 16, 16), 2.5f)) g.FillPath(new SolidBrush(Theme.ImageGreen), path);
                    g.FillEllipse(Brushes.White, 5, 9, 3.5f, 3.5f);
                    g.FillPolygon(Brushes.White, new[] { new PointF(4, 19.5f), new PointF(8.5f, 14), new PointF(11, 17), new PointF(13, 15), new PointF(16, 19.5f) });
                    break;
                case IconKind.Word:
                    LetterTile(g, Theme.WordBlue, "W");
                    break;
                case IconKind.Excel:
                    LetterTile(g, Theme.ExcelGreen, "X");
                    break;
                case IconKind.Html:
                    using (var path = Rounded(new RectangleF(2, 2, 20, 20), 3)) g.FillPath(new SolidBrush(Theme.HtmlOrange), path);
                    using (var white = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    {
                        g.DrawLines(white, new[] { new PointF(9, 8), new PointF(5.5f, 12), new PointF(9, 16) });
                        g.DrawLines(white, new[] { new PointF(15, 8), new PointF(18.5f, 12), new PointF(15, 16) });
                        g.DrawLine(white, 13, 7, 11, 17);
                    }
                    break;
                case IconKind.Text:
                    using (var path = Rounded(new RectangleF(3.5f, 2.5f, 17, 19), 2)) g.DrawPath(p, path);
                    g.DrawLine(p, 7, 8, 17, 8);
                    g.DrawLine(p, 7, 12, 17, 12);
                    g.DrawLine(p, 7, 16, 13, 16);
                    break;
                case IconKind.Folder:
                    g.DrawLines(p, new[] { new PointF(2.5f, 19.5f), new PointF(2.5f, 5), new PointF(9, 5), new PointF(11, 7.5f), new PointF(21.5f, 7.5f), new PointF(21.5f, 19.5f), new PointF(2.5f, 19.5f) });
                    break;
                case IconKind.Check:
                    g.DrawLines(p, new[] { new PointF(5, 12.5f), new PointF(10, 17.5f), new PointF(19.5f, 7) });
                    break;
                case IconKind.Close:
                    g.DrawLine(p, 6, 6, 18, 18);
                    g.DrawLine(p, 18, 6, 6, 18);
                    break;
                case IconKind.Minimize:
                    g.DrawLine(p, 6, 12, 18, 12);
                    break;
                case IconKind.Maximize:
                    g.DrawRectangle(p, 6, 6, 12, 12);
                    break;
                case IconKind.Restore:
                    g.DrawRectangle(p, 5, 8.5f, 10.5f, 10.5f);
                    g.DrawLines(p, new[] { new PointF(8.5f, 8.5f), new PointF(8.5f, 5), new PointF(19, 5), new PointF(19, 15.5f), new PointF(15.5f, 15.5f) });
                    break;
                case IconKind.Boxes:
                    g.DrawRectangle(p, 3.5f, 3.5f, 17, 5);
                    g.DrawRectangle(p, 3.5f, 11, 7.5f, 9.5f);
                    g.DrawRectangle(p, 13.5f, 11, 7, 9.5f);
                    break;
                case IconKind.Copy:
                    using (var path = Rounded(new RectangleF(8.5f, 8.5f, 12, 12), 2)) g.DrawPath(p, path);
                    g.DrawLines(p, new[] { new PointF(15.5f, 5.5f), new PointF(15.5f, 3.5f), new PointF(3.5f, 3.5f), new PointF(3.5f, 15.5f), new PointF(5.5f, 15.5f) });
                    break;
                case IconKind.List:
                    for (int i = 0; i < 3; i++)
                    {
                        g.FillEllipse(b, 3.5f, 5 + i * 6, 2.6f, 2.6f);
                        g.DrawLine(p, 9, 6.3f + i * 6, 20.5f, 6.3f + i * 6);
                    }
                    break;
                case IconKind.NumberedList:
                    for (int i = 0; i < 3; i++)
                    {
                        g.DrawLine(p, 4.5f, 4.5f + i * 6, 4.5f, 8 + i * 6);
                        g.DrawLine(p, 9, 6.3f + i * 6, 20.5f, 6.3f + i * 6);
                    }
                    break;
                case IconKind.More:
                    for (int i = 0; i < 3; i++) g.FillEllipse(b, 4.5f + i * 6, 10.8f, 2.6f, 2.6f);
                    break;
            }
        }

        private static void Corners(Graphics g, Pen p)
        {
            g.DrawLines(p, new[] { new PointF(3.5f, 8), new PointF(3.5f, 3.5f), new PointF(8, 3.5f) });
            g.DrawLines(p, new[] { new PointF(16, 3.5f), new PointF(20.5f, 3.5f), new PointF(20.5f, 8) });
            g.DrawLines(p, new[] { new PointF(20.5f, 16), new PointF(20.5f, 20.5f), new PointF(16, 20.5f) });
            g.DrawLines(p, new[] { new PointF(8, 20.5f), new PointF(3.5f, 20.5f), new PointF(3.5f, 16) });
        }

        private static void Gear(Graphics g, Pen p)
        {
            var pts = new PointF[32];
            for (int i = 0; i < 32; i++)
            {
                double a = i * Math.PI * 2 / 32;
                // Eight teeth: alternate outer/inner radius in groups of two points.
                float rr = (i / 2) % 2 == 0 ? 9.5f : 7.2f;
                pts[i] = new PointF(12 + (float)(Math.Cos(a) * rr), 12 + (float)(Math.Sin(a) * rr));
            }
            g.DrawPolygon(p, pts);
            g.DrawEllipse(p, 9, 9, 6, 6);
        }

        private static void FileBadge(Graphics g, Color color, string label)
        {
            using (var body = new GraphicsPath())
            {
                body.AddLines(new[] { new PointF(5, 2), new PointF(15, 2), new PointF(20, 7), new PointF(20, 22), new PointF(5, 22), new PointF(5, 2) });
                g.FillPath(Brushes.White, body);
                using (var outline = new Pen(Color.FromArgb(170, 170, 170), 1f)) g.DrawPath(outline, body);
            }
            using (var path = Rounded(new RectangleF(1.5f, 10, 17, 8), 1.5f)) g.FillPath(new SolidBrush(color), path);
            using (var f = new Font("Segoe UI", 5.2f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(label, f, Brushes.White, new RectangleF(1.5f, 10, 17, 8.4f), sf);
        }

        private static void LetterTile(Graphics g, Color color, string letter)
        {
            using (var back = Rounded(new RectangleF(7, 2, 15, 20), 2.5f)) g.FillPath(new SolidBrush(ColorUtil.Light(color, 0.6f)), back);
            using (var front = Rounded(new RectangleF(2, 6, 12, 12), 2)) g.FillPath(new SolidBrush(color), front);
            using (var f = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(letter, f, Brushes.White, new RectangleF(2, 6, 12, 12.5f), sf);
        }

        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            if (d <= 0.01f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>Color helpers for drawing code.</summary>
    internal static class ColorUtil
    {
        public static Color Light(Color c, float amount = 0.5f)
        {
            return Color.FromArgb(c.A, (int)(c.R + (255 - c.R) * amount), (int)(c.G + (255 - c.G) * amount), (int)(c.B + (255 - c.B) * amount));
        }
    }
}
