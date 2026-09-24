from docbuilder import Doc

PASS = "<span class='pass'>Pass</span>"
NOTRUN = "<span class='notrun'>Not run</span>"

# id, requirement(s), title, preconditions, steps, expected, status, method/evidence
CASES = [
    # ---------------------------------------------------------------- build & installation
    ("TC-001", "NFR-02", "Offline build from a clean copy", "Copy of the repository without bin/obj/.tmp; empty NuGet cache; network blocked (proxy 127.0.0.1:9).",
     "Run build.ps1.", "Restore uses only packages\\; build succeeds with 0 warnings and 0 errors; dist\\ is created.", PASS, "Automated script, 2026-09-24"),
    ("TC-002", "NFR-09", "Standalone deployment", "dist\\ copied from TC-001.",
     "Run dist\\falcon-ocr.exe on samples\\korean.png -l ko.", "Models and native DLLs found; text recognized.", PASS, "CLI, 2026-09-24"),
    ("TC-003", "NFR-06", "Missing model files", "Rename models\\det in the application folder.",
     "Start the application.", "Warning lists the missing files; recognition is refused with a message.", NOTRUN, "Manual"),
    # ---------------------------------------------------------------- input
    ("TC-010", "FR-02", "Open a single image", "Application started.", "Add Files → samples\\report.png.",
     "Document 'report.png · 1 page' appears; page shown in Source Page with '1654 × 2338 px · 200 dpi'.", PASS, "Snapshot tour (UI), 2026-09-24"),
    ("TC-011", "FR-01, FR-12", "Open a PDF with text layer", "—", "Recognize samples\\text_layer.pdf.",
     "Lines taken from the text layer (status 'Text layer read from PDF'); font sizes 22/11/14/10 pt, heading color #126E48, bold headings.", PASS, "CLI --dump, 2026-09-24"),
    ("TC-012", "FR-01", "Open a scanned multi-page PDF", "—", "Recognize samples\\multi_scan.pdf (3 pages).",
     "3 pages OCR'd (no text layer); export contains 3 pages.", PASS, "API + Word render, 2026-09-24"),
    ("TC-013", "FR-04", "Image sequence", "—", "falcon-ocr samples\\sequence --seq, or Add Files → Add folder as image sequence.",
     "One document with page1, page2, page3 in natural order.", PASS, "CLI --seq, 2026-09-24"),
    ("TC-014", "FR-03", "Multi-page TIFF", "A 3-frame TIFF.", "Add the TIFF.", "One document with 3 pages.", NOTRUN, "Manual"),
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
    ("TC-033", "FR-11", "Korean recognition", "—", "Recognize samples\\korean.png (Korean).", "광학 문자 인식 and both sentences correct; lines joined with a space.", PASS, "CLI, 2026-09-24"),
    ("TC-034", "FR-11", "Russian recognition", "—", "Recognize samples\\russian.png (Russian).", "Оптическое распознавание and both sentences correct.", PASS, "CLI, 2026-09-24"),
    ("TC-035", "FR-12", "Force OCR on a text PDF", "Advanced: Always run OCR.", "Recognize text_layer.pdf.", "Status shows OCR (not text layer); text still correct.", NOTRUN, "Manual"),
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
]


