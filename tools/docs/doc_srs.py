from docbuilder import Doc

# Requirement catalogue — shared with the test specification for traceability.
FR = [
    # id, area, title, description, priority
    ("FR-01", "Input", "Open PDF documents", "The system shall open PDF files of any page count and show every page as a document page. Password-protected PDFs shall be reported with a clear message.", "Must"),
    ("FR-02", "Input", "Open single images", "The system shall open PNG, JPEG (jpg, jpeg, jpe), BMP/DIB, GIF, TIFF, ICO, EMF and WMF images. EXIF orientation of photos shall be applied.", "Must"),
    ("FR-03", "Input", "Multi-page TIFF", "A multi-page TIFF shall open as one document with one page per frame.", "Must"),
    ("FR-04", "Input", "Image sequences", "The user shall be able to combine several images, or all images of a folder, into one multi-page document ordered naturally by file name (page2 before page10).", "Must"),
    ("FR-05", "Input", "Clipboard", "The user shall be able to create a document from an image on the clipboard, or add files copied to the clipboard.", "Should"),
    ("FR-06", "Input", "Scanner", "The user shall be able to acquire a page from a WIA-compatible scanner.", "Should"),
    ("FR-07", "Input", "Drag and drop", "Files and folders dropped on the workspace shall be added (folders as image sequences).", "Should"),
    ("FR-08", "Page editing", "Rotate", "The user shall be able to rotate a page clockwise in 90° steps before recognition.", "Must"),
    ("FR-09", "Page editing", "Crop", "The user shall be able to crop a page to a rectangle drawn on the image, crop further, or restore the full page.", "Must"),
    ("FR-10", "Page editing", "Delete", "The user shall be able to remove a document, a single page (Thumbnails view) or all documents from the workspace.", "Must"),
    ("FR-11", "Recognition", "Offline OCR engine", "Recognition shall run fully offline with the bundled PaddleOCR PP-OCRv5 models for English, Chinese, Japanese, Korean and Russian.", "Must"),
    ("FR-12", "Recognition", "PDF text layer", "For PDF pages with an embedded text layer the system shall use the embedded text, font sizes, weights, italics and colors (setting: Auto); the user can force OCR (Always OCR).", "Must"),
    ("FR-13", "Recognition", "Orientation correction", "Optionally, upside-down text lines shall be detected and corrected (orientation classifier).", "Could"),
    ("FR-14", "Recognition", "Recognition control", "The user shall be able to recognize the current document, a single page again, or all documents; see progress; and stop a running recognition.", "Must"),
    ("FR-15", "Layout", "Columns and reading order", "The system shall detect column sections and produce a reading order top-to-bottom, column by column.", "Must"),
    ("FR-16", "Layout", "Paragraphs, headings, lists", "The system shall group lines into paragraphs; classify headings (3 levels) by size and weight; detect bulleted and numbered list items, including bullets drawn as graphics.", "Must"),
    ("FR-17", "Layout", "Tables", "The system shall detect ruled tables with merged cells and cell fills, and simple borderless tables, and assign text to cells.", "Must"),
    ("FR-18", "Layout", "Pictures", "Non-text graphics (logos, photos, diagrams) shall be kept as images at their position.", "Must"),
    ("FR-19", "Layout", "Styles", "For each line the system shall estimate font size (pt), bold, text color and background fill; text-layer PDFs additionally provide font family and italic.", "Must"),
    ("FR-20", "Results", "Aligned side-by-side view", "The recognized page shall be rebuilt at the original positions and displayed next to the source page with synchronised zoom and scrolling.", "Must"),
    ("FR-21", "Results", "Selection and analysis view", "Selecting a line in either view shall highlight it in both. An 'Original Image' view shall show detected lines, tables and pictures.", "Must"),
    ("FR-22", "Results", "Correction", "The user shall be able to edit a recognized line in place; uncertain characters (confidence < 0.75) shall be highlighted.", "Must"),
    ("FR-23", "Results", "Formatting", "The user shall be able to change the paragraph style (Normal, Heading 1–3, bulleted or numbered list) and bold/italic/underline; changes shall be exported.", "Should"),
    ("FR-24", "Results", "Copy text", "The user shall be able to copy the text of the page or of the whole document.", "Should"),
    ("FR-25", "Export", "Word export", "The system shall export to .docx in three document types: Editable Document (flowing text, columns, styles, lists, tables, pictures), Exact Copy (positioned lines) and Plain Text.", "Must"),
    ("FR-26", "Export", "Excel export", "The system shall export to .xlsx with one worksheet per page; tables as cell grids with merges, borders and fills; numbers as numeric cells.", "Must"),
    ("FR-27", "Export", "HTML export", "The system shall export to a self-contained .html file (embedded images) as semantic flow or exact positioned copy.", "Must"),
    ("FR-28", "Export", "Output handling", "Exports shall be written to the configured output folder with a unique name, never leave a partial file on failure, and optionally open after export.", "Must"),
    ("FR-29", "Tools", "Quick OCR", "A Quick OCR page shall return the plain text of a pasted, dropped or opened image.", "Should"),
    ("FR-30", "Tools", "Batch processing", "The user shall be able to convert many files and folders unattended with chosen format, document type, language and output folder, with per-item status and a log.", "Must"),
    ("FR-31", "Tools", "History", "Recognitions and exports shall be recorded; the user can open outputs, show them in Explorer, reopen sources and clear the history.", "Should"),
    ("FR-32", "Tools", "Settings", "Recognition, export and advanced parameters shall be configurable and persisted per user.", "Must"),
    ("FR-33", "Tools", "Command line", "A command-line tool shall provide recognition and export for automation.", "Should"),
    ("FR-34", "Licensing", "License activation", "The application shall run licensed only with a key signed by the vendor; keys may be bound to one computer (machine code) and may expire.", "Must"),
    ("FR-35", "Licensing", "7-day trial", "Without a license the application shall run as a trial for 7 days from the first start, request a license at every start, show the remaining days, and refuse to start after the trial until a valid key is entered.", "Must"),
    ("FR-36", "Licensing", "Key generator", "A separate vendor tool shall create and protect the signing key, generate and verify license keys, and log every issued key.", "Must"),
]

