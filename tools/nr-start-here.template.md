# BloodborneX {{VERSION}} — BloodbornePC package

Windows x64 {{RELEASE_KIND}}, built {{BUILD_DATE}}.

1. Extract the entire `{{ARTIFACT_PREFIX}}-{{VERSION}}-win64.zip` to a writable folder.
2. Run **BloodbornePC.exe**, select your own decrypted Bloodborne 1.09 dump and existing saves location, then Play. The package contains no game files or saves.
3. NR is optional and off by default. To use it, supply one model at **dlss/nvngx_dlssnr.dll**, choose DLSS SR/DLAA and enable **NR**. The model is never bundled or downloaded. Hashes identify builds for diagnostics; custom runtimes are always allowed. Actual exports, initialization, feature creation and evaluation determine availability. Failure disables NR while ordinary DLSS remains available.
4. Open the in-game graphics menu with **Insert / L3+R3**. NR has its own tab; **Spec preset** is at the right. It contains two provided RTX 3070 profiles and a separate custom library with selective save/export/import. Applying a preset asks for confirmation and offers a restart.

## NR controls

One to three independently historied model passes run before DLSS SR/DLAA. Extra passes cost GPU time. Input height controls scene/SR size; **Performance** separately reduces model resolution to 0.75 scale. Native AA/DLAA retains output-size input. Automatic mask is on by default and white point is 0.125.

**Composition: Log transfer** enables the native look, detail, colour, limits and stabilization adjustments. Legacy remains available. **Extra neural style** applies live after a brief settle interval: GPU work drains, all features release, coefficients change in memory, and features/history rebuild. Zero restores stock coefficients and the saved preset; nonzero uses Old/HT. The tensor layout is validated. The model file on disk is never changed. See the source repository's `docs/NR-RELEASE.md` for controls.

Custom preset files are versioned `.bbxpreset` text. Graphics options start selected; nongraphical options start excluded. Save/export uses a highlighted, read-only copy of the settings controls with a fixed header. Click rows/groups to include/exclude them. Imported settings are validated before any application; excluded settings retain their current values. Custom presets are stored under `user/presets` beside the active config and are not part of public archives.

## Updates and compatibility

BloodborneX is the project name; **BloodbornePC** is the distribution name. Published v1.0.1/v1.0.2 clients already recognize BloodbornePC archives. The full **Lantern Engine.exe** and legacy **Bloodborne Launcher.exe** forwarder remain inside the folder for old updater discovery, replacement/restart and shortcuts. Keep them. The new public entry is BloodbornePC.exe.

The updater uses **TheHiepDang/BloodborneX** and compatible full Windows packages with separately attached matching `package.json`, `update_family: bloodbornex-native-nr-v1` and `lantern-native-nr-` engine IDs. These identifiers and existing settings/storage paths remain stable. A source ZIP or a launcher-only file is not a complete update. Packaging locally does not itself publish a release.

## Runtime and limits

Python, .NET and DLSS SR dependencies are packaged. The current main-branch package does not bundle optional Streamline/DLSS-G or FSR 4/4.1.1 assets; those modes require their external runtimes. No separate SDK, Python or MSYS2 installation is needed to play. A suitable Vulkan GPU driver is required. DLSS frame generation remains experimental and requires compatible RTX 40/50 hardware, driver/HAGS support and restart; FSR 3.1 FG is also available. HDR/FG/NR coexistence and broad gameplay/long-session coverage remain limited. Ordinary DLSS/FSR fallback paths remain available.

No unofficial RTX 20/30 DLSS FG proxy is integrated. The NR-aware Python starter is intentional; do not add an old `bb-start.exe` whose render-size preparation differs. Build and synthetic tests do not certify visual effect or gameplay stability. Consult `VALIDATION.md` and `PACKAGE-CHECKS.json` beside the archives for exact checks.

See `docs/NR-{{VERSION}}.md` for release notes. `BUILD-PROVENANCE.json` and `FILES-SHA256.txt` record package inputs. The source ZIP contains a complete already-applied engine snapshot; do not reapply old cumulative patches.

Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine). BloodborneX is a changed, unofficial fork. All bundled licenses/notices remain in `licenses/`.
