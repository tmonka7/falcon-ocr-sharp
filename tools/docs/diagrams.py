"""Draws the diagrams used by the Falcon OCR documents (docs/images/diagram-*.png)."""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1] if len(sys.argv) > 1 else "docs/images"
F = r"C:\Windows\Fonts"
GREEN, DARK, LIGHT, BLUE, ORANGE, GRAY, TEXT = (19, 134, 92), (14, 110, 76), (225, 243, 235), (24, 90, 189), (228, 120, 20), (120, 128, 138), (31, 41, 55)


def font(size, bold=False):
    return ImageFont.truetype(os.path.join(F, "segoeuib.ttf" if bold else "segoeui.ttf"), size)


def box(d, xy, title, lines=(), fill=LIGHT, outline=GREEN, title_color=DARK, size=26, line_size=20):
    x0, y0, x1, y1 = xy
    d.rounded_rectangle(xy, radius=14, fill=fill, outline=outline, width=3)
    d.text(((x0 + x1) / 2, y0 + 14), title, font=font(size, True), fill=title_color, anchor="ma")
    y = y0 + 20 + size * 1.3
    for ln in lines:
        d.text(((x0 + x1) / 2, y), ln, font=font(line_size), fill=TEXT, anchor="ma")
        y += line_size * 1.4


def arrow(d, a, b, color=GRAY, width=4, label=None):
    d.line([a, b], fill=color, width=width)
    import math
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    L = 16
    p1 = (b[0] - L * math.cos(ang - 0.45), b[1] - L * math.sin(ang - 0.45))
    p2 = (b[0] - L * math.cos(ang + 0.45), b[1] - L * math.sin(ang + 0.45))
    d.polygon([b, p1, p2], fill=color)
    if label:
        d.text(((a[0] + b[0]) / 2 + 8, (a[1] + b[1]) / 2 - 26), label, font=font(18), fill=GRAY)


def architecture():
    im = Image.new("RGB", (1800, 1180), "white")
    d = ImageDraw.Draw(im)
    d.text((900, 20), "Falcon OCR — component architecture", font=font(34, True), fill=TEXT, anchor="ma")
    # Front ends
    box(d, (60, 90, 620, 290), "FalconOcr.App (WinForms)", ["Workspace · Quick OCR · Batch", "History · Settings · Activation", "Synchronised viewers, editing"])
    box(d, (660, 90, 1140, 290), "FalconOcr.Cli", ["falcon-ocr.exe", "batch / automation", "--license, --machine-code"])
    box(d, (1180, 90, 1740, 290), "FalconOcr.KeyGen (vendor)", ["Signing key (DPAPI)", "Generate / verify keys", "Issued-key log"], fill=(253, 242, 228), outline=ORANGE, title_color=(150, 70, 0))
    # Core
    d.rounded_rectangle((60, 360, 1740, 900), radius=18, outline=DARK, width=4, fill=(250, 252, 251))
    d.text((900, 372), "FalconOcr.Core (class library, .NET Framework 4.7)", font=font(28, True), fill=DARK, anchor="ma")
    mods = [
        ("Input", ["OcrDocument, DocumentPage", "PDF / image / TIFF / sequence", "rotate · crop · page cache"]),
        ("Processing", ["OcrProcessor", "text layer or OCR", "→ LayoutAnalyzer"]),
        ("Engine", ["PaddleOcrEngine", "DBNet detector", "SVTR/CTC recognizer"]),
        ("Layout", ["styles · tables · figures", "columns · reading order", "paragraphs · headings · lists"]),
        ("Export", ["DOCX (OpenXML)", "XLSX (OpenXML)", "HTML · TXT"]),
        ("Licensing", ["LicenseManager (ECDSA)", "TrialManager (7 days)", "MachineIdentity"]),
    ]
    w = (1640 - 5 * 16) / 6
    for i, (t, ls) in enumerate(mods):
        x0 = 80 + i * (w + 16)
        box(d, (x0, 430, x0 + w, 640), t, ls, fill="white", size=24, line_size=16)
    box(d, (80, 680, 560, 870), "Imaging", ["RgbImage (BGR)", "resize · rotated crop", "no OpenCV dependency"], fill="white")
    box(d, (600, 680, 1140, 870), "Pdf", ["PDFium P/Invoke", "render pages", "text layer + fonts/colors"], fill="white")
    box(d, (1180, 680, 1720, 870), "Model", ["OcrPage · TextLine", "LayoutBlock · Section", "TableRegion · FigureRegion"], fill="white")
    for x in (340, 900):
        arrow(d, (x, 290), (x, 360))
    arrow(d, (1460, 290), (1460, 360), color=ORANGE)
    # Native / data
    box(d, (60, 960, 560, 1150), "ONNX Runtime 1.22", ["onnxruntime.dll (CPU)", "VC++ runtime app-local"], fill=(236, 242, 252), outline=BLUE, title_color=BLUE)
    box(d, (600, 960, 1140, 1150), "PaddleOCR PP-OCRv5", ["det · cls · rec (en/zh+ja/ko/ru)", "models\\ (49.5 MB, ONNX)"], fill=(236, 242, 252), outline=BLUE, title_color=BLUE)
    box(d, (1180, 960, 1740, 1150), "PDFium · OpenXML SDK", ["pdfium.dll", "DocumentFormat.OpenXml 2.20"], fill=(236, 242, 252), outline=BLUE, title_color=BLUE)
    for x in (310, 870, 1460):
        arrow(d, (x, 900), (x, 960), color=BLUE)
    im.save(os.path.join(OUT, "diagram-architecture.png"))


