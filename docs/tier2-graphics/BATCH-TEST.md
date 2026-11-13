# Tier 2 remaining milestones — local acceptance batch

Scope: T2-02 visual polish, T2-03 GGX/local lights and T2-04 exposure/tonemap/bloom/SSS implementation; T2-05 probe GI feasibility and T2-06 RT/VSM research gates. The owner explicitly requested this batch after the T2-01 candidate. Probe GI and RT/VSM are not included as implemented rendering features.

Local baseline: `main`, `e0f47880f1c88a31b7288ce852d9ce4e88dc90e9`; previous T2-01 uncommitted source, patch and `releases/1.0.4-nr.rc6` preserved. No commit, push or publication.

Isolated local candidate path:

`C:\Projects\Bloodborne Engine\BloodborneX\releases\1.0.4-nr.rc7\BloodbornePC\BloodbornePC.exe`

Packaging and portable checks are recorded by the candidate's `PACKAGE-CHECKS.json` and `VALIDATION.md`. This is a local acceptance build, not a published release.

## Controls and acceptance

The final package includes the reviewed view-space depth comparison in the SSGI normal smoother. An earlier internal packaging pass is preserved under the ignored build workspace and is superseded.

New options are in the existing Graphics launcher page and in-game Display tab. All new effects default OFF/Original. Launcher edits are loaded on the next launch; use the in-game controls for live comparison. Directional GGX, local GGX, specular AA, environment tails, inverse-square enable, POM, blur quality, smooth normals, sky corrections, exposure, SSS, tone selection and bloom selection are live. The three local-light scalar coefficients are baked into shaders at startup and explicitly require restart. Modern DoF mode still requires restart; its intensity remains live.

1. Run this portable folder separately using your normal user-supplied game/runtime setup. Do not copy it over the installed game or an earlier candidate. No game dump, proprietary NR model, personal INI, saves or credentials are distributed.
2. Start with all new effects OFF/Original. Check existing Tier 1 live controls, NR single/multipass, DLSS SR/DLAA, FSR/FG, picture tuning and custom/spec presets.
3. Compare a fixed sunlit view, dark interior, lamp/fog view, weapon/cloth/skin and Dream sky. Enable one new effect at a time, switch OFF and back ON, and compare to Original. The first new shader variant can hitch while compiling; subsequent selections reuse it.
4. Test AO edges and sky while moving the camera, at DLAA and DLSS Quality/Performance. Enable the game's motion-blur master to compare blur quality where the existing upscaler policy permits it. Unknown POM shader layouts deliberately retain Original.
5. Test exposure adaptation and recalibration across dark/bright transitions; note Active/Waiting/Unavailable status. Test bloom with Original and Modern DoF. SSS may legitimately wait for an allocated skin view. Compare GT7/ACES in SDR; the existing HDR scene curve has precedence when active.
6. Save/reload a custom preset and restart the app to verify persistence. Do not overwrite the immutable provided RTX 3070 presets. Compare FPS/frame time and VRAM in the same scene with NR OFF/ON and each new effect OFF/ON. These readings are also needed before a future probe-GI prototype.

No actual gameplay, visual-quality, long-session, HDR-display or scene-matched performance acceptance is claimed by automated tests. Stop here for the owner's acceptance test.

## Automated validation

