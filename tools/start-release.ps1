#Requires -Version 5.1
# Bootstrap only: the actual build runs in PowerShell 7.4+.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$forward = $args
$candidates = @()
$command = Get-Command pwsh.exe -ErrorAction SilentlyContinue
if ($command) { $candidates += $command.Source }
$candidates += Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe'
$config = Join-Path $repo '.release-settings.json'
if (Test-Path -LiteralPath $config) {
    $saved = Get-Content -LiteralPath $config -Raw | ConvertFrom-Json
    if ($saved.PSObject.Properties['PowerShellPath']) { $candidates += $saved.PowerShellPath }
}
$runtime = $null
foreach ($candidate in $candidates) {
    if ($candidate -and (Split-Path $candidate -Leaf) -eq 'pwsh.exe' -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        $info = [Diagnostics.FileVersionInfo]::GetVersionInfo($candidate)
        if ($info.FileMajorPart -gt 7 -or ($info.FileMajorPart -eq 7 -and $info.FileMinorPart -ge 4)) { $runtime = $candidate; break }
    }
}
if (!$runtime) { throw 'PowerShell 7.4 or newer is required. Install it, or run Prepare Release.ps1 once from an existing PowerShell 7.4+ session so its location is remembered.' }
& $runtime -NoLogo -NoProfile -File (Join-Path $repo 'Prepare Release.ps1') @forward
exit $LASTEXITCODE
