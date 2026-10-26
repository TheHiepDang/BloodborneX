# Preparing and publishing BloodborneX NR releases

## Normal use

Double-click `Prepare Release.cmd` at the project root. Enter the new version, for example `1.0.2`, and a short description. On the first run, select a complete applied engine source folder containing `build.sh`; that location is saved in the ignored `.release-settings.json`. A game installation and the root `engine/` folder containing patches are not source checkouts. The shortcut detects installed PowerShell 7.4+ or the runtime remembered by an earlier successful run.

The script always runs native compilation, launcher compilation and checks. It refuses an existing release directory, incomplete source, a mismatched native build cache, an occupied temporary build drive, or the known undeployed live-colour prototype. It never silently substitutes a binary from an older release. Native cache reuse is incremental; source changes are compiled by the normal build system.

The finished files are in `releases/<version>/Upload`. Upload **every file from that folder together**. `UPLOAD-INSTRUCTIONS.txt` beside it records the exact tag and release type. The script does not upload, create a tag, push code, modify an installed game or change user settings.

The DLSS-G HDR prerelease and the immediately following release keep the legacy
`LanternEngine-` archive prefix so installed 1.0.1 launchers can discover them.
Their launcher recognizes both that prefix and `BloodborneX-`. For the later
branding switch, build with `-ArtifactPrefix BloodborneX`. An older launcher that
skips both transition releases may need a manual update after the switch. The
legacy filename does not make this fork an official upstream release; keep the
upstream NOTICE, exact attribution and changed-version statement visible.

The builder downloads NVIDIA's pinned Streamline 2.14.16 SDK, verifies its archive
hash, and packages the seven matched production DLLs with their licence texts and
per-file manifest. DLSS Frame Generation needs no separate installation after a
normal package update. `Install Streamline.cmd` remains as a repair helper. The
package verifier checks the runtime set. The Discord release notification workflow
has been removed from this fork.

Requirements: Windows, PowerShell 7.4+, .NET SDK 8+, Git, and the existing MSYS2 CLANG64 engine build dependencies. It uses a temporary N: alias for native build paths and removes only that alias when done. `-BuildDrive` can select another unused drive; an existing native CMake cache must match the selected alias and source folder.

## Command-line use

```powershell
& '.\Prepare Release.ps1' -Version 1.0.2 -EngineSource 'C:\path\to\engine' -NoPrompt
& '.\Prepare Release.ps1' -Version 1.0.2 -EngineSource 'C:\path\to\engine' -CheckOnly -NoPrompt
```

Optional arguments include `-ReleaseNotesPath`, `-MsysRoot`, `-CachedDownloads`, `-ArtifactPrefix`, `-ReleaseRoot`, `-BuildDrive` and `-Jobs`. `-CheckOnly` checks inputs/tools without compiling or writing configuration. A supplied notes file is used verbatim. Otherwise a short versioned note is generated. Downloads are reused from earlier release caches when available; pinned Python/DLSS SR hashes are still checked.

The lower-level `tools/build-nr-release.ps1` packages already-built outputs. Prefer the root script for routine releases because it compiles first and validates the result. `tools/verify-nr-release.ps1` checks embedded version, exact external/internal metadata, fresh-engine hash, both archive checksums, every archived file hash, full manifest coverage and prohibited files. `tools/test-nr-release.ps1` tests portable startup/imports/render-size preparation and native/GPU capability loading without launching the game. Validation reports describe only checks actually performed.

## GitHub checklist

1. Use the exact tag and title `v<version>`. Stable versions are stable/latest; `X.Y.Z-nr.rcN` versions are prereleases.
2. Upload all files from the new `Upload` directory, including the separate `package.json`. The Windows ZIP must end in `-win64.zip`, with one `.zip` extension.
3. Do not rename an older ZIP to make a new release: the launcher inside still has its old embedded version. The builder rejects version mismatches.
4. Keep the source ZIP beside the Windows ZIP and checksums. It contains the complete applied engine source and current launcher/build scripts; do not reapply the historical cumulative patch on top of that snapshot.
5. After publication, verify the GitHub asset hashes against SHA256SUMS.txt and test update discovery from an older NR launcher. The same or newer installed version should not offer this release as an upgrade.

Automatic gameplay/visual validation and GitHub publication are outside this script. The user supplies the appropriate model at `dlss/nvngx_dlssnr.dll`; the model is never packaged or downloaded.
