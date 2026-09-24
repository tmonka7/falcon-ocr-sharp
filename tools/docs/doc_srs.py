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
    ("FR-11", "Recognition", "Offline OCR engine", "Recognition shall run fully offline with the bundled PaddleOCR PP-OCRv5 models for English, Chinese, Japanese and Russian. (The Korean model is bundled but the language is not offered in version 1.0.)", "Must"),
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
    ("FR-37", "User interface", "Font selection", "The results toolbar shall provide a font family combo box and a font size combo box that show and change the font of the selected paragraph; the change shall be exported.", "Must"),
    ("FR-38", "User interface", "Interface languages", "The user interface shall be available in English, Simplified Chinese and Japanese. Translations shall be stored as gettext PO files that can be edited without rebuilding; the language is chosen in Settings (default: Windows display language).", "Must"),
    ("FR-39", "User interface", "Collapsible sidebar", "A button at the bottom of the navigation bar shall collapse it to icons only and expand it again; the state shall be remembered.", "Must"),
    ("FR-40", "User interface", "Resizable settings panel", "The width of the right-hand settings panel shall be adjustable by dragging its left edge with the mouse (280–640 px); the width shall be remembered.", "Must"),
    ("FR-41", "User interface", "Trial notification", "While running as a trial, a notification with the remaining days and an 'Activate now' button shall be displayed below the toolbar; it can be hidden until the next start and disappears after activation.", "Must"),
    ("FR-42", "Settings", "Default font", "Settings shall offer a default font (Automatic = font of the recognition language, or any installed font) used for recognized text whose original font is unknown, in the results view and in DOCX, XLSX and HTML exports; the CLI accepts --font NAME.", "Must"),
    ("FR-43", "User interface", "Collapsible Files / Thumbnails panel", "A button at the bottom of the Files / Thumbnails panel of the home screen shall collapse the panel to a narrow strip and expand it again; the state shall be remembered.", "Must"),
    ("FR-44", "User interface", "Application icon", "The application, the KeyGen tool and their windows shall use the Falcon OCR icon (green tile with a scanned page); the Settings screen shall not display OCR model information.", "Should"),
    ("FR-45", "Results", "Page navigation and zoom", "The viewers shall offer Fit width, Fit page and fixed zoom levels (50–300 %), Ctrl+wheel and Ctrl +/− zoom, a hand tool and middle-mouse panning, and page navigation with PgUp/PgDn and a page counter.", "Should"),
    ("FR-46", "Input", "Document management", "Each document in the Files list shall show its page count and recognition state, and offer Recognize, Export, Export as…, Rename…, Open containing folder, Remove and Remove all.", "Should"),
    ("FR-47", "Recognition", "Model availability check", "Missing OCR model files shall be detected at start-up and before every recognition and reported by file name; recognition shall not start without them.", "Must"),
    ("FR-48", "Export", "Partial export and Export as", "Exporting a partly recognized document shall let the user recognize the missing pages first or export only the recognized pages; 'Export as…' shall let the user choose the file name and folder.", "Should"),
    ("FR-49", "Licensing", "License management in Settings", "Settings shall show the license state and machine code and offer 'Change license key…' and 'Copy machine code'.", "Should"),
    ("FR-50", "Settings", "Automatic recognition and defaults", "Settings shall offer 'Recognize automatically when files are added', 'Restore defaults' and 'Open data folder'.", "Could"),
    ("FR-51", "Tools", "Diagnostic snapshot mode", "With the environment variable FALCON_SNAPSHOT set to a folder, the application shall recognize the file given on the command line, save a PNG of every screen to that folder and exit (only while the trial or a license is valid).", "Could"),
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
    ("NFR-11", "Localization", "Every user-visible text of the application shall be translatable through the PO catalogs; a missing catalog or an untranslated entry shall fall back to English without error; CJK interfaces shall use a font that renders the script."),
    ("NFR-12", "Responsiveness", "Recognition and export shall run off the UI thread; the window shall stay responsive and show progress; the models shall be loaded in the background after start-up so the first recognition is not delayed."),
    ("NFR-13", "Resource usage", "Page images shall be cached in a small LRU cache (6 processed pages) and source image files shall not be locked while they are open in the workspace."),
]

