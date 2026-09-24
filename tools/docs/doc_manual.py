from docbuilder import Doc


def build():
    d = Doc("User Manual", "Convert scans, photos and PDFs into editable Word, Excel and HTML documents", "FOCR-UM-001",
            "This manual explains how to install, activate and use Falcon OCR 1.0. No technical knowledge is required.", chapter_breaks=False)

    # ------------------------------------------------------------------ 1
    d.h1("1. Welcome to Falcon OCR")
    d.p("Falcon OCR turns scanned documents, photos of pages, image files and PDF files into documents you can edit. "
        "It reads the text (optical character recognition, OCR) and rebuilds the page as it was — headings, paragraphs, columns, "
        "bulleted lists, tables, pictures, font sizes, bold text and colors — and saves the result as a Word document, an Excel workbook or a web page.")
    d.h2("1.1 What you can do")
    d.ul(["Open **PDF files**, **single images** (PNG, JPG, BMP, GIF, TIFF …), **multi-page TIFFs** and **image sequences** (several images that form one document).",
          "Add pages from the **clipboard** or a **scanner**, or simply drag files into the window.",
          "**Rotate** and **crop** pages before recognition.",
          "Recognize text in **English, Chinese, Japanese, Korean and Russian** — completely offline; your documents never leave your computer.",
          "Check the result **side by side** with the original, correct words and change paragraph styles.",
          "**Export** to Microsoft Word (.docx), Microsoft Excel (.xlsx) or HTML — as an editable document or as an exact copy of the page.",
          "Convert many files at once with **Batch Process**, or grab text quickly with **Quick OCR**."])
    d.h2("1.2 How it works — in four steps")
    d.table(["Step", "What you do", "Where"], [
        ("1. Add", "Add a PDF, images or a scan.", "Toolbar: Add Files, Scan, From Clipboard"),
        ("2. Set", "Choose the language, document type and output format.", "Right-hand panel"),
        ("3. Recognize", "Click Recognize and check the result next to the original.", "Toolbar: Recognize (F5)"),
        ("4. Export", "Click Export — the document opens in Word, Excel or your browser.", "Export button (Ctrl+E)"),
    ], [15, 50, 35], "Workflow")

    # ------------------------------------------------------------------ 2
    d.h1("2. Installation")
    d.h2("2.1 System requirements")
    d.table(["Item", "Requirement"], [
        ("Operating system", "Windows 10 or Windows 11, 64-bit"),
        (".NET Framework", "4.7 or later (already part of Windows 10 version 1703 and later, and of Windows 11)"),
        ("Memory", "4 GB RAM (8 GB recommended for large PDFs)"),
        ("Disk", "200 MB for the application, plus space for your documents"),
        ("Optional", "Microsoft Word / Excel (or compatible software) to open exported files; a WIA-compatible scanner"),
        ("Internet", "Not required — Falcon OCR works completely offline"),
    ], [25, 75], "System requirements")
    d.h2("2.2 Installing")
    d.ol(["Copy the **Falcon OCR** folder (supplied by your vendor or administrator) to a location of your choice, for example C:\\Program Files\\Falcon OCR or your Documents folder.",
          "Open the folder and double-click **FalconOcr.exe**. Tip: right-click it and choose *Send to → Desktop (create shortcut)*.",
          "No installer and no administrator rights are needed. Keep all files of the folder together — the models folder and the DLL files are required."])
    d.h2("2.3 Uninstalling")
    d.p("Delete the Falcon OCR folder. To remove your settings, history and license as well, delete the folder %LOCALAPPDATA%\\FalconOCR (type it in the Explorer address bar).")

    # ------------------------------------------------------------------ 3
    d.pagebreak()
    d.h1("3. Trial and activation")
    d.h2("3.1 The 7-day trial")
    d.p("When you start Falcon OCR for the first time without a license, a free **7-day trial** begins. All features are available during the trial.")
    d.img("activation-trial.png", "Activation window during the trial", 400)
    d.ul(["At every start the activation window shows how many days are left.",
          "Click **Continue Trial** to use Falcon OCR, **Activate** to enter a license key, or **Exit**.",
          "While you are using the trial, the title bar shows an amber badge **TRIAL · n days left — Activate now**, and a notification below the toolbar shows the remaining days. Click the badge or **Activate now** at any time to activate; ✕ hides the notification until the next start."])
    d.img("trial-badge.png", "Trial badge in the title bar", 560)
    d.img("trial-banner.png", "Trial notification below the toolbar", 620)
    d.p("When the 7 days are over, Falcon OCR cannot be used until you enter a license key:")
    d.img("activation-expired.png", "Activation window after the trial", 400)
    d.note("Changing the computer's date or deleting program data does not extend the trial — it ends the trial immediately.")
    d.h2("3.2 Getting a license key")
    d.ol(["Purchase a license from your vendor.",
          "If your license is bound to one computer, the vendor needs your **machine code**. It is shown in the activation window (and in Settings → License). Click **Copy** next to it and send it to your vendor by e-mail.",
          "You receive a **license key** (a long text that starts with FOCR-) or a **license file** (.lic)."])
    d.h2("3.3 Entering the key")
    d.ol(["Open the activation window: at start-up, by clicking the trial badge, or via **Settings → License → Change license key…**.",
          "Paste the key into the box (Ctrl+V) — line breaks and spaces do not matter — or click **Load license file…** and select the .lic file.",
          "Click **Activate**. A confirmation shows the licensee and the validity, e.g. *Licensed to ACME Ltd · serial 1001 · perpetual · this computer only*."])
    d.h2("3.4 License messages")
    d.table(["Message", "Meaning and what to do"], [
        ("The license key is not in a valid format.", "The key was not copied completely. Copy the whole key again or use the .lic file."),
        ("The license key is not genuine.", "The key was changed or is not from your vendor. Request a new key."),
        ("This license key was issued for a different computer.", "The key is bound to another machine code. Send your machine code to the vendor."),
        ("The license expired on …", "Your time-limited license has ended. Ask your vendor for a renewal."),
        ("The system clock is set earlier than the last use…", "Correct the date and time of your computer."),
    ], [40, 60], "License messages")
    d.tip("Administrators can deploy a license for all users by placing a file named license.key (containing the key) next to FalconOcr.exe or in C:\\ProgramData\\FalconOCR.")

    # ------------------------------------------------------------------ 4
    d.pagebreak()
    d.h1("4. The main window")
    d.img("main-recognized.png", "Main window after recognition", 640)
    d.table(["Area", "Purpose"], [
        ("Title bar", "Product name, trial badge, minimise / maximise / close. Drag it to move the window; double-click to maximise."),
        ("Trial notification", "Only in the trial: remaining days, Activate now, ✕ to hide."),
        ("Navigation bar (left)", "Home (workspace), OCR (quick text), Batch Process, History, Settings; « Collapse at the bottom."),
        ("Toolbar", "Add Files, Scan, From Clipboard, Rotate, Crop, Delete, Recognize, Export; Settings and Help on the right."),
        ("Files / Thumbnails", "Your documents; the pages of the selected document."),
        ("Source Page", "The original page with zoom and page controls."),
        ("Recognized Text / Original Image", "The recognized page rebuilt next to the original / the original with the detected areas."),
        ("OCR Settings / Output Format", "Language, document type, layout options, output format and folder; Export button. Drag its left edge to change the width."),
        ("Status bar", "Progress and messages; number of files, selection and output format."),
    ], [30, 70], "Main window areas")

    d.h2("4.1 Customizing the window")
    d.ul(["**Interface language** — English, 简体中文 (Chinese) or 日本語 (Japanese): *Settings → General → Interface language*, then **Save** and confirm the restart. On first start Falcon OCR uses the language of Windows.",
          "**Collapse the sidebar** — click **« Collapse** at the bottom of the navigation bar to show only the icons (point at an icon to see its name). Click **»** to expand it again.",
          "**Resize the settings panel** — drag the dotted grip at the left edge of the right-hand panel with the mouse (280–640 pixels).",
          "Falcon OCR remembers these choices, the window size and position."])
    d.img_grid([("sidebar-collapsed.png", "Collapsed sidebar"), ("right-panel-splitter.png", "Grip for resizing the settings panel")], width=290)
    d.img("ui-japanese.png", "The workspace with the Japanese interface", 600)

    # ------------------------------------------------------------------ 5
    d.h1("5. Adding documents")
    d.h2("5.1 Files and PDFs")
    d.ol(["Click **Add Files** and choose **Add files (PDF, images)…** (or press Ctrl+O).",
          "Select one or more files. Each file becomes a document in the Files list. A PDF shows its page count, e.g. *12 pages*."])
    d.p("Supported: PDF; PNG, JPG/JPEG, BMP, GIF, TIFF (also multi-page), ICO, EMF, WMF. Photos taken with a phone are turned upright automatically.")
    d.h2("5.2 Image sequences")
    d.p("A book or a long letter is often scanned as one image per page. Combine them into one document:")
    d.ul(["**Add Files → Add images as one document (image sequence)…** and select all page images, or",
          "**Add Files → Add folder as image sequence…** and select the folder.",
          "Pages are ordered by file name the natural way (page2 before page10)."])
    d.h2("5.3 Clipboard, scanner, drag and drop")
    d.ul(["**From Clipboard** (or Ctrl+V in the workspace) adds a copied image, e.g. a screenshot, or files you copied in Explorer.",
          "**Scan** opens the Windows scanning dialog of your scanner; the scanned page is added as a document.",
          "Drag files or folders from Explorer onto the window. A dropped folder becomes an image sequence."])
    d.h2("5.4 Managing documents")
    d.p("Right-click a document in the Files list for: Recognize, Recognize all documents, Export, Export as…, Rename…, Open containing folder, Remove, Remove all.")

    # ------------------------------------------------------------------ 6
    d.h1("6. Preparing pages")
    d.ul(["**Thumbnails** tab: shows all pages of the selected document. Click a thumbnail to open the page; a ✓ marks recognized pages.",
          "**Rotate** (Ctrl+R): turns the current page 90° clockwise. Use it for pages scanned sideways or upside down.",
          "**Crop**: the Source Page shows *Drag to select the area to keep*. Drag a rectangle; everything outside is removed. Press Esc to cancel. Click Crop again on a cropped page to crop further or to restore the full page.",
          "**Delete** (Del): removes the selected document. In the Thumbnails tab it removes only the current page."])
    d.note("Rotating or cropping a recognized page removes its result — recognize the page again afterwards.")

    # ------------------------------------------------------------------ 7
    d.pagebreak()
    d.h1("7. Choosing the settings")
    d.h2("7.1 OCR Settings")
    d.table(["Setting", "Choose", "When"], [
        ("Document Type", "Editable Document (Recommended)", "You want to edit the text; the layout is rebuilt with real paragraphs, columns, lists and tables."),
        ("", "Exact Copy (keep positions)", "The result must look exactly like the original page (forms, posters, complex layouts)."),
        ("", "Plain Text", "You only need the text."),
        ("Language", "English, Chinese, Japanese, Korean, Russian", "The language of the document. Chinese and Japanese documents may also contain English."),
        ("Layout Analysis", "Automatic", "Normal documents: columns, tables and pictures are detected."),
        ("", "Single column", "The page has one column but Automatic splits it."),
        ("", "Text lines only", "Every line becomes its own paragraph (lists of addresses, receipts)."),
        ("Detect tables and columns", "on / off", "Switch off if text is wrongly placed in tables."),
    ], [22, 30, 48], "OCR Settings")
    d.h2("7.2 Output Format")
    d.p("Choose **Microsoft Word (.docx)**, **Microsoft Excel (.xlsx)** or **HTML (.html)** and the **Output Folder** (click … to change it). The default folder is *Documents\\Falcon OCR Output*.")
    d.h2("7.3 Advanced Settings")
    d.p("Click **Advanced Settings…** only if the results need tuning:")
    d.table(["Option", "Default", "Use"], [
        ("PDF input", "Use the PDF text layer", "Digital PDFs are read exactly. Choose Always run OCR if a PDF contains wrong text."),
        ("PDF rendering resolution", "200 dpi", "Higher (300) for very small print; lower for speed."),
        ("Detector max. image side", "2560 px", "Higher for tiny text on large pages."),
        ("Text pixel / box threshold", "0.30 / 0.60", "Lower values find faint text but may add noise."),
        ("Box expansion", "1.5", "Increase if characters at the edges of words are cut off."),
        ("Minimum line confidence", "0.50", "Lines below this certainty are ignored."),
        ("CPU threads", "0 (automatic)", "Limit if the computer must stay responsive during batch jobs."),
        ("Correct upside-down text lines", "off", "Documents with rotated text blocks."),
        ("Keep pictures and graphics as images", "on", "Switch off to export text only."),
    ], [30, 20, 50], "Advanced settings")

    # ------------------------------------------------------------------ 8
    d.h1("8. Recognizing")
    d.ol(["Select the document in the Files list.",
          "Click **Recognize** (F5). The current page is processed first so you can check it right away; the status bar shows the progress.",
          "To stop, click **Stop** (the Recognize button changes while it runs). Pages already finished keep their results."])
    d.p("A dense A4 page takes about 5–6 seconds; digital PDF pages take less than a second. The first recognition after start-up may take a moment longer while the models load.")
    d.tip("Recognize again with other settings: click Recognize on a finished document and confirm, or use ⋯ → Recognize this page again.")

    # ------------------------------------------------------------------ 9
    d.pagebreak()
    d.h1("9. Checking and correcting the result")
    d.img("main-selected.png", "The selected line is highlighted in the original and in the result", 640)
    d.h2("9.1 Side-by-side view")
    d.ul(["The **Recognized Text** tab shows the page rebuilt from the recognized text at the same positions as the original: fonts, sizes, bold text, colors, tables and pictures.",
          "Zoom and scrolling are linked: zoom with −/+, the zoom box (Fit width, Fit page, 50–300 %), Ctrl+mouse wheel; move with the scroll bars, the mouse wheel, the hand tool or the middle mouse button.",
          "Page through with ‹ › or PgUp / PgDn."])
    d.h2("9.2 Finding and correcting text")
    d.ol(["Click a line in either view — it is highlighted in both, so you can compare it with the original.",
          "Characters the recognizer was unsure about are highlighted in **yellow**.",
          "Double-click the line (or press F2 / Enter) and correct the text. Press **Enter** to keep the change or **Esc** to cancel. Corrected lines are underlined with a dashed blue line and exported as corrected."])
    d.h2("9.3 Changing formatting")
    d.img("format-toolbar.png", "Formatting toolbar: style, font and size boxes, B / I / U, lists", 460)
    d.ul(["With a line selected, use the **style box** to make its paragraph Normal, Heading 1–3 or a list item.",
          "The **font box** and the **size box** show the font of the selected line. Choose another font or size from the list, or type a font name or a size (e.g. 10.5) and press Enter — the whole paragraph changes and the export uses the new font.",
          "**B**, **I**, **U** switch bold, italic and underline for the paragraph.",
          "The list buttons turn the paragraph into a bulleted or numbered list item.",
          "**⋯** menu: switch the yellow highlighting on or off, copy the text of the page or of the whole document."])
    d.h2("9.4 Original Image view")
    d.img("main-overlay.png", "Original Image: detected text lines (green), table (blue), picture (orange)", 600)
    d.p("The **Original Image** tab shows what Falcon OCR found: text lines in green, tables in blue and pictures in orange. Use it to understand the result — for example, if a table was not detected.")

    # ------------------------------------------------------------------ 10
    d.pagebreak()
    d.h1("10. Exporting")
    d.ol(["Choose the output format and the document type on the right.",
          "Click **Export** (or Ctrl+E). If some pages are not recognized yet, Falcon OCR offers to recognize them first.",
          "The file is saved in the output folder with the document's name (a number is added if the name exists, e.g. *report (2).docx*) and opens automatically. Use **Export as…** in the Files context menu to choose name and folder yourself."])
    d.h2("10.1 What the exports look like")
    d.img_grid([("export-word.png", "Word — Editable Document"), ("export-exact.png", "Word — Exact Copy"),
                ("export-excel.png", "Excel — tables as real cells"), ("export-html.png", "HTML — web page")], width=300)
    d.table(["Format", "Best for", "Details"], [
        ("Word — Editable", "Letters, reports, articles, books", "Text flows and can be edited; headings use Word heading styles (useful for a table of contents); columns, lists, tables and pictures are kept."),
        ("Word — Exact Copy", "Forms, flyers, complex layouts", "Looks like the original; each line is a separate text frame, so larger edits are less comfortable."),
        ("Excel", "Tables, invoices, statements", "One worksheet per page; tables become real cells (merged cells, borders, fills); numbers can be calculated."),
        ("HTML", "Publishing, archives, e-mail", "One self-contained file with the pictures inside; opens in any browser."),
    ], [20, 25, 55], "Choosing a format")

    # ------------------------------------------------------------------ 11
    d.pagebreak()
    d.h1("11. Quick OCR")
    d.img("quick-ocr.png", "Quick OCR", 560)
    d.p("Use **OCR** in the navigation bar when you just need the text of an image — for example a screenshot. Press Ctrl+V (or **Paste image**), drop an image, or click **Open image…**. The text appears on the right; click **Copy text**. **Open in workspace** moves the file to Home for a full conversion. Nothing is saved.")

    # ------------------------------------------------------------------ 12
    d.h1("12. Batch processing")
    d.img("batch.png", "Batch Process", 560)
    d.ol(["Open **Batch Process**.",
          "Click **Add files…** or **Add folder…** (or drag files and folders into the list). With *Treat each folder as one document* a folder of images becomes one multi-page document.",
          "Choose output format, document type, language and output folder (double-click the folder box to browse).",
          "Click **Start**. Each entry shows its progress; the log lists every result. Click **Stop** to cancel after the current page.",
          "At the end, Falcon OCR offers to open the output folder."])

    # ------------------------------------------------------------------ 13
    d.h1("13. History")
    d.img("history.png", "History", 560)
    d.p("**History** lists your recognitions and exports with time, source, output file, format, pages, language and duration. Select an entry and click **Open output** (or double-click), **Show in folder** or **Open source in workspace**. **Clear history** removes all entries.")

    # ------------------------------------------------------------------ 14
    d.h1("14. Settings")
    d.img("settings.png", "Settings", 560)
    d.table(["Section", "What you can set"], [
        ("General", "Interface language (English, 简体中文, 日本語) — takes effect after a restart."),
        ("Recognition", "Default language and layout analysis, table detection, recognize automatically when files are added, highlight uncertain characters."),
        ("Export", "Default output format (also Plain text), document type, output folder, keep colors, include pictures, open the document after export."),
        ("Advanced recognition", "The options of chapter 7.3."),
        ("OCR models", "Shows that all language models are installed."),
        ("License", "License or trial status, your machine code, Change license key…"),
    ], [25, 75], "Settings")
    d.p("Click **Save** to apply. **Restore defaults** resets all values; **Open data folder** opens the folder with your settings and history.")

    # ------------------------------------------------------------------ 15
    d.pagebreak()
    d.h1("15. Command line")
    d.p("falcon-ocr.exe (in the same folder) converts files without opening the window — useful for scripts and scheduled tasks. It uses the same license or trial as the application.")
    d.code(["falcon-ocr <files or folders> [-l en|zh|ja|ko|ru] [-f docx,xlsx,html,txt] [-o output-folder]",
            "           [--exact | --plain] [--seq] [--no-tables] [--ocr-only] [--dpi 300] [--cls]",
            "falcon-ocr --license <key>        activate",
            "falcon-ocr --machine-code         show the machine code",
            "",
            "Examples:",
            "falcon-ocr invoice.pdf -f xlsx -o D:\\Out",
            "falcon-ocr D:\\Scans\\Book --seq -l ru -f docx",
            "falcon-ocr C:\\Inbox -f docx,html --exact"])
    d.table(["Option", "Meaning"], [
        ("-l", "Language (default en)."), ("-f", "One or more formats, comma separated (default docx)."), ("-o", "Output folder (default: next to the source)."),
        ("--exact / --plain", "Exact Copy / Plain Text instead of Editable Document."), ("--seq", "All inputs form one image-sequence document."),
        ("--no-tables", "Do not detect tables."), ("--ocr-only", "Ignore PDF text layers."), ("--dpi", "PDF rendering resolution."), ("--cls", "Correct upside-down lines."),
    ], [25, 75], "Command-line options")
    d.p("Exit codes: 0 = success, 1 = usage error, 2 = models missing, 3 = not activated / trial ended.")

    # ------------------------------------------------------------------ 16
    d.h1("16. Keyboard shortcuts")
    d.table(["Keys", "Action"], [
        ("Ctrl+O", "Add files"), ("Ctrl+V", "Paste image or files from the clipboard"), ("F5", "Recognize the current document"),
        ("Ctrl+E", "Export"), ("Ctrl+R", "Rotate page"), ("Del", "Remove document (Thumbnails: page)"),
        ("PgUp / PgDn", "Previous / next page"), ("Ctrl+Plus / Ctrl+Minus, Ctrl+wheel", "Zoom in / out"),
        ("Middle mouse button (drag)", "Pan the page"), ("F2 or Enter", "Edit the selected line"), ("Enter / Esc (while editing)", "Keep / cancel the change"),
        ("Esc (crop mode)", "Cancel cropping"),
    ], [40, 60], "Keyboard shortcuts")

    # ------------------------------------------------------------------ 17
    d.h1("17. Tips for best results")
    d.ul(["Scan at **300 dpi** (200 dpi minimum) in color or grayscale. Avoid very dark or blurry scans.",
          "Photograph pages straight from above in good light; crop away the background before recognizing.",
          "Select the **correct language** — it makes the biggest difference for non-Latin scripts.",
          "Rotate pages that are sideways before recognizing.",
          "For digital PDFs keep *Use the PDF text layer* — the text is then exact.",
          "Use **Exact Copy** for forms and flyers, **Editable Document** for everything you want to edit.",
          "Check yellow-highlighted characters first; they are the most likely errors."])

    # ------------------------------------------------------------------ 18
    d.pagebreak()
    d.h1("18. Troubleshooting")
    d.table(["Problem", "Solution"], [
        ("'Some OCR model files are missing'", "The application folder is incomplete. Copy the complete Falcon OCR folder again."),
        ("'Falcon OCR must run as a 64-bit process'", "Use a 64-bit Windows."),
        ("The text is wrong or empty", "Check the language setting; for scans use at least 200 dpi; rotate sideways pages; lower the text thresholds in Advanced Settings."),
        ("Text of a two-column page is mixed", "Make sure Layout Analysis is Automatic; if a single-column page is split, choose Single column."),
        ("A table is not recognized", "Tables with visible lines work best. Check the Original Image tab. For tables without lines try Exact Copy or Excel."),
        ("Headings are recognized as normal text", "Select the line and choose Heading 1–3 in the style box before exporting."),
        ("A picture contains text I need", "Switch off 'Keep pictures and graphics as images' in Advanced Settings."),
        ("A digital PDF gives strange characters", "Advanced Settings → PDF input → Always run OCR."),
        ("Export does not open", "Install Word/Excel or a compatible program, or switch off 'Open the document after export' and open the file from the output folder."),
        ("'No scanner was found'", "Connect the scanner and install its WIA driver; or scan with the scanner software and add the files."),
        ("The application closes after the start window", "The trial has ended and no key was entered — see chapter 3."),
        ("An unexpected error message", "Details are written to %LOCALAPPDATA%\\FalconOCR\\error.log — send this file to your support contact."),
    ], [35, 65], "Troubleshooting")

    # ------------------------------------------------------------------ 19
    d.h1("19. Privacy and data")
    d.p("Falcon OCR processes everything on your computer and does not connect to the internet. It stores only the following in %LOCALAPPDATA%\\FalconOCR: settings.json (your settings), history.json (recognition and export history), license.key (your license), trial information, scans (pages acquired from a scanner) and error.log.")

    d.h1("Appendix A. Glossary")
    d.table(["Term", "Meaning"], [
        ("OCR", "Optical character recognition: reading text from images."),
        ("Document", "One entry in the Files list — a PDF, an image, a multi-page TIFF or an image sequence."),
        ("Page", "One page of a document."),
        ("Image sequence", "Several image files combined into one multi-page document."),
        ("Text layer", "Text stored inside a digital PDF."),
        ("Editable Document", "Export with flowing, editable text and the rebuilt structure."),
        ("Exact Copy", "Export that places everything at its original position."),
        ("Machine code", "Code that identifies your computer for a computer-bound license."),
        ("License key", "Text starting with FOCR- that activates Falcon OCR."),
    ], [25, 75], "Glossary")
    return d
