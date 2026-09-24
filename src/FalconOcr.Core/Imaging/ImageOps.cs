using System;
using System.Drawing;

namespace FalconOcr.Imaging
{
    public static class ImageOps
    {
        /// <summary>Bilinear resize (area-averaged when shrinking by more than 2x).</summary>
        public static RgbImage Resize(RgbImage src, int w, int h)
        {
            var dst = new RgbImage(w, h);
            float sx = (float)src.Width / w, sy = (float)src.Height / h;
            int nx = Math.Max(1, (int)Math.Floor(sx)), ny = Math.Max(1, (int)Math.Floor(sy));
            if (nx > 4) nx = 4;
            if (ny > 4) ny = 4;
            var px = new float[3];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (int j = 0; j < ny; j++)
                    {
                        float fy = (y + (j + 0.5f) / ny) * sy - 0.5f;
                        for (int i = 0; i < nx; i++)
                        {
                            float fx = (x + (i + 0.5f) / nx) * sx - 0.5f;
                            Sample(src, fx, fy, px);
                            b += px[0]; g += px[1]; r += px[2];
                        }
                    }
                    float n = nx * ny;
                    int o = (y * w + x) * 3;
                    dst.Data[o] = ClampByte(b / n);
                    dst.Data[o + 1] = ClampByte(g / n);
                    dst.Data[o + 2] = ClampByte(r / n);
                }
            }
            return dst;
        }

        /// <summary>Bilinear sample, BGR into <paramref name="bgr"/>. Out-of-range coordinates clamp to the edge.</summary>
        public static void Sample(RgbImage src, float fx, float fy, float[] bgr)
        {
            int w = src.Width, h = src.Height;
            if (fx < 0) fx = 0; else if (fx > w - 1) fx = w - 1;
            if (fy < 0) fy = 0; else if (fy > h - 1) fy = h - 1;
            int x0 = (int)fx, y0 = (int)fy;
            int x1 = x0 + 1 < w ? x0 + 1 : x0, y1 = y0 + 1 < h ? y0 + 1 : y0;
            float ax = fx - x0, ay = fy - y0;
            var d = src.Data;
            int i00 = (y0 * w + x0) * 3, i01 = (y0 * w + x1) * 3, i10 = (y1 * w + x0) * 3, i11 = (y1 * w + x1) * 3;
            for (int c = 0; c < 3; c++)
            {
                float top = d[i00 + c] + (d[i01 + c] - d[i00 + c]) * ax;
                float bot = d[i10 + c] + (d[i11 + c] - d[i10 + c]) * ax;
                bgr[c] = top + (bot - top) * ay;
            }
        }

        /// <summary>
        /// Extracts the quadrilateral (TL, TR, BR, BL) as an upright w x h image (affine warp, like
        /// PaddleOCR's get_rotate_crop_image). Tall crops (h/w &gt;= 1.5) are rotated 90° so vertical text reads left-to-right.
        /// </summary>
        public static RgbImage CropQuad(RgbImage src, PointF[] q)
        {
            float w = Math.Max(Dist(q[0], q[1]), Dist(q[3], q[2]));
            float h = Math.Max(Dist(q[0], q[3]), Dist(q[1], q[2]));
            int iw = Math.Max(1, (int)Math.Round(w)), ih = Math.Max(1, (int)Math.Round(h));
            var dst = new RgbImage(iw, ih);
            // Axis vectors of the quad (per output pixel).
            float ux = (q[1].X - q[0].X) / iw, uy = (q[1].Y - q[0].Y) / iw;
            float vx = (q[3].X - q[0].X) / ih, vy = (q[3].Y - q[0].Y) / ih;
            var px = new float[3];
            for (int y = 0; y < ih; y++)
            {
                for (int x = 0; x < iw; x++)
                {
                    float fx = q[0].X + ux * (x + 0.5f) + vx * (y + 0.5f) - 0.5f;
                    float fy = q[0].Y + uy * (x + 0.5f) + vy * (y + 0.5f) - 0.5f;
                    Sample(src, fx, fy, px);
                    int o = (y * iw + x) * 3;
                    dst.Data[o] = ClampByte(px[0]);
                    dst.Data[o + 1] = ClampByte(px[1]);
                    dst.Data[o + 2] = ClampByte(px[2]);
                }
            }
            if (ih >= iw * 1.5f) return Rotate(dst, 270);
            return dst;
        }

        /// <summary>Rotates clockwise by 90, 180 or 270 degrees.</summary>
        public static RgbImage Rotate(RgbImage src, int degrees)
        {
            degrees = ((degrees % 360) + 360) % 360;
            if (degrees == 0) return src;
            int w = src.Width, h = src.Height;
            var dst = degrees == 180 ? new RgbImage(w, h) : new RgbImage(h, w);
            var s = src.Data;
            var d = dst.Data;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx, dy;
                    switch (degrees)
                    {
                        case 90: dx = h - 1 - y; dy = x; break;
                        case 180: dx = w - 1 - x; dy = h - 1 - y; break;
                        default: dx = y; dy = w - 1 - x; break; // 270
                    }
                    int si = (y * w + x) * 3, di = (dy * dst.Width + dx) * 3;
                    d[di] = s[si];
                    d[di + 1] = s[si + 1];
                    d[di + 2] = s[si + 2];
                }
            }
            return dst;
        }

        /// <summary>Otsu threshold of a gray histogram.</summary>
        public static int Otsu(int[] hist)
        {
            long total = 0, sum = 0;
            for (int i = 0; i < 256; i++) { total += hist[i]; sum += (long)i * hist[i]; }
            if (total == 0) return 128;
            long wB = 0, sumB = 0;
            double best = -1;
            int thr = 128;
            for (int t = 0; t < 256; t++)
            {
                wB += hist[t];
                if (wB == 0) continue;
                long wF = total - wB;
                if (wF == 0) break;
                sumB += (long)t * hist[t];
                double mB = (double)sumB / wB, mF = (double)(sum - sumB) / wF;
                double between = (double)wB * wF * (mB - mF) * (mB - mF);
                if (between > best) { best = between; thr = t; }
            }
            return thr;
        }

        public static float Dist(PointF a, PointF b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static byte ClampByte(float v) => v <= 0 ? (byte)0 : v >= 255 ? (byte)255 : (byte)(v + 0.5f);
    }
}
