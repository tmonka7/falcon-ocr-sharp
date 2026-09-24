from docbuilder import Doc

PASS = "<span class='pass'>Pass</span>"
NOTRUN = "<span class='notrun'>Not run</span>"
FAIL = "<span class='fail'>Fail</span>"

# Builds used for the executions recorded in this document (see section 3.3).
A = "A"   # pre-65ea0df build (previous dist\, Korean enabled, old public key)
B = "B"   # 65ea0df build (current dist\; Korean disabled, new public key)

# id, requirement(s), title, preconditions, steps, expected, status, method/evidence
CASES = [
    # ---------------------------------------------------------------- build & installation
    ("TC-001", "NFR-02", "Offline build from a clean copy", "Copy of the repository without bin/obj/.tmp; empty NuGet cache; network blocked (proxy 127.0.0.1:9).",
     "Run build.ps1.", "Restore uses only packages\\; build succeeds with 0 warnings and 0 errors; dist\\ is created.", PASS, "Automated script, 2026-09-24"),
    ("TC-002", "NFR-09", "Standalone deployment", "dist\\ copied to a different folder (no installer).",
     "Run the copied falcon-ocr.exe on samples\\japanese.png -l ja -f docx.", "Models and native DLLs found next to the executable; text recognized; DOCX written.", PASS,
     "CLI, 2026-09-24 (re-run with japanese.png on a copy of the build A folder after Korean was disabled; the first run used korean.png)"),
    ("TC-003", "NFR-06, FR-47", "Missing model files", "Rename models\\det in the application folder.",
     "Start the application.", "Warning lists the missing files; recognition is refused with a message.", NOTRUN, "Manual"),
    ("TC-004", "NFR-09", "Application folder content", "Application folder of build A.", "Measure the folder; list models\\.",
     "≈ 78 MB; FalconOcr.exe, falcon-ocr.exe, models\\det, models\\cls and models\\rec with recognizers and dictionaries.", PASS, f"Script ({A}): 79 MB, 4 recognizers + 4 dictionaries"),
    ("TC-005", "FR-47", "CLI — model folder missing", "Copy of dist\\ without models\\.", "falcon-ocr samples\\chinese.png -l zh.",
     "Exit code 2; 'Missing model files:' names det\\ch_PP-OCRv5_det_mobile.onnx, the cls model, rec\\ch_PP-OCRv5_rec_mobile.onnx and rec\\ppocrv5_dict.txt.", PASS, f"CLI ({A})"),
    ("TC-006", "FR-47", "CLI — single model file missing", "Copy with only models\\det and the English recognizer.", "Run with -l en, then with -l zh.",
     "Exit code 2 each time; only the files needed for the language are listed (en: the cls model; zh: cls model, Chinese recognizer, dictionary).", PASS,
     f"CLI ({A}); the orientation model is required even without --cls"),
    ("TC-007", "NFR-08", "UI-independent core", "Build A.", "List the referenced assemblies of falcon-ocr.exe and FalconOcr.Core.dll.",
     "Neither references System.Windows.Forms; the CLI uses only FalconOcr.Core and base libraries.", PASS, f"Reflection script ({A})"),
    ("TC-008", "NFR-02, NFR-07", "No network access at run time", "Firewall logging or Process Monitor active.",
     "Recognize and export a PDF in the application and with the CLI.", "No network connection is opened by any Falcon OCR process.", NOTRUN, "Manual"),
    ("TC-009", "NFR-10", "Third-party licenses", "Release folder.", "Compare the bundled components with their licenses.",
     "PaddleOCR models (Apache-2.0), ONNX Runtime (MIT), PDFium, OpenXML SDK (MIT), VC++ runtime; notices shipped.", NOTRUN, "Review"),
    # ---------------------------------------------------------------- input
    ("TC-010", "FR-02", "Open a single image", "Application started.", "Add Files → samples\\report.png.",
     "Document 'report.png · 1 page' appears; page shown in Source Page with '1654 × 2338 px · 200 dpi'.", PASS, "Snapshot tour (UI), 2026-09-24"),
    ("TC-011", "FR-01, FR-12", "Open a PDF with text layer", "—", "Recognize samples\\text_layer.pdf.",
     "Lines taken from the text layer (status 'Text layer read from PDF'); font sizes 22/11/14/10 pt, heading color #126E48, bold headings.", PASS, "CLI --dump, 2026-09-24"),
    ("TC-012", "FR-01", "Open a scanned multi-page PDF", "—", "Recognize samples\\multi_scan.pdf (3 pages).",
     "3 pages OCR'd (no text layer); export contains 3 pages.", PASS, "API + Word render, 2026-09-24"),
    ("TC-013", "FR-04", "Image sequence", "—", "falcon-ocr samples\\sequence --seq, or Add Files → Add folder as image sequence.",
     "One document with page1, page2, page3 in natural order.", PASS, "CLI --seq, 2026-09-24"),
    ("TC-014", "FR-03", "Multi-page TIFF", "A 3-frame TIFF.", "Add the TIFF.", "One document with 3 pages.", NOTRUN, "Manual (CLI variant: TC-144)"),
    ("TC-015", "FR-05", "Paste image from clipboard", "Screenshot in the clipboard.", "Click From Clipboard (or Ctrl+V).",
     "Document 'Clipboard_01' is added and shown.", NOTRUN, "Manual"),
    ("TC-016", "FR-06", "Scan a page", "WIA scanner connected.", "Click Scan, scan a page.",
     "Scanned page added as a document; without scanner: 'No scanner was found…'.", NOTRUN, "Manual (no scanner available)"),
    ("TC-017", "FR-07", "Drag and drop", "—", "Drop two PDFs and a folder of images onto the workspace.",
     "Two PDF documents and one image-sequence document are added.", NOTRUN, "Manual"),
    ("TC-018", "FR-01", "Unsupported / damaged file", "A .docx and a truncated PDF.", "Add both.",
     "One message 'Some files could not be opened' listing both; the application continues.", NOTRUN, "Manual"),
    # ---------------------------------------------------------------- page editing
    ("TC-020", "FR-08", "Rotate a page", "text_layer.pdf loaded.", "Rotate 90°, recognize.",
     "Page is 2339 × 1653 px; all text-layer lines re-mapped inside the rotated page.", PASS, "API test, 2026-09-24"),
    ("TC-021", "FR-09", "Crop a page", "text_layer.pdf loaded.", "Crop to the top 25 %, recognize.",
     "Page 1653 × 585 px; only lines inside the crop are kept.", PASS, "API test, 2026-09-24"),
    ("TC-022", "FR-09", "Crop in the viewer", "Image loaded.", "Crop → drag a rectangle; then Crop again → No.",
     "Page shows only the selected area ('cropped' in footer); No restores the full page.", NOTRUN, "Manual"),
    ("TC-023", "FR-10", "Delete document / page", "Recognized document with 3 pages.", "Thumbnails → select page 2 → Delete; Files → Delete.",
     "Page 2 removed after confirmation; document removed after confirmation.", NOTRUN, "Manual"),
    # ---------------------------------------------------------------- recognition
    ("TC-030", "FR-11", "English recognition", "—", "Recognize samples\\report.png (English).", "All 54 lines correct.", PASS, "CLI --dump, 2026-09-24"),
    ("TC-031", "FR-11", "Chinese recognition", "—", "Recognize samples\\chinese.png (Chinese).", "Heading 智能文字识别 and both sentences correct.", PASS, "CLI, 2026-09-24"),
    ("TC-032", "FR-11", "Japanese recognition", "—", "Recognize samples\\japanese.png (Japanese).", "光学文字認識 and both sentences correct.", PASS, "CLI, 2026-09-24"),
    ("TC-033", "FR-11", "Korean not offered", "Build of the current source (commit 65ea0df).", "falcon-ocr samples\\korean.png -l ko (also -l kr, -l korean) -f txt,docx.",
     "The English model is used: no Hangul in the output (Latin fragments only), DOCX language en-US and font Calibri; exit code 0. Korean model files remain in models\\rec.", PASS,
     f"CLI ({B}); re-scoped, Korean disabled in 65ea0df"),
    ("TC-034", "FR-11", "Russian recognition", "—", "Recognize samples\\russian.png (Russian).", "Оптическое распознавание and both sentences correct.", PASS, "CLI, 2026-09-24"),
    ("TC-035", "FR-12", "Force OCR on a text PDF", "Advanced: Always run OCR.", "Recognize text_layer.pdf.", "Status shows OCR (not text layer); text still correct.", NOTRUN, "Manual (CLI variant: TC-163)"),
    ("TC-036", "FR-14", "Stop recognition", "Large PDF (≥ 20 pages).", "Recognize, then click Stop.", "'Recognition stopped.'; finished pages keep results.", NOTRUN, "Manual"),
    ("TC-037", "NFR-03", "Performance", "Release build, 4-core CPU.", "Recognize samples\\report.png twice.",
     "≤ 10 s per page (measured 5.0–6.2 s); text-layer page ≤ 1 s (0.3 s).", PASS, "Timing script, 2026-09-24"),
    # ---------------------------------------------------------------- layout
    ("TC-040", "FR-16", "Headings", "report.png recognized.", "Inspect blocks (CLI --dump) / Word export.",
     "'Quarterly Operations Report' Heading 1 (24.5 pt, green); '1. Key Features', '2. Results by Region', '3. Discussion' Heading 2 (16 pt, bold).", PASS, "CLI + Word, 2026-09-24"),
    ("TC-041", "FR-16", "Graphic bullets", "report.png recognized.", "Inspect list items.", "Four list items with '•' although the bullets are drawn dots.", PASS, "CLI + Word, 2026-09-24"),
    ("TC-042", "FR-16", "Paragraph separation", "report.png recognized.", "Inspect the left column of 'Discussion'.", "Two paragraphs, as in the original.", PASS, "CLI, 2026-09-24"),
    ("TC-043", "FR-17", "Ruled table", "report.png recognized.", "Inspect the table.", "5 × 4 grid, all cells correct, header fill kept, header row bold only.", PASS, "CLI + Word/Excel/HTML, 2026-09-24"),
    ("TC-044", "FR-15", "Two columns", "report.png recognized.", "Inspect the 'Discussion' section.", "Section with 2 columns; reading order left column then right column.", PASS, "CLI + Word, 2026-09-24"),
    ("TC-045", "FR-18", "Picture", "report.png recognized.", "Inspect figures.", "Blue/yellow graphic kept as picture in the right column.", PASS, "CLI + Word, 2026-09-24"),
    ("TC-046", "FR-19", "Colors and sizes", "report.png recognized.", "Inspect styles.", "Title green #146F49, subtitle grey, contact line red; body 11.5 pt, no false bold.", PASS, "CLI --dump, 2026-09-24"),
    ("TC-047", "FR-17", "Borderless table", "Page with an unruled 3-column price list.", "Recognize.", "Recognized as a table without borders.", NOTRUN, "Manual"),
    ("TC-048", "FR-18", "UI screenshot / text panels", "Screenshot of the reference UI.", "Recognize.",
     "Text inside panels stays text (no page-size 'figure'); no false tables from colored fills.", PASS, "CLI, 2026-09-24"),
    # ---------------------------------------------------------------- results view
    ("TC-050", "FR-20", "Aligned side-by-side view", "report.png recognized.", "Compare Source Page and Recognized Text at Fit width.",
     "Identical page geometry; every line at the same position on both sides.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-051", "FR-21", "Selection synchronisation", "report.png recognized.", "Select '2. Results by Region'.",
     "Line highlighted in both views; style box shows Heading 2 and B is active.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-052", "FR-21", "Original Image overlay", "report.png recognized.", "Open the Original Image tab.", "Lines green, table blue, picture orange dashed.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-053", "FR-22", "Edit a line", "Recognized page.", "Double-click a line, change the text, Enter; export.",
     "Line shows the new text with dashed underline; export contains the corrected text.", NOTRUN, "Manual"),
    ("TC-054", "FR-23", "Change paragraph style", "Recognized page.", "Select a paragraph line → Heading 1; export to Word.",
     "Paragraph exported with Heading 1 style.", NOTRUN, "Manual"),
    ("TC-055", "FR-20", "Zoom and scroll sync", "Recognized page.", "Zoom to 200 %, scroll in either view.", "The other view follows zoom and scroll.", NOTRUN, "Manual"),
    ("TC-056", "FR-24", "Copy text", "Recognized document.", "⋯ → Copy document text; paste in Notepad.", "Plain text in reading order.", NOTRUN, "Manual"),
    # ---------------------------------------------------------------- export
    ("TC-060", "FR-25", "Word — Editable Document", "report.png recognized.", "Export DOCX; open in Word.",
     "Opens without repair; headings, bullets, table with fill, two columns, picture and colors as in the original.", PASS, "Word COM render, 2026-09-24"),
    ("TC-061", "FR-25", "Word — Exact Copy", "report.png recognized.", "Export DOCX with Exact Copy.", "Lines, bullets, table and picture at original positions.", PASS, "Word COM render, 2026-09-24"),
    ("TC-062", "FR-26", "Excel export", "report.png recognized.", "Export XLSX; open in Excel.",
     "Opens without repair; table grid with borders/fill; numbers are numeric; paragraphs wrapped within page width; columns side by side.", PASS, "Excel COM render, 2026-09-24"),
    ("TC-063", "FR-27", "HTML export", "report.png recognized.", "Export HTML; open in Edge.", "Self-contained page reproducing the layout.", PASS, "Edge headless, 2026-09-24"),
    ("TC-064", "FR-25", "Multi-page export", "multi_scan.pdf recognized.", "Export DOCX and XLSX.", "Word: 3 pages; Excel: 3 worksheets.", PASS, "Word/Excel COM, 2026-09-24"),
    ("TC-065", "FR-28", "Unique file names", "Output folder contains report.docx.", "Export report again.", "report (2).docx created; nothing overwritten.", NOTRUN, "Manual"),
    ("TC-066", "FR-25", "Plain text export", "Recognized document.", "Export as text (falcon-ocr … -f txt).", "UTF-8 text file with page separators.", PASS, "API (Exporter, multi_scan.pdf), 2026-09-24"),
    # ---------------------------------------------------------------- tools
    ("TC-070", "FR-29", "Quick OCR", "—", "OCR page → Paste an image.", "Text appears with lines/confidence/time.", NOTRUN, "Manual"),
    ("TC-071", "FR-30", "Batch processing", "Folder with 3 PDFs.", "Batch → Add folder → Start.", "3 outputs; status Done; log lines; history entries.", NOTRUN, "Manual"),
    ("TC-072", "FR-31", "History", "One recognition performed.", "Open History.", "Entry with time, source, format 'Recognition', pages, language, duration.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-073", "FR-32", "Settings persistence", "—", "Change language and output folder, restart.", "Values restored after restart.", PASS, "Observed (settings.json), 2026-09-24"),
    ("TC-074", "FR-33", "Command line", "—", "falcon-ocr samples\\report.png -f docx,xlsx,html -o out.", "Three files written; paths printed.", PASS, "CLI, 2026-09-24"),
    # ---------------------------------------------------------------- licensing
    ("TC-080", "FR-34", "No license, no trial", "No license.key; trial expired.", "falcon-ocr samples\\russian.png.", "Exit code 3, 'not activated', machine code printed.", PASS, "CLI, 2026-09-24 (run before the trial existed — same code path as an expired trial)"),
    ("TC-081", "FR-34", "Key for this computer", "Key generated with this machine code.", "Activate.", "'Licensed to … · this computer only'; application runs.", PASS, "CLI, 2026-09-24"),
    ("TC-082", "FR-34", "Key for another computer", "Key with another machine code.", "Activate.", "'This license key was issued for a different computer.'", PASS, "CLI, 2026-09-24"),
    ("TC-083", "FR-34", "Expired key", "Key with expiry 2025-01-31.", "Activate.", "'The license expired on 2025-01-31.'", PASS, "CLI, 2026-09-24"),
    ("TC-084", "FR-34", "Tampered key", "One character of a valid key changed.", "Activate.", "'The license key is not genuine.'", PASS, "CLI, 2026-09-24"),
    ("TC-085", "FR-34", "Malformed key", "'hello-world'.", "Activate.", "'The license key is not in a valid format…'", PASS, "CLI, 2026-09-24"),
    ("TC-086", "FR-34", "Unbound time-limited key", "Key 'Any computer', 365 days.", "Activate.", "'valid until … · any computer'.", PASS, "CLI, 2026-09-24"),
    ("TC-087", "FR-34", "Licensed start-up", "Valid key installed.", "Start the application.", "Main window without trial badge; no activation window.", PASS, "Snapshot tour, 2026-09-24 (build before the trial feature; re-run with a key of the current signing key)"),
    ("TC-090", "FR-35", "Trial — first start", "No license, no trial data.", "Start.", "Activation window: 'Trial version — 7 of 7 days remaining'; Continue Trial available.", PASS, "Unit test + UI render, 2026-09-24"),
    ("TC-091", "FR-35", "Trial — day 4 / last day", "Trial started 3.2 / 6.9 days ago.", "Start.", "4 / 1 days remaining.", PASS, "Unit test (simulated clock), 2026-09-24"),
    ("TC-092", "FR-35", "Trial — expired", "Trial started 7.1 days ago.", "Start.", "Activation window without Continue Trial; Exit closes the application.", PASS, "Unit test + UI render, 2026-09-24"),
    ("TC-093", "FR-35", "Trial — clock turned back", "Trial used at day 3.", "Set the clock 2 days back, start.", "Trial ended (tampered).", PASS, "Unit test, 2026-09-24"),
    ("TC-094", "FR-35", "Trial — edited record", "Trial running.", "Edit the start date in the registry and file.", "Trial ended (tampered).", PASS, "Unit test, 2026-09-24"),
    ("TC-095", "FR-35", "Trial — one copy deleted", "Trial at day 1.", "Delete the registry value, check at day 3.", "Trial continues from the original start (5 days left).", PASS, "Unit test, 2026-09-24"),
    ("TC-096", "FR-35", "Trial badge and activation", "Trial running.", "Click 'TRIAL · n days left — Activate now', enter a valid key.", "Badge disappears; Settings → License shows the license.", NOTRUN, "Manual (badge display verified by snapshot)"),
    ("TC-097", "FR-35", "CLI in trial", "Trial running.", "falcon-ocr samples\\russian.png -l ru.", "Warning 'Trial version — 7 of 7 days remaining …'; recognition runs.", PASS, "CLI, 2026-09-24"),
    ("TC-100", "FR-36", "KeyGen — create signing key", "No signing key.", "FalconOcrKeyGen --init.", "Key stored (DPAPI); LicensePublicKey.cs written; fingerprint printed.", PASS, "CLI, 2026-09-24"),
    ("TC-101", "FR-36", "KeyGen — generate and verify", "Signing key present.", "--generate --name … --machine …; --verify <key>.", "Key FOCR-… (≈ 190 chars); verify reports Valid with licensee/serial; logged in issued.csv.", PASS, "CLI, 2026-09-24"),
    ("TC-102", "FR-36", "KeyGen — build mismatch warning", "Application built with another public key.", "Open the KeyGen.", "'⚠ Does not match the public key compiled into this build'.", NOTRUN, "Manual"),
    ("TC-103", "FR-36", "KeyGen — backup and import", "Signing key present.", "Backup key…, then on another account Import backup…", "Imported key has the same fingerprint and matches the build.", NOTRUN, "Manual"),
    # ---------------------------------------------------------------- user interface extensions (v1.1)
    ("TC-110", "FR-37", "Font boxes show the selection", "report.png recognized.", "Select '2. Results by Region'.",
     "Style box 'Heading 2', font box 'Calibri', size box '15.5'.", PASS, "Snapshot tour (en, ja), 2026-09-24"),
    ("TC-111", "FR-37", "Font change is exported", "report.png recognized.", "Set the heading paragraph to Georgia 30 pt (same model change as the font boxes); export DOCX and HTML.",
     "document.xml contains a Georgia run with w:sz=60; HTML contains font-size:30pt with font-family 'Georgia'.", PASS, "API script, 2026-09-24"),
    ("TC-112", "FR-37", "Font boxes — interactive", "Recognized page, line selected.", "Pick 'Times New Roman' in the font box, type 20 + Enter in the size box.",
     "Recognized Text shows the paragraph in Times New Roman 20 pt; status bar confirms.", NOTRUN, "Manual"),
    ("TC-113", "FR-38", "Japanese interface", "Settings → Interface language = 日本語.", "Start the application; open every page.",
     "All menus, buttons, tabs, messages, trial texts in Japanese; Yu Gothic UI font.", PASS, "Snapshot tour (ja), 2026-09-24"),
    ("TC-114", "FR-38", "Chinese interface", "Interface language = 简体中文.", "Start the application; open every page.",
     "All texts in Simplified Chinese; Microsoft YaHei UI font; Settings shows 常规 / 界面语言.", PASS, "Snapshot tour (zh_CN), 2026-09-24"),
    ("TC-115", "FR-38", "Catalog completeness", "—", "python tools\\i18n\\po_tool.py check.",
     "zh_CN and ja: 286/286 translated, 0 placeholder mismatches.", PASS, "po_tool, 2026-09-24"),
    ("TC-116", "FR-38", "PO parsing", "—", "Read lang\\ja.po with FalconOcr.Localization.PoFile.",
     "286 entries; multi-line help text, tab (\\t) and backslash keys resolve.", PASS, "API script, 2026-09-24"),
    ("TC-117", "FR-38", "Switch language", "English interface.", "Settings → Interface language = 日本語 → Save → Yes (restart).",
     "Application restarts in Japanese; setting persists.", NOTRUN, "Manual"),
    ("TC-118", "FR-39", "Collapsed sidebar at start-up", "SidebarCollapsed = true in settings.", "Start the application.",
     "Navigation shows icons only (64 px); » button at the bottom.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-119", "FR-39", "Collapse / expand by click", "Sidebar expanded.", "Click « Collapse, then », restart.",
     "Sidebar collapses and expands; last state restored after restart; tooltips show page names when collapsed.", NOTRUN, "Manual"),
    ("TC-120", "FR-40", "Stored panel width applied", "RightPanelWidth = 420 in settings.", "Start the application.",
     "Settings column is 420 px wide; centre area shrinks accordingly.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-121", "FR-40", "Resize by dragging", "Workspace open.", "Drag the grip at the left edge of the settings column; restart.",
     "Width follows the mouse within 280–640 px (centre ≥ 560 px); width restored after restart.", NOTRUN, "Manual"),
    ("TC-122", "FR-41", "Trial notification", "Trial running.", "Start → Continue Trial.",
     "Amber banner below the toolbar: 'You are using the trial version of Falcon OCR — 7 of 7 days left…', Activate now, ✕.", PASS, "Snapshot tour (en, ja, zh), 2026-09-24"),
    ("TC-123", "FR-41", "Hide / activate from the notification", "Trial running.", "Click ✕; restart; click Activate now and enter a valid key.",
     "✕ hides the banner until the next start; after activation banner and title-bar badge disappear.", NOTRUN, "Manual"),
    ("TC-124", "FR-41", "No notification when licensed", "Valid license installed.", "Start the application.",
     "No banner below the toolbar, no trial badge.", NOTRUN, "Manual (requires a key of the current signing key)"),
    ("TC-125", "FR-42", "Default font row", "—", "Open Settings.",
     "Recognition section shows 'Default font:' with 'Automatic (by recognition language)' and the installed fonts, plus a hint.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-126", "FR-42", "Default font applied", "Settings → Default font = Georgia → Save.", "Recognize a scanned page; export DOCX, XLSX, HTML.",
     "Results view font combo shows Georgia; exported documents use Georgia for the body text.", NOTRUN, "Manual"),
    ("TC-127", "FR-42", "CLI --font", "—", "falcon-ocr samples\\report.png --font Georgia -f docx,xlsx,html.",
     "DOCX runs, the XLSX font list and the HTML font-family use Georgia.", PASS, f"CLI + XML check ({A}; DOCX also {B})"),
    ("TC-128", "FR-43", "Collapse the Files panel", "Workspace open.", "Click « Hide panel at the bottom of the Files / Thumbnails panel.",
     "Panel shrinks to a 40 px strip with a vertical 'Files / Thumbnails' caption and »; the page viewers widen.", PASS, "Snapshot tour, 2026-09-24"),
    ("TC-129", "FR-43", "Expand and remember", "Files panel collapsed.", "Restart; click ».",
     "Panel still collapsed after restart; » restores the 214 px panel.", NOTRUN, "Manual"),
    ("TC-130", "FR-44", "Icon and no model section", "—", "Start the application and KeyGen; open Settings.",
     "New icon in the title bar, taskbar and Explorer; Settings has no 'OCR models' section.", PASS, "Snapshot tour and build output, 2026-09-24"),
    # ---------------------------------------------------------------- input formats (CLI)
    ("TC-140", "FR-02", "JPEG, BMP and GIF input", "report.png saved as JPEG (q 92), 24-bit BMP and 256-colour GIF.", "falcon-ocr <file> --dump for each file.",
     "1654 × 2338 px page; 16 blocks in 2 sections, as for the PNG.", PASS, f"CLI ({A}): JPEG 55 lines, BMP 54, GIF 55"),
    ("TC-143", "FR-02", "EXIF orientation", "report_exif6.jpg: landscape pixels with EXIF Orientation = 6.", "falcon-ocr report_exif6.jpg --dump.",
     "Page upright (1654 × 2338); title recognized as Heading 1.", PASS, f"CLI ({A})"),
    ("TC-144", "FR-03", "Multi-page TIFF (CLI)", "three_pages.tif (3 LZW frames).", "falcon-ocr three_pages.tif --dump.",
     "One document, pages 'Page 1'–'Page 3', 7 lines each.", PASS, f"CLI ({A}, {B})"),
    ("TC-145", "FR-04", "Natural sort order", "Folder natseq\\ with p1.png, p2.png, p10.png.", "falcon-ocr natseq --seq --dump.",
     "One document; page order p1, p2, p10 (not p1, p10, p2).", PASS, f"CLI ({A})"),
    # ---------------------------------------------------------------- input error handling
    ("TC-150", "FR-01", "Password-protected PDF (CLI)", "password.pdf (RC4, user password).", "falcon-ocr password.pdf -f txt.",
     "Non-zero exit; stderr contains 'The PDF is password protected.'; no output file.", PASS, f"CLI ({A}, {B}); exit code 0xE0434352 on A"),
    ("TC-151", "FR-01", "Password-protected PDF (UI)", "Application started.", "Add Files → password.pdf.",
     "'Some files could not be opened:' listing 'password.pdf: The PDF is password protected.'", NOTRUN, "Manual"),
    ("TC-152", "FR-01", "Damaged PDF", "truncated.pdf (first third of text_layer.pdf); not_a_pdf.pdf (text file).", "falcon-ocr <file> for each.",
     "Non-zero exit; 'Cannot open PDF (PDFium error 3).'", PASS, f"CLI ({A}, {B})"),
    ("TC-154", "FR-02", "Corrupt image", "fake.png (1000 zero bytes).", "falcon-ocr fake.png.", "Non-zero exit; 'Parameter is not valid.'; no output.", PASS, f"CLI ({A}, {B})"),
    ("TC-155", "FR-33", "Unsupported file type", "document.docx.", "falcon-ocr document.docx.", "Non-zero exit; 'Unsupported file type: .docx'.", PASS, f"CLI ({A}, {B})"),
    ("TC-156", "FR-33", "Input file does not exist", "—", "falcon-ocr does_not_exist.png.", "Non-zero exit; 'Could not find file …'.", PASS, f"CLI ({A}, {B})"),
    ("TC-157", "FR-33", "Valid and invalid inputs together", "chinese.png and document.docx.", "falcon-ocr chinese.png document.docx -l zh -f txt -o out.",
     "Non-zero exit (SRS 6.4). All inputs are opened first, so the valid file is not processed either.", PASS, f"CLI ({A}); see D-14"),
    ("TC-158", "NFR-06", "Blank page", "blank.png (white A4).", "falcon-ocr blank.png -f docx,txt.",
     "Exit code 0; valid DOCX without paragraphs; TXT contains only the BOM and a line break.", PASS, f"CLI ({A})"),
    # ---------------------------------------------------------------- recognition options and languages
    ("TC-160", "FR-11", "Chinese and Japanese in Word", "—", "Export chinese.png (-l zh) and japanese.png (-l ja) to DOCX and HTML.",
     "Texts exact; DOCX language zh-CN / ja-JP with Microsoft YaHei / Yu Gothic; HTML lang='zh' / 'ja'.", PASS, f"CLI + XML check ({A}, {B})"),
    ("TC-161", "FR-11", "Russian in Word", "—", "Export russian.png -l ru to DOCX and HTML.",
     "Cyrillic text exact; DOCX language ru-RU, font Calibri; HTML lang='ru'.", PASS, f"CLI + XML check ({A}, {B})"),
    ("TC-162", "FR-11", "Unknown language code", "—", "falcon-ocr russian.png -l xx -f txt.",
     "English model used without warning (SRS 6.4); Cyrillic comes out as Latin look-alikes; exit code 0.", PASS, f"CLI ({A})"),
    ("TC-163", "FR-12", "--ocr-only on a text PDF", "—", "falcon-ocr text_layer.pdf --ocr-only --dump.",
     "textLayer=False; same 5 blocks and 2 sections as with the text layer; heading still Heading 1.", PASS, f"CLI ({A}): 3.25 s vs 0.35 s"),
    ("TC-164", "FR-12, FR-19", "Text-layer font in exports", "—", "falcon-ocr text_layer.pdf -f docx,html.",
     "DOCX runs and HTML use the PDF font Helvetica; heading color 126E48; two-column section in Word.", PASS, f"CLI + XML check ({A})"),
    ("TC-165", "FR-13", "Orientation classifier", "russian.png rotated by 180°.", "falcon-ocr russian_180.png -l ru --cls --dump.",
     "All three lines recognized upright.", FAIL, f"CLI ({A}, {B}): only the heading is recovered; both sentences stay pictures — D-11"),
    ("TC-166", "FR-17", "--no-tables", "—", "falcon-ocr report.png --no-tables --dump.",
     "No Table block; table rows become paragraphs such as 'North 1,250' (23 blocks instead of 16).", PASS, f"CLI ({A})"),
    ("TC-167", "FR-01", "PDF rendering resolution", "—", "falcon-ocr multi_scan.pdf --dpi 300 --dump, then --dpi 100.",
     "Page 1 is 2481 × 3507 px at 300 dpi and 827 × 1169 px at 100 dpi; all pages processed.", PASS,
     f"CLI ({A}): 54 lines at 300 dpi, 53 at 100 dpi"),
    ("TC-168", "FR-15", "Footer below a two-column section", "report.png.", "Export TXT, DOCX and HTML; check where 'Contact: ocr-team@example.com' appears.",
     "The footer follows the right column (last text of the page).", FAIL, f"CLI ({A}, {B}): footer placed at the end of the left column, before the right column — D-15"),
    ("TC-169", "NFR-04", "Character accuracy", "report.png and its source text in tools\\make_samples.py.", "Compare the TXT export with the source text (whitespace ignored).",
     "Character accuracy ≥ 98 %.", PASS, f"Script ({A}): 1389 of 1389 characters correct (100 %)"),
    # ---------------------------------------------------------------- export formats x document types
    ("TC-170", "FR-25", "DOCX — Editable structure", "report.png.", "falcon-ocr report.png -f docx; inspect document.xml.",
     "Heading1, Heading2 and ListParagraph styles; 4 list paragraphs; table with fill DCECE4; 2-column section; 1 picture; colors 146F49, 606060, CA2626.", PASS, f"XML check ({A}, {B})"),
    ("TC-171", "FR-25", "DOCX — Exact Copy structure", "report.png.", "… --exact -f docx; inspect.", "37 framed (positioned) paragraphs, 1 anchored picture, the table; no heading styles.", PASS, f"XML check ({A}, {B})"),
    ("TC-172", "FR-25", "DOCX — Plain Text", "report.png.", "… --plain -f docx; inspect.", "19 plain paragraphs; no styles, table, picture or colors.", PASS, f"XML check ({A}, {B})"),
    ("TC-173", "FR-26", "XLSX content", "report.png.", "… -f xlsx; inspect the workbook.",
     "Sheet 'Page 1'; 10 merged ranges; 12 numeric cells (1250, 1410 …); fills 146F49/DCECE4; wrapped paragraph style.", PASS, f"XML check ({A}, {B})"),
    ("TC-175", "FR-27", "HTML — Editable", "report.png.", "… -f html; inspect.", "No external references; 1 embedded image; h1 ×1, h2 ×3, 4 list items, 1 table; lang='en'.", PASS, f"Script ({A}, {B})"),
    ("TC-176", "FR-27", "HTML — Exact Copy", "report.png.", "… --exact -f html; inspect.", "About 40 absolutely positioned elements; embedded picture; no heading or list markup.", PASS, f"Script ({A}, {B})"),
    ("TC-177", "FR-27", "HTML — Plain Text", "report.png.", "… --plain -f html; inspect.", "Text only: no images, tables or headings.", PASS, f"Script ({A}, {B})"),
    ("TC-178", "FR-25", "TXT encoding and page breaks", "multi_scan.pdf.", "… -f txt; read the bytes.", "UTF-8 with BOM; 2 form feeds between 3 pages; identical TXT for all document types.", PASS, f"Script ({A})"),
    # ---------------------------------------------------------------- CLI options and exit codes
    ("TC-180", "FR-33", "Usage text", "—", "falcon-ocr; falcon-ocr --help.", "Usage on stdout, exit code 1.", PASS, f"CLI ({A}, {B})"),
    ("TC-181", "FR-34", "--machine-code", "—", "falcon-ocr --machine-code; FalconOcrKeyGen --machine-code.", "Exit 0; the same 16-character code (4 groups) from both tools.", PASS, f"CLI ({A}, {B})"),
    ("TC-182", "FR-33", "Output streams", "Trial running.", "Run a conversion; capture stdout and stderr separately.",
     "stdout: only the written paths; stderr: trial notice, progress and timing.", PASS, f"CLI ({A})"),
    ("TC-183", "FR-33", "Default folder and format", "chinese.png copied to an empty folder.", "falcon-ocr <copy> -l chinese (no -f, no -o).",
     "chinese.docx next to the input; 'chinese' accepted as alias of zh.", PASS, f"CLI ({A})"),
    ("TC-184", "FR-33", "--dump without -f", "—", "falcon-ocr chinese.png --dump -o newdir.", "Layout printed; nothing exported; newdir not created.", PASS, f"CLI ({A})"),
    ("TC-185", "FR-28", "Existing output file (CLI)", "chinese.txt exists in the output folder.", "Convert chinese.png to txt twice.",
     "The file is replaced (SRS 6.4); no 'chinese (2).txt', no .tmp file.", PASS, f"CLI ({A})"),
    ("TC-186", "FR-28, NFR-06", "Output file locked", "chinese.txt opened exclusively by another process.", "falcon-ocr chinese.png -l zh -f txt -o <folder>.",
     "Export fails with a non-zero exit; the original file is unchanged; no .tmp left.", PASS, f"CLI ({B})"),
    ("TC-187", "FR-33", "Option without value", "—", "falcon-ocr chinese.png -l.", "Usage message, exit code 1.", FAIL,
     f"CLI ({A}, {B}): IndexOutOfRangeException, exit 0xE0434352 — D-12"),
    ("TC-188", "FR-33", "Non-numeric --dpi", "—", "falcon-ocr text_layer.pdf --dpi abc.", "Usage or clear message, exit code 1.", FAIL, f"CLI ({A}): FormatException stack trace — D-12"),
    ("TC-189", "FR-33", "Unknown export format", "—", "falcon-ocr chinese.png -l zh -f pdf.", "Rejected, or a warning that DOCX is used.", FAIL,
     f"CLI ({A}, {B}): chinese.docx written silently, exit 0 — D-13"),
    # ---------------------------------------------------------------- licensing negatives and KeyGen command line
    ("TC-190", "FR-34", "CLI — malformed key", "—", "falcon-ocr --license hello-world.",
     "Exit 3; 'Falcon OCR is not activated: The license key is not in a valid format…', machine code and activation hint; no license.key.", PASS, f"CLI ({A}, {B})"),
    ("TC-191", "FR-34, NFR-07", "CLI — forged key", "FOCR key with a valid payload and a random 64-byte signature.", "falcon-ocr --license <key>.",
     "Exit 3; 'The license key is not genuine.'; no license.key written.", PASS, f"CLI ({A}, {B})"),
    ("TC-193", "FR-34", "Empty key", "—", "falcon-ocr --license \"\"; falcon-ocr --license.", "Exit 3; 'No license key has been entered.'", PASS, f"CLI ({A})"),
    ("TC-194", "FR-36", "KeyGen --verify", "Signing key present.", "FalconOcrKeyGen --verify <forged key>; --verify hello-world.",
     "'InvalidSignature: The license key is not genuine.' and 'Malformed: …'; exit 3.", PASS, f"KeyGen CLI ({A})"),
    ("TC-196", "FR-34", "Clock set back (time-limited key)", "Time-limited key active; license.state records today.", "Set the clock 2 days back; start.",
     "'The system clock is set earlier than the last use of the application.'", NOTRUN, "Manual (clock change not allowed on the test PC)"),
    ("TC-197", "FR-35, NFR-07", "Trial data copied to another PC", "evaluation.dat and registry value from computer 1.", "Copy both to computer 2; start.",
     "HMAC does not match: trial treated as tampered (ended).", NOTRUN, "Manual (second computer)"),
    ("TC-198", "FR-34", "Administrator-deployed key", "Valid license.key in the application folder or %ProgramData%\\FalconOCR.", "Start as a user without a personal key.",
     "Application starts licensed.", NOTRUN, "Manual"),
    ("TC-199", "FR-34", "Key of the previous signing key", "Key issued before 65ea0df; build B.", "Activate.",
     "'The license key is not genuine.' — keys must be re-issued.", NOTRUN, "Manual (no such key on the test PC)"),
    # ---------------------------------------------------------------- non-functional
    ("TC-200", "NFR-03", "Scanned page time", "report.png family.", "11 conversions of report.png plus JPEG/BMP/GIF.",
     "≤ 10 s per page.", PASS, f"CLI timings ({A}, {B}): 5.5–6.8 s"),
    ("TC-201", "NFR-03", "Text-layer page time", "—", "text_layer.pdf with and without --ocr-only.", "Text-layer page ≤ 1 s.", PASS, f"CLI ({A}, {B}): 0.30–0.35 s (OCR 3.25 s)"),
    ("TC-202", "NFR-03", "20-page throughput", "report_20p.pdf (20 scanned pages).", "falcon-ocr report_20p.pdf -f docx,xlsx.",
     "Completes without error; ≤ 10 s per page; 20 worksheets.", PASS, f"CLI ({A}): 100.9 s (5.0 s/page), 20 sheets, 20 tables"),
    ("TC-203", "NFR-01", "Memory", "—", "Sample the peak working set while converting multi_scan.pdf.", "Well below the 4 GB minimum RAM.", PASS, f"PowerShell sampling ({A}): 1,172 MB"),
    ("TC-204", "NFR-12", "Responsive UI", "Large PDF.", "Recognize; move, resize and scroll meanwhile.", "Window responds; progress updates; Stop works.", NOTRUN, "Manual"),
    ("TC-205", "NFR-13", "Source files not locked", "report.png open in the workspace.", "Rename the file in Explorer.", "Rename succeeds.", NOTRUN, "Manual"),
    ("TC-206", "NFR-06", "Corrupt settings.json", "settings.json replaced by invalid text (test account).", "Start the application.", "Starts with defaults; no error.", NOTRUN, "Manual (settings must not be modified here)"),
    ("TC-207", "NFR-06", "Error log", "Provoke an unexpected error.", "Check %LOCALAPPDATA%\\FalconOCR\\error.log.", "Entry with time and stack trace; message box names the log.", NOTRUN, "Manual"),
    ("TC-208", "NFR-05", "High DPI and shortcuts", "Display scaling 150 %.", "Open every page; use Ctrl+O, Ctrl+R, Ctrl+E, PgDn, Del.",
     "No clipped or blurred controls; shortcuts add, rotate, export, page and remove.", NOTRUN, "Manual"),
    # ---------------------------------------------------------------- workspace and settings (FR-45 … FR-51)
    ("TC-210", "FR-45", "Zoom", "Recognized page.", "Fit width, Fit page, 50 % … 300 %, Ctrl+wheel, Ctrl +/−.", "Both viewers zoom together; the zoom box shows the value.", NOTRUN, "Manual"),
    ("TC-211", "FR-45", "Navigation and panning", "3-page document.", "PgDn/PgUp; hand tool; middle mouse drag.", "Counter 'Page 2/3'; both viewers pan together.", NOTRUN, "Manual"),
    ("TC-212", "FR-46", "Document context menu", "Two documents in Files.", "Right-click: Rename…, Open containing folder, Export as…",
     "Entry shows page count and state; rename updates the list; Explorer opens the folder.", NOTRUN, "Manual"),
    ("TC-214", "FR-48", "Partial export", "3-page PDF, page 1 recognized.", "Export.",
     "'2 of 3 pages are not recognized.' Yes recognizes first; No exports page 1 only.", NOTRUN, "Manual"),
    ("TC-215", "FR-48", "Export as…", "Recognized document.", "Export as… → choose name and folder.", "File written with the chosen name.", NOTRUN, "Manual"),
    ("TC-216", "FR-49", "License section in Settings", "Trial running.", "Settings → Copy machine code; Change license key…",
     "'Machine code copied: …'; the activation window opens.", NOTRUN, "Manual"),
    ("TC-217", "FR-50", "Automatic recognition and defaults", "—", "Enable 'Recognize automatically when files are added'; add a file; Restore defaults; Open data folder.",
     "File recognized on adding; 'Restore all settings to their defaults?'; Explorer shows %LOCALAPPDATA%\\FalconOCR.", NOTRUN, "Manual"),
    ("TC-218", "FR-51", "Snapshot mode", "Trial valid.", "FALCON_SNAPSHOT=<folder>; FalconOcr.exe samples\\report.png.",
     "One PNG per screen in the folder; the application exits.", NOTRUN, "Not run here (GUI launch excluded from this run)"),
    # ---------------------------------------------------------------- localization and tools
    ("TC-220", "FR-38", "Catalogs after 1.2", "Current lang\\*.po.", "python tools\\i18n\\po_tool.py check.", "zh_CN and ja fully translated, 0 placeholder mismatches.", PASS, "po_tool: 292/292 each, 2026-09-24"),
    ("TC-221", "NFR-11", "Core error messages translatable", "lang\\ja.po, lang\\zh_CN.po.", "Search the catalogs for messages shown in 'Some files could not be opened' (e.g. 'The PDF is password protected.').",
     "Every such message has a translation.", FAIL, "Script, 2026-09-24: Core exception texts are not in the catalogs — D-16"),
    ("TC-224", "FR-30", "Batch without inputs", "Batch page, empty list.", "Start.", "'Add files or folders first.'", NOTRUN, "Manual"),
    ("TC-225", "FR-31", "Clear history", "History with entries.", "Clear history → Yes.", "'Clear the whole history?'; list empty; history.json emptied.", NOTRUN, "Manual"),
]

