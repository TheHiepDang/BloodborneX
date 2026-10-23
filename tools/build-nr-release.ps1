# Build a fresh, allow-listed local Windows NR candidate; never copy a working game folder.
# Requires PowerShell 7, .NET 8+ SDK, MSYS2 CLANG64 and a built, patched engine checkout.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EngineSource,
    [string]$Version = '0.9.9-nr.rc1',
    [string]$MsysRoot = 'C:\msys64',
    [string]$ReleaseRoot = (Join-Path $PSScriptRoot '..\releases')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$engine = (Resolve-Path $EngineSource).Path
$release = Join-Path ([IO.Path]::GetFullPath($ReleaseRoot)) $Version
if ($Version -notmatch '^\d+\.\d+\.\d+-nr\.rc\d+$') { throw 'Expected version such as 0.9.9-nr.rc1' }
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
        Invoke-WebRequest -Uri $Url -OutFile $target -TimeoutSec 300
        $actual = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if ($Hash -and $actual -ne $Hash) { throw "Download hash mismatch: $Name" }
        $script:downloads.Add([ordered]@{name=$Name; url=$Url; sha256=$actual})
        return $target
    }
    $script:downloads = [Collections.Generic.List[object]]::new()
    Run 'dotnet' @('run','--project',(Join-Path $repo 'launcher\windows\tests\UpdateChecks\UpdateChecks.csproj'),'-c','Release')
    $publish = Join-Path $build 'launcher'
    Run 'dotnet' @('publish', (Join-Path $repo 'launcher\windows\BloodborneLauncher\BloodborneLauncher.csproj'),
        '-c','Release','-r','win-x64','--self-contained','true','-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true','-p:EnableCompressionInSingleFile=true',
        '-p:DebugType=none',"-p:Version=$Version",'-o',$publish,'--nologo')
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
    $python = Download 'https://www.python.org/ftp/python/3.13.17/python-3.13.17-embeddable-amd64.zip' 'python-3.13.17-embeddable-amd64.zip' '0BA1EE4BFCBDC3A559D27E2E9CE76D04A7839947A23F46375872C755C0D463B8'
    $pythonDir = Join-Path $package 'engine\tools\python'
    Expand-Archive -LiteralPath $python -DestinationPath $pythonDir
    # Generated embedded-runtime search configuration: no dependence on a machine Python install.
    [IO.File]::WriteAllText((Join-Path $pythonDir 'python313._pth'), "python313.zip`n.`n..\..\scripts`n", [Text.UTF8Encoding]::new($false))
    $sr = Download 'https://raw.githubusercontent.com/NVIDIA/DLSS/v310.9.1/lib/Windows_x86_64/rel/nvngx_dlss.dll' 'nvngx_dlss.dll' '3975567B8943C53ACCE397F2B72380092F84F162D00B0D2C7D08A1025C563983'
    Copy-One $sr (Join-Path $targetOut 'nvngx_dlss.dll')
    $srLicense = Download 'https://raw.githubusercontent.com/NVIDIA/DLSS/v310.9.1/LICENSE.txt' 'NVIDIA-DLSS-LICENSE.txt'
    Copy-One $srLicense (Join-Path $package 'licenses\NVIDIA-DLSS-LICENSE.txt')
    Copy-One (Join-Path $pythonDir 'LICENSE.txt') (Join-Path $package 'licenses\Python-LICENSE.txt')
    Copy-One (Join-Path $repo 'LICENSE') (Join-Path $package 'licenses\Lantern-GPL-2.0.txt')
    Copy-Item -LiteralPath (Join-Path $engine 'licenses') -Destination (Join-Path $package 'licenses\engine') -Recurse
    Copy-Item -LiteralPath (Join-Path $MsysRoot 'clang64\share\licenses') -Destination (Join-Path $package 'licenses\msys2-clang64') -Recurse
    & (Join-Path $PSScriptRoot 'copy-nr-notices.ps1') -EngineSource $engine -Package $package
    foreach ($name in @('DLSSNR-HANDOFF.md','NR-RC1.md','NR-RELEASE.md')) {
        Copy-One (Join-Path $repo "docs\$name") (Join-Path $package "docs\$name")
    }
    Copy-One (Join-Path $repo 'docs\NR-RELEASE.md') (Join-Path $package 'START-HERE.md')
    [IO.File]::WriteAllText((Join-Path $package 'LOCAL-NR-RELEASE'), "$Version`nUpdates require compatible full packages from TheHiepDang/BloodborneX.`n")
    @{version=$Version;engine="lantern-native-nr-$Version";update_family='bloodbornex-native-nr-v1';repository='TheHiepDang/BloodborneX'} |
        ConvertTo-Json | Set-Content (Join-Path $package 'package.json') -Encoding utf8
    Copy-One (Join-Path $package 'package.json') (Join-Path $release 'package.json')
    $provenance = [ordered]@{
        version=$Version; created_utc=[DateTime]::UtcNow.ToString('o');
        repository_head=(& git -C $repo rev-parse HEAD); dirty_source=$true;
        engine_upstream='https://github.com/DarkIzuku/bloodborne_pc'; engine_base='8ecba9a4abdd54747e31d7fee54982e422b5f8e2';
        engine_patch_sha256=(Get-FileHash (Join-Path $repo 'engine\bbhost-changes.patch')).Hash;
        tested_engine_outputs=$engineHashes; downloads=$script:downloads;
        nr_runtime_included=$false; startup='bundled Python; native bb-start intentionally omitted';
        optional_assets_omitted=@('Streamline/DLSS-G','FSR4/FSR4.1.1','bbbridge online helper');
        toolchain=(& (Join-Path $bin 'clang.exe') --version | Select-Object -First 1)
    }
    $provenance | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $package 'BUILD-PROVENANCE.json') -Encoding utf8
    # Source snapshot includes uncommitted final sources, but excludes ignored binaries/runtime/user data.
    $source = Join-Path $release 'LanternEngine-source'
    function Copy-GitSource([string]$From,[string]$To) {
        $files = & git -C $From -c core.quotepath=false ls-files --cached --others --exclude-standard
        if ($LASTEXITCODE) { throw "Cannot enumerate source $From" }
        foreach ($relative in $files | Select-Object -Unique) {
            $file = Join-Path $From $relative
            if ((Test-Path -LiteralPath $file -PathType Leaf) -and $relative -notmatch '(^|/)(nvngx_dlssnr\.dll|releases)(/|$)') {
                Copy-One $file (Join-Path $To $relative)
            }
        }
    }
    Copy-GitSource $repo (Join-Path $source 'BloodborneX')
    Copy-GitSource $engine (Join-Path $source 'engine-source')
    foreach ($submodule in @('gpu/third_party/fsr-vulkan','gpu/third_party/imgui','third_party/LibAtrac9')) {
        Copy-GitSource (Join-Path $engine $submodule) (Join-Path $source "engine-source/$submodule")
    }
    Copy-One (Join-Path $package 'BUILD-PROVENANCE.json') (Join-Path $source 'BUILD-PROVENANCE.json')
    # The only binaries allowed in the portable tree are explicitly built/copied above.
    $forbidden = Get-ChildItem $package,$source -Recurse -File | Where-Object {
        $_.Name -match '^(nvngx_dlssnr\.dll|eboot\..*|boot.*\.bin|prepared\.json|game_dir\.txt|msys-2\.0\.dll)$' -or
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
    foreach ($entry in @(@($package,"LanternEngine-$Version-win64.zip"), @($source,"LanternEngine-$Version-source.zip"))) {
        [IO.Compression.ZipFile]::CreateFromDirectory($entry[0],(Join-Path $release $entry[1]),[IO.Compression.CompressionLevel]::Optimal,$true)
    }
    Get-ChildItem $release -File -Filter '*.zip' | ForEach-Object { '{0}  {1}' -f (Get-FileHash $_.FullName).Hash,$_.Name } |
        Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii
    Write-Host "Release created: $release"
} finally { Stop-Transcript | Out-Null }
