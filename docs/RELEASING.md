# Preparing and publishing BloodborneX NR releases

## Normal use

Double-click `Prepare Release.cmd` at the project root. Enter the new version, for example `1.0.3`, and a short description. On the first run, select a complete applied engine source folder containing `build.sh`; that location is saved in the ignored `.release-settings.json`. A game installation and the root `engine/` folder containing patches are not source checkouts. The shortcut detects installed PowerShell 7.4+ or the runtime remembered by an earlier successful run.

The script always runs native compilation, launcher compilation and checks. It refuses an existing release directory, incomplete source, a mismatched native build cache, an occupied temporary build drive, or the known undeployed live-colour prototype. It never silently substitutes a binary from an older release. Native cache reuse is incremental; source changes are compiled by the normal build system.

The finished files are in `releases/<version>/Upload`. Upload **every file from that folder together**. `UPLOAD-INSTRUCTIONS.txt` beside it records the exact tag and release type. The script does not upload, create a tag, push code, modify an installed game or change user settings.

Requirements: Windows, PowerShell 7.4+, .NET SDK 8+, Git, and the existing MSYS2 CLANG64 engine build dependencies. It uses a temporary N: alias for native build paths and removes only that alias when done. `-BuildDrive` can select another unused drive; an existing native CMake cache must match the selected alias and source folder.

## Command-line use

```powershell
& '.\Prepare Release.ps1' -Version 1.0.2 -EngineSource 'C:\path\to\engine' -NoPrompt
& '.\Prepare Release.ps1' -Version 1.0.2 -EngineSource 'C:\path\to\engine' -CheckOnly -NoPrompt
& '.\Prepare Release.ps1' -Version 1.0.3 -ArtifactPrefix BloodbornePC -EngineSource 'C:\path\to\engine' -NoPrompt
```

Optional arguments include `-ReleaseNotesPath`, `-MsysRoot`, `-CachedDownloads`, `-ReleaseRoot`, `-BuildDrive` and `-Jobs`. `-CheckOnly` checks inputs/tools without compiling or writing configuration. A supplied notes file is used verbatim. Otherwise a short versioned note is generated. Downloads are reused from earlier release caches when available; pinned Python/DLSS SR hashes are still checked.

The default archive prefix is `BloodbornePC` starting with v1.0.3. Both archives and their root folders use this prefix. Existing v1.0.1/v1.0.2 clients already recognize it. Keep `Lantern Engine.exe` (the full launcher) and `Bloodborne Launcher.exe` (a compatibility forwarder) inside the package; `BloodbornePC.exe` is the new public entry forwarder. Do not remove the legacy internal executable names, change `lantern.ini`/per-user storage paths, or replace the `lantern-native-nr-` protocol ID as a cosmetic rename.

The updater accepts `BloodbornePC`, `LanternEngine` and `BloodborneX`; it prefers `BloodborneX` if present. However, published pre-1.0.3 clients do **not** recognize `BloodborneX`. Do not switch the distribution to BloodborneX-only while promising automatic updates to those clients. The builder's optional BloodborneX mode emits an identical legacy compatibility ZIP and verifies its hash, but v1.0.3 intentionally uses BloodbornePC with exactly two archives. Never rename an old archive as a new release.

The lower-level `tools/build-nr-release.ps1` packages already-built outputs. Prefer the root script for routine releases because it compiles first and validates the result. `tools/verify-nr-release.ps1` checks embedded version, exact external/internal metadata, fresh-engine hash, both archive checksums, every archived file hash, full manifest coverage and prohibited files. `tools/test-nr-release.ps1` tests portable startup/imports/render-size preparation and native/GPU capability loading without launching the game. Validation reports describe only checks actually performed.

## GitHub checklist

1. Use the exact tag and title `v<version>`. Stable versions are stable/latest; `X.Y.Z-nr.rcN` versions are prereleases.
2. Upload all files from the new `Upload` directory, including the separate `package.json`. The Windows ZIP must end in `-win64.zip`, with one `.zip` extension.
3. Do not rename an older ZIP to make a new release: the launcher inside still has its old embedded version. The builder rejects version mismatches.
4. Keep the source ZIP beside the Windows ZIP and checksums. It contains the complete applied engine source and current launcher/build scripts; do not reapply the historical cumulative patch on top of that snapshot.
5. After publication, verify the GitHub asset hashes against SHA256SUMS.txt and test update discovery from an older NR launcher. The same or newer installed version should not offer this release as an upgrade.

Automatic gameplay/visual validation and GitHub publication are outside this script. The user supplies the appropriate model at `dlss/nvngx_dlssnr.dll`; the model is never packaged or downloaded.

## Command-line publication

Use command-line tools only for publication. First complete the local checks and review/commit the intended public release source and engine supplement, then push that commit to main. Keep AGENTS.md, agent research, local validation logs, settings and secrets private. The source collector and archive verifier independently reject private paths. Run `tools/test-release-file-policy.ps1` when changing that policy. Stop if authentication or networking fails; do not substitute browser authentication or publication.

With the release commit pushed, run from the repository root:

```powershell
gh auth status
git fetch origin main
$releaseCommit = git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve release commit' }
$remoteCommit = git rev-parse origin/main
if ($releaseCommit -ne $remoteCommit) { throw 'Push the reviewed release commit first' }
gh release create "v$version" --repo TheHiepDang/BloodborneX --target $releaseCommit --title "v$version" --notes-file "releases/$version/Upload/RELEASE-NOTES.md" --draft
if ($LASTEXITCODE -ne 0) { throw 'Release creation failed; inspect remote state before retrying' }
gh release upload "v$version" "releases/$version/Upload/*" --repo TheHiepDang/BloodborneX
if ($LASTEXITCODE -ne 0) { throw 'Asset upload failed; leave release as draft' }
gh release view "v$version" --repo TheHiepDang/BloodborneX --json assets,isDraft,targetCommitish
```

Set `$version` to the new version before running these commands. Before publishing, compare all seven uploaded asset names, sizes and GitHub SHA-256 digests with the verified local files. Download and compare bytes if digests are unavailable. Then publish with `gh release edit "v$version" --repo TheHiepDang/BloodborneX --draft=false --latest` and verify `/releases/latest`, asset digests and updater discovery again. An existing tag/release requires inspecting its target and assets before any retry; do not overwrite it blindly. Do not move an already-published tag to disguise later source changes.
