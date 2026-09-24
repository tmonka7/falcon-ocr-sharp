"""
Generates the Falcon OCR application icon (src/FalconOcr.App/app.ico, 16–256 px) and a PNG preview.
Design: green rounded tile, white page with folded corner and text lines, scan brackets and a
highlighted "recognized" line. Small sizes use a simplified drawing so they stay crisp.
  python tools/make_icon.py
"""
import os
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OUT = os.path.join(ROOT, "src", "FalconOcr.App", "app.ico")
PREVIEW = os.path.join(ROOT, "docs", "images", "app-icon.png")

TOP, BOTTOM = (26, 160, 110), (10, 96, 66)
ACCENT = (255, 193, 7)


def tile(size, detail):
    s = size * 4  # supersampling
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    # Vertical gradient inside a rounded square.
    grad = Image.new("RGBA", (s, s))
    gd = ImageDraw.Draw(grad)
    for y in range(s):
        t = y / (s - 1)
        gd.line([(0, y), (s, y)], fill=tuple(int(TOP[i] + (BOTTOM[i] - TOP[i]) * t) for i in range(3)) + (255,))
    mask = Image.new("L", (s, s), 0)
    r = int(s * 0.22)
    pad = int(s * 0.03)
    ImageDraw.Draw(mask).rounded_rectangle((pad, pad, s - pad, s - pad), radius=r, fill=255)
    im.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(im)

    # White page with folded corner.
    x0, y0, x1, y1 = s * 0.27, s * 0.17, s * 0.73, s * 0.83
    fold = s * 0.13
    page = [(x0, y0), (x1 - fold, y0), (x1, y0 + fold), (x1, y1), (x0, y1)]
    d.polygon(page, fill=(255, 255, 255, 255))  # page with the top-right corner cut off
    d.polygon([(x1 - fold, y0), (x1 - fold, y0 + fold), (x1, y0 + fold)], fill=(190, 230, 212, 255))  # fold

    line_col = (120, 150, 138, 255)
    if detail >= 2:
        lines = [(0.34, 0.33, 0.56), (0.34, 0.43, 0.66), (0.34, 0.53, 0.66), (0.34, 0.63, 0.60), (0.34, 0.73, 0.52)]
        w = s * 0.045
    else:
        lines = [(0.34, 0.38, 0.66), (0.34, 0.52, 0.66), (0.34, 0.66, 0.58)]
        w = s * 0.07
    for i, (a, y, b) in enumerate(lines):
        col = ACCENT + (255,) if (detail >= 2 and i == 2) or (detail < 2 and i == 1) else line_col
        d.rounded_rectangle((s * a, s * y - w / 2, s * b, s * y + w / 2), radius=w / 2, fill=col)

    if detail >= 2:
        # Scan brackets around the page corners.
        c = (255, 255, 255, 235)
        bw, L = s * 0.035, s * 0.12
        m = s * 0.10
        for (cx, cy, dx, dy) in [(m, m, 1, 1), (s - m, m, -1, 1), (m, s - m, 1, -1), (s - m, s - m, -1, -1)]:
            d.line([(cx, cy), (cx + dx * L, cy)], fill=c, width=int(bw))
            d.line([(cx, cy), (cx, cy + dy * L)], fill=c, width=int(bw))
    return im.resize((size, size), Image.LANCZOS)


def main():
    sizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256]
    images = [tile(n, 1 if n <= 24 else 2) for n in sizes]
    images[-1].save(OUT, format="ICO", sizes=[(n, n) for n in sizes], append_images=images[:-1])
    # Preview strip for the documentation.
    strip = Image.new("RGBA", (sum(sizes[i] for i in (0, 3, 5, 8, 9)) + 60, 256), (255, 255, 255, 255))
    x = 10
    for i in (9, 8, 5, 3, 0):
        strip.paste(images[i], (x, (256 - sizes[i]) // 2), images[i])
        x += sizes[i] + 10
    strip.save(PREVIEW)
    print("icon written:", OUT)


if __name__ == "__main__":
    main()
