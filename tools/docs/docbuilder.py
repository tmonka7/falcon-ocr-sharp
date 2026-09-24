"""Tiny document builder: produces Word-friendly HTML that html2docx.ps1 turns into .docx (real heading styles, TOC)."""
import html
import os
import re

IMAGE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "docs", "images"))

PRODUCT = "Falcon OCR"
VERSION = "1.0"          # product version
DOC_VERSION = "1.1"      # document revision
DATE = "2026-09-24"
ORG = "Falcon OCR Development Team"

CSS = """
body { font-family: Calibri, 'Segoe UI', sans-serif; font-size: 11pt; color: #1f2937; }
h1 { font-family: 'Segoe UI Semibold', Calibri; font-size: 18pt; color: #0e6e4c; margin-top: 18pt; margin-bottom: 6pt; page-break-before: always; }
h2 { font-family: 'Segoe UI Semibold', Calibri; font-size: 14pt; color: #13865c; margin-top: 14pt; margin-bottom: 4pt; }
h3 { font-family: 'Segoe UI Semibold', Calibri; font-size: 12pt; color: #1f2937; margin-top: 10pt; margin-bottom: 3pt; }
p { margin-top: 3pt; margin-bottom: 6pt; line-height: 115%; }
li { margin-bottom: 3pt; }
table { border-collapse: collapse; width: 100%; margin-top: 4pt; margin-bottom: 8pt; }
th { background: #e1f3eb; color: #0e6e4c; font-weight: bold; border: 1px solid #9fcbb7; padding: 4pt 5pt; text-align: left; vertical-align: top; font-size: 10pt; }
td { border: 1px solid #c9d3cf; padding: 3pt 5pt; vertical-align: top; font-size: 10pt; }
.caption { font-size: 9pt; color: #64707e; font-style: italic; margin-top: 2pt; margin-bottom: 10pt; text-align: center; }
.note { background: #fff8e1; border-left: 4pt solid #ffc107; padding: 5pt 8pt; margin: 6pt 0; }
.tip { background: #e1f3eb; border-left: 4pt solid #13865c; padding: 5pt 8pt; margin: 6pt 0; }
.code { font-family: Consolas, monospace; font-size: 9.5pt; background: #f4f6f8; padding: 5pt 8pt; margin: 4pt 0; }
.cover-title { font-family: 'Segoe UI Semibold'; font-size: 30pt; color: #0e6e4c; margin-top: 120pt; margin-bottom: 6pt; }
.cover-sub { font-size: 16pt; color: #13865c; margin-bottom: 40pt; }
.cover-meta { font-size: 11pt; color: #1f2937; }
.pass { color: #1b7a3e; font-weight: bold; }
.notrun { color: #9a6700; font-weight: bold; }
.fail { color: #b42318; font-weight: bold; }
"""


def inline(text):
    """Escapes text and applies **bold**, *italic* and `code` markup."""
    t = html.escape(text)
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t)
    t = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"<i>\1</i>", t)
    t = re.sub(r"`(.+?)`", r"<span style='font-family:Consolas;font-size:10pt;color:#0e6e4c'>\1</span>", t)
    return t


