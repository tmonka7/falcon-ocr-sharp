from docbuilder import Doc


def build():
    d = Doc("System Design Document", "Architecture, components, algorithms, data and licensing design", "FOCR-SDD-001",
            "This document describes how Falcon OCR 1.0 is built: architecture, components, processing algorithms, data model, "
            "export mapping, licensing, storage, build and deployment. It is written for developers who maintain or extend the product.")

    d.h1("1. Introduction")
    d.h2("1.1 Purpose and audience")
    d.p("The System Design Document (SDD) explains the internal structure of Falcon OCR and the reasons behind the main design decisions. "
        "It is intended for developers, reviewers and testers. Requirement identifiers (FR-nn / NFR-nn) refer to the Software Requirements Specification FOCR-SRS-001.")
    d.h2("1.2 Design goals")
    d.table(["Goal", "Design decision"], [
        ("Fully offline (NFR-02)", "PaddleOCR models as ONNX files in the repository; ONNX Runtime and PDFium as bundled native DLLs; NuGet packages vendored in packages\\ with a local-only nuget.config."),
        ("Layout fidelity (FR-15…FR-20)", "Every recognized element keeps its page coordinates; a layout analyser rebuilds sections, columns, blocks, tables and figures; exporters map this model to Word, Excel and HTML, both as flow and as exact positions."),
        ("No heavy imaging dependency", "Image processing (resize, rotated crop, binarisation, connected components, convex hull, minimum-area rectangle) is implemented in C#; no OpenCV."),
        (".NET Framework 4.7 / WinForms (NFR-01)", "SDK-style projects targeting net47, C# 7.3; UI built in code with custom-drawn controls."),
        ("Separation of concerns (NFR-08)", "UI-independent FalconOcr.Core used by the desktop app, the CLI and the KeyGen."),
        ("Tamper-resistant licensing (FR-34/35)", "Asymmetric signatures (ECDSA P-256) — the app can verify but not create keys; HMAC-protected trial state stored twice."),
    ], [30, 70], "Design goals and decisions")

    d.h1("2. Architecture")
    d.h2("2.1 Component overview")
    d.img("diagram-architecture.png", "Component architecture", 610)
    d.table(["Project", "Type", "Responsibility"], [
        ("FalconOcr.Core", "Class library (net47, x64)", "Documents, OCR engine, layout analysis, exporters, licensing. Copies models and native DLLs to every consuming application."),
        ("FalconOcr.App", "WinForms executable FalconOcr.exe", "Desktop application: shell, workspace, Quick OCR, batch, history, settings, activation."),
        ("FalconOcr.Cli", "Console executable falcon-ocr.exe", "Recognition and export from the command line (automation, batch, tests)."),
        ("FalconOcr.KeyGen", "WinForms executable FalconOcrKeyGen.exe", "Vendor-only: signing key management, license key generation and verification. Not shipped."),
    ], [20, 25, 55], "Projects of the solution FalconOcr.sln")
    d.h2("2.2 Core namespaces")
    d.table(["Namespace", "Main types", "Purpose"], [
        ("FalconOcr.Input", "OcrDocument, DocumentPage, PageImageCache, NaturalComparer", "Opening PDFs, images, multi-page TIFFs, sequences, clipboard bitmaps; rotation and normalised crop; LRU cache of processed page images."),
        ("FalconOcr.Pdf", "PdfDocument, PdfiumNative", "PDFium P/Invoke (serialised by a global lock): render pages at a DPI, extract the text layer with font name, size, weight, italic flag and fill color."),
        ("FalconOcr.Imaging", "RgbImage, ImageOps", "24-bit BGR buffers, area-averaged bilinear resize, affine crop of rotated quadrilaterals, rotation, Otsu threshold."),
        ("FalconOcr.Engine", "PaddleOcrEngine, TextDetector, TextRecognizer, TextOrientationClassifier, Geometry, OcrOptions, LanguageCatalog", "ONNX Runtime sessions, DBNet post-processing, CTC decoding with character positions, language/model catalogue."),
        ("FalconOcr.Layout", "LayoutAnalyzer, StyleEstimator, TableDetector, FigureDetector", "Reconstruction of the page structure and styles."),
        ("FalconOcr.Model", "OcrPage, TextLine, TextStyle, LayoutLine, LayoutBlock, LayoutSection, TableRegion, TableCell, FigureRegion", "Result model shared by the viewers and the exporters."),
        ("FalconOcr.Processing", "OcrProcessor, PageProgress", "Page pipeline: text layer or OCR, then layout."),
        ("FalconOcr.Export", "Exporter, DocxExporter, XlsxExporter, HtmlExporter, TextMeasure, ExportHelpers", "Writing DOCX, XLSX, HTML and TXT; text fitting for Exact Copy."),
        ("FalconOcr.Licensing", "LicenseManager, LicenseCodec, LicenseInfo, MachineIdentity, TrialManager, LicensePublicKey", "License validation and storage, key encoding, machine code, 7-day trial."),
    ], [20, 35, 45], "Core namespaces")
    d.h2("2.3 Application structure")
    d.table(["Type", "Responsibility"], [
        ("Program", "Start-up: 64-bit check, license check, trial and activation window, crash handler (error.log)."),
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

    d.h1("3. Processing design")
    d.img("diagram-pipeline.png", "Page processing pipeline", 620)
    d.h2("3.1 Page loading")
    d.ul(["PDF pages are rendered by PDFium at the configured resolution (default 200 dpi) into 24-bit bitmaps; very large pages are limited to 144 megapixels.",
          "Images are read through a memory copy (the file is not locked), drawn onto white (transparency) and EXIF orientation is applied.",
          "DocumentPage.GetProcessedImage applies the rotation (0/90/180/270°) and the normalised crop rectangle (fractions of the rotated page, therefore valid for any PDF DPI).",
          "PageImageCache keeps the six most recently used processed images; rotating or cropping increments the page Version and invalidates the cache and the result.",
          "Effective DPI: PDF render DPI; the image DPI if between 150 and 1200; otherwise estimated from an A4/Letter page size (long side / 11.3 in) for large images, else 96."])
    d.h2("3.2 PDF text layer")
    d.p("If the setting is Auto and a PDF page contains at least 16 non-space characters, PdfDocument.ExtractLines reads every character with FPDFText_GetLooseCharBox (mapped to device pixels with FPDF_PageToDevice), font size, weight, font name (subset prefix and style suffixes removed) and fill color. "
        "Characters are grouped into lines at hard breaks, baseline jumps (> 0.6 × height), backward jumps and horizontal gaps > 1.6 × font size (column/cell gaps). Each line receives the style of its dominant run. With rotation or crop, the lines are re-mapped into the processed image.")
    d.h2("3.3 Text detection (DBNet)")
    d.ul(["Input: image scaled so that the longest side ≤ 2560 px (DetMaxSide) and each side is a multiple of 32; minimum side 64 px.",
          "Normalisation: BGR order with ImageNet mean/std, NCHW tensor; output: probability map.",
          "Post-processing: binary map (p > 0.3) → 8-connected components → pixel-corner convex hull per component (monotone chain) → minimum-area rectangle (rotating calipers).",
          "Box score = mean probability inside the rectangle; boxes below 0.6 are rejected. Unclip: both sides grow by D = A × 1.5 / L (area, perimeter). Boxes are mapped back to the original size."])
    d.h2("3.4 Recognition (SVTR + CTC)")
    d.ul(["Each box is cut out with an affine warp (quadrilateral → upright rectangle); tall crops (h ≥ 1.5 w) are rotated for vertical text.",
          "Optional orientation classifier (PP-LCNet, 80 × 160 input) flips lines with 180° probability > 0.9.",
          "Crops are resized to height 48, sorted by aspect ratio and batched (8 per batch) with zero padding to the widest ratio (minimum width 320).",
          "Greedy CTC decoding: arg-max per time step, blanks and repeats removed; class count = dictionary size + 2 (blank, space). The dictionary file is preferred; the model's 'character' metadata is a fallback.",
          "Per-character confidence and centre position are kept; positions are mapped along the line to page x-coordinates (used for table cell splitting and low-confidence highlighting).",
          "Lines with mean confidence < 0.5 (MinConfidence) or empty text are dropped."])
    d.h2("3.5 Layout analysis")
    d.table(["Step", "Algorithm"], [
        ("Background", "Mode of coarsely quantised colors on a sparse grid; near-white becomes pure white."),
        ("Style estimation (OCR lines)", "Otsu threshold inside the line box; polarity chosen by comparing both classes with the page background (supports white text on colored panels). Ink extent: rows with ≥ 1/6 of peak ink, extended over contiguous weak rows (ascenders/descenders). Font size = ink height ÷ zone factor (0.52 x-height only, 0.72 ascenders or descenders, 0.92 both, 0.95 brackets, 0.88 CJK), −1 px anti-aliasing. Stroke width = 2 × ink area ÷ perimeter; bold when > 1.25 × page median. Color = mean of the strongest ink pixels, snapped to black/white/grey. Background fill when it differs from the page background."),
        ("Size normalisation", "Sizes within 9 % of a frequent size snap to it; table cells snap to the table's dominant size (±30 %) and keep bold only for bold rows/columns."),
        ("Ruling lines", "Global Otsu binarisation; horizontal and vertical ink runs ≥ max(0.25 in, min side / 40) with small gaps; runs merged into strokes; thick strokes (filled areas) and strokes inside text lines are discarded; a rule must have background on both sides."),
        ("Ruled tables", "Horizontal and vertical rules that intersect form a group; row/column edges are clustered; a border between two grid cells exists when a rule covers > 50 % of the shared edge; cells without border are merged with union-find and made rectangular. Text lines are assigned to cells; lines crossing a column border are split at the character positions. Cell fill and alignment are detected."),
        ("Figures", "Grid cells (dpi/50 px) that differ from the background, minus text lines, rules and tables, dilated by two cells → connected components → filtered by size, edge slivers and whole-page tints; overlapping regions merged. Flat colored panels with text and containers holding many text lines stay text. Lines ≥ 70 % inside a figure become part of the picture."),
        ("Bullets", "Recognised bullet characters, a separate small marker box, or a solid blob left of the line (size 0.12–0.7 × text height, compact, vertically centred)."),
        ("Sections and columns", "Items (lines, tables, figures) are grouped into horizontal bands by vertical overlap; consecutive bands are merged into a section while they share interior gutters ≥ max(1.1 × median line height, 1.2 % page width). Sections with gutters are split into columns; a gutter pattern with short cells in ≥ 3 bands becomes a borderless table."),
        ("Blocks", "Within a column, segments on the same baseline form visual lines; a new paragraph starts at a larger gap (> median gap + max(0.4 h, 4 px)), a style change, a list marker, an indent change or after a short line. Alignment (left, centre, right, justify), first-line/hanging indent, line spacing and space before are measured."),
        ("Headings", "Blocks of ≤ 3 lines and ≤ 160 characters with size ≥ 1.18 × body size, or bold single lines of ≤ 12 words without final period; levels by descending size (max. 3). Numbered headings ('1. Introduction') win over ordered list items."),
    ], [20, 80], "Layout analysis steps")
    d.h2("3.6 Threading and performance")
    d.ul(["Recognition runs on a thread-pool worker; the UI receives progress through IProgress and per-page callbacks marshalled with BeginInvoke. One job runs at a time (SemaphoreSlim); ONNX Runtime uses one thread per physical core (IntraOpNumThreads = 0) — using logical cores was measured 2 × slower.",
          "Measured on the development machine: dense A4 page at 200 dpi 5.0–6.2 s (detection ≈ 1.6 s, recognition ≈ 3.4 s, layout ≈ 0.4 s); PDF text-layer page 0.3 s; model loading ≈ 0.9 s (warm-up at start-up).",
          "Sessions are cached per recognition model and rebuilt only when the thread setting changes."])

    d.h1("4. Data design")
    d.h2("4.1 Result model")
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
    d.h2("4.2 Persistent data")
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
    d.h2("4.3 Configuration defaults")
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

    d.h1("5. Export design")
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
    d.ul(["Every export is written to name.tmp and moved into place, so failures never leave a truncated file (FR-28).",
          "Exact Copy measures text with GDI+ to compute the horizontal scale that makes each line exactly as wide as the source line.",
          "Excel: '1,250', '12.5' and '45%' become numeric cells with number formats; text cells use shared strings; a style registry de-duplicates fonts, fills, borders and cell formats."])

    d.h1("6. Licensing design")
    d.img("diagram-licensing.png", "Start-up licensing flow", 560)
    d.h2("6.1 License keys")
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
    d.h2("6.2 Trial")
    d.ul(["TrialManager.Check() runs at start-up when no valid key is installed. The first call stores the start time; each call updates the last-seen time.",
          "Records are stored in the registry and in a hidden file, each protected by HMAC-SHA256 keyed with a constant and the machine hash (records cannot be edited or copied to another computer).",
          "The earliest valid start time wins, so deleting one copy does not restart the trial. A record with an invalid HMAC, or a current time more than 24 h before the last use, marks the trial as tampered (expired).",
          "Days left = 7 − floor(days used), minimum 1 while running; expired when ≥ 7 days have passed.",
          "UI: activation window at every start (Activate / Continue Trial / Exit), amber title-bar badge 'TRIAL · n days left — Activate now', Settings → License; after expiry only Activate / Exit. The CLI prints the trial status and exits with code 3 after expiry."])
    d.h2("6.3 Key generator")
    d.ul(["The signing key is a CNG ECDSA P-256 key created exportable, stored DPAPI-encrypted for the current Windows user; backups are Base64 text files that must be kept offline.",
          "'New signing key' writes src\\FalconOcr.Core\\Licensing\\LicensePublicKey.cs (Blob and fingerprint); after a rebuild the application accepts keys of the new signer and rejects all older keys.",
          "The KeyGen shows whether its key matches the public key compiled into the current build (fingerprint comparison).",
          "Every issued key is appended to issued.csv with licensee, serial, machine code, expiry and note."])
    d.note("Signatures prevent forged keys; they do not prevent modification of the executable itself. Use an obfuscator if protection against patching is required.")

    d.h1("7. User interface localization")
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
    d.h2("7.1 Workspace layout extensions")
    d.table(["Feature", "Design"], [
        ("Font combos (FR-37)", "Two editable combo boxes on the results toolbar: installed font families (common document fonts first, then alphabetical) and sizes 8–72 pt (any value 4–200 can be typed). Selection or Enter applies the value to every line of the selected paragraph and to the block style, so DOCX runs, HTML CSS and XLSX fonts use it."),
        ("Collapsible sidebar (FR-39)", "SidebarToggle docked at the bottom of the navigation bar; collapsed width 64 px (icons only, names as tooltips), expanded 184 px; AppSettings.SidebarCollapsed."),
        ("Resizable settings panel (FR-40)", "A WinForms Splitter (GripSplitter, with hover highlight and grip dots) docked between the centre area and the settings column; MinSize 280 px, the centre keeps at least 560 px; AppSettings.RightPanelWidth stores the width in 96-dpi pixels."),
        ("Trial notification (FR-41)", "Amber panel docked below the toolbar only while Program.IsTrial: clock icon, remaining days, 'Activate now' (opens the activation window) and ✕ (hide until the next start). MainForm.ShowActivation removes the banner and the title-bar badge after a successful activation."),
    ], [25, 75], "Workspace layout extensions")

    d.h1("8. Error handling and logging")
    d.table(["Situation", "Handling"], [
        ("Missing model files", "Warning at start-up listing the files; recognition is refused with a message."),
        ("Unsupported / unreadable file", "Collected and shown in one message after adding files; other files are still added."),
        ("Password-protected PDF", "'The PDF is password protected.'"),
        ("Recognition error", "Message box; the job stops; already recognized pages keep their results."),
        ("Export error", "Temporary file removed, message box, status 'Export failed.'"),
        ("Unhandled exception", "Logged to error.log with stack trace and shown once (re-entrancy guarded)."),
        ("Corrupt settings/history", "Defaults are used (the files are optional)."),
        ("No scanner", "'No scanner was found…' from WIA error 0x80210015."),
    ], [30, 70], "Error handling")

    d.h1("9. Build and deployment")
    d.h2("9.1 Repository layout")
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
    d.h2("9.2 Offline build")
    d.ul(["nuget.config clears all sources and uses packages\\ only; Directory.Build.props sets net47, C# 7.3, x64 and binding redirects.",
          "build.ps1 checks the vendored dependencies, builds the solution in Release/x64 and copies the application and CLI to dist\\ (KeyGen excluded).",
          "tools\\fetch-dependencies.ps1 (online, once) downloads models (ModelScope RapidAI/RapidOCR), ONNX Runtime and PDFium binaries and re-populates the NuGet feed.",
          "Verified: clean copy of the repository, empty NuGet cache and blocked network → restore and build succeed with 0 warnings, 0 errors."])
    d.h2("9.3 Deployment")
    d.p("The dist folder (≈ 78 MB, 115 files) is copied to the target computer — no installer, registry entries or administrator rights are needed. It contains FalconOcr.exe, falcon-ocr.exe, FalconOcr.Core.dll, managed dependencies and .NET Standard facades, native DLLs and the models folder. A license.key file may be placed in the folder or in %ProgramData%\\FalconOCR for site deployment.")
    d.h2("9.4 Extending the product")
    d.table(["Extension", "How"], [
        ("New recognition language", "Add the PP-OCRv5 rec model and dictionary to models\\rec, add an entry to LanguageCatalog.All (model, dictionary, default font, culture) and to the OcrLanguage enum; the UI lists it automatically."),
        ("New interface language", "Copy lang\\falcon-ocr.pot to lang\\<code>.po, translate it, and add the code to L.Languages (and a UI font in Theme if the script needs one)."),
        ("New export format", "Implement a writer over List<OcrPage>, add a value to ExportFormat and a case in Exporter.Export."),
        ("GPU inference", "Replace the ONNX Runtime package/DLL with a GPU build and append the execution provider in PaddleOcrEngine.GetSessionOptions."),
    ], [25, 75], "Extension points")

    d.h1("10. Requirements traceability")
    from doc_srs import FR
    comp = {
        "Input": "Input.OcrDocument / DocumentPage; App.WorkspacePage; Services.Scanner",
        "Page editing": "DocumentPage.Rotation / ApplyCrop; WorkspacePage; ImageViewer (crop)",
        "Recognition": "Engine.*; Processing.OcrProcessor; Pdf.PdfDocument; Services.OcrService",
        "Layout": "Layout.LayoutAnalyzer, StyleEstimator, TableDetector, FigureDetector",
        "Results": "UI.LayoutView, UI.ImageViewer, UI.PageCanvas; WorkspacePage",
        "Export": "Export.Exporter, DocxExporter, XlsxExporter, HtmlExporter",
        "Tools": "QuickOcrPage, BatchPage, HistoryPage, SettingsPage, AppSettings, FalconOcr.Cli",
        "Licensing": "Licensing.*; ActivationForm; MainForm (badge); FalconOcr.KeyGen",
        "User interface": "Localization.L / PoFile, lang\\*.po, Theme; WorkspacePage (font combos, GripSplitter, trial banner); MainForm (SidebarToggle); AppSettings",
    }
    d.table(["Requirement", "Title", "Design elements"], [(r[0], r[2], comp[r[1]]) for r in FR], [12, 30, 58], "Requirement to design traceability")
    return d
