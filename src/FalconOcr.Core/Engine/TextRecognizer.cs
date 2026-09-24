using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FalconOcr.Imaging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FalconOcr.Engine
{
    public sealed class RecognizedText
    {
        public string Text = "";
        public float Confidence;
        /// <summary>Character centers as a fraction (0..1) of the crop width.</summary>
        public float[] CharCenters = new float[0];
        public float[] CharConfidence = new float[0];
    }

    /// <summary>PP-OCR SVTR/CTC text-line recognizer.</summary>
    public sealed class TextRecognizer : IDisposable
    {
        private const int InputHeight = 48;
        private readonly InferenceSession _session;
        private readonly string _input;
        private readonly string[] _chars; // [0] = CTC blank, [n+1] = space

        public TextRecognizer(string modelPath, string dictPath, SessionOptions options)
        {
            _session = new InferenceSession(modelPath, options);
            _input = _session.InputMetadata.Keys.First();

            List<string> dict;
            if (File.Exists(dictPath))
            {
                dict = File.ReadAllText(dictPath, Encoding.UTF8).Split('\n').Select(s => s.TrimEnd('\r')).ToList();
                if (dict.Count > 0 && dict[dict.Count - 1].Length == 0) dict.RemoveAt(dict.Count - 1);
            }
            else if (_session.ModelMetadata.CustomMetadataMap.TryGetValue("character", out var meta))
            {
                dict = meta.Split('\n').ToList();
            }
            else throw new FileNotFoundException("Recognition dictionary not found", dictPath);

            _chars = new[] { "" }.Concat(dict).Concat(new[] { " " }).ToArray();
        }

        public RecognizedText[] Recognize(IList<RgbImage> crops, int batchSize)
        {
            var results = new RecognizedText[crops.Count];
            // Similar aspect ratios in one batch keep padding (wasted compute) small.
            var order = Enumerable.Range(0, crops.Count).OrderBy(i => (float)crops[i].Width / crops[i].Height).ToArray();
            for (int b = 0; b < order.Length; b += batchSize)
            {
                var idx = order.Skip(b).Take(batchSize).ToArray();
                float maxRatio = 320f / InputHeight;
                foreach (var i in idx) maxRatio = Math.Max(maxRatio, (float)crops[i].Width / crops[i].Height);
                int inW = Math.Min(4800, (int)Math.Ceiling(InputHeight * maxRatio));

                var tensor = new DenseTensor<float>(new[] { idx.Length, 3, InputHeight, inW });
                var buf = tensor.Buffer.Span;
                var widths = new int[idx.Length];
                int plane = InputHeight * inW;
                for (int k = 0; k < idx.Length; k++)
                {
                    var crop = crops[idx[k]];
                    int rw = Math.Min(inW, Math.Max(1, (int)Math.Ceiling(InputHeight * (float)crop.Width / crop.Height)));
                    widths[k] = rw;
                    var r = ImageOps.Resize(crop, rw, InputHeight);
                    int off = k * 3 * plane;
                    for (int y = 0; y < InputHeight; y++)
                    {
                        for (int x = 0; x < rw; x++)
                        {
                            int s = (y * rw + x) * 3, p = y * inW + x;
                            buf[off + p] = r.Data[s] / 127.5f - 1f;
                            buf[off + plane + p] = r.Data[s + 1] / 127.5f - 1f;
                            buf[off + 2 * plane + p] = r.Data[s + 2] / 127.5f - 1f;
                        }
                    }
                }

                using (var output = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_input, tensor) }))
                {
                    var t = output.First().AsTensor<float>();
                    int steps = t.Dimensions[1], classes = t.Dimensions[2];
                    var data = t is DenseTensor<float> dt ? dt.Buffer.ToArray() : t.ToArray();
                    for (int k = 0; k < idx.Length; k++)
                        results[idx[k]] = Decode(data, k * steps * classes, steps, classes, inW, widths[k]);
                }
            }
            return results;
        }

        private RecognizedText Decode(float[] data, int offset, int steps, int classes, int inW, int realW)
        {
            var sb = new StringBuilder();
            var centers = new List<float>();
            var confs = new List<float>();
            int prev = 0;
            int runStart = 0;
            float stepW = (float)inW / steps;
            for (int t = 0; t < steps; t++)
            {
                int o = offset + t * classes;
                int best = 0;
                float bp = data[o];
                for (int c = 1; c < classes; c++)
                {
                    if (data[o + c] > bp) { bp = data[o + c]; best = c; }
                }
                if (best != prev)
                {
                    if (best != 0 && best < _chars.Length)
                    {
                        var sym = _chars[best];
                        sb.Append(sym);
                        // One position per UTF-16 unit so that positions stay aligned with string indices.
                        for (int u = 0; u < sym.Length; u++)
                        {
                            confs.Add(bp);
                            centers.Add(Math.Min(1f, (t + 0.5f) * stepW / realW));
                        }
                        runStart = t;
                    }
                }
                else if (best != 0 && confs.Count > 0)
                {
                    // Repeated emission of the same symbol: move its center to the middle of the run.
                    int units = _chars[best].Length;
                    float c = Math.Min(1f, ((runStart + t) / 2f + 0.5f) * stepW / realW);
                    for (int u = 1; u <= units && u <= centers.Count; u++)
                    {
                        centers[centers.Count - u] = c;
                        confs[confs.Count - u] = Math.Max(confs[confs.Count - u], bp);
                    }
                }
                prev = best;
            }
            var text = sb.ToString();
            // A multi-char mapping would break the char<->position alignment; dictionaries are 1 char per entry.
            if (text.Length != centers.Count)
            {
                centers = Enumerable.Range(0, text.Length).Select(i => (i + 0.5f) / Math.Max(1, text.Length)).ToList();
                confs = Enumerable.Repeat(confs.Count == 0 ? 0f : confs.Average(), text.Length).ToList();
            }
            return new RecognizedText
            {
                Text = text,
                Confidence = confs.Count == 0 ? 0 : confs.Average(),
                CharCenters = centers.ToArray(),
                CharConfidence = confs.ToArray()
            };
        }

        public void Dispose() => _session.Dispose();
    }

    /// <summary>PP-LCNet text-line orientation classifier (0° / 180°).</summary>
    public sealed class TextOrientationClassifier : IDisposable
    {
        private static readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
        private static readonly float[] Std = { 0.229f, 0.224f, 0.225f };
        private readonly InferenceSession _session;
        private readonly string _input;
        private readonly int _w, _h;

        public TextOrientationClassifier(string modelPath, SessionOptions options)
        {
            _session = new InferenceSession(modelPath, options);
            var meta = _session.InputMetadata.First();
            _input = meta.Key;
            var dims = meta.Value.Dimensions;
            _h = dims.Length == 4 && dims[2] > 0 ? dims[2] : 80;
            _w = dims.Length == 4 && dims[3] > 0 ? dims[3] : 160;
        }

        /// <summary>Returns true for each crop that is upside down (with confidence above 0.9).</summary>
        public bool[] IsRotated180(IList<RgbImage> crops, int batchSize = 16)
        {
            var res = new bool[crops.Count];
            for (int b = 0; b < crops.Count; b += batchSize)
            {
                int n = Math.Min(batchSize, crops.Count - b);
                var tensor = new DenseTensor<float>(new[] { n, 3, _h, _w });
                var buf = tensor.Buffer.Span;
                int plane = _w * _h;
                for (int k = 0; k < n; k++)
                {
                    var r = ImageOps.Resize(crops[b + k], _w, _h);
                    int off = k * 3 * plane;
                    for (int i = 0, j = 0; i < plane; i++, j += 3)
                    {
                        // RGB order for the PaddleX classifier.
                        buf[off + i] = (r.Data[j + 2] / 255f - Mean[0]) / Std[0];
                        buf[off + plane + i] = (r.Data[j + 1] / 255f - Mean[1]) / Std[1];
                        buf[off + 2 * plane + i] = (r.Data[j] / 255f - Mean[2]) / Std[2];
                    }
                }
                using (var output = _session.Run(new[] { NamedOnnxValue.CreateFromTensor(_input, tensor) }))
                {
                    var t = output.First().AsTensor<float>();
                    for (int k = 0; k < n; k++)
                        res[b + k] = t[k, 1] > t[k, 0] && t[k, 1] > 0.9f;
                }
            }
            return res;
        }

        public void Dispose() => _session.Dispose();
    }
}
