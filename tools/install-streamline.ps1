[CmdletBinding()]
param(
    [string]$PackageRoot = (Join-Path $PSScriptRoot '..'),
    [string]$SdkArchivePath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$version = '2.14.16'
$sdkHash = '32041E51DB0CAB709A271B72A352281B690C2ABBD669A827809401A5DD23956B'
$names = @(
    'sl.interposer.dll', 'sl.common.dll', 'sl.dlss_g.dll', 'sl.reflex.dll',
    'sl.pcl.dll', 'nvngx_dlssg.dll', 'NvLowLatencyVk.dll',
    'nvngx_dlss.license.txt', 'reflex.license.txt', 'LICENSE-Streamline.txt'
)
$root = [IO.Path]::GetFullPath($PackageRoot)
$target = Join-Path $root 'engine\out'
if (!(Test-Path -LiteralPath (Join-Path $target 'bb-probe.exe') -PathType Leaf)) {
    throw "Choose an extracted BloodborneX package with engine\out\bb-probe.exe: $root"
}
$stage = Join-Path $target ('.streamline-staging-' + [guid]::NewGuid().ToString('N'))
$archive = if ($SdkArchivePath) { [IO.Path]::GetFullPath($SdkArchivePath) }
           else { Join-Path $stage 'streamline-sdk.zip' }
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    if (!$SdkArchivePath) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $url = "https://github.com/NVIDIA-RTX/Streamline/releases/download/v$version/streamline-sdk-v$version.zip"
        Write-Host "Downloading NVIDIA Streamline SDK $version from its official release..."
        Invoke-WebRequest -Uri $url -OutFile $archive -UseBasicParsing
    }
    if (!(Test-Path -LiteralPath $archive -PathType Leaf) -or
        (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $sdkHash) {
        throw 'The SDK archive does not match NVIDIA Streamline 2.14.16. No runtime files were changed.'
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        foreach ($name in $names) {
            $entryName = if ($name -eq 'LICENSE-Streamline.txt') { 'license.txt' }
                         else { "bin/x64/$name" }
            $entry = $zip.GetEntry($entryName)
            if ($null -eq $entry -or $entry.Length -eq 0) {
                throw "The official SDK archive lacks $entryName. No runtime files were changed."
            }
            [IO.Compression.ZipFileExtensions]::ExtractToFile(
                $entry, (Join-Path $stage $name), $false)
        }
    } finally { $zip.Dispose() }
    $hashes = [ordered]@{}
    foreach ($name in $names) {
        $hashes[$name] = (Get-FileHash -LiteralPath (Join-Path $stage $name) -Algorithm SHA256).Hash
    }
    $manifest = [ordered]@{ version=$version; sdk_sha256=$sdkHash; files=$hashes }
    [IO.File]::WriteAllText(
        (Join-Path $stage 'streamline-runtime.json'),
        ($manifest | ConvertTo-Json -Depth 5),
        [Text.UTF8Encoding]::new($false))

    # Keep user-installed DLLs and notices recoverable, including unknown Streamline plugins.
    $existing = @(Get-ChildItem -LiteralPath $target -File | Where-Object {
        $_.Name -in $names -or $_.Name -eq 'streamline-runtime.json' -or
        $_.Name -like 'sl.*.dll'
    })
    if ($existing.Count) {
        $backup = Join-Path $target ('backups\before-streamline-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        New-Item -ItemType Directory -Path $backup -Force | Out-Null
        foreach ($file in $existing) { Move-Item -LiteralPath $file.FullName -Destination $backup }
        Write-Host "Previous Streamline files backed up to $backup"
    }
    foreach ($name in $names) { Move-Item -LiteralPath (Join-Path $stage $name) -Destination $target }
    Move-Item -LiteralPath (Join-Path $stage 'streamline-runtime.json') -Destination $target
    Write-Host "Verified Streamline $version installed in $target"
    Write-Host 'Select DLSS Frame Generation, then start the game. HDR gameplay still needs testing.'
} finally {
    $resolvedTarget = [IO.Path]::GetFullPath($target).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    $resolvedStage = [IO.Path]::GetFullPath($stage)
    if (!$resolvedStage.StartsWith($resolvedTarget, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean an unexpected staging path: $resolvedStage"
    }
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
