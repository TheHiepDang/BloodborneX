# Publishing BloodborneX NR releases

1. Choose a unique numeric version, for example `1.0.1`. Build the launcher with that embedded version; changing a ZIP filename or GitHub title does not change the executable.
2. Run `tools/build-nr-release.ps1` with the tested applied `-EngineSource`, `-Version 1.0.1`, and `-ReleaseNotesFile NR-1.0.1.md`. The builder refuses to overwrite existing release folders. Cached downloads must retain their pinned hashes.
3. Verify archive integrity/manifests, engine fingerprint and feature sources, launcher version, metadata, clean portable startup, and updater selection. Preserve the proprietary NR model exclusion.
4. Create GitHub tag and release `v1.0.1`. Use a stable release for a stable version; NR candidate tags use `vX.Y.Z-nr.rcN` and prerelease status. An arbitrary tag such as `NR-Performance` cannot be parsed by existing launchers.
5. Attach `LanternEngine-1.0.1-win64.zip`, `LanternEngine-1.0.1-source.zip`, **`package.json`**, `SHA256SUMS.txt` and validation. Do not add a second `.zip` extension. The Windows filename must end in `-win64.zip`.
6. The separately attached metadata must match the ZIP metadata exactly: `version=1.0.1`, `engine=lantern-native-nr-1.0.1`, `update_family=bloodbornex-native-nr-v1`. A package.json only inside the ZIP is not enough.
7. Verify the published GitHub asset checksums and run the old RC2 updater's discovery path against the live release. It must select the full Windows ZIP, accept metadata, and report it as newer. Verify stable and prerelease channels.

Keep an applied source ZIP beside the portable package when the working source contains uncommitted changes; BUILD-PROVENANCE.json records that fact. Do not claim GitHub's automatically generated source tarball contains those uncommitted changes.