GROUPS = [
    ("5.1 Build and installation", "TC-00"), ("5.2 Input", "TC-01"), ("5.3 Page editing", "TC-02"), ("5.4 Recognition", "TC-03"),
    ("5.5 Layout reconstruction", "TC-04"), ("5.6 Results view", "TC-05"), ("5.7 Export", "TC-06"), ("5.8 Tools", "TC-07"),
    ("5.9 Licensing", "TC-08"), ("5.10 Trial", "TC-09"), ("5.11 Key generator", "TC-10"), ("5.12 User interface extensions", ("TC-11", "TC-12", "TC-130")),
    ("5.13 Input formats (CLI)", "TC-14"), ("5.14 Input error handling", "TC-15"), ("5.15 Recognition options and languages", "TC-16"),
    ("5.16 Export formats and document types", "TC-17"), ("5.17 Command-line options and exit codes", "TC-18"),
    ("5.18 Licensing negatives and KeyGen command line", "TC-19"), ("5.19 Non-functional", "TC-20"),
    ("5.20 Workspace and settings (FR-45 – FR-51)", "TC-21"), ("5.21 Localization and tools", "TC-22"),
]

# Older cases executed (again) in the run of 2026-09-24.
RUN_TODAY = {"TC-002", "TC-004", "TC-005", "TC-006", "TC-007", "TC-033", "TC-127"}