NFR = [
    ("NFR-01", "Platform", "Windows 10/11 x64, .NET Framework 4.7 runtime, WinForms user interface."),
    ("NFR-02", "Offline", "The solution shall restore and build without network access (vendored NuGet feed, models and native libraries in the repository) and shall never contact the network at run time."),
    ("NFR-03", "Performance", "A dense A4 page at 200 dpi shall be recognized in ≤ 10 s on a current 4-core CPU (measured: 5.0–6.2 s). A PDF page with a text layer shall be processed in ≤ 1 s (measured: 0.3 s)."),
    ("NFR-04", "Accuracy", "On clean printed pages at 200 dpi, character accuracy shall be ≥ 98 %; ruled tables shall keep their grid (verified on the sample set: text, table grid and headings reproduced exactly)."),
    ("NFR-05", "Usability", "The user interface shall follow the ABBYY-style layout (toolbar, file list, source and result side by side, settings panel), support keyboard shortcuts, and be DPI aware."),
    ("NFR-06", "Reliability", "Exports use temporary files; unexpected errors are logged to %LOCALAPPDATA%\\FalconOCR\\error.log; corrupt settings fall back to defaults."),
    ("NFR-07", "Security", "Documents never leave the computer. License keys use ECDSA P-256 signatures; the vendor signing key is stored DPAPI-encrypted; trial data is HMAC-protected and bound to the computer."),
    ("NFR-08", "Maintainability", "Layered design (Core / App / CLI / KeyGen); engine, layout and exporters are UI-independent and testable from the CLI."),
    ("NFR-09", "Deployment", "The application folder is self-contained (≈ 78 MB, including models, ONNX Runtime, PDFium and app-local VC++ runtime); no installer or administrator rights are required."),
    ("NFR-10", "Compliance", "Third-party components are used under their licenses: PaddleOCR models (Apache-2.0), ONNX Runtime (MIT), PDFium (BSD-3/Apache-2.0), OpenXML SDK (MIT), VC++ runtime (Microsoft redistributable)."),
]


