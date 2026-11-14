# Development-only extraction: no registry changes and no system-wide install.
[CmdletBinding()]
param(
    [string]$Archive = '',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\.release-work\vulkan-validation-1.4.363.0')
)
$ErrorActionPreference = 'Stop'
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$url = 'https://mirror.msys2.org/mingw/clang64/mingw-w64-clang-x86_64-vulkan-validation-layers-1.4.363.0-1-any.pkg.tar.zst'
$expected = 'D1A8BAA9B2309EDF9FD11DFF8CF3BBBFC7104F9093894EF2F2221445960A7E20'
if (!$Archive) {
    $Archive = Join-Path $output 'validation.pkg.tar.zst'
    if (!(Test-Path -LiteralPath $Archive)) { Invoke-WebRequest -Uri $url -OutFile $Archive -TimeoutSec 60 }
}
$Archive = (Resolve-Path -LiteralPath $Archive).Path
if ((Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash -ne $expected) { throw 'Validation-layer archive hash mismatch; refusing extraction' }
$tar = Join-Path $env:SystemRoot 'System32\tar.exe'
& $tar -xf $Archive -C $output
if ($LASTEXITCODE) { throw 'Validation-layer extraction failed' }
$layer = Join-Path $output 'clang64\bin'
foreach ($name in @('VkLayer_khronos_validation.dll','VkLayer_khronos_validation.json')) {
    if (!(Test-Path -LiteralPath (Join-Path $layer $name))) { throw "Missing layer file: $name" }
}
Write-Output "Extracted development layer: $layer"
Write-Output 'Pass this directory to test-nr-foveation-poc.ps1 -ValidationLayerDirectory. Existing CLANG64 dependency DLLs must remain on PATH.'