| Check | Result / evidence |
| --- | --- |
| Fresh native engine and shader lab | PASS; existing CLANG64/Ninja build and native linker; `bb-probe.exe` SHA256 `935E624BF842C5D97297799FF23E477D597C6BC29B9438CDA3A32C68BC2B78FC` |
| WPF and Avalonia launchers | PASS; .NET Release builds. Existing compiler warnings and unavailable NuGet vulnerability feed remain warnings. |
| New native policy/hooks | PASS: exposure policy, native exposure and native SSS fixtures; unsupported/ownership/disable/restoration branches |
| Shading guards | PASS: upstream GGX, lobe and finite falloff tests. Captured full directional material replay NOT RUN (external game captures required). |
| Motion-blur GPU output | PASS on RTX 3070: HDR/gains, motion extremes, depth/velocity refusal, odd extents and edges |
| Sky GPU output | PASS: exact UV math, geometry/texture refusal, zenith filtering/alpha/lower panorama and cache reuse; owned synthetic fingerprint/sample fixtures, not game screenshots |
| Bloom/tone IR | PASS: Original bloom bit-exact in owned fixture, ON/OFF/restoration, use-definition order, GT7/ACES branch construction, HDR exclusion and metadata-tail round trip |
| Effect Vulkan pipelines | PASS: 22 compute pipelines including three Froyok stages on RTX 3070. Khronos validation layer unavailable; zero debug messages does not establish validation-layer cleanliness. |
| Settings/custom preset persistence | PASS: new flags/modes/scalars save/load, selective preset apply, NaN/out-of-range rejection; existing library CRUD/import/export and UI snapshot isolation |
| NR preservation | PASS: 9 protected NR core/shader/forwarder/spec-profile source hashes unchanged; NR extra-style/identity diagnostics, HDR shader `--nr`, synthetic forwarder live 0/+64/-64/+16/0 with 3-pass lifecycle and model-file preservation |
| Tier 1 / DoF regressions | PASS: Regular/HQ GTAO GPU tests, Modern DoF GPU tests, Tier 1 live-state, shadow/fog/tint, occlusion, upscaler support, UI composition and motion-history checks |
| Launcher/updater | PASS: UpdateChecks, NrRuntimeChecks, NrOptimizationChecks, DisplayChecks and LocalizationChecks; no game started |
| Reproducible source | PASS: 74-file batch supplement clean preimage dry-run, application and already-applied hash checks; preimages match T2-01 outputs |

Source-compatible cache IDs were regenerated using the same source selection/normalization as applied `build.sh`: `gpu-cache.id=775ab45d7bd1da1d`, legacy ID `6a0da895344036d9`. The renderer uses `-tier2-post-v1` to segregate compiler variants. Expect fresh shader compilation/cache warming; compare performance only after it settles.

Engine source inventory: `engine/tier2-batch-baseline.json` lists every changed native/shader/test/license file and its pre/post SHA256; `engine/tier2-batch.patch` reproduces them after T2-01 using `tools/apply-tier2-batch.py`. Outer-repository changes are the two existing Graphics launcher pages, patch/provenance/build guidance and these roadmap/testing tools and reports. Updater production logic, immutable presets, NR runtime/model bytes, previous releases and game files are unchanged.

NOT TESTED: actual gameplay/live visible A/B, material image quality, NR+FG long sessions, HDR display output, scene transitions across real maps, or same-scene FPS/VRAM. T2-05 is NO-GO pending measured budget; T2-06 is research-only CONDITIONAL GO for a separately authorized prototype. See their reports. No experimental probe or RT controls are exposed.

Final packaging: PASS. The Windows ZIP verifies all 504 files and the source ZIP all 2606 files against their manifests. Both embedded versions and the native engine hash match this build. Portable extraction/startup, bundled Python, NR input preparation and Vulkan capability detection pass with system Python/MSYS2 removed from PATH. Packaged source passes the 74-file supplement hash check; Froyok and GT7 licenses are present. See `releases/1.0.4-nr.rc7/PACKAGE-CHECKS.json` and `VALIDATION.md`. Startup patch tests also passed: 37 tests, 3 intentional skips.

Performance tradeoff to measure: recognized upgraded shader programs currently select pipelines on the recording owner even in Original, preserving conservative thread ownership. Their CPU cost is unmeasured. Enhanced combinations reuse cached immutable pipelines, but first selection can compile and consume additional driver cache memory. These checks do not establish unchanged gameplay FPS or bounded whole-session VRAM.

Final executable: `C:\Projects\Bloodborne Engine\BloodborneX\releases\1.0.4-nr.rc7\BloodbornePC\BloodbornePC.exe`. Stop for owner acceptance; no commit, push or publication.
