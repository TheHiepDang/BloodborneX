$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release-file-policy.ps1')
foreach ($path in @('AGENTS.md','engine-source/agents.md','BloodbornePC-source/BloodborneX/AGENTS.md',
    'agents_space/diagnostic.json','.agents/settings.json','.codex/settings.json',
    '.release-work/private.txt','.release-settings.json','.env','.env.local',
    'docs/NR-1.0.3-RC4-TEST.md','docs/NR-CUSTOM-PRESETS-BUILD-VALIDATION.md',
    'docs/NR-EXTRA-STYLE-ANALYSIS.md','BloodborneX-RenoDX-NR-analysis.md',
    'tools/Shadow Capture.cmd','tools/shadow-capture.ps1','tools/nr-style-test-default.ini',
    'user/presets/mine.bbxpreset')) {
    if (!(Test-PrivateReleasePath $path)) { throw "Private path accepted: $path" }
}
foreach ($path in @('README.md','engine/README.md','docs/NR-1.0.3.md','docs/NR-CONTROLS.md',
    'docs/RELEASING.md','engine/nr-look-controls.patch','engine/nr-look-controls-baseline.json',
    'tools/spec-preset-rtx3070-slop.ini','tools/release-file-policy.ps1',
    'engine-source/tests/test_settings.cpp')) {
    if (Test-PrivateReleasePath $path) { throw "Public path rejected: $path" }
}
Write-Output 'PASS: private source/archive paths rejected; public release documentation and source retained.'
