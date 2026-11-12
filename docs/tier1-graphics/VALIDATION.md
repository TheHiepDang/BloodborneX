# M1 GTAO validation, 11 November 2026

Baseline: clean local main and remote main both
`5710aeb4626cc5f86ba0730b9afc3e1de5c4442d`. No branch/worktree/ref changes,
commits, pushes, publication, game launches or installed-package modifications.
The earlier separately-created worktree was not used.

## Completed checks

| Check | Result / coverage |
| --- | --- |
| Pre-change source | All 24 NR supplement files matched their recorded hashes. Source preimages saved before GTAO edits. No separate unmodified full baseline rebuild was run. |
| Native build | Existing CLANG64 CMake/Ninja `bbgpu`, host shaders and GTAO test compiled. Existing native executable helper linked fresh `out/bb-probe.exe`. |
| Launcher builds | Both WPF and Avalonia Release builds passed. Existing entry-point/async warnings and offline NuGet vulnerability-feed warnings remain. |
| GTAO GPU output | Adapted regular-GTAO subset of pinned upstream `test_gtao.cpp` passed on NVIDIA GeForce RTX 3070: six scenes, occlusion, packed depth, strength zero/half/full, deterministic output, native/Performance/Balanced UV mapping and sky. No gameplay or performance claim. |
| Settings | Native `settings-test` passed, including new default-off and on/off save-load/startup-snapshot round trips. Both immutable 73-key profiles and custom preset CRUD/invalid-input tests passed. |
| Preset UI | Headless `preset-ui-test` passed: selection, fallback rows, group toggle, snapshot isolation and fixed header. No full GUI/gamepad certification. |
| NR style | Native `nr-extra-style-test` and owned synthetic-runtime forwarder lifecycle test passed, including multipass, zero/positive/negative style and restore. |
| NR GPU shaders | Existing `hdr-native-shader-test --nr` passed on RTX 3070: NR base/scaled LDR/HDR, alpha, depth/motion units, look controls/history and finite output. Synthetic HDR is not live HDR gameplay. |
| Rendering support | `upscaler-support-test`, `ui-composition-test`, `motion-history-test` passed. |
| Launchers/updater | UpdateChecks, NrRuntimeChecks, NrOptimizationChecks, DisplayChecks and LocalizationChecks passed. No network publication or in-place update against a game installation. |
| Startup patches | Python `test_patches.py`: 37 tests run, OK, three skipped by existing conditions. |
| Cache path review | Cold compile and cached load use identical layout-guarded substitution; persisted shader binary remains original. No cache schema/ID change. Real game cold/warm/off-on-off cycles remain manual. |
| Source reproducibility | Maintained GTAO patch plus per-file before/after hashes and guarded application helper. A fresh replay from preimages, checking without mutation, application and already-applied checks all passed. The helper stages LF-normalized inputs and validates every output before copying back, avoiding parent-repo discovery and CRLF ambiguity. |
| Package validation | Existing archive verifier passed Windows/source ZIP hashes, complete file manifests, privacy policy, version/metadata and fresh engine checks. Existing portable smoke test passed from a fresh extraction with spaces, restricted PATH, bundled Python, NR startup preparation and real Vulkan capability probing. No game launched. |

The forwarder fixture initially lacked the CLANG64 DLL search path; rerunning with
`C:\msys64\clang64\bin` on PATH passed. Native compilation initially caught a
Vulkan-Hpp const-handle constructor mismatch, corrected before the successful
build. Production behavior was not weakened for either issue.

## Local test build

The existing `tools/build-nr-release.ps1` packaging flow completed with
`-Version 1.0.4-nr.rc1 -ArtifactPrefix BloodbornePC` and separate root
`.release-work/gtao-build`. Cached, verified dependencies are read from the
v1.0.3 download cache; its binaries and settings are not copied into the build.
The source snapshot was refreshed with the final patch/helper, source files and
documentation, then its manifest/archive checksums were regenerated and verified.
The Windows archive and executable bytes were retained from the completed build.

Run separately:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\gtao-build\1.0.4-nr.rc1\BloodbornePC\BloodbornePC.exe`

Portable ZIP:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\gtao-build\1.0.4-nr.rc1\BloodbornePC-1.0.4-nr.rc1-win64.zip`

This is a local candidate only. Do not install it over stable v1.0.3. Choose a
separate save folder before playing, keep SSAO enabled and turn GTAO on in
Graphics, then start/restart the game. Normal builds remain default-off.

Native executable:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\nr-controls-engine\out\bb-probe.exe`

GTAO GPU regression executable:
`C:\Projects\Bloodborne Engine\BloodborneX\.release-work\nr-controls-engine\out\gpu\gtao-test.exe`

## Remaining acceptance

GTAO/SSAO A/B in actual gameplay, live DLSS/FSR/NR coexistence, scene scaling,
warm pipeline-cache switching, resolution transitions, HDR/FG and long sessions
remain unverified. There are no new images, descriptors, passes or synchronization
calls in the regular-GTAO implementation; this reduces the resource-lifetime
surface but does not certify the driver or in-game pipeline. HQ AO is deferred.

Follow the manual checklist in README.md. M1 is a local test candidate until that
gameplay acceptance is completed; M2 should add HQ AO after this checkpoint.
