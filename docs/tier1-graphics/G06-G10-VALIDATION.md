# G06-G10 validation checkpoint

11 November 2026. Existing main at
`5710aeb4626cc5f86ba0730b9afc3e1de5c4442d`, with prior G01-G05 work preserved.
No commits, pushes, branches/worktrees, publication, installed-game changes or
game launches. Earlier rc1/rc2 and stable v1.0.3 packages remain untouched.

## Verified

| Check | Outcome and scope |
| --- | --- |
| Native compilation | Existing CLANG64 CMake/Ninja engine/library and all added shaders/tests compiled. Existing native helper linked fresh bb-probe.exe. |
| Launcher compilation | WPF and Avalonia Release builds passed. Existing PadMap/unawaited-call and offline NuGet-audit warnings remain. |
| Opt-in hook context | Owned instruction fixture passed preserved incoming RAX and an edited saved XMM0 despite a clobbering callback. Existing engine-hooks-test also passed the unchanged default ABI, seven arguments, XMM preservation, return slot, guards, staged installation and tail-call checks. |
| Native G09/G10 fixtures | Upstream native-local-shadows-test adapted to the independent G09 flag passed point/spot guards, independent refusal, instruction execution, isolated writes, four tiers, softness and 4096/8192 atlas selection. The executed cascade hook produced the expected overlap intervals and preserved native far-plane clamps. No game executable was used. |
| Lamp light model | Current upstream lamp-fog-lights-test passed de-duplication, importance ordering, bounds, camera transformation, fading, warp handling, color tracking and light-budget cases. |
| Point fog GPU | RTX 3070: upstream point-volumetric-test passed two-light, surfaces/sky, misses and zero-intensity cases; largest reported error against its 20,000-step reference was 0.614% across 1,881 values. |
| Sunlight tint GPU | RTX 3070: upstream volumetric-tint-test passed fallback/white-light selection, brightest-light choice, fade, first-frame and steady-color-off cases. Complete directional volume scene composition remains a gameplay check. |
| Effect pipelines | All 18 cumulative compute pipelines created on RTX 3070. Validation layer unavailable: zero messenger warnings is not full Vulkan validation coverage. |
| Directional shadow GPU | Existing atlas/PCF/PCSS/cascade/sky/fade/offset/thin-caster/frame-stability tests passed. These synthetic cases do not certify live scene correctness. |
| Settings/presets | Native settings tests passed new defaults, save/load/startup snapshots, quality selections and strengths. Both immutable 73-key profiles, custom presets and headless preset UI tests passed. |
| Earlier effects | Regular GTAO GPU and HQ GTAO output/denoise/proxy/edge cases passed. |
| NR regressions | Base/scaled/look NR GPU tests passed LDR/HDR/alpha/depth/motion/history cases. NR extra-style and owned-runtime multipass lifecycle tests passed. NR implementation/shader/model/profile sources were not edited by this batch. |
| Launcher regressions | UpdateChecks, NrRuntimeChecks, NrOptimizationChecks, DisplayChecks and LocalizationChecks passed. |

The source change is kept in the maintained atmosphere/shadows supplement and
per-file preimage/output hash manifest. The helper stages the patch and validates
all results before writing, without creating Git refs. The original picture hook
implementation stays unchanged; the new shadow hooks are in shadow_controls.cpp.

## Candidate and acceptance

Local test version: `1.0.4-nr.rc3`, prefix BloodbornePC, output root
`.release-work/g06-g10-build`. It uses the existing packaging system, current
source/build outputs and cached dependencies. User settings, game data, saves and
the proprietary NR model are not inputs.

Entry:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\g06-g10-build\1.0.4-nr.rc3\BloodbornePC\BloodbornePC.exe`

Archive/hash/privacy checks and non-game portable startup status are recorded by
PACKAGE-CHECKS.json and VALIDATION.md in that output directory.

Live G06-G10 appearance, scene transitions, NR/FG/HDR coexistence, performance,
VRAM pressure and long-session stability await the user's acceptance pass.
Use G06-G10.md for the control table and scene checklist. Test High map sizes
before Ultra on the RTX 3070. No performance gain is promised.