def pipeline():
    im = Image.new("RGB", (1800, 760), "white")
    d = ImageDraw.Draw(im)
    d.text((900, 20), "Page processing pipeline", font=font(34, True), fill=TEXT, anchor="ma")
    steps = [
        ("1 Load", ["render PDF 200 dpi", "or decode image", "rotate · crop"]),
        ("2 Text", ["PDF text layer?", "yes → exact lines", "no → OCR"]),
        ("3 Detect", ["DBNet prob. map", "components → boxes", "unclip 1.5"]),
        ("4 Recognize", ["crop + 48 px lines", "SVTR + CTC decode", "char positions"]),
        ("5 Analyse", ["styles · rules", "tables · figures", "sections · blocks"]),
        ("6 Export", ["DOCX · XLSX", "HTML · TXT", "editable / exact"]),
    ]
    w = 260
    for i, (t, ls) in enumerate(steps):
        x0 = 40 + i * (w + 36)
        box(d, (x0, 110, x0 + w, 330), t, ls, fill="white" if i != 2 and i != 3 else LIGHT)
        if i < len(steps) - 1:
            arrow(d, (x0 + w + 2, 220), (x0 + w + 34, 220))
    d.text((40, 380), "Layout analysis (step 5) in detail", font=font(28, True), fill=DARK)
    sub = [
        "StyleEstimator: ink extent → font size (pt), stroke width → bold, strongest ink → color, fill behind text",
        "TableDetector: long thin strokes with background on both sides → grid → merged cells (union-find) → cell text",
        "FigureDetector: non-background cells minus text/rules → components → pictures (PNG crops); text panels stay text",
        "Segmentation: bands of overlapping lines → sections with common gutters → columns (reading order: top-down, left-right)",
        "Blocks: visual lines → paragraphs (gap, style, indent, short line, list marker) → headings (≥1.18× body size / bold)",
        "Bullets: recognised markers, separate marker boxes, or drawn blobs left of the line (pixel test)",
    ]
    y = 430
    for s in sub:
        d.ellipse((50, y + 10, 62, y + 22), fill=GREEN)
        d.text((80, y), s, font=font(22), fill=TEXT)
        y += 48
    im.save(os.path.join(OUT, "diagram-pipeline.png"))


def licensing():
    im = Image.new("RGB", (1600, 1150), "white")
    d = ImageDraw.Draw(im)
    d.text((800, 20), "Start-up licensing flow", font=font(34, True), fill=TEXT, anchor="ma")

    def diamond(cx, cy, w, h, t):
        d.polygon([(cx, cy - h / 2), (cx + w / 2, cy), (cx, cy + h / 2), (cx - w / 2, cy)], fill=(255, 248, 225), outline=ORANGE, width=3)
        d.text((cx, cy - 14), t, font=font(22, True), fill=(120, 60, 0), anchor="ma")

    def label(x, y, t):
        d.text((x, y), t, font=font(20, True), fill=GRAY)

    box(d, (600, 80, 1000, 160), "Application start")
    arrow(d, (800, 160), (800, 210))
    diamond(800, 280, 480, 140, "Valid license key installed?")
    arrow(d, (1040, 280), (1180, 280)); label(1080, 245, "yes")
    box(d, (1180, 225, 1560, 335), "Main window", ["licensed mode"])
    arrow(d, (800, 350), (800, 410)); label(815, 365, "no")
    diamond(800, 480, 420, 140, "Trial still running?")
    # trial running (left) / trial over (right)
    d.line([(590, 480), (330, 480)], fill=GRAY, width=4); arrow(d, (330, 480), (330, 560)); label(420, 445, "yes")
    d.line([(1010, 480), (1270, 480)], fill=GRAY, width=4); arrow(d, (1270, 480), (1270, 560)); label(1120, 445, "no")
    box(d, (90, 560, 570, 680), "Activation window", ["banner: \"n of 7 days remaining\""])
    box(d, (1030, 560, 1510, 680), "Activation window", ["banner: \"trial ended\" (cannot skip)"], fill=(253, 236, 234), outline=(200, 40, 40), title_color=(160, 30, 30))
    # outcomes
    outs_left = [("Activate", ["valid key →", "licensed mode"]), ("Continue Trial", ["main window", "with trial badge"]), ("Exit", ["application", "closes"])]
    for i, (btn, sub) in enumerate(outs_left):
        x = 20 + i * 196
        arrow(d, (330, 680), (x + 90, 790))
        box(d, (x, 790, x + 180, 910), btn, sub, size=20, line_size=17)
    outs_right = [("Activate", ["valid key →", "licensed mode"]), ("Exit", ["application", "closes"])]
    for i, (btn, sub) in enumerate(outs_right):
        x = 1080 + i * 220
        arrow(d, (1270, 680), (x + 95, 790))
        box(d, (x, 790, x + 190, 910), btn, sub, size=20, line_size=17)
    notes = [
        "Trial: 7 days from the first start; state stored twice (registry + hidden file), HMAC-bound to the computer.",
        "Edited records or a clock turned back before the last use end the trial immediately.",
        "Keys are ECDSA P-256 signatures produced only by FalconOcr.KeyGen; the application holds the public key only.",
    ]
    for i, n in enumerate(notes):
        d.text((40, 960 + i * 40), n, font=font(22), fill=TEXT)
    im.save(os.path.join(OUT, "diagram-licensing.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    architecture()
    pipeline()
    licensing()
    print("diagrams written to", OUT)
