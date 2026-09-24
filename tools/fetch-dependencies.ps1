<#
.SYNOPSIS
  One-time ONLINE step that vendors every build/runtime dependency into the repository,
  so that the solution can afterwards be restored, built and run fully OFFLINE.

  - models/        PaddleOCR PP-OCRv5 models (ONNX export from RapidAI/RapidOCR) + dictionaries
  - lib/native/x64 onnxruntime.dll (ONNX Runtime CPU) and pdfium.dll (PDF rendering)
  - packages/      every .nupkg needed by `dotnet restore` (local feed, see nuget.config)

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\fetch-dependencies.ps1
  powershell -ExecutionPolicy Bypass -File tools\fetch-dependencies.ps1 -Only packages
#>
param(
    [ValidateSet('all', 'models', 'native', 'packages')]
    [string]$Only = 'all',
    [string]$OnnxRuntimeVersion = '1.22.1'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$root = Split-Path -Parent $PSScriptRoot
$tmp = Join-Path $root '.tmp'
New-Item -ItemType Directory -Force $tmp | Out-Null

function Get-File([string]$url, [string]$dest) {
    if (Test-Path $dest) { Write-Host "  exists  $dest"; return }
    New-Item -ItemType Directory -Force (Split-Path -Parent $dest) | Out-Null
    Write-Host "  get     $url"
    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile "$dest.part"
    Move-Item -Force "$dest.part" $dest
}

if ($Only -in 'all', 'models') {
    Write-Host 'Models (PP-OCRv5 mobile, ONNX)'
    $ms = 'https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/master'
    $models = @{
        'det/ch_PP-OCRv5_det_mobile.onnx'                    = 'onnx/PP-OCRv5/det/ch_PP-OCRv5_det_mobile.onnx'
        'cls/ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx' = 'onnx/PP-OCRv5/cls/ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx'
        'rec/ch_PP-OCRv5_rec_mobile.onnx'                    = 'onnx/PP-OCRv5/rec/ch_PP-OCRv5_rec_mobile.onnx'
        'rec/ppocrv5_dict.txt'                               = 'paddle/PP-OCRv5/rec/ch_PP-OCRv5_rec_mobile/ppocrv5_dict.txt'
        'rec/en_PP-OCRv5_rec_mobile.onnx'                    = 'onnx/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile.onnx'
        'rec/ppocrv5_en_dict.txt'                            = 'paddle/PP-OCRv5/rec/en_PP-OCRv5_rec_mobile/ppocrv5_en_dict.txt'
        'rec/korean_PP-OCRv5_rec_mobile.onnx'                = 'onnx/PP-OCRv5/rec/korean_PP-OCRv5_rec_mobile.onnx'
        'rec/ppocrv5_korean_dict.txt'                        = 'paddle/PP-OCRv5/rec/korean_PP-OCRv5_rec_mobile/ppocrv5_korean_dict.txt'
        'rec/eslav_PP-OCRv5_rec_mobile.onnx'                 = 'onnx/PP-OCRv5/rec/eslav_PP-OCRv5_rec_mobile.onnx'
        'rec/ppocrv5_eslav_dict.txt'                         = 'paddle/PP-OCRv5/rec/eslav_PP-OCRv5_rec_mobile/ppocrv5_eslav_dict.txt'
    }
    foreach ($k in $models.Keys) { Get-File "$ms/$($models[$k])" (Join-Path $root "models/$k") }
}

if ($Only -in 'all', 'native') {
    Write-Host 'Native libraries (win-x64)'
    $native = Join-Path $root 'lib/native/x64'
    New-Item -ItemType Directory -Force $native | Out-Null

    if (-not (Test-Path "$native/onnxruntime.dll")) {
        $pkg = Join-Path $tmp "microsoft.ml.onnxruntime.$OnnxRuntimeVersion.zip"
        Get-File "https://api.nuget.org/v3-flatcontainer/microsoft.ml.onnxruntime/$OnnxRuntimeVersion/microsoft.ml.onnxruntime.$OnnxRuntimeVersion.nupkg" $pkg
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [IO.Compression.ZipFile]::OpenRead($pkg)
        try {
            foreach ($e in $zip.Entries | Where-Object { $_.FullName -like 'runtimes/win-x64/native/*.dll' }) {
                [IO.Compression.ZipFileExtensions]::ExtractToFile($e, (Join-Path $native $e.Name), $true)
                Write-Host "  extract $($e.Name)"
            }
        } finally { $zip.Dispose() }
    }

    if (-not (Test-Path "$native/pdfium.dll")) {
        $tgz = Join-Path $tmp 'pdfium-win-x64.tgz'
        Get-File 'https://github.com/bblanchon/pdfium-binaries/releases/latest/download/pdfium-win-x64.tgz' $tgz
        $out = Join-Path $tmp 'pdfium'
        New-Item -ItemType Directory -Force $out | Out-Null
        tar -xzf $tgz -C $out
        Copy-Item (Join-Path $out 'bin/pdfium.dll') $native -Force
        Copy-Item (Join-Path $out 'LICENSE') (Join-Path $native 'pdfium.LICENSE.txt') -Force -ErrorAction SilentlyContinue
        Write-Host '  extract pdfium.dll'
    }

    # onnxruntime.dll links the VC++ 2015-2022 runtime dynamically; deploy it app-local
    # (redistributable per the Visual C++ runtime license) so no redist install is needed.
    foreach ($crt in 'msvcp140.dll', 'msvcp140_1.dll', 'vcruntime140.dll', 'vcruntime140_1.dll') {
        $src = Join-Path $env:WINDIR "System32\$crt"
        if (-not (Test-Path "$native/$crt") -and (Test-Path $src)) {
            Copy-Item $src $native
            Write-Host "  copy    $crt"
        }
    }
}

if ($Only -in 'all', 'packages') {
    Write-Host 'NuGet packages -> packages/ (local offline feed)'
    $cache = Join-Path $tmp 'nuget-cache'
    dotnet restore (Join-Path $root 'FalconOcr.sln') --configfile (Join-Path $PSScriptRoot 'nuget.online.config') --packages $cache
    if ($LASTEXITCODE -ne 0) { throw 'restore failed' }
    $feed = Join-Path $root 'packages'
    New-Item -ItemType Directory -Force $feed | Out-Null
    Get-ChildItem $cache -Recurse -Filter *.nupkg | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $feed $_.Name) -Force
        Write-Host "  vendor  $($_.Name)"
    }
}

Write-Host 'Done.'
