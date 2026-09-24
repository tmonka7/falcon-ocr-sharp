"""Synthetic test pages for Falcon OCR (layout, tables, lists, colors, CJK).  Usage: python tools/make_samples.py samples"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
F = r"C:\Windows\Fonts"
def font(name, size): return ImageFont.truetype(os.path.join(F, name), size)

DPI = 200
W, H = int(8.27 * DPI), int(11.69 * DPI)
pt = lambda p: int(round(p * DPI / 72))

def wrap(d, text, f, width):
    words, lines, cur = text.split(), [], ""
    for w in words:
        t = (cur + " " + w).strip()
        if d.textlength(t, font=f) <= width: cur = t
        else: lines.append(cur); cur = w
    if cur: lines.append(cur)
    return lines

def para(d, x, y, text, f, width, fill=(0, 0, 0), leading=1.35):
    for ln in wrap(d, text, f, width):
        d.text((x, y), ln, font=f, fill=fill)
        y += int(f.size * leading)
    return y

LOREM = ("Optical character recognition converts images of typed or printed text into machine-encoded text. "
         "Modern engines combine a text detector with a sequence recognizer, and a layout stage restores columns, "
         "paragraphs and tables so that the result can be edited in a word processor or spreadsheet.")

# ---------------------------------------------------------------- page 1: report layout
img = Image.new("RGB", (W, H), "white")
d = ImageDraw.Draw(img)
m = pt(54)
d.text((m, pt(50)), "Quarterly Operations Report", font=font("arialbd.ttf", pt(24)), fill=(18, 110, 72))
d.text((m, pt(86)), "Prepared by the Document Automation Team", font=font("ariali.ttf", pt(12)), fill=(90, 90, 90))
y = para(d, m, pt(118), LOREM, font("arial.ttf", pt(11)), W - 2 * m)
y += pt(14)
d.text((m, y), "1. Key Features", font=font("arialbd.ttf", pt(16)), fill=(0, 0, 0)); y += pt(28)
for item in ["Accurate text recognition for scanned pages", "Supports multiple languages and scripts",
             "Converts PDF, images and scanned documents", "Outputs to Word, Excel, HTML and more"]:
    d.ellipse((m + pt(6), y + pt(5), m + pt(11), y + pt(10)), fill=(0, 0, 0))
    d.text((m + pt(22), y), item, font=font("arial.ttf", pt(11)), fill=(0, 0, 0)); y += pt(18)
y += pt(14)
d.text((m, y), "2. Results by Region", font=font("arialbd.ttf", pt(16)), fill=(0, 0, 0)); y += pt(30)

# ruled table with header fill and a merged header cell
cols = [m, m + pt(150), m + pt(260), m + pt(370), m + pt(480)]
rows = [y, y + pt(24), y + pt(46), y + pt(68), y + pt(90), y + pt(112)]
d.rectangle((cols[0], rows[0], cols[-1], rows[1]), fill=(220, 237, 228))
data = [["Region", "Q1", "Q2", "Q3", ""],
        ["North", "1,250", "1,410", "1,388"],
        ["South", "980", "1,020", "1,105"],
        ["East", "1,732", "1,690", "1,845"],
        ["West", "1,115", "1,240", "1,302"]]
for r in rows: d.line((cols[0], r, cols[-1], r), fill=(40, 40, 40), width=2)
for c in cols: d.line((c, rows[0], c, rows[-1]), fill=(40, 40, 40), width=2)
fb, fr = font("arialbd.ttf", pt(11)), font("arial.ttf", pt(11))
for ri, row in enumerate(data):
    for ci, val in enumerate(row[:4]):
        f = fb if ri == 0 else fr
        d.text((cols[ci] + pt(6), rows[ri] + pt(5)), val, font=f, fill=(0, 0, 0))
d.text((cols[4] + pt(6) - (cols[4] - cols[3]), rows[0] + pt(5)), "", font=fb)
d.text((cols[3] + pt(6), rows[0] + pt(5)), "Q3", font=fb, fill=(0, 0, 0))
y = rows[-1] + pt(24)

# two columns
d.text((m, y), "3. Discussion", font=font("arialbd.ttf", pt(16)), fill=(0, 0, 0)); y += pt(30)
colw = (W - 2 * m - pt(24)) // 2
f = font("times.ttf", pt(11))
text = (LOREM + " " + LOREM).split(". ")
yl = para(d, m, y, ". ".join(text[:3]) + ".", f, colw)
yl = para(d, m, yl + pt(8), ". ".join(text[3:]) , f, colw)
yr = para(d, m + colw + pt(24), y, "The second column continues the discussion. " + LOREM, f, colw)
d.rectangle((m + colw + pt(24), yr + pt(10), m + colw + pt(24) + pt(180), yr + pt(100)), fill=(52, 120, 200))
d.polygon([(m + colw + pt(60), yr + pt(90)), (m + colw + pt(110), yr + pt(30)), (m + colw + pt(160), yr + pt(90))], fill=(250, 200, 40))
y = max(yl, yr + pt(100)) + pt(20)
d.text((m, y), "Contact: ocr-team@example.com", font=font("arial.ttf", pt(10)), fill=(200, 30, 30))
img.save(os.path.join(OUT, "report.png"), dpi=(DPI, DPI))
img.save(os.path.join(OUT, "report_scan.pdf"), resolution=DPI)

# ---------------------------------------------------------------- CJK / Korean / Russian samples
def simple(name, lines, fname, size=pt(14)):
    im = Image.new("RGB", (W, int(H / 3)), "white")
    dd = ImageDraw.Draw(im)
    yy = pt(40)
    for i, ln in enumerate(lines):
        ff = font(fname, int(size * (1.5 if i == 0 else 1)))
        dd.text((pt(54), yy), ln, font=ff, fill=(0, 0, 0))
        yy += int(ff.size * 1.6)
    im.save(os.path.join(OUT, name), dpi=(DPI, DPI))

simple("chinese.png", ["智能文字识别", "光学字符识别技术可以将扫描的文档转换为可编辑的文本。", "支持中文、英文和日文的混合识别。"], "msyh.ttc")
simple("japanese.png", ["光学文字認識", "スキャンした文書を編集可能なテキストに変換します。", "日本語と英語の混在文書にも対応しています。"], "YuGothM.ttc")
simple("korean.png", ["광학 문자 인식", "스캔한 문서를 편집 가능한 텍스트로 변환합니다.", "한국어와 영어가 섞인 문서도 지원합니다."], "malgun.ttf")
simple("russian.png", ["Оптическое распознавание", "Система преобразует отсканированные документы в текст.", "Поддерживаются русский и английский языки."], "arial.ttf")

# image sequence (3 short pages)
for i in range(1, 4):
    im = Image.new("RGB", (W // 2, H // 4), "white")
    dd = ImageDraw.Draw(im)
    dd.text((pt(30), pt(30)), f"Chapter {i}", font=font("arialbd.ttf", pt(18)), fill=(0, 0, 0))
    para(dd, pt(30), pt(70), LOREM, font("arial.ttf", pt(10)), W // 2 - pt(60))
    os.makedirs(os.path.join(OUT, "sequence"), exist_ok=True)
    im.save(os.path.join(OUT, "sequence", f"page{i}.png"), dpi=(DPI, DPI))

# ---------------------------------------------------------------- text-layer PDF (hand-written, Helvetica)
def pdf_text(path):
    objs = []
    content = []
    def txt(x, y, size, s, bold=False, rgb=(0, 0, 0)):
        s = s.replace("\\", "\\\\").replace("(", "\\(").replace(")", "\\)")
        content.append(f"BT /{'F2' if bold else 'F1'} {size} Tf {rgb[0]/255:.3f} {rgb[1]/255:.3f} {rgb[2]/255:.3f} rg {x} {y} Td ({s}) Tj ET")
    txt(54, 780, 22, "Digital PDF Text Layer", True, (18, 110, 72))
    y = 750
    for ln in ["This page has real text objects, so Falcon OCR reads the embedded text",
               "directly instead of running recognition. Fonts, sizes and colors come",
               "from the PDF itself."]:
        txt(54, y, 11, ln); y -= 15
    txt(54, y - 12, 14, "Summary", True); y -= 40
    for ln in ["Left column text line one", "Left column text line two", "Left column text line three"]:
        txt(54, y, 10, ln); y -= 13
    y2 = y + 39
    for ln in ["Right column line one", "Right column line two", "Right column line three"]:
        txt(320, y2, 10, ln); y2 -= 13
    stream = "\n".join(content).encode("latin-1")
    objs.append(b"<< /Type /Catalog /Pages 2 0 R >>")
    objs.append(b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>")
    objs.append(b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>")
    objs.append(b"<< /Length %d >>\nstream\n" % len(stream) + stream + b"\nendstream")
    objs.append(b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")
    objs.append(b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>")
    out = b"%PDF-1.4\n"
    offs = []
    for i, o in enumerate(objs, 1):
        offs.append(len(out))
        out += b"%d 0 obj\n" % i + o + b"\nendobj\n"
    xref = len(out)
    out += b"xref\n0 %d\n0000000000 65535 f \n" % (len(objs) + 1)
    for o in offs: out += b"%010d 00000 n \n" % o
    out += b"trailer\n<< /Size %d /Root 1 0 R >>\nstartxref\n%d\n%%%%EOF\n" % (len(objs) + 1, xref)
    open(path, "wb").write(out)

pdf_text(os.path.join(OUT, "text_layer.pdf"))
print("ok")
