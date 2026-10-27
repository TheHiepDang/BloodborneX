# Non-game portable-package smoke tests. Extracts to a fresh temporary path with spaces.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Release, [switch]$HostedCi)
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
    # This process belongs to this fresh extraction, never to an installed user game.
    $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $package 'Lantern Engine.exe'))
    $start.WorkingDirectory = $package
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $config = Join-Path $testDir 'isolated-launcher-config'
    New-Item -ItemType Directory -Path $config -Force | Out-Null
    @{check_updates=$false;setup_shown=$true;ui_language='en';port_dir=$package} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $config 'settings.json') -Encoding utf8
    $start.Environment['BLOODBORNEX_CONFIG_DIR'] = $config
    $gui = [Diagnostics.Process]::Start($start)
    try {
        if (!$gui.WaitForInputIdle(30000)) { throw 'Packaged launcher did not reach a ready window.' }
        $gui.Refresh()
        if ($gui.HasExited -or $gui.MainWindowTitle -ne 'Lantern Engine' -or
            @(Get-ChildItem -LiteralPath $package -Recurse -File -Filter '*crash*.log').Count) {
            throw 'Packaged launcher startup failed or wrote a crash report.'
        }
        $checks.Add('Packaged self-contained WPF launcher reaches its ready window with development PATH removed.')
    } finally {
        if (!$gui.HasExited) {
            $gui.CloseMainWindow() | Out-Null
            if (!$gui.WaitForExit(10000)) { $gui.Kill(); throw 'Owned smoke-test launcher did not close cleanly.' }
        }
    }

    $capability = & (Join-Path $package 'engine\out\bb-gpu-capabilities.exe') --live-resolution 2>&1
    $capabilityExit = $LASTEXITCODE
    $capabilityText = $capability -join "`n"
    if ($capabilityExit -eq 0) {
        $checks.Add('Packaged Vulkan capability enumeration succeeds; this is not NR/gameplay validation.')
    } elseif ($HostedCi -and $capabilityExit -eq 1 -and $capabilityText.Trim() -match '^GPU scene scaling: (cannot create Vulkan instance|no Vulkan device)$') {
        $checks.Add('HARDWARE UNAVAILABLE: packaged probe ran, but this hosted runner provides no Vulkan ICD/device. GPU/NR/FG gameplay is not certified.')
        Write-Host '[Validation][Hardware] No Vulkan ICD/device on hosted runner; strict native loader, package and non-GPU checks remain required.'
    } else { throw "Packaged Vulkan capability probe failed unexpectedly: $capabilityText (exit $capabilityExit)" }
} finally {
    Set-Location -LiteralPath $oldLocation.Path
    $env:PATH = $oldPath
    $env:PYTHONDONTWRITEBYTECODE = $oldBytecode
}
$report = @("# BloodborneX $($metadata.version) portable validation",'',"Completed $([DateTimeOffset]::UtcNow.ToString('o')).",'')
$report += $checks | ForEach-Object { if ($_ -like "HARDWARE UNAVAILABLE:*") { "- $_" } else { "- PASS: $_" } }
$report += @('', 'A fresh temporary extraction with spaces was used. Startup ran without system Python/MSYS2 PATH.',
    'These checks do not launch gameplay or certify rendering/NR visual quality. Vulkan validation-layer coverage is not claimed.',
    'No game, saves or proprietary NR model is included. No GitHub publication or push was performed.', '',
    'Temporary extraction was isolated from installed games and user data.')
$report | Set-Content (Join-Path $releaseDir 'VALIDATION.md') -Encoding utf8
$report
