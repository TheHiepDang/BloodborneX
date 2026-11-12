# Live-toggle validation checkpoint

12 November 2026. Existing main HEAD
5710aeb4626cc5f86ba0730b9afc3e1de5c4442d, with all inherited G01-G13 work
preserved. No new branch/worktree, staging, commit, push or publication.
Stable v1.0.3 and earlier candidates remain untouched.

## Executed checks

| Check | Result and scope |
| --- | --- |
| Native build | Existing CLANG64 CMake/Ninja bbgpu compiled with live effect flags, SSGI/SSR history invalidation and frame-generator reset request; fresh bb-probe.exe linked. |
| CPU transition state | tier1-live-test passed 8,192 mixed flag transitions, stable scene snapshots and no-op changes. This checks the state used by dispatch, not actual in-game GPU switching. |
| Launchers | WPF and Avalonia Release builds passed; existing PadMap/unawaited-call and offline NuGet audit warnings remain. |
| Persistence/presets | Native settings save/load, five existing key/default/startup snapshots, point strength, both immutable 73-key profiles, selective custom presets and headless preset UI passed. |
| GPU effects | RTX 3070: point-shadow two-target attenuation and off/sky/no-delta/no-light/floor/tile checks passed; point-fog reference error .614% over 1,881 values; sunlight tint and directional shadow PCF/PCSS/cascade checks passed. No shaders changed in this batch. |
| GPU pipelines | All 19 cumulative effect pipelines created. Khronos validation layer unavailable; zero messenger warnings is not full Vulkan validation. |
| NR GPU | Base/scaled/look shaders passed LDR/HDR identity, alpha, depth, motion, history rejection/reset and finite-output cases. |
| NR style/multipass | Extra-style model checks and real forwarder using an owned test-runtime passed live 0/+64/-64/+16/0, three-pass lifecycle gate and baseline restore with one NGX initialization. No proprietary model inference was exercised. |
| Other native | Motion history, UI composition and occlusion model checks passed. |
| Application | UpdateChecks, NrRuntimeChecks, NrOptimizationChecks, DisplayChecks and LocalizationChecks passed. |
| Maintainable source | 19-file engine supplement with per-file preimage/output hashes. Helper passed check-only replay, application, already-applied detection and current source verification. |
| NR preservation | Nine NR implementation, shader, forwarder and immutable-profile source files still match their recorded NR-controls hashes. Existing flag-only ResetHistory API is called; those source files are unchanged. |

Source review confirmed all five startup gates were removed from renderer effect
paths; startup-gated shader/native-hook controls remain unchanged. The overlay
restart predicate excludes the five live flags. Runtime flag sampling happens
on the recording owner at display boundaries; no UI-side Vulkan mutation,
resource teardown, shader-cache purge or GPU idle wait was added.

Transitions invalidate SSGI/SSR histories, reset NR and downstream reconstruction,
and mark the next frame-generation job as reset/not presented. These API calls
compiled; full live orchestration, driver synchronization and visual transition
quality require gameplay acceptance. Resources remain cached after use while
off, so a VRAM decrease on every disable is not expected.

## Candidate

Version 1.0.4-nr.rc5, cumulative G01-G13 plus five live switches, packaged with the
existing build-nr-release.ps1, clean defaults and cached build dependencies.
Normal BloodbornePC naming and updater identity retained. Full applied engine
source is included; no proprietary model, game data or user configuration.

Entry:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\tier1-live-build\1.0.4-nr.rc5\BloodbornePC\BloodbornePC.exe`

Archive/privacy/source-hash and non-game portable startup results are recorded
by the output directory's PACKAGE-CHECKS.json and VALIDATION.md. Refer to
LIVE-TOGGLES.md for the exact live/restart matrix and acceptance checklist.

No gameplay was launched. NR/FG/HDR coexistence, repeated live toggling, memory
stability, resizing/menus/transitions and visible effects await the user test.
