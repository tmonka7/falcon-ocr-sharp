"""
Builds the Falcon OCR documentation set (docs/*.docx).

  python tools/docs/diagrams.py docs/images      # diagrams (once, or after changes)
  python tools/docs/build_docs.py [srs sdd scr tcs um]

Each document is generated as HTML and converted to a native Word document by Microsoft Word
(tools/docs/html2docx.ps1: embedded pictures, heading styles, table of contents, page numbers).
"""
import os
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(__file__))
from docbuilder import DEFAULT_HISTORY  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TMP = os.path.join(ROOT, ".tmp", "docs")
OUT = os.path.join(ROOT, "docs")

DOCS = {
    "srs": ("doc_srs", "Falcon OCR - Software Requirements Specification.docx", "Software Requirements Specification"),
    "sdd": ("doc_sdd", "Falcon OCR - System Design Document.docx", "System Design Document"),
    "scr": ("doc_screens", "Falcon OCR - Screen Design Document.docx", "Screen Design Document"),
    "tcs": ("doc_tests", "Falcon OCR - Test Case Specification.docx", "Test Case Specification"),
    "um": ("doc_manual", "Falcon OCR - User Manual.docx", "User Manual"),
}


def main():
    os.makedirs(TMP, exist_ok=True)
    wanted = sys.argv[1:] or list(DOCS)
    for key in wanted:
        module, docx, title = DOCS[key]
        doc = __import__(module).build()
        # A fresh file name per run: Word silently blocks on a "serious error last time" prompt for files
        # that were open when a previous Word instance was killed.
        html_path = os.path.join(TMP, f"{key}-{int(time.time())}.html")
        with open(html_path, "w", encoding="utf-8") as f:
            f.write(doc.render(DEFAULT_HISTORY))
        ps = os.path.join(os.path.dirname(__file__), "html2docx.ps1")
        try:
            subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ps,
                            "-Html", html_path, "-Docx", os.path.join(OUT, docx), "-Title", title], check=True, timeout=300)
        except subprocess.TimeoutExpired:
            subprocess.run(["taskkill", "/IM", "WINWORD.EXE", "/F"], capture_output=True)
            raise SystemExit(f"Word did not finish converting {key} within 5 minutes.")


if __name__ == "__main__":
    main()
