#Requires -Version 7.4
[CmdletBinding()]
param(
    [string]$Version,
    [string]$EngineSource,
    [string]$MsysRoot,
    [string]$ReleaseNotesPath,
    [string]$CachedDownloads,
    [ValidateSet('LanternEngine','BloodborneX')][string]$ArtifactPrefix = 'LanternEngine',
    [string]$ReleaseRoot = (Join-Path $PSScriptRoot 'releases'),
    [ValidatePattern('^[D-Zd-z]$')][string]$BuildDrive = 'N',
    [ValidateRange(1,64)][int]$Jobs = 4,
    [switch]$NoPrompt,
    [switch]$HostedCi,
    [switch]$CheckOnly
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (!$IsWindows) { throw 'Prepare Release currently builds Windows x64 packages. Run it on Windows with PowerShell 7.' }
$repo = $PSScriptRoot
$settingsFile = Join-Path $repo '.release-settings.json'
$settings = if (Test-Path -LiteralPath $settingsFile) { Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
function Ask([string]$Label,[string]$Default='') {
    if ($NoPrompt) { return $Default }
    $answer = Read-Host ($Label + $(if ($Default) { " [$Default]" } else { '' }))
    if ([string]::IsNullOrWhiteSpace($answer)) { return $Default }
    return $answer.Trim().Trim('"')
}
function Is-Engine([string]$Folder) {
    return $Folder -and (Test-Path -LiteralPath (Join-Path $Folder 'build.sh')) -and
        (Test-Path -LiteralPath (Join-Path $Folder 'gpu\CMakeLists.txt')) -and
        (Test-Path -LiteralPath (Join-Path $Folder 'gpu\shadps4\video_core\renderer_vulkan\vk_dlss_nr.cpp'))
}
if (!$Version) { $Version = Ask 'Release version (for example 1.0.2 or 1.0.2-nr.rc1)' }
if ($Version -notmatch '^\d+\.\d+\.\d+(-nr\.rc[1-9]\d*)?$') { throw 'Enter a numeric version such as 1.0.2 or 1.0.2-nr.rc1. Do not enter v, a ZIP filename or a release title.' }
$numeric = ($Version -split '-')[0]
$parsedVersion = $null
if (![version]::TryParse($numeric,[ref]$parsedVersion) -or $parsedVersion.ToString(3) -ne $numeric -or @($numeric.Split('.') | Where-Object { [int]$_ -gt 65534 }).Count) { throw 'Use three normal version numbers between 0 and 65534, without leading zeros.' }
$release = Join-Path ([IO.Path]::GetFullPath($ReleaseRoot)) $Version
if (Test-Path -LiteralPath $release) { throw "Release already exists: $release. Choose a new version; existing releases are never overwritten." }
if (!$EngineSource) {
    if (Is-Engine $settings['EngineSource']) { $EngineSource = $settings['EngineSource'] }
    else {
        $candidate = @((Join-Path $repo 'engine-source'),(Join-Path $repo 'engine')) | Where-Object { Is-Engine $_ } | Select-Object -First 1
        $EngineSource = Ask 'Applied engine source folder (contains build.sh; remembered for next time)' $candidate
    }
}
if (!(Is-Engine $EngineSource)) { throw 'Select a complete applied engine source checkout, not a game installation or the engine/ patch-only folder. Supply -EngineSource when using -NoPrompt.' }
$engine = (Resolve-Path -LiteralPath $EngineSource).Path
$cacheSource = Join-Path $engine 'gpu\shadps4\video_core\cache_storage.cpp'
if ((Get-Content -LiteralPath $cacheSource -Raw) -match 'image-live-v1') { throw 'This source contains the undeployed live-colour prototype. Select the reviewed release engine source instead.' }
if (!$MsysRoot) { $MsysRoot = $(if ($settings['MsysRoot']) { $settings['MsysRoot'] } else { 'C:\msys64' }) }
$MsysRoot = [IO.Path]::GetFullPath($MsysRoot)
foreach ($command in @('dotnet','git')) { if (!(Get-Command $command -ErrorAction SilentlyContinue)) { throw "Missing $command on PATH. Install the documented build dependencies first." } }
$sdks = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0 -or !(@($sdks | Where-Object { $_ -match '^(8|9|[1-9]\d+)\.' }).Count)) { throw '.NET SDK 8 or newer is required (the runtime alone is not enough).' }
foreach ($tool in @('usr\bin\bash.exe','usr\bin\cygpath.exe','clang64\bin\clang.exe','clang64\bin\clang++.exe','clang64\bin\cmake.exe','clang64\bin\ninja.exe','clang64\bin\llvm-rc.exe','clang64\bin\llvm-readobj.exe')) {
    if (!(Test-Path -LiteralPath (Join-Path $MsysRoot $tool))) { throw "Missing MSYS2 CLANG64 build tool: $tool" }
}
foreach ($required in @('gpu\third_party\fsr-vulkan\CMakeLists.txt','gpu\third_party\imgui\imgui.h','third_party\LibAtrac9\C\src')) {
    if (!(Test-Path -LiteralPath (Join-Path $engine $required))) { throw "Engine source is incomplete: $required. Initialize its dependencies before building." }
}
$BuildDrive = $BuildDrive.ToUpperInvariant()
$aliasRoot = $BuildDrive + ':\'
$nativeCache = Join-Path $engine 'out\gpu\CMakeCache.txt'
$expectedHome = "$($BuildDrive):/$((Split-Path $engine -Leaf))/gpu"
if (Test-Path -LiteralPath $nativeCache) {
    $cachedHome = (Get-Content -LiteralPath $nativeCache | Where-Object { $_ -like 'CMAKE_HOME_DIRECTORY:INTERNAL=*' }) -replace '^CMAKE_HOME_DIRECTORY:INTERNAL=',''
    if ($cachedHome -and $cachedHome -ne $expectedHome) { throw "This engine cache was built at $cachedHome; the build would use $expectedHome. Use the matching -BuildDrive/source folder, or a fresh applied source checkout." }
}
if (Test-Path -LiteralPath $aliasRoot) { throw "Build drive $aliasRoot is already in use. It will not be changed. Free it or choose a different -BuildDrive with a fresh build cache." }
if (!$CachedDownloads) {
    $CachedDownloads = Get-ChildItem -LiteralPath (Join-Path $repo 'releases') -Directory -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName 'downloads' } |
        Where-Object { Test-Path -LiteralPath (Join-Path $_ 'python-3.13.17-embeddable-amd64.zip') } | Select-Object -First 1
}
if ($ReleaseNotesPath -and !(Test-Path -LiteralPath $ReleaseNotesPath -PathType Leaf)) { throw "Release notes not found: $ReleaseNotesPath" }
$details = [pscustomobject]@{ Version=$Version; Tag="v$Version"; ArtifactPrefix=$ArtifactPrefix; EngineSource=$engine; Release=$release; BuildDrive=$BuildDrive; MsysRoot=$MsysRoot }
if ($CheckOnly) { $details; return }
$engineOut = Join-Path $engine 'out'
$running = Get-Process -Name 'bb-probe' -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($engineOut+'\',[StringComparison]::OrdinalIgnoreCase) }
if ($running) { throw 'The selected engine is running. Close that game before compiling its source.' }
$work = Join-Path $repo '.release-work'
New-Item -ItemType Directory -Path $work -Force | Out-Null
if (!$ReleaseNotesPath) {
    $summary = Ask 'What changed? (short release note)' "BloodborneX $Version built from the selected engine and current launcher source."
    $ReleaseNotesPath = Join-Path $work "notes-$Version.md"
    [IO.File]::WriteAllText($ReleaseNotesPath,"# BloodborneX v$Version`n`n$summary`n`nSupply the appropriate NR DLL yourself at ``dlss/nvngx_dlssnr.dll``. See START-HERE.md for requirements.`n",[Text.UTF8Encoding]::new($false))
}
$ReleaseNotesPath = (Resolve-Path -LiteralPath $ReleaseNotesPath).Path
@{ EngineSource=$engine; MsysRoot=$MsysRoot; PowerShellPath=[Environment]::ProcessPath } | ConvertTo-Json | Set-Content -LiteralPath $settingsFile -Encoding utf8
$mapped = $false
try {
    Write-Host "[1/4] Compiling the selected engine source..." -ForegroundColor Cyan
    Write-Host "Engine source: $engine"
    & subst.exe "$($BuildDrive):" (Split-Path $engine -Parent)
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the temporary short build path.' }
    $mapped = $true
    $cygpath = Join-Path $MsysRoot 'usr\bin\cygpath.exe'
    $helper = & $cygpath -u (Join-Path $repo 'tools\build-release-engine.sh')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot translate the native build helper path.' }
    $shortEngine = '/' + $BuildDrive.ToLowerInvariant() + '/' + (Split-Path $engine -Leaf)
    & (Join-Path $MsysRoot 'usr\bin\bash.exe') --noprofile --norc $helper $shortEngine $Jobs 2>&1 |
        Tee-Object -FilePath (Join-Path $work "engine-$Version.log") | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Native compilation failed. See .release-work/engine-$Version.log. No release was packaged." }
} finally {
    if ($mapped) {
        # Remove only the drive alias created by this run, never files or a user drive.
        $mapping = & subst.exe
        $expectedTarget = (Split-Path $engine -Parent).TrimEnd('\')
        $ownMapping = @($mapping | Where-Object { $_ -match ('^'+[regex]::Escape($BuildDrive)+':\\: => ') -and $_.EndsWith($expectedTarget,[StringComparison]::OrdinalIgnoreCase) })
        if ($ownMapping.Count -eq 1) { & subst.exe "$($BuildDrive):" /D }
    }
}
$builtHash = (Get-FileHash -LiteralPath (Join-Path $engineOut 'bb-probe.exe')).Hash
Write-Host '[2/4] Compiling the launcher and packaging a clean release...' -ForegroundColor Cyan
$arguments = @{ EngineSource=$engine; Version=$Version; ReleaseNotesPath=$ReleaseNotesPath; MsysRoot=$MsysRoot; ReleaseRoot=$ReleaseRoot; ArtifactPrefix=$ArtifactPrefix }
if ($CachedDownloads) { $arguments.CachedDownloads=$CachedDownloads }
& (Join-Path $repo 'tools\build-nr-release.ps1') @arguments
Write-Host '[3/4] Checking versions, archives, metadata and portable startup...' -ForegroundColor Cyan
& (Join-Path $repo 'tools\verify-nr-release.ps1') -Release $release -Version $Version -ExpectedEngineHash $builtHash -ArtifactPrefix $ArtifactPrefix
& (Join-Path $repo 'tools\test-nr-release.ps1') -Release $release -HostedCi:$HostedCi | Out-Host
Write-Host '[4/4] Preparing the upload folder...' -ForegroundColor Cyan
$upload = Join-Path $release 'Upload'
New-Item -ItemType Directory -Path $upload | Out-Null
foreach ($name in @("$ArtifactPrefix-$Version-win64.zip","$ArtifactPrefix-$Version-source.zip",'package.json','SHA256SUMS.txt','RELEASE-NOTES.md','VALIDATION.md','PACKAGE-CHECKS.json')) {
    Copy-Item -LiteralPath (Join-Path $release $name) -Destination (Join-Path $upload $name)
}
$prerelease = $Version.Contains('-nr.rc')
@("Release ready: $Version","GitHub tag and title: v$Version",$(if ($prerelease) { 'Release type: prerelease' } else { 'Release type: stable/latest' }),
    '', 'Upload EVERY file in this Upload folder together. Keep the original filenames.',
    'The separate package.json is required. Do not rename an older ZIP to the new version.',
    '', "Embedded launcher version verified: $Version", "Freshly compiled engine SHA256: $builtHash",
    'This script prepares files locally; it does not publish or change an installed game.') |
    Set-Content -LiteralPath (Join-Path $release 'UPLOAD-INSTRUCTIONS.txt') -Encoding utf8
Write-Host "`nREADY: $upload" -ForegroundColor Green
Write-Host "GitHub tag/title: v$Version. Upload all seven files in that folder without renaming them."
