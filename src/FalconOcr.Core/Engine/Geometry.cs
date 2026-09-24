using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace FalconOcr.Engine
{
    public static class Geometry
    {
        /// <summary>Andrew's monotone chain. Returns the hull counter-clockwise without the repeated first point.</summary>
        public static List<PointF> ConvexHull(List<PointF> pts)
        {
            if (pts.Count < 3) return new List<PointF>(pts);
            var p = pts.OrderBy(a => a.X).ThenBy(a => a.Y).ToList();
            var hull = new PointF[p.Count * 2];
            int k = 0;
            for (int i = 0; i < p.Count; i++)
            {
                while (k >= 2 && Cross(hull[k - 2], hull[k - 1], p[i]) <= 0) k--;
                hull[k++] = p[i];
            }
            for (int i = p.Count - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(hull[k - 2], hull[k - 1], p[i]) <= 0) k--;
                hull[k++] = p[i];
            }
            return hull.Take(k - 1).ToList();
        }

        private static float Cross(PointF o, PointF a, PointF b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

        /// <summary>Minimum-area enclosing rectangle of a convex hull (rotating calipers).</summary>
        public static RotatedRect MinAreaRect(List<PointF> hull)
        {
            if (hull.Count == 0) return new RotatedRect();
            if (hull.Count < 3)
            {
                float minX = hull.Min(q => q.X), maxX = hull.Max(q => q.X), minY = hull.Min(q => q.Y), maxY = hull.Max(q => q.Y);
                return new RotatedRect { Cx = (minX + maxX) / 2, Cy = (minY + maxY) / 2, W = maxX - minX, H = maxY - minY, Angle = 0 };
            }
            double bestArea = double.MaxValue;
            var best = new RotatedRect();
            for (int i = 0; i < hull.Count; i++)
            {
                var a = hull[i];
                var b = hull[(i + 1) % hull.Count];
                double ex = b.X - a.X, ey = b.Y - a.Y;
                double len = Math.Sqrt(ex * ex + ey * ey);
                if (len < 1e-6) continue;
                ex /= len; ey /= len;
                double minU = double.MaxValue, maxU = double.MinValue, minV = double.MaxValue, maxV = double.MinValue;
                foreach (var q in hull)
                {
                    double u = q.X * ex + q.Y * ey;
                    double v = -q.X * ey + q.Y * ex;
                    if (u < minU) minU = u;
                    if (u > maxU) maxU = u;
                    if (v < minV) minV = v;
                    if (v > maxV) maxV = v;
                }
                double area = (maxU - minU) * (maxV - minV);
                if (area < bestArea)
                {
                    bestArea = area;
                    double cu = (minU + maxU) / 2, cv = (minV + maxV) / 2;
                    best = new RotatedRect
                    {
                        Cx = (float)(cu * ex - cv * ey),
                        Cy = (float)(cu * ey + cv * ex),
                        W = (float)(maxU - minU),
                        H = (float)(maxV - minV),
                        Angle = (float)Math.Atan2(ey, ex)
                    };
                }
            }
            return best.Normalized();
        }

        public static float PolygonArea(IList<PointF> p)
        {
            double a = 0;
            for (int i = 0; i < p.Count; i++)
            {
                var c = p[i];
                var n = p[(i + 1) % p.Count];
                a += c.X * n.Y - n.X * c.Y;
            }
            return (float)Math.Abs(a / 2);
        }

        public static float PolygonPerimeter(IList<PointF> p)
        {
            double s = 0;
            for (int i = 0; i < p.Count; i++)
            {
                var c = p[i];
                var n = p[(i + 1) % p.Count];
                s += Math.Sqrt((c.X - n.X) * (c.X - n.X) + (c.Y - n.Y) * (c.Y - n.Y));
            }
            return (float)s;
        }

        public static RectangleF BoundsOf(IEnumerable<PointF> pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
            return minX > maxX ? RectangleF.Empty : RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }

        public static RectangleF Union(IEnumerable<RectangleF> rects)
        {
            RectangleF u = RectangleF.Empty;
            bool first = true;
            foreach (var r in rects)
            {
                u = first ? r : RectangleF.Union(u, r);
                first = false;
            }
            return u;
        }

        /// <summary>Overlap of the intervals divided by the smaller length.</summary>
        public static float OverlapRatio(float a0, float a1, float b0, float b1)
        {
            float inter = Math.Min(a1, b1) - Math.Max(a0, b0);
            float min = Math.Min(a1 - a0, b1 - b0);
            return inter <= 0 || min <= 0 ? 0 : inter / min;
        }

        /// <summary>Fraction of <paramref name="a"/>'s area inside <paramref name="b"/>.</summary>
        public static float Coverage(RectangleF a, RectangleF b)
        {
            var i = RectangleF.Intersect(a, b);
            if (i.Width <= 0 || i.Height <= 0 || a.Width <= 0 || a.Height <= 0) return 0;
            return i.Width * i.Height / (a.Width * a.Height);
        }
    }

    public struct RotatedRect
    {
        public float Cx, Cy, W, H;
        /// <summary>Radians; direction of the W side.</summary>
        public float Angle;

        /// <summary>Makes W the long-ish reading direction for near-horizontal text (angle in (-45°, 45°]).</summary>
        public RotatedRect Normalized()
        {
            var r = this;
            double deg = r.Angle * 180 / Math.PI;
            while (deg > 45) { deg -= 90; Swap(ref r); }
            while (deg <= -45) { deg += 90; Swap(ref r); }
            r.Angle = (float)(deg * Math.PI / 180);
            return r;
        }

        private static void Swap(ref RotatedRect r)
        {
            float t = r.W; r.W = r.H; r.H = t;
        }

        /// <summary>Corners TL, TR, BR, BL (image coordinates, y down).</summary>
        public PointF[] Corners()
        {
            float c = (float)Math.Cos(Angle), s = (float)Math.Sin(Angle);
            float hw = W / 2, hh = H / 2, cx = Cx, cy = Cy;
            PointF P(float u, float v) => new PointF(cx + u * c - v * s, cy + u * s + v * c);
            return new[] { P(-hw, -hh), P(hw, -hh), P(hw, hh), P(-hw, hh) };
        }
    }
}