# Defects and observations found in the run of 2026-09-24 (open).
OPEN_DEFECTS = [
    ("D-11", "Minor", "--cls recovers only the heading of a page turned by 180°; both sentences remain pictures.", "TC-165"),
    ("D-12", "Minor", "An option without value (-l, -o, -f, --dpi, --font at the end) or a non-numeric --dpi ends with an unhandled exception instead of the usage text.", "TC-187, TC-188"),
    ("D-13", "Minor", "An unknown -f value silently produces DOCX.", "TC-189"),
    ("D-14", "Minor", "CLI input errors print a .NET stack trace and exit with 0xE0434352; one bad input stops the whole call. Allowed by SRS 6.4, but unfriendly for scripts.", "TC-150 – TC-157"),
    ("D-15", "Major", "A line below a two-column section is attached to the left column: the footer is exported before the right column (TXT, DOCX, HTML).", "TC-168"),
    ("D-16", "Minor", "Error texts of FalconOcr.Core (e.g. 'The PDF is password protected.') are not in the PO catalogs and appear in English in the Chinese and Japanese interface.", "TC-221"),
    ("OBS-1", "Info", "The orientation model (cls) must be present even when --cls is not used.", "TC-006"),
    ("OBS-2", "Info", "Most system cases ran on build A (before 65ea0df); only the core subset was repeated on build B. The rest must be repeated on the release build (regression suite R2, section 6.8).", "Section 3.3"),
]


