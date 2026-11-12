# G11-G13 validation checkpoint

11 November 2026. Existing main HEAD:
`5710aeb4626cc5f86ba0730b9afc3e1de5c4442d`. All inherited G01-G10 dirty work is
preserved. No branches/worktrees, staging, commits, pushes or publication.
Stable v1.0.3, rc1/rc2/rc3, game files, saves, user INIs and model DLLs untouched.

## Executed checks

| Check | Result and limits |
| --- | --- |
| Engine build | CLANG64 CMake/Ninja bbgpu and host shader generation passed; native helper linked a fresh bb-probe.exe. |
| Launchers | WPF and Avalonia Release builds passed. Existing PadMap/unawaited-call and offline NuGet-audit warnings remain. |
| Point-shadow GPU output | RTX 3070: both packed HDR light targets checked at odd 19x11 dimensions. 418 values attenuated; zero strength, sky, absent lights, no positive delta, pre-draw snapshot floor and clipped tile guards passed. Reused the existing Vulkan fixture. This does not execute live Rasterizer resource orchestration. |
| Effect pipelines | 19 cumulative compute pipelines created, including the matched 7-descriptor/64-byte point-shadow pipeline. Khronos validation layer unavailable; zero messenger warnings is not full Vulkan validation coverage. |
| Occlusion model | Existing share/scaling/predication/history checks and new complete/nonempty/zero/fresh, late-write, reversed-frame and extreme-limit boundaries passed. Live query resolve and guest-memory reuse remain gameplay coverage. |
| Settings and presets | New point-shadow default, save/load/startup snapshots, strength and selective preset application/rejection passed. Both immutable 73-key presets and custom CRUD/export/import tests passed. Headless preset reference UI test passed. |
| G01-G10 regressions | Regular and HQ GTAO GPU output, directional shadow PCF/PCSS/cascade tests, point-fog GPU reference (largest error .614% across 1,881 values), tint GPU, light tracking, native resolution/cascade fixtures and hook-context checks passed. |
| NR GPU regressions | Base, scaled and look shaders passed LDR/HDR identity, alpha/detail/depth/motion/history/finite-output cases. |
| NR style/lifecycle | Extra-style model checks and actual forwarder with owned test-runtime 0/+64/-64/+16/0 changes and three-pass lifecycle gate passed; one NGX initialization. No proprietary model inference/gameplay claim. |
| NR source preservation | Nine NR implementation/shader/forwarder/immutable-preset files still match the recorded NR-controls hashes. Settings/overlay were extended without editing these files. |
| Other native checks | UI composition, motion history, upscaler support and engine hook ABI fixtures passed. |
| Python | 37 patch tests passed (3 skipped); 5 SSR tests passed. |
| Application regressions | UpdateChecks, NrRuntimeChecks, NrOptimizationChecks, DisplayChecks and LocalizationChecks passed. |
| Maintainable sources | 17-file supplement and preimage/output manifest exported. Guarded helper verified staging replay, real application, already-applied detection and current applied source hashes without Git metadata writes. |

G13 removes unused camera-inverse/gap work only. Matrix-selection/toggle/log
branches were reviewed; no FPS benchmark or speedup claim. G12 deliberately
uses conservative boolean visibility instead of small positive sample counts.
G11 restores attachment load operations after snapshots, including its reduced
proxy fallback; native lighting remains the fallback on unsupported draws.
Resource ordering/lifetime was reviewed; validation-layer and live-game coverage
are unavailable in this automated pass.

## Candidate

Version `1.0.4-nr.rc4`, containing cumulative G01-G13 work, created by the existing
build-nr-release.ps1 packaging system with cached dependencies and clean default
settings. Normal BloodbornePC naming and updater family/engine identity retained.
No private files, user configuration, game files or proprietary NR model are
package inputs. Full applied source is included separately.

Entry:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\g11-g13-build\1.0.4-nr.rc4\BloodbornePC\BloodbornePC.exe`

The output directory's PACKAGE-CHECKS.json and VALIDATION.md record archive,
privacy, exact source/binary hashes and non-game portable startup outcomes.

Acceptance remains the user's combined in-game test: G11 shadow appearance and
cost, G12 lamp/glare and geometry behavior, all effects with NR single/multipass,
DLAA/SR/FSR, transitions, resizing, frame generation, HDR and long sessions.
Use G11-G13.md for controls and checklist. No gameplay was launched here.
