# Build a fresh, allow-listed local Windows NR candidate; never copy a working game folder.
# Requires PowerShell 7, .NET 8+ SDK, MSYS2 CLANG64 and a built, patched engine checkout.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EngineSource,
    [string]$Version = '1.0.1',
    [string]$ReleaseNotesFile = '',
    [string]$ReleaseNotesPath = '',
    [string]$MsysRoot = 'C:\msys64',
    [string]$CachedDownloads = '',
    [ValidateSet('LanternEngine','BloodborneX')][string]$ArtifactPrefix = 'LanternEngine',
    [string]$ReleaseRoot = (Join-Path $PSScriptRoot '..\releases')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$engine = (Resolve-Path $EngineSource).Path
$release = Join-Path ([IO.Path]::GetFullPath($ReleaseRoot)) $Version
if ($Version -notmatch '^\d+\.\d+\.\d+(-nr\.rc\d+)?$') { throw 'Expected a stable version such as 1.0.1 or an NR candidate such as 0.9.9-nr.rc2' }
if ($ReleaseNotesFile) { throw 'Internal docs are not release inputs. Supply a public -ReleaseNotesPath instead.' }
$notes = if ($ReleaseNotesPath) { [IO.Path]::GetFullPath($ReleaseNotesPath) } else { '' }
if ($notes -and !(Test-Path -LiteralPath $notes -PathType Leaf)) { throw "Missing public release notes: $notes" }
$inputLock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ci/inputs.json') -Raw | ConvertFrom-Json
if (Test-Path -LiteralPath $release) { throw "Refusing to overwrite existing release: $release" }
$bin = Join-Path $MsysRoot 'clang64\bin'
$out = Join-Path $engine 'out'
foreach ($file in @('bb-probe.exe','bb-gpu-capabilities.exe','bb-engine-assets.exe','bb-shader-lab.exe','nvngx.dll_dlssnr.dll','gpu-cache.id','gpu-cache.legacy.id')) {
    if (!(Test-Path -LiteralPath (Join-Path $out $file))) { throw "Missing tested engine output: $file" }
}
New-Item -ItemType Directory -Path $release | Out-Null
$package = Join-Path $release 'LanternEngine'
$build = Join-Path $release 'build'
$cache = Join-Path $release 'downloads'
foreach ($dir in @($package,$build,$cache)) { New-Item -ItemType Directory -Path $dir | Out-Null }
if (!$notes) {
    $notes = Join-Path $build 'public-notes.txt'
    [IO.File]::WriteAllText($notes, "BloodborneX v$Version`n`nA changed, unofficial fork of Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine).`nSupply your own NR model at dlss/nvngx_dlssnr.dll. See START-HERE.md.`n")
}
Start-Transcript -Path (Join-Path $release 'build.log') | Out-Null
try {
    function Copy-One([string]$From, [string]$To) {
        New-Item -ItemType Directory -Path (Split-Path $To) -Force | Out-Null
        Copy-Item -LiteralPath $From -Destination $To
    }
    function Run([string]$Exe, [string[]]$Arguments) {
        & $Exe @Arguments
        if ($LASTEXITCODE -ne 0) { throw "$Exe failed: $LASTEXITCODE" }
    }
    function Download([string]$Url, [string]$Name, [string]$Hash = '') {
        $target = Join-Path $cache $Name
        $cached = if ($CachedDownloads) { Join-Path $CachedDownloads $Name } else { '' }
        if ($cached -and (Test-Path -LiteralPath $cached)) {
            Copy-Item -LiteralPath $cached -Destination $target
        } else {
            Invoke-WebRequest -Uri $Url -OutFile $target -TimeoutSec 300
        }
        $actual = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if ($Hash -and $actual -ne $Hash) { throw "Download hash mismatch: $Name" }
        $script:downloads.Add([ordered]@{name=$Name; url=$Url; sha256=$actual})
        return $target
    }
    $script:downloads = [Collections.Generic.List[object]]::new()
    Run 'dotnet' @('run','--project',(Join-Path $repo 'launcher\windows\tests\UpdateChecks\UpdateChecks.csproj'),'-c','Release')
    Run 'dotnet' @('run','--project',(Join-Path $repo 'launcher\windows\tests\NrRuntimeChecks\NrRuntimeChecks.csproj'),'-c','Release')
    $publish = Join-Path $build 'launcher'
    Run 'dotnet' @('publish', (Join-Path $repo 'launcher\windows\BloodborneLauncher\BloodborneLauncher.csproj'),
        '-c','Release','-r','win-x64','--self-contained','true','-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true','-p:EnableCompressionInSingleFile=true',
        '-p:DebugType=none',"-p:Version=$Version",'-o',$publish,'--nologo')
    $actualVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $publish 'Lantern Engine.exe')).ProductVersion
    if (($actualVersion -split '\+')[0] -ne $Version) { throw "Compiled launcher version $actualVersion does not match requested release $Version" }
    Copy-One (Join-Path $publish 'Lantern Engine.exe') (Join-Path $package 'Lantern Engine.exe')
    foreach ($forward in @(@('BloodbornePlay','play.rc','Bloodborne.exe'), @('LanternForward','forward.rc','Bloodborne Launcher.exe'))) {
        Push-Location (Join-Path $repo "launcher\windows\$($forward[0])")
        try {
            $res = Join-Path $build "$($forward[0]).res"
            Run (Join-Path $bin 'llvm-rc.exe') @('/fo',$res,$forward[1])
            Run (Join-Path $bin 'clang.exe') @('-municode','-mwindows','-O2','main.c',$res,'-o',(Join-Path $package $forward[2]))
        } finally { Pop-Location }
    }
    $targetOut = Join-Path $package 'engine\out'
    # Deliberately omit bb-start.exe: RC1 uses the NR-aware Python startup path.
    $engineHashes = @()
    foreach ($file in @('bb-probe.exe','bb-gpu-capabilities.exe','bb-engine-assets.exe','bb-shader-lab.exe','nvngx.dll_dlssnr.dll','gpu-cache.id','gpu-cache.legacy.id')) {
        $from = Join-Path $out $file
        Copy-One $from (Join-Path $targetOut $file)
        $engineHashes += [ordered]@{name=$file;sha256=(Get-FileHash $from).Hash}
    }
    Copy-One (Join-Path $out 'nvngx.dll_dlssnr.dll') (Join-Path $package 'dlss\nvngx.dll_dlssnr.dll')
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $engine 'scripts') -File -Filter '*.py') {
        Copy-One $file.FullName (Join-Path $package "engine\scripts\$($file.Name)")
    }
    foreach ($file in @('Bloodborne.xml','Bloodborne-1.09-sites.json','redzone.json','redzone_functions.json')) {
        Copy-One (Join-Path $engine "patches\$file") (Join-Path $package "engine\patches\$file")
    }
    Copy-One (Join-Path $engine 'data\image_109_hashes.txt') (Join-Path $package 'engine\data\image_109_hashes.txt')
    Copy-One (Join-Path $engine 'scripts\image_109_hashes.txt') (Join-Path $package 'engine\scripts\image_109_hashes.txt')
    Copy-One (Join-Path $PSScriptRoot 'nr-release-default.ini') (Join-Path $package 'lantern.ini')
    Copy-One (Join-Path $PSScriptRoot 'nr-release-default.ini') (Join-Path $package 'engine\lantern.default.ini')
    Copy-One (Join-Path $PSScriptRoot 'Install Streamline.cmd') (Join-Path $package 'Install Streamline.cmd')
    Copy-One (Join-Path $PSScriptRoot 'install-streamline.ps1') (Join-Path $package 'tools\Install Streamline.ps1')
    # Ship the signed production runtime with the package so an updater install is
    # ready for DLSS-G without a separate user setup step. The installer extracts
    # only the pinned files and writes their verified manifest.
    $streamline = Download $inputLock.downloads.streamline.url 'streamline-sdk-v2.14.16.zip' $inputLock.downloads.streamline.sha256
    & (Join-Path $PSScriptRoot 'install-streamline.ps1') -PackageRoot $package -SdkArchivePath $streamline
    if (!$?) { throw 'Could not install the verified Streamline runtime into the package' }
    & (Join-Path $PSScriptRoot 'ci/stage-msvc-runtime.ps1') -Package $package
    # Resolve the native dependency closure rather than assuming MSYS2 is installed on the player PC.
    $queue = [Collections.Generic.Queue[string]]::new()
    Get-ChildItem $package -Recurse -File | Where-Object Extension -In '.exe','.dll' | ForEach-Object { $queue.Enqueue($_.FullName) }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    while ($queue.Count) {
        $image = $queue.Dequeue()
        if (!$seen.Add($image)) { continue }
        $imports = & (Join-Path $bin 'llvm-readobj.exe') --coff-imports $image
        if ($LASTEXITCODE) { throw "Cannot inspect $image" }
        foreach ($line in $imports) {
            if ($line -notmatch '^\s+Name: (.+\.dll)\s*$') { continue }
            $name = $Matches[1]
            if ($name -match '^(api-ms-|ext-ms-)' -or (Test-Path -LiteralPath (Join-Path $env:SystemRoot "System32\$name"))) { continue }
            $dest = Join-Path $targetOut $name
            if (!(Test-Path -LiteralPath $dest)) {
                $found = @((Join-Path $out $name),(Join-Path $bin $name)) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
                if (!$found) { throw "Unresolved dependency $name ($image)" }
                Copy-One $found $dest
            }
            $queue.Enqueue($dest)
        }
    }
    $python = Download $inputLock.downloads.python.url 'python-3.13.17-embeddable-amd64.zip' $inputLock.downloads.python.sha256
    $pythonDir = Join-Path $package 'engine\tools\python'
    Expand-Archive -LiteralPath $python -DestinationPath $pythonDir
    # Generated embedded-runtime search configuration: no dependence on a machine Python install.
    [IO.File]::WriteAllText((Join-Path $pythonDir 'python313._pth'), "python313.zip`n.`n..\..\scripts`n", [Text.UTF8Encoding]::new($false))
    $sr = Download $inputLock.downloads.'dlss-sr'.url 'nvngx_dlss.dll' $inputLock.downloads.'dlss-sr'.sha256
    Copy-One $sr (Join-Path $targetOut 'nvngx_dlss.dll')
    $srLicense = Download $inputLock.downloads.'dlss-sr-license'.url 'NVIDIA-DLSS-LICENSE.txt' $inputLock.downloads.'dlss-sr-license'.sha256
    Copy-One $srLicense (Join-Path $package 'licenses\NVIDIA-DLSS-LICENSE.txt')
    Copy-One (Join-Path $pythonDir 'LICENSE.txt') (Join-Path $package 'licenses\Python-LICENSE.txt')
    Copy-One (Join-Path $repo 'LICENSE') (Join-Path $package 'licenses\GPL-2.0-legacy.txt')
    Copy-One (Join-Path $repo 'licenses\GPL-3.0.txt') (Join-Path $package 'licenses\GPL-3.0.txt')
    Copy-One (Join-Path $repo 'NOTICE') (Join-Path $package 'NOTICE')
    Copy-One (Join-Path $repo 'licenses/Chiaki-ngx-AGPL-3.0-OpenSSL.txt') (Join-Path $package 'licenses/Chiaki-ngx-AGPL-3.0-OpenSSL.txt')
    Copy-Item -LiteralPath (Join-Path $engine 'licenses') -Destination (Join-Path $package 'licenses\engine') -Recurse
    Copy-Item -LiteralPath (Join-Path $MsysRoot 'clang64\share\licenses') -Destination (Join-Path $package 'licenses\msys2-clang64') -Recurse
    & (Join-Path $PSScriptRoot 'copy-nr-notices.ps1') -EngineSource $engine -Package $package
    $startText = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'nr-start-here.template.txt')).Replace('{{VERSION}}',$Version).Replace('{{BUILD_DATE}}',[DateTimeOffset]::UtcNow.ToString('yyyy-MM-dd')+' UTC')
    $releaseKind = if ($Version.Contains('-nr.rc')) { 'prerelease' } else { 'stable release' }
    $startText = $startText.Replace('{{RELEASE_KIND}}',$releaseKind)
    [IO.File]::WriteAllText((Join-Path $package 'START-HERE.md'),$startText,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $package 'LOCAL-NR-RELEASE'), "$Version`nUpdates require compatible full packages from TheHiepDang/BloodborneX.`n")
    @{version=$Version;engine="lantern-native-nr-$Version";update_family='bloodbornex-native-nr-v1';repository='TheHiepDang/BloodborneX'} |
        ConvertTo-Json | Set-Content (Join-Path $package 'package.json') -Encoding utf8
    Copy-One (Join-Path $package 'package.json') (Join-Path $release 'package.json')
    Copy-One (Join-Path $repo 'README.md') (Join-Path $package 'README.md')
    Copy-One $notes (Join-Path $package 'RELEASE-NOTES.md')
    Copy-One $notes (Join-Path $release 'RELEASE-NOTES.md')
    $supplemental = @('frame-generation-memory-fix.patch','nr-runtime-compatibility-fix.patch','dlss-fg-hdr-test.patch') | ForEach-Object {
        @{name=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $repo "engine\$_")).Hash}
    }

    $provenance = [ordered]@{
        version=$Version; artifact_prefix=$ArtifactPrefix; created_utc=[DateTime]::UtcNow.ToString('o');
        repository_head=(& git -C $repo rev-parse HEAD); dirty_source=([bool](& git -C $repo status --porcelain --untracked-files=no));
        engine_upstream='https://github.com/DarkIzuku/bloodborne_pc'; engine_base='8ecba9a4abdd54747e31d7fee54982e422b5f8e2';
        engine_patch_sha256=(Get-FileHash (Join-Path $repo 'engine\bbhost-changes.patch')).Hash;
        supplemental_engine_patches=@($supplemental); applied_engine_snapshot='LanternEngine-source/engine-source';
        fixes=@('FSR FG transient allocation retirement','RTX 50 original NR runtime selection','NR Performance model scaling','DLSS-G HDR presenter input','Complete Streamline runtime packaged','Discord features removed');
        native_reconstruction=$(if (Test-Path -LiteralPath (Join-Path $engine 'CI-SOURCE.json')) { Get-Content -LiteralPath (Join-Path $engine 'CI-SOURCE.json') -Raw | ConvertFrom-Json } else { @{ mode='local-applied-snapshot'; reproducible_source_verified=$false; approved_markdown=@($inputLock.inherited_engine_markdown) } });
        dependency_lock_sha256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'ci/inputs.json')).Hash;
        app_local_crt=(Get-Content -LiteralPath (Join-Path $targetOut 'msvc-runtime.json') -Raw | ConvertFrom-Json);
        tested_engine_outputs=$engineHashes; downloads=$script:downloads;
        nr_runtime_included=$false; streamline_runtime_included=$true; startup='bundled Python; native bb-start intentionally omitted';
        optional_assets_omitted=@('FSR4/FSR4.1.1','bbbridge online helper');
        toolchain=(& (Join-Path $bin 'clang.exe') --version | Select-Object -First 1)
    }
    $provenance | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $package 'BUILD-PROVENANCE.json') -Encoding utf8
    # Source snapshot includes uncommitted final sources, but excludes ignored binaries/runtime/user data.
    $source = Join-Path $release 'LanternEngine-source'
    function Copy-GitSource([string]$From,[string]$To) {
        $kind = if ($From -eq $repo) { 'repo' } else { 'engine' }
        Run 'python' @((Join-Path $PSScriptRoot 'ci/policy.py'), '--stage-from', $From, '--stage-to', $To,
                       '--source-kind', $kind, '--engine-root', $engine)
    }
    Copy-GitSource $repo (Join-Path $source 'BloodborneX')
    Copy-GitSource $engine (Join-Path $source 'engine-source')
    $provenance.native_reconstruction | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $source 'engine-source/CI-SOURCE.json') -Encoding utf8
    foreach ($submodule in @('gpu/third_party/fsr-vulkan','gpu/third_party/imgui','third_party/LibAtrac9')) {
        Copy-GitSource (Join-Path $engine $submodule) (Join-Path $source "engine-source/$submodule")
    }
    foreach ($helper in @('build-nr-release.ps1','nr-release-default.ini','copy-nr-notices.ps1','build-release-engine.sh','start-release.ps1','verify-nr-release.ps1','test-nr-release.ps1','nr-start-here.template.txt','install-streamline.ps1','Install Streamline.cmd')) {
        Copy-One (Join-Path $PSScriptRoot $helper) (Join-Path $source "BloodborneX/tools/$helper")
    }
    @'