def status_counts(cases):
    return (sum(1 for c in cases if c[6] == PASS), sum(1 for c in cases if c[6] == FAIL), sum(1 for c in cases if c[6] == NOTRUN))


def in_group(case_id, prefix):
    return case_id.startswith(prefix)


# End-to-end procedures: title, covered cases, preconditions, [(action, expected), ...]
PROCEDURES = [
    ("6.1 Scanned page to Word (desktop)", "TC-010, TC-050, TC-060, TC-170",
     "Application started (trial or license); Settings at their defaults; samples\\report.png available.", [
         ("Click **Add Files** and select samples\\report.png.", "Files lists 'report.png · 1 page'; Source Page shows the page with '1654 × 2338 px · 200 dpi' in the footer."),
         ("In OCR Settings choose Language English, Document Type 'Editable Document (Recommended)', Layout Analysis 'Automatic'.", "The values are shown in the settings column."),
         ("Click **Recognize**.", "The button changes to Stop during recognition; the status bar ends with 'Text recognition completed — 1 page(s) in n s.' (n ≤ 10)."),
         ("Compare Source Page and Recognized Text at Fit width.", "Every line sits at the same position on both sides; headings, bullets, table, two columns, picture and red footer are visible."),
         ("Select 'Microsoft Word (.docx)' in Output Format and click **Export**.", "Status bar 'Exported to <output folder>\\report.docx'; the document opens in Word (Open after export is on)."),
         ("Check the document in Word.", "Opens without repair; Heading 1 title in green, three Heading 2 headings, 4 bulleted items, 5 × 4 table with header fill, two-column section, picture."),
     ]),
    ("6.2 Digital PDF with text layer", "TC-011, TC-163, TC-164, TC-201",
     "PDF input setting 'Use the PDF text layer when present (fast, exact)'.", [
         ("Add samples\\text_layer.pdf and click Recognize.", "Page processed in about 0.3 s; the status reports that the text layer was read."),
         ("Select the heading 'Digital PDF Text Layer'.", "Style box Heading 1, 22 pt, bold; color #126E48."),
         ("Export DOCX and HTML.", "Both use the font of the PDF (Helvetica) and keep the two-column section."),
         ("Advanced Settings → 'Always run OCR (ignore embedded text)'; recognize again.", "Recognition takes several seconds (3.25 s measured with the CLI); text unchanged; sizes within ±0.5 pt."),
         ("Restore the text-layer setting.", "Next recognition uses the text layer again."),
     ]),
    ("6.3 Batch processing", "TC-071, TC-224, TC-072",
     "Folder with three PDFs; output folder writable.", [
         ("Open **Batch Process** and click **▶ Start** with an empty list.", "Message 'Add files or folders first.'"),
         ("Click 'Add folder…' and select the folder.", "Three rows with status 'Waiting'; the footer shows 'Total Files: 3'."),
         ("Choose Microsoft Word (.docx), Editable Document, English and the output folder; click ▶ Start.", "Rows change to 'Recognizing…' and 'Done'; the log shows 'Started: 3 document(s) → …'."),
         ("Wait for the end.", "'Finished: 3 succeeded, 0 failed, n s.' and the question '3 document(s) exported. Open the output folder?'"),
         ("Open History.", "Three new entries with source, output, format, pages, language and duration."),
     ]),
    ("6.4 Activation with a computer-bound key", "TC-081, TC-096, TC-123, TC-216",
     "Trial running; a key for this machine code, issued with the signing key of the build under test (for build B: fingerprint 6CD0-2A14-9706-8C3E).", [
         ("Start the application.", "Activation window 'Activate Falcon OCR' with 'You are using the trial version — n of 7 days left.'"),
         ("Click **Copy** next to the machine code.", "'Machine code copied to the clipboard.'; the code has 4 groups of 4 characters."),
         ("Click Continue Trial.", "Main window with the title-bar badge 'TRIAL · n days left — Activate now' and the amber notification below the toolbar."),
         ("Click 'Activate now', paste the key under '2. Enter or paste the license key:' and click **Activate**.", "'Thank you — Falcon OCR is activated.' followed by 'Licensed to … · serial … · this computer only'."),
         ("Close the message.", "Badge and notification disappear; Settings shows the license."),
         ("Restart the application.", "No activation window; main window without trial marks."),
     ]),
    ("6.5 Trial expiry", "TC-090, TC-092, TC-097",
     "Separate Windows test account (never the account holding the trial of the test PC); trial started more than 7 days ago, e.g. with the simulated clock of the unit test.", [
         ("Start the application.", "Activation window with 'Your trial period has ended. A license key is required to continue.'; only Activate and Exit are offered."),
         ("Click Exit.", "The application closes; no main window."),
         ("Run falcon-ocr samples\\russian.png -l ru.", "'The 7-day trial ended on yyyy-MM-dd.', 'Falcon OCR is not activated: …', machine code; exit code 3."),
         ("Activate a valid key and start again.", "Main window opens licensed."),
     ]),
    ("6.6 KeyGen: issue and verify", "TC-100, TC-101, TC-194",
     "Vendor computer with the signing key; the application built with the matching public key.", [
         ("Start FalconOcrKeyGen.", "Signing key section shows the fingerprint; no mismatch warning."),
         ("Enter 'Licensee (customer):', the customer's machine code, Expiry 'Perpetual (never expires)'; click **Generate**.", "A key 'FOCR-…' appears under 'License key:'; serial number increases by one."),
         ("Click 'Save as .lic file…'.", "File saved; 'Open issued log' shows the new line with licensee, serial and machine."),
         ("Paste the key into 'Verify a key'.", "'Signature OK' with licensee, serial, issue date and machine."),
         ("Change one character and verify again.", "Signature check fails (on the command line: 'InvalidSignature: The license key is not genuine.', exit 3)."),
     ]),
    ("6.7 Switching the interface language", "TC-113, TC-114, TC-117",
     "English interface.", [
         ("Settings → General → 'Interface language:' = 日本語 → Save.", "Question 'The interface language is changed after a restart. Restart Falcon OCR now?'"),
         ("Answer Yes.", "The application restarts in Japanese with the Yu Gothic UI font."),
         ("Open Home, OCR, Batch Process, History, Settings and the activation window.", "No English text except product names and file names."),
         ("Repeat with 简体中文, then English.", "Chinese with Microsoft YaHei UI; English restored; setting kept after restart."),
     ]),
]

