using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace FalconOcr.Imaging
{
    /// <summary>Packed 24-bit BGR image (same byte order as OpenCV / PaddleOCR), no row padding.</summary>
    public sealed class RgbImage
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Data { get; }

        public RgbImage(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            Data = new byte[width * height * 3];
        }

        public RgbImage(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }

        public static RgbImage FromBitmap(Bitmap bmp)
        {
            var img = new RgbImage(bmp.Width, bmp.Height);
            Bitmap src = bmp;
            bool dispose = false;
            if (bmp.PixelFormat != PixelFormat.Format24bppRgb)
            {
                // Flatten transparency onto white — transparent PNGs otherwise turn black.
                src = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(src))
                {
                    g.Clear(Color.White);
                    g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                }
                dispose = true;
            }
            try
            {
                var bd = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    int rowBytes = img.Width * 3;
                    for (int y = 0; y < img.Height; y++)
                        Marshal.Copy(bd.Scan0 + y * bd.Stride, img.Data, y * rowBytes, rowBytes);
                }
                finally { src.UnlockBits(bd); }
            }
            finally
            {
                if (dispose) src.Dispose();
            }
            return img;
        }

        public Bitmap ToBitmap()
        {
            var bmp = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            var bd = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                int rowBytes = Width * 3;
                for (int y = 0; y < Height; y++)
                    Marshal.Copy(Data, y * rowBytes, bd.Scan0 + y * bd.Stride, rowBytes);
            }
            finally { bmp.UnlockBits(bd); }
            return bmp;
        }

        public byte[] ToPng(Rectangle? region = null)
        {
            var r = region ?? new Rectangle(0, 0, Width, Height);
            r.Intersect(new Rectangle(0, 0, Width, Height));
            if (r.Width <= 0 || r.Height <= 0) return null;
            using (var bmp = Crop(r).ToBitmap())
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public RgbImage Crop(Rectangle r)
        {
            r.Intersect(new Rectangle(0, 0, Width, Height));
            var dst = new RgbImage(Math.Max(1, r.Width), Math.Max(1, r.Height));
            int rowBytes = dst.Width * 3;
            for (int y = 0; y < dst.Height; y++)
                Buffer.BlockCopy(Data, ((r.Y + y) * Width + r.X) * 3, dst.Data, y * rowBytes, rowBytes);
            return dst;
        }

        /// <summary>Luma (0..255) of the pixel.</summary>
        public int Gray(int x, int y)
        {
            int i = (y * Width + x) * 3;
            return (Data[i] * 29 + Data[i + 1] * 150 + Data[i + 2] * 77) >> 8;
        }

        public byte[] ToGray()
        {
            var g = new byte[Width * Height];
            for (int i = 0, j = 0; i < g.Length; i++, j += 3)
                g[i] = (byte)((Data[j] * 29 + Data[j + 1] * 150 + Data[j + 2] * 77) >> 8);
            return g;
        }

        public Color GetPixel(int x, int y)
        {
            int i = (y * Width + x) * 3;
            return Color.FromArgb(Data[i + 2], Data[i + 1], Data[i]);
        }
    }
}
