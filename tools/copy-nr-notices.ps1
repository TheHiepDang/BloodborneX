# Supplement MSYS2 notices with statically linked/vendor and self-contained launcher notices.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$EngineSource,[Parameter(Mandatory)][string]$Package)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Copy-Notice([string]$From,[string]$Relative) {
    $to = Join-Path $Package "licenses\$Relative"
    New-Item -ItemType Directory -Path (Split-Path $to) -Force | Out-Null
    Copy-Item -LiteralPath $From -Destination $to -Force
}
foreach ($relative in @('LICENSE','gpu/shadps4/LICENSE','gpu/third_party/fsr-vulkan/LICENSE.txt',
    'gpu/third_party/fsr-vulkan/NOTICE.md','gpu/third_party/fsr-vulkan/upstream/ffx-1.1.4/sdk/LICENSE.txt',
    'gpu/third_party/imgui/LICENSE.txt','gpu/third_party/fonts/LICENSE-DejaVu.txt',
    'gpu/third_party/fonts/LICENSE-LanternMenuSC.txt','gpu/third_party/half/LICENSE.txt',
    'gpu/third_party/sirit/LICENSE.txt','gpu/third_party/sirit/externals/SPIRV-Headers/LICENSE',
    'gpu/third_party/streamline/LICENSE.txt','third_party/LibAtrac9/LICENSE')) {
    Copy-Notice (Join-Path $EngineSource $relative) "vendor/$relative"
}
$project = Join-Path $repo 'launcher\windows\BloodborneLauncher'
$assets = Get-Content (Join-Path $project 'obj\project.assets.json') -Raw | ConvertFrom-Json
$nuget = @($assets.packageFolders.PSObject.Properties.Name)[0]
$config = Get-Content (Join-Path $project 'bin\Release\net8.0-windows\win-x64\Lantern Engine.runtimeconfig.json') -Raw | ConvertFrom-Json
$packages = @('wpf-ui/4.3.0','wpf-ui.abstractions/4.3.0')
foreach ($framework in $config.runtimeOptions.includedFrameworks) {
    $packages += "$($framework.name.ToLowerInvariant()).runtime.win-x64/$($framework.version)"
}
foreach ($dependency in $packages) {
    $folder = Join-Path $nuget $dependency
    $notices = @(Get-ChildItem -LiteralPath $folder -File | Where-Object Name -Match '^(LICENSE|THIRD.?PARTY.?NOTICES)')
    if (!$notices.Count) { throw "No notices for launcher dependency: $dependency" }
    foreach ($file in $notices) { Copy-Notice $file.FullName "launcher/$dependency/$($file.Name)" }
}
