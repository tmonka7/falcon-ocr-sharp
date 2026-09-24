"""
One-off helper used to internationalise the WinForms code: wraps user-visible string literals in L.T("...")
and turns interpolated strings $"..{x}.." into L.F("..{0}..", x). Idempotent (already wrapped strings are kept).
  python tools/i18n/wrap_strings.py <file.cs> ...
"""
import re
import sys

SKIP_LINE = re.compile(r'DllImport|\bcase "|\bconst\b|^\s*\[|GetEnvironmentVariable|Regex|FromProgID|GetTypeFromProgID|FontFamily|Process\.Start|'
                       r'Registry|StyleId|\.Navigate\(|_pages\[|_nav\[|Json\.|Path\.Combine|ToString\("|"home"')
NOT_UI = re.compile(r'^(Segoe UI|Consolas|Calibri|Arial|explorer\.exe|FALCON_|Falcon OCR$)|^[\w.\-]+\.(exe|png|json|key|lic|dll|po|txt|pdf|docx)$')


def parse_interpolated(src, i):
    """src[i] == '$' and src[i+1] == '"'. Returns (end_index_exclusive, parts) where parts is list of (text|('hole', expr, fmt))."""
    j = i + 2
    parts, buf = [], []
    while j < len(src):
        c = src[j]
        if c == '\\':
            buf.append(src[j:j + 2]); j += 2; continue
        if c == '"':
            parts.append(''.join(buf))
            return j + 1, parts
        if c == '{':
            if src[j + 1] == '{':
                buf.append('{{'); j += 2; continue
            parts.append(''.join(buf)); buf = []
            depth, k, in_str = 1, j + 1, False
            while depth:
                ch = src[k]
                if in_str:
                    if ch == '\\': k += 1
                    elif ch == '"': in_str = False
                elif ch == '"': in_str = True
                elif ch in '({[': depth += 1 if ch == '{' else 0
                elif ch == '}': depth -= 1
                if ch == '(' and not in_str: depth += 0
                k += 1
            hole = src[j + 1:k - 1]
            # split format specifier at top-level ':' (not inside parentheses / ternary)
            expr, fmt, par = hole, None, 0
            for n, ch in enumerate(hole):
                if ch in '([': par += 1
                elif ch in ')]': par -= 1
                elif ch == ':' and par == 0 and '?' not in hole[:n]:
                    expr, fmt = hole[:n], hole[n + 1:]
                    break
            parts.append(('hole', expr.strip(), fmt))
            j = k
            continue
        if c == '}' and src[j + 1:j + 2] == '}':
            buf.append('}}'); j += 2; continue
        buf.append(c); j += 1
    raise ValueError("unterminated interpolated string")


def convert(src):
    out, i = [], 0
    lines_skip = set()
    # line numbers to skip
    pos = 0
    for ln in src.split('\n'):
        if SKIP_LINE.search(ln):
            lines_skip.add(pos)
        pos += len(ln) + 1
    line_starts = sorted(lines_skip)

    def skipped(index):
        start = src.rfind('\n', 0, index) + 1
        return start in lines_skip

    while i < len(src):
        c = src[i]
        # comments
        if src.startswith('//', i):
            e = src.find('\n', i); e = len(src) if e < 0 else e
            out.append(src[i:e]); i = e; continue
        if c == '$' and src[i + 1:i + 2] == '"':
            end, parts = parse_interpolated(src, i)
            literal = ''.join(p for p in parts if isinstance(p, str))
            if skipped(i) or len(re.findall(r'[A-Za-z]', literal)) < 3 or src[max(0, i - 4):i] == 'L.F(':
                out.append(src[i:end]); i = end; continue
            fmt, args, n = [], [], 0
            for p in parts:
                if isinstance(p, str):
                    fmt.append(p)
                else:
                    fmt.append('{%d%s}' % (n, ':' + p[2] if p[2] else ''))
                    args.append(p[1]); n += 1
            out.append('L.F("' + ''.join(fmt) + '", ' + ', '.join(args) + ')')
            i = end
            continue
        if c == '@' and src[i + 1:i + 2] == '"':
            e = i + 2
            while True:
                e = src.find('"', e)
                if src[e + 1:e + 2] == '"': e += 2; continue
                break
            out.append(src[i:e + 1]); i = e + 1; continue
        if c == "'":
            m = re.match(r"'(\\.|[^\\'])'", src[i:])
            if m:
                out.append(m.group(0)); i += len(m.group(0)); continue
        if c == '"':
            e = i + 1
            while src[e] != '"':
                e += 2 if src[e] == '\\' else 1
            lit = src[i + 1:e]
            wrapped = src[max(0, i - 4):i] in ('L.T(', 'L.F(')
            ui = (not wrapped and not skipped(i) and len(lit) >= 2 and re.match(r'^([A-Z✓⚙▶■⏳⚠✗…]|\d\. [A-Z])', lit)
                  and len(re.findall(r'[A-Za-z]', lit)) >= 2 and not NOT_UI.search(lit)
                  and not re.match(r'^[A-Z][A-Za-z0-9]*$', lit) or (not wrapped and not skipped(i) and lit in (
                      'Recognize', 'Export', 'Delete', 'Scan', 'Rotate', 'Crop', 'Settings', 'Help', 'Files', 'Thumbnails', 'Normal',
                      'Activate', 'Exit', 'Cancel', 'Copy', 'Verify', 'Save', 'Automatic', 'Language', 'Recognition', 'History', 'Batch', 'Home', 'OCR', 'Stop', 'Ready', 'Activated',
                      'Defaults', 'Remove', 'Input', 'Type', 'Status', 'Time', 'Source', 'Output', 'Format', 'Pages', 'Duration', 'Waiting', 'Done',
                      'Failed', 'Stopped', 'Unlicensed', 'Edited', 'Result', 'Key', 'Licensee', 'Serial', 'Machine', 'Issued', 'Expires')))
            out.append(('L.T("' + lit + '")') if ui else ('"' + lit + '"'))
            i = e + 1
            continue
        out.append(c); i += 1
    return ''.join(out)


if __name__ == "__main__":
    for path in sys.argv[1:]:
        src = open(path, encoding='utf-8-sig').read()
        new = convert(src)
        if new != src:
            if 'using FalconOcr.Localization;' not in new:
                new = new.replace('using System;', 'using System;\nusing FalconOcr.Localization;', 1)
            open(path, 'w', encoding='utf-8').write(new)
            print('converted', path)
