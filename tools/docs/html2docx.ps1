<#
.SYNOPSIS
  Converts the generated HTML documents into native Word documents with Microsoft Word (COM):
  embeds pictures, inserts the table of contents, A4 page setup, header/footer with page numbers.
.EXAMPLE
  powershell -File tools\docs\html2docx.ps1 -Html build\docs\srs.html -Docx "docs\Falcon OCR - Requirements.docx" -Title "Software Requirements Specification"
#>
param(
    [Parameter(Mandatory)][string]$Html,
    [Parameter(Mandatory)][string]$Docx,
    [Parameter(Mandatory)][string]$Title
)
$ErrorActionPreference = 'Stop'
$Html = (Resolve-Path $Html).Path
$Docx = [IO.Path]::GetFullPath($Docx)

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    Write-Host "open"
    # Read-only: a lock left by an aborted run must not trigger a hidden "file in use" prompt.
    $doc = $word.Documents.Open($Html, $false, $true)
    $doc.ActiveWindow.View.Type = 3   # print layout

    # Pictures: [[IMG:path|widthPx]] placeholders become embedded inline pictures.
    while ($true) {
        $rng = $doc.Content
        $rng.Find.ClearFormatting()
        $rng.Find.MatchWildcards = $true
        if (-not $rng.Find.Execute('\[\[IMG:*\]\]')) { break }
        $spec = $rng.Text.Substring(6, $rng.Text.Length - 8)
        $parts = $spec.Split('|')
        $rng.Text = ""
        $pic = $doc.InlineShapes.AddPicture($parts[0], $false, $true, $rng)
        $wPt = [double]$parts[1] * 0.75
        if ($pic.Width -gt $wPt) { $pic.LockAspectRatio = -1; $h = $pic.Height * $wPt / $pic.Width; $pic.Width = $wPt; $pic.Height = $h }
    }
    Write-Host "pictures done"

    # Page setup: A4 portrait, 2.2 cm margins.
    $cm = 28.3465
    foreach ($sec in @($doc.Sections)) {
        $ps = $sec.PageSetup
        $ps.PaperSize = 7          # wdPaperA4
        $ps.Orientation = 0
        $ps.TopMargin = 2.2 * $cm; $ps.BottomMargin = 2.0 * $cm
        $ps.LeftMargin = 2.2 * $cm; $ps.RightMargin = 2.2 * $cm
        $ps.HeaderDistance = 1.0 * $cm; $ps.FooterDistance = 1.0 * $cm
        $ps.DifferentFirstPageHeaderFooter = $true
    }

    # Pictures wider than the text column are scaled down (keeps aspect ratio).
    $maxW = $doc.Sections.Item(1).PageSetup.PageWidth - $doc.Sections.Item(1).PageSetup.LeftMargin - $doc.Sections.Item(1).PageSetup.RightMargin
    foreach ($s in @($doc.InlineShapes)) {
        if ($s.Width -gt $maxW) { $s.LockAspectRatio = -1; $h = $s.Height * $maxW / $s.Width; $s.Width = $maxW; $s.Height = $h }
    }

    # Header / footer.
    $sec = $doc.Sections.Item(1)
    $hdr = $sec.Headers.Item(1).Range
    $hdr.Text = "Falcon OCR 1.0  ·  $Title"
    $hdr.Font.Name = "Calibri"; $hdr.Font.Size = 8.5; $hdr.Font.Color = 0x7E7064; $hdr.ParagraphFormat.Alignment = 2
    $ftr = $sec.Footers.Item(1).Range
    $ftr.Text = "Page "
    $ftr.Font.Name = "Calibri"; $ftr.Font.Size = 8.5; $ftr.Font.Color = 0x7E7064; $ftr.ParagraphFormat.Alignment = 1
    $r = $sec.Footers.Item(1).Range; $r.Collapse(0); $r.Fields.Add($r, 33) | Out-Null            # PAGE
    $r = $sec.Footers.Item(1).Range; $r.Collapse(0); $r.InsertAfter(" of "); $r.Collapse(0); $r.Fields.Add($r, 26) | Out-Null   # NUMPAGES

    # Headings stay on the page of the text that follows them.
    foreach ($id in -2, -3, -4) { try { $doc.Styles.Item($id).ParagraphFormat.KeepWithNext = -1 } catch {} }

    Write-Host "layout done"
    # Table of contents at the [[TOC]] marker.
    $find = $doc.Content
    if ($find.Find.Execute("[[TOC]]")) {
        $find.Text = ""
        $doc.TablesOfContents.Add($find, $true, 1, 2) | Out-Null
    }

    # Tables: repeat header row on page breaks, don't split rows.
    foreach ($t in @($doc.Tables)) {
        try { $t.Rows.Item(1).HeadingFormat = -1 } catch {}
        try { $t.Rows.AllowBreakAcrossPages = 0 } catch {}
    }

    try { $doc.BuiltInDocumentProperties.Item("Title").Value = "Falcon OCR - $Title" } catch {}
    try { $doc.BuiltInDocumentProperties.Item("Author").Value = "Falcon OCR Development Team" } catch {}
    foreach ($toc in @($doc.TablesOfContents)) { $toc.Update() }
    $doc.Fields.Update() | Out-Null
    New-Item -ItemType Directory -Force (Split-Path $Docx) | Out-Null
    $doc.SaveAs2($Docx, 16)       # wdFormatXMLDocument
    $pages = $doc.ComputeStatistics(2)
    $doc.Close($false)
    "{0}  ({1} pages)" -f $Docx, $pages
}
finally {
    $word.Quit()
    [Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
