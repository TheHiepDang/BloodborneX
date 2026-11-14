# Compile and run the fixed M2 prototype's CPU and existing Vulkan NR fixtures.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EngineSource,
    [string]$MsysRoot = 'C:\msys64',
    [string]$ValidationLayerDirectory = '',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\.release-work\foveation-tests')
)
$ErrorActionPreference = 'Stop'
$engine = (Resolve-Path -LiteralPath $EngineSource).Path
if (!(Test-Path -LiteralPath (Join-Path $engine 'build.sh'))) { throw 'Expected a complete applied engine source' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
$include = Join-Path $output 'include'
$headers = Join-Path $include 'video_core/host_shaders'
New-Item -ItemType Directory -Path $headers -Force | Out-Null
$savedPath = $env:PATH
$savedLayerPath = $env:VK_LAYER_PATH
$savedValidation = $env:BB_TEST_VALIDATION
$savedDisabledLayers = $env:VK_LOADER_LAYERS_DISABLE
try {
    $env:PATH = (Join-Path $MsysRoot 'clang64/bin') + ';' + $env:PATH
    if ($ValidationLayerDirectory) {
        $layer = (Resolve-Path -LiteralPath $ValidationLayerDirectory).Path
        $env:VK_LAYER_PATH = $layer
        $env:PATH = $layer + ';' + $env:PATH
        $env:BB_TEST_VALIDATION = '1'
        $env:VK_LOADER_LAYERS_DISABLE = '~implicit~'
    }
    $shaders = Join-Path $engine 'gpu/shadps4/video_core/host_shaders'
    foreach ($name in @('dlss_nr','dlss_nr_scaled','dlss_nr_look','dlss_nr_foveated',
        'hdr_native_exposure','hdr_native_gain','hdr_native_luma','hdr_native_snapshot','hdr_native_stats')) {
        $sourceName = $name
        [string[]]$defines = @()
        if ($name.StartsWith('hdr_native_')) {
            $sourceName = 'hdr_native'
            $stage = $name.Substring('hdr_native_'.Length).ToUpperInvariant()
            $defines = if ($stage -eq 'SNAPSHOT') { @('-DSTATS=1','-DSNAPSHOT=1') } else { @('-D' + $stage + '=1') }
        }
        & glslangValidator.exe --target-env vulkan1.3 --vn ($name.ToUpperInvariant() + '_COMP') `
            @defines -o (Join-Path $headers ($name + '_comp.h')) (Join-Path $shaders ($sourceName + '.comp'))
        if ($LASTEXITCODE) { throw "Shader compile failed: $name" }
    }
    & clang++.exe -std=c++23 -O2 -UNDEBUG -Wall -Wextra -Werror `
        (Join-Path $engine 'tests/test_nr_foveation.cpp') -o (Join-Path $output 'nr-foveation-test.exe')
    if ($LASTEXITCODE) { throw 'CPU test compile failed' }
    & (Join-Path $output 'nr-foveation-test.exe')
    if ($LASTEXITCODE) { throw 'CPU coordinate test failed' }
    & clang++.exe -std=c++23 -O1 -UNDEBUG -Wall -Werror -I $include `
        (Join-Path $engine 'tests/test_hdr_native.cpp') -lvulkan-1 -o (Join-Path $output 'nr-foveation-shader-test.exe')
    if ($LASTEXITCODE) { throw 'Vulkan fixture compile failed' }
    & (Join-Path $output 'nr-foveation-shader-test.exe') --nr
    if ($LASTEXITCODE) { throw 'Vulkan NR fixture failed' }
    Write-Output 'PASS: CPU coordinates and synthetic Vulkan shader checks. Model inference, validation-layer cleanliness and gameplay performance require separate checks.'
} finally {
    $env:PATH = $savedPath
    $env:VK_LAYER_PATH = $savedLayerPath
    $env:BB_TEST_VALIDATION = $savedValidation
    $env:VK_LOADER_LAYERS_DISABLE = $savedDisabledLayers
}
