# Shared by source collection and archive verification; paths may include archive roots.
function Test-PrivateReleasePath([string]$Path) {
    $normal = $Path.Replace('\','/')
    return $normal -match '(?i)(^|/)(AGENTS\.md|agents_space|\.agents|\.codex|\.release-work|\.release-settings\.json|\.env(?:\.[^/]*)?)(/|$)' -or
        $normal -match '(?i)(^|/)BloodborneX-RenoDX-NR-analysis\.md$' -or
        $normal -match '(?i)(^|/)docs/NR-(?:.*-TEST|.*-BUILD-VALIDATION|EXTRA-STYLE-ANALYSIS)\.md$' -or
        $normal -match '(?i)(^|/)tools/(?:Shadow Capture\.cmd|shadow-capture\.ps1|nr-style-test-default\.ini)$' -or
        $normal -match '(?i)\.bbxpreset$'
}
