# BloodborneX — Tier 2 Graphics Integration Roadmap

> **Status:** M0/T2-01 complete; owner authorized T2-02–06 as a combined acceptance batch on 12 November 2026. T2-02/03/04 implemented in rc7; T2-05 NO-GO at budget gate; T2-06 research complete. See [batch report](BATCH-TEST.md). Stop for owner acceptance.
> **Prepared:** 2026-11-12  
> **Target repository:** `C:\Projects\Bloodborne Engine\BloodborneX` — [TheHiepDang/BloodborneX](https://github.com/TheHiepDang/BloodborneX)  
> **Working branch:** Existing local `main`; **do not create a branch or worktree** unless the owner explicitly changes this instruction  
> **Baseline:** BloodborneX v1.0.3 has been released; Tier 1 G01–G13 and their live-toggle follow-up have been implemented and passed initial manual acceptance testing, as reported by the project owner. The precise current local HEAD and uncommitted state must be verified by Codex.

## 0. Objective and scope

Bring **additional graphics enhancements from BBFSS** into BloodborneX beyond the previously completed G01–G13 scope. Preserve BloodborneX's specialized **DLSS Neural Rendering (NR)**, temporal upscalers, frame generation, HDR, original visual settings, custom/spec presets, `BloodbornePC` identity, and the current auto-updater.

This plan is deliberately **incremental**. Each milestone produces a self-contained, separately testable **local build**, followed by an **owner-performed gameplay acceptance test** before the next milestone begins. **Do not import BBFSS beta wholesale** and do not start all six milestones in one Codex run.

### Evidence and source versions

Source-tree inventory verified on **2026-11-12**; pin these historical references for reproducibility, then check upstream for later changes before each milestone:

| Repository | Branch | Verified commit | Role |
|---|---|---|---|
| [BBFSS](https://github.com/fromsoftserve/BBFSS/tree/bbfss) | `bbfss` | `42bbde58126cf0e94c6cdaae149ccc0a788e4713` | Original DoF, native exposure, lighting/tilt work |
| [BBFSS beta](https://github.com/fromsoftserve/BBFSS/tree/beta) | `beta` | `306221668207a5b38c825ed6a28f66fbefed192a` | Advanced GI, GGX, bloom/tonemapping, SSS, sky and RT experiments |
| [LanternEngine](https://github.com/Awakenchan/LanternEngine) | `main` | `573f83fd366d70bf26ce788edb9775a1b863676a` (0.9.42) | Reference for already-upstreamed features and renderer compatibility |
| BloodborneX | local `main` | **Record at execution time** | Authoritative implementation target |

**Important:** The previously inspected public BloodborneX GitHub tree did not represent all recently completed Tier 1 local work. **Do not infer that something is absent from BloodborneX solely because it is absent from the public GitHub snapshot.** Inspect the current local code, generated/applied engine source, source patches, tests, build scripts, and settings first.

**Starting links:** [BBFSS original README](https://github.com/fromsoftserve/BBFSS/blob/bbfss/README.md) · [BBFSS beta README](https://github.com/fromsoftserve/BBFSS/blob/beta/README.md) · [BBFSS branch comparison](https://github.com/fromsoftserve/BBFSS/compare/bbfss...beta) · [BBFSS PR #1](https://github.com/fromsoftserve/BBFSS/pull/1).

## 1. Engineering rules — apply to every milestone

1. **Work on the existing `main` checkout** at `C:\Projects\Bloodborne Engine\BloodborneX`. Before any edit, record `HEAD`, `git status`, relevant patches/snapshots and build provenance. Preserve any pre-existing uncommitted work. Do not reset, stash, discard or overwrite unrelated changes.
2. **Do not create another worktree or branch; do not commit, push, merge, tag or publish releases** unless expressly requested by the owner.
3. Use the **actual authoritative build input**. If the project still applies `engine/bbhost-changes.patch` into `.release-work/`, modify the reproducible source/patch mechanism, not only a generated engine folder. Verify a clean re-application/rebuild is reproducible.
4. **Preserve stable v1.0.3**, the current working test build, game data, user configuration and save data. Place each distinct test build in a new path under `releases/`, with a unique milestone suffix. Never silently replace `1.0.3-nr.rc3` or any other pre-existing folder.
5. **Do not rename application or update identifiers**. Preserve the `BloodbornePC` packaging conventions, auto-updater family, compatibility and metadata. Preserve third-party credits/licenses when porting code.
6. **Default new effects OFF / Original** unless a verified correctness-only fix cannot reasonably be disabled. Existing installations and saved/custom presets must look and behave as before when new settings are not selected. Adding settings must not corrupt or reorder preset serialization.
7. Keep each effect **independent and reversible**. Provide explicit original/fallback modes; ensure disabled paths do not alter image composition, NR input, or allocate unnecessary expensive resources.
8. Follow upstream's **actual live-vs-restart behavior**. Only claim live support if toggling changes the rendered result in the current session. Classify shader-variant or allocation changes as restart-required where necessary; live intensity sliders remain live when safe.
9. Preserve **NR single/multipass, shader evaluation and color pipeline**, DLSS SR/DLAA, FSR, frame generation, HDR/SDR, UI/presets and existing G01–G13 controls. Validate with NR **ON and OFF**.
10. Keep GPU cost visible: the owner tests primarily at **1920×1080 on an RTX 3070, 8 GB VRAM**. Record scene-matched FPS/frame time and VRAM when available, avoiding an extensive new benchmark harness. No invented performance claims.
11. Reuse current build/test tools; import/adapt upstream targeted tests where useful. Confirm shader cache versioning and descriptor/command-buffer lifetime when changing shader generation, pipelines or resources.
12. **One milestone per execution.** Stop at its GO/NO-GO checkpoint and report concrete evidence and build path; wait for the owner's acceptance before starting the next.

## 2. Mandatory M0 — Local baseline and gap audit

**Purpose:** Prevent duplicate or conflicting ports. This is a short gate at the start of T2-01, not a stand-alone investigation month.

- Read current `AGENTS.md` if present and review `docs/tier1-graphics/` / current Tier 1 implementation reports.
- Verify current local branch, HEAD, status, build inputs and test package layout.
- Confirm the feature-state and actual shader/render-hook paths of **G01–G13**, including live toggles and default-off behavior.
- Audit whether these features are **already present or partially present**: modern DoF, POM cavity, motion-blur quality, smooth AO/GI normals, sky fixes, native auto exposure, GGX modes, Froyok bloom, GT7/ACES tonemapping, native SSS, radiance probes, wet-ground GI, inverse-square lighting, VSM and RT features.
- Map shared shader, frame resource, depth/normal/history and composition dependencies; note where NR evaluates relative to effects and tonemapping.
- Verify version/license provenance for any selected upstream code.
- Record source branch + SHA and create a **state matrix**: `Already integrated | Partially integrated | Missing | Superseded | Not suitable`.
- Establish a known-good baseline build and relevant tests if feasible; otherwise document specific pre-existing blockers.

**M0 deliverable:** `docs/tier2-graphics/00-baseline-gap-audit.md` in the local repository, with feature matrix and chosen minimal implementation strategy. **Skip already-integrated items**, do not re-port them.

## 3. Priority and dependency map

| ID | Milestone | Source | Priority | Main dependencies | Restart/live guidance |
|---|---|---|---|---|---|
| **T2-01** | Modern map-driven bokeh DoF | `bbfss`, `beta` | **First** | Native YEBIS DoF parameters, depth/motion, HDR ordering | Engine/path opt-in at startup; intensity live |
| **T2-02** | Visual correctness & shader polish | Mostly `beta` | **High** | GTAO/SSGI/SSR, sky shaders, existing G01–G13 | Small correctness-only fixes may be automatic; expose options appropriately |
| **T2-03** | GGX / advanced local-light response | `beta` | **High impact** | Shader recompilation, material and local lighting hooks | Specular shader model usually restart; scalar tuning live where verified |
| **T2-04** | Cinematic exposure, tonemap & bloom | Both; mostly `beta` | **Medium** | Native exposure; HDR/SDR and NR composition | Exposure and tone strength live when safe; bloom mode may restart |
| **T2-05** | Probe GI and glossy/wet reflections | `beta` | **Experimental** | G03 SSGI; view-depth, normals, motion/history | Explicit history invalidation and resource lifecycle |
| **T2-06** | VSM / ray-traced lighting | `beta`, compare LanternEngine RT | **Research / optional** | RT device extensions, geometry capture, Vulkan memory/synchronization | RT initialization may require restart; not assumed shippable |

**Recommended execution order:** `M0 → T2-01 → owner acceptance → T2-02 → acceptance → T2-03 → acceptance → T2-04 → acceptance → T2-05 feasibility gate → T2-06 research gate`.

T2-02 may safely precede T2-01 if M0 identifies an existing AO projection bug or immediate stability regression; record the reason. Do **not** treat T2-05 or T2-06 as automatic implementation commitments.

---

## 4. T2-01 — Modern map-driven bokeh depth of field

**Goal:** Port BBFSS's map-aware modern DoF as a selectable alternative to the original YEBIS DoF without altering the original mode.

**Primary references:**

- [BBFSS DoF specification](https://github.com/fromsoftserve/BBFSS/blob/bbfss/engine/docs/depth-of-field.md)
- [`depth_of_field.comp`](https://github.com/fromsoftserve/BBFSS/blob/bbfss/engine/gpu/shadps4/video_core/host_shaders/depth_of_field.comp)
- [`vk_depth_of_field.cpp`](https://github.com/fromsoftserve/BBFSS/blob/bbfss/engine/gpu/shadps4/video_core/renderer_vulkan/vk_depth_of_field.cpp)
- [`test_depth_of_field.cpp`](https://github.com/fromsoftserve/BBFSS/blob/bbfss/engine/tests/test_depth_of_field.cpp)
- Beta optimization: [`184ca5199769`](https://github.com/fromsoftserve/BBFSS/commit/184ca5199769) (view caching/barriers/ordering).

**Implementation scope:**

- Trace Bloodborne's YEBIS DoF parameters and current pre-/post-upscale, postprocessing and NR composition order.
- Adapt map-authored far-field blur, calibrated bokeh footprint, autofocus and foreground blur to BloodborneX's Vulkan layout. Keep alpha/HDR handling correct and avoid hardcoding a one-size-fits-all cinematic look.
- Provide **Original DoF** (default) vs **Modern DoF** (opt-in), with clear startup requirements for shader/path selection. Expose **DoF intensity**, default `1.0`, range `0–3`, **live** once modern DoF is active. Preserve existing game's master DoF toggle and map-controlled behavior.
- Reuse/accommodate upstream compute shaders, resource transitions and tests. Avoid repeated per-dispatch allocations if beta fixes are applicable.
- Make modern mode independent of NR and enable it only when required inputs/camera buffers are valid; gracefully fall back on unsupported passes.
- Ensure no duplicate DoF when the native path would otherwise run.

**Acceptance:** Original mode visually and functionally matches the current BloodborneX baseline; modern mode produces controlled near/far blur across several maps; live intensity adjustment is visible; UI/cutscenes/loading and NR/HDR remain stable; passes GPU shader tests; compiles and produces a new test build.

**Done when:** Codex supplies source/build paths, relevant automated test output, a concrete gameplay checklist and an isolated `releases/` test package. **Pause for owner test.**

---

## 5. T2-02 — Visual correctness, AO/GI polish and small improvements

**Goal:** Fix noticeable artifacts before expanding the lighting model. Keep subfeatures independent; integrate only those confirmed missing in M0.

**Work packages (ordered):**

1. **Smooth AO/GI normals:** BBFSS beta `graphics_smooth_normals` averages normals on compatible nearby surface depth to reduce shimmering/blotchy indirect light and glossy sparkle. Keep an opt-out and compare against the original normal pipeline.
2. **Reduced-resolution GTAO projection correction:** inspect [BBFSS beta commit `bf6aa2cbc23b`](https://github.com/fromsoftserve/BBFSS/commit/bf6aa2cbc23b) and verify whether the bug exists under BloodborneX's DLSS/FSR internal scaling. Port the **minimal correction** if applicable; no generic renderer rewrite.
3. **Sky seam/zenith corrections:** adapt optional sky seam and zenith passes, with conservative defaults and visible regression targets (sky horizon, top-of-screen gradients, day/night, camera movement). References: [`vk_sky_seam.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_sky_seam.cpp), [`vk_sky_zenith.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_sky_zenith.cpp), [`test_sky_seam.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/tests/test_sky_seam.cpp) and [`test_sky_zenith.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/tests/test_sky_zenith.cpp). Respect upstream environment-only overrides where useful.
4. **POM cavity and motion-blur-quality gap check:** these were in LanternEngine, not explicitly separate G01–G13 deliverables. Verify whether BloodborneX already contains them. Port only if absent and safe; ensure NR temporal vectors and frame-generation behavior are unchanged.
5. **Volumetric lightshaft tilt** (optional): BBFSS original `volumetric_tilt.glsl` / `test_volumetric_tilt.cpp`, with default zero. Treat as an artistic control, **not** a correctness fix. Consider a live slider only if current Vulkan implementation safely supports it.

**Acceptance:** A/B visual evidence with consistent camera view; no new halo/seams/crawling and no regression to G01/G02/G03/G04 live controls; no projection mismatch across native/quality/performance scaling; preserve defaults and baseline visuals.

**Exit gate:** If a correction is not applicable to the actual BloodborneX code, record `N/A` and skip it rather than manufacturing code changes. Build locally and pause for owner approval.

---

## 6. T2-03 — GGX and advanced local-light shading

**Goal:** Offer more physically plausible material highlights and lighting options while preserving an exact Original shader mode.

**Primary references:**

- [`vk_ggx_patch.h`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_ggx_patch.h)
- [`vk_ggx_lobes.h`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_ggx_lobes.h)
- [`test_ggx_directional.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/tests/test_ggx_directional.cpp) and [`test_ggx_patch.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/tests/test_ggx_patch.cpp)
- [Full GGX and defaults commit `d2e40c3b0096`](https://github.com/fromsoftserve/BBFSS/commit/d2e40c3b0096)
- [Finite inverse-square source size commit `c7a7a51e7f62`](https://github.com/fromsoftserve/BBFSS/commit/c7a7a51e7f62)

**Substeps:**

1. Add selectable **Original (game-authored Blinn–Phong)**, **GGX hybrid** and **GGX full** modes; retain **Original** as the BloodborneX default. Honor upstream's need for shader replacement/compilation and restart indicators where necessary.
2. Integrate and validate local-light GGX, specular antialiasing and environment GGX tails as **independent switches**, retaining safe original fallback. Do not blindly enable the aggressive BBFSS beta defaults.
3. Evaluate **inverse-square local-light falloff** with bounded/finitely sized source, intensity, core size and sharpness controls. **Keep inverse-square OFF by default** to preserve authored lamps and avoid blown-out nearby surfaces.
4. Avoid re-patching or re-writing shader binaries each frame. Audit shader cache keys and invalidation, and use upstream tests to verify that unrelated shaders are unaffected.
5. Check behavior on skin, weapons, fabric, wet paving, distant lights and night scenes; compare with/without NR.

**Acceptance:** `Original` reproduces current BloodborneX visuals, shader variant selection survives restart, no unintended lighting on unmodified materials, stable GPU resources and FPS; all added shader tests pass. Include material comparison instructions, not only screenshots of reflective surfaces.

**Risk:** **High compatibility sensitivity** because changes operate in native game shader recompiler paths, not just independent postprocessing. Stop if unknown shader hashes or version-specific offsets make patches unsafe.

---

## 7. T2-04 — Cinematic exposure, tone mapping, bloom and native SSS

**Goal:** Introduce optional postprocessing/material refinements without double-processing NR or breaking HDR.

**Separate optional work packages:**

- **Native auto exposure** — BBFSS original [`auto_exposure.cpp`](https://github.com/fromsoftserve/BBFSS/blob/bbfss/engine/gpu/shim/engine/auto_exposure.cpp), [`exposure_policy.h`](https://github.com/fromsoftserve/BBFSS/blob/bbfss/engine/gpu/shim/engine/exposure_policy.h), native tests. Preserve the game's authored exposure and YEBIS metering; BBFSS bounds adjustments around area reference roughly **−1 / +0.5 EV**. Default OFF. Provide live enable/recalibration where safe. Verify behavior across map transitions, brightness extremes and exposure changes.
- **Alternative tonemappers** — BBFSS beta `Original | GT7 | ACES`, with exposure control; default **Original**. Map their insertion point precisely relative to NR output, SDR/HDR transfer and any existing HDR scene curve. Never combine two independently active tonemappers unintentionally.
- **Froyok bloom** — BBFSS beta [`vk_froyok_bloom.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_froyok_bloom.cpp) + `froyok_bloom.comp`. Default original bloom; new blend/spread sliders only when Froyok is selected. Maintain correct DoF → bloom → tone map (or justified equivalent) ordering. **Inspect and retain [`engine/licenses/FROYOK-BLOOM.txt`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/licenses/FROYOK-BLOOM.txt) and any other applicable attribution.**
- **Native skin subsurface scattering (SSS)** — BBFSS beta [`native_sss.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shim/engine/native_sss.cpp), [`test_native_sss.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/tests/test_native_sss.cpp). Default OFF; guarded native hooks and clear active/waiting/unavailable state. This package is optional and may move to its own mini-milestone if it needs significant native integration.

**Acceptance:** Original exposure/tonemapper/bloom/SSS all preserve current output; new alternatives independently opt-in, with correct HDR and SDR handling; no NR color-space mismatch, white clipping or double exposure; stable transitions and presets. Do not package changes to the updater, release identity or color pipeline unrelated to the selected feature.

**Milestone split rule:** If work grows, complete exposure first, then tonemap, bloom and SSS in distinct acceptance builds; do not ship an untested combination.

---

## 8. T2-05 — Probe GI, glossy GI reflections and wet surfaces

**Goal:** Evaluate BBFSS beta's advanced GI as an **optional replacement/extension of G03**, not a second independent GI contribution accidentally stacked on top.

**Primary references:**

- [`ssgi_probe.comp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/host_shaders/ssgi_probe.comp)
- [`vk_ssgi.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_ssgi.cpp)
- [Probe optimization commit `8b97daf6fde5`](https://github.com/fromsoftserve/BBFSS/commit/8b97daf6fde5)
- BBFSS beta settings: `graphics_gi_probes`, `graphics_gi_reflections`, `graphics_gi_reflections_full_res`, `graphics_gi_reflection_strength`, `graphics_gi_wet_ground`.

**Feasibility gate before implementation:**

1. Document actual frame graph placement, depth/normal/motion data, history lifecycle and shared resources with G03/G04, NR and FG.
2. Capture baseline scene-matched frame time and VRAM on RTX 3070 with NR ON and OFF; estimate incremental GPU memory and cost; no new standalone benchmark infrastructure.
3. Confirm GI cannot be accidentally double-counted and that there are separate controls for original screen-space GI, probe GI, glossy GI and wet-ground reflection response.
4. Design safe disable/re-enable, camera cuts, load transitions and DLSS internal-resolution changes; invalid histories must be reset, not sampled.
5. Present **GO/NO-GO** for an implementation based on complexity and GPU budget. A NO-GO is a valid milestone result.

**If GO:** Implement incrementally: (a) probes only, (b) glossy reflections, (c) wet-ground response. Default all new contributions OFF; use quality/resolution presets rather than implicitly full-resolution rendering.

**Acceptance:** No stale-history ghosting, shimmer or double GI; no frame-in-flight lifetime errors; predictable behavior under rapid toggles and resolution changes; measured FPS/VRAM cost in the same scene; visual gain justified relative to G03+G04.

---

## 9. T2-06 — Virtual shadow maps and ray-traced lighting (research gate)

**Goal:** Investigate but **do not automatically implement** BBFSS beta's RT/VSM stack on top of a working G01–G13 renderer.

**Primary references:**

- [`vk_sun_vsm.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_sun_vsm.cpp)
- [`sun_vsm.comp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/host_shaders/sun_vsm.comp)
- [`vk_rt_shadow.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/gpu/shadps4/video_core/renderer_vulkan/vk_rt_shadow.cpp)
- [`test_rt_shadows.cpp`](https://github.com/fromsoftserve/BBFSS/blob/beta/engine/tests/test_rt_shadows.cpp)
- [VSM scene/caster fixes `45e1e6cd51`](https://github.com/fromsoftserve/BBFSS/commit/45e1e6cd51)

**Study:** RT device extension requirements; BLAS/TLAS or equivalent captured-geometry representation; geometry visibility/culling and animation; VSM page residency; caster preservation; ray-traced sun and lamp shadows; possible RT GI; integration with G05/G06/G09–G11, NR and temporal upscaling; RTX 3070 occupancy and VRAM constraints.

**Compare against LanternEngine's existing RT files first** (`vk_rt_shadow.cpp`, `rt_shadow.comp`, `point_shadow_ray.comp`) to avoid importing superseded code or two competing acceleration-structure lifecycles.

**Deliverable:** `docs/tier2-graphics/06-rt-vsm-feasibility.md` with a *GO*, *CONDITIONAL GO* or *NO-GO*, evidenced code/dependency map, estimated resource needs and a minimally invasive prototype design if feasible. **Do not ship experimental RT as an enabled default.** A separate user instruction is required to implement it.

---

## 10. Cross-cutting validation and preservation matrix

For every implemented subfeature, record test method, PASS/FAIL/NOT TESTED and evidence. **Do not claim gameplay acceptance from compile-only tests.**

| Area | Minimum check |
|---|---|
| Vanilla/Original fallback | Disabled new effect produces existing BloodborneX visuals and behavior |
| New effect ON/OFF | Visible result changes; saved setting correct; re-enable restores correct output |
| Restart classification | Live controls work in the same session; startup-only changes show truthful pending-restart notice |
| NR OFF / ON | Both paths launch and composite correctly; verify multipass where used |
| Upscaling | Native/DLAA and at least one DLSS scaling mode show correct spatial alignment |
| Frame generation | FG enable/disable behavior unchanged; no new presentation/history artifacts |
| HDR / SDR | Correct color and postprocessing order, no obvious double-tonemap or clipped HDR |
| Resolution changes | Correct allocations and invalidated histories when internal/output size changes |
| GPU safety | No Vulkan validation errors when available, invalid descriptor/transition issues, or memory growth after repeated toggles |
| Scene coverage | At least one sun/shadow scene, lamp/fog scene, dark interior and sky/cutscene scene when relevant |
| Performance | Record same-scene FPS/frame time and VRAM with new effect OFF and ON; note NR and upscaler state |
| Presets | Built-in and custom saved/exported presets handle new settings without disturbing existing options |
| UI / updater | Existing branding, saved settings, version compatibility and auto-updater unchanged |
| Build | Compiled `BloodbornePC` test build at unique absolute path under `releases/` |

**Owner's preferred acceptance approach:** Short fixed-scene, repeated in-game A/B testing, rather than investing time in a large synthetic performance harness. Add automated targeted shader/regression tests where useful, not as a substitute for actual game validation.

**Regression handling:** If a subfeature fails real gameplay acceptance, retain a known-good toggle/fallback and back out only its unsafe portion; do not silently modify another feature or the NR pipeline to hide the issue.

## 11. Required milestone deliverables and report structure

For **each** implemented milestone `NN-name`:

1. `docs/tier2-graphics/NN-name.md` — scope, actual local baseline SHA/status, source branch and exact SHA, files changed, architecture decisions, defaults, restart/live semantics, test results, known limitations, owner acceptance checklist and explicit GO/NO-GO.
2. A **new local test build** under `releases/` (e.g. `releases/1.0.3-t2-01-dof-test/`; Codex may choose a noncolliding name). Include **absolute Windows path**, main executable and clear install/run notes.
3. A compact comparison of new effect OFF vs ON (screenshots or numbers if actually captured; otherwise manual test instructions).
4. A separate line identifying what was **not tested** (especially actual gameplay, NR, HDR, unusual maps).
5. A closing **STOP** after the milestone report and build, leaving the next milestone unstarted.

For the **roadmap as a whole**, retain the following project docs within `docs/tier2-graphics/` once Codex copies this plan into the repository:

- `README.md` — this roadmap / status matrix (or link to this file).
- `00-baseline-gap-audit.md`
- `01-modern-dof.md`
- `02-visual-correctness.md`
- `03-ggx-lighting.md`
- `04-cinematic-postprocessing.md`
- `05-probe-gi.md`
- `06-rt-vsm-feasibility.md`

Do not fabricate these reports before their milestone work is performed.

## 12. Codex kickoff instructions (copy/paste)

> Read the Tier 2 graphics roadmap at `docs/tier2-graphics/README.md` (or the exact local Markdown path I provide). Treat it as the project plan and preservation contract. **Execute only M0 and T2-01 (Modern DoF) in this session**. Work directly in the existing BloodborneX `main` checkout; no branch or worktree; do not commit, push or release. Confirm the current local source/build authority, skip functionality already integrated, adapt the referenced BBFSS code conservatively, run relevant tests and produce a separate local `BloodbornePC` test build under `releases/`. Preserve Tier 1 live toggles, NR, HDR, presets, branding and updater. Provide an exact build path, implementation report and concise manual test checklist, then **stop for my acceptance test**.

## 13. Roadmap tracker

| Milestone | Planning | Implementation | Build | Manual acceptance | Decision |
|---|---|---|---|---|---|
| M0 baseline/gap audit | Defined | Not started | — | — | Pending |
| T2-01 modern DoF | Complete | Implemented | 1.0.4-nr.rc6 candidate | Awaiting owner test | CONDITIONAL GO; STOP |
| T2-02 visual correctness | Complete | Implemented; optional tilt deferred | rc7 batch | Awaiting owner test | CONDITIONAL GO |
| T2-03 GGX/local lights | Complete | Implemented; discrete toggles live | rc7 batch | Awaiting owner test | CONDITIONAL GO |
| T2-04 exposure/tonemap/bloom/SSS | Complete | Implemented; live | rc7 batch | Awaiting owner test | CONDITIONAL GO |
| T2-05 probe GI | Gate complete | Deferred; no measured NR ON/OFF budget | No prototype | Not tested | NO-GO for implementation |
| T2-06 RT/VSM | Research complete | Separate authorization required | No prototype | Not tested | CONDITIONAL GO for separate prototype |

**Project policy:** The next milestone starts **only after** the owner accepts the previous milestone's build, unless the owner expressly changes the sequence.
