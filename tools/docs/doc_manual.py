from docbuilder import Doc


def build():
    d = Doc("User Manual", "Convert scans, photos and PDFs into editable Word, Excel and HTML documents", "FOCR-UM-001",
            "This manual explains how to install, activate and use Falcon OCR 1.0 — from the first start to batch conversion and the "
            "command line. No technical knowledge is required.", chapter_breaks=False)

    # ================================================================== 1
    d.h1("1. Welcome to Falcon OCR")
    d.p("Falcon OCR turns scanned documents, photos of pages, image files and PDF files into documents you can edit. "
        "It reads the text (optical character recognition, OCR) and rebuilds the page as it was — headings, paragraphs, columns, "
        "lists, tables, pictures, font sizes, bold text and colors — and saves it as a Word document, an Excel workbook or a web page. "
        "Everything happens on your own computer: Falcon OCR never connects to the internet, so confidential documents stay where they are.")
    d.h2("1.1 What you can do")
    d.ul(["Open **PDF files**, **images** (PNG, JPG, BMP, GIF, TIFF …), **multi-page TIFFs** and **image sequences** (several images that form one document).",
          "Add pages from the **clipboard** or a **scanner**, or drag files into the window; **rotate** and **crop** pages.",
          "Recognize **English, Chinese, Japanese and Russian** text — completely offline.",
          "Check the result **side by side** with the original, correct words and change fonts, sizes and paragraph styles.",
          "**Export** to Word (.docx), Excel (.xlsx), HTML or plain text — as an editable document or as an exact copy of the page.",
          "Convert many files with **Batch Process**, grab text with **Quick OCR**, or automate with the **command line**.",
          "Work in an **English, Chinese or Japanese** user interface."])
    d.h2("1.2 How it works — in four steps")
    d.table(["Step", "What you do", "Where"], [
        ("1. Add", "Add a PDF, images or a scan.", "Toolbar: Add Files, Scan, From Clipboard"),
        ("2. Set", "Choose the language, document type and output format.", "Right-hand panel"),
        ("3. Recognize", "Click Recognize and check the result next to the original.", "Toolbar: Recognize (F5)"),
        ("4. Export", "Click Export — the document opens in Word, Excel or your browser.", "Export button (Ctrl+E)"),
    ], [15, 50, 35], "Workflow")
    d.p("Chapters 2–4 get you started, chapters 5–11 explain the workspace, and chapter 12 contains step-by-step tutorials for typical jobs. "
        "The numbered orange markers in the figures correspond to the numbered rows of the table next to each figure.")

    # ================================================================== 2
    d.pagebreak()
    d.h1("2. Installation")
    d.h2("2.1 System requirements")
    d.table(["Item", "Requirement"], [
        ("Operating system", "Windows 10 or Windows 11, 64-bit"),
        (".NET Framework", "4.7 or later (part of Windows 10 version 1703 and later, and of Windows 11)"),
        ("Memory / disk", "4 GB RAM (8 GB recommended for large PDFs); 200 MB for the application"),
        ("Screen", "At least 1280 × 768 (the window cannot be smaller than 1100 × 700)"),
        ("Optional", "Word / Excel or compatible software to open exports; a WIA-compatible scanner"),
        ("Internet", "Not required"),
    ], [25, 75], "System requirements")
    d.h2("2.2 Installing, updating, uninstalling")
    d.ol(["Copy the **Falcon OCR** folder (supplied by your vendor or administrator) to a location of your choice, e.g. C:\\Program Files\\Falcon OCR.",
          "Double-click **FalconOcr.exe**. Tip: right-click it and choose *Send to → Desktop (create shortcut)*.",
          "No installer and no administrator rights are needed. Keep all files together — the models and lang folders and the DLL files are required. "
          "The folder also contains **falcon-ocr.exe**, the command-line tool (chapter 20)."])
    d.p("To **update**, replace the contents of the folder with the new version; your settings, history and license are stored in your user profile and are kept. "
        "To **uninstall**, delete the folder, and %LOCALAPPDATA%\\FalconOCR if you also want to remove settings, history and license (chapter 22).")
    d.tip("Drag a PDF or image onto FalconOcr.exe (or use *Open with*) to start Falcon OCR with that file already added.")

    # ================================================================== 3
    d.h1("3. Getting started")
    d.h2("3.1 The first start and the trial")
    d.p("When you start Falcon OCR for the first time without a license, a free **7-day trial** with all features begins and the **Activate Falcon OCR** window appears.")
    d.img("um-activation-steps.png", "The activation window during the trial", 400)
    d.table(["#", "Element", "What it does"], [
        ("1", "Trial status", "Remaining days and end date, e.g. *Trial version — 5 of 7 days remaining (ends 2026-09-29)*."),
        ("2", "Machine code + Copy", "The code of this computer, needed only for computer-bound licenses (chapter 4)."),
        ("3", "License key box", "Paste the key you received."),
        ("4", "Load license file…", "Reads the key from a .lic, .key or .txt file."),
        ("5", "Activate", "Checks the key and activates Falcon OCR."),
        ("6", "Continue Trial", "Starts Falcon OCR in trial mode (only while the trial runs)."),
        ("7", "Exit", "Closes Falcon OCR."),
    ], [6, 28, 66], "Activation window")
    d.p("The window appears at every start until a license is entered. In the main window the title bar shows an amber badge **TRIAL · n days left — Activate now**, "
        "and a notification below the toolbar shows the remaining days. Click the badge or **Activate now** to activate; ✕ hides the notification until the next start.")
    d.img("trial-badge.png", "Trial badge in the title bar", 480)
    d.p("After 7 days *Continue Trial* is no longer offered; Falcon OCR can only be used with a license key.")
    d.img("activation-expired.png", "Activation window after the trial", 360)
    d.note("Setting the clock back or editing the trial information does not extend the trial — it ends it immediately "
           "(*The trial information on this computer is invalid (modified data or system clock set back).*).")
    d.h2("3.2 Your first conversion in five minutes")
    d.ol(["Start Falcon OCR and click **Continue Trial** (or activate first). The workspace (**Home**) opens.",
          "Click **Add Files → Add files (PDF, images)…** and open a printed page — a letter or a report. It appears in the **Files** list and under **Source Page**.",
          "On the right, check **Language** and keep **Document Type** at *Editable Document (Recommended)*.",
          "Click **Recognize** (F5). After a few seconds the rebuilt page appears under **Recognized Text**, next to the original.",
          "Click lines to compare them with the original; double-click a line to correct a word, then press Enter.",
          "Keep **Microsoft Word (.docx)** and click the green **Export** button. Word opens the document, saved in *Documents\\Falcon OCR Output*."])
    d.img("main-loaded.png", "A page has been added — Recognized Text still says “Click Recognize to convert this page”", 600)

    # ================================================================== 4
    d.pagebreak()
    d.h1("4. Activation and license management")
    d.h2("4.1 License types")
    d.p("A key is either **computer-bound** (works only on the computer whose machine code was sent to the vendor — *this computer only*) or valid on **any computer**, "
        "and either **perpetual** or **time-limited** (*valid until 2027-09-24*).")
    d.h2("4.2 Getting a license key")
    d.ol(["Purchase a license from your vendor.",
          "For a computer-bound license the vendor needs your **machine code** — four groups of four characters, e.g. *P9BB-82PR-DS2C-1R5Y*. "
          "Click **Copy** in the activation window (or **Copy machine code** in *Settings → License*, or run `falcon-ocr --machine-code`) and e-mail it to your vendor.",
          "You receive a **license key** (a long text starting with FOCR-) or a **license file** (.lic)."])
    d.h2("4.3 Activating step by step")
    d.ol(["Open the activation window: at start-up, with the trial badge or **Activate now**, or via *Settings → License → Change license key…*.",
          "Paste the whole key with Ctrl+V (line breaks and spaces do not matter), or click **Load license file…** and select the .lic file.",
          "Click **Activate**.",
          "The confirmation *Thank you — Falcon OCR is activated.* shows the licensee and validity, e.g. *Licensed to ACME Ltd · serial 1001 · perpetual · this computer only*.",
          "The trial badge and notification disappear; from now on Falcon OCR starts directly."])
    d.p("If the key is rejected, a red message appears below the box (4.6); the key stays in the box so you can correct it. "
        "*Settings → License* always shows the current state (✓ licensed, ⏳ trial, ✗ invalid) and your machine code.")
    d.h2("4.4 Moving to a new computer")
    d.ol(["Copy the Falcon OCR folder to the new computer.",
          "Key for **any computer**: enter the same key there.",
          "**Computer-bound** key: send the new computer's machine code to your vendor with your serial number, and activate with the new key.",
          "On the old computer delete %LOCALAPPDATA%\\FalconOCR\\license.key if the license must no longer be used there."])
    d.note("The machine code stays the same when you update Windows or Falcon OCR, but changes when Windows is reinstalled.")
    d.h2("4.5 Several users and company-wide deployment")
    d.p("The activated key is saved per Windows user in %LOCALAPPDATA%\\FalconOCR\\license.key; other users of the same computer activate with the same key. "
        "Administrators can deploy a license for everyone by placing a text file **license.key** containing the key next to FalconOcr.exe or in C:\\ProgramData\\FalconOCR.")
    d.h2("4.6 License messages")
    d.table(["Message", "What to do"], [
        ("The license key is not in a valid format. Check that it was copied completely.", "Copy the whole key again or use the .lic file."),
        ("The license key is not genuine.", "The key was changed or is not from your vendor. Request a new key."),
        ("This license key was issued for a different computer.", "Send your current machine code to the vendor."),
        ("The license expired on …", "Ask your vendor for a renewal."),
        ("The system clock is set earlier than the last use of the application.", "Correct the date and time of your computer."),
        ("The 7-day trial ended on …", "Enter a license key to continue."),
    ], [50, 50], "License messages")

    # ================================================================== 5
    d.pagebreak()
    d.h1("5. A tour of the main window")
    d.img("um-main-callouts.png", "The areas of the main window", 640)
    d.table(["#", "Area", "Purpose"], [
        ("1", "Title bar", "Drag to move the window; double-click to maximise or restore."),
        ("2", "Trial badge", "Only in the trial; click to activate."),
        ("3", "Navigation bar", "Home (workspace), OCR (Quick OCR), Batch Process, History, Settings."),
        ("4", "Toolbar", "Document and page commands (5.1); Settings and Help on the right."),
        ("5", "Trial notification", "Only in the trial: remaining days, Activate now, ✕."),
        ("6", "Files / Thumbnails", "Your documents, or the pages of the selected document."),
        ("7", "Source Page", "The original page with zoom and page controls."),
        ("8", "Recognized Text / Original Image", "The rebuilt page / the original with the detected areas."),
        ("9 / 10", "OCR Settings / Output Format", "Recognition options; format and output folder."),
        ("11", "Export", "Saves and opens the current document."),
        ("12", "Status bar", "Progress and messages; files, selection and output format."),
        ("13 / 14", "Collapse / Hide panel", "Shrink the navigation bar / hide the Files panel."),
    ], [8, 30, 62], "Main window areas")
    d.p("Resize the window by dragging an edge. Its size and position are remembered; on the first start it opens maximised. "
        "Documents you add to Home stay there while you visit the other screens.")
    d.h2("5.1 The toolbar")
    d.img("um-toolbar.png", "The toolbar of the workspace", 580)
    d.table(["#", "Button", "What it does", "Keys"], [
        ("1", "Add Files", "Add files (PDF, images)… · Add images as one document (image sequence)… · Add folder as image sequence…", "Ctrl+O"),
        ("2", "Scan", "Scans a page with the Windows scanning dialog.", ""),
        ("3", "From Clipboard", "Adds a copied image or files copied in Explorer.", "Ctrl+V"),
        ("4", "Rotate", "Turns the current page 90° clockwise.", "Ctrl+R"),
        ("5", "Crop", "Keeps only a rectangle you drag.", "Esc cancels"),
        ("6", "Delete", "Removes the document (Thumbnails tab: the page).", "Del"),
        ("7", "Recognize", "Recognizes the document; becomes **Stop** while running.", "F5"),
        ("8", "Export", "Exports the document in the chosen format.", "Ctrl+E"),
    ], [5, 18, 61, 16], "Toolbar buttons")
    d.p("**Help** on the right shows a short summary of the workflow, the main shortcuts and your license state.")
    d.h2("5.2 Files, Thumbnails and the page views")
    d.img_grid([("um-files-panel.png", "Files panel: document (1), Thumbnails tab (2), Hide panel (3)"),
                ("files-panel-collapsed.png", "The panel hidden to a narrow strip")], width=190)
    d.p("Each entry in the **Files** list shows an icon, the name and the page count (*1 page*, *12 pages*, *3 images*), plus *• recognized* or *• 2/5 done*. "
        "The **Thumbnails** tab shows all pages of the selected document; a ✓ marks recognized pages.")
    d.img("um-viewer-bar.png", "Zoom and page controls above the Source Page", 540)
    d.table(["#", "Control", "Description"], [
        ("1 / 2", "Zoom out / in", "20 % steps (also Ctrl+Minus / Ctrl+Plus, Ctrl+mouse wheel)."),
        ("3", "Zoom box", "Fit width, Fit page, 50–300 %, or type a value and press Enter."),
        ("4 / 5", "Fit width / Fit page", "Fit the page to the width / show the whole page."),
        ("6", "Rotate page 90°", "Same as Rotate in the toolbar."),
        ("7", "Pan (hand tool)", "Dragging moves the page instead of selecting lines. The middle mouse button always pans."),
        ("8–10", "Previous / page / next", "Page indicator, e.g. *2 / 12* (also PgUp / PgDn)."),
    ], [10, 25, 65], "Source Page controls")
    d.p("The footer shows the page name, size and resolution, e.g. *report.png · 1654 × 2338 px · 200 dpi*, plus *rotated 90°* or *cropped*. "
        "Below the result a status line reports e.g. *Text recognition completed — 54 lines, 16 blocks, 100% confidence (5.4s).*, "
        "or *Text layer read from PDF — …* for digital PDFs.")
    d.h2("5.3 The settings panel and status bar")
    d.img("um-settings-panel.png", "OCR Settings and Output Format", 230)
    d.table(["#", "Setting", "Description"], [
        ("1", "Document Type", "Editable Document (Recommended), Exact Copy (keep positions), Plain Text (8.1)."),
        ("2", "Language", "The language of the document (8.2)."),
        ("3", "Layout Analysis", "Automatic, Single column or Text lines only (8.3)."),
        ("4", "Detect tables and columns", "Finds tables and multi-column text."),
        ("5", "Advanced Settings…", "Advanced recognition options (chapter 17)."),
        ("6", "Output format", "Word, Excel or HTML."),
        ("7", "Output Folder", "Where exports are saved; … opens a folder dialog."),
        ("8", "Export", "Exports the selected document."),
    ], [6, 28, 66], "Settings panel")
    d.p("Choices in this panel are saved immediately as your defaults. The status bar shows progress and messages on the left and "
        "*Total Files · Selected · Output* on the right.")
    d.img("um-status-bar.png", "Status bar: message (1), number of files (2), selection (3), output format (4)", 600)
    d.h2("5.4 Arranging the window")
    d.ul(["**« Collapse** at the bottom of the navigation bar shows only icons (point at one to see its name); **»** expands it.",
          "**« Hide panel** reduces the Files / Thumbnails panel to a strip; **»** on the strip shows it again.",
          "Drag the dotted grip at the left edge of the settings panel to change its width (280–640 pixels), or the gap between the two page views.",
          "Falcon OCR remembers these choices."])
    d.img("um-compact-layout.png", "Most room for the pages: navigation bar collapsed (1) and Files panel hidden (2)", 600)

    # ================================================================== 6
    d.pagebreak()
    d.h1("6. Adding documents")
    d.p("A **document** is one entry in the Files list — a PDF, an image, a multi-page TIFF or an image sequence. Each is recognized and exported separately.")
    d.h2("6.1 Files and PDFs")
    d.p("Click **Add Files → Add files (PDF, images)…** (Ctrl+O), select one or more files and click **Open**.")
    d.table(["Type", "Extensions", "Notes"], [
        ("PDF", ".pdf", "Digital PDFs are read from their text layer; scanned pages are recognized."),
        ("Images", ".png .jpg .jpeg .jpe .bmp .dib .gif", "One page each. Phone photos are turned upright automatically."),
        ("TIFF", ".tif .tiff", "Multi-page TIFFs become one document with one page per frame."),
        ("Other", ".ico .emf .wmf", "Accepted when dropped or pasted, or with *All files*."),
    ], [15, 38, 47], "Supported input files")
    d.p("Files that cannot be opened are listed in *Some files could not be opened:* with the reason; the others are still added.")
    d.h2("6.2 Image sequences")
    d.p("Pages scanned or photographed as one image each can be combined into one document — and one exported file:")
    d.img("um-sequence-pages.png", "Three page images that belong to one document", 520)
    d.ul(["**Add images as one document (image sequence)…** — select all page images; the document is named e.g. *Scans (3 images)*.",
          "**Add folder as image sequence…** — all images directly in the folder become one document named after the folder; PDFs in it are added separately.",
          "Pages are sorted by file name the natural way (page2 before page10)."])
    d.h2("6.3 Clipboard, scanner, drag and drop")
    d.ul(["**From Clipboard** or Ctrl+V adds a copied image (e.g. a Win+Shift+S screenshot) as *Clipboard_01*, *Clipboard_02* …, or files copied in Explorer.",
          "**Scan** opens the Windows scanning dialog (WIA). Choose paper source and color, click **Scan**; the page is saved in %LOCALAPPDATA%\\FalconOCR\\scans and added.",
          "Drag files or folders onto the Files list or a page view. A dropped folder becomes an image sequence."])
    d.tip("For many pages, save them with your scanner's own software into one folder and add the folder as an image sequence.")
    d.h2("6.4 Managing documents")
    d.p("Right-click a document for **Recognize**, **Recognize all documents**, **Export**, **Export as…** (choose name and folder), **Rename…** (also changes the export file name), "
        "**Open containing folder**, **Remove** and **Remove all**. Removing never deletes the original file.")
    d.note("The workspace is not saved when you close Falcon OCR. Export what you want to keep; the History (chapter 15) remembers what you converted.")

    # ================================================================== 7
    d.h1("7. Preparing pages")
    d.h2("7.1 Before you start: preparing originals")
    d.p("Most recognition problems are decided before the page reaches Falcon OCR. Go through this checklist once for every new kind of original:")
    d.table(["Check", "Why it matters", "In Falcon OCR"], [
        ("Is it a digital PDF?", "PDFs made from Word or a web page contain their text. It is read exactly and in less than a second per page.", "Keep *PDF input: Use the PDF text layer*; the status line then says *Text layer read from PDF*."),
        ("Resolution at least 200 dpi, ideally 300", "Small letters need enough pixels; below 200 dpi, e and c or 1 and l get confused.", "The Source Page footer shows the dpi, e.g. *200 dpi*. Raise *PDF rendering resolution* for scanned PDFs with small print."),
        ("Grayscale or color, not black-and-white", "Thin strokes and light-gray text disappear in pure black-and-white scans.", "Colors are kept in the export unless you switch off *Keep text, fill and page colors*."),
        ("Upright and straight", "Sideways pages cannot be read; strongly skewed lines are read less accurately.", "**Rotate** (Ctrl+R) before recognizing; rescan strongly skewed pages."),
        ("Only the page, no background", "Desk, fingers, shadows or the opposite book page become pictures or noise.", "**Crop** the page."),
        ("One language per document", "The recognition model is chosen by the Language setting.", "Choose the language; English words inside Chinese, Japanese or Russian text are read too."),
        ("Pages in the right order", "Image sequences are sorted by file name.", "Name files page01, page02 …; check the Thumbnails tab."),
        ("Only the pages you need", "Every page costs time and ends up in the export.", "Remove pages in the Thumbnails tab with **Delete**."),
    ], [24, 40, 36], "Checklist for originals")
    d.h2("7.2 Working with pages")
    d.ul(["**Browse** — the Thumbnails tab, ‹ › or PgUp / PgDn.",
          "**Rotate** (Ctrl+R) — 90° clockwise per click, for pages scanned sideways or upside down.",
          "**Crop** — the page shows *Drag to select the area to keep — Esc to cancel*. Drag a rectangle; everything outside is removed. "
          "Clicking Crop on a cropped page asks *Yes = crop further, No = restore the full page*.",
          "**Remove a page** — in the Thumbnails tab, **Delete** removes the current page after a confirmation."])
    d.tip("Crop away desk, fingers or the opposite book page on photos — dark borders can be mistaken for pictures or table lines.")
    d.note("Rotating or cropping a recognized page removes its result; recognize it again afterwards. Your original file is never changed.")

    # ================================================================== 8
    d.pagebreak()
    d.h1("8. Choosing the settings")
    d.h2("8.1 Document type")
    d.table(["Document type", "Result", "Choose it for"], [
        ("Editable Document (Recommended)", "Flowing text with paragraphs, Word heading styles, columns, lists, tables and pictures.", "Text you want to edit: letters, reports, articles, contracts, books."),
        ("Exact Copy (keep positions)", "Every line, table and picture at its original position; each line is a frame fitted to the original width.", "Forms, flyers, posters, certificates, complex layouts."),
        ("Plain Text", "Only the paragraphs in reading order.", "Text for other programs or databases."),
    ], [24, 42, 34], "Document types")
    d.p("The document type applies to Word and HTML. In Excel, tables are always real cells and text is placed on a grid derived from the page.")
    d.h3("Worked example: one page, three document types")
    d.p("The sample page below — a quarterly report with a title, an italic subtitle, a bulleted list, a ruled table, two columns of text, a picture and a red contact line — "
        "was recognized once and exported to Word three times, changing only the document type.")
    d.img_grid([("sample-input.png", "The original page"), ("export-word.png", "Editable Document"), ("export-exact.png", "Exact Copy")], width=200, cols=3)
    d.table(["Element of the page", "Editable Document", "Exact Copy", "Plain Text"], [
        ("Title “Quarterly Operations Report”", "Word Heading 1, in its color", "A frame at the original position and size", "A plain paragraph"),
        ("Numbered headings (1. Key Features …)", "Heading 2 — usable for a table of contents", "Positioned lines", "Plain paragraphs"),
        ("Bulleted list", "Word list paragraphs with bullets", "Lines with their bullets at the original indent", "Lines starting with •"),
        ("Table “Results by Region”", "Word table with borders and the header fill", "The table at its original position", "One text line per table row"),
        ("Two-column discussion", "A real two-column Word section; text flows from one column to the next", "Every line where it was", "Paragraphs in reading order: left column, then right"),
        ("Picture", "Inline picture", "Picture at its original position", "Left out"),
        ("Red contact line", "Red text", "Red text at the bottom of the page", "Black text"),
        ("Best for", "Editing and reusing the content", "Printing or archiving a faithful copy", "Copying the words into other programs"),
    ], [24, 27, 26, 23], "The same page in the three document types")
    d.p("Rule of thumb: start with **Editable Document**. Switch to **Exact Copy** only when the result must look like the original more than it must be easy to edit — changing the document type does not require recognizing the page again, "
        "because it only affects the export.")
    d.h2("8.2 Language")
    d.p("Choose the language of the document — it makes the biggest difference for non-Latin scripts.")
    d.img("um-language-samples.png", "Sample pages in Chinese, Japanese and Russian", 460)
    d.table(["Language", "Also recognizes", "Default font"], [
        ("English", "Latin letters, digits, punctuation", "Calibri"),
        ("Chinese (中文)", "Simplified and Traditional Chinese, with English", "Microsoft YaHei"),
        ("Japanese (日本語)", "Kanji, Hiragana, Katakana, with English", "Yu Gothic"),
        ("Russian (Русский)", "Cyrillic (also Ukrainian, Belarusian), with English", "Calibri"),
    ], [25, 50, 25], "Recognition languages")
    d.note("Korean is not offered in Falcon OCR 1.0. The model of a newly selected language is loaded in the background, so the next recognition may take a moment longer.")
    d.h2("8.3 Layout analysis")
    d.table(["Option", "What happens", "Use it for"], [
        ("Automatic", "Columns, headings, paragraphs, lists, tables and pictures are detected.", "Normal documents (default)."),
        ("Single column", "The page is read top to bottom as one column.", "One-column pages that are wrongly split."),
        ("Text lines only", "Every line becomes its own paragraph.", "Address lists, receipts, labels."),
    ], [20, 45, 35], "Layout analysis")
    d.p("**Detect tables and columns** is on by default; switch it off if text is wrongly placed in tables or columns.")
    d.h2("8.4 Output format and folder")
    d.table(["Format", "Best for", "Details"], [
        ("Word (.docx)", "Letters, reports, books, forms", "Heading styles (for a table of contents), real columns, tables with merged cells and fills, pictures."),
        ("Excel (.xlsx)", "Tables, invoices, statements", "One worksheet per page (*Page 1*, *Page 2* …); tables as real cells; numbers can be calculated."),
        ("HTML (.html)", "Publishing, archives, e-mail", "One self-contained file with embedded pictures."),
        ("Plain text (.txt)", "Further processing", "UTF-8 text; pages separated by a form feed."),
    ], [18, 27, 55], "Output formats")
    d.p("The panel offers Word, Excel and HTML. Plain text is chosen as *Default output format* in Settings (status bar: *Output: TEXT*), in Batch Process or with `-f txt`. "
        "The **Output Folder** is *Documents\\Falcon OCR Output* by default and is created if necessary.")

    # ================================================================== 9
    d.h1("9. Recognizing")
    d.ol(["Select the document and click **Recognize** (F5).",
          "The **current page is processed first** so you can check it right away. The status bar shows e.g. *Recognizing report — Page 3 (1/12)* and a progress bar; finished pages appear immediately.",
          "At the end the status bar says e.g. *Text recognition completed — 12 page(s) in 41.3s.*"])
    d.p("Click **Stop** to cancel; finished pages keep their results, and Recognize continues with the rest later. "
        "A dense A4 page takes about 5–6 seconds; digital PDF pages less than a second. The first recognition after start-up takes a little longer while the models load.")
    d.p("**Digital PDFs** contain their text (a *text layer*). Falcon OCR reads it exactly, with the real fonts, sizes and colors, and uses OCR only for scanned pages. "
        "If a PDF gives garbled text, choose *Always run OCR* (chapter 17).")
    d.ul(["**Recognize again** with new settings: click Recognize on a finished document and confirm *… is already recognized. Recognize it again with the current settings?*",
          "**One page**: **⋯ → Recognize this page again** in the formatting bar.",
          "**Automatically**: *Settings → Recognize automatically when files are added*."])

    # ================================================================== 10
    d.pagebreak()
    d.h1("10. Checking and editing the result")
    d.img("main-selected.png", "The selected line is highlighted in the original and in the result", 620)
    d.h2("10.1 Side by side")
    d.p("**Recognized Text** shows the page rebuilt from the recognized text at the same positions as the original. Both views always show the same area at the same zoom — "
        "scroll or zoom one and the other follows. Click a line in either view: it is outlined in both, and the other view scrolls to it.")
    d.img("um-selected-zoom.png", "The heading “2. Results by Region” selected in the original (1) and in the result (2)", 600)
    d.h2("10.2 Correcting text")
    d.ol(["Characters recognized with less than 75 % certainty are highlighted in **yellow** — check these first.",
          "Double-click the line (or select it and press F2 or Enter) and correct the text.",
          "Press **Enter** to keep the change or **Esc** to cancel. Corrected lines get a dashed blue underline and are exported as corrected."])
    d.h2("10.3 The formatting bar")
    d.img("um-format-bar.png", "Formatting bar with a heading selected", 540)
    d.table(["#", "Control", "What it does"], [
        ("1", "Style", "Normal, Heading 1–3 or List item. Headings become Word heading styles and HTML h1–h3."),
        ("2", "Font", "Font of the selected line; pick one or type a name and press Enter."),
        ("3", "Size", "Pick 8–72 pt or type a size such as 10.5 (4–200) and press Enter."),
        ("4–6", "B, I, U", "Bold, italic, underline."),
        ("7 / 8", "Bulleted / Numbered list", "Makes the paragraph a list item; numbers continue from the item above."),
        ("9", "⋯ More", "Highlight uncertain characters · Copy page text · Copy document text · Recognize this page again."),
    ], [7, 22, 71], "Formatting bar")
    d.p("Formatting applies to the **whole paragraph** of the selected line so exports stay consistent; inside a table only the selected line changes, and the style box is unavailable for tables and pictures. "
        "The font list starts with the common fonts (Calibri, Arial, Times New Roman, Segoe UI, Microsoft YaHei, Yu Gothic …), followed by all installed fonts. "
        "**Copy page text** / **Copy document text** put the recognized text on the clipboard.")
    d.h2("10.4 The Original Image view")
    d.p("The **Original Image** tab shows what Falcon OCR found: text lines in green, tables in blue and pictures in orange — useful when a table was not detected or a picture was treated as text.")
    d.img_grid([("main-overlay.png", "Original Image tab"), ("um-overlay-zoom.png", "Text lines (1), a table (2), a picture (3)")], width=290)

    # ================================================================== 11
    d.pagebreak()
    d.h1("11. Exporting")
    d.ol(["Choose the output format and document type on the right, then click **Export** (Ctrl+E).",
          "If pages are not recognized yet, Falcon OCR offers to recognize them. With some pages missing: *Yes = recognize them first, No = export only the recognized pages*.",
          "The file is saved in the output folder under the document's name; an existing file is never overwritten — *report (2).docx* is used instead.",
          "The status bar shows *Exported to …*, the export is added to the History, and the file opens."])
    d.p("**Export as…** in the Files context menu lets you choose name and folder. Switch off *Settings → Open the document after export* if files should not open automatically.")
    d.img_grid([("export-word.png", "Word — Editable Document"), ("export-exact.png", "Word — Exact Copy"),
                ("export-excel.png", "Excel — tables as real cells"), ("export-html.png", "HTML — web page")], width=280)
    d.p("For scanned pages the original font is unknown; Falcon OCR uses the language's default font or the one chosen in *Settings → Default font*. "
        "Size, bold and color are estimated from the image; italics are taken over from digital PDFs.")
    d.tip("If the exported file will be opened on another computer, use a font installed there — Calibri, Arial or Times New Roman are safe choices.")

    # ================================================================== 12
    d.pagebreak()
    d.h1("12. Step-by-step tutorials")
    d.h2("12.1 Convert a scanned PDF to Word")
    d.ol(["**Add Files → Add files (PDF, images)…** and open the PDF.",
          "In the **Thumbnails** tab check that all pages are upright; rotate sideways pages.",
          "Select the **Language** and keep **Editable Document (Recommended)**.",
          "Click **Recognize** and check the first page while the others are processed.",
          "Go through the pages with PgDn, correct yellow words, and set headings with the style box.",
          "Choose **Microsoft Word (.docx)** and click **Export**."])
    d.tip("For small print set *Advanced Settings → PDF rendering resolution* to 300 dpi before recognizing.")
    d.h2("12.2 Get the text of a photo or screenshot")
    d.ol(["Take a screenshot with Win+Shift+S or copy an image.",
          "Press **Ctrl+V** in the workspace; the image is added as *Clipboard_01*.",
          "For a photo, **Crop** away the background.",
          "Click **Recognize**, then **⋯ → Copy page text** — or export it."])
    d.h2("12.3 Combine page images into one document")
    d.ol(["Put the page images into one folder, named in order (page01.png, page02.png …).",
          "**Add Files → Add folder as image sequence…** and select the folder; one document with *n images* appears.",
          "Check the order in **Thumbnails** and remove unwanted pages.",
          "Click **Recognize** and **Export** — all pages end up in one file (in Excel: one worksheet per page)."])
    d.h2("12.4 Convert a table to Excel")
    d.ol(["Add the page and make sure **Detect tables and columns** is on.",
          "Click **Recognize** and check in the **Original Image** tab that the table has a blue frame.",
          "Correct wrong digits by double-clicking the cell line.",
          "Choose **Microsoft Excel (.xlsx)** and click **Export**. Tables become real cells, and numbers can be calculated."])
    d.tip("Tables with visible lines work best; for tables without lines, Excel still places the text on a grid of columns.")
    d.h2("12.5 Publish a page as HTML")
    d.ol(["Add and recognize the document and choose **HTML (.html)**.",
          "Choose **Editable Document** for a page that adapts to the browser window, or **Exact Copy** for a page that looks like the original.",
          "Click **Export**. The file contains its pictures, so you can e-mail it or put it on a web server as a single file."])
    d.h2("12.6 Convert a whole folder")
    d.ol(["Open **Batch Process** and click **Add folder…**.",
          "Leave *Treat each folder as one document* on if the images are pages of one document; switch it off if each image is a separate document.",
          "Choose format, document type, language and output folder, then click **Start**.",
          "At the end, answer **Yes** to open the output folder."])
    d.h2("12.7 Quick OCR to the clipboard")
    d.ol(["Copy an image or take a screenshot.",
          "Click **OCR** in the navigation bar and press **Ctrl+V**.",
          "Click **Copy text** and paste the text where you need it."])
    d.h2("12.8 Recognize Chinese, Japanese or Russian")
    d.ol(["Add the document and choose **Chinese (中文)**, **Japanese (日本語)** or **Russian (Русский)** as **Language**; English words are recognized as well.",
          "Click **Recognize** and export as usual. Word receives the right language setting for spelling and line breaking."])
    d.note("If the text shows as boxes on another computer, the font (e.g. Microsoft YaHei or Yu Gothic) is missing there.")
    d.h2("12.9 Recreate a form or flyer exactly")
    d.p("Recognize the page, choose **Exact Copy (keep positions)** and export to Word or HTML. Every line keeps its position; edit single words in the frames.")

    # ================================================================== 13
    d.pagebreak()
    d.h1("13. Quick OCR")
    d.p("Use **OCR** in the navigation bar when you just need the text of an image. Nothing is saved.")
    d.img("um-quickocr-callouts.png", "Quick OCR", 600)
    d.table(["#", "Element", "Description"], [
        ("1", "Open image…", "Opens an image or a PDF (first page)."),
        ("2", "Paste image", "Uses the clipboard image (Ctrl+V)."),
        ("3", "Copy text", "Copies the recognized text."),
        ("4", "Open in workspace", "Adds the opened file to Home for a full conversion."),
        ("5 / 6", "Image / Text", "The image with the detected lines (drop images here) / the editable text."),
    ], [8, 26, 66], "Quick OCR")
    d.p("An information line shows e.g. *12 lines · 97% confidence · 0.8s · English*. Quick OCR uses the language selected in the workspace. "
        "Open in workspace works for files; add a pasted image to Home with Ctrl+V there.")

    # ================================================================== 14
    d.h1("14. Batch processing")
    d.p("**Batch Process** recognizes and exports many documents unattended.")
    d.img("um-batch-callouts.png", "Batch Process", 600)
    d.table(["#", "Element", "Description"], [
        ("1", "Add files… / Add folder… / Clear list", "Fill or empty the list; you can also drag files and folders into it."),
        ("2", "Input list", "Input, type (PDF, PNG … or *Image sequence*) and status: Waiting, Recognizing…, Page 2/5, Done, Failed, Stopped."),
        ("3", "Treat each folder as one document", "On: a folder's images form one document. Off: every file is separate. PDFs are always separate."),
        ("4–6", "Output format, Document type, Language", "Word, Excel, HTML or Plain text; Editable / Exact Copy / Plain Text; the language for the whole batch."),
        ("7", "Output folder", "Double-click to browse."),
        ("8 / 9", "Start / Stop, progress", "Stop cancels after the current page."),
        ("10", "Log", "e.g. *✓ report (3 p., 4.2s) → report.docx* or *✗ …: error*."),
    ], [7, 33, 60], "Batch Process")
    d.p("The options start with your defaults; the advanced recognition options apply too. Only files directly inside a folder are included (no subfolders). "
        "Existing files are never overwritten. At the end the log shows *Finished: n succeeded, n failed* and Falcon OCR offers to open the output folder. "
        "Click **Start** again to retry — entries marked *Done* are skipped.")

    # ================================================================== 15
    d.h1("15. History")
    d.img("um-history-callouts.png", "History", 600)
    d.p("**History** lists the last 500 recognitions and exports with Time, Source, Output, Format (*Recognition*, DOCX, XLSX, HTML, TEXT), Pages, Language and Duration (5). "
        "Select an entry and click **Open output** (1, or double-click), **Show in folder** (2) or **Open source in workspace** (3) — e.g. to export it again in another format. "
        "**Clear history** (4) removes all entries, not your files.")

    # ================================================================== 16
    d.pagebreak()
    d.h1("16. Settings reference")
    d.p("The **Settings** screen holds the defaults. Click **Save** to apply (*Settings saved.*).")
    d.img("um-settings-callouts.png", "Settings: General (1), Recognition (2), Export (3), Advanced recognition (4); Save (5), Restore defaults (6), Open data folder (7)", 600)
    d.table(["Option", "Default", "Description"], [
        ("Interface language", "Windows language", "English, 简体中文 or 日本語; after a restart (chapter 18)."),
        ("Default language", "English", "Recognition language."),
        ("Layout analysis", "Automatic", "Automatic (columns, tables, figures), Single column, Text lines only."),
        ("Detect tables and columns", "On", "Finds tables and columns."),
        ("Recognize automatically when files are added", "Off", "Starts recognition as soon as documents are added."),
        ("Highlight uncertain characters in the results view", "On", "Yellow highlighting."),
        ("Default font", "Automatic (by recognition language)", "Font for text whose original font is unknown (scans, images); applies to pages recognized afterwards."),
        ("Default output format", "Microsoft Word (.docx)", "Word, Excel, HTML or Plain text (.txt)."),
        ("Document type", "Editable Document", "Editable Document, Exact Copy or Plain Text."),
        ("Output folder", "Documents\\Falcon OCR Output", "Double-click to browse; empty = default."),
        ("Keep text, fill and page colors", "On", "Off: black text on white."),
        ("Include pictures in exported documents", "On", "Off: text and tables only."),
        ("Open the document after export", "On", "Opens exports automatically."),
        ("Advanced recognition", "—", "See chapter 17."),
        ("License", "—", "Status, machine code, **Change license key…**, **Copy machine code**."),
    ], [34, 24, 42], "All settings")
    d.p("**Restore defaults** fills in the default values (click Save to apply them); **Open data folder** opens %LOCALAPPDATA%\\FalconOCR.")

    # ================================================================== 17
    d.h1("17. Advanced recognition options")
    d.p("Available under **Advanced Settings…** in the workspace (OK, Cancel, **Defaults**) and in Settings. The defaults suit most documents; change one value at a time and recognize again to see the effect.")
    d.table(["Option", "Default (range)", "When to change it"], [
        ("PDF input", "Use the PDF text layer", "*Always run OCR (ignore embedded text)* if a PDF shows strange characters or missing words."),
        ("PDF rendering resolution (dpi)", "200 (100–600)", "300 for very small print; 150 for speed with large print."),
        ("Detector max. image side (px)", "2560 (960–6000)", "Higher for tiny text on large pages (posters, 600 dpi scans); lower to save time."),
        ("Text pixel threshold", "0.30 (0.05–0.90)", "Lower (0.2) for faint or light-gray text; higher if background patterns become text."),
        ("Text box threshold", "0.60 (0.10–0.95)", "Lower to keep faint lines; higher to drop noise."),
        ("Box expansion (unclip ratio)", "1.5 (1.0–3.0)", "1.8–2.0 if first or last letters or accents are cut off; lower if lines merge."),
        ("Minimum line confidence", "0.50 (0–0.95)", "Lines below this certainty are dropped. Lower for poor print; higher against garbage from stamps or logos."),
        ("CPU threads (0 = automatic)", "0 (0–64)", "E.g. 2 to keep the computer responsive during long jobs."),
        ("Correct upside-down text lines", "Off", "On for documents with rotated text blocks (slightly slower)."),
        ("Keep pictures and graphics as images", "On", "Off when a picture contains text you need."),
    ], [28, 20, 52], "Advanced recognition options")

    # ================================================================== 18
    d.pagebreak()
    d.h1("18. Interface language")
    d.p("The interface is available in **English**, **简体中文** and **日本語**. On the first start Falcon OCR follows the display language of Windows. To change it, "
        "open **Settings** (设置 / 設定), choose **Interface language** under **General**, click **Save** and answer **Yes** to "
        "*The interface language is changed after a restart. Restart Falcon OCR now?* The interface language is independent of the recognition language.")
    d.p("The table lists the most important labels in the three interface languages — useful when you support colleagues who work with another interface language, or when you follow this manual with a Chinese or Japanese interface.")
    d.table(["English", "简体中文", "日本語"], [
        ("Home · OCR · Batch Process · History · Settings", "主页 · OCR · 批量处理 · 历史记录 · 设置", "ホーム · OCR · 一括処理 · 履歴 · 設定"),
        ("Add Files · Scan · From Clipboard", "添加文件 · 扫描 · 从剪贴板", "ファイルを追加 · スキャン · クリップボードから"),
        ("Rotate · Crop · Delete", "旋转 · 裁剪 · 删除", "回転 · トリミング · 削除"),
        ("Recognize · Stop · Export · Export as…", "识别 · 停止 · 导出 · 导出为…", "認識 · 停止 · エクスポート · 名前を付けてエクスポート…"),
        ("Help", "帮助", "ヘルプ"),
        ("Files · Thumbnails · Hide panel · Collapse", "文件 · 缩略图 · 隐藏面板 · 收起", "ファイル · サムネイル · パネルを隠す · 折りたたむ"),
        ("Source Page · Recognized Text · Original Image", "源页面 · 识别文本 · 原始图像", "元のページ · 認識テキスト · 元の画像"),
        ("OCR Settings · Output Format", "OCR 设置 · 输出格式", "OCR 設定 · 出力形式"),
        ("Document Type: · Language: · Layout Analysis:", "文档类型： · 语言： · 版面分析：", "文書の種類： · 言語： · レイアウト解析："),
        ("Editable Document (Recommended)", "可编辑文档（推荐）", "編集可能な文書（推奨）"),
        ("Exact Copy (keep positions)", "精确副本（保持位置）", "完全コピー（位置を保持）"),
        ("Plain Text", "纯文本", "プレーン テキスト"),
        ("Detect tables and columns", "检测表格和分栏", "表と段組みを検出"),
        ("Output Folder:", "输出文件夹：", "出力フォルダー："),
        ("Quick OCR · Copy text", "快速 OCR · 复制文本", "クイック OCR · テキストをコピー"),
        ("Clear history", "清除历史记录", "履歴を消去"),
        ("Interface language: · Save · Restore defaults · Open data folder", "界面语言： · 保存 · 恢复默认值 · 打开数据文件夹", "表示言語： · 保存 · 既定値に戻す · データ フォルダーを開く"),
        ("Activate · Activate now · Continue Trial", "激活 · 立即激活 · 继续试用", "アクティベート · 今すぐアクティベート · 試用を続ける"),
        ("Change license key… · Copy machine code", "更改许可证密钥… · 复制机器码", "ライセンスキーを変更… · マシンコードをコピー"),
    ], [36, 30, 34], "Interface labels in English, Chinese and Japanese")
    d.img("um-ui-chinese.png", "The workspace with the Chinese interface", 580)
    d.img("ui-japanese.png", "The workspace with the Japanese interface", 580)

    # ================================================================== 19
    d.h1("19. Tips for the best recognition quality")
    d.ul(["Scan at **300 dpi** (200 minimum; 400–600 for print below 8 pt) in **grayscale or color** — black-and-white scans lose thin strokes.",
          "Place the page straight, close the lid and keep the glass clean; save as PNG or TIFF rather than heavily compressed JPEG.",
          "Photograph pages **straight from above** in **even light**, without shadows or flash reflections; keep the picture sharp and crop the background.",
          "Select the **correct language** and **rotate** sideways pages before recognizing.",
          "Keep *Use the PDF text layer* for digital PDFs — the text is then exact.",
          "Use **Exact Copy** for forms and flyers, **Editable Document** for text you want to edit.",
          "Check yellow characters first, and use the **Original Image** tab to see whether tables, columns and pictures were found."])

    # ================================================================== 20
    d.pagebreak()
    d.h1("20. Command line")
    d.p("**falcon-ocr.exe** converts files without opening the window — for scripts, scheduled tasks and document systems. It uses the same license or trial as the application.")
    d.code(["falcon-ocr <files or folders> [-l en|zh|ja|ru] [-f docx,xlsx,html,txt] [-o output-folder]",
            "           [--exact | --plain] [--seq] [--no-tables] [--ocr-only] [--dpi 300] [--cls] [--font NAME] [--dump]",
            "falcon-ocr --license <key>        activate",
            "falcon-ocr --machine-code         show the machine code"])
    d.table(["Option", "Meaning (default)"], [
        ("files or folders", "PDFs and images; a folder adds the supported files directly inside it."),
        ("-l", "Language: en, zh, ja, ru (en). Unknown codes fall back to English."),
        ("-f", "Formats, comma separated: docx, xlsx, html, txt (docx)."),
        ("-o", "Output folder (the folder of each source file)."),
        ("--exact / --plain", "Exact Copy / Plain Text (Editable Document)."),
        ("--seq", "All input images form one image-sequence document."),
        ("--no-tables / --ocr-only / --cls", "No table detection / ignore PDF text layers / correct upside-down lines."),
        ("--dpi N / --font NAME", "PDF rendering resolution (200) / font for text with unknown font."),
        ("--dump", "Prints the recognized layout; without -f no file is written."),
    ], [30, 70], "Command-line options")
    d.p("Output files are named after their source (report.pdf → report.docx); unlike the application, the command line **replaces** an existing file of the same name. "
        "Paths of written files go to the standard output, progress and messages to the error output.")
    d.code(["falcon-ocr D:\\Scans\\contract.pdf                         REM Word next to the original",
            "falcon-ocr invoice.pdf -f xlsx -o D:\\Out                 REM Excel into another folder",
            "falcon-ocr C:\\Inbox -f docx,html --exact                 REM every file, two formats",
            "falcon-ocr D:\\Scans\\Book --seq -l ru                     REM Russian page images, one document",
            "falcon-ocr screenshot.png -l zh -f txt                    REM Chinese to plain text",
            "falcon-ocr manual.pdf -l ja --ocr-only --dpi 300          REM Japanese PDF, broken text layer",
            "falcon-ocr D:\\Receipts --no-tables --plain -f txt        REM receipts as text",
            "falcon-ocr --license \"FOCR-07MG6-000WM-...\"              REM activate"])
    d.p("For a nightly conversion, create a task in the Windows **Task Scheduler** that runs falcon-ocr.exe with e.g. `D:\\Inbox -f docx -o D:\\Converted`, "
        "as the Windows user who activated Falcon OCR (or deploy license.key, chapter 4.5).")
    d.table(["Exit code", "Meaning"], [
        ("0", "Success."), ("1", "Usage error — no files given, or --help."), ("2", "Model files are missing."),
        ("3", "Not activated or trial ended; the machine code is printed."),
    ], [18, 82], "Exit codes")

    # ================================================================== 21
    d.h1("21. Keyboard and mouse shortcuts")
    d.table(["Keys", "Action"], [
        ("Ctrl+O", "Add files"), ("Ctrl+V", "Paste image or files (Home, Quick OCR)"), ("F5", "Recognize / stop"),
        ("Ctrl+E", "Export"), ("Ctrl+R", "Rotate page"), ("Del", "Remove document (Thumbnails: page)"),
        ("PgUp / PgDn", "Previous / next page"), ("Ctrl+Plus / Ctrl+Minus, Ctrl+wheel", "Zoom in / out"),
        ("Middle mouse button (drag)", "Pan the page"), ("Double-click, F2 or Enter", "Edit the selected line"),
        ("Enter / Esc (while editing)", "Keep / cancel the change"), ("Esc (crop mode)", "Cancel cropping"),
        ("Double-click the title bar", "Maximise / restore"),
    ], [40, 60], "Keyboard and mouse shortcuts")
    d.p("While you type in a text or combo box, Ctrl+V and Del work in that box as usual.")

    # ================================================================== 22
    d.h1("22. File locations and privacy")
    d.p("Falcon OCR processes everything on your computer — no online activation, no telemetry, no cloud. Documents are only read; results are written only where you export them. "
        "Workspace documents are kept in memory and are gone when you close Falcon OCR.")
    d.table(["Location (in %LOCALAPPDATA%\\FalconOCR unless stated)", "Content"], [
        ("settings.json", "Settings, window layout, interface language."),
        ("history.json", "History (file paths, format, pages, language, duration — no document content)."),
        ("license.key, license.state", "Your license key; date of the last use."),
        ("evaluation.dat (hidden), HKEY_CURRENT_USER\\Software\\FalconOCR", "Trial information."),
        ("scans\\", "Pages acquired from a scanner."),
        ("error.log", "Details of unexpected errors for your support contact."),
        ("Documents\\Falcon OCR Output", "Default export folder."),
    ], [50, 50], "Files stored by Falcon OCR")

    # ================================================================== 23
    d.pagebreak()
    d.h1("23. Troubleshooting")
    d.table(["Problem or message", "Solution"], [
        ("*Some OCR model files are missing from the installation* / *OCR models are missing*", "The application folder is incomplete. Copy the complete Falcon OCR folder again."),
        ("*Falcon OCR must run as a 64-bit process.*", "Use 64-bit Windows."),
        ("The application closes after the activation window", "The trial has ended and no key was entered — see chapter 4."),
        ("The window opens off-screen", "Press Win+Up, or delete settings.json in the data folder."),
        ("The interface language did not change", "Restart Falcon OCR."),
        ("*Some files could not be opened: … unsupported file type*", "Only PDFs and images can be added. Print other files to PDF."),
        ("A PDF cannot be opened", "It may be damaged or password-protected; remove the protection or print it to a new PDF."),
        ("*The clipboard does not contain an image.*", "Copy the image again; copied text is not an image."),
        ("*No scanner was found …* / *WIA is not available …*", "Connect the scanner and install its WIA driver, or scan with the scanner software and add the files."),
        ("Image-sequence pages in the wrong order", "Pages are sorted by file name; rename them (page01, page02 …)."),
        ("The text is wrong or empty", "Check the language; scan at 200–300 dpi; rotate sideways pages; lower the text thresholds."),
        ("Letters at word ends are missing", "Increase *Box expansion* to 1.8–2.0."),
        ("Faint text is missing / garbage from stamps", "Lower / raise *Text pixel threshold* and *Minimum line confidence*."),
        ("Two-column text is mixed", "Use Automatic with *Detect tables and columns*; for single-column pages choose Single column."),
        ("A table is not recognized", "Check the Original Image tab; tables with lines work best. Try Exact Copy or Excel."),
        ("Text is wrongly put into a table", "Switch off *Detect tables and columns*."),
        ("Headings are normal text", "Select the line and choose Heading 1–3."),
        ("A picture contains text I need", "Switch off *Keep pictures and graphics as images*."),
        ("A digital PDF gives strange characters", "*PDF input → Always run OCR*."),
        ("Recognition is slow / the PC is sluggish", "Lower PDF resolution or detector size; set *CPU threads* to 2–4."),
        ("*Cannot create the output folder: …*", "Choose another folder with …"),
        ("*Export failed.*", "The file may be open in Word or Excel; close it or use Export as…."),
        ("The export does not open", "Install Word/Excel or a compatible program, or open the file from the output folder."),
        ("Chinese or Japanese text shows as boxes", "Install the font on that computer or choose another default font."),
        ("*An unexpected error occurred …*", "Send %LOCALAPPDATA%\\FalconOCR\\error.log to your support contact."),
    ], [40, 60], "Troubleshooting")

    # ================================================================== 24
    d.h1("24. Frequently asked questions")
    for q, a in [
        ("Does Falcon OCR need the internet? Are my documents sent anywhere?", "No and no. Everything, including activation, works offline on your computer."),
        ("Can it read handwriting?", "It is designed for printed and typed text; handwriting is not recognized reliably."),
        ("Is my original file changed?", "Never. Rotation, cropping and corrections only affect the workspace and the exports."),
        ("Can I save my work and continue later?", "The workspace is not saved. Export the result; History lets you reopen the source quickly."),
        ("How do I export only some pages?", "Remove the other pages in the Thumbnails tab, or recognize only the pages you need and answer **No** when exporting."),
        ("How do I get several PDFs into one Word file?", "Merge them into one PDF first; images can be combined as an image sequence."),
        ("Can I restart the trial?", "No. It runs 7 days from the first start on a computer."),
        ("What happens when a time-limited license expires?", "Falcon OCR asks for a new key at the next start; settings and history are kept."),
    ]:
        d.h3(q)
        d.p(a)

    # ================================================================== A
    d.pagebreak()
    d.h1("Appendix A. Glossary")
    d.table(["Term", "Meaning"], [
        ("OCR", "Optical character recognition: reading text from images."),
        ("Document / page", "One entry in the Files list / one page of it."),
        ("Image sequence", "Several image files combined into one multi-page document."),
        ("Text layer", "Text stored inside a digital PDF."),
        ("Layout analysis", "Finding columns, paragraphs, headings, lists, tables and pictures."),
        ("Confidence", "How certain the recognizer is (0–100 %); uncertain characters are yellow."),
        ("dpi", "Dots per inch — the resolution of a scan; 300 dpi is ideal."),
        ("Editable Document / Exact Copy", "Export with flowing text / with everything at its original position."),
        ("WIA", "Windows Image Acquisition, the Windows scanner interface."),
        ("Machine code", "Code identifying your computer for a computer-bound license."),
        ("License key / file", "Text starting with FOCR- / a .lic file containing it."),
    ], [30, 70], "Glossary")

    # ================================================================== C (quick reference, placed before the vendor appendix)
    d.h1("Appendix B. Quick reference card")
    d.p("Frequently used workflows at a glance. Each row starts in the workspace (**Home**) unless stated otherwise.")
    d.table(["I want to …", "Do this", "Settings to check"], [
        ("Edit a scanned letter in Word", "Ctrl+O → F5 → correct yellow words → Ctrl+E", "Editable Document · Word · Language"),
        ("Reuse text from a digital PDF", "Ctrl+O → F5 (text layer, < 1 s per page) → Ctrl+E", "PDF input: Use the PDF text layer"),
        ("Copy the text of a screenshot", "OCR → Ctrl+V → Copy text", "Language (from the workspace)"),
        ("Turn a table into a spreadsheet", "Add → F5 → check the blue frame in Original Image → Excel → Ctrl+E", "Detect tables and columns on"),
        ("Make one document from page images", "Add Files → Add folder as image sequence… → F5 → Ctrl+E", "File names in page order"),
        ("Keep the look of a form or flyer", "F5 → Exact Copy → Ctrl+E", "Exact Copy · Word or HTML"),
        ("Publish a page on the intranet", "F5 → HTML → Ctrl+E → copy the single .html file", "Editable Document (adapts) or Exact Copy"),
        ("Convert a whole folder", "Batch Process → Add folder… → Start", "Treat each folder as one document"),
        ("Convert files every night", "Task Scheduler → falcon-ocr.exe D:\\Inbox -f docx -o D:\\Out", "License for the task's Windows user"),
        ("Export only some pages", "Thumbnails → Delete unwanted pages → Ctrl+E", "—"),
        ("Recognize one page again", "⋯ → Recognize this page again", "Change the setting first"),
        ("Send the machine code to the vendor", "Settings → License → Copy machine code", "—"),
        ("Enter a new license key", "Settings → License → Change license key… → paste → Activate", "—"),
        ("Get more room for the pages", "« Collapse and « Hide panel", "Remembered for the next start"),
    ], [28, 44, 28], "Frequently used workflows")
    d.table(["Most important keys", ""], [
        ("Ctrl+O / Ctrl+V", "Add files / paste image"), ("F5", "Recognize (again: Stop)"), ("Ctrl+E", "Export"),
        ("Ctrl+R", "Rotate"), ("PgUp / PgDn", "Page back / forward"), ("F2 · Enter · Esc", "Edit line · keep · cancel"),
    ], [35, 65], "Keys to remember")

    # ================================================================== B
    d.pagebreak()
    d.h1("Appendix C. For vendors: the License Key Generator")
    d.p("**FalconOcrKeyGen.exe** creates license keys. It is **not** part of the customer installation — keep the program and above all its signing key private. "
        "Keys are digitally signed; Falcon OCR contains only the public part of the key, so it can check keys but not create them.")
    d.img("um-keygen-callouts.png", "The License Key Generator", 440)
    d.table(["#", "Element", "Description"], [
        ("1", "Signing key", "New signing key…, Import backup…, Backup key…, Write public key to app…; the status warns if the key does not match the application build."),
        ("2 / 3", "Licensee, Machine code", "Customer name (required); the customer's code, e.g. P9BB-82PR-DS2C-1R5Y."),
        ("4", "Any computer", "Key not bound to a machine code."),
        ("5", "Expiry", "Perpetual, or a date (30 days / 1 year buttons)."),
        ("6", "Serial number", "Proposed automatically."),
        ("7 / 8", "Generate License Key; Copy key, Save as .lic file…, Open issued log", "Create and deliver the key; view all issued keys."),
        ("9", "Verify", "Shows licensee, serial, dates and binding of a key."),
    ], [8, 32, 60], "License Key Generator")
    d.ol(["**Once:** click **New signing key…**, save the offered **backup** offline, click **Write public key to app…** and rebuild Falcon OCR (build.ps1). "
          "A new signing key invalidates all previously issued keys once the application is rebuilt.",
          "**Per customer:** enter the licensee and machine code (or tick Any computer), choose the expiry and click **Generate License Key**.",
          "Send the key (**Copy key**) or the .lic file. Every key is logged in %APPDATA%\\FalconOcrKeyGen\\issued.csv."])
    d.code(["FalconOcrKeyGen --init [--force]  |  --backup <file>  |  --import <file>",
            "FalconOcrKeyGen --generate --name \"ACME Ltd\" [--machine P9BB-82PR-DS2C-1R5Y] [--days 365 | --expires 2027-09-24]",
            "FalconOcrKeyGen --verify <key>  |  --machine-code"])
    d.p("The signing key (%APPDATA%\\FalconOcrKeyGen\\signing.key) is encrypted for the Windows user who created it; on another PC or account use **Import backup…**.")
    return d