# Regression suites: name, trigger, content, method
REGRESSION = [
    ("R1 Smoke", "Every build", "TC-001, 002, 005, 030, 031, 032, 033, 034, 097, 170, 173, 175, 180, 190", "CLI harness and XML checks"),
    ("R2 System (CLI)", "Every release candidate", "All executed cases TC-140 – TC-203 and TC-004 – TC-007", "CLI harness; compare with the values in section 7"),
    ("R3 Licensing", "Changes to Licensing, KeyGen or the public key", "TC-080 – TC-103, TC-190 – TC-199", "CLI, unit tests with simulated clock, manual activation"),
    ("R4 User interface", "Changes to App or the PO catalogs", "TC-010, 050 – 056, 110 – 130, 210 – 218, procedures 6.1, 6.4, 6.7", "Snapshot tour and manual steps"),
    ("R5 Localization", "New or edited strings", "TC-113 – 117, 220, 221", "po_tool check, snapshot tour per language"),
    ("R6 Performance", "Engine, layout or ONNX Runtime changes", "TC-037, 200 – 203", "Timed CLI runs on the reference PC"),
]

# Defect reports: id, title, severity, build, steps, expected, actual, analysis
DEFECT_REPORTS = [
    ("D-11", "Orientation classifier misses upside-down sentences", "Minor (FR-13, Could)", "Builds A and B",
     ["Create russian_180.png: samples\\russian.png turned by 180° (dpi kept at 200).", "Run falcon-ocr russian_180.png -l ru --cls --dump."],
     "Heading and both sentences recognized upright.",
     "lines=1: only 'Оптическое распознавание' is recognized; one sentence area is a Figure block. Without --cls (same page without dpi information) no line is recognized at all (5 Figure blocks).",
     "Upside-down lines apparently fall below the detection or confidence thresholds before the classifier can correct them; to be analysed in the engine."),
    ("D-12", "Missing or invalid option value crashes the CLI", "Minor (FR-33)", "Builds A and B",
     ["Run falcon-ocr samples\\chinese.png -l", "Run falcon-ocr samples\\text_layer.pdf --dpi abc"],
     "Usage text and exit code 1.",
     "'Unhandled Exception: System.IndexOutOfRangeException' and 'System.FormatException' with stack traces; exit code 0xE0434352.",
     "Program.cs reads option values with args[++i] and int.Parse without checks."),
    ("D-13", "Unknown export format silently produces DOCX", "Minor (FR-33)", "Builds A and B",
     ["Run falcon-ocr samples\\chinese.png -l zh -f pdf -o <out>"],
     "The value is rejected, or a warning names the format used.",
     "chinese.docx is written; no message; exit code 0.",
     "ParseFormat maps every unknown value to DOCX."),
    ("D-14", "CLI input errors end the whole run with a stack trace", "Minor (usability)", "Builds A and B",
     ["Run falcon-ocr samples\\chinese.png document.docx -l zh -f txt -o <out>"],
     "chinese.txt is written; document.docx is reported in one line; non-zero exit code.",
     "'Unhandled Exception: System.NotSupportedException: Unsupported file type: .docx' with stack trace; no output at all; exit code 0xE0434352.",
     "All inputs are opened before processing and exceptions are not caught. Allowed by SRS 6.4, therefore the cases pass."),
    ("D-15", "Footer below two columns exported inside the left column", "Major (FR-15, Must)", "Builds A and B",
     ["Run falcon-ocr samples\\report.png --dump -f txt,docx,html -o <out>", "Look at the second section of the dump and at the end of report.txt."],
     "'Contact: ocr-team@example.com' is the last text of the page, after the right column.",
     "The dump lists it as '[Paragraph c0 … @149,2011]' inside the section 'cols=2 y=1393-2048'; TXT, DOCX and HTML place it before 'The second column continues the discussion.'",
     "The two-column section extends down to the footer, and the footer lies within the x-range of column 0. The section should end above a line that starts below both columns."),
    ("D-16", "Core error messages not translatable", "Minor (NFR-11)", "Catalogs of 65ea0df",
     ["Search lang\\ja.po and lang\\zh_CN.po for 'The PDF is password protected.'", "In the Japanese interface add password.pdf (manual confirmation, TC-151)."],
     "A translated message in the warning.",
     "The message is not in the catalogs; the warning line is shown in English.",
     "PdfDocument and OcrDocument throw fixed English texts that are shown via ex.Message; they are not passed through L.T."),
]