# Detailed specification per functional requirement: (details / business rules, acceptance criterion).
DETAILS = {
    "FR-01": (["Pages are rendered on demand at the PDF rendering resolution (default 200 dpi).",
               "Protected files are rejected with 'The PDF is password protected.'; opening them with a password is not in scope."],
              "A 3-page PDF appears as one document with 3 pages."),
    "FR-02": (["Images are converted to 24-bit RGB on white; EXIF orientations 3, 6 and 8 are applied.",
               "Source files are not locked."],
              "Every listed format opens; a phone photo with EXIF orientation 6 is shown upright."),
    "FR-03": (["Each frame becomes a page 'Page n'; a single-frame TIFF is an ordinary image."],
              "A 3-frame TIFF is recognized and exported as one 3-page document."),
    "FR-04": (["Menu entries 'Add images as one document (image sequence)…' and 'Add folder as image sequence…' (not recursive).",
               "Digit groups are compared as numbers; TIFF frames become separate pages."],
              "page1, page2, page10 are exported in the order 1, 2, 10."),
    "FR-05": (["Files copied in Explorer are added as files; a bitmap becomes a document Clipboard_01, Clipboard_02, …"], None),
    "FR-06": (["The WIA dialog is used through late-bound COM; the page is saved as %LOCALAPPDATA%\\FalconOCR\\scans\\Scan_yyyyMMdd_HHmmss.png and added."],
              "Without a scanner a clear message appears and the application continues."),
    "FR-07": (["A dropped folder becomes one image sequence; PDFs in it are added as separate documents; unsupported files are listed in one summary message."],
              "A folder with 5 PNG files and 1 PDF adds a 5-page sequence and a PDF document."),
    "FR-08": (["Toolbar, viewer button or Ctrl+R; rotation discards the page's result and crop; text-layer lines are mapped into the rotated page."],
              "A sideways page is recognized correctly after rotation."),
    "FR-09": (["The crop is stored relative to the rotated page, so it is valid at any resolution.",
               "On a cropped page the user chooses 'Yes = crop further, No = restore the full page'."],
              "Only the cropped region is recognized; restoring brings back the full page."),
    "FR-10": (["Documents with results and single pages are removed after confirmation; 'Remove all' asks first. Files on disk are never deleted."],
              "Removed documents disappear from the list and the Total Files counter; the source files are unchanged."),
    "FR-11": (["Lines with a mean confidence below the minimum line confidence (default 0.5) are discarded."],
              "Recognition succeeds with the network disabled."),
    "FR-12": (["Auto mode reads a page from its text layer if it contains at least 16 characters; other pages, and all pages with 'Always run OCR' (CLI --ocr-only), are recognized from pixels."],
              "A digital PDF page is processed in ≤ 1 s and reproduces the original text exactly."),
    "FR-13": (["Option 'Correct upside-down text lines (orientation classifier)', CLI --cls; default off."],
              "An upside-down line is recognized correctly with the option on."),
    "FR-14": (["F5 recognizes the pages without result, current page first; for a fully recognized document the user confirms a new recognition.",
               "While running, the Recognize button becomes Stop; only one recognition runs at a time."],
              "Stop keeps the pages already recognized."),
    "FR-15": (["Order: sections top to bottom, columns left to right, lines top to bottom. 'Single column' disables columns; 'Text lines only' outputs one paragraph per line."],
              "The two-column section of the sample report becomes two Word columns in the right order."),
    "FR-16": (["Headings: ≤ 3 lines, ≥ 1.18 × the body size (size carrying most characters) or a short bold line; distinct sizes map to levels 1–3.",
               "List markers: bullet characters (•, ●, ■, -, – …), numbering (1., a), iv.) or drawn bullet graphics."],
              "The sample report keeps its title, headings, bulleted list and numbered headings."),
    "FR-17": (["Ruled tables come from intersecting strokes; lines crossing cell borders are split using character positions. Switchable (CLI --no-tables)."],
              "The sample table keeps its grid, header fill and numbers in Word, Excel and HTML."),
    "FR-18": (["Regions that differ from the background after masking text and rules become PNG pictures; uniform areas and containers holding text are not pictures."],
              "The sample picture appears at the same place in every export format."),
    "FR-19": (["Effective resolution = image dpi if 150–1200, otherwise (uncropped, long side ≥ 1600 px) long side / 11.3, limited to 96–600 dpi."],
              "Heading sizes of the sample are within ±1 pt; bold and colored text is reproduced."),
    "FR-20": (["Both views use the same geometry; zoom and scrolling are synchronised in both directions."], None),
    "FR-21": (["Clicking a line selects it in both views; Original Image overlays lines, table cells and pictures."], None),
    "FR-22": (["Double-click or F2 opens the editor, Enter confirms. Highlighting can be switched off; edits are discarded when the page is rotated, cropped or recognized again."],
              "An edited word appears corrected in the exported file."),
    "FR-23": (["Styles Normal, Heading 1–3, List item; bulleted and numbered list; Bold, Italic, Underline for the selected paragraph."],
              "A paragraph set to Heading 2 is exported as Word style Heading 2 and as h2 in HTML."),
    "FR-24": (["More menu: 'Copy page text', 'Copy document text' (reading order)."], None),
    "FR-25": (["Exact Copy scales each line horizontally (40–250 %) to its original width. The document font is the default font (FR-42); the language tag is en-US, zh-CN, ja-JP or ru-RU."],
              "All three types open in Word without repair prompts."),
    "FR-26": (["Worksheets 'Page n' without grid lines; text outside tables on a column grid, paragraphs merged across their columns.",
               "Numbers with sign, thousands separators, decimals or % become numeric cells with a matching format."],
              "'1,250' in the sample table is numeric, so it can be summed in Excel."),
    "FR-27": (["Editable: h1–h3, p, ul/ol, table, figure with CSS grid columns; Exact Copy: absolute positions; images as data:image/png;base64."],
              "The file displays correctly when copied alone to another computer."),
    "FR-28": (["Default folder Documents\\Falcon OCR Output (created if needed); an existing name gets ' (2)', ' (3)' …; invalid characters become '_'.",
               "The file is written as name.tmp and renamed after success."],
              "Exporting twice creates report.docx and report (2).docx; a failed export leaves no file."),
    "FR-29": (["Input by Ctrl+V, drop or 'Open image…' (images and PDF, first page only); 'Copy text' and 'Open in workspace'; nothing is saved."],
              "A pasted screenshot returns its text; nothing is written to disk or to History."),
    "FR-30": (["Item states: Waiting, Recognizing…, Page i/n, Done, Failed, Stopped.",
               "A failed item does not stop the batch; a new Start skips items already Done."],
              "10 files including one corrupt file give 9 outputs and 1 'Failed' entry."),
    "FR-31": (["Entries (time, source, output, format, pages, language, duration) are listed newest first; the latest 500 are kept."],
              "An export appears at the top of History and 'Open output' opens it."),
    "FR-32": (["Settings sections: General, Recognition, Export, Advanced recognition, License. Advanced settings are also reachable from the workspace."],
              "Changed settings survive a restart; a corrupt settings file is replaced by defaults."),
    "FR-33": (["Progress goes to standard error, output paths to standard output (section 6.3); same engine, exporters and licensing as the application."],
              "'falcon-ocr samples\\report.png -f docx,html' writes both files and returns 0."),
    "FR-34": (["Keys may be typed, pasted (spaces, line breaks and hyphens ignored) or loaded from a .lic, .key or .txt file; rules in section 3.12, format in section 5.5."],
              "Altered keys, keys for another computer and expired keys are rejected with the matching message."),
    "FR-35": (["All features are available during the trial; state storage as in section 5.6."],
              "Day 1 shows 7 of 7 days; after 7 × 24 h only Activate and Exit remain."),
    "FR-36": (["GUI and command line (section 6.4), never copied to dist; expiry perpetual, 30 days, 1 year or a date; serials start at 1001; every key is self-verified and appended to issued.csv."],
              "A generated key activates the application built with the matching public key."),
    "FR-37": (["The size box accepts typed values (Enter applies); both boxes show the font of the selected paragraph."], None),
    "FR-38": (["First start: Chinese or Japanese if Windows uses that display language, otherwise English. A change takes effect after the offered restart."],
              "All screens appear in the chosen language after the restart."),
    "FR-39": (["Width 184 px expanded, 64 px collapsed; collapsed items show their names as tooltips."], None),
    "FR-40": (["Default 340 px; the viewers keep at least 560 px; stored in 96-dpi pixels so it is DPI independent."], None),
    "FR-41": (["The title bar also shows 'TRIAL · n days left — Activate now'."],
              "After activation from the notification neither badge nor notification is shown; × hides it only until the next start."),
    "FR-42": (["Automatic = Calibri (English, Russian), Microsoft YaHei (Chinese), Yu Gothic (Japanese); applies to pages recognized afterwards; PDF text-layer fonts are kept."],
              "With Arial as default font a scan is exported in Arial in DOCX, XLSX and HTML."),
    "FR-43": (["'Hide panel' collapses the panel to a strip that expands it again."], None),
    "FR-44": (["Used for both executables, the main window, the activation window and the KeyGen; missing models are still reported (FR-47)."], None),
    "FR-45": (["Ctrl +/− change the zoom by a factor of 1.2; the info line shows size, dpi, rotation and crop."],
              "PgDn moves to the next page; Fit page shows the whole page; a typed '175' zooms to 175 %."),
    "FR-46": (["States: '1 page', 'n pages' or 'n images' plus 'recognized' or 'i/n done'. The new name is used for export file names."], None),
    "FR-47": (["Start-up: warning with the hint to run tools\\fetch-dependencies.ps1; otherwise models are loaded in the background. CLI: exit code 2."],
              "Renaming a model file produces a message naming exactly that file."),
    "FR-48": (["No page recognized: 'Recognize it now?'. Some pages missing: 'Yes = recognize them first, No = export only the recognized pages'."],
              "Choosing No exports only the recognized pages."),
    "FR-49": (["The state reads e.g. 'Licensed to … · serial … · valid until … · this computer only', or the trial message."],
              "A key entered via 'Change license key…' replaces the stored key and the License section shows the new licensee."),
    "FR-50": (["Automatic recognition is off by default; restored defaults take effect with Save; the data folder is %LOCALAPPDATA%\\FalconOCR."],
              "With automatic recognition on, a dropped file is recognized without a further click; Restore defaults followed by Save resets the settings."),
    "FR-51": (["Nine PNG files (1-loaded … 3-settings) are written; an expired trial cannot be bypassed this way."], None),
}

AREA_INTRO = {
    "Input": "Every input becomes a document with one or more pages; source files are never modified.",
    "Page editing": "Every edit changes the processed image and discards the page's recognition result.",
    "Recognition": "A page becomes text lines with positions, confidence and style — from a PDF text layer or with PaddleOCR.",
    "Layout": "Layout analysis turns the lines into sections, columns, paragraphs, headings, lists, tables and pictures.",
    "Results": "The results area lets the user check and correct the recognition before export.",
    "Export": "Export writes the recognized pages to Word, Excel, HTML or text in one of three document types.",
    "Tools": "The navigation pages besides Home, and the command-line front end.",
    "Licensing": "Vendor-signed keys, an evaluation period and the vendor's key generator.",
    "User interface": "Added in document versions 1.1 and 1.2.",
    "Settings": "Settings beyond the general configuration of FR-32.",
}


