# Falcon OCR

Offline OCR for Windows (.NET Framework 4.7, WinForms) with an ABBYY FineReader–style workspace.
Converts PDFs, single images, multi-page TIFFs and image sequences into **Word (.docx)**, **Excel (.xlsx)**
and **HTML**, rebuilding the original page structure and styling: columns, headings, paragraphs, lists,
ruled tables (with merged cells and fills), pictures, font sizes, bold text and colors.

Recognition uses **PaddleOCR PP-OCRv5** models (ONNX) on ONNX Runtime; everything — models, native
libraries and NuGet packages — is inside the repository, so the solution builds and runs with no network.

| Language | Recognition model |
|---|---|
| English | `en_PP-OCRv5_rec_mobile` |
| Chinese (Simplified/Traditional) | `ch_PP-OCRv5_rec_mobile` |
| Japanese | `ch_PP-OCRv5_rec_mobile` (PP-OCRv5's main model covers Chinese, Japanese and English) |
| Korean | `korean_PP-OCRv5_rec_mobile` |
| Russian | `eslav_PP-OCRv5_rec_mobile` (East Slavic: Russian, Ukrainian, Belarusian) |

Text detection: `ch_PP-OCRv5_det_mobile` (all languages). Optional line orientation: `PP-LCNet_x0_25_textline_ori`.

## Build (offline)

Requirements: Windows 10/11 x64 and the .NET SDK (verified with 9.0; it builds the net47 target) or Visual Studio
2022 with SDK-style project support. The .NET Framework 4.7 reference assemblies come from the vendored feed,
so no targeting pack is needed; running requires only the .NET Framework 4.7+ runtime that ships with Windows 10/11.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1      # → dist\FalconOcr.exe (+ dist\falcon-ocr.exe CLI)
```

or open `FalconOcr.sln` in Visual Studio (platform **x64**) and press F5. `nuget.config` clears all online
sources and restores only from `packages\`.

To refresh the vendored dependencies (online, once), run `tools\fetch-dependencies.ps1`
(`-Only models|native|packages`).

## Using the application

- **Home** — the workspace: add files (PDF / images / image sequence / folder), paste from the clipboard or
  scan (WIA), rotate or crop pages, **Recognize** (F5) and **Export** (Ctrl+E).
  - *Source Page* and *Recognized Text* are shown side by side with identical geometry: zoom, scrolling and
    selection are synchronised, so every recognized line sits exactly over its source. Click a line to find it in
    the image; double-click (or F2) to correct it. Uncertain characters are highlighted.
  - The formatting bar changes the paragraph style (Normal / Heading 1–3 / list) and B/I/U — changes are exported.
  - *Original Image* shows the analysis: text lines, tables and pictures.
  - Right panel: document type (Editable / Exact Copy / Plain Text), language, layout analysis, table detection,
    advanced settings, output format and output folder.
- **OCR** — quick text extraction: paste/drop an image, get plain text.
- **Batch Process** — convert many files/folders unattended.
- **History** — past recognitions and exports.
- **Settings** — defaults, advanced recognition parameters, model status.

Digital PDFs are read from their text layer (exact text, real fonts/sizes/colors) and OCR is used only for
scanned pages (configurable).

### Document types

| | Word | Excel | HTML |
|---|---|---|---|
| **Editable Document** | Flowing text, real Word columns (sections), heading styles, bullet numbering, tables with merged cells/fills, inline pictures, paragraph spacing/indents/alignment | Tables as real grids (numbers as numbers), text placed on a column grid derived from the page | Semantic HTML (h1–h3, p, ul/ol, table, figure), CSS grid columns, embedded images |
| **Exact Copy** | Every line in a positioned frame at its original place, width-fitted; tables and pictures positioned on the page | as Editable | Absolutely positioned lines, tables and images |
| **Plain Text** | Paragraphs only | as Editable | Preformatted text |

## Interface languages

The user interface is available in **English, 简体中文 (Chinese) and 日本語 (Japanese)** —
*Settings → General → Interface language* (first start: the Windows display language).
Translations are gettext PO files in `lang/` (`zh_CN.po`, `ja.po`, template `falcon-ocr.pot`), copied next to
the executable and editable with Poedit or any UTF-8 editor without rebuilding.

- `python tools/i18n/po_tool.py extract` — collect all `L.T("…")` / `L.F("…")` strings, update the template and catalogs (translations are kept)
- `python tools/i18n/po_tool.py check` — report untranslated entries and `{0}` placeholder mismatches
- New language: copy `falcon-ocr.pot` to `lang/<code>.po`, translate, add the code to `L.Languages` (`src/FalconOcr.Core/Localization/L.cs`)

## Trial

Without a license Falcon OCR runs as a **7-day trial** from the first start. The activation window appears at
every start (Activate · Continue Trial · Exit) and the title bar shows `TRIAL · n days left — Activate now`.
After 7 days only Activate / Exit remain. The trial state is stored in the registry and in a hidden file, both
HMAC-protected and bound to the computer; edited data or a clock set back ends the trial. The CLI follows the same rules.

## Licensing (KeyGen)

Licensed use requires a license key produced by **`src/FalconOcr.KeyGen`**
(`FalconOcrKeyGen.exe`, a vendor-only tool — `build.ps1` never copies it to `dist`).

- Keys are signed with **ECDSA P-256**. The KeyGen holds the private key; the app contains only the public key
  (`src/FalconOcr.Core/Licensing/LicensePublicKey.cs`), so it can verify keys but nobody can create them from the app.
- A key carries: licensee, serial number, issue date, optional expiry date, optional machine binding.
- **Machine binding:** the customer copies the *machine code* shown in the activation window
  (or `falcon-ocr --machine-code`) and sends it to you; tick "Any computer" for an unbound (site) key.
- The activated key is stored in `%LOCALAPPDATA%\FalconOCR\license.key`. Administrators can also deploy a
  `license.key` next to `FalconOcr.exe` or in `%ProgramData%\FalconOCR\`.

Vendor workflow:

1. First time only: run `FalconOcrKeyGen.exe` and choose **New signing key** (or `FalconOcrKeyGen --init`).
   It stores the private key DPAPI-encrypted in `%APPDATA%\FalconOcrKeyGen\signing.key`, writes the public key to
   `LicensePublicKey.cs` — then **rebuild** — and asks you to save a backup. *Keep the backup offline and secret;
   creating a new signing key invalidates every license issued before.*
2. Per customer: enter the licensee, the machine code (or "Any computer") and the expiry → **Generate** →
   send the key text or the saved `.lic` file. Every issued key is appended to `%APPDATA%\FalconOcrKeyGen\issued.csv`.

Command line: `FalconOcrKeyGen --generate --name "ACME Ltd" --machine P9BB-82PR-DS2C-1R5Y --days 365`,
`--verify <key>`, `--backup <file>`, `--import <file>`, `--machine-code`.

Customers activate in the startup window (paste the key or *Load license file…*), later via
*Settings → License → Change license key…*, or with `falcon-ocr --license <key>`.

## Command line

```
falcon-ocr <files|folders...> [-l en|zh|ja|ko|ru] [-f docx,xlsx,html,txt] [-o outDir]
           [--exact | --plain] [--seq] [--no-tables] [--ocr-only] [--dpi 200] [--cls] [--dump]
```

`--seq` treats all inputs as one image sequence; `--dump` prints the reconstructed layout.
Example: `falcon-ocr samples\report.png -f docx,html --dump`

## Project layout

```
src/FalconOcr.Core      engine + layout + exporters (class library)
  Engine/               PaddleOCR pipeline on ONNX Runtime: DBNet detector (+ box post-processing),
                        SVTR/CTC recognizer with per-character positions, orientation classifier
  Imaging/              BGR image buffer, resize, rotated crop (no OpenCV dependency)
  Pdf/                  PDFium P/Invoke: rendering and text layer with font/size/weight/color
  Input/                documents: PDF, image, multi-page TIFF, image sequence, clipboard; rotate/crop
  Layout/               style estimation (size, weight, color, fill), ruled + borderless tables,
                        figures, column sections, reading order, paragraphs, headings, lists
  Export/               DOCX / XLSX (OpenXML SDK), HTML, text
src/FalconOcr.App       WinForms application (code-built UI, custom-drawn controls and vector icons)
src/FalconOcr.Cli       command-line front end
src/FalconOcr.KeyGen    license key generator (vendor-only, not shipped)
models/                 PP-OCRv5 ONNX models + dictionaries (copied next to the exe at build time)
lib/native/x64/         onnxruntime.dll, pdfium.dll, app-local VC++ 2015-2022 runtime
packages/               vendored NuGet feed for offline restore
samples/                test documents (generated by tools/make_samples.py)
```

Notes:
- x64 only (ONNX Runtime and PDFium are 64-bit native libraries).
- The UI is built in code (no `.Designer.cs`), so the WinForms designer is not used.
- Targeting .NET 4.7 with netstandard2.0 packages copies the standard facade DLLs next to the exe; this is expected.
- Italic is taken from PDF text layers; for scanned pages bold, size and color are estimated from pixels.
- Diagnostics for headless machines: set `FALCON_SNAPSHOT=<folder>` and start the app with a file argument —
  it recognizes the file, renders every page of the UI to PNG and exits.

## Documentation

| Document | File |
|---|---|
| User Manual | `docs/Falcon OCR - User Manual.docx` |
| Software Requirements Specification | `docs/Falcon OCR - Software Requirements Specification.docx` |
| System Design Document | `docs/Falcon OCR - System Design Document.docx` |
| Screen Design Document | `docs/Falcon OCR - Screen Design Document.docx` |
| Test Case Specification (with results) | `docs/Falcon OCR - Test Case Specification.docx` |

The documents are generated from `tools/docs/*.py` (content) and converted by Microsoft Word
(`python tools/docs/build_docs.py`; diagrams: `python tools/docs/diagrams.py docs/images`).

## Third-party components

| Component | License |
|---|---|
| PaddleOCR PP-OCRv5 models (ONNX export by RapidAI/RapidOCR) | Apache-2.0 |
| ONNX Runtime 1.22.1 | MIT |
| PDFium (bblanchon/pdfium-binaries) | BSD-3-Clause / Apache-2.0 (`lib/native/x64/pdfium.LICENSE.txt`) |
| DocumentFormat.OpenXml 2.20 | MIT |
| Microsoft Visual C++ 2015-2022 runtime (app-local) | Microsoft redistributable |
