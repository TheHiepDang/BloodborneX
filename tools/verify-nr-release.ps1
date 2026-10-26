#Requires -Version 7.4
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Release,[Parameter(Mandatory)][string]$Version,[Parameter(Mandatory)][string]$ExpectedEngineHash,
      [ValidateSet('LanternEngine','BloodborneX')][string]$ArtifactPrefix='LanternEngine')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=(Resolve-Path -LiteralPath $Release).Path
$package=Join-Path $root 'LanternEngine'
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$Condition,[string]$Name) { if (!$Condition) { throw "Release check failed: $Name" };$checks.Add($Name);Write-Host "PASS: $Name" }
$metadata=Get-Content -LiteralPath (Join-Path $root 'package.json') -Raw | ConvertFrom-Json
Check ($metadata.version -eq $Version -and $metadata.engine -eq "lantern-native-nr-$Version" -and $metadata.update_family -eq 'bloodbornex-native-nr-v1' -and $metadata.repository -eq 'TheHiepDang/BloodborneX') 'Release/update metadata matches the requested version'
$exe=Join-Path $package 'Lantern Engine.exe'
$info=[Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
Check (($info.ProductVersion -split '\+')[0] -eq $Version -and ([version]$info.FileVersion).ToString(3) -eq ($Version -split '-')[0]) 'Compiled launcher version matches metadata and release tag'
Check ((Get-FileHash -LiteralPath (Join-Path $package 'engine\out\bb-probe.exe')).Hash -eq $ExpectedEngineHash) 'Package contains the engine compiled in this run'
Check ((Get-Content -LiteralPath (Join-Path $package 'LOCAL-NR-RELEASE') -First 1) -eq $Version) 'NR marker matches the release version'
Check ((Test-Path -LiteralPath (Join-Path $package 'NOTICE') -PathType Leaf) -and
       (Get-Content -LiteralPath (Join-Path $package 'NOTICE') -Raw).Contains('Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine)') -and
       (Test-Path -LiteralPath (Join-Path $package 'licenses\GPL-3.0.txt') -PathType Leaf)) 'Upstream attribution and GPL-3.0 terms are included'
Check ((Test-Path -LiteralPath (Join-Path $package 'Install Streamline.cmd') -PathType Leaf) -and
       (Test-Path -LiteralPath (Join-Path $package 'tools\Install Streamline.ps1') -PathType Leaf)) 'Streamline repair helper is included'
$streamlineOut=Join-Path $package 'engine\out'
$streamlineRecord=Get-Content -LiteralPath (Join-Path $streamlineOut 'streamline-runtime.json') -Raw | ConvertFrom-Json -AsHashtable
$streamlineFiles=@('sl.interposer.dll','sl.common.dll','sl.dlss_g.dll','sl.reflex.dll','sl.pcl.dll',
    'nvngx_dlssg.dll','NvLowLatencyVk.dll','nvngx_dlss.license.txt','reflex.license.txt','LICENSE-Streamline.txt')
Check ($streamlineRecord.version -eq '2.14.16' -and
       $streamlineRecord.sdk_sha256 -eq '32041E51DB0CAB709A271B72A352281B690C2ABBD669A827809401A5DD23956B' -and
       $streamlineRecord.files.Count -eq $streamlineFiles.Count) 'Pinned Streamline SDK record is included'
foreach ($name in $streamlineFiles) {
    $file=Join-Path $streamlineOut $name
    Check ((Test-Path -LiteralPath $file -PathType Leaf) -and
           (Get-Item -LiteralPath $file).Length -gt 0 -and
           (Get-FileHash -LiteralPath $file).Hash -eq $streamlineRecord.files[$name]) "Matched Streamline runtime file: $name"
}
Check (@(Get-ChildItem -LiteralPath $streamlineOut -File -Filter 'sl.*.dll' |
         Where-Object { $_.Name -notin $streamlineFiles }).Count -eq 0) 'No extra Streamline plugin is packaged'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$expectedArchives=@("$ArtifactPrefix-$Version-win64.zip","$ArtifactPrefix-$Version-source.zip")
$checksumLines=@(Get-Content -LiteralPath (Join-Path $root 'SHA256SUMS.txt'))
Check ($checksumLines.Count -eq 2) 'Exactly two archive checksums'
foreach ($line in $checksumLines) {
    Check ($line -match '^([A-Fa-f0-9]{64})  ([^/\\]+)$') 'Safe archive checksum entry'
    $expected=$Matches[1];$name=$Matches[2]
    Check ($name -cin $expectedArchives) 'Archive filename follows the updater contract'
    Check ((Get-FileHash -LiteralPath (Join-Path $root $name)).Hash -eq $expected) "Archive checksum: $name"
}
foreach ($name in $expectedArchives) {
    $zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $root $name))
    try {
        $files=@($zip.Entries | Where-Object { !$_.FullName.EndsWith('/') })
        if ($name.EndsWith('-source.zip') -and @($files | Where-Object {
            $_.FullName -match '/(sl\.[^/]*\.dll|nvngx_dlssg\.dll|NvLowLatencyVk\.dll)$'
        }).Count -gt 0) { throw 'Streamline binaries must not appear in the source snapshot' }
        $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($entry in $files) {
            if (!$names.Add($entry.FullName)) { throw "Duplicate ZIP path: $($entry.FullName)" }
            if ($entry.FullName -match '(^|/)(nvngx_dlssnr\.dll|eboot\.[^/]*|boot[^/]*\.bin|prepared\.json|game_dir\.txt|\.git|\.release-settings\.json|\.release-work)(/|$)|\.(pdb|dmp|log|bak|old|orig)$') { throw "Forbidden archive input: $($entry.FullName)" }
        }
        $manifest=@($files | Where-Object { $_.FullName -match '^[^/]+/FILES-SHA256\.txt$' })
        Check ($manifest.Count -eq 1) "One complete file manifest: $name"
        $prefix=$manifest[0].FullName.Split('/')[0]+'/'
        $reader=[IO.StreamReader]::new($manifest[0].Open())
        try { $lines=@($reader.ReadToEnd().TrimEnd().Split("`n")) } finally { $reader.Dispose() }
        $listed=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($line in $lines) {
            if ($line.TrimEnd("`r") -notmatch '^([A-Fa-f0-9]{64})  (.+)$') { throw 'Malformed file manifest' }
            $hash=$Matches[1];$relative=$Matches[2];$path=$prefix+$relative
            if (!$listed.Add($path)) { throw "Duplicate manifest entry: $path" }
            $entry=$zip.GetEntry($path)
            if (!$entry) { throw "Missing ZIP entry: $path" }
            $stream=$entry.Open()
            try { $actual=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
            if ($actual -ne $hash) { throw "ZIP content checksum mismatch: $path" }
        }
        Check ($listed.Count -eq $files.Count-1 -and @($files | Where-Object { $_.FullName -ne $manifest[0].FullName -and !$listed.Contains($_.FullName) }).Count -eq 0) "All $($listed.Count) archive file hashes and manifest coverage: $name"
        if ($name.EndsWith('-win64.zip')) {
            $entry=$zip.GetEntry($prefix+'package.json');$reader=[IO.StreamReader]::new($entry.Open())
            try { $inside=$reader.ReadToEnd() } finally { $reader.Dispose() }
            Check ($inside -ceq [IO.File]::ReadAllText((Join-Path $root 'package.json'))) 'Separate metadata exactly matches the Windows ZIP'
            foreach ($relative in @('Lantern Engine.exe','engine/out/bb-probe.exe')) {
                $stream=$zip.GetEntry($prefix+$relative).Open()
                try { $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
                Check ($hash -eq (Get-FileHash -LiteralPath (Join-Path $package $relative)).Hash) "Verified version/engine bytes are inside the ZIP: $relative"
            }
        }
    } finally { $zip.Dispose() }
}
@{version=$Version;tag="v$Version";engine_sha256=$ExpectedEngineHash;checks=@($checks);gameplay_tested=$false} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'PACKAGE-CHECKS.json') -Encoding utf8
