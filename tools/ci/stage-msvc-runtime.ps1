# App-local release CRT from the licensed Visual Studio redistributable directory.
# This is a hosted toolchain component, recorded by version/hash, never copied from System32.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Package)
$ErrorActionPreference='Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (!(Test-Path -LiteralPath $vswhere)) { throw 'Visual Studio 2022 redist is required for portable Streamline packaging.' }
$vs = & $vswhere -latest -products '*' -version '[17.0,18.0)' -property installationPath
if ($LASTEXITCODE -or !$vs) { throw 'No licensed Visual Studio 2022 installation with redistributable CRT was found.' }
$base = Join-Path $vs 'VC/Redist/MSVC'
$candidates = @(Get-ChildItem -LiteralPath $base -Directory | ForEach-Object {
    $candidate = Join-Path $_.FullName 'x64/Microsoft.VC143.CRT'
    $file = Join-Path $candidate 'vcruntime140.dll'
    if (Test-Path -LiteralPath $file) {
        [pscustomobject]@{Path=$candidate;Version=[version]([Diagnostics.FileVersionInfo]::GetVersionInfo($file).FileVersion -split ' ')[0]}
    }
} | Sort-Object Version -Descending)
if (!$candidates.Count) { throw 'Visual Studio x64 redistributable CRT directory is unavailable; no System32 fallback is allowed.' }
$crt = $candidates[0]
$target = Join-Path $Package 'engine/out'
New-Item -ItemType Directory -Path $target -Force | Out-Null
$records = [ordered]@{}
$notice = @('Microsoft Visual C++ x64 app-local runtime',
    'Redistributed from the Visual Studio 2022 Microsoft.VC143.CRT redistributable directory.',
    'Microsoft Software License Terms and redistributable list apply:',
    'https://learn.microsoft.com/cpp/windows/redistributing-visual-cpp-files',
    'https://visualstudio.microsoft.com/license-terms/', '')
foreach ($name in @('msvcp140.dll','vcruntime140.dll','vcruntime140_1.dll')) {
    $from = Join-Path $crt.Path $name
    if (!(Test-Path -LiteralPath $from)) { throw "Matched Visual Studio CRT lacks $name" }
    $to = Join-Path $target $name
    Copy-Item -LiteralPath $from -Destination $to
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($to)
    $records[$name] = @{sha256=(Get-FileHash -LiteralPath $to).Hash;version=$version.FileVersion}
    $notice += "$name $($version.FileVersion) - $($version.LegalCopyright)"
}
@{provider='Visual Studio 2022 x64 VC143 release redistributable';version=$crt.Version.ToString();files=$records} |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'msvc-runtime.json') -Encoding utf8
New-Item -ItemType Directory -Path (Join-Path $Package 'licenses') -Force | Out-Null
$notice | Set-Content -LiteralPath (Join-Path $Package 'licenses/Microsoft-VC-runtime.txt') -Encoding utf8
Write-Host "[Package] Staged matched app-local Microsoft CRT $($crt.Version); exact identities recorded."