# Building this applied source snapshot

Use engine-source/ directly: it already includes the current release fixes and
vendored source. Do not reapply BloodborneX/engine/bbhost-changes.patch on top.
That cumulative legacy patch is retained as historical context; the applied
snapshot is authoritative for this release. See BUILD-PROVENANCE.json.

Build engine-source/build.sh with the documented MSYS2 CLANG64 dependencies.
The launcher source is BloodborneX/launcher/windows. No game, NR runtime or
Streamline binaries are included in this source snapshot. The portable package
contains the pinned NVIDIA Streamline runtime and its licence texts.
Packaging scripts are in BloodborneX/tools.
'@ | Set-Content -LiteralPath (Join-Path $source 'BUILD-FROM-SNAPSHOT.md') -Encoding utf8
    Copy-One (Join-Path $package 'BUILD-PROVENANCE.json') (Join-Path $source 'BUILD-PROVENANCE.json')
    # The only binaries allowed in the portable tree are explicitly built/copied above.
    $forbidden = Get-ChildItem $package,$source -Recurse -File | Where-Object {
        $_.Name -match '^(nvngx_dlssnr\.dll|eboot\..*|boot.*\.bin|prepared\.json|game_dir\.txt|msys-2\.0\.dll)$' -or
        ($_.FullName.StartsWith($source+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -and
         $_.Name -match '^(nvngx_dlssg\.dll|NvLowLatencyVk\.dll|sl\..*\.dll)$') -or
        $_.Extension -in '.pdb','.dmp','.log','.orig','.bak','.old'
    }
    if ($forbidden) { throw "Forbidden release content: $($forbidden[0].FullName)" }
    foreach ($tree in @($package,$source)) {
        $manifest = foreach ($file in Get-ChildItem $tree -Recurse -File | Sort-Object FullName) {
            '{0}  {1}' -f (Get-FileHash $file.FullName).Hash, ([IO.Path]::GetRelativePath($tree,$file.FullName).Replace('\','/'))
        }
        $manifest | Set-Content (Join-Path $tree 'FILES-SHA256.txt') -Encoding utf8
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach ($entry in @(@($package,"$ArtifactPrefix-$Version-win64.zip"), @($source,"$ArtifactPrefix-$Version-source.zip"))) {
        [IO.Compression.ZipFile]::CreateFromDirectory($entry[0],(Join-Path $release $entry[1]),[IO.Compression.CompressionLevel]::Optimal,$true)
    }
    Get-ChildItem $release -File -Filter '*.zip' | ForEach-Object { '{0}  {1}' -f (Get-FileHash $_.FullName).Hash,$_.Name } |
        Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii
    Write-Host "Release created: $release"
} finally { Stop-Transcript | Out-Null }
