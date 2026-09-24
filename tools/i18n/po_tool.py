"""
gettext catalog maintenance for Falcon OCR.

  python tools/i18n/po_tool.py extract          # scan sources -> lang/falcon-ocr.pot, update lang/*.po (keeps translations)
  python tools/i18n/po_tool.py check            # list untranslated / obsolete entries per catalog

Translatable strings are the first argument of L.T("...") / L.F("...", ...) (adjacent literals joined with +),
plus the language names of LanguageCatalog (translated at run time with L.T(DisplayName)).
"""
import glob
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LANG = os.path.join(ROOT, "lang")
SOURCES = glob.glob(os.path.join(ROOT, "src", "FalconOcr.App", "**", "*.cs"), recursive=True) + \
          glob.glob(os.path.join(ROOT, "src", "FalconOcr.Core", "Licensing", "*.cs"))
EXTRA = ["English", "Chinese (中文)", "Japanese (日本語)", "Korean (한국어)", "Russian (Русский)"]
CATALOGS = {"zh_CN": "Chinese (Simplified)", "ja": "Japanese"}


def cs_unescape(s):
    out, i = [], 0
    while i < len(s):
        c = s[i]
        if c == "\\" and i + 1 < len(s):
            n = s[i + 1]
            out.append({"n": "\n", "t": "\t", "r": "\r", '"': '"', "\\": "\\", "0": "\0"}.get(n, "\\" + n))
            i += 2
        else:
            out.append(c)
            i += 1
    return "".join(out)


CALL = re.compile(r'\bL\.[TF]\(\s*')
LIT = re.compile(r'"((?:[^"\\]|\\.)*)"\s*')


def extract_file(path):
    src = open(path, encoding="utf-8-sig").read()
    found = []
    for m in CALL.finditer(src):
        i = m.end()
        parts = []
        while True:
            lm = LIT.match(src, i)
            if not lm:
                break
            parts.append(cs_unescape(lm.group(1)))
            i = lm.end()
            if src.startswith("+", i):
                j = i + 1
                while src[j].isspace():
                    j += 1
                if src[j] == '"':
                    i = j
                    continue
            break
        if parts:
            line = src.count("\n", 0, m.start()) + 1
            found.append(("".join(parts), os.path.relpath(path, ROOT).replace("\\", "/") + ":" + str(line)))
    return found


def po_escape(s):
    return s.replace("\\", "\\\\").replace('"', '\\"').replace("\t", "\\t").replace("\n", "\\n")


def po_quote(s):
    if "\n" not in s or s.endswith("\n") and s.count("\n") == 1:
        return '"' + po_escape(s) + '"'
    lines = s.split("\n")
    chunks = [po_escape(l) + ("\\n" if k < len(lines) - 1 else "") for k, l in enumerate(lines)]
    return '""\n' + "\n".join('"' + c + '"' for c in chunks if c)


def read_po(path):
    """Returns {msgid: msgstr} (same parser rules as FalconOcr.Localization.PoFile)."""
    entries, cur, field = {}, {"msgid": None, "msgstr": None}, None

    def unq(t):
        t = t.strip()[1:-1]
        return cs_unescape(t)

    def flush():
        if cur["msgid"] and cur["msgstr"] is not None:
            entries[cur["msgid"]] = cur["msgstr"]
        cur["msgid"], cur["msgstr"] = None, None

    if not os.path.exists(path):
        return entries
    for raw in open(path, encoding="utf-8"):
        line = raw.strip()
        if not line:
            flush(); field = None; continue
        if line.startswith("#"):
            continue
        if line.startswith("msgid "):
            if field == "msgstr":
                flush()
            field = "msgid"; cur["msgid"] = unq(line[6:])
        elif line.startswith("msgstr"):
            field = "msgstr"; cur["msgstr"] = unq(line[line.index('"'):])
        elif line.startswith('"') and field:
            cur[field] = (cur[field] or "") + unq(line)
    flush()
    return entries


def header(lang_code, lang_name):
    return ('msgid ""\nmsgstr ""\n'
            '"Project-Id-Version: Falcon OCR 1.0\\n"\n'
            f'"Language: {lang_code}\\n"\n'
            '"MIME-Version: 1.0\\n"\n'
            '"Content-Type: text/plain; charset=UTF-8\\n"\n'
            '"Content-Transfer-Encoding: 8bit\\n"\n'
            f'"X-Language-Name: {lang_name}\\n"\n\n')


def collect():
    msgs = {}
    for path in sorted(SOURCES):
        for text, ref in extract_file(path):
            msgs.setdefault(text, []).append(ref)
    for e in EXTRA:
        msgs.setdefault(e, []).append("src/FalconOcr.Core/Engine/OcrOptions.cs (LanguageCatalog.DisplayName)")
    return msgs


def write_catalog(path, msgs, translations, lang_code, lang_name):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("# Falcon OCR user interface — " + (lang_name or "template") + "\n")
        f.write("# Edit msgstr values (Poedit or any UTF-8 text editor). {0}, {1} … are placeholders and must be kept.\n")
        f.write(header(lang_code, lang_name or ""))
        for msgid in sorted(msgs, key=lambda m: msgs[m][0]):
            for ref in msgs[msgid][:3]:
                f.write("#: " + ref + "\n")
            if "{0" in msgid:
                f.write("#, csharp-format\n")
            f.write("msgid " + po_quote(msgid) + "\n")
            f.write("msgstr " + po_quote(translations.get(msgid, "")) + "\n\n")


def main():
    cmd = sys.argv[1] if len(sys.argv) > 1 else "check"
    msgs = collect()
    os.makedirs(LANG, exist_ok=True)
    if cmd == "extract":
        write_catalog(os.path.join(LANG, "falcon-ocr.pot"), msgs, {}, "", None)
        for code, name in CATALOGS.items():
            path = os.path.join(LANG, code + ".po")
            write_catalog(path, msgs, read_po(path), code, name)
        print(f"{len(msgs)} messages")
    rc = 0
    for code in CATALOGS:
        tr = read_po(os.path.join(LANG, code + ".po"))
        missing = [m for m in msgs if not tr.get(m)]
        bad = [m for m in msgs if tr.get(m) and re.findall(r"\{\d+[^}]*\}", m) and sorted(re.findall(r"\{\d+[^}]*\}", m)) != sorted(re.findall(r"\{\d+[^}]*\}", tr[m]))]
        print(f"{code}: {len(msgs) - len(missing)}/{len(msgs)} translated, {len(bad)} placeholder mismatches")
        for m in missing[:40]:
            print("   missing:", repr(m)[:110])
        for m in bad:
            print("   placeholders:", repr(m)[:110])
        rc |= 1 if (missing or bad) else 0
    if cmd == "dump":
        import json
        json.dump(sorted(msgs), open(os.path.join(ROOT, ".tmp", "msgids.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=0)
    sys.exit(rc if cmd == "check" else 0)


if __name__ == "__main__":
    main()