def use_case(d, uid, title, actor, pre, trigger, main, alt, post, fr):
    d.h3(f"{uid} {title}")
    d.table(["Item", "Description"], [("Actor", actor), ("Preconditions", pre), ("Trigger", trigger), ("Postconditions", post), ("Requirements", fr)], [22, 78])
    d.p("**Main flow**")
    d.ol(main)
    d.p("**Alternative and exception flows**")
    d.ul(alt)


def build():
    d = Doc("Software Requirements Specification", "Offline OCR for PDF and images with layout-preserving Word, Excel and HTML export", "FOCR-SRS-001",
            "This document specifies the functional and non-functional requirements of Falcon OCR 1.0. It is the reference for design, implementation, testing and acceptance.")

    # ------------------------------------------------------------------ 1
    d.h1("1. Introduction")
    d.h2("1.1 Purpose")
    d.p("This Software Requirements Specification (SRS) describes what Falcon OCR shall do. It is written for the product owner, developers, testers and the reviewers who accept the product. Each requirement has an identifier (FR-nn, NFR-nn) that is referenced by the System Design Document (FOCR-SDD-001) and the Test Case Specification (FOCR-TCS-001).",
        "It describes version 1.0 including the extensions of document versions 1.1 and 1.2; capabilities that exist in the code but are not offered (e.g. Korean) are marked as such.")
    d.h2("1.2 Product scope")
    d.p("Falcon OCR is a Windows desktop application that converts scanned documents, photos, image sequences and PDF files into editable documents. Text is recognized offline with PaddleOCR PP-OCRv5 models; the original page structure (columns, headings, paragraphs, lists, tables, pictures) and styling (font size, weight, color, fills) are reconstructed and exported to Microsoft Word, Microsoft Excel and HTML. The user interface follows the layout familiar from ABBYY FineReader.")
    d.p("The main business goals are:")
    d.ul(["**Confidentiality** — documents are processed only on the user's computer.",
          "**Less retyping** — exports keep structure and formatting.",
          "**Simple deployment** — a self-contained folder that runs without installation or administrator rights.",
          "**Controlled distribution** — a 7-day evaluation followed by vendor-signed license keys."])
    d.h2("1.3 Definitions and abbreviations")
    d.table(["Term", "Meaning"], [
        ("OCR", "Optical character recognition — converting images of text into machine-encoded text."),
        ("PP-OCRv5", "PaddleOCR 5th-generation models: text detection (DBNet), line orientation (PP-LCNet) and recognition (SVTR with CTC)."),
        ("ONNX / ONNX Runtime", "Open model format and the Microsoft inference engine used to run the models on the CPU."),
        ("PDFium", "PDF rendering library (as used by Chrome) used to render pages and read the text layer."),
        ("Text layer", "Text objects embedded in a digital PDF (as opposed to a scanned image)."),
        ("Document", "An entry of the Files list: a PDF, an image, a multi-page TIFF, an image sequence, a clipboard image or a scan."),
        ("Image sequence", "Several image files combined into one multi-page document in natural file-name order."),
        ("Section", "A horizontal band of a page with a fixed number of columns."),
        ("Confidence", "Recognizer probability (0–1) per character and per line."),
        ("Editable Document", "Export type that produces flowing, editable text with the reconstructed structure and styles."),
        ("Exact Copy", "Export type that places every line, table and picture at its original position."),
        ("Machine code", "16-character identifier of a computer (e.g. P9BB-82PR-DS2C-1R5Y) used to bind a license."),
        ("License key", "Text starting with FOCR- that contains signed license data."),
        ("Trial", "7-day evaluation period that starts with the first launch."),
        ("DPAPI", "Windows Data Protection API, used to encrypt the vendor's signing key."),
        ("HMAC", "Keyed hash (here HMAC-SHA256) that protects the trial records against editing."),
    ], [25, 75], "Terms")
    d.h2("1.4 References")
    d.ul(["FOCR-SDD-001 System Design Document", "FOCR-SCR-001 Screen Design Document", "FOCR-TCS-001 Test Case Specification",
          "FOCR-UM-001 User Manual", "Reference UI: ABBYY FineReader-style workspace screenshot supplied by the product owner",
          "README.md of the repository (build, command line, licensing workflow)"])
    d.h2("1.5 Conventions")
    d.ul(["Requirement statements use **shall**; explanatory text uses present tense.",
          "Functional requirements are numbered FR-01 … FR-51, non-functional requirements NFR-01 … NFR-13, use cases UC-01 … UC-09. Numbers are never reused.",
          "Priorities follow MoSCoW: **Must** (required for release), **Should** (important), **Could** (desirable). Won't items are listed in section 2.7.",
          "Pixel sizes of the user interface are given at 96 dpi (100 % scaling) and scale with the display DPI.",
          "Paths use Windows environment variables: %LOCALAPPDATA% (C:\\Users\\<user>\\AppData\\Local), %APPDATA% (…\\AppData\\Roaming), %ProgramData% (C:\\ProgramData).",
          "Texts in quotes are the English user-interface texts."])

    # ------------------------------------------------------------------ 2
    d.h1("2. Overall description")
    d.h2("2.1 Product perspective")
    d.p("Falcon OCR is a standalone product consisting of the desktop application (FalconOcr.exe), a command-line tool (falcon-ocr.exe) and a vendor-only license key generator (FalconOcrKeyGen.exe). All processing happens locally; no cloud service is used.")
    d.img("diagram-architecture.png", "Product components", 600)
    d.p("Application and CLI share the class library FalconOcr.Core (engine, PDF access, layout, exporters, licensing, localization); the KeyGen uses the same licensing code, so keys are checked with exactly the rules of the application.")
    d.h2("2.2 Product functions")
    d.p("Every page passes the processing chain below; the function groups Input, Page editing, Recognition, Layout, Results, Export, Tools, Licensing, User interface and Settings are specified in chapter 3.")
    d.img("diagram-pipeline.png", "Processing pipeline", 600)
    d.h2("2.3 User classes")
    d.table(["User class", "Description", "Main tasks"], [
        ("Office user", "Converts letters, reports, invoices and forms; little technical knowledge.", "Add files, recognize, check the result, export to Word/Excel."),
        ("Power user / archivist", "Processes large volumes, multilingual material.", "Batch processing, image sequences, command line, advanced settings."),
        ("Administrator", "Deploys the application in an organisation.", "Copy the application folder, deploy license.key, configure defaults."),
        ("Vendor", "Sells and licenses Falcon OCR.", "Generate license keys with the KeyGen, manage the signing key."),
        ("Translator", "Maintains an interface language.", "Edit lang\\*.po with Poedit or a text editor; check with po_tool."),
    ], [20, 40, 40], "User classes")
    d.h2("2.4 Operating environment")
    d.ul(["Windows 10 or Windows 11, 64-bit (the application refuses to run as a 32-bit process).",
          ".NET Framework 4.7 or later (included in Windows 10 1703+ and Windows 11).",
          "At least 4 GB RAM (8 GB recommended for large PDFs), 200 MB free disk space.",
          "The main window has a minimum size of 1100 × 700 px (scaled with DPI).",
          "Optional: Microsoft Word / Excel or compatible software to open exported files; WIA scanner driver for scanning.",
          "Build: .NET SDK (verified with 9.0) or Visual Studio 2022; no network needed."])
    d.h2("2.5 Design and implementation constraints")
    d.ul(["Implementation in C# on .NET Framework 4.7 with Windows Forms; the user interface is built in code (no designer files).",
          "x64 only (ONNX Runtime and PDFium are 64-bit native libraries).",
          "Offline build: all NuGet packages, models and native libraries are stored in the repository; nuget.config clears all online sources.",
          "OCR models: PaddleOCR PP-OCRv5 (ONNX export) — no model download at run time.",
          "The license verification key is compiled into the application; changing the signing key requires a rebuild."])
    d.h2("2.6 Assumptions and dependencies")
    d.ul(["Input pages are printed text; handwriting recognition is out of scope.",
          "Scanned pages have at least 150 dpi effective resolution; 200–300 dpi gives the best results.",
          "Fonts referenced by exports (by default Calibri, Microsoft YaHei, Yu Gothic) are installed where the files are opened.",
          "The Windows MachineGuid is stable for the lifetime of a Windows installation; reinstalling Windows changes the machine code.",
          "The system clock is approximately correct; deliberate changes are detected as described in section 3.12."])
    d.h2("2.7 Out of scope and future items")
    d.table(["Item", "Status in 1.0"], [
        ("Korean recognition", "Model and dictionary are bundled; the language entry is disabled (not offered in the UI, CLI -l ko falls back to English). Can be re-enabled in LanguageCatalog."),
        ("Handwriting recognition", "Not supported."),
        ("Password-protected PDFs", "Detected and reported; opening with a password is not offered."),
        ("Searchable PDF output", "Not offered; outputs are DOCX, XLSX, HTML and TXT."),
        ("GPU inference", "Not offered (extension point, see SDD)."),
        ("Installer, auto-update, online activation", "Not offered."),
        ("Undo of edits, re-arranging pages", "Not offered; edits are discarded by recognizing again."),
        ("Recent-files menu", "Opened files are recorded (up to 15) in the settings but not yet displayed."),
    ], [30, 70], "Out-of-scope items")

    # ------------------------------------------------------------------ 3
    d.h1("3. Functional requirements")
    d.p("Priority uses MoSCoW: **Must** (required for release), **Should** (important), **Could** (desirable). Each requirement is given with its statement, the business rules and the acceptance criterion that the test cases of FOCR-TCS-001 verify. A summary of all requirements is given in Appendix A.")
    areas = []
    for r in FR:
        if r[1] not in areas:
            areas.append(r[1])
    for i, area in enumerate(areas, 1):
        d.h2(f"3.{i} {area}")
        d.p(AREA_INTRO[area])
        for r in [x for x in FR if x[1] == area]:
            rules, accept = DETAILS[r[0]]
            d.h3(f"{r[0]} {r[2]} ({r[4]})")
            d.p(r[3])
            d.ul(rules)
            if accept:
                d.p("*Acceptance:* " + accept)
        if area == "User interface":
            d.img("srs-toolbar.png", "Toolbar with the trial notification below it (FR-41)", 620)

    d.h2(f"3.{len(areas) + 1} Supported languages")
    d.table(["Language", "Recognition model", "Default font", "Notes"], [
        ("English", "en_PP-OCRv5_rec_mobile", "Calibri", "Latin letters, digits, punctuation."),
        ("Chinese", "ch_PP-OCRv5_rec_mobile", "Microsoft YaHei", "Simplified and Traditional Chinese, also English; lines joined without spaces."),
        ("Japanese", "ch_PP-OCRv5_rec_mobile", "Yu Gothic", "PP-OCRv5's main model covers Kanji, Hiragana, Katakana; lines joined without spaces."),
        ("Russian", "eslav_PP-OCRv5_rec_mobile", "Calibri", "East Slavic Cyrillic (Russian, Ukrainian, Belarusian)."),
        ("Korean (not offered)", "korean_PP-OCRv5_rec_mobile", "Malgun Gothic", "Bundled but disabled in 1.0 (see section 2.7)."),
    ], [18, 30, 17, 35], "Languages")
    d.p("The detection model ch_PP-OCRv5_det_mobile and the orientation model PP-LCNet_x0_25_textline_ori are shared by all languages. The recognition language also determines the language tag written to exported documents and whether line breaks inside paragraphs are joined with or without a space.")

    d.h2(f"3.{len(areas) + 2} Licensing rules")
    d.table(["Rule", "Specification"], [
        ("Trial length", "7 days counted from the first start of the application on the computer. Day 1 shows '7 of 7 days remaining'; the trial ends when 7 × 24 hours have passed."),
        ("Prompt", "While no valid license is installed, the activation window is shown at every start with Activate, Continue Trial and Exit. In the main window a title-bar badge and a notification below the toolbar show the remaining days."),
        ("Trial over", "After the trial the activation window cannot be skipped: only Activate and Exit remain."),
        ("Tamper protection", "Trial state is stored in the registry and in a file; edited data or a system clock set back more than 24 hours before the last use ends the trial."),
        ("Key content", "Licensee name, serial number, issue date, optional expiry date, optional machine binding."),
        ("Key validation", "Signature (ECDSA P-256/SHA-256) with the public key compiled into the application; machine code; expiry date."),
        ("Expiry", "The expiry date is the last valid day (inclusive). For time-limited keys a clock set back more than one day before the last recorded use is rejected ('The system clock is set earlier than the last use of the application.')."),
        ("Key storage", "%LOCALAPPDATA%\\FalconOCR\\license.key (per user); also read from the application folder and %ProgramData%\\FalconOCR. The first valid key in this order is used."),
        ("Command line", "falcon-ocr follows the same rules; --license <key> activates; --machine-code prints the code."),
    ], [22, 78], "Licensing rules")
    d.img_grid([("activation-trial.png", "Activation window during the trial"), ("activation-expired.png", "Activation window after the trial")], 300)
    d.table(["Validation result", "Message shown to the user"], [
        ("Valid", "Licensed to <name> · serial <n> · valid until <date> / perpetual · any computer / this computer only"),
        ("Missing", "No license key has been entered."),
        ("Malformed", "The license key is not in a valid format. Check that it was copied completely."),
        ("InvalidSignature", "The license key is not genuine."),
        ("WrongMachine", "This license key was issued for a different computer."),
        ("Expired", "The license expired on <yyyy-MM-dd>."),
        ("ClockTampered", "The system clock is set earlier than the last use of the application."),
    ], [25, 75], "License validation results")

    # ------------------------------------------------------------------ 4
    d.h1("4. Use cases")
    d.p("The use cases describe the main workflows from the user's point of view; they connect the requirements of chapter 3 and are the basis of the end-to-end tests.")
    d.table(["ID", "Use case", "Primary actor"], [
        ("UC-01", "Convert a scanned document to Word", "Office user"), ("UC-02", "Convert a digital PDF", "Office user"),
        ("UC-03", "Check and correct the result", "Office user"), ("UC-04", "Convert many files in a batch", "Power user"),
        ("UC-05", "Activate a license", "Any user"),
        ("UC-06", "Issue a license key", "Vendor"), ("UC-07", "Automate conversion from the command line", "Integrator"),
        ("UC-08", "Extract text quickly (Quick OCR)", "Office user"), ("UC-09", "Export a multi-column document", "Office user"),
    ], [12, 58, 30], "Use case overview")

    use_case(d, "UC-01", "Convert a scanned document to Word", "Office user",
             "Licensed or trial; OCR models complete.", "The user has a scanned page (e.g. report.png) to edit in Word.",
             ["The user clicks Add Files (Ctrl+O) or drops the image on the workspace; it appears in Files and Source Page.",
              "The user checks language and document type in OCR Settings.",
              "The user clicks Recognize (F5); the rebuilt page appears in Recognized Text.",
              "The user selects Microsoft Word (.docx) and clicks Export (Ctrl+E); the file is written and opened in Word."],
             ["Sideways or partial page: the user rotates (Ctrl+R) or crops before step 3.",
              "Stop during step 3: recognized pages are kept.",
              "Export fails: the error is shown and no partial file remains."],
             "A .docx exists in the output folder; History records recognition and export.", "FR-02, FR-07 … FR-09, FR-11, FR-14 … FR-20, FR-25, FR-28, FR-31")
    d.img("main-recognized.png", "Result of UC-01: source page and recognized page side by side", 620)

    use_case(d, "UC-02", "Convert a digital PDF", "Office user",
             "As UC-01; PDF input 'Use the PDF text layer when present'.", "The user needs an editable copy of a PDF created by a word processor.",
             ["The user adds the PDF; each page is listed.", "Recognize reads pages with a text layer in well under a second each; scanned pages are recognized with OCR.",
              "The user exports to Word, Excel or HTML."],
             ["Password-protected PDF: 'The PDF is password protected.'.", "Broken text layer: the user selects 'Always run OCR' and recognizes again."],
             "The export reproduces the original text, fonts, sizes and colors.", "FR-01, FR-12, FR-25 … FR-28")

    use_case(d, "UC-03", "Check and correct the result", "Office user",
             "A page is recognized.", "The user wants to fix errors and structure before export.",
             ["The user looks for highlighted uncertain characters; clicking a line shows it in the source image.",
              "The user double-clicks the line (or presses F2), corrects it and presses Enter.",
              "The user changes a paragraph's style, font or size, and checks tables and pictures in Original Image.",
              "The user exports; the corrections are contained in the file."],
             ["Structure completely wrong: the user changes Layout Analysis and recognizes again (manual corrections are discarded)."],
             "The recognition result contains the corrections.", "FR-20 … FR-24, FR-37, FR-45")

    use_case(d, "UC-04", "Convert many files in a batch", "Power user",
             "Licensed or trial; the output folder is writable.", "Files or folders must be converted unattended.",
             ["The user opens Batch Process, adds files and folders and selects format, document type, language and output folder.",
              "The user clicks Start; each item runs through Recognizing…, Page i/n and Done; the log lists each output.",
              "At the end the user may open the output folder."],
             ["An item fails: it is marked Failed with the reason in the log; the batch continues.",
              "Stop: the current item is marked Stopped; a new Start continues with the items not yet Done."],
             "One output file per item; History contains all exports.", "FR-28, FR-30, FR-31")

    use_case(d, "UC-05", "Activate a license", "Any user",
             "The user has a key or .lic file (for bound keys after sending the machine code).", "Activation window, 'Activate now' or Settings → 'Change license key…'.",
             ["The user copies the machine code and sends it to the vendor if a bound key is required.",
              "The user pastes the key or clicks 'Load license file…', then Activate.",
              "'Thank you — Falcon OCR is activated.' is shown; badge and notification disappear."],
             ["Invalid key: the reason (table 'License validation results') is shown in red.", "CLI: falcon-ocr --license <key>."],
             "license.key is stored in %LOCALAPPDATA%\\FalconOCR; later starts show no activation window.", "FR-34, FR-49")

    use_case(d, "UC-06", "Issue a license key", "Vendor",
             "The KeyGen holds the signing key matching the customer's build.", "A customer has ordered a license.",
             ["The KeyGen shows the key fingerprint and '✓ Matches the public key compiled into this Falcon OCR build.'.",
              "The vendor enters licensee, machine code (or 'Any computer'), expiry and an optional note.",
              "'Generate License Key' self-verifies, shows and logs the key; the vendor sends the text or a .lic file."],
             ["No signing key: create one (write the public key to the app, rebuild, back up) or import a backup.",
              "Invalid machine code: 'A machine code has 16 characters (4 groups of 4).'."],
             "The key exists and is recorded in issued.csv.", "FR-36")
    d.img("keygen.png", "KeyGen window", 360)

    use_case(d, "UC-07", "Automate conversion from the command line", "Integrator",
             "The computer is licensed or in the trial.", "A script or scheduled task calls falcon-ocr.",
             ["The script passes inputs, language, formats and output folder.", "Progress goes to standard error, output paths to standard output; exit code 0 means success."],
             ["Not activated: exit code 3.", "Missing models: exit code 2.", "No arguments or -h: usage, exit code 1."],
             "The output files exist (files of the same name are replaced).", "FR-33, FR-42")

    use_case(d, "UC-08", "Extract text quickly (Quick OCR)", "Office user",
             "Licensed or trial; models complete.", "The user needs the text of a screenshot, photo or single page, not a formatted document.",
             ["The user opens OCR in the navigation bar.", "The user presses Ctrl+V, drops an image or clicks 'Open image…' (images or PDF).",
              "The image appears with the detected lines; the text appears in the text box and the info line shows lines, confidence, time and language.",
              "The user corrects the text in the box if needed and clicks 'Copy text'."],
             ["A PDF or multi-page TIFF is opened: only its first page is recognized.", "The user wants structure or export: 'Open in workspace' hands the input over to Home.",
              "Recognition fails: the info line shows 'Recognition failed: <reason>'."],
             "The text is on the clipboard; no file and no history entry is written.", "FR-29")
    d.img("quick-ocr.png", "Quick OCR page", 600)

    use_case(d, "UC-09", "Export a multi-column document", "Office user",
             "A page with a one-column title area, a two-column body and a table (e.g. samples\\report.png) is recognized with Layout Analysis 'Automatic'.", "The user needs an editable Word document with the original columns.",
             ["The user checks in Original Image that both columns, the table and the picture were detected.",
              "The user selects Editable Document and Microsoft Word (.docx) and clicks Export.",
              "The export writes one Word section per band: the title area with one column, the body as a two-column section with a column break, then one column again.",
              "Text flows column by column in reading order; the table and the picture keep their place in the flow."],
             ["The columns must be kept visually but not flowing: the user selects Exact Copy.",
              "One column is wanted: the user selects Layout Analysis 'Single column' and recognizes again.",
              "HTML is chosen: the columns become a CSS grid; Excel: they become worksheet columns."],
             "The Word document has real columns that re-flow when text is edited.", "FR-15, FR-16, FR-25, FR-26, FR-27")
    d.img("main-overlay.png", "Original Image view showing the detected columns, table and picture", 600)

    # ------------------------------------------------------------------ 5
    d.h1("5. Data requirements")
    d.h2("5.1 Data locations")
    d.table(["Data", "Location", "Format"], [
        ("Settings", "%LOCALAPPDATA%\\FalconOCR\\settings.json", "JSON (DataContract), UTF-8"),
        ("History", "%LOCALAPPDATA%\\FalconOCR\\history.json", "JSON, up to 500 entries"),
        ("Error log", "%LOCALAPPDATA%\\FalconOCR\\error.log", "Text, appended"),
        ("Scans", "%LOCALAPPDATA%\\FalconOCR\\scans\\Scan_yyyyMMdd_HHmmss.png", "PNG"),
        ("Activated license", "%LOCALAPPDATA%\\FalconOCR\\license.key", "Key text"),
        ("Deployed license", "<application folder>\\license.key, %ProgramData%\\FalconOCR\\license.key", "Key text"),
        ("License use time", "%LOCALAPPDATA%\\FalconOCR\\license.state", "UTC ticks of the last use"),
        ("Trial state", "HKCU\\Software\\FalconOCR, value EvaluationState; %LOCALAPPDATA%\\FalconOCR\\evaluation.dat (hidden)", "Base64 body + HMAC"),
        ("Exports", "Output folder (default Documents\\Falcon OCR Output)", "DOCX, XLSX, HTML, TXT"),
        ("KeyGen signing key", "%APPDATA%\\FalconOcrKeyGen\\signing.key", "DPAPI-encrypted (current user)"),
        ("KeyGen serial counter / log", "%APPDATA%\\FalconOcrKeyGen\\serial.txt, issued.csv", "Text, CSV (UTF-8)"),
        ("Translations", "<application folder>\\lang\\zh_CN.po, ja.po", "gettext PO (UTF-8)"),
        ("Models", "<application folder>\\models\\det, cls, rec", "ONNX, dictionary text"),
    ], [22, 50, 28], "Data locations")
    d.p("Recognition results and edits exist only in memory for the duration of the session; they are not saved automatically. The user's documents are never copied, uploaded or modified.")
    d.h2("5.2 Application settings")
    d.p("The following fields are stored in settings.json. Missing fields (e.g. in files of older versions) take their default values; an unreadable file is replaced by defaults without an error message.")
    d.table(["Field", "Default", "Meaning / UI"], [
        ("Ocr", "see 5.3", "Recognition options"),
        ("Format", "Docx", "Output format (Docx, Xlsx, Html, Text) — Output Format panel, Settings"),
        ("Mode", "Editable", "Document type (Editable, ExactCopy, PlainText)"),
        ("OutputFolder", "Documents\\Falcon OCR Output", "Target folder of Export"),
        ("OpenAfterExport", "true", "Open the document after export"),
        ("KeepColors", "true", "Keep text, fill and page colors"),
        ("IncludeImages", "true", "Include pictures in exported documents"),
        ("AutoRecognize", "false", "Recognize automatically when files are added"),
        ("ShowConfidence", "true", "Highlight uncertain characters"),
        ("UiLanguage", "null (= Windows language)", "en, zh_CN or ja"),
        ("SidebarCollapsed / FilesPanelCollapsed", "false", "Collapsed state of the navigation bar and the Files / Thumbnails panel"),
        ("RightPanelWidth", "0 (= 340 px)", "Width of the settings column in 96-dpi pixels"),
        ("Maximized / Bounds", "true / empty", "Window state and position (restored only if on a visible screen)"),
        ("RecentFiles", "empty", "Last 15 opened files (recorded only)"),
    ], [30, 22, 48], "settings.json")
    d.h2("5.3 Recognition options")
    d.img("srs-settings-defaults.png", "Settings page with the default values", 460)
    d.table(["Option", "Default", "Range (UI)", "Meaning"], [
        ("Language", "English", "English, Chinese, Japanese, Russian", "Recognition model and export language"),
        ("Layout", "Automatic", "Automatic, Single column, Text lines only", "Layout analysis mode"),
        ("DetectTables", "true", "on/off", "Detect tables and columns"),
        ("DetectFigures", "true", "on/off", "Keep pictures and graphics as images"),
        ("PdfText", "Auto", "Auto, Always OCR", "Use the PDF text layer when present"),
        ("PdfDpi", "200", "100–600, step 25", "PDF rendering resolution"),
        ("DetMaxSide", "2560", "960–6000 px, step 160", "Longest image side fed to the detector"),
        ("DetThreshold", "0.30", "0.05–0.90", "Text pixel threshold"),
        ("BoxThreshold", "0.60", "0.10–0.95", "Text box threshold"),
        ("UnclipRatio", "1.5", "1.0–3.0", "Box expansion"),
        ("UseAngleClassifier", "false", "on/off", "Correct upside-down text lines"),
        ("MinConfidence", "0.50", "0–0.95", "Minimum line confidence"),
        ("Threads", "0", "0–64 (0 = automatic, one per physical core)", "CPU threads of ONNX Runtime"),
        ("RecBatchSize", "8", "— (not in UI)", "Lines per recognizer batch"),
        ("DefaultFont", "null (Automatic)", "Automatic or installed font", "Font for text of unknown font"),
    ], [20, 14, 30, 36], "Recognition options")
    d.h2("5.4 Recognition result")
    d.p("A recognized page holds its size, resolution, text lines (polygon, text, line and character confidence, character positions, style) and blocks in sections (paragraph, heading 1–3, list item, table with cells, merges and fills, figure). Results view and exporters use only this model, independent of the recognition source.")
    d.h2("5.5 License key format")
    d.table(["Part", "Size", "Content"], [
        ("Version", "1 byte", "1"), ("Serial", "4 bytes", "Serial number (KeyGen counter, starting at 1001)"),
        ("Issued", "2 bytes", "Days since 2024-01-01"), ("Expires", "2 bytes", "Days since 2024-01-01; 0 = perpetual"),
        ("Machine", "10 bytes", "Machine hash; all zero = any computer"), ("Name", "1 + ≤ 48 bytes", "Licensee, UTF-8 (trimmed to 48 bytes)"),
        ("Signature", "64 bytes", "ECDSA P-256 / SHA-256 over the payload"),
    ], [18, 18, 64], "License key payload")
    d.p("The text form is 'FOCR-' plus the Crockford Base32 encoding of payload and signature in groups of five; decoding ignores case, spaces, line breaks and hyphens. The machine code is the Base32 form of the first 10 bytes of SHA-256 over the Windows MachineGuid, shown as four groups of four characters.")
    d.h2("5.6 Trial state")
    d.ul(["Record: start and last-seen time (UTC), Base64, plus an HMAC-SHA256 keyed with the machine hash — it cannot be edited or copied to another computer.",
          "The record is written to the registry and to the hidden file at every check; the earliest valid start time of both copies is used.",
          "If only invalid records exist, or the clock is more than 24 hours earlier than the last-seen time, the trial is marked as tampered and ended.",
          "Days left = 7 − whole days used (at least 1 while running); 0 when ended."])
    d.h2("5.7 Vendor data (KeyGen)")
    d.ul(["signing.key is DPAPI-encrypted for the current Windows user; moving it requires a backup file ('Backup key…', --backup).",
          "issued.csv has the columns IssuedAt, Serial, Licensee, MachineCode, Expires, Note, Key; serial.txt holds the last serial."])

    # ------------------------------------------------------------------ 6
    d.h1("6. External interface requirements")
    d.h2("6.1 User interface")
    d.p("The main window shall contain the regions below (details: Screen Design Document FOCR-SCR-001). The interface is available in English, 简体中文 and 日本語.")
    d.table(["Region", "Required content"], [
        ("Title bar", "Logo, 'Falcon OCR', subtitle 'Convert Scans and Images into Editable Documents', trial badge, minimize/maximize/close."),
        ("Navigation bar", "Home, OCR, Batch Process, History, Settings; Collapse button at the bottom."),
        ("Toolbar", "Add Files (with menu: files, image sequence, folder), Scan, From Clipboard, Rotate, Crop, Delete, Recognize/Stop, Export; Settings and Help links."),
        ("Files / Thumbnails", "Document list with page count and state; thumbnails of the pages; Hide panel button."),
        ("Source Page", "Zoom, fit, rotate, hand tool, page navigation; info line with size, dpi, rotation, crop."),
        ("Recognized Text / Original Image", "Formatting bar (style, font, size, B/I/U, lists, More menu); rebuilt page; analysis overlay; result status line."),
        ("OCR Settings / Output Format", "Document type, language, layout analysis, detect tables, Advanced Settings…; Word/Excel/HTML, output folder; Export button."),
        ("Status bar", "Status message with progress; Total Files, Selected, Output format counters."),
    ], [26, 74], "Main window regions")
    d.img("srs-ocr-settings-panel.png", "OCR Settings and Output Format panel", 260)
    d.img_grid([("ui-chinese-settings.png", "Chinese interface"), ("ui-japanese.png", "Japanese interface")], 300)
    d.h2("6.2 Keyboard and mouse")
    d.table(["Input", "Action"], [
        ("Ctrl+O", "Add files"), ("Ctrl+V", "Paste image or files (when no text field has the focus)"), ("F5", "Recognize the current document"),
        ("Ctrl+E", "Export the current document"), ("Ctrl+R", "Rotate the current page 90° clockwise"), ("Del", "Remove the current document or page"),
        ("PgUp / PgDn", "Previous / next page"), ("Ctrl + / Ctrl −, Ctrl+wheel", "Zoom in / out"), ("Middle mouse button, hand tool", "Pan"),
        ("Click / double-click / F2", "Select a line / edit it"), ("Enter (font, size, zoom boxes)", "Apply the typed value"),
    ], [34, 66], "Keyboard and mouse")
    d.h2("6.3 Command-line interface")
    d.code(["falcon-ocr <files|folders...> [-l en|zh|ja|ru] [-f docx,xlsx,html,txt] [-o outDir]",
            "           [--exact | --plain] [--seq] [--no-tables] [--ocr-only] [--dpi N] [--cls] [--font NAME] [--dump]",
            "falcon-ocr --license <KEY>",
            "falcon-ocr --machine-code"])
    d.table(["Option", "Meaning", "Default"], [
        ("files / folders", "Inputs; a folder contributes its supported files (not recursive), each as its own document", "—"),
        ("-l", "Recognition language: en, zh (ch, chinese), ja (jp, japanese), ru (russian); other values → English", "en"),
        ("-f", "Comma-separated formats: docx, xlsx (excel), html (htm), txt (text)", "docx"),
        ("-o", "Output folder (created if necessary)", "folder of the input"),
        ("--exact / --plain", "Exact Copy / Plain Text document type", "Editable"),
        ("--seq", "All inputs form one image sequence", "off"),
        ("--no-tables", "Disable table detection", "on"),
        ("--ocr-only", "Ignore PDF text layers", "Auto"),
        ("--dpi N", "PDF rendering resolution", "200"),
        ("--cls", "Enable the orientation classifier", "off"),
        ("--font NAME", "Default font for text of unknown font", "language font"),
        ("--dump", "Print the reconstructed layout (sections, blocks, tables); without -f nothing is exported", "off"),
        ("--license KEY", "Validate and store the key, then continue with the remaining arguments", "—"),
        ("--machine-code", "Print the machine code and exit", "—"),
    ], [20, 62, 18], "Command-line options")
    d.table(["Exit code", "Meaning"], [
        ("0", "Success (or machine code printed, or activation without further arguments)"),
        ("1", "Usage shown (no arguments, -h, --help)"),
        ("2", "OCR model files missing"),
        ("3", "Not activated: invalid key given or trial ended"),
    ], [15, 85], "CLI exit codes")
    d.p("Output files are named after the input and replace existing files of the same name.")
    d.h2("6.4 KeyGen command line")
    d.table(["Command", "Effect"], [
        ("--init [--force] [--public-cs path]", "Create a signing key, write LicensePublicKey.cs; refuses to replace an existing key without --force (exit 2)."),
        ("--backup file / --import file", "Export the signing key to a backup file / import a backup."),
        ("--generate --name N [--machine CODE] [--days N | --expires yyyy-MM-dd] [--serial N] [--note text]", "Issue a key, log it and print it."),
        ("--verify KEY", "Print status and details; exit 0 if valid, 3 otherwise."),
        ("--machine-code", "Print this computer's machine code."),
    ], [45, 55], "KeyGen command line")
    d.h2("6.5 Software interfaces")
    d.table(["Interface", "Use"], [
        ("ONNX Runtime 1.22 (onnxruntime.dll)", "Inference of detection, orientation and recognition models on the CPU."),
        ("PDFium (pdfium.dll)", "Rendering PDF pages, reading the text layer with fonts, sizes and colors."),
        ("OpenXML SDK 2.20", "Writing .docx and .xlsx files."),
        ("WIA (Windows Image Acquisition)", "Scanner access through late-bound COM."),
        ("GDI+ (System.Drawing)", "Image decoding (all raster formats, EMF/WMF), text measurement for Exact Copy, installed font list."),
        ("Windows CNG / DPAPI", "ECDSA signature verification (application) and signing-key protection (KeyGen)."),
        ("File system / registry", "Settings, history, license and trial state in the user profile."),
    ], [35, 65], "Software interfaces")
    d.h2("6.6 File formats")
    d.table(["Direction", "Formats"], [
        ("Input", "PDF; PNG, JPG/JPEG/JPE, BMP/DIB, GIF, TIF/TIFF (multi-page), ICO, EMF, WMF"),
        ("Output", "DOCX (Word 2007+), XLSX (Excel 2007+), HTML5 (self-contained), TXT (UTF-8)"),
        ("License", "Key text (FOCR-…) or .lic file containing the key"),
        ("Translations", "gettext PO (UTF-8): lang\\zh_CN.po, lang\\ja.po; template lang\\falcon-ocr.pot"),
    ], [20, 80], "File formats")
    d.p("The text format is UTF-8 with byte order mark; pages are separated by a form feed. A .lic file written by the KeyGen contains the lines 'Falcon OCR license', 'Licensee: …', 'Issued: …', a blank line and the key; the application reads the first text starting with 'FOCR-' up to the next blank line.")
    d.img_grid([("export-word.png", "Word export (Editable)"), ("export-excel.png", "Excel export")], 280)
    d.h2("6.7 Export fidelity")
    d.p("The following table specifies which properties of the recognized page each output keeps. Excel ignores the document type; Plain Text keeps only the text in every format.")
    d.table(["Property", "DOCX Editable", "DOCX Exact Copy", "XLSX", "HTML Editable", "HTML Exact Copy"], [
        ("Page size", "Source page size, landscape if wider than high", "As Editable", "One worksheet per page, margins 0.5 in", "One page box per page (source size), page break when printed", "As HTML Editable"),
        ("Reading order", "Sections, columns, lines", "By position", "Top to bottom on a grid", "As DOCX Editable", "By position"),
        ("Columns", "Word section columns with column breaks", "Positions", "Column grid from section edges", "CSS grid", "Positions"),
        ("Headings", "Styles Heading 1–3", "Size and weight", "Size and weight", "h1–h3", "Size and weight"),
        ("Lists", "Word bullets and numbering", "Marker as text (drawn bullets as •)", "Marker as text", "ul / ol", "Marker as text"),
        ("Tables", "Word table, merges, fills, borders", "Positioned table", "Cell grid, merges, borders, fills, numbers", "table with spans", "Positioned table"),
        ("Pictures", "Inline PNG", "Positioned PNG", "Not exported", "figure with embedded PNG", "Positioned embedded PNG"),
        ("Font, size, B/I/U", "Kept", "Kept, width-fitted 40–250 %", "Kept", "Kept (CSS)", "Kept (CSS)"),
        ("Text and fill colors", "Kept if 'Keep colors'", "Kept if 'Keep colors'", "Kept if 'Keep colors'", "Kept if 'Keep colors'", "Kept, incl. page background"),
    ], [15, 19, 17, 17, 16, 16], "Export fidelity by format and document type")
    d.ul(["'Include pictures in exported documents' off removes pictures from DOCX and HTML; 'Keep text, fill and page colors' off writes black text without fills.",
          "In flowing text a word hyphenated at the end of a line is joined with the next line when that line starts with a lower-case letter; Chinese and Japanese lines are joined without a space.",
          "Word documents carry the language tag of the recognition language; HTML carries the lang attribute (en, zh, ja, ru) and is written as UTF-8.",
          "Plain Text: DOCX paragraphs only, HTML preformatted text, TXT UTF-8 with byte order mark and a form feed between pages."])
    d.h2("6.8 Translation interface")
    d.ul(["Catalogs are standard gettext PO files: msgid is the English text, msgstr the translation; multi-line strings and C escapes are supported; comments and msgctxt are ignored.",
          "Placeholders {0}, {1} … must be kept; python tools/i18n/po_tool.py check reports untranslated entries and placeholder mismatches.",
          "A new language requires a new lang\\<code>.po file and an entry in L.Languages (rebuild)."])

    # ------------------------------------------------------------------ 7
    d.h1("7. Non-functional requirements")
    d.table(["ID", "Category", "Requirement"], NFR, [10, 16, 74], "Non-functional requirements")
    d.h2("7.1 Performance")
    d.table(["Measure", "Requirement", "Measured / verified"], [
        ("Dense A4 scan, 200 dpi, 4-core CPU", "≤ 10 s per page", "5.0–6.2 s"),
        ("PDF page with text layer", "≤ 1 s per page", "0.3 s"),
        ("First recognition after start", "No model-loading delay once the background warm-up has finished", "Warm-up at start-up"),
        ("CPU threads", "Automatic = one per physical core (hyper-threaded logical cores would slow recognition down)", "Threads = 0"),
    ], [30, 40, 30], "Performance requirements")
    d.h2("7.2 Other quality attributes")
    d.table(["Attribute", "Required characteristics"], [
        ("Reliability", "Unhandled exceptions are logged to error.log and reported; settings and history are written via a temporary file; a failed page or batch item never ends the session; closing during recognition asks for confirmation."),
        ("Security", "No network access or telemetry; only the public key is shipped; the KeyGen is never distributed. The license threat model covers casual misuse, not binary patching."),
        ("Usability", "Three steps (add, recognize, export) convert a page; the Help dialog summarises workflow and shortcuts; progress is always visible; the UI scales with the display DPI."),
        ("Localization", "All interface texts (about 290 catalog entries) exist in Chinese and Japanese; the recognition language is independent of the interface language."),
        ("Maintainability", "The Core library has no UI dependency and is tested through the CLI; extension points are described in the SDD."),
    ], [24, 76], "Quality attributes")

    # ------------------------------------------------------------------ 8
    d.h1("8. Error handling requirements")
    d.p("Errors shall be reported in the user's language, name the affected file or object and leave the workspace consistent. Confirmations offer Cancel and never delete files on disk; informational results go to the status bar. Required conditions and messages:")
    d.table(["Condition", "Required behavior / message"], [
        ("Unsupported file type", "'Some files could not be opened:' with 'file: unsupported file type'; other files are added."),
        ("Password-protected PDF", "'The PDF is password protected.' in the same summary."),
        ("Clipboard without image", "'The clipboard does not contain an image.' (information)."),
        ("WIA not installed", "'Windows Image Acquisition (WIA) is not available on this computer.'"),
        ("No scanner connected", "'No scanner was found. Connect a WIA-compatible scanner and try again.'"),
        ("Model files missing at start", "Warning listing the files and the hint to run tools\\fetch-dependencies.ps1 and rebuild."),
        ("Model files missing before recognition", "'OCR models are missing:' with the files; recognition does not start."),
        ("Recognition error", "Status 'Recognition failed.' and an error message; recognized pages are kept."),
        ("Page rotated/cropped during recognition", "The stale result is discarded silently."),
        ("Export without document", "'Add and recognize a document first.'"),
        ("Output folder cannot be created", "'Cannot create the output folder: <reason>'."),
        ("Export error", "Status 'Export failed.' and the error; no partial file remains."),
        ("Batch item fails", "Item 'Failed', log line '✗ path: reason'; the batch continues."),
        ("32-bit process", "'Falcon OCR must run as a 64-bit process.'; the application ends."),
        ("Unexpected exception", "'An unexpected error occurred: … Details were written to: <error.log>'."),
        ("Invalid license key", "Reason from the table 'License validation results', shown in red in the activation window."),
        ("KeyGen signing key unreadable", "'The stored signing key cannot be opened (it is encrypted for the Windows user that created it) … Import a backup of the key instead.'"),
    ], [32, 68], "Error conditions")

    # ------------------------------------------------------------------ 9
    d.h1("9. Constraints on verification")
    d.p("Each requirement is verified by one of the following methods; the test cases are specified in FOCR-TCS-001.")
    d.table(["Method", "Applied to"], [
        ("Test (GUI)", "Workflows, dialogs, messages and settings persistence in the desktop application."),
        ("Test (CLI)", "Recognition, layout and export on the sample documents (samples\\), licensing and exit codes; --dump for the layout."),
        ("Inspection", "Exported files opened in Word, Excel and a browser; data files in the profile; build output."),
        ("Measurement", "Recognition times (NFR-03), accuracy on the sample set (NFR-04), distribution size (NFR-09)."),
        ("Analysis / review", "Security, compliance and maintainability requirements (NFR-07, NFR-08, NFR-10)."),
    ], [22, 78], "Verification methods")
    d.p("The sample set (tools/make_samples.py) contains report.png, crop.png, language samples, an image sequence folder, multi_scan.pdf and text_layer.pdf.")

    # ------------------------------------------------------------------ 10
    d.h1("10. Acceptance criteria")
    d.ul(["All Must requirements are implemented and their test cases in FOCR-TCS-001 pass.",
          "The solution builds offline from a clean copy of the repository with build.ps1.",
          "The sample documents in samples\\ are reproduced with correct text, headings, list items, table grid, columns, pictures and colors in Word, Excel and HTML.",
          "A key generated by the KeyGen activates the application; keys for other computers, expired, altered or malformed keys are rejected; the trial behaves as specified in section 3.",
          "The performance figures of NFR-03 are met on the reference computer.",
          "The interface is complete in English, Chinese and Japanese (po_tool check reports no untranslated entries)."])

    d.h1("Appendix A. Requirements summary")
    d.table(["ID", "Area", "Requirement", "Priority"], [(r[0], r[1], r[2], r[4]) for r in FR], [10, 18, 57, 15], "All functional requirements")

    d.h1("Appendix B. Requirements change log")
    d.p("Changes to the requirement baseline (all dated 2026-09-24 unless stated otherwise).")
    d.table(["Doc. version", "Change", "Requirements"], [
        ("0.9 (2026-09-20)", "Draft for internal review.", "—"),
        ("1.0", "Baseline released with Falcon OCR 1.0: OCR engine, layout reconstruction, exports, tools, trial and licensing with vendor-signed keys and KeyGen.", "FR-01 … FR-36, NFR-01 … NFR-10"),
        ("1.1", "User-interface extensions: font and size combo boxes, English/Chinese/Japanese interface via gettext PO files, collapsible sidebar, resizable settings panel, trial notification below the toolbar.", "FR-37 … FR-41 added"),
        ("1.2", "New application icon and removal of the OCR model information from Settings; default font setting (also CLI --font); collapsible Files / Thumbnails panel.", "FR-42 … FR-44 added"),
        ("1.2", "Specification detailed: business rules and acceptance criteria per requirement; use case, data, interface, export fidelity and error handling chapters; existing behavior documented as new requirements.", "FR-45 … FR-51, NFR-11 … NFR-13 added"),
        ("1.2", "Korean recognition disabled in the product (model still bundled); requirement and language table corrected.", "FR-11 amended"),
    ], [16, 62, 22], "Requirements change log")
    d.p("Identifiers are never reused or renumbered; an amended requirement keeps its identifier, and a withdrawn requirement stays in the catalogue marked as withdrawn.")

    return d
