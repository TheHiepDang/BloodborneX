# G02-G05 validation checkpoint

11 November 2026. Local main remains `5710aeb4626cc5f86ba0730b9afc3e1de5c4442d`
with the preserved uncommitted M1 work plus this batch. No commits, pushes,
branches, worktrees or publication. The user reported M1 looked good during a
short gameplay session; combined G02-G05 gameplay acceptance is still pending.

## Results

| Check | Result and limits |
| --- | --- |
| Native engine | Existing CLANG64 CMake/Ninja build compiled the new shader passes, scene callbacks and engine library; native executable linked with the existing helper. |
| Both launchers | WPF and Avalonia Release builds passed. Existing warnings about offline NuGet auditing, PadMap entry point and unawaited calls remain. |
| Regular GTAO GPU | Passed six synthetic scenes, packed depth, AO strength, deterministic output, native/Performance/Balanced UV mapping and sky on RTX 3070. |
| HQ GTAO GPU | Adapted current upstream GPU test passed: plane/corner/curved/rails/foliage/moving scenes, packed depth, strength, noise reduction, edge rejection and partial/padded/proxy UV cases. Full internal-resolution production interception remains a gameplay check; its compute pipelines were created in the pipeline test. |
| Effect pipelines | All 12 compute pipelines for this batch created successfully on RTX 3070, including HQ full-resolution and filters, SSGI, SSR and PCSS. Vulkan validation layer unavailable; zero messenger warnings does not establish validation-layer coverage. |
| Directional shadows GPU | Upstream tests passed atlas-safe filtering, PCSS edge widths/contact hardening, thin casters, buffer offsets, RGB preservation, stock/sky fallback and frame stability. The tests also exercise shader cascade code; production cascade blending stays zero in this batch. |
| SSR checks | Five upstream Python geometry/composite/intensity tests passed. Replacement fragment modules and compute pipelines created. Real-game checksum compatibility and reflection quality remain unverified. |
| SSGI | All four shader stages compiled and their pipelines created. Real scene capture, visual output and live coexistence with NR remain unverified. |
| Settings/presets | Native settings save/load passed new on/off/quality/startup-snapshot and strength round trips; both immutable 73-key profiles and custom-preset tests passed. Headless preset UI passed selection/snapshot tests. |
| NR regressions | Existing NR base/scaled/look shader GPU tests passed LDR/HDR/alpha/depth/motion/history cases. NR extra-style and owned synthetic-runtime multipass lifecycle tests passed. NR pipeline implementation, model file, immutable preset arrays and NR shader source bytes remain unchanged. |
| Launcher regressions | UpdateChecks, NrRuntimeChecks, NrOptimizationChecks, DisplayChecks and LocalizationChecks passed. No game launched. |
| Reproducible source | New 36-file supplement applied to preserved M1 preimages in a fresh replay directory. Check-only, application, every output hash and already-applied checks passed. |

The dispatcher preserves the existing recording thread and uses the existing
scene target, scheduler and resource retirement APIs. No Scheduler::Finish call
was added inside NR Prepare or an active draw. New resources are independently
owned. Runtime restart flags avoid live shader/pipeline-key mismatches.

Sources: LanternEngine `573f83fd366d70bf26ce788edb9775a1b863676a` for HQ AO/SSR,
`4889a8a8bd72d74adc7ce49b89ddd951eba48bd2` for pre-ray-tracing SSGI/PCSS. These
effects originate in work by fromsoftserve/BBFSS and Awaken; SPDX notices remain.
Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine).

## Candidate output

The existing packaging flow uses version `1.0.4-nr.rc2`, artifact prefix
BloodbornePC and separate output root `.release-work/tier1-batch-build`.
The M1 candidate and stable v1.0.3 remain untouched. Settings are clean defaults;
user configuration, saves, game data and the proprietary NR model are not copied.

Expected entry:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\tier1-batch-build\1.0.4-nr.rc2\BloodbornePC\BloodbornePC.exe`

Packaging and portable-smoke status is recorded by `PACKAGE-CHECKS.json` and
`VALIDATION.md` in that output folder. Those reports do not certify gameplay.

Use the combined acceptance checklist in G02-G05.md. In particular, test wet
surfaces, sunlit contact edges, dark interiors, scene changes and menus with NR
off, then your usual NR settings. Record visible changes, GPU cost and stability.
