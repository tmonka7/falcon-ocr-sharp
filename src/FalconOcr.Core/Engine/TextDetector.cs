using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FalconOcr.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FalconOcr.Engine
{
    public sealed class DetectedBox
    {
        /// <summary>TL, TR, BR, BL in source image pixels.</summary>
        public PointF[] Quad;
        public float Score;
        public RectangleF Bounds => Geometry.BoundsOf(Quad);
    }

    /// <summary>PP-OCR DBNet text detector (probability map + box post-processing).</summary>
    public sealed class TextDetector : IDisposable
    {
        private static readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
        private static readonly float[] Std = { 0.229f, 0.224f, 0.225f };

        private readonly InferenceSession _session;
        private readonly string _input;

        public TextDetector(string modelPath, SessionOptions options)
        {
            _session = new InferenceSession(modelPath, options);
            _input = _session.InputMetadata.Keys.First();
        }

        public List<DetectedBox> Detect(RgbImage img, OcrOptions o)
        {
            int w = img.Width, h = img.Height;
            float scale = 1f;
            int maxSide = Math.Max(w, h);
            if (maxSide > o.DetMaxSide) scale = (float)o.DetMaxSide / maxSide;
            // Tiny inputs (e.g. a cropped word) get upscaled so strokes survive the network's 4x downsampling.
            if (Math.Min(w, h) * scale < 64) scale = 64f / Math.Min(w, h);
            int rw = Math.Max(32, (int)Math.Round(w * scale / 32f) * 32);
            int rh = Math.Max(32, (int)Math.Round(h * scale / 32f) * 32);
            var resized = rw == w && rh == h ? img : ImageOps.Resize(img, rw, rh);

            var tensor = new DenseTensor<float>(new[] { 1, 3, rh, rw });
            var buf = tensor.Buffer.Span;
            int plane = rw * rh;
            var d = resized.Data;
            // BGR channel order with ImageNet statistics, exactly as PaddleOCR's NormalizeImage on a cv2 image.
            for (int i = 0, j = 0; i < plane; i++, j += 3)
            {
                buf[i] = (d[j] / 255f - Mean[0]) / Std[0];
                buf[plane + i] = (d[j + 1] / 255f - Mean[1]) / Std[1];
                buf[2 * plane + i] = (d[j + 2] / 255f - Mean[2]) / Std[2];
            }

            float[] prob;
            using (var results = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_input, tensor) }))
            {
                var t = results.First().AsTensor<float>();
                prob = t is DenseTensor<float> dt ? dt.Buffer.ToArray() : t.ToArray();
            }

            var boxes = PostProcess(prob, rw, rh, o);
            float sx = (float)w / rw, sy = (float)h / rh;
            foreach (var b in boxes)
            {
                for (int i = 0; i < 4; i++)
                    b.Quad[i] = new PointF(Clamp(b.Quad[i].X * sx, 0, w), Clamp(b.Quad[i].Y * sy, 0, h));
            }
            return boxes.OrderBy(b => b.Bounds.Top).ThenBy(b => b.Bounds.Left).ToList();
        }

        private static List<DetectedBox> PostProcess(float[] prob, int w, int h, OcrOptions o)
        {
            var result = new List<DetectedBox>();
            var labels = new int[w * h];
            var stack = new Stack<int>();
            int label = 0;
            float thr = o.DetThreshold;
            var rowMin = new Dictionary<int, int>();
            var rowMax = new Dictionary<int, int>();

            for (int start = 0; start < prob.Length; start++)
            {
                if (labels[start] != 0 || prob[start] <= thr) continue;
                label++;
                labels[start] = label;
                stack.Push(start);
                rowMin.Clear();
                rowMax.Clear();
                int count = 0;
                while (stack.Count > 0)
                {
                    int p = stack.Pop();
                    int px = p % w, py = p / w;
                    count++;
                    if (!rowMin.TryGetValue(py, out var mn) || px < mn) rowMin[py] = px;
                    if (!rowMax.TryGetValue(py, out var mx) || px > mx) rowMax[py] = px;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = py + dy;
                        if (ny < 0 || ny >= h) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = px + dx;
                            if (nx < 0 || nx >= w || (dx == 0 && dy == 0)) continue;
                            int q = ny * w + nx;
                            if (labels[q] == 0 && prob[q] > thr)
                            {
                                labels[q] = label;
                                stack.Push(q);
                            }
                        }
                    }
                }
                if (count < 4) continue;

                // Pixel-corner hull of the component from its per-row extents.
                var pts = new List<PointF>(rowMin.Count * 4);
                foreach (var kv in rowMin)
                {
                    int y = kv.Key;
                    pts.Add(new PointF(kv.Value, y));
                    pts.Add(new PointF(kv.Value, y + 1));
                    pts.Add(new PointF(rowMax[y] + 1, y));
                    pts.Add(new PointF(rowMax[y] + 1, y + 1));
                }
                var hull = Geometry.ConvexHull(pts);
                var rect = Geometry.MinAreaRect(hull);
                if (Math.Min(rect.W, rect.H) < 3) continue;

                float score = BoxScore(prob, w, h, rect);
                if (score < o.BoxThreshold) continue;

                // Unclip: DB shrinks text kernels by D = A * r / L; grow the box back by the same offset.
                float dist = rect.W * rect.H * o.UnclipRatio / (2 * (rect.W + rect.H));
                rect.W += 2 * dist;
                rect.H += 2 * dist;
                if (Math.Min(rect.W, rect.H) < 5) continue;

                result.Add(new DetectedBox { Quad = rect.Normalized().Corners(), Score = score });
                if (result.Count >= 3000) break;
            }
            return result;
        }

        /// <summary>Mean probability inside the rotated rectangle (PaddleOCR "fast" box score).</summary>
        private static float BoxScore(float[] prob, int w, int h, RotatedRect r)
        {
            var corners = r.Corners();
            var b = Geometry.BoundsOf(corners);
            int x0 = Math.Max(0, (int)Math.Floor(b.Left)), x1 = Math.Min(w - 1, (int)Math.Ceiling(b.Right));
            int y0 = Math.Max(0, (int)Math.Floor(b.Top)), y1 = Math.Min(h - 1, (int)Math.Ceiling(b.Bottom));
            float c = (float)Math.Cos(r.Angle), s = (float)Math.Sin(r.Angle);
            float hw = r.W / 2, hh = r.H / 2;
            double sum = 0;
            int n = 0;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - r.Cx, dy = y + 0.5f - r.Cy;
                    float u = dx * c + dy * s, v = -dx * s + dy * c;
                    if (Math.Abs(u) > hw || Math.Abs(v) > hh) continue;
                    sum += prob[y * w + x];
                    n++;
                }
            }
            return n == 0 ? 0 : (float)(sum / n);
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

        public void Dispose() => _session.Dispose();
    }
}