def procedures(d):
    d.h1("6. Test procedures")
    d.p("The following procedures describe the most important end-to-end cases step by step, with the expected result of each step. "
        "They are used for the manual executions and for the acceptance run. The status of the covered cases is given in section 5; a procedure "
        "passes when every step shows its expected result.")
    for title, cases, pre, steps in PROCEDURES:
        d.h2(title)
        d.p(f"**Covers:** {cases}. **Preconditions:** {pre}")
        d.table(["#", "Action", "Expected result"], [(str(i), a, e) for i, (a, e) in enumerate(steps, 1)], [5, 45, 50])
    d.h2("6.8 Regression test suites")
    d.p("Selection of cases to repeat after a change. R1 is the build acceptance test; R2 must be completed on the release build (OBS-2).")
    d.table(["Suite", "Trigger", "Cases", "Method"], REGRESSION, [16, 22, 40, 22], "Regression suites")


def defect_reports(d):
    d.h2("8.3 Defect reports")
    d.p("Reproduction data for the open defects (all state Open). Environment: section 3; generated inputs: section 4.2.")
    for did, title, sev, build, steps, expected, actual, analysis in DEFECT_REPORTS:
        d.h3(f"{did} {title}")
        d.table(["Field", "Content"], [
            ("Severity", sev), ("Found on", f"{build}, 2026-09-24"),
            ("Steps", " ".join(f"{i}. {s}" for i, s in enumerate(steps, 1))),
            ("Expected", expected), ("Actual", actual), ("Analysis", analysis),
        ], [18, 82])


