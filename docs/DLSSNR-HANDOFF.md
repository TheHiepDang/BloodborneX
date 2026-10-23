# BloodborneX native DLSS Neural Rendering — handoff

Checkpoint: 23 October 2026, local Windows RTX 3070 RC1. The user accepted the
current image and requested consolidation, documentation and a portable package.
Do not reopen color-composition experiments or speculative optimizations without
a new request. NR means neural rendering (feature 18), not DLSS SR or frame gen.

## Authoritative source and release

- Original repository: `C:\Projects\Bloodborne Engine\BloodborneX`.
- Fork/update repository: https://github.com/TheHiepDang/BloodborneX . Local remote
  already points there. Packaging does not commit, push, tag or publish.
- Engine source is maintained as `engine/bbhost-changes.patch`, applied to
  https://github.com/DarkIzuku/bloodborne_pc tag `v0.4.0-darkizuku-preview.1`,
  commit `8ecba9a4abdd54747e31d7fee54982e422b5f8e2`. It is a full cumulative patch,
  including prior Lantern work, not an incremental NR patch.
- `engine/win_compat.h` is copied into the reconstructed engine's `gpu` folder.
- Working/tested engine checkout for this session:
  `C:\Users\hiepd\Documents\Codex\2026-10-22\referenced-chatgpt-conversation-this-is-an\work\bloodborne_pc`.
  This development location is not required by the portable package.
- The release source ZIP includes a filtered snapshot of the original repository,
  the applied engine and its three source submodules (including dirty vendor fixes).
  It excludes Git internals, build outputs, games, user profiles and the NR model.
- Release output: `releases/0.9.9-nr.rc1/`; see `NR-RELEASE.md` for installation.
  Existing test game/profile files are not overwritten or copied into the release.

## Architecture and file map

Lantern/bbport/shadPS4 executes the game's x86-64 CPU code natively and translates
its PS4 GPU commands to Vulkan. It is not a conventional source port of the game.
Paths below are relative to the reconstructed engine, represented in the patch.

| Area | Files |
| --- | --- |
| NR lifecycle/resources/NGX feature 18 | `gpu/shadps4/video_core/renderer_vulkan/vk_dlss_nr.cpp`, declarations in `vk_dlss.h` |
| Ordinary DLSS and shared NGX interface | `gpu/shadps4/video_core/renderer_vulkan/vk_dlss.cpp`, `vk_dlss.h` |
| Pre-SR injection in both scene paths | `gpu/shadps4/video_core/renderer_vulkan/vk_temporal_upscaler.cpp` |
| Color encode/resolve | `gpu/shadps4/video_core/host_shaders/dlss_nr.comp` |
| Vulkan forwarder | `gpu/dlssnr_forwarder.cpp` |
| Settings and overlay | `gpu/shim/bbport_settings.cpp`, `.h`, `bbport_overlay.cpp` |
| Frame generation swapchain fix | `gpu/shadps4/video_core/renderer_vulkan/vk_frame_generation.h` |
| Startup render-size overrides | `scripts/patches.py`, `scripts/run_windows.py` |
| Build/shader generation | `gpu/CMakeLists.txt`, `.github/workflows/windows-offline-build.yml` |
| Detailed engine documentation | `docs/dlss_neural_rendering.md` |

Original-repository launcher changes are in
`launcher/windows/BloodborneLauncher/Core/{Choices,IniFile,DlssNrRuntime,GameEnvironment,Updater}.cs`
and both WPF/Avalonia `Views/*Page.cs`. Shared slider controls carry factory reset
arrows. `tools/build-nr-release.ps1` replaces the stale legacy release helper for
this candidate; it is an allow-list package builder, not a game-folder copier.

Flow: scene -> reactive mask from untouched scene -> encode once -> NR pass 1 ->
NR pass 2/3 when enabled -> compose once -> DLSS SR/DLAA -> game post/UI -> present.
Inputs are real scene depth and RG16F camera/object motion. Images are owned
RGBA16F proxies/outputs; do not expose arbitrary game targets as NGX storage outputs.
Each enabled NR pass has an independent feature handle/history/output. Later passes
consume the preceding raw model output, not the original source and not a repeated
composition. Current resolve blends 75% luminance-only and 25% per-channel ratio,
with bounded ratios and near-black handling. It is conservative, not full RenoDX.

## Critical lessons — do not regress

1. Never call `Scheduler::Finish()` from NR Prepare after the caller obtained its
   active command buffer. It submits/replaces that buffer and caused stale-buffer
   commands, black patches and trails. Wait submitted work, defer a same-tick rebuild,
   and cross a GPU submission boundary between feature creation and evaluation.
2. NR has its own allocated NGX parameter block; do not reuse ordinary DLSS params
   or shut down shared NGX/Streamline state. Serialize create/evaluate/release.
3. Windows parameter ABI setters: unsigned slot 4, float slot 6, pointer slot 0.
   Incorrect setter dispatch appeared to succeed but gave unusable model results.
4. Motion convention is pixel-space previous-minus-current with scale **+1/+1**,
   matching existing SR. The earlier negative-scale experiment was wrong.
5. Keep automatic masking enabled. With no control mask/default skin, mask off gave
   byte-identical model RGB despite evaluation cost in same-frame captures.
6. Skin -1 is an intentional sentinel. Low white point changes proxy encoding;
   the user found 0.125 visibly better and extra passes stronger. Earlier weak
   multipass results at white 4.21 do not mean passes are disconnected.
7. Effective per-pass style must be applied at both feature creation and evaluation
   and included in the feature-rebuild key. Preserve overrides while customization is off.
8. The FSR FG extra-image decision must not depend on transient overload pause state;
   that caused swapchain image-count churn. This does not solve all FG stutter.