def build():
    d = Doc("Software Requirements Specification", "Offline OCR for PDF and images with layout-preserving Word, Excel and HTML export", "FOCR-SRS-001",
            "This document specifies the functional and non-functional requirements of Falcon OCR 1.0. It is the reference for design, implementation, testing and acceptance.")

    d.h1("1. Introduction")
    d.h2("1.1 Purpose")
    d.p("This Software Requirements Specification (SRS) describes what Falcon OCR shall do. It is written for the product owner, developers, testers and the reviewers who accept the product. Each requirement has an identifier (FR-nn, NFR-nn) that is referenced by the System Design Document (FOCR-SDD-001) and the Test Case Specification (FOCR-TCS-001).")
    d.h2("1.2 Product scope")
    d.p("Falcon OCR is a Windows desktop application that converts scanned documents, photos, image sequences and PDF files into editable documents. Text is recognized offline with PaddleOCR PP-OCRv5 models; the original page structure (columns, headings, paragraphs, lists, tables, pictures) and styling (font size, weight, color, fills) are reconstructed and exported to Microsoft Word, Microsoft Excel and HTML. The user interface follows the layout familiar from ABBYY FineReader.")
    d.h2("1.3 Definitions and abbreviations")
    d.table(["Term", "Meaning"], [
        ("OCR", "Optical character recognition — converting images of text into machine-encoded text."),
        ("PP-OCRv5", "PaddleOCR 5th-generation models: text detection (DBNet), line orientation (PP-LCNet) and recognition (SVTR with CTC)."),
        ("ONNX / ONNX Runtime", "Open model format and the Microsoft inference engine used to run the models on the CPU."),
        ("Text layer", "Text objects embedded in a digital PDF (as opposed to a scanned image)."),
        ("Editable Document", "Export type that produces flowing, editable text with the reconstructed structure and styles."),
        ("Exact Copy", "Export type that places every line, table and picture at its original position."),
        ("Machine code", "16-character identifier of a computer (e.g. P9BB-82PR-DS2C-1R5Y) used to bind a license."),
        ("License key", "Text starting with FOCR- that contains signed license data."),
        ("Trial", "7-day evaluation period that starts with the first launch."),
        ("DPAPI", "Windows Data Protection API, used to encrypt the vendor's signing key."),
    ], [25, 75], "Terms")
    d.h2("1.4 References")
    d.ul(["FOCR-SDD-001 System Design Document", "FOCR-SCR-001 Screen Design Document", "FOCR-TCS-001 Test Case Specification",
          "FOCR-UM-001 User Manual", "Reference UI: ABBYY FineReader-style workspace screenshot supplied by the product owner"])

    d.h1("2. Overall description")
    d.h2("2.1 Product perspective")
    d.p("Falcon OCR is a standalone product consisting of the desktop application (FalconOcr.exe), a command-line tool (falcon-ocr.exe) and a vendor-only license key generator (FalconOcrKeyGen.exe). All processing happens locally; no cloud service is used.")
    d.img("diagram-architecture.png", "Product components", 600)
    d.h2("2.2 User classes")
    d.table(["User class", "Description", "Main tasks"], [
        ("Office user", "Converts letters, reports, invoices and forms; little technical knowledge.", "Add files, recognize, check the result, export to Word/Excel."),
        ("Power user / archivist", "Processes large volumes, multilingual material.", "Batch processing, image sequences, command line, advanced settings."),
        ("Administrator", "Deploys the application in an organisation.", "Copy the application folder, deploy license.key, configure defaults."),
        ("Vendor", "Sells and licenses Falcon OCR.", "Generate license keys with the KeyGen, manage the signing key."),
    ], [20, 40, 40], "User classes")
    d.h2("2.3 Operating environment")
    d.ul(["Windows 10 or Windows 11, 64-bit.", ".NET Framework 4.7 or later (included in Windows 10 1703+ and Windows 11).",
          "At least 4 GB RAM (8 GB recommended for large PDFs), 200 MB free disk space.",
          "Optional: Microsoft Word / Excel or compatible software to open exported files; WIA scanner driver for scanning."])
    d.h2("2.4 Design and implementation constraints")
    d.ul(["Implementation in C# on .NET Framework 4.7 with Windows Forms.",
          "x64 only (ONNX Runtime and PDFium are 64-bit native libraries).",
          "Offline build: all NuGet packages, models and native libraries are stored in the repository.",
          "OCR models: PaddleOCR PP-OCRv5 (ONNX export) — no model download at run time."])
    d.h2("2.5 Assumptions and dependencies")
    d.ul(["Input pages are printed text; handwriting recognition is out of scope.",
          "Scanned pages have at least 150 dpi effective resolution; 200–300 dpi gives the best results.",
          "Fonts of the original document are not embedded in exports when unknown; Calibri (Latin/Cyrillic), Microsoft YaHei (Chinese), Yu Gothic (Japanese) and Malgun Gothic (Korean) are used as defaults."])

    d.h1("3. Functional requirements")
    d.p("Priority uses MoSCoW: **Must** (required for release), **Should** (important), **Could** (desirable).")
    areas = []
    for r in FR:
        if r[1] not in areas:
            areas.append(r[1])
    for i, area in enumerate(areas, 1):
        d.h2(f"3.{i} {area}")
        d.table(["ID", "Requirement", "Description", "Priority"],
                [(r[0], r[2], r[3], r[4]) for r in FR if r[1] == area], [9, 20, 61, 10])

    d.h2(f"3.{len(areas) + 1} Supported languages")
    d.table(["Language", "Recognition model", "Notes"], [
        ("English", "en_PP-OCRv5_rec_mobile", "Latin letters, digits, punctuation."),
        ("Chinese", "ch_PP-OCRv5_rec_mobile", "Simplified and Traditional Chinese, also English."),
        ("Japanese", "ch_PP-OCRv5_rec_mobile", "PP-OCRv5's main model covers Kanji, Hiragana, Katakana."),
        ("Korean", "korean_PP-OCRv5_rec_mobile", "Hangul, lines joined with spaces."),
        ("Russian", "eslav_PP-OCRv5_rec_mobile", "East Slavic Cyrillic (Russian, Ukrainian, Belarusian)."),
    ], [18, 32, 50], "Languages")

    d.h2(f"3.{len(areas) + 2} Licensing rules")
    d.table(["Rule", "Specification"], [
        ("Trial length", "7 days counted from the first start of the application on the computer. Day 1 shows '7 of 7 days remaining'; the trial ends when 7 × 24 hours have passed."),
        ("Prompt", "While no valid license is installed, the activation window is shown at every start with Activate, Continue Trial and Exit."),
        ("Trial over", "After the trial the activation window cannot be skipped: only Activate and Exit remain."),
        ("Tamper protection", "Trial state is stored in the registry and in a file; edited data or a system clock set back more than 24 hours before the last use ends the trial."),
        ("Key content", "Licensee name, serial number, issue date, optional expiry date, optional machine binding."),
        ("Key validation", "Signature (ECDSA P-256/SHA-256) with the public key compiled into the application; machine code; expiry date."),
        ("Key storage", "%LOCALAPPDATA%\\FalconOCR\\license.key (per user); also read from the application folder and %ProgramData%\\FalconOCR."),
        ("Command line", "falcon-ocr follows the same rules; --license <key> activates; --machine-code prints the code."),
    ], [22, 78], "Licensing rules")

    d.h1("4. External interface requirements")
    d.h2("4.1 User interface")
    d.p("The main window shall contain: a green title bar with product name, trial badge and window buttons; a left navigation bar (Home, OCR, Batch Process, History, Settings); a ribbon-style toolbar (Add Files, Scan, From Clipboard, Rotate, Crop, Delete, Recognize, Export, Settings, Help); a Files/Thumbnails panel; the Source Page viewer; the Recognized Text / Original Image panel; the OCR Settings and Output Format panel with the Export button; and a status bar. Details: Screen Design Document FOCR-SCR-001.")
    d.h2("4.2 Software interfaces")
    d.table(["Interface", "Use"], [
        ("ONNX Runtime 1.22 (onnxruntime.dll)", "Inference of detection, orientation and recognition models on the CPU."),
        ("PDFium (pdfium.dll)", "Rendering PDF pages, reading the text layer with fonts, sizes and colors."),
        ("OpenXML SDK 2.20", "Writing .docx and .xlsx files."),
        ("WIA (Windows Image Acquisition)", "Scanner access through late-bound COM."),
        ("File system / registry", "Settings, history, license and trial state in the user profile."),
    ], [35, 65], "Software interfaces")
    d.h2("4.3 File formats")
    d.table(["Direction", "Formats"], [
        ("Input", "PDF; PNG, JPG/JPEG/JPE, BMP/DIB, GIF, TIF/TIFF (multi-page), ICO, EMF, WMF"),
        ("Output", "DOCX (Word 2007+), XLSX (Excel 2007+), HTML5 (self-contained), TXT (UTF-8)"),
        ("License", "Key text (FOCR-…) or .lic file containing the key"),
    ], [20, 80], "File formats")

    d.h1("5. Non-functional requirements")
    d.table(["ID", "Category", "Requirement"], NFR, [10, 16, 74], "Non-functional requirements")

    d.h1("6. Acceptance criteria")
    d.ul(["All Must requirements are implemented and their test cases in FOCR-TCS-001 pass.",
          "The solution builds offline from a clean copy of the repository with build.ps1.",
          "The sample documents in samples\\ are reproduced with correct text, headings, list items, table grid, columns, pictures and colors in Word, Excel and HTML.",
          "A key generated by the KeyGen activates the application; keys for other computers, expired, altered or malformed keys are rejected; the trial behaves as specified in section 3."])

    d.h1("Appendix A. Requirements summary")
    d.table(["ID", "Area", "Requirement", "Priority"], [(r[0], r[1], r[2], r[4]) for r in FR], [10, 18, 57, 15], "All functional requirements")
    return d
