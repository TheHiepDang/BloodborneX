# T2-01 Modern Depth of Field

12 November 2026. **CONDITIONAL GO: built candidate for owner acceptance.**
M0 is complete; T2-02 through T2-06 remain unstarted. **STOP after this build.**

## Baseline and sources

Local main `e0f47880f1c88a31b7288ce852d9ce4e88dc90e9` was clean at entry.
The full applied source is `.release-work/nr-controls-engine`; maintained repo
patches plus that complete source, not an installation or prior candidate's
binaries, are the build inputs. Stable v1.0.3 and earlier candidates are preserved.
See [00-baseline-gap-audit.md](00-baseline-gap-audit.md) for the complete feature
matrix, prior 65-file Tier 1 hash check and known-good rc5 baseline.

Source: [BBFSS beta](https://github.com/fromsoftserve/BBFSS/tree/306221668207a5b38c825ed6a28f66fbefed192a),
exact SHA `306221668207a5b38c825ed6a28f66fbefed192a`. Original bbfss specification:
`42bbde58126cf0e94c6cdaae149ccc0a788e4713`. Beta includes optimization
`184ca51997693e84d7a8c12b245809b945bf44c0` (cached views, fixed descriptor-copy
arrays, compute-chain barriers). No bloom, sky, GGX, probes or RT code imported.
Ported cpp/header/shader/GPU test retain GPL-3.0-or-later notices.

## Controls and behavior

| Control | Default | Application |
| --- | --- | --- |
| Existing Depth of field (DoF) master | Existing default OFF preserved | Existing startup game patch; must be enabled for either DoF implementation |
| Depth of field mode | Original DoF (`graphics_dof_mode=0`) | Modern DoF = 1; requires restart, with native pending-restart indicator |
| DoF intensity | 1.0 (`graphics_dof_intensity`) | 0..3, live in the overlay once Modern mode is active; independent of Picture tuning |

Both WPF/Avalonia launchers and the graphics overlay expose the controls.
Launcher edits configure the next run; use the overlay to change intensity live.
Settings persist and are included in selective custom presets and exports.
Both immutable 73-key RTX 3070 profiles remain unchanged; old INIs/presets retain
Original without needing new keys. Serialization preserves existing key order
relative to one another. The mode cannot switch resources mid-frame.

Original mode does not create modern GPU resources or modify native DoF RGB.
Modern mode also remains inactive with the existing master DoF disabled.
The authored map still chooses its DoF curve/enable state. No GPARAM/game data
or map-name table is added. Intensity scales the calibrated far footprint and
added near radius without changing authored ranges or autofocus distance.
Zero restores sharp RGB on the recognized chain while retaining native alpha;
unsupported/invalid paths keep their native result.

## Architecture and ordering

The narrow after-draw bridge captures the inverse projection from shadow resolve
0xf55643de; snapshots depth at alpha encoding 0xfbe7c4b6; captures seven-tap kernel
weights/offsets from PS e80526cc + VS 7e956bc3; composites only PS 9e1cb278 + VS
d3ca03f3, one color target/layer, matching source UID, sharp/blur guest addresses,
valid buffer offsets/ranges and matching target extents.

The map's YEBIS far curve is encoded in sharp HDR alpha. GPU kernel covariance
calibrates a 48-sample bokeh disk. Bilateral half-size preparation/gather,
full-size edge repair and reconstruction replace the supported native RGB.
The original passes still execute, providing a fallback; this does not blur the
already blurred output again. Native output alpha remains intact and HDR RGB is
not tone mapped or clipped to SDR by the new pass.

Foreground autofocus meters an upper-center 7x7 depth grid on the GPU, takes a
bounded median, smooths inverse focus over time and resets on first use, size,
projection change or a long gap. Near blur uses conservative coverage and a
central player-framing safety region; these are heuristics, not segmentation.
There is no accumulated color history or CPU depth readback.

The effect stays at native YEBIS recombination before original bloom/tone mapping.
Tier 1 scene lighting/volumes, NR before SR/DLAA at the existing temporal trigger,
reactive/multipass histories and HDR gain-map/UI/presentation hooks are not
relocated or rewritten. The existing draw stream decides its exact relation to
the upscaler in each rendering mode; live command-order/scene integration is
part of acceptance, not established by a synthetic shader test.

Current SceneTargets proxies supply internal dimensions; native/output-size
paths use their actual bound HDR extents. Odd sizes are supported. Multi-layer,
multisampled, incompatible formats, aliasing or mismatched chains retain stock.

Nine shader variants compile through existing host-shader CMake targets. Modern
pipeline/state initialization is lazy at the first eligible camera draw, uses
format checks and soft Vulkan creation failure. Original/master-off allocate
no modern images. The 48-byte camera/kernel/focus state and images are reused;
view cache keys include image handle, UID and format. Resized images and evicted
views retire through Scheduler::DeferOperation. Fixed-size captured descriptors
outlive their stack inputs. Full barriers delimit chains; narrower compute
barriers link dependent passes. No new GPU idle wait/Finish is added.

A video-memory headroom guard precedes image resizing. Tile allocation changes
only on extent/radius tile-size changes, not every dispatch. Cached memory stays
until resize/shutdown. No new shader-compiler/cache schema, game SPIR-V rewrite
or module substitution is introduced, so existing cache compatibility is retained.
First use can hitch while compute pipelines are created.

## Maintained source and reproducibility

`engine/tier2-modern-dof.patch` plus `tier2-modern-dof-baseline.json` cover these
13 engine files; the hash manifest lists exact preimage and resulting bytes:

- `gpu/CMakeLists.txt`
- `gpu/shadps4/video_core/host_shaders/depth_of_field.comp`
- `gpu/shadps4/video_core/renderer_vulkan/vk_depth_of_field.cpp`
- `gpu/shadps4/video_core/renderer_vulkan/vk_depth_of_field.h`
- `gpu/shadps4/video_core/renderer_vulkan/vk_effect_pass.h`
- `gpu/shadps4/video_core/renderer_vulkan/vk_rasterizer.cpp`
- `gpu/shadps4/video_core/renderer_vulkan/vk_rasterizer.h`
- `gpu/shadps4/video_core/renderer_vulkan/vk_tier1_effects.inc`
- `gpu/shim/bbport_overlay.cpp`
- `gpu/shim/bbport_settings.cpp`
- `gpu/shim/bbport_settings.h`
- `tests/test_depth_of_field.cpp`
- `tests/test_settings.cpp`

Other changed maintained files: both launcher Views/GraphicsPage.cs,
engine/README.md, .gitignore (track Tier 2 docs), tools/apply-tier2-modern-dof.py,
tools/build-nr-release.ps1 (add ordered Tier 1/Tier 2 patch hashes to build
provenance only), and docs/tier2-graphics/{README,00-baseline-gap-audit,01-modern-dof}.md.
Updater code, family/engine identifiers, compatibility filenames and settings
template are unchanged.

After the five Tier 1 supplements, run:

```powershell
python tools/apply-tier2-modern-dof.py C:\path\to\full-engine --check
python tools/apply-tier2-modern-dof.py C:\path\to\full-engine
```

Helper stages LF-normalized files, checks every preimage, replays the patch and
verifies all output hashes before writing. Check-only replay, actual application
and already-applied checks passed in a fresh preimage fixture; the current source
also matches. This requires no Git refs. Complete source packages already have
the change applied; do not reapply older patches. Use normal build.sh, or the
existing configured CLANG64 Ninja tree and existing local native link helper.

## Automated validation

| Area | Result | Evidence and limits |
| --- | --- | --- |
| Engine and new shaders | PASS | bbgpu + nine variants compiled; fresh bb-probe linked. Initial upstream EffectDof enum mismatch was adapted to the existing master key/startup snapshot; final build passed. |
| Both launchers | PASS | WPF and Avalonia Release builds; existing PadMap/unawaited-call and offline NuGet-audit warnings remain. |
| Modern DoF GPU | PASS | Actual nine shader variants on RTX 3070: calibrated radius, odd extents, HDR/alpha, zero strength, focus/near fallback, occlusion, foliage repair, invalid camera/kernel/layout, autofocus median/time convergence/sky, near-only blur and close-up protection. |
| Settings/presets | PASS | Original/mode/intensity defaults, persistence, startup snapshot, live requested intensity, slider default, custom preset apply/reject; existing 73-key profiles, CRUD/export/import and preset UI checks. |
| Tier 1/live | PASS | 8,192 CPU state transitions; regular/HQ GTAO GPU; 19 existing effect pipelines; point shadows/fog/tint; directional PCF/PCSS/cascade; native map hooks; motion history, UI composition and occlusion. |
| NR preservation | PASS | Nine NR implementation/shader/forwarder/immutable-profile files still match recorded hashes. Base/scaled/look GPU and extra-style/owned-runtime three-pass lifecycle checks passed. No proprietary inference claim. |
| Other regressions | PASS | Upscaler support, 37 Python patch tests (3 skips), five SSR tests; UpdateChecks, NrRuntimeChecks, NrOptimizationChecks, DisplayChecks and LocalizationChecks. |
| GPU lifetime/sync | REVIEWED / NOT FULLY TESTED | UID guards, deferred retirement and captured descriptor values reviewed; actual shader GPU tests ran. Khronos validation layer unavailable. Live renderer orchestration and repeated resizing need gameplay. |
| Portable/package | See output records | Existing archive/hash/privacy/source-manifest and restricted-PATH startup/capability checks recorded in PACKAGE-CHECKS.json and VALIDATION.md. |

Test commands include `ninja -C out/gpu bbgpu depth-of-field-shader-test settings-test preset-ui-test`,
their corresponding executables, `hdr-native-shader-test --nr`, the NR style
forwarder fixture, native Tier 1 tests and existing dotnet check projects.
Local detailed Tier 1 regression outputs are under
`.release-work/tier2-dof-test-output`; package provenance records source/output hashes.

## Local candidate and run notes

Version **1.0.4-nr.rc6**, T2-01 test candidate containing all prior Tier 1/live work.
Existing packaging system, fresh source outputs and clean default configuration:

`C:\Projects\Bloodborne Engine\BloodborneX\releases\1.0.4-nr.rc6\BloodbornePC\BloodbornePC.exe`

Full applied source is alongside it in BloodbornePC-source/engine-source. The
Windows/source ZIPs, checksums and separate local metadata remain in the same
version folder. No model, game dump, personal INI, credentials, saves or mods are
included. Optional Streamline/FSR4 assets remain omitted exactly as in the prior
packaging workflow; this build does not claim to bundle them.

Run from this separate folder, point it to your existing dump without moving or
modifying the dump, and use a separate save directory. Supply your NR DLL yourself
only if you test NR. Do not copy over stable v1.0.3 or an installed game.

## Owner acceptance: OFF versus ON

1. Enable the existing master **Depth of field (DoF)**. Choose **Original DoF**,
   restart, and compare a fixed view with the accepted rc5 build using identical
   other settings. Original should match the previous authored YEBIS result.
2. Choose **Modern DoF**, restart, confirm the recognized recombination log and
   compare the same view at intensity 1. A pipeline-ready log alone is not proof
   the game used the supported path. Unsupported passes should stay original.
3. Move intensity 0 -> .5 -> 1 -> 3 -> 0 in the overlay, without restarting.
   Check sharp RGB at zero, controlled far bokeh/near blur and unchanged HUD/alpha.
4. Compare Central Yharnam long streets, Hunter's Dream, a lamp/fog scene, a dark
   interior, foreground leaves/rails, sky/loading, menus and a cutscene. Move the
   camera and frame the hunter close/off-center; look for halos, trails and focus
   pumping. The central safety region can also protect nearby scenery.
5. Repeat NR OFF/ON with single and multipass; native/DLAA and DLSS Balanced or
   Performance. Check foreground edges during motion and internal/output size
   changes. Repeat FSR, FG enable/disable and HDR/SDR where supported.
6. Save/export/apply a custom preset containing both DoF keys; restart to verify
   persistence and truthful mode/master pending-restart indicators. Exercise the
   five Tier 1 live toggles again. Watch VRAM during repeated intensity/size changes.
7. Measure the same fixed scene (uncapped) with Original, Modern 0 and Modern 1,
   then 3. Record FPS/frame time and VRAM, GPU/driver, NR/pass count and upscaler.

| Fixed-scene configuration | FPS / frame time / VRAM |
| --- | --- |
| Original, NR OFF and ON | NOT MEASURED; owner captures |
| Modern intensity 0, NR OFF and ON | NOT MEASURED; owner captures |
| Modern intensity 1 and 3, same NR/upscaler | NOT MEASURED; owner captures |

No screenshots or game-performance values were captured here. GPU tests verify
controlled synthetic filtering; they do not establish map appearance or cost.

**NOT TESTED:** Actual gameplay, Original visual equality, rendered live slider
response in game, proprietary NR inference/NR+DoF single/multipass gameplay,
FG/HDR presentation, uncommon DoF chains/maps/cutscenes, extended memory/sync
stability and scene-matched FPS/VRAM. No gameplay acceptance is claimed.

**STOP:** Await owner acceptance of T2-01. T2-02 and all later milestones remain
unstarted. No commits, pushes, tags, releases or updater publication.

Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine).
BloodborneX is a changed, unofficial fork; third-party notices retained.