## Settings contract

Main keys: `dlss_nr` (off), `dlss_nr_height` (720, 0=Native), `dlss_nr_passes` (1..3,
default 1), `dlss_nr_customize_passes` (off), `dlss_nr_preset` (0), `dlss_nr_style` (0),
`dlss_nr_intensity` (1), `dlss_nr_local_structure` (1), `dlss_nr_local_tone` (1),
`dlss_nr_skin_structure` (-1), `dlss_nr_auto_mask` (1), `dlss_nr_white_point` (0.125).

Extra-pass keys use `dlss_nr_pass2_` / `dlss_nr_pass3_` prefixes with suffixes
`intensity`, `local_structure`, `local_tone`, `skin_structure`, `style`. Numeric
defaults match main defaults; style -1 inherits main, 0/1/2 selects independently.
Styles Standard/Natural/Cinematic are community labels. Presets are undocumented
and may alias. White point/preset/mask stay shared; do not invent per-pass white
point without changing the encode/chain design. White point saves/displays 3 decimals.

Height choices: 480,540,720,900,1080,1200,1440,1600,1800,2160 plus Native; clamp to
output, preserve aspect and align dimensions to four. NR with SR overrides preset
render size and selects the nearest existing DLSS quality mode. DLAA uses output
size and preserves the saved lower choice. NR disabled restores prior normal SR
behavior. Bypass NR for non-DLSS providers and invalid/menu/loading inputs.

## Runtime, licensing and packaging

The user supplies `dlss/nvngx_dlssnr.dll`, x64 unsigned DVS production 310.9.16.0,
276,162,560 bytes, SHA-256
`CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED`.
Never commit, upload, re-sign, patch or redistribute it. Unknown models fail closed;
`BB_DLSS_NR_ALLOW_UNKNOWN=1` is unsupported developer research only.
The open-source `nvngx.dll_dlssnr.dll` caller-gate forwarder is separately built.
It contains no NVIDIA code. Retain GPL notices and RenoDX MIT attribution.

Primary technical reference:
https://github.com/Dagherbou/OptiScaler_DLSSNR/tree/dlss-neural-rendering/OptiScaler/dlssnr .
Pre-SR lifecycle reference:
https://github.com/wilsjo2/OptiScaler-DLSSNR-PreSR-Multipass .
Chiaki's D3D12 worker proved the identical model on this RTX 3070; do not port its
synthetic depth/optical flow or CPU/video reconstruction into this native path.

The launcher now sets `BB_DLSS_DIR` to the same directory used by runtime validation.
Ordinary DLSS still falls back to its executable directory. The package bundles
official DLSS SR, self-contained .NET launcher, embedded Python and native DLL closure.
It intentionally omits native `bb-start.exe`: `start/patches.cpp` lacks Python's NR
render-size override parity. Port that logic and test before enabling the native starter.
Optional Streamline, FSR4/4.1.1 and bbbridge assets are not part of this NR candidate.

Updater points to `TheHiepDang/BloodborneX`. `LOCAL-NR-RELEASE` enables NR compatibility
gating: full win64 ZIP plus attached metadata `update_family=bloodbornex-native-nr-v1`
and `engine=lantern-native-nr-*`. Tags `vX.Y.Z-nr.rcN` sort as prereleases; current NR
builds default to prerelease checks. Source ZIPs cannot become the runtime asset.
No upstream or launcher-only replacement is accepted for a marked NR package.

## Rebuild and validation

See `engine/README.md` for upstream clone, recursive submodules, patch and MSYS2
CLANG64 prerequisites. Copy the compatibility header as documented there. Build
with `BB_JOBS=8 ./build.sh` in CLANG64. The tested optimized build is RelWithDebInfo,
O2, LTO on, PGO off. A short drive path (the session used B:) avoids Windows path limits.

Run `settings-test`, `upscaler-support-test`, `ui-composition-test`,
`motion-history-test`, `hdr-native-shader-test --nr`, and Python
`python -m unittest discover -s tests -p test_patches.py` from the applied engine.
The HDR shader test needs Vulkan/RTX; tests were passing before consolidation.
Both WPF and Avalonia launchers compile. Existing WPF warnings are not new NR errors.

To package from the original repo in PowerShell 7:

```powershell
./tools/build-nr-release.ps1 -EngineSource 'D:\path\to\applied-engine' -Version 0.9.9-nr.rc1
```

The script refuses an existing version folder. It reuses tested engine binaries,
publishes the primary WPF launcher, builds native launcher forwarders, downloads
hash-checked embedded Python and pinned official SR, copies licenses/docs/source,
and emits portable/source ZIPs, manifests and checksums under `releases`. It never
imports local game data or the user's current INI. Check `VALIDATION.md` beside the
archives for the completed package smoke-test result. Public distribution requires
separate dependency/license/security review; local RC completion is not legal certification.

## Acceptance and next work

User confirmed stable 720p play, DLAA, NR toggles and restoration to 720p; custom
pass controls/reset arrows behaved correctly. Same-frame captures proved all three
passes changed raw model output, no nonfinite/alpha corruption, and GPU resolve
matched the CPU reference (MAE about 0.00002548). Details in `NR-RC1.md`.

Do not promise FPS gains: a ~2 minute observation was GPU-limited at ~40–44 FPS,
with intermittent 50–94 ms worst-frame spikes not yet attributed. Next work is
separate scene/NR/SR GPU timings, controlled same-scene A/Bs, longer transition/HDR/
ultrawide tests and FSR FG pacing. Multipass performance cost is real. Standard
DLSS FG cannot be validated on RTX 3070. Initial Streamline loading requires restart.
