from docbuilder import Doc


def build():
    d = Doc("System Design Document", "Architecture, components, algorithms, data and licensing design", "FOCR-SDD-001",
            "This document describes how Falcon OCR 1.0 is built: architecture, components, processing algorithms, data model, "
            "export mapping, user interface, licensing, storage, build and deployment. It is written for developers who maintain or extend the product.")

    # ================================================================== 1
    d.h1("1. Introduction")
    d.h2("1.1 Purpose, audience and scope")
    d.p("The System Design Document (SDD) explains the internal structure of Falcon OCR and the reasons behind the main design decisions. "
        "It is intended for developers, reviewers and testers. Requirement identifiers (FR-nn / NFR-nn) refer to the Software Requirements Specification FOCR-SRS-001.",
        "The document covers the four projects of FalconOcr.sln (core library, desktop application, command-line tool, vendor key generator), the model and native-library layout and the build scripts. "
        "Third-party components (PaddleOCR networks, ONNX Runtime, PDFium, Open XML SDK) are treated as black boxes with a documented interface. "
        "Every class, method and threshold named here exists in the source tree of release 1.0 (state of 2026-09-24).")
    d.h2("1.2 Design goals")
    d.table(["Goal", "Design decision"], [
        ("Fully offline (NFR-02)", "PaddleOCR models as ONNX files in the repository; ONNX Runtime and PDFium as bundled native DLLs; NuGet packages vendored in packages\\ with a local-only nuget.config. No code path opens a network connection."),
        ("Layout fidelity (FR-15…FR-20)", "Every recognized element keeps its page coordinates; a layout analyser rebuilds sections, columns, blocks, tables and figures; exporters map this model to Word, Excel and HTML, both as flow and as exact positions."),
        ("No heavy imaging dependency", "Image processing (resize, rotated crop, binarisation, connected components, convex hull, minimum-area rectangle) is implemented in C#; no OpenCV."),
        (".NET Framework 4.7 / WinForms (NFR-01)", "SDK-style projects targeting net47, C# 7.3; UI built in code with custom-drawn controls."),
        ("Separation of concerns (NFR-08)", "UI-independent FalconOcr.Core used by the desktop app, the CLI and the KeyGen."),
        ("Tamper-resistant licensing (FR-34/35)", "Asymmetric signatures (ECDSA P-256) — the app can verify but not create keys; HMAC-protected trial state stored twice."),
        ("Robustness (NFR-06)", "Atomic writes (temporary file + move) for exports and settings; persistence reads fall back to defaults; one global crash handler."),
    ], [30, 70], "Design goals and decisions")
    d.h2("1.3 Definitions")
    d.table(["Term", "Meaning"], [
        ("Processed image", "The page image that is recognized: original rendering → rotation → crop. All result coordinates are pixels of this image."),
        ("Segment / visual line", "One detector box (TextLine); segments on the same baseline form a visual line (LayoutLine)."),
        ("Block / section", "A paragraph, heading, list item, table or figure in reading order (LayoutBlock) / a horizontal band with a constant set of columns (LayoutSection)."),
        ("Gutter", "A vertical strip without content that separates two columns."),
        ("DBNet / SVTR / CTC", "Text detector producing a probability map / recognition network / its Connectionist Temporal Classification output."),
        ("Twip / EMU", "Word units: 1/1440 inch / 1/914 400 inch."),
    ], [25, 75], "Definitions")

    # ================================================================== 2
    d.h1("2. Architecture")
    d.h2("2.1 Component overview")
    d.img("diagram-architecture.png", "Component architecture", 610)
    d.table(["Project", "Type", "Responsibility"], [
        ("FalconOcr.Core", "Class library (net47, x64)", "Documents, OCR engine, layout analysis, exporters, licensing, localization. Copies models, translations and native DLLs to every consuming application."),
        ("FalconOcr.App", "WinForms executable FalconOcr.exe", "Desktop application: shell, workspace, Quick OCR, batch, history, settings, activation."),
        ("FalconOcr.Cli", "Console executable falcon-ocr.exe", "Recognition and export from the command line (automation, batch, tests)."),
        ("FalconOcr.KeyGen", "WinForms executable FalconOcrKeyGen.exe", "Vendor-only: signing key management, license key generation and verification. Not shipped."),
    ], [20, 25, 55], "Projects of the solution FalconOcr.sln")
    d.p("The architecture is strictly layered: the three executables reference only FalconOcr.Core, and the core never references WinForms (System.Drawing is used for bitmaps and GDI+ text measurement). "
        "Inside the core the dependencies run Processing → Engine / Pdf / Layout → Imaging / Model and Export → Model. The result model is the contract between analysis and presentation: viewers and exporters read it, and editing in the application changes it in place.")
    d.h2("2.2 Core namespaces")
    d.table(["Namespace", "Main types", "Purpose"], [
        ("FalconOcr.Input", "OcrDocument, DocumentPage, PageImageCache, NaturalComparer", "Opening PDFs, images, multi-page TIFFs, sequences, clipboard bitmaps; rotation and normalised crop; LRU cache of processed page images."),
        ("FalconOcr.Pdf", "PdfDocument, PdfiumNative", "PDFium P/Invoke (serialised by a global lock): render pages at a DPI, extract the text layer with font name, size, weight, italic flag and fill color."),
        ("FalconOcr.Imaging", "RgbImage, ImageOps", "24-bit BGR buffers, area-averaged bilinear resize, affine crop of rotated quadrilaterals, rotation, Otsu threshold."),
        ("FalconOcr.Engine", "PaddleOcrEngine, TextDetector, TextRecognizer, TextOrientationClassifier, Geometry, OcrOptions, LanguageCatalog", "ONNX Runtime sessions, DBNet post-processing, CTC decoding with character positions, language/model catalogue."),
        ("FalconOcr.Layout", "LayoutAnalyzer, StyleEstimator, TableDetector, FigureDetector", "Reconstruction of the page structure and styles."),
        ("FalconOcr.Model", "OcrPage, TextLine, TextStyle, LayoutLine, LayoutBlock, LayoutSection, TableRegion, TableCell, FigureRegion", "Result model shared by the viewers and the exporters."),
        ("FalconOcr.Processing", "OcrProcessor, PageProgress", "Page pipeline: text layer or OCR, then layout."),
        ("FalconOcr.Export", "Exporter, DocxExporter, XlsxExporter, HtmlExporter, TextMeasure, ExportHelpers, Units", "Writing DOCX, XLSX, HTML and TXT; text fitting for Exact Copy."),
        ("FalconOcr.Licensing", "LicenseManager, LicenseCodec, LicenseInfo, MachineIdentity, TrialManager, LicensePublicKey", "License validation and storage, key encoding, machine code, 7-day trial."),
        ("FalconOcr.Localization", "L, PoFile, UiLanguage", "Run-time translation from gettext PO catalogs."),
    ], [20, 35, 45], "Core namespaces")
    d.h2("2.3 Application structure")
    d.table(["Type", "Responsibility"], [
        ("Program", "Start-up: working directory = executable folder (native DLL resolution), interface language, 64-bit check, license check, trial and activation window, crash handler (error.log)."),
        ("MainForm (IShell)", "Borderless window with custom title bar (drag, maximise to the work area, resize hit-testing), trial badge, sidebar navigation, status bar; owns AppSettings, HistoryStore and OcrService."),
        ("WorkspacePage", "Home screen: toolbar, file list/thumbnails, ImageViewer + LayoutView side by side, settings column, recognition and export workflows, keyboard shortcuts."),
        ("QuickOcrPage, BatchPage, HistoryPage, SettingsPage", "Secondary pages (PageBase)."),
        ("ActivationForm, AdvancedSettingsDialog, Prompt", "Dialogs."),
        ("UI.PageCanvas", "Zoom/scroll surface in page-pixel coordinates (fit width, fit page, custom zoom, pan tool, scroll fraction synchronisation)."),
        ("UI.ImageViewer / UI.LayoutView", "Source image with overlay and crop rubber band / reconstructed page with editing."),
        ("UI.Controls, UI.Icons, UI.Theme", "Custom-drawn controls (ToolButton, NavButton, TabStrip, CardPanel, AccentButton, IconButton, IconRadio), vector icons, colors and fonts."),
        ("Services.OcrService", "One shared OcrProcessor; jobs serialised with a SemaphoreSlim and executed on the thread pool; warm-up of models."),
        ("Services.AppSettings / HistoryStore / Scanner", "JSON persistence (DataContractJsonSerializer), history, WIA scanning via late-bound COM."),
    ], [30, 70], "Application types")
    d.h2("2.4 Third-party components")
    d.table(["Component", "Version / files", "Integration"], [
        ("ONNX Runtime", "Microsoft.ML.OnnxRuntime.Managed 1.22.1; onnxruntime.dll, onnxruntime_providers_shared.dll", "Managed API from the vendored feed; the native DLLs come from lib\\native\\x64."),
        ("PDFium", "pdfium.dll (+ pdfium.LICENSE.txt)", "Own Cdecl P/Invoke declarations in PdfiumNative; no wrapper package."),
        ("Open XML SDK", "DocumentFormat.OpenXml 2.20.0", "Strongly typed DOCX/XLSX elements."),
        ("PaddleOCR PP-OCRv5", "det, textline orientation cls, rec models + dictionaries", "ONNX files under models\\det, models\\cls, models\\rec."),
        ("VC++ runtime", "msvcp140(_1).dll, vcruntime140(_1).dll", "App-local, no redistributable installation needed."),
        ("Support packages", "System.Memory 4.5.5, System.Buffers 4.5.1, System.Numerics.Vectors 4.5.0, System.Runtime.CompilerServices.Unsafe 4.5.3, NETFramework reference assemblies 1.0.3", "Span-based tensors of ONNX Runtime on net47; build without a targeting pack."),
    ], [18, 44, 38], "Third-party components")
    d.h2("2.5 Runtime view")
    d.p("Figure 2 shows one recognition job started from the workspace. The UI thread never blocks: it awaits OcrService.RecognizeAsync, which acquires the job gate and runs the page loop on a thread-pool worker. "
        "Each page result is handed back through BeginInvoke; progress travels through Progress<PageProgress>, which posts to the UI synchronisation context captured at its creation.")
    d.img("sdd-sequence-recognition.png", "Sequence of a recognition job", 620)
    d.ul(["**Coordinates are the source of truth:** every element carries its rectangle in processed-image pixels plus the page DPI; physical units are derived only in exporters and viewers.",
          "**Stateless algorithms, stateful sessions:** layout classes and exporters are static and keep no state between pages; only PaddleOcrEngine holds long-lived resources (ONNX sessions).",
          "**One caller at a time for native code:** every PDFium call runs under PdfiumNative.Sync, ONNX sessions under PaddleOcrEngine._sync."])

    # ================================================================== 3
    d.h1("3. Processing design")
    d.img("diagram-pipeline.png", "Page processing pipeline", 620)
    d.p("OcrProcessor.ProcessPage is the entry point for one page: it obtains the processed bitmap, converts it into an RgbImage, determines the effective DPI and the language, chooses between the PDF text layer and OCR, and calls LayoutAnalyzer.Analyze; the elapsed time is stored in OcrPage.Elapsed. "
        "ProcessDocument iterates over the pages of a document (used by the CLI); the application calls ProcessPage through OcrService.")
    d.h2("3.1 Page loading")
    d.table(["SourceKind", "Created by", "Page source"], [
        ("Pdf", "OcrDocument.OpenPdf", "PdfDocument page index; rendered on demand."),
        ("Image / MultiPageImage", "OcrDocument.Open", "ImagePath and FrameIndex; a TIFF with several frames gives one page per frame."),
        ("ImageSequence", "OcrDocument.OpenImageSequence", "Images ordered with NaturalComparer ('page2' < 'page10'); TIFF frames included."),
        ("Clipboard / Scan", "OcrDocument.FromBitmap / file", "In-memory bitmap copy; scans are saved as PNG and opened as files."),
    ], [22, 30, 48], "Document sources")
    d.ul(["PDF pages are rendered by PDFium at the configured resolution (default 200 dpi) with FPDF_ANNOT | FPDF_PRINTING onto white and copied into 24-bit bitmaps; very large pages are limited to 144 megapixels.",
          "Images are read through a memory copy (the file is not locked), drawn onto white (transparency) and EXIF orientation is applied.",
          "DocumentPage.GetProcessedImage applies the rotation (0/90/180/270°) and the normalised crop rectangle (fractions of the rotated page, therefore valid for any PDF DPI).",
          "PageImageCache keeps the six most recently used processed images (matched by page, Version and, for PDFs, DPI) and always returns a clone that the caller disposes; rotating or cropping increments the page Version and invalidates the cache and the result.",
          "Effective DPI: PDF render DPI; the image DPI if between 150 and 1200; otherwise estimated from an A4/Letter page size (long side / 11.3 in) for large images, else 96."])
    d.p("Because recognition runs in the background, OcrService remembers page.Version before ProcessPage and stores the result only if the version is unchanged, so a page rotated or cropped during recognition keeps no stale result.")
    d.h2("3.2 PDF text layer")
    d.p("If the setting is Auto and a PDF page contains at least 16 non-space characters, PdfDocument.ExtractLines reads every character with FPDFText_GetLooseCharBox (mapped to device pixels with FPDF_PageToDevice), font size, weight, font name (subset prefix and style suffixes removed) and fill color. "
        "Characters are grouped into lines at hard breaks, baseline jumps (> 0.6 × height), backward jumps and horizontal gaps > 1.6 × font size (column/cell gaps). Each line receives the style of its dominant run. With rotation or crop, the lines are re-mapped into the processed image.")
    d.table(["Attribute", "Source", "Rule"], [
        ("Bold", "FPDFText_GetFontWeight, font name", "weight ≥ 600, or name contains Bold, Black, Heavy, Semibold or Demi."),
        ("Italic", "FPDFText_GetFontInfo flags, font name", "FPDF_FONT_ITALIC flag, or name contains Italic or Oblique."),
        ("Font family", "FPDFText_GetFontInfo", "CleanFontName: 'ABCDEF+' subset prefix removed, style suffixes (-Bold, ,Italic, -Regular …) and MT/PS/PSMT removed, CamelCase split ('TimesNewRoman' → 'Times New Roman')."),
        ("Size", "FPDFText_GetFontSize", "In points; if ≤ 0.5 the size is estimated as 0.8 × box height."),
        ("Color", "FPDFText_GetFillColor", "RGB of the fill; black if unavailable."),
        ("Positions", "Loose char boxes", "CharLeft/CharRight per UTF-16 unit, CharConfidence = 1, Confidence = 1; consecutive spaces collapse to one."),
    ], [16, 30, 54], "Text-layer attributes")
    d.p("MapLines (OcrProcessor) converts each line polygon with DocumentPage.MapFromOriginal into processed coordinates, drops lines that are less than 50 % inside the processed image and rebuilds an axis-aligned polygon. For 90°, 180° and 270° rotation the per-character extents no longer run along x and are dropped; for crops they are shifted by the crop offset. "
        "If the text layer yields no line at all, the page falls back to OCR; PdfTextMode.AlwaysOcr forces OCR for broken text layers.")
    d.h2("3.3 Text detection (DBNet)")
    d.img("sdd-detector.png", "Detector pre- and post-processing", 620)
    d.ul(["Input: image scaled so that the longest side ≤ 2560 px (DetMaxSide) and each side is a multiple of 32; small inputs are upscaled to a short side of 64 px so that strokes survive the network's down-sampling.",
          "Normalisation: BGR order with ImageNet mean/std, NCHW tensor; output: probability map.",
          "Post-processing: binary map (p > 0.3) → 8-connected components (≥ 4 px) → pixel-corner convex hull per component (monotone chain) → minimum-area rectangle (rotating calipers).",
          "Box score = mean probability inside the rectangle; boxes below 0.6 are rejected. Unclip: both sides grow by 2D with D = A × 1.5 / L (area, perimeter). Boxes are mapped back to the original size, sorted by top and left; at most 3000 per page."])
    d.h2("3.4 Recognition (SVTR + CTC)")
    d.ul(["Each box is cut out with an affine warp (quadrilateral → upright rectangle, bilinear sampling); tall crops (h ≥ 1.5 w) are rotated for vertical text.",
          "Optional orientation classifier (PP-LCNet, 80 × 160 input) flips lines with 180° probability > 0.9.",
          "Crops are resized to height 48, sorted by aspect ratio and batched (8 per batch) with zero padding to the widest ratio (minimum width 320, maximum 4800); pixels are normalised to [−1, 1].",
          "Greedy CTC decoding: arg-max per time step, blanks and repeats removed; class count = dictionary size + 2 (blank, space). The dictionary file is preferred; the model's 'character' metadata is a fallback.",
          "Per-character confidence and centre position are kept; positions are mapped along the line to page x-coordinates (used for table cell splitting and low-confidence highlighting).",
          "Lines with mean confidence < 0.5 (MinConfidence) or empty text are dropped."])
    d.p("A character's centre is the middle of its run of time steps relative to the unpadded crop width; its confidence is the maximum probability over the run. The left edge of character i is the midpoint between centres i−1 and i, the right edge the midpoint between i and i+1; the first and last characters are extended symmetrically. "
        "The extents are projected onto the page between the midpoints of the left and right quad edges and stored in TextLine.CharLeft / CharRight. If a dictionary entry maps to several UTF-16 units the decoder emits one position per unit; if positions and characters still differ in number, uniform positions are substituted. "
        "Leading and trailing whitespace is trimmed while the per-character arrays stay aligned, and line ids are assigned 1…n. When the orientation classifier flips a crop, the polygon is re-ordered (BR, BL, TL, TR) so that it still starts at the visual top-left.")
    d.h2("3.5 Languages, models and sessions")
    d.table(["Language", "Recognition model / dictionary", "Default font", "Culture"], [
        ("English", "en_PP-OCRv5_rec_mobile.onnx / ppocrv5_en_dict.txt", "Calibri", "en-US"),
        ("Chinese, Japanese", "ch_PP-OCRv5_rec_mobile.onnx / ppocrv5_dict.txt (one shared model)", "Microsoft YaHei / Yu Gothic", "zh-CN / ja-JP"),
        ("Russian", "eslav_PP-OCRv5_rec_mobile.onnx / ppocrv5_eslav_dict.txt", "Calibri", "ru-RU"),
        ("Korean", "korean_PP-OCRv5_rec_mobile.onnx (files present, entry disabled in 1.0)", "Malgun Gothic", "ko-KR"),
    ], [18, 50, 18, 14], "LanguageCatalog")
    d.p("Detection (det\\ch_PP-OCRv5_det_mobile.onnx) and orientation (cls\\ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx) are shared by all languages. The Korean entry is commented out in LanguageCatalog.All, the CLI parser and DocxExporter.IsCjk. "
        "PaddleOcrEngine creates sessions lazily — the detector, one recognizer per model file (Chinese and Japanese share one) and the classifier only when enabled — with one SessionOptions object (ORT_ENABLE_ALL, sequential execution, InterOpNumThreads 1, IntraOpNumThreads = OcrOptions.Threads). "
        "A change of the thread setting disposes and rebuilds all sessions. FindMissingModels lists absent files: the application warns at start-up and refuses to recognize, the CLI exits with code 2.")

    # ================================================================== 4
    d.h1("4. Layout analysis design")
    d.p("LayoutAnalyzer.Analyze(img, lines, dpi, options, fromTextLayer, defaultFont) turns positioned text lines into a structured OcrPage. The algorithm is purely geometric and photometric — no additional neural network — and takes about 0.4 s for a dense A4 page. Figure 5 shows the order of the steps.")
    d.img("sdd-layout-steps.png", "Layout analysis steps", 600)
    d.h2("4.1 Background and style estimation")
    d.p("The page background is the mode of 4-bit-quantised colours on a sparse grid; near-white becomes pure white. For OCR lines StyleEstimator measures each line box (text-layer pages skip this step because the PDF supplies exact values):")
    d.ul(["**Polarity:** Otsu threshold inside the box; the class close to the page background (within 40 grey levels) is background, otherwise the minority class is ink — this supports white text on coloured panels.",
          "**Font size:** ink extent = rows with ≥ 1/6 of peak ink, extended over contiguous weak rows (ascenders/descenders); size = (ink height − 1 px) ÷ zone factor (0.52 x-height only, 0.72 ascenders or descenders, 0.92 both, 0.95 brackets, 0.88 CJK), rounded to 0.5 pt. Commas do not count as descenders ('1,250').",
          "**Weight:** stroke width = 2 × ink area ÷ ink perimeter; bold when stroke/size exceeds 1.25 × the page median.",
          "**Colours:** text colour = mean of the strongest ink pixels, snapped to black, white or a grey step of 16; the box background becomes BackColor when it differs from the page background by more than 45.",
          "**Normalisation:** sizes within 9 % of a frequent size snap to it, so a paragraph does not alternate between 10.5 and 11 pt."])
    d.p("The background is sampled with a step of min(width, height) / 150. The strongest ink pixels are those between the Otsu threshold and the extreme grey value of the box, which avoids the lightening effect of anti-aliased edges. "
        "SnapColor maps near-black (maximum channel < 80, spread < 30) to black, near-white (minimum > 225) to white and unsaturated colours (spread < 18) to a grey rounded to steps of 16, so exports look intentional instead of noisy. "
        "Only lines with at least four characters contribute to the median stroke ratio, because very short texts give unreliable measurements; lines without a font family receive the default font (OcrOptions.EffectiveFont).")
    d.h2("4.2 Ruling lines and ruled tables")
    d.p("The page is binarised with a global Otsu threshold capped at 200. TableDetector finds horizontal and vertical ink runs of at least max(0.25 in, min side / 40) with gaps up to max(2, dpi / 100) px, merges runs on adjacent rows into strokes, discards strokes thicker than max(3, dpi / 25) px (filled areas) and strokes inside text lines (underscores, dashes), and requires background on both sides of a rule.")
    d.ol(["Horizontal and vertical rules that intersect (tolerance max(4, dpi / 30)) are united with union-find; groups with at least two rules of each direction form grids.",
          "Row and column edges are clustered; a border between two grid cells exists when a rule covers > 50 % of the shared edge. Cells without border are merged and the merged groups are made rectangular, because Word and Excel spans must be rectangles. Fewer than two cells means a frame, not a table.",
          "Text lines more than 50 % inside the table are assigned to cells; lines crossing a column border are split at the character positions (SplitAcrossColumns).",
          "Cell fill (most common colour away from text), alignment (from the cell margins) and border colour are detected; sizes snap to the table's dominant size (±30 %) and bold is kept only for rows or columns in which ≥ 60 % of the lines are bold."])
    d.h2("4.3 Figures")
    d.p("FigureDetector marks grid cells of dpi/50 px whose pixels differ from the background, clears text lines, rules and tables, dilates by two cells and labels connected components. The following filters apply:")
    d.table(["Filter", "Rule"], [
        ("Size", "Sides ≥ max(0.2 in, 2 % of the short page side), area ≥ 0.2 % of the page, ≥ 20 cells."),
        ("Scan artefacts", "Edge slivers (aspect > 12) and regions > 85 % of the page (tints) are dropped; overlapping regions are merged."),
        ("Coloured panels", "A flat single-colour region whose text covers > 20 % stays text with shading."),
        ("Text containers", "Regions with more than six lines or > 25 % text area are boxes around text, not pictures."),
    ], [22, 78], "Figure filters")
    d.p("Each figure stores the PNG of its region; lines ≥ 70 % inside a figure are marked InFigure and exported as part of the picture.")
    d.h2("4.4 Sections, columns and reading order")
    d.p("Flow items are the remaining lines plus one item per table and figure. Items are grouped into horizontal bands by vertical overlap; consecutive bands are merged into a section while they share interior gutters of at least max(1.1 × median line height, 1.2 % of the page width). "
        "A section with gutters is split into columns at the gutter centres; blocks are built per column, so the reading order is top-to-bottom within a column, column by column within a section, and section by section down the page.")
    d.p("A gutter pattern becomes a borderless table when the section has at least three bands without tables or figures, ≥ 60 % of the bands have items in two or more columns, the median text length is ≤ 30 characters, and there are at least three columns or the median is ≤ 16.")
    d.h2("4.5 Blocks, lists and alignment")
    d.p("Within a column, segments with a vertical overlap > 0.55 form visual lines. A new paragraph starts at a larger gap (> median gap + max(0.4 h, 4 px)), a style change (size tolerance 16 %), a list marker, a change of the left edge by more than 1.2 × line height (except a first-line indent), an indent after the second line, or after a short line that ends more than max(3 h, 12 % of the column) before the column edge (unless both lines are centred).")
    d.p("List markers are recognised in four forms: a drawn bullet (a solid blob 0.12–0.7 × text height, compact and vertically centred, left of the line), a separate tiny first segment, a bullet character prefix (•, ●, ■, ➢, ✓, – …) and an ordered prefix ('1.', 'a)', '(iv)'). MarkerPrefixLength and TextLeft let exporters strip the marker and indent the text.")
    d.table(["Alignment", "Single line", "Several lines"], [
        ("Center", "left margin > 10 % of the column and margins equal within max(6 %, h)", "centres within 0.8 h, left edges spread > 1.2 h"),
        ("Right", "left margin > 30 %, right margin < max(3 %, 0.5 h)", "right edges within 0.5 h, left edges spread > 1.5 h"),
        ("Justify", "—", "≥ 3 lines, right edges within 0.6 h and at the column edge"),
    ], [14, 43, 43], "Alignment detection")
    d.p("Each block also receives its dominant style, line spacing (median distance of line tops), first-line or hanging indent and the space before it.")
    d.h2("4.6 Headings and layout modes")
    d.p("The body size is the size carrying the most characters. Blocks of ≤ 3 lines and ≤ 160 characters with size ≥ 1.18 × body size, or bold single lines of ≤ 12 words without final period, become headings; levels follow descending size (max. 3). Numbered headings ('1. Introduction') win over ordered list items. "
        "If candidates exceed 60 % of more than four blocks, no headings are assigned (there is no body text to contrast with).")
    d.table(["LayoutMode", "Behaviour"], [
        ("Automatic", "All steps (tables and figures as enabled by DetectTables / DetectFigures)."),
        ("SingleColumn", "No tables, figures or gutters; paragraphs, lists and headings for one column."),
        ("LinesOnly", "One paragraph per text row; no tables, figures, lists or headings."),
    ], [18, 82], "Layout modes")

    # ================================================================== 5
    d.h1("5. Threading, cancellation and performance")
    d.table(["Thread", "Work", "Synchronisation"], [
        ("UI thread", "Controls, editing, painting of the page canvases, dialogs.", "Results via BeginInvoke; Progress<T> posts to the UI context."),
        ("Recognition worker", "OcrService loop: page loading, text layer, ONNX inference, layout.", "SemaphoreSlim gate (1, 1) — one job at a time; engine lock around sessions."),
        ("ONNX Runtime pool", "Intra-op parallelism inside session.Run.", "One thread per physical core by default."),
        ("Background loaders", "Page display, thumbnails, export.", "Load tokens discard outdated page loads; cache and PDFium locks."),
        ("Warm-up", "Model loading after the main window is shown.", "Uses the same gate, so an early recognition waits instead of loading twice."),
    ], [20, 42, 38], "Threads")
    d.p("OcrService clones the options at the start of each job, so changing settings never affects a running job; batch processing and Quick OCR share the same service. "
        "The Recognize button becomes Stop while a job runs and cancels its CancellationTokenSource; the token is checked before each page and after detection, classification and recognition. Pages finished before cancellation keep their results.")
    d.ul(["ONNX Runtime uses one thread per physical core (IntraOpNumThreads = 0) — using logical cores was measured 2 × slower.",
          "Measured on the development machine: dense A4 page at 200 dpi 5.0–6.2 s (detection ≈ 1.6 s, recognition ≈ 3.4 s, layout ≈ 0.4 s); PDF text-layer page 0.3 s; model loading ≈ 0.9 s (warm-up at start-up).",
          "Batches of crops with similar aspect ratios keep zero padding small; the current page is recognized first so that a result appears after one page time.",
          "Working memory of an A4 page at 200 dpi: bitmap ≈ 11.6 MB, detector tensor ≈ 46.6 MB, probability map and labels ≈ 31 MB — released after each page."])

    # ================================================================== 6
    d.h1("6. Data design")
    d.img("sdd-class-model.png", "Result model class diagram", 620)
    d.h2("6.1 Result model")
    d.table(["Class", "Key members", "Notes"], [
        ("OcrPage", "Width, Height, Dpi, Background, Lines, Blocks, Sections, FromTextLayer, Elapsed", "All coordinates in pixels of the processed page image."),
        ("TextLine", "Polygon, Bounds, InkBounds, Text, Confidence, CharLeft/CharRight/CharConfidence, Style, InFigure, Edited", "One detector box (or PDF line)."),
        ("TextStyle", "FontSizePt, Bold, Italic, Underline, Color, BackColor, FontFamily", ""),
        ("LayoutSection", "Bounds, Columns, Blocks, SpaceBefore", "Horizontal band with a fixed number of columns."),
        ("LayoutBlock", "Kind (Paragraph, Heading, ListItem, Table, Figure), Lines, Align, HeadingLevel, ListMarker, OrderedList, MarkerPrefixLength, TextLeft, MarkerGlyph, FirstLineIndent, SpaceBefore, LineSpacing, Style, Table, Figure, Column", "Blocks are in reading order."),
        ("LayoutLine", "Segments, Bounds, Text", "Segments on one baseline; wide gaps become tabs."),
        ("TableRegion / TableCell", "RowEdges, ColumnEdges, Cells (Row, Col, RowSpan, ColSpan, Lines, Fill, Align), HasBorders, BorderColor", ""),
        ("FigureRegion", "Bounds, Png", "Cropped picture."),
    ], [18, 52, 30], "Result model")
    d.p("Invariants: OcrPage.Blocks is the concatenation of the section blocks; every line is in exactly one block, table cell or figure; the per-character arrays are null or as long as Text. "
        "Editing a line sets Edited, clears the character arrays and sets Confidence to 1.")
    d.table(["Member", "Semantics"], [
        ("OcrPage.GetPlainText()", "Block texts in reading order separated by blank lines; tables as tab-separated rows; figures contribute nothing."),
        ("OcrPage.MeanConfidence", "Mean line confidence (shown by Quick OCR)."),
        ("LayoutLine.Text", "Segments joined by a space, or by a tab when the gap exceeds 2.5 × line height."),
        ("LayoutBlock.GetFlowText()", "Lines joined for flowing output: hyphenated words re-joined (letter + '-' + lower-case continuation); no space between CJK characters (Hangul excepted); list marker stripped."),
        ("LayoutBlock.AllTextLines()", "Table cell lines for tables, otherwise all segments."),
        ("TableCell.GroupRows()", "Cell lines grouped into visual rows (centre distance < half the smaller height), each sorted left to right."),
        ("TableRegion.CellAt(row, col)", "The cell covering a grid position, taking row and column spans into account."),
        ("TextStyle.SameAs(o, tol)", "Relative size difference ≤ tol, same bold and italic, colour distance ≤ 60."),
    ], [30, 70], "Derived members")
    d.h2("6.2 Input and options model")
    d.table(["Class", "Key members"], [
        ("OcrDocument", "Name, SourcePath, Kind, Pages, IsRecognized; Open, OpenPdf, OpenImageSequence, FromBitmap, RemovePage, Dispose (owns the PdfDocument)."),
        ("DocumentPage", "Label, ImagePath, FrameIndex, PdfPageIndex, Rotation, Crop, Result, Version; GetProcessedImage, ApplyCrop, EffectiveDpi, MapFromOriginal."),
        ("OcrOptions [DataContract]", "Language, Layout, DetectTables, DetectFigures, PdfText, PdfDpi, DetMaxSide, DetThreshold, BoxThreshold, UnclipRatio, UseAngleClassifier, MinConfidence, Threads, RecBatchSize, DefaultFont; EffectiveFont, Clone."),
        ("ExportOptions", "Mode, Language, IncludeImages, DefaultFont, KeepColors, Pages — built per export by AppSettings.ExportOptions()."),
        ("AppSettings [DataContract]", "Ocr, Format, Mode, OutputFolder, OpenAfterExport, KeepColors, IncludeImages, AutoRecognize, ShowConfidence, Maximized, Bounds, RecentFiles (15), UiLanguage, SidebarCollapsed, FilesPanelCollapsed, RightPanelWidth."),
        ("HistoryStore / HistoryEntry", "Entries (max. 500, newest first), Changed event / Time, Source, Output, Format, Pages, Language, Seconds."),
    ], [24, 76], "Input and options model")
    d.h2("6.3 Persistent data")
    d.table(["Data", "Location", "Format"], [
        ("Settings", "%LOCALAPPDATA%\\FalconOCR\\settings.json", "JSON (AppSettings, OcrOptions); defaults applied for missing fields."),
        ("History", "%LOCALAPPDATA%\\FalconOCR\\history.json", "JSON, newest first, max. 500 entries."),
        ("License key", "%LOCALAPPDATA%\\FalconOCR\\license.key (also application folder, %ProgramData%\\FalconOCR)", "Key text."),
        ("License clock guard", "%LOCALAPPDATA%\\FalconOCR\\license.state", "Ticks of the last use (time-limited keys)."),
        ("Trial state", "HKCU\\Software\\FalconOCR\\EvaluationState and %LOCALAPPDATA%\\FalconOCR\\evaluation.dat (hidden)", "Base64(\"1|startTicks|lastSeenTicks\") + '.' + HMAC-SHA256."),
        ("Scans", "%LOCALAPPDATA%\\FalconOCR\\scans\\", "PNG files acquired from WIA."),
        ("Crash log", "%LOCALAPPDATA%\\FalconOCR\\error.log", "Text."),
        ("Signing key (vendor)", "%APPDATA%\\FalconOcrKeyGen\\signing.key", "ECC private blob, DPAPI (CurrentUser) with application entropy."),
        ("Issued keys (vendor)", "%APPDATA%\\FalconOcrKeyGen\\issued.csv, serial.txt", "CSV log, last serial number."),
    ], [20, 45, 35], "Persistent data")
    d.ul(["Json.Load returns defaults for a missing or unreadable file; Json.Save writes path.tmp and moves it into place, so a crash while saving keeps the previous file.",
          "DataContractJsonSerializer skips constructors; [OnDeserializing] methods in AppSettings and OcrOptions assign the defaults first, so fields missing in older files stay sane.",
          "RightPanelWidth is stored in 96-dpi pixels; history values are language-neutral and translated only for display."])
    d.h2("6.4 Configuration defaults")
    d.table(["Setting", "Default", "Setting", "Default"], [
        ("Language", "English", "PDF input", "Use text layer (Auto)"),
        ("Layout analysis", "Automatic", "PDF resolution", "200 dpi"),
        ("Detect tables", "On", "Detector max. side", "2560 px"),
        ("Keep pictures", "On", "Text / box threshold", "0.30 / 0.60"),
        ("Orientation classifier", "Off", "Unclip ratio", "1.5"),
        ("Minimum confidence", "0.50", "CPU threads", "0 (automatic)"),
        ("Output format", "Word (.docx)", "Document type", "Editable Document"),
        ("Output folder", "Documents\\Falcon OCR Output", "Open after export", "On"),
        ("Keep colors / pictures", "On / On", "Highlight uncertain characters", "On"),
    ], [25, 25, 25, 25], "Defaults")
    d.p("AdvancedSettingsDialog limits the values to safe ranges: PDF resolution 100–600 dpi, detector side 960–6000 px, thresholds 0.05–0.9 (text) and 0.1–0.95 (box), unclip ratio 1.0–3.0, minimum confidence 0–0.95, threads 0–64.")
    d.h2("6.5 Coordinate systems and units")
    d.table(["Space", "Unit", "Conversion"], [
        ("PDF page", "Points (1/72 in), origin bottom-left", "FPDF_PageToDevice → device pixels at the render DPI."),
        ("Original image", "Pixels of the unrotated rendering", "DocumentPage.MapFromOriginal → processed pixels (rotation, then crop offset)."),
        ("Processed image / result model", "Pixels, origin top-left", "OcrPage.Dpi converts to physical sizes (OcrPage.PxToPt)."),
        ("Word", "Twips (px × 1440 / dpi), EMU (px × 914 400 / dpi), half-points", "Units.Twips, Units.Emu, Units.HalfPoints."),
        ("Excel", "Column width in characters ≈ px × 96 / dpi / 7; row height in points", "XlsxExporter.BuildSheet."),
        ("HTML", "CSS px (px × 96 / dpi); font sizes in pt", "HtmlExporter (k = 96 / dpi)."),
        ("Screen", "Device pixels = page px × Zoom × DeviceDpi / 96 × 96 / PageDpi", "PageCanvas.ViewScale; zoom 1.0 = physical size."),
    ], [24, 38, 38], "Coordinate systems")

    # ================================================================== 7
    d.h1("7. Export design")
    d.img("sdd-export-flow.png", "Export dispatch", 620)
    d.p("Exporter.Export collects the results of the requested pages (ExportOptions.Pages or all recognized pages), creates the target folder and delegates to the writer of the format; the document name without extension becomes the title.")
    d.h2("7.1 Mapping of the result model")
    d.table(["Element", "Word (Editable)", "Word (Exact Copy)", "Excel", "HTML (Editable / Exact)"], [
        ("Page", "Section per page: size from pixels/DPI, margins from content box", "Section per page, zero margins", "Worksheet per page", "<article> page of original size"),
        ("Columns", "Continuous section with w:cols (individual widths/spaces) and column breaks", "—", "Column grid from table/column edges", "CSS grid / absolute"),
        ("Paragraph", "Indent, first-line/hanging, spacing before, 'at least' line spacing, alignment, shading", "Framed paragraph at line position, width-fitted (character scale)", "Cell merged across its columns, wrapped", "<p> with margins / absolute <div> with scaleX"),
        ("Heading", "Heading 1–3 style + measured size/color", "as line", "bold cell", "<h1>–<h3>"),
        ("List", "Bullet numbering definition; ordered items keep literal markers with a tab", "bullet glyph drawn", "marker text", "<ul>/<ol>"),
        ("Table", "w:tbl fixed layout, gridSpan / vMerge, borders, fills, row heights", "floating table (tblpPr) at page position", "cells with merges, thin borders, fills, numbers as numbers", "<table> with colspan/rowspan"),
        ("Picture", "Inline picture", "Anchored picture behind text", "—", "Embedded data-URI PNG"),
        ("Run style", "Font (Latin + East Asian), size, bold, italic, underline, color", "same", "Font, size, bold, italic, color", "CSS"),
    ], [12, 26, 22, 20, 20], "Mapping of the result model to the export formats")
    d.ul(["Every export is written to name.tmp and moved into place, so failures never leave a truncated file (FR-28); Exporter.UniquePath adds ' (2)', ' (3)' … to avoid overwriting.",
          "Exact Copy measures text with GDI+ to compute the horizontal scale that makes each line exactly as wide as the source line.",
          "Excel: '1,250', '12.5' and '45%' become numeric cells with number formats; text cells use shared strings; a style registry de-duplicates fonts, fills, borders and cell formats."])
    d.h2("7.2 Word writer")
    d.p("DocxExporter.Write creates a WordprocessingDocument with a main part, a style part and a numbering part, writes each page in the selected mode and sets the package properties Title, Creator ('Falcon OCR') and Created.")
    d.table(["Part", "Content"], [
        ("Document defaults", "Run fonts: ASCII/HighAnsi/ComplexScript = default font (Calibri if none), EastAsia = default font or the language font; 11 pt (half-points 22); language = culture of the recognition language, EastAsia = the culture for Chinese/Japanese, else zh-CN; paragraphs with 0 spacing after and single line spacing."),
        ("Styles", "Normal (default), heading 1–3 (Heading1…3: bold, 15/12/9 pt, keep with next, keep lines, outline level 0–2), List Paragraph, Table Grid."),
        ("Numbering", "One single-level abstract numbering (id 1) with bullet '•', left indent 720 twips, hanging 360 twips; instance 1."),
        ("Runs", "Font (Latin and East Asian), size in half-points, bold, italic, colour when not black and KeepColors is on, underline, character scale in Exact Copy; a segment keeps its own size only when it differs from the block size by more than 20 %."),
    ], [22, 78], "DOCX styles, numbering and runs")
    d.ul(["**Editable:** page margins come from the content box (360–2880 twips; bottom ≤ 720 twips to absorb font-metric drift). Consecutive single-column sections share one Word section; each multi-column section becomes a continuous section with individual column widths and column breaks. "
          "Each block becomes one paragraph with style, numbering, shading, spacing before, 'at least' line spacing, indents and justification; label … value lines keep their positions through tab stops.",
          "**Exact Copy:** zero margins; figures anchored behind the text; tables floating at their page position; each line (including drawn bullets) a framed paragraph at its ink position with exact line height 1.22 × font size and a character scale from TextMeasure.FitScale (40–250 %).",
          "**Plain Text:** one paragraph per line of flowing text; tables as tab-separated rows."])
    d.p("In Editable mode each Word column starts at the left margin plus the offset of its column rectangle from the first column, and the first block of a column gets the distance from the section top as spacing before (capped at 2880 twips). "
        "Ordered list items write their literal marker followed by a tab, so numbering such as 'a)' or '(iv)' is preserved exactly, while unordered items use the bullet numbering. Hyphenated line ends are re-joined as in GetFlowText, and no space is inserted between CJK characters. "
        "Section properties are attached to the last paragraph of each section, or to an empty paragraph with an exact 1-pt line height, so that section breaks add no visible space; landscape pages get PageOrientation Landscape.")
    d.p("Tables use fixed layout, grid columns from the column edges, 'at least' row heights, GridSpan and VerticalMerge for spans, single borders in the detected colour (none for borderless tables), cell shading and one paragraph per visual row of a cell.")
    d.h2("7.3 Excel and HTML writers")
    d.p("XlsxExporter builds one worksheet per page ('Page n', gridlines hidden). The column grid is formed from the content edges, all table column edges and the column edges of multi-column sections. Table rows map to sheet rows with merges for spans; text blocks are cells merged across the grid columns they cover, wrapped when they have several lines. "
        "Entries that overlap vertically in different columns share one row, so side-by-side content stays side by side. Figures are not exported and the document type does not change the Excel layout.")
    d.ul(["Numbers: cells matching ^[-+]?(\\d{1,3}(,\\d{3})+|\\d+)(\\.\\d+)?%?$ are written as numeric values with built-in number formats 1/2 (integer/decimal), 3/4 (thousands separator) or 9/10 (percent, value divided by 100) and right alignment; values with more than two decimals and no separator use the General format.",
          "StyleRegistry de-duplicates fonts (name, size, bold, italic, colour), fills (solid), borders (thin, colour) and cell formats (font, fill, border, alignment, wrap, number format) by string keys; index 0 is the default font of the language.",
          "Merged regions with borders or fills get styled placeholder cells in the covered columns so that Excel draws them completely; cells of a row are sorted by column index and duplicates removed.",
          "Column widths are at least 2 characters, table rows at least 15 pt high; page margins are 0.5 in."])
    d.p("HtmlExporter writes one self-contained UTF-8 file (without BOM): a <!DOCTYPE html> document with the language attribute from the culture, a viewport meta tag, an embedded style sheet and one <article class=\"page\"> per page with the page size in CSS pixels (k = 96 / dpi). Pictures are embedded as data:image/png;base64 URIs, so the file can be mailed or archived alone; a print style sheet removes shadows and inserts page breaks.")
    d.table(["Mode", "Structure"], [
        ("Editable (FlowPage)", "Padding from the content box; one <section> per layout section with margin-top from SpaceBefore; multi-column sections as CSS grid (grid-template-columns from the column widths, column-gap from the gutter); paragraphs <p>, headings <h1>–<h3>, consecutive list items grouped into <ul> or <ol class=\"lit\"> (literal markers in a hanging <span class=\"m\">); inline spans only for segments whose weight, slant or colour differ from the block."),
        ("Exact Copy (ExactPage)", "Fixed-size page with position:relative; figures and tables absolutely positioned; each positioned line an absolutely positioned <div class=\"l\"> with white-space:pre, line-height 1.2, vertical centring on the ink box and transform:scaleX(s) when the fitted scale differs from 1 by more than 2 %."),
        ("Plain Text (PlainPage)", "<article class=\"page plain\"> with the HTML-encoded plain text in a pre-wrapped monospace block."),
    ], [22, 78], "HTML modes")
    d.p("The text format joins OcrPage.GetPlainText of all pages with a form feed and a line break and is written as UTF-8 with BOM, so Notepad and Excel detect the encoding reliably.")
    d.h2("7.4 Export workflow in the application")
    d.p("WorkspacePage.ExportCurrentAsync first checks the recognition state (FR-48): if no page is recognized it offers to recognize the document; if only some pages are, Yes recognizes the missing pages first and No exports only the recognized ones. "
        "Export writes to Exporter.UniquePath in the configured output folder; 'Export as…' in the file context menu opens a SaveFileDialog with the filter of the format instead. The export itself runs with Task.Run, followed by a history entry, a status message and — if 'Open after export' is set — Process.Start of the file (falling back to Explorer with the file selected).")
    d.h2("7.5 Shared helpers")
    d.table(["Helper", "Design"], [
        ("Units", "Twips, Emu, HalfPoints, Hex and Css conversions."),
        ("ExportHelpers.PositionedLines", "Lines outside tables and figures plus a '•' for every drawn bullet — also used by LayoutView, so the screen and the exports agree."),
        ("TextMeasure", "GDI+ width in points (GenericTypographic, Arial fallback for fonts that are not installed); FitScale clamps to 40–250 %; calls serialised by a lock."),
    ], [28, 72], "Export helpers")

    # ================================================================== 8
    d.h1("8. User interface design")
    d.h2("8.1 Shell and workspace")
    d.p("MainForm implements IShell (Settings, History, Ocr, SetStatus, SetCounters, Navigate, Workspace), which every page receives. Pages are created once and kept in a dictionary under the keys 'home', 'ocr', 'batch', 'history' and 'settings'; pages implementing IActivatable refresh themselves in OnActivated. "
        "The window is borderless: dragging the custom title bar sends WM_NCLBUTTONDOWN/HTCAPTION, WndProc answers WM_NCHITTEST near the edges with the resize codes, and MaximizedBounds is set to the working area so the taskbar stays visible.")
    d.p("WorkspacePage is built entirely in code: the toolbar (Add Files, Scan, From Clipboard, Rotate, Crop, Delete, Recognize, Export), the optional trial banner, the Files / Thumbnails panel, the source page and the results (tabs 'Recognized Text' / 'Original Image'), the GripSplitter and the settings column. "
        "Pages are loaded on a worker thread; a load token discards images that arrive after the user has moved on.")
    d.h2("8.2 Synchronised canvases and results view")
    d.p("PageCanvas is a double-buffered panel that draws one page in page-pixel coordinates; with the same page size, DPI and zoom two canvases map each page pixel to the same screen offset. FitWidth and FitPage recompute the zoom on resize, custom zoom is clamped to 0.1–6, Ctrl + wheel zooms and middle-drag (or the hand tool) pans. "
        "SyncFrom copies zoom and scroll fraction from the canvas the user moved to the others, guarded against recursion.")
    d.p("LayoutView redraws the recognized page at the original positions: background, figures, table fills and borders, block shading and every line in its estimated font, scaled to the ink width and centred on the ink box — the geometry of the Exact Copy exporters. Characters with confidence < 0.75 are highlighted in yellow.")
    d.table(["Interaction", "Implementation"], [
        ("Select", "Hit test on line bounds; LineSelected / LineClicked keep the selection in both views."),
        ("Edit", "Double-click, F2 or Enter opens a TextBox over the line; Enter or focus loss commits, Escape cancels; edited lines are underlined with a dashed line."),
        ("Format", "Style combo (Normal, Heading 1–3), bullet and numbered buttons change the block kind; Bold / Italic / Underline and the font combos change the styles of the paragraph lines."),
        ("Original Image tab", "ImageViewer with an overlay of detected lines, tables and figures; the same class provides the crop rubber band."),
    ], [22, 78], "Results interactions")
    d.h2("8.3 Controls, theme and DPI")
    d.p("Custom controls derive from HoverControl (double buffering, hover state): ToolButton, NavButton, TabStrip, AccentButton, IconButton, IconRadio and CardPanel; all icons are GDI+ vector paths. Theme holds the green palette and the fonts; the UI font follows the interface language because Segoe UI has no CJK glyphs. "
        "The manifest declares system DPI awareness, long paths and asInvoker; sizes are written for 96 dpi and scaled with DeviceDpi / 96.")
    d.h2("8.4 Secondary pages and shortcuts")
    d.table(["Page", "Design"], [
        ("Quick OCR", "Image viewer and text box; open, paste or drop an image; RecognizePageAsync with the current options; nothing is saved."),
        ("Batch", "Input list (folders optionally as image sequences), format, document type, language, output folder; per item recognize → unique path → export → history; failures are logged and processing continues."),
        ("History", "List of recognitions and exports with open, show in folder, reopen source and clear; refreshed on HistoryStore.Changed."),
        ("Settings", "General, Recognition, Export, Advanced recognition and License sections; save, restore defaults, open data folder."),
    ], [16, 84], "Secondary pages")
    d.p("Workspace shortcuts: Ctrl+O add files, F5 recognize / stop, Ctrl+E export, Ctrl+R rotate, Page Up/Down change page, Ctrl + '+' / '−' zoom, Ctrl+V paste and Delete remove (both not while a text box has the focus).")
    d.h2("8.5 Workspace layout extensions")
    d.table(["Feature", "Design"], [
        ("Font combos (FR-37)", "Two editable combo boxes on the results toolbar: installed font families (common document fonts first, then alphabetical) and sizes 8–72 pt (any value 4–200 can be typed). Selection or Enter applies the value to every line of the selected paragraph and to the block style, so DOCX runs, HTML CSS and XLSX fonts use it."),
        ("Collapsible sidebar (FR-39)", "SidebarToggle docked at the bottom of the navigation bar; collapsed width 64 px (icons only, names as tooltips), expanded 184 px; AppSettings.SidebarCollapsed."),
        ("Resizable settings panel (FR-40)", "A WinForms Splitter (GripSplitter, with hover highlight and grip dots) docked between the centre area and the settings column; MinSize 280 px, the centre keeps at least 560 px; AppSettings.RightPanelWidth stores the width in 96-dpi pixels."),
        ("Trial notification (FR-41)", "Amber panel docked below the toolbar only while Program.IsTrial: clock icon, remaining days, 'Activate now' (opens the activation window) and ✕ (hide until the next start). MainForm.ShowActivation removes the banner and the title-bar badge after a successful activation."),
        ("Default font (FR-42)", "OcrOptions.DefaultFont (null = automatic) and OcrOptions.EffectiveFont(language) feed LayoutAnalyzer; ExportOptions.DefaultFont sets the DOCX document defaults, the HTML body font and the XLSX style font. CLI: --font NAME."),
        ("Collapsible Files panel (FR-43)", "PanelToggle docked at the bottom of the Files / Thumbnails panel; collapsed width 40 px (tabs and lists hidden, toggle fills the strip with a vertical caption and »), expanded 214 px; AppSettings.FilesPanelCollapsed."),
        ("Application icon (FR-44)", "src\\FalconOcr.App\\app.ico (16–256 px, generated by tools\\make_icon.py) is the ApplicationIcon of FalconOcr.exe and KeyGen and an embedded resource; Theme.AppIcon / AppIconBitmap supply window icons and the title-bar logo. The Settings model section is commented out in SettingsPage."),
    ], [25, 75], "Workspace layout extensions")
    d.h2("8.6 Diagnostic snapshot mode")
    d.p("For headless verification (FR-51) the environment variable FALCON_SNAPSHOT names an output folder. Program then skips the activation prompt — only while the trial is still valid — and MainForm.RunSnapshotTour, started after the window is shown and the command-line files are added, "
        "recognizes the loaded document, selects a line, switches to the overlay tab, collapses the Files panel and visits the Quick OCR, Batch, History and Settings pages. After each step the window is drawn with DrawToBitmap into a PNG (1-loaded, 2-recognized, 2b-selected, 2c-overlay, 2d-files-collapsed, 3-ocr, 3-batch, 3-history, 3-settings); then the application closes. "
        "The screenshots of the documentation set are produced this way, and the tour doubles as a smoke test of the whole UI.")
    d.h2("8.7 Command-line interface")
    d.p("falcon-ocr.exe performs the same license and trial check as the desktop application, runs OcrProcessor.ProcessDocument synchronously and exports each document to every requested format; output paths go to standard output, progress to standard error.")
    d.table(["Arguments", "Effect"], [
        ("<files / folders> -l en|zh|ja|ru -f docx,xlsx,html,txt -o DIR", "Inputs (a folder adds its supported files), language, one or more formats (default docx), output folder (default: next to the input)."),
        ("--exact, --plain, --seq", "Exact Copy / Plain Text; combine all inputs into one image sequence."),
        ("--no-tables, --ocr-only, --dpi N, --cls, --font NAME", "Options DetectTables, PdfText = AlwaysOcr, PdfDpi, orientation classifier, default font."),
        ("--dump", "Print the section/block structure of every page (with FALCON_DUMP_LINES=1 also every line) — the main tool for tuning layout heuristics."),
        ("--license KEY, --machine-code", "Activate a key / print the machine code."),
    ], [40, 60], "CLI arguments")
    d.p("Exit codes: 0 success, 1 usage shown, 2 model files missing, 3 not activated or trial expired.")

    # ================================================================== 9
    d.h1("9. Licensing design")
    d.img("diagram-licensing.png", "Start-up licensing flow", 560)
    d.p("Program.Main calls LicenseManager.CheckInstalled before any window is created. Without a valid key, TrialManager.Check reads or starts the trial and ActivationForm is shown: it returns OK after activation, Ignore for 'Continue Trial' (only while the trial runs) and Cancel for Exit. "
        "Program.IsTrial controls the title-bar badge and the banner.")
    d.h2("9.1 License keys")
    d.table(["Offset", "Size", "Field"], [
        ("0", "1", "Version (1)"),
        ("1", "4", "Serial number (UInt32)"),
        ("5", "2", "Issue date — days since 2024-01-01"),
        ("7", "2", "Expiry date — days since 2024-01-01 (0 = perpetual)"),
        ("9", "10", "Machine hash (all zero = any computer)"),
        ("19", "1", "Licensee name length n (≤ 48 bytes)"),
        ("20", "n", "Licensee name (UTF-8)"),
        ("20 + n", "64", "ECDSA P-256 signature (SHA-256, IEEE P1363) over bytes 0 … 19 + n"),
    ], [15, 10, 75], "License key payload")
    d.ul(["Text form: 'FOCR-' followed by Crockford Base32 (no I, L, O, U; tolerant decoding) in groups of five. Typical length ≈ 190 characters.",
          "Machine hash = first 10 bytes of SHA-256('FalconOCR|' + Windows MachineGuid); shown to the customer as 16 characters in four groups (machine code).",
          "Validation order: format → signature (public key LicensePublicKey.Blob compiled into the application) → payload → machine → expiry → clock guard.",
          "The KeyGen signs with the private key and verifies every generated key before returning it."])
    d.table(["LicenseStatus", "Condition"], [
        ("Missing / Malformed", "No key; not Base32, too short, unknown version or truncated payload."),
        ("InvalidSignature", "ECDsaCng.VerifyData fails (or the public key cannot be imported)."),
        ("WrongMachine / Expired", "Machine hash differs from this computer / today (UTC) is after the expiry day."),
        ("ClockTampered", "Time-limited key and the clock is more than one day before the last use recorded in license.state."),
        ("Valid", "All checks passed; LicenseInfo.ToString shows licensee, serial, validity and binding."),
    ], [24, 76], "License validation results")
    d.p("Keys are parsed tolerantly (whitespace, hyphens and case ignored, I/L read as 1 and O as 0) and stored normalised in the user profile by Activate. CheckInstalled searches the user profile, the application folder and %ProgramData%\\FalconOCR and returns the first valid key.")
    d.h2("9.2 Trial")
    d.img("sdd-trial-states.png", "Trial state machine", 600)
    d.ul(["TrialManager.Check() runs at start-up when no valid key is installed. The first call stores the start time; each call updates the last-seen time.",
          "Records are stored in the registry and in a hidden file, each protected by HMAC-SHA256 keyed with a constant and the machine hash (records cannot be edited or copied to another computer); the MAC comparison is constant-time.",
          "The earliest valid start time wins, so deleting one copy does not restart the trial. A record with an invalid HMAC, or a current time more than 24 h before the last use, marks the trial as tampered (expired).",
          "Days left = 7 − floor(days used), minimum 1 while running; expired when ≥ 7 days have passed.",
          "UI: activation window at every start (Activate / Continue Trial / Exit), amber title-bar badge 'TRIAL · n days left — Activate now', Settings → License; after expiry only Activate / Exit. The CLI prints the trial status and exits with code 3 after expiry."])
    d.h2("9.3 Key generator")
    d.ul(["The signing key is a CNG ECDSA P-256 key created exportable, stored DPAPI-encrypted for the current Windows user; backups are Base64 text files that must be kept offline.",
          "'New signing key' writes src\\FalconOcr.Core\\Licensing\\LicensePublicKey.cs (Blob and fingerprint); after a rebuild the application accepts keys of the new signer and rejects all older keys.",
          "The KeyGen shows whether its key matches the public key compiled into the current build (fingerprint = first 8 bytes of SHA-256 of the public blob).",
          "Every issued key is appended to issued.csv with licensee, serial, machine code, expiry and note; serial numbers continue from serial.txt (first serial 1001)."])
    d.table(["Command", "Function"], [
        ("(no arguments)", "KeyGenForm: signing key status and fingerprint, licensee, serial, machine code or any computer, perpetual or expiry date, note, Generate, Copy, Save, and a verifier for pasted keys."),
        ("--init [--force]", "Create a signing key and write LicensePublicKey.cs; --force replaces an existing key (invalidates all issued keys)."),
        ("--backup FILE / --import FILE", "Export the private blob as a Base64 text file with a warning header / import such a backup."),
        ("--generate --name N [--machine CODE] [--days D | --expires DATE] [--serial S] [--note T]", "Issue a key, commit the serial, append it to issued.csv and print it."),
        ("--verify KEY", "Validate a key against the signer's public key without machine and clock checks."),
        ("--machine-code", "Print the machine code of the vendor computer."),
    ], [36, 64], "KeyGen commands")
    d.p("SigningKey.Issue encodes the payload, signs it with ECDsaCng (SHA-256), formats the key and validates it again with the signer's public key before returning it; a key that fails this self-check raises a CryptographicException instead of being issued. "
        "The DPAPI entropy 'FalconOcr.KeyGen/signing-key/v1' binds the protected key file to the application, and only the same Windows user can decrypt it.")
    d.note("Signatures prevent forged keys; they do not prevent modification of the executable itself. Use an obfuscator if protection against patching is required.")

    # ================================================================== 10
    d.h1("10. User interface localization")
    d.p("The interface is translated with gettext PO catalogs (FR-38). Source strings in the code are English; the translation happens at run time.")
    d.table(["Element", "Design"], [
        ("FalconOcr.Localization.L", "L.T(\"English text\") returns the translation of the active catalog or the English text; L.F(\"… {0} …\", args) translates a composite format string and formats it. Languages: en, zh_CN (简体中文), ja (日本語)."),
        ("PoFile", "Minimal PO reader in Core: msgid/msgstr, multi-line strings, C escapes (\\n, \\t, \\\", \\\\); comments, flags and msgctxt are ignored."),
        ("Catalogs", "lang\\zh_CN.po and lang\\ja.po (UTF-8) are copied next to the executable by FalconOcr.Core.csproj; lang\\falcon-ocr.pot is the template. They can be edited with Poedit or a text editor without rebuilding."),
        ("Language selection", "Settings → General → Interface language (AppSettings.UiLanguage). Program.Main loads the catalog before any window is created; without a setting the Windows display language is used. Changing the language offers an immediate restart (texts are created with the windows)."),
        ("Fonts", "Theme picks the UI font per language: Segoe UI (English), Microsoft YaHei UI (Chinese), Yu Gothic UI (Japanese) — Segoe UI has no CJK glyphs."),
        ("Core messages", "License and trial messages (LicenseCheck.Message, LicenseInfo.ToString, TrialStatus.Message) use L.T/L.F, so they appear translated in the activation window and in Settings."),
        ("Data vs. text", "Persisted values stay language-neutral (e.g. history format 'Recognition' is translated only for display); zoom modes are compared with the translated item text."),
        ("Tooling", "tools\\i18n\\po_tool.py extract scans L.T/L.F calls (joining concatenated literals) and updates the .pot and .po files keeping existing translations; po_tool.py check reports untranslated entries and placeholder mismatches ({0}, {1:P0} …). tools\\i18n\\translations.py holds the initial translations (286 messages, 100 % translated)."),
    ], [22, 78], "Localization design")
    d.p("Unknown codes or missing catalogs fall back to English. CLI output, logs and exported documents are not translated, so they remain comparable between installations.")

    # ================================================================== 11
    d.h1("11. Error handling, logging and security")
    d.h2("11.1 Error handling and logging")
    d.table(["Situation", "Handling"], [
        ("Missing model files", "Warning at start-up listing the files; recognition is refused with a message."),
        ("Unsupported / unreadable file", "Collected and shown in one message after adding files; other files are still added."),
        ("Password-protected PDF", "'The PDF is password protected.'"),
        ("Recognition error", "Message box; the job stops; already recognized pages keep their results."),
        ("Export error", "Temporary file removed, message box, status 'Export failed.'"),
        ("Batch item failure", "Item marked 'Failed' and logged; processing continues."),
        ("32-bit process", "'Falcon OCR must run as a 64-bit process.' — the native DLLs are x64 only."),
        ("Unhandled exception", "Logged to error.log with stack trace and shown once (re-entrancy guarded)."),
        ("Corrupt settings/history", "Defaults are used (the files are optional)."),
        ("No scanner", "'No scanner was found…' from WIA error 0x80210015."),
    ], [30, 70], "Error handling")
    d.p("Program installs Application.ThreadException and AppDomain.UnhandledException handlers; ReportCrash appends the time and exception text to %LOCALAPPDATA%\\FalconOCR\\error.log and shows the log path, with an Interlocked flag preventing cascades of message boxes. Expected failures are shown locally and not logged; nothing is sent anywhere.")
    d.h2("11.2 Security considerations")
    d.table(["Threat", "Measure", "Residual risk"], [
        ("Disclosure of documents", "All processing local, no network code; Quick OCR saves nothing.", "Exported files are not encrypted."),
        ("Forged or shared keys", "ECDSA signatures, public key only in the application; optional machine binding.", "Reinstalling Windows changes the MachineGuid."),
        ("Theft of the signing key", "DPAPI with entropy; KeyGen never in dist.", "The vendor's Windows session can use the key."),
        ("Trial reset, clock changes", "Two HMAC copies, earliest start wins, rollback detection.", "Deleting both copies restarts the trial."),
        ("Malicious input files", "144-MP page limit; files read into memory.", "Vulnerabilities of PDFium / GDI+ decoders."),
        ("Privileges", "asInvoker; writes only to the user profile and HKCU.", "—"),
    ], [22, 48, 30], "Security measures")

    # ================================================================== 12
    d.h1("12. Build and deployment")
    d.img("sdd-deployment.png", "Build and deployment view", 620)
    d.h2("12.1 Repository layout")
    d.code(["src/FalconOcr.Core      engine, layout, exporters, licensing",
            "src/FalconOcr.App       desktop application",
            "src/FalconOcr.Cli       command line",
            "src/FalconOcr.KeyGen    license key generator (vendor only)",
            "models/                 PP-OCRv5 ONNX models + dictionaries (49.5 MB)",
            "lib/native/x64/         onnxruntime.dll, pdfium.dll, VC++ runtime (19.6 MB)",
            "packages/               vendored NuGet feed (33.2 MB)",
            "tools/                  fetch-dependencies.ps1, sample and document generators",
            "lang/                   interface translations (zh_CN.po, ja.po, falcon-ocr.pot)",
            "docs/                   project documents", "samples/                test documents"])
    d.h2("12.2 Offline build")
    d.ul(["nuget.config clears all sources and uses packages\\ only; Directory.Build.props sets net47, C# 7.3, x64 and binding redirects.",
          "build.ps1 checks the vendored dependencies, builds the solution in Release/x64 and copies the application and CLI to dist\\ (KeyGen excluded).",
          "tools\\fetch-dependencies.ps1 (online, once) downloads models (ModelScope RapidAI/RapidOCR), ONNX Runtime and PDFium binaries and re-populates the NuGet feed.",
          "Verified: clean copy of the repository, empty NuGet cache and blocked network → restore and build succeed with 0 warnings, 0 errors."])
    d.p("Binding redirects are generated because ONNX Runtime targets netstandard2.0; warnings NU1701 and NU1603 are suppressed for the offline feed. FalconOcr.Core.csproj links the native DLLs, the models and the PO files as content with CopyToOutputDirectory, so every project that references the core — App, CLI, KeyGen — gets a runnable output folder.")
    d.h2("12.3 Deployment")
    d.p("The dist folder (≈ 78 MB, 115 files) is copied to the target computer — no installer, registry entries or administrator rights are needed. It contains FalconOcr.exe, falcon-ocr.exe, FalconOcr.Core.dll, managed dependencies and .NET Standard facades, native DLLs and the models folder. A license.key file may be placed in the folder or in %ProgramData%\\FalconOCR for site deployment.")
    d.p("Program.Main sets the current directory to the executable folder so that onnxruntime.dll and pdfium.dll are found however the application is started. With the app-local VC++ runtime the product runs on any Windows 10/11 x64 with .NET Framework 4.7 or later; removing it means deleting the folder (user data stays in %LOCALAPPDATA%\\FalconOCR).")

    # ================================================================== 13
    d.h1("13. Extension points and design decisions")
    d.h2("13.1 Extension points")
    d.table(["Extension", "How"], [
        ("New recognition language", "Add the PP-OCRv5 rec model and dictionary to models\\rec, add an entry to LanguageCatalog.All (model, dictionary, default font, culture) and to the OcrLanguage enum; the UI lists it automatically."),
        ("Re-enable Korean", "Uncomment the Korean entries in LanguageCatalog.All, the CLI parser and DocxExporter.IsCjk; the model files are present."),
        ("New interface language", "Copy lang\\falcon-ocr.pot to lang\\<code>.po, translate it, and add the code to L.Languages (and a UI font in Theme if the script needs one)."),
        ("New export format", "Implement a writer over List<OcrPage>, add a value to ExportFormat and a case in Exporter.Export."),
        ("GPU inference", "Replace the ONNX Runtime package/DLL with a GPU build and append the execution provider in PaddleOcrEngine.GetSessionOptions."),
        ("Layout tuning", "Thresholds are local constants in the Layout classes; falcon-ocr --dump shows the effect on the sample set."),
    ], [25, 75], "Extension points")
    d.h2("13.2 Design decisions")
    d.table(["Decision", "Alternatives", "Rationale"], [
        ("PP-OCRv5 on ONNX Runtime", "Tesseract, Windows OCR, cloud", "High accuracy for Latin and CJK print, small models, CPU inference, offline, supported .NET API."),
        ("Own imaging code", "OpenCvSharp / Emgu CV", "Few operations needed; saves > 50 MB of native binaries."),
        ("Own PDFium P/Invoke", "Wrapper libraries", "Access to font info, weight, fill colour and char boxes."),
        ("PDF text layer first", "Always OCR", "Exact text and fonts at 0.3 s per page; AlwaysOcr remains available."),
        ("Heuristic layout analysis", "Layout network", "No extra model, deterministic, fast, tunable with --dump."),
        ("Pixels + DPI in the model", "Physical units", "One conversion point per output; Exact Copy stays exact."),
        ("One OCR job at a time", "Parallel pages", "ONNX Runtime already uses all physical cores."),
        ("Exact Copy as scaled text", "Text boxes, images of text", "Text stays searchable and editable while matching line widths."),
        ("ECDSA keys in Base32", "Symmetric serials, RSA", "Application cannot create keys; 64-byte signatures keep keys short."),
        ("PO catalogs", ".resx satellites", "Editable with Poedit without rebuilding."),
        ("xcopy deployment", "MSI installer", "No administrator rights; runs from any folder."),
    ], [26, 24, 50], "Design decisions")

    # ================================================================== 14
    d.h1("14. Requirements traceability")
    from doc_srs import FR
    comp = {
        "Input": "Input.OcrDocument / DocumentPage; App.WorkspacePage; Services.Scanner",
        "Page editing": "DocumentPage.Rotation / ApplyCrop; WorkspacePage; ImageViewer (crop)",
        "Recognition": "Engine.*; Processing.OcrProcessor; Pdf.PdfDocument; Services.OcrService",
        "Layout": "Layout.LayoutAnalyzer, StyleEstimator, TableDetector, FigureDetector",
        "Results": "UI.LayoutView, UI.ImageViewer, UI.PageCanvas; WorkspacePage",
        "Export": "Export.Exporter, DocxExporter, XlsxExporter, HtmlExporter",
        "Tools": "QuickOcrPage, BatchPage, HistoryPage, SettingsPage, AppSettings, FalconOcr.Cli, MainForm.RunSnapshotTour",
        "Licensing": "Licensing.*; ActivationForm; MainForm (badge); FalconOcr.KeyGen",
        "User interface": "Localization.L / PoFile, lang\\*.po, Theme; WorkspacePage (font combos, GripSplitter, trial banner, PanelToggle); MainForm (SidebarToggle); Theme.AppIcon, app.ico; AppSettings",
        "Settings": "SettingsPage (default font); OcrOptions.DefaultFont / EffectiveFont; ExportOptions.DefaultFont; LayoutAnalyzer; FalconOcr.Cli (--font)",
    }
    d.table(["Requirement", "Title", "Design elements"], [(r[0], r[2], comp[r[1]]) for r in FR], [12, 30, 58], "Requirement to design traceability")
    try:
        from doc_srs import NFR
    except ImportError:
        NFR = []
    nfr_design = {
        "NFR-01": "Directory.Build.props (net47, x64, C# 7.3); WinForms UI built in code (chapter 8)",
        "NFR-02": "nuget.config with packages\\ only; models\\ and lib\\native in the repository; no network code (12.2)",
        "NFR-03": "Session caching, aspect-ratio batching, physical-core threading, current page first (3.5, 5)",
        "NFR-04": "DBNet/SVTR pipeline reproduced from PaddleOCR (3.3, 3.4); ruled-table grid reconstruction (4.2)",
        "NFR-05": "Workspace layout, synchronised canvases, shortcuts, DeviceDpi scaling (8)",
        "NFR-06": "Temporary file + move for exports and JSON; error.log crash handler; defaults on corrupt files (6.3, 11.1)",
        "NFR-07": "ECDSA keys, DPAPI signing key, HMAC trial records (9); security measures (11.2)",
        "NFR-08": "Layered projects, UI-independent Core, CLI --dump (2.1, 8.7)",
        "NFR-09": "dist\\ xcopy folder with app-local native DLLs (12.3)",
        "NFR-10": "Third-party components shipped unmodified with their license files (2.4)",
        "NFR-11": "L.T / L.F, PoFile, English fallback, language-dependent UI fonts (10)",
        "NFR-12": "OcrService worker, Progress<T>, BeginInvoke, background warm-up (5)",
        "NFR-13": "PageImageCache (6 entries), images read through memory copies (3.1)",
    }
    if NFR:
        d.table(["Requirement", "Area", "Design elements"], [(n[0], n[1], nfr_design.get(n[0], "See chapters 2–12")) for n in NFR], [12, 18, 70], "Non-functional requirement traceability")
    return d
