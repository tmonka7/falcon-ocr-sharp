from docbuilder import Doc


def build():
    d = Doc("Screen Design Document", "Screens, layouts, controls, navigation and visual style", "FOCR-SCR-001",
            "This document defines every screen and dialog of Falcon OCR 1.0: purpose, layout, controls and their behavior, states, navigation, "
            "messages, localization and the visual style guide. It is the reference for UI implementation, UI review and UI testing.")

    # ================================================================== 1
    d.h1("1. Introduction")
    d.h2("1.1 Purpose and scope")
    d.p("The user interface follows the ABBYY FineReader-style reference of the product owner: green title bar, dark-green navigation bar, ribbon toolbar, "
        "document list, source page and recognition result side by side, and a settings column with a prominent Export button. This document describes the "
        "screens of FalconOcr.exe and of the vendor tool FalconOcrKeyGen.exe. Screen (SCR-nn), control (e.g. TB-05, VW-07) and message (MSG-nn) identifiers are "
        "referenced by the Test Case Specification.")
    d.h2("1.2 Screen inventory")
    d.table(["ID", "Screen / dialog", "Type", "Opened from", "Source"], [
        ("SCR-01", "Main window shell (title bar, navigation, status bar)", "Borderless form", "Application start", "MainForm.cs"),
        ("SCR-02", "Home — workspace", "Page", "Navigation 'Home' (start page)", "WorkspacePage.cs"),
        ("SCR-03", "Home — Original Image (analysis overlay)", "Tab of SCR-02", "Results tab 'Original Image'", "ImageViewer.cs"),
        ("SCR-04", "OCR — Quick OCR", "Page", "Navigation 'OCR'", "OtherPages.cs"),
        ("SCR-05", "Batch Process", "Page", "Navigation 'Batch Process'", "OtherPages.cs"),
        ("SCR-06", "History", "Page", "Navigation 'History'", "OtherPages.cs"),
        ("SCR-07", "Settings", "Page", "Navigation 'Settings', toolbar link 'Settings'", "OtherPages.cs"),
        ("SCR-08", "Activation window (trial running / trial ended / licensed)", "Modal dialog", "Start-up without license, trial badge, Activate now, Settings → License", "ActivationForm.cs"),
        ("SCR-09", "Advanced OCR Settings", "Modal dialog", "OCR Settings → Advanced Settings…", "Dialogs.cs"),
        ("SCR-10", "Rename dialog, message boxes, context menus", "Modal dialogs", "File list context menu, toolbar, Help", "Dialogs.cs, WorkspacePage.cs"),
        ("SCR-11", "License Key Generator (vendor tool)", "Separate program", "FalconOcrKeyGen.exe", "KeyGenForm.cs"),
    ], [9, 34, 14, 27, 16], "Screen inventory")
    d.h2("1.3 Conventions")
    d.ul(["Sizes are pixels at 96 dpi; the main window scales them with the monitor DPI (at 150 % the 52 px title bar is 78 px).",
          "Labels are quoted in English; ZH / JA texts are taken from lang/zh_CN.po and lang/ja.po. Placeholders such as {0} are filled at run time.",
          "'Owner-drawn' controls are custom controls painted with GDI+ (chapter 3); all others are standard WinForms controls.",
          "Screenshots: 2026-09-24, 1920 × 1032 px, English interface, trial mode, sample report.png."])
    # ================================================================== 2
    d.h1("2. Visual style guide")
    d.p("All colors and fonts are defined centrally in the class Theme (src/FalconOcr.App/UI/Theme.cs); the few literal colors (trial notification, caption hover, overlay pens) are listed with the tokens.")
    d.h2("2.1 Color tokens")
    d.img("scr-color-palette.png", "Color tokens of the Falcon OCR theme", 600)
    d.table(["Token", "RGB", "Use"], [
        ("Header", "#0E6E4C", "Title bar, dialog headers"),
        ("SidebarTop → SidebarBottom", "#10704E → #0A583E", "Navigation gradient"),
        ("SidebarSelected / SidebarHover", "#3A9670 / #22825E", "Current page / hover in navigation"),
        ("Accent / AccentDark", "#13865C / #0C6847", "Primary buttons, selected tab, icons, selection / pressed, window frame"),
        ("AccentLight / AccentHover", "#E1F3EB / #EEF8F3", "Selected item, pressed or checked / hover"),
        ("Window / Panel / Border", "#F4F6F8 / #FFFFFF / #DEE3E8", "Background / cards / 1 px lines"),
        ("Text / SubText", "#1F2937 / #64707E", "Body / secondary text"),
        ("Canvas", "#E2E6EA", "Viewer background"),
        ("Selection / LowConfidence", "#13865C alpha 70 / #FFD600 alpha 90", "Selected line / uncertain characters"),
        ("Danger", "#C82828", "Stop state, errors"),
        ("Trial badge / notification", "#FFC107 (hover #FFD666) / #FFF4D6, text #664700", "Title bar pill / banner"),
        ("File types", "PDF #DE3129, image #2EA05A, Word #185ABD, Excel #107C41, HTML #E44D26", "File badges, format icons"),
    ], [28, 36, 36], "Color tokens")
    d.h2("2.2 Typography")
    d.img("scr-typography.png", "Type scale (English interface)", 600)
    d.table(["Font token", "English", "Chinese / Japanese", "Used for"], [
        ("Base", "Segoe UI 9.75 pt", "YaHei UI / Yu Gothic UI 9.75", "Controls, labels, status bar"),
        ("Small", "Segoe UI 8.5 pt", "8.5", "Counters, file list second line"),
        ("Bold", "Segoe UI Semibold 10 pt", "10 bold", "Selected tab, badge, Activate / Save / Start"),
        ("Heading", "Segoe UI Semibold 12 pt", "12 bold", "Card and section titles, placeholders"),
        ("Title", "Segoe UI Semibold 15 pt", "15 bold", "Page titles SCR-04 … 07"),
        ("Nav", "Segoe UI 11 pt", "11", "Navigation entries"),
        ("Semibold(17)", "Segoe UI Semibold 17 pt", "17 bold", "Product name"),
    ], [16, 22, 30, 32], "Font tokens")
    d.p("The UI font family is chosen at start-up from the interface language (Segoe UI has no CJK glyphs); CJK 'semibold' tokens use the bold style. "
        "Machine code and license keys use Consolas; the B / I / U glyphs are always Segoe UI 11 pt.")
    d.h2("2.3 Spacing and sizing grid")
    d.table(["Element", "Size (px)", "Remarks"], [
        ("Title bar", "52 high", "Logo 34 at x 18, name at x 62, tagline at x 215; caption buttons 52 × 52"),
        ("Navigation bar", "184 / 64 wide", "Entries 56 high, icon 26 at x 22, text at x 62; toggle 52"),
        ("Toolbar", "92 high", "Padding 16/8; ToolButton 84 × 72 (From Clipboard 116, Recognize 96); separators 17 × 64"),
        ("Trial notification", "40 high", "Activate now 130 wide, ✕ 34"),
        ("Files panel", "214 / 40 wide", "Tabs 38, items 64, toggle 40"),
        ("Centre", "padding 8", "Splitter 8, 50 : 50, each side at least 200"),
        ("Tab strip / toolbars / footers", "38 / 46 / 40 high", "Tabs at most 150 wide, 3 px underline; IconButton 30 × 30"),
        ("Settings column", "340 (280 – 640)", "Splitter 6; OCR card 390, Output card 290, header 44; Export 52; gaps 10"),
        ("Status bar", "36 high", "Message 520, progress 200, counters 420"),
        ("Secondary pages", "padding 24/16", "Header 70 (title 40, subtitle 26); body padding 20; flat buttons 130 × 34"),
    ], [26, 20, 54], "Sizing grid")
    d.h2("2.4 Icons")
    d.p("Icons are vector glyphs drawn with GDI+ on a 24 × 24 grid (Icons.cs, enum IconKind), stroke 1.8 units, round caps. No bitmaps are used, so icons are sharp "
        "at every DPI and take the color of the calling control.")
    d.table(["Group", "Icons (IconKind)"], [
        ("Navigation", "Home, Ocr, Batch, History, Settings, ChevronLeft / Right"),
        ("Toolbar and links", "AddCircle, Scanner, Clipboard, Rotate, Crop, Trash, Play / Stop, Export, Settings, Help"),
        ("Viewer and formatting", "ZoomOut, ZoomIn, FitWidth, FitPage, Hand, List, NumberedList, More (B / I / U are text glyphs)"),
        ("File types", "Pdf, Image, Images, Word, Excel, Html, Text, Folder"),
        ("Window and misc", "Minimize, Maximize, Restore, Close, Check, Copy, Boxes, ChevronUp / Down, Logo"),
    ], [24, 76], "Icon inventory")
    d.p("The application icon (app.ico, 16–256 px) is used for both executables, the window icons, the title-bar logo (34 px) and the activation header (44 px).")
    d.img("app-icon.png", "Application icon", 96)
    d.h2("2.5 Interaction conventions")
    d.ul(["One primary action per screen in a filled green button: Export (SCR-02), ▶ Start (SCR-05), Save (SCR-07), Activate (SCR-08), Generate License Key (SCR-11).",
          "Owner-drawn controls show AccentHover on hover, AccentLight when pressed and grey when disabled.",
          "Long operations report progress in the status bar and are stopped with the same button that started them (Recognize → Stop, ▶ Start → ■ Stop).",
          "Destructive actions ask for confirmation (remove recognized documents, Remove all, Clear history, Restore defaults, new signing key).",
          "The workspace settings column saves immediately; SCR-07 saves with Save. Window bounds, collapsed panels and the column width are remembered."])

    # ================================================================== 3
    d.h1("3. Component library")
    d.p("The look comes from owner-drawn, double-buffered controls in UI/Controls.cs and the page classes:")
    d.table(["Component", "Used for", "Normal", "Hover / pressed", "Checked / selected", "Disabled"], [
        ("ToolButton 84 × 72", "Toolbar commands", "Transparent, icon 28 px above caption", "Rounded (r 6) AccentHover / AccentLight", "—", "Icon #AAB0B8, caption grey"),
        ("NavButton 56 high", "Navigation entries", "White icon 26 px + text", "Right-rounded SidebarHover", "Right-rounded SidebarSelected", "—"),
        ("TabStrip 38 high", "Files/Thumbnails, Source Page, Recognized Text/Original Image", "Text in Base", "Tab filled AccentHover", "Bold accent text, 3 px accent underline", "—"),
        ("AccentButton 48–52 high", "Export", "Accent fill, white icon + Semibold 12", "#169265 / AccentDark", "—", "#A0BEB0"),
        ("IconButton 30 × 30", "Viewer and formatting tools, card chevrons", "Icon or glyph in Text", "Rounded (r 4) AccentHover", "AccentLight fill, accent icon (toggle)", "Silver icon"),
        ("IconRadio 44 high", "Output format options", "Grey ring 18 px, icon 26 px, text", "Row AccentHover", "Accent ring with filled dot", "—"),
        ("CardPanel", "OCR Settings, Output Format", "White, 1 px Border, icon + Heading title", "—", "Collapsed: header only (chevron down)", "—"),
        ("BorderPanel", "Toolbar, panels, footers, status bar", "White with 1 px border on selected sides", "—", "—", "—"),
        ("GripSplitter 6 wide", "Left edge of the settings column", "Window color, 5 grey dots #AAB2BA", "AccentLight with accent dots", "—", "—"),
        ("PanelToggle 40 high", "Hide / show the files panel", "« + 'Hide panel' in SubText", "AccentHover", "Collapsed: vertical caption + »", "—"),
        ("SidebarToggle 52 high", "Collapse / expand navigation", "« + 'Collapse' on green", "Rounded SidebarHover", "Collapsed: » only", "—"),
        ("TrialBadge", "Title bar trial pill", "#FFC107 pill 30 high, Bold text", "#FFD666", "—", "Removed after activation"),
        ("InlineLink 104 × 34", "Settings / Help links", "Icon 20 px + text", "AccentHover background", "—", "—"),
        ("PageCanvas", "ImageViewer, LayoutView", "Canvas background, page with 2/3 px shadow, 16 px margin", "Hand / I-beam / cross cursors", "Selection highlight", "Placeholder text"),
    ], [16, 20, 18, 16, 16, 14], "Components and visual states")
    d.note("Owner-drawn controls react to the mouse only; they draw no focus rectangle. The main commands also have keyboard shortcuts (5.9).")

    # ================================================================== 4
    d.h1("4. SCR-01 Main window shell")
    d.h2("4.1 Purpose and entry")
    d.p("The shell hosts all pages and the window chrome. It opens after the license check: directly with a valid license, otherwise after SCR-08 (Activate or "
        "Continue Trial). First start: maximized, normal size 1536 × 1000 px (minimum 1100 × 700); later starts restore the saved bounds if still on a monitor.")
    d.img("scr-main-annotated.png", "Main window and workspace with numbered regions (legend in the following table)", 640)
    d.table(["No.", "Element", "No.", "Element", "No.", "Element"], [
        ("1", "Logo, product name", "10", "Files / Thumbnails tabs", "19", "Result status, page"),
        ("2", "Tagline", "11", "Document list item", "20", "Splitter grip"),
        ("3", "Trial badge", "12", "Hide panel", "21", "OCR Settings card"),
        ("4", "Caption buttons", "13", "Source Page caption, viewer toolbar", "22", "Output Format card"),
        ("5", "Navigation bar", "14", "Source canvas", "23", "Export button"),
        ("6", "Collapse button", "15", "Image info footer", "24", "Status message"),
        ("7", "Toolbar", "16", "Results tabs", "25", "Counters"),
        ("8", "Settings / Help links", "17", "Formatting toolbar", "", ""),
        ("9", "Trial notification", "18", "Recognized Text view", "", ""),
    ], [6, 22, 6, 30, 6, 30], "Legend of the annotated main window")
    d.h2("4.2 Title bar")
    d.img("scr-titlebar.png", "Title bar in trial mode", 640)
    d.table(["ID", "Control", "Type", "Behavior"], [
        ("SH-01", "Title area", "Panel 52 px, Header color", "Left-drag moves the window (WM_NCLBUTTONDOWN / HTCAPTION); double-click toggles maximize / restore."),
        ("SH-02", "Logo, name, tagline", "Painted", "Not clickable; tagline in #E1F5EC, 520 px area from x 215, clipped when longer."),
        ("SH-03", "Trial badge", "TrialBadge", "'TRIAL · n days left — Activate now' (singular for 1 day); width = text + 40 px. Opens SCR-08; removed after activation."),
        ("SH-04", "Minimize", "WhiteIconButton 52 px", "Minimizes (restorable from the taskbar)."),
        ("SH-05", "Maximize / Restore", "WhiteIconButton 52 px", "Toggles the state; icon follows the state."),
        ("SH-06", "Close", "WhiteIconButton 52 px", "Hover red #C42B1C; MSG-30 while a recognition runs."),
    ], [9, 20, 20, 51], "SCR-01 title bar controls")
    d.h2("4.3 Navigation bar")
    d.img("scr-sidebar-states.png", "Navigation bar expanded and collapsed", 360)
    d.table(["ID", "Entry", "ZH", "JA", "Target"], [
        ("NAV-01", "Home", "主页", "ホーム", "SCR-02 (selected at start)"),
        ("NAV-02", "OCR", "OCR", "OCR", "SCR-04"),
        ("NAV-03", "Batch Process", "批量处理", "一括処理", "SCR-05"),
        ("NAV-04", "History", "历史记录", "履歴", "SCR-06 (reloads the list)"),
        ("NAV-05", "Settings", "设置", "設定", "SCR-07 (reloads the fields from the settings)"),
        ("NAV-06", "« Collapse / »", "收起", "折りたたむ", "Toggles 184 ↔ 64 px; tooltip 'Collapse sidebar' / 'Expand sidebar'"),
    ], [11, 22, 16, 16, 35], "SCR-01 navigation entries")
    d.p("Exactly one entry is selected (right-rounded SidebarSelected background). Other pages are hidden, not disposed, so they keep their state (lists, a running "
        "batch). Collapsed, only centred icons are drawn and names appear as tooltips; the state is stored in SidebarCollapsed.")
    d.h2("4.4 Status bar")
    d.img("scr-statusbar.png", "Status bar after recognition", 640)
    d.table(["ID", "Element", "Behavior"], [
        ("SB-01", "Status message", "520 px, inset 16. 'Ready', progress ('Recognizing report.png — report.png (1/1)'), results ('Text recognition completed — 1 page(s) in 5.4s.'), confirmations and errors."),
        ("SB-02", "Progress bar", "180 px; visible only while recognition or a batch reports a percentage."),
        ("SB-03", "Counters", "Small font: 'Total Files: n | Selected: 0/1 | Output: DOCX' (format of the settings column)."),
    ], [10, 18, 72], "SCR-01 status bar")
    d.h2("4.5 Window behavior, states and DPI")
    d.table(["Aspect", "Rule"], [
        ("Border", "Borderless, 1 px AccentDark frame (removed when maximized), system drop shadow."),
        ("Resizing", "Outer 6 px are resize handles in the normal state."),
        ("Maximize", "Limited to the monitor work area, so the taskbar stays visible."),
        ("Persistence", "Maximized flag and normal bounds are saved on close."),
        ("Trial / licensed", "Trial: badge and notification, Help ends with the trial message. Licensed: neither; Help ends with 'Licensed to … · serial … · perpetual · any computer'."),
        ("Start-up", "Missing model files: MSG-01; else the engine warms up. Command-line files are added to SCR-02."),
        ("DPI", "DPI aware; all shell and workspace sizes × DeviceDpi / 96; vector icons."),
    ], [18, 82], "SCR-01 window rules")

    # ================================================================== 5
    d.h1("5. SCR-02 Home — workspace")
    d.h2("5.1 Purpose and entry")
    d.p("The start page: documents on the left, the source page and the page rebuilt from the recognition result side by side, OCR and output settings on the "
        "right. Reached with Home, Open in workspace (SCR-04), Open source in workspace (SCR-06) and command-line files.")
    d.img("scr-main-wireframe.png", "Wireframe of the workspace at the default window size 1536 × 1000 px with the nominal sizes", 640)
    d.table(["Region", "Size", "Content"], [
        ("Toolbar", "full width × 92", "Add Files, Scan, From Clipboard | Rotate, Crop, Delete | Recognize | Export; right: Settings, Help."),
        ("Trial notification", "full width × 40", "Only while running as a trial."),
        ("Files panel", "214 / 40 wide", "Tabs Files / Thumbnails, document list or page thumbnails, Hide panel."),
        ("Source Page", "½ of centre", "Caption tab, viewer toolbar, ImageViewer, image info footer."),
        ("Results", "½ of centre", "Tabs Recognized Text / Original Image, formatting toolbar, LayoutView or overlay viewer, status line."),
        ("Settings column", "340 (280 – 640) wide", "Cards OCR Settings and Output Format, Export button; grip splitter at its left edge."),
    ], [20, 18, 62], "SCR-02 regions")
    d.p("Source Page and Results have identical header (38 + 46 px) and footer (40 px) heights, so both canvases have the same geometry and every page pixel "
        "appears at the same position on both sides (FR-20). Zoom mode, zoom and scroll are synchronized between the three canvases.")
    d.h2("5.2 Toolbar")
    d.img("scr-toolbar.png", "Toolbar and trial notification", 640)
    d.table(["ID", "Button", "Action", "Rule", "Key"], [
        ("TB-01", "Add Files", "Opens the Add menu (next table).", "Always", "Ctrl+O (file dialog)"),
        ("TB-02", "Scan", "WIA dialog; page saved as Scan_yyyyMMdd_HHmmss.png in the data folder and added.", "MSG-12 / 13", "—"),
        ("TB-03", "From Clipboard", "Adds copied files or the image as 'Clipboard_01', 'Clipboard_02' …", "MSG-11 if no image", "Ctrl+V"),
        ("TB-04", "Rotate", "Current page 90° clockwise; discards result and crop; status '{page} rotated to {n}° — recognize again to update the text.'", "Needs a page", "Ctrl+R"),
        ("TB-05", "Crop", "Crop mode on the source page (5.4); MSG-14 on a cropped page.", "Needs a page", "Esc cancels"),
        ("TB-06", "Delete", "Removes the document (MSG-16 if recognized); on the Thumbnails tab with several pages removes the page (MSG-15).", "Needs a document", "Del"),
        ("TB-07", "Recognize / Stop", "Unrecognized pages, current page first; MSG-20 if all done; no document: Add Files dialog. Running: 'Stop' with red icon, click cancels.", "Always", "F5"),
        ("TB-08", "Export", "Exports the current document (5.7).", "MSG-24 without document", "Ctrl+E"),
        ("TB-09 / 10", "Settings / Help", "SCR-07 / MSG-40.", "Always", "—"),
    ], [9, 14, 51, 14, 12], "SCR-02 toolbar inventory")
    d.p("Separators group acquisition, page editing, recognition and output; Add Files, Recognize and Export icons are Accent green to mark the main workflow.")
    d.table(["Menu item", "Result"], [
        ("Add files (PDF, images)…  Ctrl+O", "Multi-select dialog (PDF, PNG, JPG, BMP, GIF, TIFF); one document per file."),
        ("Add images as one document (image sequence)…", "Selected images become one document '{folder} ({n} images)'."),
        ("Add folder as image sequence…", "Folder images (sorted by name) become one document; PDFs in it are added individually."),
    ], [40, 60], "Add Files menu")
    d.p("The trial notification (trial only) is an amber strip with a clock icon, 'You are using the trial version of Falcon OCR — {n} of 7 days left. All features "
        "are available during the trial.', a green 'Activate now' button (SCR-08) and ✕ (tooltip 'Hide until the next start'). It is removed after activation.")
    d.h2("5.3 Files panel")
    d.img("scr-files-states.png", "Files panel: document added, document recognized, panel collapsed", 480)
    d.table(["ID", "Element", "Behavior"], [
        ("FP-01", "Tab Files", "Owner-drawn document list, item 64 px; selecting shows the first page."),
        ("FP-02", "Tab Thumbnails", "96 × 128 px page thumbnails rendered in the background; caption page number and '✓' when recognized."),
        ("FP-03", "List item", "PDF badge, image stack or image icon; name; '1 page' / 'n pages' / 'n images', then '  •  k/n done' or '  •  recognized' (green). Selected: rounded AccentLight."),
        ("FP-04", "Context menu", "See 13.3; right-click selects the item first."),
        ("FP-05", "Hide panel / »", "Collapses to a 40 px strip with the vertical caption 'Files / Thumbnails'; remembered."),
        ("FP-06", "Drop target", "Files and folders on the list, thumbnails, viewers and background."),
    ], [9, 17, 74], "SCR-02 files panel")
    d.p("Thumbnails are rebuilt whenever the Thumbnails tab is opened, the document changes or a page is rotated or cropped; a rebuild that is overtaken by a newer "
        "one is abandoned. While a recognition runs, each finished page updates the list item ('k/n done') and its thumbnail caption ('n ✓') immediately. Names "
        "longer than the panel are cut with an ellipsis; the full name can be changed with Rename….")
    d.h2("5.4 Source Page viewer")
    d.img("scr-viewer-toolbar.png", "Source Page caption and viewer toolbar", 480)
    d.table(["ID", "Control", "Tooltip", "Behavior", "Key / mouse"], [
        ("VW-01 / 02", "− / +", "Zoom out / Zoom in", "÷ / × 1.2 around the centre, 10 – 600 %.", "Ctrl+Minus / Plus, Ctrl+wheel"),
        ("VW-03", "Zoom box", "—", "Fit width, Fit page, 50 … 300 % or a typed value + Enter; 100 % = physical size.", "Enter"),
        ("VW-04 / 05", "Fit width / Fit page", "Fit width / Fit page", "Modes that follow the viewer size; Fit width is the default.", "—"),
        ("VW-06", "Rotate", "Rotate page 90°", "Same as TB-04.", "Ctrl+R"),
        ("VW-07", "Hand", "Pan (hand tool)", "Toggle; left-drag pans all canvases, line clicks disabled.", "Middle-drag"),
        ("VW-08 … 10", "‹  i / n  ›", "Previous page (PgUp) / Next page (PgDn)", "Page navigation; '0 / 0' without a document.", "PgUp / PgDn"),
        ("VW-11", "Canvas", "—", "Processed page with shadow; placeholder 'Add files to get started'; click selects a line.", "Click"),
        ("VW-12", "Info footer", "—", "'{label}  ·  {w} × {h} px  ·  {dpi} dpi' + 'rotated {n}°' / 'cropped'.", "—"),
    ], [11, 15, 20, 36, 18], "SCR-02 viewer controls")
    d.h3("Crop mode")
    d.p("Cross cursor and a dark hint bar 'Drag to select the area to keep — Esc to cancel'. While dragging, the outside is dimmed (black 43 %) and the rectangle "
        "dashed white. A rectangle over 16 × 16 page px is applied and the result discarded ('{page} cropped — recognize again to update the text.'). Crops are "
        "stored as page fractions and can be refined.")
    d.h2("5.5 Results panel")
    d.img("scr-results-states.png", "Results panel: not recognized, recognized, line selected", 620)
    d.table(["ID", "Control", "Behavior"], [
        ("RS-01 / 02", "Tabs", "Recognized Text (LayoutView, chapter 6) / Original Image (SCR-03, formatting toolbar disabled)."),
        ("RS-03", "Recognized Text view", "Placeholder 'Click Recognize to convert this page'; hover outline, click selects, double-click / F2 / Enter edits."),
        ("RS-04", "Status line", "Check icon + 'Text recognition completed — n lines, n blocks, n% confidence (n.ns).' or 'Text layer read from PDF — n lines, n blocks.'; else 'This page has not been recognized yet.' / 'No document selected.'"),
        ("RS-05", "Page label", "'i / n'."),
    ], [10, 22, 68], "SCR-02 results panel")
    d.h3("Formatting toolbar")
    d.img("scr-format-states.png", "Formatting toolbar states", 560)
    d.table(["ID", "Control", "Tooltip", "Behavior", "Enabled when"], [
        ("FT-01", "Style box", "—", "Normal, Heading 1–3, List item for the paragraph of the selected line.", "Line in a text block"),
        ("FT-02", "Font box", "Font", "Editable, auto-complete; common document fonts first, then installed fonts; selection or Enter applies.", "Line selected"),
        ("FT-03", "Size box", "Font size", "8 … 72 pt list, any 4–200 typed + Enter; invalid input restores the value.", "Line selected"),
        ("FT-04 … 06", "B / I / U", "Bold / Italic / Underline", "Toggle for the paragraph (table cell: line); checked mirrors the line.", "Line selected"),
        ("FT-07 / 08", "Lists", "Bulleted list / Numbered list", "'•' or a number continuing preceding numbered items.", "Line selected"),
        ("FT-09", "⋯", "More", "Menu, see 13.3.", "Always"),
    ], [10, 13, 18, 41, 18], "SCR-02 formatting toolbar")
    d.p("Without an own font the line shows the default font (SCR-07 or the language font). Changes report 'Font set to {font}.', 'Font size set to {n} pt.' or "
        "'Paragraph style changed to {style}.'; copy commands report 'Text copied to the clipboard.' or 'Nothing to copy — recognize the document first.'.")
    d.h2("5.6 Settings column")
    d.img_grid([("scr-ocr-card.png", "OCR Settings card"), ("scr-output-card.png", "Output Format card and Export button")], width=250)
    d.table(["ID", "Field", "Values", "Default"], [
        ("SC-01", "Document Type:", "Editable Document (Recommended) / Exact Copy (keep positions) / Plain Text", "Editable"),
        ("SC-02", "Language:", "English, Chinese (中文), Japanese (日本語), Russian (Русский); warms up the model", "English"),
        ("SC-03", "Layout Analysis:", "Automatic / Single column / Text lines only", "Automatic"),
        ("SC-04", "Detect tables and columns", "check box", "on"),
        ("SC-05", "⚙  Advanced Settings…", "opens SCR-09", "—"),
        ("SC-06", "Output format", "Microsoft Word (.docx) / Microsoft Excel (.xlsx) / HTML (.html)", "Word"),
        ("SC-07", "Output Folder:", "text box (saved on leave) + … folder dialog", "Documents\\Falcon OCR Output"),
        ("SC-08", "Export", "AccentButton, same as TB-08", "—"),
    ], [8, 22, 54, 16], "SCR-02 settings column")
    d.p("Changes are saved immediately. Cards collapse with their chevron. The grip splitter resizes the column (280 – 640 px, at least 560 px left for the "
        "centre); the width is remembered. Saving SCR-07 reloads the column.")
    d.h2("5.7 Export flow")
    d.ol(["Without a selected document: MSG-24 'Add and recognize a document first.'.",
          "Unrecognized pages: MSG-25 when no page is recognized (OK recognizes first), MSG-26 when some are (Yes recognize first, No export only recognized pages, Cancel).",
          "Target: Export writes to the output folder with a unique file name derived from the document name (the folder is created; MSG-27 if impossible); Export as… (context menu) asks for the path with a save dialog.",
          "Status 'Exporting {file}…', then 'Exported to {path}' and a History entry; with 'Open the document after export' the file opens in its associated application (or Explorer selects it).",
          "Errors: status 'Export failed.' and an error message box titled Export with the exception text."])
    d.h2("5.8 Screen states")
    d.table(["State", "Condition", "Visible behavior"], [
        ("Empty", "No document", "Placeholders, 'No document selected.', '0 / 0', formatting disabled."),
        ("Loaded", "Page not recognized", "Source image; 'Click Recognize to convert this page'; status 'Loading {page}…' then 'Ready'."),
        ("Recognizing", "Running", "Stop button; 'Recognizing {doc} — {page} (i/n)' with progress; finished pages appear at once; list 'k/n done'."),
        ("Recognized", "Page has a result", "Rebuilt page, check icon and summary; '• recognized'; thumbnail '✓'."),
        ("Selected / editing", "Line clicked / F2", "Highlight in all views; toolbar enabled; in-place editor (6.3)."),
        ("Crop mode", "Crop clicked", "Cross cursor, hint bar, dimmed rubber band; the results switch to Recognized Text."),
        ("Rotated / cropped", "Rotate or crop applied", "Result discarded, footer shows 'rotated n°' / 'cropped', results placeholder again, list state recalculated."),
        ("Exporting", "Export running", "Status 'Exporting {file}…', then 'Exported to {path}'; the document opens when 'Open the document after export' is on."),
        ("PDF text layer", "PDF page with embedded text", "Result read without OCR; status 'Text layer read from PDF — n lines, n blocks.' without confidence."),
        ("Stopped / failed", "Stop / exception", "'Recognition stopped.' / 'Recognition failed.' + MSG-21; finished pages keep results."),
    ], [16, 20, 64], "SCR-02 states")
    d.h2("5.9 Keyboard shortcuts and focus")
    d.table(["Key", "Action", "Condition"], [
        ("Ctrl+O", "Add files dialog", "Workspace visible"),
        ("F5 / Ctrl+E / Ctrl+R", "Recognize (Stop) / Export / Rotate", "Workspace visible"),
        ("PgUp / PgDn, Ctrl+Plus / Minus", "Page / zoom", "Workspace visible"),
        ("Ctrl+V", "From Clipboard", "Focus not in a text box or combo box"),
        ("Del", "Delete document / page", "Focus not in a text box or combo box"),
        ("F2 / Enter, then Enter / Esc", "Edit line, commit / cancel", "Recognized Text view, line selected"),
        ("Esc", "Leave crop mode", "Crop mode"),
    ], [30, 35, 35], "SCR-02 keyboard shortcuts")
    d.p("Tab order follows creation order: zoom box, viewer, style, font and size boxes, Recognized Text view, then the settings column top-down. Clicking a canvas "
        "focuses it for wheel and edit keys.")
    d.h2("5.10 Resize behavior")
    d.ul(["Toolbar left aligned, Settings / Help right aligned; files panel and settings column keep their widths.",
          "The centre splits 50 : 50 at first display (8 px splitter); Fit modes follow the size, custom zoom keeps the scroll fraction.",
          "Card contents are docked top-down, so the settings column never scrolls horizontally; the zoom box keeps the typed text while it has the focus.",
          "At the minimum size (1100 × 700) collapsing the navigation or files panel gives the canvases more room."])

    # ================================================================== 6
    d.h1("6. SCR-03 Original Image and results rendering rules")
    d.h2("6.1 Original Image tab")
    d.img("main-overlay.png", "Original Image tab: detected lines (green), table (blue), picture (orange, dashed), blocks (dotted)", 640)
    d.p("The tab shows the processed image with the analysis on top, to check what the layout analysis found. Clicking a line selects it everywhere; the "
        "formatting toolbar is disabled; zoom and scroll stay synchronized.")
    d.img("scr-overlay-zoom.png", "Overlay detail: table cells, text line polygons and the dashed picture frame", 420)
    d.table(["Element", "Pen / fill (widths in screen px at any zoom)", "Drawn for"], [
        ("Text line", "Outline #13865C alpha 200, 1.5 px; fill #13865C alpha 28", "Every line polygon outside pictures"),
        ("Table", "#185ABD alpha 220, 2.5 px", "Table block rectangle"),
        ("Picture", "#E47814 alpha 220, 2.5 px, dashed", "Figure block rectangle"),
        ("Paragraph block", "#5A5A5A alpha 150, 1 px, dotted, 3 px outside", "Other blocks"),
        ("Selected line", "Fill Selection (alpha 70), outline Accent 2 px", "Also on the Source Page"),
    ], [20, 50, 30], "Overlay rendering")
    d.h2("6.2 Recognized Text rendering")
    d.p("The LayoutView redraws the page from the result at the original positions: page and paragraph background colors, pictures cut from the source, tables "
        "with fills and borders, and each line with its estimated font, size, weight, italic, underline and color. Each line is scaled horizontally (0.4 – 2.5) to the "
        "ink width of the source line, which keeps both sides aligned even without the exact font (fallback Segoe UI). Borderless tables get dotted blue cell outlines.")
    d.img("scr-selection-zoom.png", "The selected line on the Source Page and in the Recognized Text view", 540)
    d.table(["Visual", "Appearance", "Rule"], [
        ("Hover", "Outline Accent alpha 120, I-beam cursor", "Line under the mouse (not with the hand tool)."),
        ("Selection", "Fill Accent alpha 40, outline Accent 2 px", "Scrolled into view (centred) when selected in another view; empty click clears."),
        ("Low confidence", "#FFD600 at 35 %", "Characters below 0.75 confidence (or the whole line); only with 'Highlight uncertain characters'; never on edited lines."),
        ("Edited line", "Dashed underline #185ABD alpha 160", "Lines changed by the user (confidence set to 100 %)."),
    ], [18, 36, 46], "Results view visual rules")
    d.h2("6.3 In-place editing")
    d.p("Double-click, F2 or Enter opens a text box over the line (line width + 40 px, at least 160 px) in the line's font at the current zoom (8 – 28 pt), text "
        "selected. Enter or clicking elsewhere commits, Esc cancels, scrolling commits. The status shows 'Edited: {text}'; edits are exported.")

    # ================================================================== 7
    d.h1("7. SCR-04 Quick OCR")
    d.p("Recognizes one image immediately and shows its plain text without touching the workspace. Subtitle: 'Paste (Ctrl+V), drop or open an image to get its text "
        "instantly — nothing is saved.'.")
    d.img("scr-quickocr-annotated.png", "Quick OCR page with numbered controls", 600)
    d.table(["No.", "Control", "Behavior"], [
        ("1", "Title, subtitle", "Page header (Title 15 pt, SubText)."),
        ("2", "Open image…", "Dialog 'Images and PDF'; the first page is recognized."),
        ("3", "Paste image", "Clipboard image or first copied file; also Ctrl+V outside the text box."),
        ("4", "Copy text", "Copies the text (when not empty)."),
        ("5", "Open in workspace", "Adds the source file to SCR-02 and navigates there (clipboard images are not added)."),
        ("6", "Image area", "Placeholder 'Drop an image here or press Ctrl+V'; drop target; green line overlay after recognition."),
        ("7", "Text", "Editable multi-line box, 11 pt, no wrap."),
        ("8", "Info line", "'Recognizing…', then 'n lines · n% confidence · n.ns · {language}' or 'Recognition failed: {error}'."),
    ], [7, 22, 71], "SCR-04 controls")
    d.p("The body is split 50 : 50 (12 px splitter). A new image replaces the previous one; a late result of a replaced image is ignored.")
    d.table(["State", "Image area", "Text / info line"], [
        ("Empty", "Placeholder 'Drop an image here or press Ctrl+V'", "Empty / empty"),
        ("Recognizing", "Image without overlay", "Cleared / 'Recognizing…'"),
        ("Recognized", "Image with green line overlay", "Plain text / lines, confidence, seconds, language"),
        ("Failed", "Image without overlay", "Empty / 'Recognition failed: {error}'"),
    ], [18, 40, 42], "SCR-04 states")

    # ================================================================== 8
    d.h1("8. SCR-05 Batch Process")
    d.p("Recognizes and exports many files or folders unattended, in three steps: 1. Inputs, 2. Options, 3. Run.")
    d.img("scr-batch-annotated.png", "Batch Process page with numbered controls", 600)
    d.table(["No.", "Control", "Behavior"], [
        ("1", "Title, subtitle", "'Recognize and export many documents at once. Folders can be treated as one multi-page document.'"),
        ("2", "Add files… / Add folder… / Clear list", "File dialog 'PDF and images', folder dialog, clear (not while running); drag and drop also works."),
        ("3", "Input list", "Columns Input, Type, Status; 220 px high."),
        ("4", "Treat each folder as one document (image sequence)", "Default on: a folder's images form one input; its PDFs are added individually. Off: every supported file."),
        ("5 – 7", "Output format / Document type / Language", "Word, Excel, HTML, Plain text (.txt) / Editable, Exact Copy, Plain Text / recognition language; reloaded from Settings when the page is shown (not while running)."),
        ("8", "Output folder (double-click to browse)", "Empty = default folder; created when missing."),
        ("9", "▶  Start / ■  Stop", "Accent 140 × 38, red while running; Stop cancels after the current page."),
        ("10", "Progress bar", "Processed inputs in %; status bar 'Batch: i/n documents'."),
        ("11", "Log", "Time-stamped result lines."),
    ], [8, 30, 62], "SCR-05 controls")
    d.table(["Status value", "ZH / JA", "Meaning"], [
        ("Waiting", "等待中 / 待機中", "Added, not yet processed."),
        ("Recognizing…", "正在识别… / 認識中…", "Document opened, recognition starting."),
        ("Page i/n", "第 i/n 页 / ページ i/n", "Page progress of the current document."),
        ("Done", "完成 / 完了", "Recognized and exported; skipped by the next Start."),
        ("Failed", "失败 / 失敗", "Error; the log contains the message; retried by the next Start."),
        ("Stopped", "已停止 / 停止", "Cancelled by the user; retried by the next Start."),
    ], [20, 30, 50], "SCR-05 status values")
    d.p("Because Done items are skipped, a stopped or partly failed batch can simply be started again. The Type column shows the file extension in upper case "
        "(PDF, PNG, …) or 'Image sequence' for folders. Log example:")
    d.code(["08:12:03  Started: 3 document(s) → DOCX in C:\\Users\\…\\Documents\\Falcon OCR Output",
            "08:12:09  ✓ report.png (1 p., 5.4s) → report.docx",
            "08:12:15  ✗ C:\\scans\\broken.pdf: {error message}",
            "08:12:40  Finished: 2 succeeded, 1 failed, 37s."])
    d.p("At the end the status bar shows 'Batch finished: n succeeded, n failed.' and MSG-35 offers to open the output folder; an empty list gives MSG-34.")

    # ================================================================== 9
    d.h1("9. SCR-06 History")
    d.p("Recent recognitions and exports, newest first, at most 500 entries (history.json). The list reloads when shown and when entries are added.")
    d.img("scr-history-annotated.png", "History page with numbered controls", 600)
    d.table(["No.", "Control", "Behavior"], [
        ("1", "Title, subtitle", "'Recent recognitions and exports.'"),
        ("2", "Open output", "Opens the output file (also double-click); nothing when missing."),
        ("3", "Show in folder", "Explorer with the output selected."),
        ("4", "Open source in workspace", "Adds the source to SCR-02 and navigates there."),
        ("5", "Clear history", "MSG-36."),
        ("6 – 7", "Columns / rows", "Time (yyyy-MM-dd HH:mm), Source, Output ('—' for recognitions), Format (Recognition, DOCX, XLSX, HTML, TEXT), Pages, Language, Duration."),
    ], [8, 28, 64], "SCR-06 controls")

    # ================================================================== 10
    d.h1("10. SCR-07 Settings")
    d.p("Defaults for recognition and export ('Defaults for recognition and export. Everything runs offline on this computer.') in a scrollable two-column grid "
        "(captions 240 px, combo boxes 300 px); the buttons stay at the bottom.")
    d.img("scr-settings-annotated.png", "Settings page with numbered sections and fields", 600)
    d.table(["No.", "Field", "Values", "Default"], [
        ("1 – 2", "General · Interface language:", "English / 简体中文 (Chinese) / 日本語 (Japanese)", "Windows language"),
        ("3", "Recognition", "Section title (Heading, Accent)", "—"),
        ("4", "Default language:", "English, Chinese (中文), Japanese (日本語), Russian (Русский); same list as SC-02", "English"),
        ("5", "Layout analysis:", "Automatic (columns, tables, figures) / Single column / Text lines only", "Automatic"),
        ("6", "Check boxes", "Detect tables and columns · Recognize automatically when files are added · Highlight uncertain characters in the results view", "on · off · on"),
        ("7", "Default font:", "Automatic (by recognition language) or an installed font; hint: applies to pages recognized afterwards", "Automatic"),
        ("8 – 9", "Export · format · type · Output folder:", "Word / Excel / HTML / Plain text (.txt) · Editable / Exact Copy / Plain Text · folder (double-click)", "Word · Editable · Documents\\Falcon OCR Output"),
        ("10", "Check boxes", "Keep text, fill and page colors · Include pictures in exported documents · Open the document after export", "on · on · on"),
        ("11", "Advanced recognition", "Same fields as SCR-09", "chapter 12"),
        ("—", "License", "'✓ Licensed to …' / '⏳ Trial version — n of 7 days remaining (ends date).' / '✗ …' and 'Machine code: …'; Change license key… (SCR-08), Copy machine code", "—"),
        ("12 – 14", "Save · Restore defaults · Open data folder", "Save applies ('Settings saved.'); Restore defaults asks MSG-37 and fills the defaults; data folder %LOCALAPPDATA%\\FalconOCR", "—"),
    ], [9, 27, 46, 18], "SCR-07 fields")
    d.p("Save writes all fields to settings.json, reloads the workspace settings column and shows 'Settings saved.' in the status bar. A changed interface "
        "language asks MSG-38 to restart, because texts are created together with the windows. Leaving the page without Save discards the edits: the fields are "
        "reloaded from the settings whenever the page is shown, together with the license text. Restore defaults only fills the fields; nothing changes until Save. "
        "An empty output folder is replaced by the default folder on Save. The embedded advanced fields and SCR-09 edit the same values, so a change in one appears "
        "in the other after saving.")
    d.table(["State", "License line"], [
        ("Trial", "⏳ Trial version — n of 7 days remaining (ends yyyy-MM-dd). / Machine code: XXXX-XXXX-XXXX-XXXX"),
        ("Licensed", "✓ Licensed to {name} · serial {n} · perpetual (or valid until date) · any computer (or this computer only)"),
        ("Invalid key installed", "✗ followed by the validation message, e.g. 'This license key was issued for a different computer.'"),
    ], [22, 78], "SCR-07 license states")

    # ================================================================== 11
    d.h1("11. SCR-08 Activation window")
    d.p("Fixed dialog (client 620 × 530 px, fixed 96-dpi coordinates) with a 76 px green header. It appears at every start without a valid license and from the "
        "trial badge, Activate now and Settings → Change license key….")
    d.img("scr-activation-annotated.png", "Activation window while the trial is running, with numbered elements", 400)
    d.table(["No.", "Element", "Behavior"], [
        ("1", "Header", "'Activate Falcon OCR' and subtitle 'You are using the trial version — n of 7 days left.' / 'Your trial period has ended. A license key is required to continue.' / 'Enter your license key.'"),
        ("2", "Trial banner", "Green: 'Trial version — n of 7 days remaining (ends date).  All features are available during the trial.'. Red: 'The 7-day trial ended on date.' or the tamper message."),
        ("3 – 4", "Machine code, Copy", "Read-only Consolas 13 pt box; Copy shows 'Machine code copied to the clipboard.' in green."),
        ("5", "License key box", "'2. Enter or paste the license key:'; multi-line Consolas 10.5 pt; spaces and line breaks allowed."),
        ("6", "Status line", "Red validation message (next table); cleared when the key changes."),
        ("7", "Load license file…", "Reads a .lic, .key or .txt file."),
        ("8", "Activate", "Validates and installs; MSG-41; closes with OK."),
        ("9", "Continue Trial", "Start-up during the trial only."),
        ("10", "Exit / Cancel", "Exit at start-up (closes the application), Cancel otherwise."),
    ], [8, 20, 72], "SCR-08 elements")
    d.table(["Validation result", "Status line text"], [
        ("Missing / malformed", "No license key has been entered. / The license key is not in a valid format. Check that it was copied completely."),
        ("Invalid signature / wrong machine", "The license key is not genuine. / This license key was issued for a different computer."),
        ("Expired / clock tampered", "The license expired on yyyy-MM-dd. / The system clock is set earlier than the last use of the application."),
    ], [26, 74], "SCR-08 validation messages")
    d.table(["Mode", "Subtitle / banner", "Buttons", "Result"], [
        ("Start-up, trial running", "Days left / green", "Load license file…, Activate, Continue Trial, Exit", "Licensed, trial mode or closed"),
        ("Start-up, trial ended or tampered", "Trial ended / red", "Load license file…, Activate, Exit", "Licensed or closed"),
        ("From the running application", "Days left or 'Enter your license key.'", "Load license file…, Activate, Cancel", "Badge and notification removed on success"),
    ], [26, 24, 30, 20], "SCR-08 modes")
    d.p("Modes: at start-up during the trial all buttons are shown (Continue Trial → trial mode); after the trial Continue Trial is hidden and anything but Activate "
        "closes the application; from the running application the last button is Cancel and success removes the badge and the notification ('Activated — …'). "
        "Enter = Activate, Esc = Exit / Cancel; tab order: machine code, Copy, key box, Load license file…, Activate, Continue Trial, Exit / Cancel.")
    d.img("activation-expired.png", "Activation window after the trial has ended", 380)

    # ================================================================== 12
    d.h1("12. SCR-09 Advanced OCR Settings")
    d.p("Modal, auto-sized dialog 'Advanced OCR Settings' (ZH 高级 OCR 设置, JA OCR 詳細設定), centred on the main window; the same editor is embedded in SCR-07.")
    d.table(["Field", "Range / step", "Default", "Effect"], [
        ("PDF input:", "Use the PDF text layer when present (fast, exact) / Always run OCR (ignore embedded text)", "Text layer", "FR-12"),
        ("PDF rendering resolution (dpi):", "100 – 600 / 25", "200", "Quality vs. speed"),
        ("Detector max. image side (px):", "960 – 6000 / 160", "2560", "Small text vs. speed"),
        ("Text pixel threshold:", "0.05 – 0.90 / 0.05", "0.30", "Lower finds faint text"),
        ("Text box threshold:", "0.10 – 0.95 / 0.05", "0.60", "Lower keeps uncertain boxes"),
        ("Box expansion (unclip ratio):", "1.0 – 3.0 / 0.1", "1.5", "Box padding"),
        ("Minimum line confidence:", "0.00 – 0.95 / 0.05", "0.50", "Drops unreliable lines"),
        ("CPU threads (0 = automatic):", "0 – 64 / 1", "0", "0 = one per physical core"),
        ("Correct upside-down text lines (orientation classifier)", "check box", "off", "FR-13"),
        ("Keep pictures and graphics as images", "check box", "on", "FR-18"),
    ], [34, 36, 12, 18], "SCR-09 fields")
    d.p("Buttons: Defaults (reload defaults, stay open), OK (Enter, saves), Cancel (Esc). Out-of-range stored values are clamped.")

    # ================================================================== 13
    d.h1("13. SCR-10 Dialogs, menus and message catalogue")
    d.h2("13.1 Rename dialog")
    d.p("'Rename document': 420 × 120 px, caption 'Name:', pre-filled text box, OK / Cancel. Blank names are ignored; the name is trimmed and used for export file names.")
    d.h2("13.2 Message catalogue")
    d.p("Standard message boxes owned by the application window; titles in brackets.")
    d.table(["ID", "Trigger", "Text", "Buttons"], [
        ("MSG-01", "Start-up, models missing", "[Falcon OCR] Some OCR model files are missing from the installation: {files} Run tools\\fetch-dependencies.ps1 on a connected machine and rebuild.", "OK"),
        ("MSG-02", "Recognize, model missing", "[Recognize] OCR models are missing: {files}", "OK"),
        ("MSG-03", "32-bit process", "[Falcon OCR] Falcon OCR must run as a 64-bit process.", "OK"),
        ("MSG-04", "Unhandled error", "[Falcon OCR] An unexpected error occurred: {message} Details were written to: {path}\\error.log", "OK"),
        ("MSG-10", "Add files errors", "[Add Files] Some files could not be opened: {file}: {reason}", "OK"),
        ("MSG-11", "Clipboard empty", "[From Clipboard] The clipboard does not contain an image.", "OK"),
        ("MSG-12 / 13", "Scan", "[Scan] Windows Image Acquisition (WIA) is not available on this computer. / No scanner was found. Connect a WIA-compatible scanner and try again.", "OK"),
        ("MSG-14", "Crop a cropped page", "[Crop] This page is already cropped. Yes = crop further, No = restore the full page.", "Yes / No / Cancel"),
        ("MSG-15 / 16", "Delete page / document", "[Delete] Remove {page} from \"{doc}\"? / Remove \"{doc}\" and its recognition results?", "OK / Cancel"),
        ("MSG-17", "Remove all", "[Remove all] Remove all documents from the workspace?", "OK / Cancel"),
        ("MSG-20", "Recognize again", "[Recognize] \"{doc}\" is already recognized. Recognize it again with the current settings?", "OK / Cancel"),
        ("MSG-21", "Operation error", "[Recognize] / [Export] / [Scan] / [From Clipboard] {exception message}", "OK"),
        ("MSG-22", "Image sequence error", "[Add image sequence] {exception message}", "OK"),
        ("MSG-24", "Export, no document", "[Export] Add and recognize a document first.", "OK"),
        ("MSG-25", "Export, not recognized", "[Export] \"{doc}\" has not been recognized yet. Recognize it now?", "OK / Cancel"),
        ("MSG-26", "Export, partly recognized", "[Export] {n} of {m} pages are not recognized. Yes = recognize them first No = export only the recognized pages", "Yes / No / Cancel"),
        ("MSG-27", "Output folder", "[Export] Cannot create the output folder: {reason}", "OK"),
        ("MSG-30", "Close while running", "[Falcon OCR] Recognition is still running. Stop it and exit?", "OK / Cancel"),
        ("MSG-34 / 35", "Batch", "[Batch Process] Add files or folders first. / {n} document(s) exported. Open the output folder?", "OK / Yes No"),
        ("MSG-36 / 37", "Clear / reset", "[History] Clear the whole history? / [Settings] Restore all settings to their defaults?", "OK / Cancel"),
        ("MSG-38", "UI language", "[Interface language] The interface language is changed after a restart. Restart Falcon OCR now?", "Yes / No"),
        ("MSG-40", "Help", "[Help] Four workflow steps, shortcuts and the license or trial status.", "OK"),
        ("MSG-41", "Activated", "[Activated] Thank you — Falcon OCR is activated. Licensed to {name} · serial {n} · perpetual · any computer", "OK"),
    ], [11, 19, 56, 14], "Message catalogue")
    d.p("Warnings use the warning icon (MSG-01, 10, 30), confirmations the question icon, results and hints the information icon (MSG-11, 24, 34, 35, 40, 41) "
        "and failures the error icon. Status-bar messages complement the boxes and never require a click. KeyGen messages (SCR-11) use the same icons in English.")
    d.h2("13.3 Context menus")
    d.table(["Menu", "Items"], [
        ("File list", "Recognize · Recognize all documents · Export · Export as… | Rename… · Open containing folder | Remove · Remove all"),
        ("Add Files", "Add files (PDF, images)…  Ctrl+O · Add images as one document (image sequence)… · Add folder as image sequence…"),
        ("⋯ (formatting)", "✓ Highlight uncertain characters | Copy page text · Copy document text | Recognize this page again"),
    ], [20, 80], "Context menus")

    # ================================================================== 14
    d.h1("14. SCR-11 License Key Generator (vendor)")
    d.p("Vendor-only, English-only tool with the same header and accent colors and standard system controls; resizable (780 × 900, minimum 700 × 700) with a "
        "scrollable two-column body. The screenshot is a documentation sample whose signing key does not match the build (orange warning).")
    d.img("scr-keygen-annotated.png", "License Key Generator with numbered controls", 460)
    d.table(["No.", "Control", "Behavior"], [
        ("1 – 2", "Header, status", "'Fingerprint …' with '✓ Matches the public key compiled into this Falcon OCR build.' (green) or '⚠ Does not match …' (orange); 'No signing key loaded.' (red)."),
        ("3", "Signing key buttons", "New signing key… (warns before replacing, then backup and public key export), Import backup…, Backup key…, Write public key to app…"),
        ("4 – 6", "Licensee, Machine code, Any computer", "Licensee required; machine code upper case, disabled with Any computer."),
        ("7 – 9", "Expiry, Serial, Note", "Perpetual (default) or date (today + 1 year), 30 days, 1 year; next serial; note for the log only."),
        ("10 – 12", "Generate License Key, key box, actions", "Accent 240 × 42; read-only Consolas key; Copy key, Save as .lic file…, Open issued log."),
        ("13 – 15", "Verify a key", "Key box, Verify, result 'Signature OK' (or '— EXPIRED') with licensee, serial, dates and machine, or the validation message in red."),
    ], [9, 28, 63], "SCR-11 controls")
    d.p("Without a signing key the tool asks 'No signing key exists yet. Create a new signing key now? (Or choose No and import a backup.)'.")

    # ================================================================== 15
    d.h1("15. Localization layout rules")
    d.p("FalconOcr.exe is available in English, Simplified Chinese and Japanese. Source strings are English; translations come from lang/zh_CN.po and lang/ja.po "
        "(gettext, editable with Poedit); missing entries fall back to English. The first start follows the Windows display language.")
    d.img("ui-japanese.png", "Workspace with the Japanese interface", 600)
    d.ul(["Layouts are identical in all languages; texts are measured at run time (trial badge) or truncated with an ellipsis (tool buttons, tabs, names).",
          "Font per language: Segoe UI, Microsoft YaHei UI, Yu Gothic UI; semibold becomes bold for CJK.",
          "Translations use full-width punctuation (：, （）, 。) and keep the trailing colon of captions.",
          "Shortcuts, file extensions, format names and the product name are not translated; dates are yyyy-MM-dd."])
    d.img("scr-l10n-toolbar.png", "Toolbar captions in English, Chinese and Japanese", 580)
    d.img("scr-l10n-settings-column.png", "Settings column in English, Chinese and Japanese", 540)
    d.table(["English", "Chinese (zh_CN)", "Japanese (ja)"], [
        ("Add Files / From Clipboard", "添加文件 / 从剪贴板", "ファイルを追加 / クリップボードから"),
        ("Recognize / Stop", "识别 / 停止", "認識 / 停止"),
        ("Recognized Text / Original Image", "识别文本 / 原始图像", "認識テキスト / 元の画像"),
        ("OCR Settings / Output Format", "OCR 设置 / 输出格式", "OCR 設定 / 出力形式"),
        ("TRIAL · {0} days left — Activate now", "试用版 · 剩余 {0} 天 — 立即激活", "試用版 · 残り {0} 日 — 今すぐアクティベート"),
        ("Total Files: {0} | Selected: {1} | Output: {2}", "文件总数：{0} | 已选择：{1} | 输出：{2}", "ファイル数：{0} | 選択：{1} | 出力：{2}"),
    ], [36, 30, 34], "Localization examples")
    d.p("Known untranslated items: the section title 'License' in SCR-07, 'OK' in SCR-09 and the Rename dialog, generated page labels ('Page 1'), stored History "
        "values, and SCR-11. The catalogs still contain 'Korean (한국어)'; Korean is not offered in version 1.0.")

    # ================================================================== 16
    d.h1("16. Accessibility notes")
    d.table(["Topic", "Design"], [
        ("Keyboard", "Workspace commands have shortcuts; standard controls and dialogs (Accept / Cancel) are keyboard operable. Owner-drawn buttons are mouse only — new commands need a shortcut or a standard control."),
        ("Contrast", "Text #1F2937 on white and white on #0E6E4C / #13865C exceed 4.5 : 1; SubText on white is about 4.9 : 1."),
        ("Color independence", "Recognition state also appears as text; overlay colors are paired with solid, dashed and dotted lines."),
        ("Scaling and tooltips", "DPI-aware layout, vector icons; icon-only buttons have translated tooltips. Test at 100 – 200 %."),
    ], [22, 78], "Accessibility")

    # ================================================================== 17
    d.h1("17. Navigation map")
    d.img("scr-navigation-diagram.png", "Screen transition diagram", 640)
    d.table(["From", "Action", "To"], [
        ("Start-up (licensed)", "—", "SCR-01 with SCR-02"),
        ("Start-up (unlicensed)", "Activate / Continue Trial / Exit", "SCR-01 licensed / SCR-01 trial / application closed"),
        ("Start-up (trial ended)", "Activate / Exit", "SCR-01 licensed / application closed"),
        ("Any page", "Navigation bar", "SCR-02, 04, 05, 06, 07"),
        ("SCR-01 title bar", "Trial badge", "SCR-08"),
        ("SCR-02", "Activate now (trial notification)", "SCR-08"),
        ("SCR-02", "Toolbar Settings / Help", "SCR-07 / MSG-40"),
        ("SCR-02", "Advanced Settings…", "SCR-09"),
        ("SCR-02", "Context menu Rename…", "SCR-10 Rename dialog"),
        ("SCR-02 results", "Original Image tab", "SCR-03"),
        ("SCR-04", "Open in workspace", "SCR-02"),
        ("SCR-06", "Open source in workspace", "SCR-02"),
        ("SCR-07", "Change license key…", "SCR-08"),
        ("SCR-01", "Close (recognition running)", "MSG-30, then closed"),
    ], [30, 35, 35], "Navigation")
    return d
