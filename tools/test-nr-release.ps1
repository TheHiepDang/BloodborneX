# Non-game portable-package smoke tests. Extracts to a fresh temporary path with spaces.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Release)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$releaseDir = (Resolve-Path $Release).Path
$testDir = Join-Path ([IO.Path]::GetTempPath()) ("BloodborneX portable smoke " + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
$checks = [Collections.Generic.List[string]]::new()
foreach ($line in Get-Content (Join-Path $releaseDir 'SHA256SUMS.txt')) {
    if ($line -notmatch '^([A-Fa-f0-9]{64})  (.+)$') { throw 'Malformed archive checksum' }
    if ((Get-FileHash (Join-Path $releaseDir $Matches[2])).Hash -ne $Matches[1]) { throw "Archive checksum mismatch: $line" }
}
$checks.Add('Portable and source ZIP SHA-256 checksums match.')
$archives = @(Get-ChildItem $releaseDir -File -Filter '*-win64.zip')
if ($archives.Count -ne 1) { throw 'Expected exactly one Windows package ZIP' }
Expand-Archive -LiteralPath $archives[0].FullName -DestinationPath $testDir
$package = Join-Path $testDir 'LanternEngine'
$manifestLines = @(Get-Content (Join-Path $package 'FILES-SHA256.txt'))
foreach ($line in $manifestLines) {
    if ($line -notmatch '^([A-Fa-f0-9]{64})  (.+)$') { throw 'Malformed file manifest' }
    $expected = $Matches[1]; $relative = $Matches[2]
    if ((Get-FileHash -LiteralPath (Join-Path $package $relative)).Hash -ne $expected) { throw "File checksum mismatch: $relative" }
}
$files = @(Get-ChildItem $package -Recurse -File)
if ($files.Count -ne $manifestLines.Count + 1) { throw 'Unexpected extra package files' }
if ($files | Where-Object { $_.Name -match '^(nvngx_dlssnr\.dll|eboot\..*|boot.*\.bin|prepared\.json|game_dir\.txt|bb-start\.exe)$' }) { throw 'Forbidden package input' }
$checks.Add("All $($manifestLines.Count) packaged-file hashes match; no NR model, native starter or game-derived inputs.")
$metadata = Get-Content (Join-Path $package 'package.json') -Raw | ConvertFrom-Json
if ($metadata.update_family -ne 'bloodbornex-native-nr-v1' -or $metadata.repository -ne 'TheHiepDang/BloodborneX' -or !(Test-Path (Join-Path $package 'LOCAL-NR-RELEASE'))) { throw 'Missing update family/marker' }
$checks.Add('Fork update metadata and NR compatibility marker present.')
$oldPath = $env:PATH
$oldBytecode = $env:PYTHONDONTWRITEBYTECODE
$oldLocation = Get-Location
try {
    $env:PATH = "$env:SystemRoot\System32;$env:SystemRoot"
    $env:PYTHONDONTWRITEBYTECODE = '1'
    Set-Location -LiteralPath $package
    $python = Join-Path $package 'engine\tools\python\python.exe'
    & $python --version
    if ($LASTEXITCODE) { throw 'Bundled Python failed' }
    & $python -c "import run_windows,patches,game_check,engine_assets; from pathlib import Path; s=patches.read_settings(Path('lantern.ini')); assert s['dlss_nr']=='0' and s['dlss_nr_white_point']=='0.125'; s['dlss_nr']='1'; assert patches.render_size(s)==(1280,720); s['dlss_nr_height']='480'; assert patches.render_size(s)==(852,480); s['preset']='0'; assert patches.render_size(s) is None; assert Path(game_check.__file__).with_name('image_109_hashes.txt').is_file(); print('PASS: bundled startup imports, defaults, 720p/480p/DLAA preparation')"
    if ($LASTEXITCODE) { throw 'Bundled startup test failed' }
    $checks.Add('Bundled Python and startup imports work without system Python/MSYS2; NR defaults and 720p/480p/DLAA preparation pass.')
    $probe = & (Join-Path $package 'engine\out\bb-probe.exe') 2>&1
    if ($LASTEXITCODE -ne 1 -or ($probe -join "`n") -notmatch 'Usage:') { throw "Engine loader smoke failed: $probe" }
    $checks.Add('Packaged bb-probe reaches its expected usage exit with PATH restricted to Windows folders.')
    & (Join-Path $package 'engine\out\bb-gpu-capabilities.exe') --live-resolution
    if ($LASTEXITCODE) { throw 'Packaged Vulkan capability probe failed' }
    $checks.Add('Packaged Vulkan capability probe succeeds with PATH restricted to Windows folders.')
} finally {
    Set-Location -LiteralPath $oldLocation.Path
    $env:PATH = $oldPath
    $env:PYTHONDONTWRITEBYTECODE = $oldBytecode
}
$report = @("# BloodborneX $($metadata.version) portable validation",'',"Completed $([DateTimeOffset]::UtcNow.ToString('o')).",'')
$report += $checks | ForEach-Object { "- PASS: $_" }
$report += @('', 'A fresh temporary extraction with spaces was used. Startup ran without system Python/MSYS2 PATH.',
    'These checks do not launch gameplay or certify rendering/NR visual quality. Vulkan validation-layer coverage is not claimed.',
    'No game, saves or proprietary NR model is included. No GitHub publication or push was performed.', '',
    "Temporary smoke extraction retained at: $testDir")
$report | Set-Content (Join-Path $releaseDir 'VALIDATION.md') -Encoding utf8
$report