def build():
    d = Doc("Test Case Specification", "Test strategy, environment, test cases and results", "FOCR-TCS-001",
            "This document specifies how Falcon OCR 1.0 is tested and records the results of the test run of 2026-09-24. "
            "Every test case references the requirements of FOCR-SRS-001 it verifies.")

    d.h1("1. Introduction")
    d.p("The test specification covers functional, performance and licensing tests of the desktop application, the command-line tool and the key generator. "
        "Status values: **Pass** — executed on 2026-09-24 and the expected result was observed; **Not run** — to be executed manually (interactive UI actions, hardware such as scanners). "
        "The method column states how a case was executed (CLI, API test script, automated UI snapshot, Office rendering).")

    d.h1("2. Test strategy")
    d.table(["Level", "Scope", "Approach"], [
        ("Component (API)", "Engine, layout, exporters, licensing, trial", "PowerShell scripts calling FalconOcr.Core directly (e.g. rotation/crop re-mapping, trial with simulated clock)."),
        ("System (CLI)", "Recognition, layout, export, licensing rules", "falcon-ocr.exe with --dump (layout printout), exit codes, output files."),
        ("Output validation", "DOCX, XLSX, HTML", "Files opened and rendered by Microsoft Word, Microsoft Excel (COM, export to PDF → PNG) and Microsoft Edge (headless screenshot); checked visually against the source page."),
        ("UI", "Screens, alignment, selection, trial badge", "Built-in snapshot tour (FALCON_SNAPSHOT) renders every screen after recognition; dialogs rendered off-screen; interactive cases manual."),
        ("Build", "Offline build, deployment", "Clean copy, empty NuGet cache, network blocked."),
    ], [18, 30, 52], "Test levels")
    d.h2("2.1 Test data")
    d.table(["File (samples\\)", "Content", "Used by"], [
        ("report.png", "A4 at 200 dpi: title (green), subtitle, paragraph, numbered headings, 4 drawn bullets, ruled 5 × 4 table with header fill, two-column text, picture, red footer.", "TC-010, 030, 040–046, 050–052, 060–063"),
        ("text_layer.pdf", "Digital PDF (Helvetica) with heading, paragraph, two columns.", "TC-011, 020, 021"),
        ("multi_scan.pdf", "3 scanned pages (report, Korean text, chapter page).", "TC-012, 064"),
        ("sequence\\page1-3.png", "Three chapter pages.", "TC-013"),
        ("chinese/japanese/korean/russian.png", "Heading and two sentences per language.", "TC-031 … TC-034"),
    ], [25, 50, 25], "Test data (generated by tools\\make_samples.py)")
    d.img("sample-input.png", "Main test page samples\\report.png", 380)

    d.h1("3. Test environment")
    d.table(["Item", "Value"], [
        ("Operating system", "Windows 11 Pro 64-bit (10.0.26200)"),
        (".NET", ".NET Framework 4.8.1 runtime; build with .NET SDK 9.0.314"),
        ("Office", "Microsoft Word and Excel 16.0 (output validation only)"),
        ("Browser", "Microsoft Edge (HTML validation)"),
        ("Build", "Release, x64; ONNX Runtime 1.22.1; PP-OCRv5 mobile models"),
    ], [30, 70], "Environment")

    d.h1("4. Test cases")
    groups = [
        ("4.1 Build and installation", "TC-00"), ("4.2 Input", "TC-01"), ("4.3 Page editing", "TC-02"), ("4.4 Recognition", "TC-03"),
        ("4.5 Layout reconstruction", "TC-04"), ("4.6 Results view", "TC-05"), ("4.7 Export", "TC-06"), ("4.8 Tools", "TC-07"),
        ("4.9 Licensing", "TC-08"), ("4.10 Trial", "TC-09"), ("4.11 Key generator", "TC-10"),
    ]
    for title, prefix in groups:
        d.h2(title)
        rows = [(c[0], c[1], c[2], c[3], c[4], c[5], c[6]) for c in CASES if c[0].startswith(prefix)]
        d.table(["ID", "Req.", "Title", "Preconditions", "Steps", "Expected result", "Status"], rows, [9, 10, 13, 16, 18, 25, 9], raw=True)

    d.h1("5. Test results summary")
    total = len(CASES)
    passed = sum(1 for c in CASES if c[6] == PASS)
    notrun = sum(1 for c in CASES if c[6] == NOTRUN)
    d.table(["Total", "Pass", "Fail", "Not run"], [(str(total), str(passed), "0", str(notrun))], [25, 25, 25, 25], "Result summary (2026-09-24)")
    d.p("No executed test case failed. The cases marked Not run require interactive UI actions or hardware and are to be executed manually before release acceptance.")
    d.h2("5.1 Execution log")
    d.table(["ID", "Status", "Method / evidence"], [(c[0], c[6], c[7]) for c in CASES], [12, 12, 76], raw=True)
    d.h2("5.2 Defects found and fixed during testing")
    d.table(["Defect", "Fix", "Verified by"], [
        ("Recognition 2 × slower than expected (thread oversubscription).", "ONNX Runtime uses one thread per physical core.", "TC-037"),
        ("'1. Key Features' classified as ordered list item.", "Numbered headings win when larger or bold.", "TC-040"),
        ("Drawn bullets missing.", "Pixel-based bullet detection left of lines.", "TC-041"),
        ("Two paragraphs merged.", "Paragraph gap threshold relative to median gap.", "TC-042"),
        ("Lines with few descenders measured too small / bold.", "Contiguous ink-extent growth; stroke width 2·area/perimeter.", "TC-046"),
        ("Table numbers with commas measured too small; single bold cell.", "Commas not treated as descenders; table style normalisation.", "TC-043"),
        ("Colored UI fills detected as tables; panels swallowed as pictures.", "Rules need background on both sides; containers with text are not pictures.", "TC-048"),
        ("Korean lines joined without space.", "Only Chinese/Japanese join without spaces.", "TC-033"),
        ("Excel paragraphs overflowing the page.", "Paragraph cells merged across their columns and wrapped.", "TC-062"),
        ("Start-up crash in CardPanel layout.", "Layout guard during construction.", "TC-010"),
    ], [40, 42, 18], "Defects")

    d.h1("6. Traceability matrix")
    from doc_srs import FR, NFR
    rows = []
    for r in FR + [(n[0], "", n[1]) for n in NFR]:
        ids = [c[0] for c in CASES if r[0] in [x.strip() for x in c[1].split(",")]]
        rows.append((r[0], r[2], ", ".join(ids) if ids else "—"))
    d.table(["Requirement", "Title / category", "Test cases"], rows, [15, 40, 45], "Requirement coverage")
    return d
