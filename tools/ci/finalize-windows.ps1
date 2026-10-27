#Requires -Version 7.4
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Tag,[Parameter(Mandatory)][string]$EngineSource,[Parameter(Mandatory)][string]$MsysRoot)
$ErrorActionPreference='Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $repo
$record = Get-Content -LiteralPath (Join-Path $EngineSource 'CI-SOURCE.json') -Raw | ConvertFrom-Json
$lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.json') -Raw | ConvertFrom-Json
if ($record.commit -ne $lock.engine.commit -or $record.inputs_sha256 -ne (Get-FileHash (Join-Path $PSScriptRoot 'inputs.json')).Hash.ToLowerInvariant()) {
    throw '[Source] CI requires freshly reconstructed, checksum-locked source.'
}
$env:RestoreLockedMode='true'
foreach ($name in @('UpdateChecks','NrRuntimeChecks','NrOptimizationChecks','StreamlineRuntimeChecks')) {
    & dotnet run --project "launcher/windows/tests/$name/$name.csproj" -c Release
    if ($LASTEXITCODE) { throw "[Test] Launcher regression failed: $name" }
}
& './Prepare Release.ps1' -Version $Tag.Substring(1) -EngineSource $EngineSource -MsysRoot $MsysRoot -NoPrompt -HostedCi -Jobs 2
$folder = Join-Path $repo "releases/$($Tag.Substring(1))"
$portable = Join-Path $folder 'LanternEngine'
& python tools/ci/release.py windows-deps --folder $portable
if ($LASTEXITCODE) { throw '[Package] Full x64 native import audit failed.' }
& python tools/ci/policy.py --archive (Join-Path $folder "LanternEngine-$($Tag.Substring(1))-win64.zip") --kind windows
if ($LASTEXITCODE) { throw '[Policy] Portable archive privacy audit failed.' }
& python tools/ci/policy.py --archive (Join-Path $folder "LanternEngine-$($Tag.Substring(1))-source.zip") --kind source
if ($LASTEXITCODE) { throw '[Policy] Source archive privacy audit failed.' }
New-Item -ItemType Directory -Path dist -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $folder 'Upload') -Destination (Join-Path $repo 'build/ci-upload') -Recurse
Get-ChildItem -LiteralPath (Join-Path $folder 'Upload') -File | Copy-Item -Destination dist
& python tools/ci/release.py assets --platform windows --folder dist
if ($LASTEXITCODE) { throw '[Release] Asset verification/provenance failed.' }
