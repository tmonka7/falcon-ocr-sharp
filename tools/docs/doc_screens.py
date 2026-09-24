from docbuilder import Doc


def build():
    d = Doc("Screen Design Document", "Screens, layouts, controls, navigation and visual style", "FOCR-SCR-001",
            "This document defines every screen and dialog of Falcon OCR 1.0: purpose, layout, controls and their behavior, navigation, "
            "messages and the visual style guide. It is the reference for UI implementation and UI testing.")

    d.h1("1. Introduction")
    d.p("The user interface follows the ABBYY FineReader-style reference supplied by the product owner: a green title bar, a dark-green navigation bar on the left, "
        "a ribbon toolbar, the document list, the source page and the recognition result side by side, and a settings column with a prominent Export button. "
        "Screen identifiers (SCR-nn) are referenced by the Test Case Specification.")
    d.table(["ID", "Screen / dialog", "Opened from"], [
        ("SCR-01", "Main window shell (title bar, navigation, status bar)", "Application start"),
        ("SCR-02", "Home — workspace", "Navigation 'Home' (start screen)"),
        ("SCR-03", "Home — Original Image (analysis overlay)", "Results tab 'Original Image'"),
        ("SCR-04", "OCR — Quick OCR", "Navigation 'OCR'"),
        ("SCR-05", "Batch Process", "Navigation 'Batch Process'"),
        ("SCR-06", "History", "Navigation 'History'"),
        ("SCR-07", "Settings", "Navigation 'Settings', toolbar link 'Settings'"),
        ("SCR-08", "Activation window (trial running / trial ended)", "Start-up without license, trial badge, Settings → License"),
        ("SCR-09", "Advanced OCR Settings dialog", "OCR Settings → Advanced Settings…"),
        ("SCR-10", "Rename / message dialogs, context menus", "File list context menu, Help"),
        ("SCR-11", "License Key Generator (vendor tool)", "FalconOcrKeyGen.exe"),
    ], [10, 55, 35], "Screen inventory")

    d.h1("2. Visual style guide")
    d.h2("2.1 Colors")
    d.table(["Token", "RGB", "Use"], [
        ("Header", "#0E6E4C", "Title bar, dialog headers"),
        ("Sidebar", "#10704E → #0A583E (gradient)", "Navigation bar"),
        ("Sidebar selected / hover", "#3A9670 / #22825E", "Current page / hover in navigation"),
        ("Accent", "#13865C", "Primary buttons, selected tab underline, icons, links"),
        ("Accent light / hover", "#E1F3EB / #EEF8F3", "Selected list item, button hover"),
        ("Window / Panel", "#F4F6F8 / #FFFFFF", "Background / cards"),
        ("Border", "#DEE3E8", "Card and panel borders"),
        ("Text / Sub-text", "#1F2937 / #64707E", "Body text / secondary text"),
        ("Canvas", "#E2E6EA", "Viewer background around the page"),
        ("Low confidence", "#FFD600 (35 % alpha)", "Uncertain characters in the results"),
        ("Trial badge", "#FFC107", "Title bar trial badge"),
        ("Danger", "#C82828", "Errors, Stop button"),
    ], [28, 32, 40], "Color palette")
    d.h2("2.2 Typography and icons")
    d.ul(["Segoe UI 9.75 pt for controls; Segoe UI Semibold 10 pt (bold), 12 pt (card titles), 15 pt (page titles), 17 pt (product name); navigation 11 pt.",
          "Icons are vector glyphs drawn with GDI+ on a 24 × 24 grid (no bitmaps), so they are sharp at every DPI: Home, OCR, Batch, History, Settings, Help, Add, Scanner, Clipboard, Rotate, Crop, Trash, Play/Stop, Export, zoom, fit, hand, chevrons, file types (PDF, image, images, Word, Excel, HTML).",
          "All sizes are scaled with the monitor DPI (system DPI aware)."])
    d.h2("2.3 Interaction conventions")
    d.ul(["Primary action per screen in a filled green button (Export, Start, Save, Activate, Generate License Key).",
          "Hover highlight on all clickable owner-drawn controls; disabled controls are grey.",
          "Long operations show progress in the status bar and can be stopped (Recognize → Stop, Batch Start → Stop).",
          "Confirmation before destructive actions (remove recognized documents, clear history, new signing key)."])

    d.h1("3. SCR-01 Main window shell")
    d.img("main-loaded.png", "Main window with a document loaded (trial mode)", 640)
    d.table(["Region", "Content and behavior"], [
        ("Title bar (52 px, green)", "Logo, 'Falcon OCR', tagline 'Convert Scans and Images into Editable Documents'. Drag to move, double-click to maximise/restore. Trial badge 'TRIAL · n days left — Activate now' (only while unlicensed; click opens SCR-08). Minimise, maximise/restore, close (red hover)."),
        ("Navigation bar (184 px)", "Home, OCR, Batch Process, History, Settings. The current page is highlighted with a rounded light-green background."),
        ("Content", "The selected page (SCR-02 … SCR-07)."),
        ("Status bar (36 px)", "Left: status message ('Ready', progress text) and progress bar during recognition/batch. Right: 'Total Files: n | Selected: n | Output: DOCX'."),
        ("Window", "Borderless, resizable at the 6 px edge, maximises to the monitor work area (taskbar stays visible), remembers size/position and maximised state."),
    ], [25, 75], "SCR-01 regions")

    d.h1("4. SCR-02 Home — workspace")
    d.img("main-selected.png", "Workspace after recognition; the selected line is highlighted on both sides", 640)
    d.h2("4.1 Layout")
    d.table(["Region", "Width", "Content"], [
        ("Toolbar", "full, 92 px", "Add Files, Scan, From Clipboard | Rotate, Crop, Delete | Recognize | Export; right: Settings, Help."),
        ("Files panel", "214 px", "Tabs Files / Thumbnails; document list (icon, name, 'n pages', 'recognized' or 'k/n done'); thumbnails of the pages of the selected document."),
        ("Source Page", "½ of centre", "Caption tab, viewer toolbar, ImageViewer, footer 'name · w × h px · dpi · rotated/cropped'."),
        ("Results", "½ of centre", "Tabs Recognized Text / Original Image, formatting toolbar, LayoutView, status 'Text recognition completed — n lines, n blocks, confidence (time)' and page 'i / n'."),
        ("Settings column", "340 px", "Card 'OCR Settings', card 'Output Format', Export button."),
    ], [20, 18, 62], "SCR-02 regions")
    d.p("Source Page and Results have identical header (tab strip + toolbar) and footer heights, so both canvases have the same geometry: at equal zoom every page pixel appears at the same vertical position on both sides (FR-20).")
    d.h2("4.2 Toolbar")
    d.table(["Button", "Behavior", "Shortcut"], [
        ("Add Files", "Menu: Add files (PDF, images)… / Add images as one document (image sequence)… / Add folder as image sequence…", "Ctrl+O"),
        ("Scan", "WIA acquisition dialog; the scanned page is added as a document.", "—"),
        ("From Clipboard", "Adds the clipboard image (Clipboard_01 …) or the copied files.", "Ctrl+V"),
        ("Rotate", "Rotates the current page 90° clockwise; clears its result.", "Ctrl+R"),
        ("Crop", "Crop mode: drag a rectangle on the source page; Esc cancels. On a cropped page: Yes = crop further, No = restore.", "—"),
        ("Delete", "Removes the selected document (confirmation if recognized); in Thumbnails removes the current page.", "Del"),
        ("Recognize", "Recognizes the unrecognized pages of the current document (current page first); asks to re-recognize a finished document. While running the button shows Stop.", "F5"),
        ("Export", "Exports the current document with the right-hand settings (asks to recognize missing pages).", "Ctrl+E"),
        ("Settings / Help", "Open SCR-07 / help text with shortcuts and license status.", "—"),
    ], [16, 70, 14], "SCR-02 toolbar")
    d.h2("4.3 Files panel")
    d.ul(["Owner-drawn list item (64 px): PDF badge, image or image-stack icon; name; 'n pages' / 'n images'; green 'recognized' when all pages are done.",
          "Context menu: Recognize, Recognize all documents, Export, Export as…, Rename…, Open containing folder, Remove, Remove all.",
          "Thumbnails tab: page thumbnails (96 × 128) generated in the background, caption with page number and ✓ when recognized; selecting a thumbnail shows the page.",
          "Drop target for files and folders (also the viewers)."])
    d.h2("4.4 Source Page viewer")
    d.table(["Control", "Behavior"], [
        ("− / +", "Zoom out / in by 20 % (Ctrl+wheel, Ctrl+Minus/Plus)."),
        ("Zoom box", "Fit width, Fit page, 50 … 300 % or a typed value + Enter."),
        ("Fit width / Fit page", "Zoom modes that follow the window size."),
        ("Rotate", "Same as toolbar Rotate."),
        ("Hand", "Toggle pan tool (left-drag pans); middle-drag always pans."),
        ("‹  i / n  ›", "Previous / next page (PgUp / PgDn)."),
        ("Canvas", "Page with shadow; click a recognized line to select it; crop rubber band in crop mode with dimmed outside area."),
    ], [22, 78], "SCR-02 viewer controls")
    d.h2("4.5 Results panel")
    d.table(["Control", "Behavior"], [
        ("Recognized Text tab", "LayoutView: the page redrawn from the recognition result — lines with estimated font/size/weight/color fitted to their source width, table grids and fills, pictures, bullets. Hover outlines a line (I-beam); click selects (also in the source view); double-click / F2 / Enter edits in place (Enter commit, Esc cancel); edited lines are underlined with a dashed blue line; uncertain characters are highlighted yellow."),
        ("Original Image tab", "SCR-03."),
        ("Style box", "Normal, Heading 1, Heading 2, Heading 3, List item — applied to the paragraph of the selected line."),
        ("B / I / U", "Toggle bold / italic / underline for the paragraph; state reflects the selected line."),
        ("Bulleted / numbered list", "Turn the paragraph into a list item (numbered items count up from preceding numbered items)."),
        ("⋯ menu", "Highlight uncertain characters (toggle), Copy page text, Copy document text, Recognize this page again."),
        ("Status line", "Check-mark icon and result summary, or 'This page has not been recognized yet.'"),
    ], [22, 78], "SCR-02 results controls")
    d.h2("4.6 Settings column")
    d.table(["Field", "Values", "Default"], [
        ("Document Type", "Editable Document (Recommended) / Exact Copy (keep positions) / Plain Text", "Editable Document"),
        ("Language", "English, Chinese (中文), Japanese (日本語), Korean (한국어), Russian (Русский)", "English"),
        ("Layout Analysis", "Automatic / Single column / Text lines only", "Automatic"),
        ("Detect tables and columns", "check box", "on"),
        ("Advanced Settings…", "opens SCR-09", "—"),
        ("Output Format", "Microsoft Word (.docx) / Microsoft Excel (.xlsx) / HTML (.html)", "Word"),
        ("Output Folder", "text + … (folder browser)", "Documents\\Falcon OCR Output"),
        ("Export", "green button, same as toolbar Export", "—"),
    ], [26, 54, 20], "SCR-02 settings column")
    d.p("Both cards can be collapsed with the chevron in their header. Changes are saved immediately.")

    d.h1("5. SCR-03 Original Image (analysis overlay)")
    d.img("main-overlay.png", "Original Image tab: detected lines (green), table (blue), picture (orange, dashed), blocks (dotted)", 640)
    d.p("Shows the source image with the analysis: every text line polygon in translucent green, table regions in blue, pictures dashed orange and paragraph blocks dotted grey. Clicking a line selects it everywhere. The formatting toolbar is disabled on this tab.")

    d.h1("6. SCR-04 Quick OCR")
    d.img("quick-ocr.png", "Quick OCR page", 600)
    d.table(["Control", "Behavior"], [
        ("Open image…", "Open an image or PDF (first page) and recognize it immediately."),
        ("Paste image / Ctrl+V", "Recognize the clipboard image or the first copied file."),
        ("Drop zone (left)", "Drop an image; shows the image with the detected lines."),
        ("Text (right)", "Plain text in reading order; editable."),
        ("Copy text", "Copies the text."),
        ("Open in workspace", "Adds the source file to the Home workspace."),
        ("Info line", "'n lines · confidence · time · language' or the error."),
    ], [25, 75], "SCR-04 controls")

    d.h1("7. SCR-05 Batch Process")
    d.img("batch.png", "Batch Process page", 600)
    d.table(["Control", "Behavior"], [
        ("Add files… / Add folder… / Clear list", "Fill the input list (drag and drop also works). Folders become one image-sequence document when 'Treat each folder as one document' is checked; PDFs in folders are added individually."),
        ("Input list", "Columns Input, Type, Status (Waiting, Recognizing…, Page i/n, Done, Failed, Stopped)."),
        ("Output format / Document type / Language / Output folder", "Options of the run (defaults from Settings; double-click the folder box to browse)."),
        ("Start / Stop", "Processes the items not yet 'Done'; progress bar and status bar percentage; confirmation to open the output folder at the end."),
        ("Log", "Time-stamped result per document (✓ name (pages, seconds) → output file, ✗ error) and summary."),
    ], [30, 70], "SCR-05 controls")

    d.h1("8. SCR-06 History")
    d.img("history.png", "History page", 600)
    d.p("List (newest first) with Time, Source, Output, Format ('Recognition' or DOCX/XLSX/HTML/TEXT), Pages, Language and Duration. Buttons: Open output (also double-click), Show in folder, Open source in workspace, Clear history (confirmation).")

    d.h1("9. SCR-07 Settings")
    d.img("settings.png", "Settings page", 600)
    d.table(["Section", "Fields"], [
        ("Recognition", "Default language, Layout analysis, Detect tables and columns, Recognize automatically when files are added, Highlight uncertain characters."),
        ("Export", "Default output format (incl. Plain text), Document type, Output folder (double-click to browse), Keep text/fill/page colors, Include pictures, Open the document after export."),
        ("Advanced recognition", "Same fields as SCR-09."),
        ("OCR models (offline)", "Model folder and each model with ✓ / ✗ missing and size; supported languages."),
        ("License", "State (✓ licensed to …, ⏳ trial n of 7 days, ✗ invalid) and machine code; Change license key… (SCR-08), Copy machine code."),
        ("Buttons", "Save (applies to the workspace immediately), Restore defaults, Open data folder."),
    ], [25, 75], "SCR-07 sections")

    d.h1("10. SCR-08 Activation window")
    d.img("activation-trial.png", "Activation window while the trial is running", 420)
    d.img("activation-expired.png", "Activation window after the trial has ended", 420)
    d.table(["Element", "Behavior"], [
        ("Header", "'Activate Falcon OCR' and a subtitle: trial days left / trial ended / enter your license key."),
        ("Banner", "Green: 'Trial version — n of 7 days remaining (ends date). All features are available during the trial.' Red: 'The 7-day trial ended on date.' or tamper message."),
        ("Machine code + Copy", "Read-only code (Consolas 13 pt) to send to the vendor for a computer-bound key."),
        ("License key box", "Multi-line; accepts pasted keys with spaces/line breaks."),
        ("Status line (red)", "Validation result: not genuine, malformed, other computer, expired, clock set back."),
        ("Load license file…", "Reads the key from a .lic/.key/.txt file."),
        ("Activate (primary)", "Validates and stores the key; success message; closes."),
        ("Continue Trial", "Only at start-up while the trial is running; starts the application in trial mode."),
        ("Exit / Cancel", "At start-up closes the application; from the running application closes the dialog."),
    ], [25, 75], "SCR-08 elements")

    d.h1("11. SCR-09 Advanced OCR Settings")
    d.table(["Field", "Range", "Default", "Effect"], [
        ("PDF input", "Use the PDF text layer when present / Always run OCR", "Text layer", "FR-12"),
        ("PDF rendering resolution", "100 – 600 dpi", "200", "Quality vs. speed for scanned PDFs"),
        ("Detector max. image side", "960 – 6000 px", "2560", "Small text on large pages vs. speed"),
        ("Text pixel threshold", "0.05 – 0.9", "0.30", "Lower finds faint text"),
        ("Text box threshold", "0.1 – 0.95", "0.60", "Lower keeps uncertain boxes"),
        ("Box expansion (unclip ratio)", "1.0 – 3.0", "1.5", "Box padding around text"),
        ("Minimum line confidence", "0 – 0.95", "0.50", "Drops unreliable lines"),
        ("CPU threads", "0 – 64", "0 = automatic", "0 uses one thread per physical core"),
        ("Correct upside-down text lines", "check box", "off", "FR-13"),
        ("Keep pictures and graphics as images", "check box", "on", "FR-18"),
    ], [30, 28, 17, 25], "SCR-09 fields")
    d.p("Buttons: Defaults (reload default values), OK (save), Cancel.")

    d.h1("12. SCR-10 Other dialogs and messages")
    d.table(["Situation", "Message / dialog"], [
        ("Recognize on a recognized document", "'\"name\" is already recognized. Recognize it again with the current settings?' OK / Cancel"),
        ("Export with unrecognized pages", "'n of m pages are not recognized. Yes = recognize them first, No = export only the recognized pages' Yes / No / Cancel"),
        ("Crop on a cropped page", "'This page is already cropped. Yes = crop further, No = restore the full page.'"),
        ("Remove recognized document", "'Remove \"name\" and its recognition results?'"),
        ("Close during recognition", "'Recognition is still running. Stop it and exit?'"),
        ("Missing models", "'Some OCR model files are missing from the installation: … Run tools\\fetch-dependencies.ps1 …'"),
        ("Add files errors", "'Some files could not be opened:' followed by one line per file."),
        ("Rename…", "Single-line input dialog with OK / Cancel."),
        ("Help", "Workflow summary, keyboard shortcuts and license status."),
    ], [30, 70], "Messages")

    d.h1("13. SCR-11 License Key Generator (vendor)")
    d.img("keygen.png", "License Key Generator", 520)
    d.table(["Section", "Controls"], [
        ("Signing key", "Status: fingerprint and ✓ matches / ⚠ does not match the public key of the build. Buttons: New signing key…, Import backup…, Backup key…, Write public key to app…"),
        ("License", "Licensee, Machine code (upper case), Any computer, Expiry (Perpetual, date, 30 days, 1 year), Serial number (auto-increment), Note (log only), Generate License Key (primary), key box (read-only), Copy key, Save as .lic file…, Open issued log."),
        ("Verify a key", "Key box, Verify, result (signature, licensee, serial, dates, machine)."),
    ], [20, 80], "SCR-11 sections")

    d.h1("14. Navigation map")
    d.table(["From", "Action", "To"], [
        ("Start-up (unlicensed)", "Activate / Continue Trial / Exit", "SCR-01 licensed / SCR-01 trial / closed"),
        ("Any page", "Navigation bar", "SCR-02, 04, 05, 06, 07"),
        ("SCR-01 title bar", "Trial badge", "SCR-08"),
        ("SCR-02", "Toolbar Settings / Help", "SCR-07 / help message"),
        ("SCR-02", "Advanced Settings…", "SCR-09"),
        ("SCR-02 results", "Original Image tab", "SCR-03"),
        ("SCR-04", "Open in workspace", "SCR-02"),
        ("SCR-06", "Open source in workspace", "SCR-02"),
        ("SCR-07", "Change license key…", "SCR-08"),
    ], [30, 35, 35], "Navigation")
    return d