class Doc:
    def __init__(self, title, subtitle, doc_id, purpose, chapter_breaks=True):
        self.title, self.subtitle, self.doc_id, self.purpose = title, subtitle, doc_id, purpose
        self.chapter_breaks = chapter_breaks  # False: chapters flow on (use pagebreak() explicitly)
        self.parts = []
        self.fig = 0
        self.tab = 0

    # -------------------------------------------------------------- blocks
    def h1(self, t): self.parts.append(f"<h1>{inline(t)}</h1>")
    def h2(self, t): self.parts.append(f"<h2>{inline(t)}</h2>")
    def h3(self, t): self.parts.append(f"<h3>{inline(t)}</h3>")

    def p(self, *texts):
        for t in texts:
            self.parts.append(f"<p>{inline(t)}</p>")

    def ul(self, items):
        self.parts.append("<ul>" + "".join(f"<li>{inline(i)}</li>" for i in items) + "</ul>")

    def ol(self, items):
        self.parts.append("<ol>" + "".join(f"<li>{inline(i)}</li>" for i in items) + "</ol>")

    def note(self, t): self.parts.append(f"<p class='note'><b>Note:</b> {inline(t)}</p>")
    def tip(self, t): self.parts.append(f"<p class='tip'><b>Tip:</b> {inline(t)}</p>")

    def code(self, lines):
        body = "<br>".join(html.escape(l).replace(" ", "&nbsp;") for l in lines)
        self.parts.append(f"<p class='code'>{body}</p>")

    def table(self, headers, rows, widths=None, caption=None, raw=False):
        """widths: list of percentages. raw=True keeps cell HTML (used for status colors)."""
        cols = ""
        if widths:
            cols = "".join(f"<col width='{w}%'>" for w in widths)
        h = "".join(f"<th width='{widths[i]}%'>" + inline(x) + "</th>" if widths else "<th>" + inline(x) + "</th>" for i, x in enumerate(headers))
        body = ""
        for r in rows:
            body += "<tr>" + "".join(f"<td>{c if raw and isinstance(c, str) and c.startswith('<') else inline(str(c))}</td>" for c in r) + "</tr>"
        # Table captions go above the table so they never end up alone on the next page.
        if caption:
            self.tab += 1
            self.parts.append(f"<p class='caption' style='text-align:left;margin-bottom:2pt;page-break-after:avoid'>Table {self.tab} – {inline(caption)}</p>")
        self.parts.append(f"<table>{cols}<thead><tr>{h}</tr></thead><tbody>{body}</tbody></table>")

    def img(self, file, caption, width=620):
        self.fig += 1
        # Placeholder replaced by an embedded picture in html2docx.ps1 (Word's HTML image import is unreliable).
        path = os.path.join(IMAGE_DIR, file)
        if not os.path.exists(path):
            raise FileNotFoundError(path)
        self.parts.append(f"<p style='text-align:center;margin-bottom:0'>[[IMG:{path}|{width}]]</p>")
        self.parts.append(f"<p class='caption'>Figure {self.fig} – {inline(caption)}</p>")

    def img_grid(self, items, width=300, cols=2):
        """Pictures side by side: items = [(file, caption), ...]."""
        cells = []
        for file, caption in items:
            self.fig += 1
            path = os.path.join(IMAGE_DIR, file)
            if not os.path.exists(path):
                raise FileNotFoundError(path)
            cells.append(f"<td style='border:none;text-align:center;vertical-align:top'><p style='text-align:center;margin:0'>[[IMG:{path}|{width}]]</p>"
                         f"<p class='caption'>Figure {self.fig} – {inline(caption)}</p></td>")
        rows = "".join("<tr>" + "".join(cells[i:i + cols]) + "</tr>" for i in range(0, len(cells), cols))
        self.parts.append(f"<table style='border:none'>{rows}</table>")

    def pagebreak(self): self.parts.append("<br clear=all style='page-break-before:always'>")

    # -------------------------------------------------------------- output
    def render(self, history):
        meta = [("Product", f"{PRODUCT} {VERSION}"), ("Document ID", self.doc_id), ("Version", DOC_VERSION), ("Date", DATE),
                ("Status", "Released for review"), ("Prepared by", ORG)]
        cover = (f"<p class='cover-title'>{inline(self.title)}</p><p class='cover-sub'>{inline(self.subtitle)}</p>"
                 + "<table style='width:70%'>" + "".join(f"<tr><td style='width:30%;background:#f4f6f8'><b>{k}</b></td><td>{inline(v)}</td></tr>" for k, v in meta) + "</table>"
                 + f"<p class='cover-meta' style='margin-top:30pt'>{inline(self.purpose)}</p>")
        control = ("<br clear=all style='page-break-before:always'><p style='font-family:Segoe UI Semibold;font-size:14pt;color:#0e6e4c'>Document control</p>"
                   + "<table><tr><th width='12%'>Version</th><th width='16%'>Date</th><th width='22%'>Author</th><th width='50%'>Changes</th></tr>"
                   + "".join(f"<tr><td>{inline(a)}</td><td>{inline(b)}</td><td>{inline(c)}</td><td>{inline(d)}</td></tr>" for a, b, c, d in history)
                   + "</table><p style='font-family:Segoe UI Semibold;font-size:14pt;color:#0e6e4c;margin-top:18pt'>Contents</p><p>[[TOC]]</p>")
        body = "\n".join(self.parts)
        css = CSS if self.chapter_breaks else CSS.replace("page-break-before: always;", "")
        return (f"<html><head><meta charset='utf-8'><title>{html.escape(self.title)}</title><style>{css}</style></head>"
                f"<body>{cover}{control}{body}</body></html>")


DEFAULT_HISTORY = [
    ("0.9", "2026-09-20", ORG, "Draft for internal review."),
    ("1.0", DATE, ORG, "Released with Falcon OCR 1.0: OCR engine, layout reconstruction, exports, trial and licensing."),
    ("1.1", DATE, ORG, "UI extensions: font and size combo boxes, English/Chinese/Japanese interface (gettext PO), collapsible sidebar, resizable settings panel, trial notification below the toolbar."),
]
