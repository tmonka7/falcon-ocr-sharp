<#
.SYNOPSIS
  Offline build of Falcon OCR. Restores only from the vendored feed in .\packages (see nuget.config)
  and copies a ready-to-run application (with models and native libraries) to .\dist.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build.ps1
  powershell -ExecutionPolicy Bypass -File build.ps1 -Configuration Debug
#>
param([ValidateSet('Release', 'Debug')][string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

foreach ($required in 'models\det\ch_PP-OCRv5_det_mobile.onnx', 'lib\native\x64\onnxruntime.dll', 'lib\native\x64\pdfium.dll') {
    if (-not (Test-Path (Join-Path $root $required))) {
        throw "Missing $required. Run tools\fetch-dependencies.ps1 once on a machine with internet access."
    }
}

dotnet build (Join-Path $root 'FalconOcr.sln') -c $Configuration -p:Platform=x64 -nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$dist = Join-Path $root 'dist'
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
$app = Join-Path $root "src\FalconOcr.App\bin\x64\$Configuration\net47"
$cli = Join-Path $root "src\FalconOcr.Cli\bin\x64\$Configuration\net47"
Copy-Item $app $dist -Recurse
Copy-Item (Join-Path $cli 'falcon-ocr.exe*') $dist
Get-ChildItem $dist -Filter *.pdb | Remove-Item
Write-Host ""
Write-Host "Ready: $dist\FalconOcr.exe  (command line: $dist\falcon-ocr.exe)"
