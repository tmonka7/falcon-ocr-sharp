using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using FalconOcr.Imaging;
using FalconOcr.Model;
using Microsoft.ML.OnnxRuntime;

namespace FalconOcr.Engine
{
    /// <summary>
    /// Offline PaddleOCR (PP-OCRv5) pipeline on ONNX Runtime: detection → optional orientation → recognition.
    /// Thread-safe for sequential use from one worker; sessions are loaded lazily and cached per language.
    /// </summary>
    public sealed class PaddleOcrEngine : IDisposable
    {
        private readonly string _modelDir;
        private readonly object _sync = new object();
        private SessionOptions _sessionOptions;
        private int _threads = -1;
        private TextDetector _det;
        private TextOrientationClassifier _cls;
        private readonly Dictionary<string, TextRecognizer> _rec = new Dictionary<string, TextRecognizer>(StringComparer.OrdinalIgnoreCase);

        public PaddleOcrEngine(string modelDirectory = null)
        {
            _modelDir = modelDirectory ?? LanguageCatalog.DefaultModelDirectory;
        }

        public string ModelDirectory => _modelDir;

        /// <summary>Lists missing model files (empty when the installation is complete).</summary>
        public static List<string> FindMissingModels(string modelDir, OcrLanguage? language = null)
        {
            var files = new List<string> { LanguageCatalog.DetModel, LanguageCatalog.ClsModel };
            foreach (var l in LanguageCatalog.All.Where(l => language == null || l.Language == language))
            {
                files.Add(l.RecModel);
                files.Add(l.Dictionary);
            }
            return files.Distinct().Where(f => !File.Exists(Path.Combine(modelDir, f))).ToList();
        }

        private SessionOptions GetSessionOptions(OcrOptions o)
        {
            // 0 = ONNX Runtime default (one thread per physical core); logical-core counts oversubscribe
            // hyper-threaded CPUs and run about 2x slower.
            int threads = Math.Max(0, o.Threads);
            if (_sessionOptions != null && threads == _threads) return _sessionOptions;
            // Thread count changed: sessions created with the old options are rebuilt.
            DisposeSessions();
            _threads = threads;
            _sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                IntraOpNumThreads = threads,
                InterOpNumThreads = 1
            };
            return _sessionOptions;
        }

        /// <summary>Loads the models for the given language (so the first page is not slowed down).</summary>
        public void Warmup(OcrOptions o)
        {
            lock (_sync)
            {
                var so = GetSessionOptions(o);
                EnsureDetector(so);
                EnsureRecognizer(o.Language, so);
            }
        }

        private TextDetector EnsureDetector(SessionOptions so)
        {
            return _det ?? (_det = new TextDetector(Path.Combine(_modelDir, LanguageCatalog.DetModel), so));
        }

        private TextRecognizer EnsureRecognizer(OcrLanguage lang, SessionOptions so)
        {
            var lm = LanguageCatalog.Get(lang);
            if (!_rec.TryGetValue(lm.RecModel, out var r))
            {
                r = new TextRecognizer(Path.Combine(_modelDir, lm.RecModel), Path.Combine(_modelDir, lm.Dictionary), so);
                _rec[lm.RecModel] = r;
            }
            return r;
        }

        /// <summary>Recognizes all text lines on a page image.</summary>
        public List<TextLine> Recognize(RgbImage image, OcrOptions o, CancellationToken ct = default(CancellationToken))
        {
            lock (_sync)
            {
                var so = GetSessionOptions(o);
                var det = EnsureDetector(so);
                var rec = EnsureRecognizer(o.Language, so);

                var boxes = det.Detect(image, o);
                ct.ThrowIfCancellationRequested();
                if (boxes.Count == 0) return new List<TextLine>();

                var crops = boxes.Select(b => ImageOps.CropQuad(image, b.Quad)).ToList();

                if (o.UseAngleClassifier)
                {
                    if (_cls == null) _cls = new TextOrientationClassifier(Path.Combine(_modelDir, LanguageCatalog.ClsModel), so);
                    var flip = _cls.IsRotated180(crops);
                    for (int i = 0; i < crops.Count; i++)
                    {
                        if (!flip[i]) continue;
                        crops[i] = ImageOps.Rotate(crops[i], 180);
                        var q = boxes[i].Quad;
                        boxes[i].Quad = new[] { q[2], q[3], q[0], q[1] };
                    }
                }
                ct.ThrowIfCancellationRequested();

                var texts = rec.Recognize(crops, Math.Max(1, o.RecBatchSize));
                ct.ThrowIfCancellationRequested();

                var lines = new List<TextLine>();
                for (int i = 0; i < boxes.Count; i++)
                {
                    var t = texts[i];
                    var text = t.Text.Trim();
                    if (text.Length == 0 || t.Confidence < o.MinConfidence) continue;
                    lines.Add(ToLine(boxes[i], t));
                }
                for (int i = 0; i < lines.Count; i++) lines[i].Id = i + 1;
                return lines;
            }
        }

        private static TextLine ToLine(DetectedBox box, RecognizedText t)
        {
            var bounds = box.Bounds;
            var line = new TextLine
            {
                Polygon = box.Quad,
                Bounds = bounds,
                Confidence = t.Confidence
            };

            // Trim surrounding whitespace while keeping per-char data aligned.
            int s = 0, e = t.Text.Length;
            while (s < e && char.IsWhiteSpace(t.Text[s])) s++;
            while (e > s && char.IsWhiteSpace(t.Text[e - 1])) e--;
            line.Text = t.Text.Substring(s, e - s);

            int n = line.Text.Length;
            var centers = t.CharCenters.Skip(s).Take(n).ToArray();
            line.CharConfidence = t.CharConfidence.Skip(s).Take(n).ToArray();
            if (centers.Length == n && n > 0)
            {
                // Map crop fractions onto the page along the line's reading direction (projected on x).
                float x0 = (box.Quad[0].X + box.Quad[3].X) / 2, x1 = (box.Quad[1].X + box.Quad[2].X) / 2;
                float len = x1 - x0;
                line.CharLeft = new float[n];
                line.CharRight = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float c = centers[i];
                    float l = i > 0 ? (centers[i - 1] + c) / 2 : Math.Max(0, c - (n > 1 ? (centers[1] - c) / 2 : 0.5f));
                    float r = i < n - 1 ? (c + centers[i + 1]) / 2 : Math.Min(1, c + (n > 1 ? (c - centers[n - 2]) / 2 : 0.5f));
                    line.CharLeft[i] = x0 + l * len;
                    line.CharRight[i] = x0 + r * len;
                }
            }
            return line;
        }

        private void DisposeSessions()
        {
            _det?.Dispose();
            _det = null;
            _cls?.Dispose();
            _cls = null;
            foreach (var r in _rec.Values) r.Dispose();
            _rec.Clear();
            _sessionOptions?.Dispose();
            _sessionOptions = null;
        }

        public void Dispose()
        {
            lock (_sync) DisposeSessions();
        }
    }
}
