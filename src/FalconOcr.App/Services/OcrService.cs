using System;
using FalconOcr.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FalconOcr.Engine;
using FalconOcr.Input;
using FalconOcr.Model;
using FalconOcr.Processing;

namespace FalconOcr.App.Services
{
    /// <summary>
    /// Single shared OCR pipeline for the whole application. Jobs run on a worker thread one at a time
    /// (the ONNX sessions are reused; recognition itself is multi-threaded inside ONNX Runtime).
    /// </summary>
    internal sealed class OcrService : IDisposable
    {
        private readonly OcrProcessor _processor = new OcrProcessor();
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public bool Busy => _gate.CurrentCount == 0;

        public static List<string> MissingModels(OcrLanguage? lang = null) => PaddleOcrEngine.FindMissingModels(LanguageCatalog.DefaultModelDirectory, lang);

        /// <summary>Loads the models in the background so the first recognition starts immediately.</summary>
        public Task WarmupAsync(OcrOptions o)
        {
            var opts = o.Clone();
            return Task.Run(async () =>
            {
                await _gate.WaitAsync().ConfigureAwait(false);
                try { _processor.Engine.Warmup(opts); }
                catch { /* reported when recognition is actually requested */ }
                finally { _gate.Release(); }
            });
        }

        /// <summary>Recognizes the given pages; <paramref name="pageDone"/> is raised (on the worker thread) after each page.</summary>
        public async Task RecognizeAsync(IList<DocumentPage> pages, OcrOptions o, IProgress<PageProgress> progress, Action<DocumentPage> pageDone, CancellationToken ct)
        {
            var opts = o.Clone();
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < pages.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var p = pages[i];
                        progress?.Report(new PageProgress { Document = p.Document, Page = p, Done = i, Total = pages.Count, Message = L.F("Recognizing {0} — {1} ({2}/{3})", p.Document.Name, p.Label, i + 1, pages.Count) });
                        int version = p.Version;
                        var result = _processor.ProcessPage(p, opts, ct);
                        if (p.Version == version) p.Result = result; // page was not rotated/cropped meanwhile
                        pageDone?.Invoke(p);
                    }
                    progress?.Report(new PageProgress { Done = pages.Count, Total = pages.Count, Message = L.T("Text recognition completed.") });
                }, ct).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public Task<OcrPage> RecognizePageAsync(DocumentPage page, OcrOptions o, CancellationToken ct)
        {
            var list = new List<DocumentPage> { page };
            return RecognizeAsync(list, o, null, null, ct).ContinueWith(t =>
            {
                if (t.IsFaulted) throw t.Exception.InnerException ?? t.Exception;
                if (t.IsCanceled) throw new OperationCanceledException();
                return page.Result;
            }, TaskScheduler.Default);
        }

        public void Dispose()
        {
            _processor.Dispose();
            _gate.Dispose();
        }
    }
}