def build():
    d = Doc("Test Case Specification", "Test plan, environment, test cases, results and traceability", "FOCR-TCS-001",
            "This document specifies how Falcon OCR 1.0 is tested and records the results of the test runs of 2026-09-24. "
            "Every test case references the requirements of FOCR-SRS-001 it verifies.")

    # ================================================================ 1
    d.h1("1. Introduction")
    d.h2("1.1 Purpose")
    d.p("This Test Case Specification (TCS) is the test plan and the test record for Falcon OCR 1.0. It defines scope, approach, environment, test data and "
        "pass/fail criteria, lists every test case with its steps and expected result, and records what was run on 2026-09-24, how, and with what result. "
        "The reviewers who accept the release use it together with the Software Requirements Specification (FOCR-SRS-001).")
    d.h2("1.2 Scope")
    d.p("In scope are the three deliverables of the product:")
    d.ul(["**FalconOcr.exe** — the desktop application: input, page editing, recognition, layout reconstruction, results view, export, batch, history, settings, trial and activation, interface languages.",
          "**falcon-ocr.exe** — the command-line tool: recognition and export for automation, licensing on the command line, exit codes.",
          "**FalconOcrKeyGen.exe** — the vendor tool: signing key, key generation and verification."])
    d.p("Out of scope: handwriting, degraded material (below 150 dpi, heavy noise), differences between Office versions, "
        "the PaddleOCR models themselves and installers (the product is deployed by copying a folder).")
    d.h2("1.3 References")
    d.ul(["FOCR-SRS-001 Software Requirements Specification — requirement IDs FR-nn / NFR-nn.",
          "FOCR-SDD-001 System Design Document — components referred to in fault analysis.",
          "FOCR-SCR-001 Screen Design Document — texts and layout checked by the UI cases.",
          "FOCR-UM-001 User Manual — command-line options and exit codes."])
    d.h2("1.4 Status values")
    d.table(["Status", "Meaning"], [
        (PASS, "Executed; the observed result matched the expected result."),
        (FAIL, "Executed; the observed result differed. A defect is recorded in section 8."),
        (NOTRUN, "Not executed in this run: interactive UI steps, hardware (scanner, second computer), clock changes or data that must not be modified on the test PC. To be executed manually before acceptance."),
    ], [15, 85], "Status values", raw=True)
    d.p("Each case states how it was executed: CLI run, API or analysis script, the built-in snapshot tour of the user interface, or rendering in Word, Excel or Edge. "
        "Cases run on 2026-09-24 also name the build (A or B, section 3.3).")

    # ================================================================ 2
    d.h1("2. Test approach")
    d.h2("2.1 Test levels")
    d.table(["Level", "Scope", "Approach"], [
        ("Component (API)", "Engine, layout, exporters, licensing, trial", "PowerShell scripts calling FalconOcr.Core directly (e.g. rotation/crop re-mapping, trial with simulated clock)."),
        ("System (CLI)", "Recognition, layout, export, licensing rules, exit codes", "falcon-ocr.exe with --dump (layout printout), exit codes, stdout/stderr, output files."),
        ("Output validation", "DOCX, XLSX, HTML, TXT", "Rendering in Word, Excel (COM, export to PDF → PNG) and Edge (headless screenshot); unzipping DOCX/XLSX and checking the XML with Python."),
        ("UI", "Screens, alignment, selection, trial badge", "Built-in snapshot tour (FALCON_SNAPSHOT) renders every screen after recognition; dialogs rendered off-screen; interactive cases manual."),
        ("Build", "Offline build, deployment", "Clean copy, empty NuGet cache, network blocked; isolated build of the current commit."),
    ], [18, 30, 52], "Test levels")
    d.h2("2.2 Test types and techniques")
    d.table(["Type", "What is checked", "Technique"], [
        ("Functional", "Every FR against the SRS.", "Positive case per feature; variants per format, language, document type."),
        ("Negative", "Corrupt, unsupported and protected inputs; bad options; forged, malformed and empty keys.", "Error guessing and equivalence classes of invalid input."),
        ("Boundary", "Trial day 1 / day 7 / 7.1 days; clock 24 h back; panel width 280–640 px; PDF 100 / 200 / 300 dpi.", "Boundary value analysis."),
        ("Compatibility", "Image formats; PDF with and without text layer.", "Same content in every format, results compared."),
        ("Localization", "Interface in English, Chinese, Japanese; catalog completeness.", "po_tool check, snapshot tours per language, catalog search."),
        ("Performance", "Time per page, 20-page throughput, memory.", "Repeated timed runs on the Release build."),
        ("Security", "Signature check, machine binding, trial tamper protection, no network use.", "Forged, altered keys; edited trial data."),
        ("Regression", "Core cases on the current source.", "Repeated on build B."),
    ], [16, 44, 40], "Test types")
    d.h2("2.3 Automation")
    d.p("The system cases of 2026-09-24 were executed by a shell harness that runs falcon-ocr.exe once per case and stores stdout, stderr, exit code "
        "and duration. Exported files are checked by Python scripts that unzip DOCX and XLSX packages and count styles, tables, sections, merged "
        "cells, numeric cells, fonts, language tags and embedded images, and that check HTML files for external references. The user interface is checked with the snapshot tour; "
        "cases that need mouse or keyboard interaction remain manual.")
    d.h2("2.4 Entry criteria")
    d.ul(["The solution builds offline without warnings (TC-001).",
          "The application folder contains all models and native libraries (TC-004).",
          "A valid trial or license is available on the test computer; no license is activated during the run.",
          "The sample set (samples\\) and the generated negative data (section 4.2) are available."])
    d.h2("2.5 Exit and acceptance criteria")
    d.ul(["All Must requirements have at least one test case, and every executed Must case passes.",
          "No open defect of severity Critical or Major, or a documented decision by the product owner to accept it.",
          "The Not run cases are executed manually on the release build and recorded in this document.",
          "The results are confirmed on the release build of the current source (see OBS-2)."])
    d.p("With D-15 (Major) open and the manual cases still pending, the exit criteria are **not yet met**.")
    d.h2("2.6 Suspension and resumption")
    d.p("Testing is suspended when the build fails, the models cannot be loaded, or the trial on the test computer has ended (every CLI call then returns exit code 3). "
        "It resumes on a new build or with a test license. Licenses and trial data are never edited or deleted to continue testing.")
    d.h2("2.7 Roles")
    d.table(["Role", "Responsibility"], [
        ("Test lead", "Maintains this document, plans the runs, decides on suspension, reports the results."),
        ("Tester", "Executes the cases, records evidence, raises defects with steps and logs."),
        ("Developer", "Analyses and fixes defects; provides the build under test."),
        ("Product owner", "Decides on open defects and accepts the release."),
        ("Vendor (KeyGen holder)", "Issues test licenses; the signing key is never given to testers."),
    ], [25, 75], "Roles")
    d.h2("2.8 Defect severity")
    d.table(["Severity", "Definition"], [
        ("Critical", "Crash, data loss, or the product cannot be used or licensed."),
        ("Major", "A Must requirement is not met and there is no reasonable workaround."),
        ("Minor", "A Should/Could requirement is not met, or a Must feature has a workaround."),
        ("Info", "Observation or improvement proposal."),
    ], [14, 86], "Severity classes")
    d.p("A defect is raised with an ID (D-nn), severity, the failing test case and the log. It is closed when the case passes on a new build.")
    d.h2("2.9 Risks")
    d.table(["Risk", "Effect", "Mitigation"], [
        ("Build under test differs from the source", "Results do not describe the release.", "Record the build per case; repeat core cases on build B; re-run on the release build."),
        ("Public key replaced in 65ea0df", "Keys of the old signing key no longer activate.", "Re-issue test keys; re-run TC-081 – TC-087 (TC-199)."),
        ("Korean disabled in 65ea0df", "Earlier Korean results are obsolete.", "TC-033 re-scoped and re-run on build B."),
        ("Trial on the test PC expires", "All CLI tests end with exit code 3.", "Plan runs within the 7 days; use a test license."),
        ("Manual cases not executed", "UI regressions undetected.", "Execute the Not run list before acceptance."),
    ], [28, 30, 42], "Risks")

    # ================================================================ 3
    d.h1("3. Test environment")
    d.h2("3.1 Hardware and software")
    d.table(["Item", "Value"], [
        ("Computer", "Intel Core i9-9900K, 8 cores / 16 threads, 3.6 GHz; 128 GB RAM"),
        ("Operating system", "Windows 11 Pro 64-bit (10.0.26200)"),
        (".NET", ".NET Framework 4.8.1 runtime; build with .NET SDK 9.0.314"),
        ("Office", "Microsoft Word and Excel 16.0 (output validation only)"),
        ("Browser", "Microsoft Edge (HTML validation)"),
        ("Scripts", "Python 3.12 with Pillow 12.3 (test data, XML checks); Git Bash; Windows PowerShell 5.1"),
        ("Product", "Release, x64; ONNX Runtime 1.22.1; PP-OCRv5 mobile models"),
    ], [25, 75], "Environment")
    d.h2("3.2 User data on the test computer")
    d.table(["Location", "Content", "Rule during testing"], [
        ("%LOCALAPPDATA%\\FalconOCR\\license.key", "Activated key (not present)", "Not created: only invalid keys were entered."),
        ("%LOCALAPPDATA%\\FalconOCR\\license.state", "Last use of a time-limited key", "Not modified."),
        ("%LOCALAPPDATA%\\FalconOCR\\evaluation.dat, HKCU\\Software\\FalconOCR", "Trial start and last use", "Only updated by the product itself."),
        ("%LOCALAPPDATA%\\FalconOCR\\settings.json, history.json", "Settings and history", "Not modified."),
        ("Scratch folder", "Generated inputs (tcs-in), outputs (tcs-out), logs", "All test output is written here."),
    ], [38, 30, 32], "User data")
    d.h2("3.3 Builds under test")
    d.p("Commit 65ea0df disables Korean in the language catalog, the CLI and the Word exporter, and replaces the public license key. "
        "The run of 2026-09-24 started on an application folder built before that commit, so two builds were tested. They are named by identity, not by folder:")
    d.table(["Build", "Identity", "Differences"], [
        ("Earlier runs", "Development builds before 2026-09-24 07:37", "Cases TC-001 – TC-130 marked Pass without a build name."),
        ("A", "Pre-65ea0df build (previous dist\\, built 2026-09-24 07:37)", "Korean enabled; old public key, fingerprint 1155-9149-8948-EC89."),
        ("B", "65ea0df build (current dist\\)", "Korean not offered (-l ko → English); new public key, fingerprint 6CD0-2A14-9706-8C3E."),
    ], [16, 42, 42], "Builds")
    d.p("The build B cases were executed on a build of commit 65ea0df made in an isolated copy of the repository; there were no uncommitted source changes. "
        "The rebuilt dist\\ has the same identity: its FalconOcr.Core.dll carries fingerprint 6CD0-2A14-9706-8C3E and has no Korean model entry.")
    d.note("Build A still recognizes Korean. Results of build A are valid for all features that 65ea0df did not change; this was confirmed by repeating the core cases on build B (identical results).")

    # ================================================================ 4
    d.h1("4. Test data")
    d.h2("4.1 Sample documents")
    d.table(["File (samples\\)", "Content", "Used by"], [
        ("report.png", "A4 at 200 dpi: green title, grey subtitle, paragraph, numbered headings, 4 drawn bullets, ruled 5 × 4 table with header fill, two-column text, picture, red footer.", "TC-010, 030, 040–046, 050–052, 060–063, 17x, 200"),
        ("text_layer.pdf", "Digital PDF (Helvetica): heading, paragraph, two columns.", "TC-011, 020, 021, 163, 164, 201"),
        ("multi_scan.pdf", "3 scanned pages: report, Korean text, chapter page.", "TC-012, 064, 167, 178, 179, 203"),
        ("sequence\\page1-3.png", "Three chapter pages.", "TC-013"),
        ("chinese / japanese / russian.png", "Heading and two sentences per language.", "TC-031, 032, 034, 160, 161"),
        ("korean.png", "Korean heading and two sentences.", "TC-033 (language not offered)"),
    ], [25, 50, 25], "Sample documents (generated by tools\\make_samples.py)")
    d.img("sample-input.png", "Main test page samples\\report.png", 380)
    d.h2("4.2 Generated test data")
    d.p("Additional inputs were generated with Python (Pillow) into the scratch folder:")
    d.table(["File", "How it was made", "Purpose"], [
        ("report.jpg / .bmp / .gif", "report.png saved as JPEG (q 92), 24-bit BMP, 256-colour GIF", "Format compatibility"),
        ("report_exif6.jpg", "Page turned 90° with EXIF Orientation = 6", "EXIF handling"),
        ("three_pages.tif", "The sequence pages as a 3-frame LZW TIFF", "Multi-page TIFF"),
        ("natseq\\p1, p2, p10.png", "Names that sort differently as text and as numbers", "Natural sort"),
        ("russian_180.png", "russian.png turned by 180°", "Orientation classifier"),
        ("blank.png", "White A4 page", "Empty result"),
        ("report_20p.pdf", "report.png repeated on 20 pages", "Throughput"),
        ("password.pdf", "One-page PDF, RC4 40-bit, user password", "Protected PDF"),
        ("truncated.pdf, not_a_pdf.pdf, fake.png, document.docx", "Cut file, text file, zero bytes, wrong type", "Error handling"),
        ("forged key", "FOCR key: valid payload, random signature", "Signature check"),
    ], [30, 45, 25], "Generated data")

    # ================================================================ 5
    d.h1("5. Test cases")
    d.p("Cases are grouped by feature area. IDs are stable; gaps are reserved. The method and evidence of each case are listed in section 7; step-by-step procedures for the main end-to-end cases are in section 6.")
    for title, prefix in GROUPS:
        rows = [(c[0], c[1], c[2], c[3], c[4], c[5], c[6]) for c in CASES if in_group(c[0], prefix)]
        if not rows:
            continue
        d.h2(title)
        d.table(["ID", "Req.", "Title", "Preconditions", "Steps", "Expected result", "Status"], rows, [9, 10, 13, 16, 18, 25, 9], raw=True)

    # ================================================================ 6
    procedures(d)

    # ================================================================ 7
    d.h1("7. Test results")
    total = len(CASES)
    passed, failed, notrun = status_counts(CASES)
    d.table(["Total", "Pass", "Fail", "Not run"], [(str(total), str(passed), str(failed), str(notrun))], [25, 25, 25, 25], "Result summary (2026-09-24)")
    d.p(f"{passed + failed} of {total} cases were executed; {failed} failed. The failures are recorded as defects in section 8. "
        "The cases marked Not run require interactive UI actions, hardware or changes to the test computer and are to be executed manually before release acceptance.")
    d.h2("7.1 Results by area")
    rows = []
    for title, prefix in GROUPS:
        cs = [c for c in CASES if in_group(c[0], prefix)]
        if cs:
            p, f, n = status_counts(cs)
            rows.append((title.split(" ", 1)[1], str(len(cs)), str(p), str(f), str(n)))
    d.table(["Area", "Cases", "Pass", "Fail", "Not run"], rows, [52, 12, 12, 12, 12], "Results by area")
    d.h2("7.2 Execution log — run of 2026-09-24")
    d.p("Cases executed in this run. A and B name the build (section 3.3).")
    run = [c for c in CASES if c[6] != NOTRUN and (int(c[0][3:]) >= 140 or c[0] in RUN_TODAY)]
    d.table(["ID", "Status", "Method / evidence"], [(c[0], c[6], c[7]) for c in run], [12, 12, 76], raw=True)
    d.h2("7.3 Earlier executions")
    d.p("Cases passed in the development test runs before 2026-09-24, by method:")
    methods = {}
    for c in CASES:
        if c[6] == PASS and c not in run:
            m = c[7].split(",")[0].split(" (")[0]
            methods.setdefault(m, []).append(c[0])
    d.table(["Method", "Cases"], [(m, ", ".join(ids)) for m, ids in methods.items()], [30, 70], "Earlier executions")
    d.note("TC-080 was run before the trial existed (same code path as an expired trial). TC-087 and TC-081 – TC-086 used keys of the signing key that was replaced in 65ea0df and must be repeated (TC-199).")
    d.h2("7.4 Measurements")
    d.table(["Measurement", "Value", "Case"], [
        ("Scanned A4 page, 200 dpi (11 runs)", "5.5 – 6.8 s per page including model loading", "TC-200"),
        ("Text-layer PDF page", "0.30 – 0.35 s; the same page with --ocr-only 3.25 s", "TC-201"),
        ("20 scanned pages", "100.9 s, 5.0 s per page", "TC-202"),
        ("Small pages (sequence, TIFF)", "0.7 – 1.4 s per page", "TC-144, 145"),
        ("Peak working set, 3-page PDF", "1,172 MB", "TC-203"),
        ("Character accuracy, report.png", "100 % (1389 / 1389)", "TC-169"),
    ], [40, 45, 15], "Measurements")

    # ================================================================ 7
    d.h1("8. Defects")
    d.h2("8.1 Defects found and fixed during development testing")
    d.table(["Defect", "Fix", "Verified by"], [
        ("Recognition 2 × slower than expected (thread oversubscription).", "ONNX Runtime uses one thread per physical core.", "TC-037"),
        ("'1. Key Features' classified as ordered list item.", "Numbered headings win when larger or bold.", "TC-040"),
        ("Drawn bullets missing.", "Pixel-based bullet detection left of lines.", "TC-041"),
        ("Two paragraphs merged.", "Paragraph gap threshold relative to median gap.", "TC-042"),
        ("Lines with few descenders measured too small / bold.", "Contiguous ink-extent growth; stroke width 2·area/perimeter.", "TC-046"),
        ("Table numbers with commas measured too small; single bold cell.", "Commas not treated as descenders; table style normalisation.", "TC-043"),
        ("Colored UI fills detected as tables; panels swallowed as pictures.", "Rules need background on both sides; containers with text are not pictures.", "TC-048"),
        ("Korean lines joined without space.", "Only Chinese/Japanese join without spaces.", "Earlier build (Korean disabled since 65ea0df)"),
        ("Excel paragraphs overflowing the page.", "Paragraph cells merged across their columns and wrapped.", "TC-062"),
        ("Start-up crash in CardPanel layout.", "Layout guard during construction.", "TC-010"),
    ], [40, 42, 18], "Fixed defects")
    d.h2("8.2 Open defects and observations")
    d.table(["ID", "Severity", "Description", "Cases"], OPEN_DEFECTS, [8, 10, 64, 18], "Open defects (2026-09-24)")
    defect_reports(d)

    # ================================================================ 8
    d.h1("9. Traceability matrix")
    from doc_srs import FR, NFR
    d.p("Coverage of every requirement of FOCR-SRS-001. 'Passed' means at least one executed case passed and none failed.")
    rows, gaps, fails, covered_pass = [], 0, 0, 0
    for r in FR + [(n[0], "", n[1]) for n in NFR]:
        cs = [c for c in CASES if r[0] in [x.strip() for x in c[1].split(",")]]
        p, f, n = status_counts(cs)
        state = "Gap" if not cs else "Failed" if f else "Passed" if p else "Not run"
        gaps += state == "Gap"
        fails += state == "Failed"
        covered_pass += state == "Passed"
        rows.append((r[0], r[2], ", ".join(c[0] for c in cs) if cs else "—", state))
    d.table(["Requirement", "Title / category", "Test cases", "State"], rows, [13, 30, 45, 12], "Requirement coverage")
    d.p(f"{len(rows)} requirements: {covered_pass} passed, {fails} with a failed case, {len(rows) - covered_pass - fails - gaps} covered by Not run cases only, {gaps} without a test case.")
    return d
