# M0 baseline and gap audit

12 November 2026. **GO for T2-01 only.** No T2-02 through T2-06 code is authorized
or implemented in this checkpoint.

## Authority and baseline

- Existing local main: `e0f47880f1c88a31b7288ce852d9ce4e88dc90e9`.
  Initial `git status --short` was empty. Public GitHub main and the local
  origin/main tracking ref both reported 5710aeb4626cc5f86ba0730b9afc3e1de5c4442d;
  the local e0f4788 main is authoritative. No fetch/ref write was performed. No branch/worktree or Git metadata edits.
- Repository engine/ is patch-only. Actual complete source remains
  `.release-work/nr-controls-engine` with build.sh, native CLANG64 out/gpu and
  out/bb-probe.exe. It has no independent Git identity.
- Five ordered Tier 1 manifests merge into 65 authoritative final source paths;
  all 65 matched before T2-01 edits. Their patches and helpers are committed on
  the current baseline. New work must be an additional reproducible supplement.
- Known-good local candidate: `.release-work/tier1-live-build/1.0.4-nr.rc5`.
  Its original packaging provenance records the earlier 5710aeb source-repository
  HEAD; do not relabel that candidate as e0f4788. Owner reports initial Tier 1/live
  acceptance. Its package and v1.0.3 stay untouched.
- Before T2 changes: tier1-live-test passed 8,192 state transitions;
  preset-ui-test passed; RTX 3070 NR base/scaled/look shader checks passed.
  These are a baseline smoke check, not another gameplay acceptance pass.

## Tier 1 preservation inventory

| ID | Existing implementation | Runtime semantics |
| --- | --- | --- |
| G01/G02 | vk_gtao.h, gtao.frag, vk_ao_spatial, full/filter shaders | Startup GTAO/quality, original SSAO fallback, strength once |
| G03 | vk_ssgi.cpp and compute shaders | Frame-latched live enable, temporal invalidation |
| G04 | vk_tier1_shaders.h, guarded SSR/composite/water replacements and vk_ssr_temporal | Startup selection; cold/cached paths preserve original SPIR-V |
| G05/G06 | vk_contact_shadow and guest directional controls | PCSS startup, contact live; independent strengths |
| G07/G08 | vk_volumetric_light, point_volumetric, lamp_fog_lights | Independent live sun/fog with tint/light-memory reset |
| G09/G10 | shim/engine/shadow_controls.cpp | Guarded cascade/filter and map allocation hooks; restart |
| G11 | vk_point_shadow.cpp and matched shader | Live screen-space local-light delta attenuation |
| G12 | vk_occlusion and occlusion_model | Complete/fresh zero-only hiding, conservative visible fallback |
| G13 | vk_camera_motion and inherited renderer caches | Conditional inverse work; existing async/cache/retirement retained |

Five live flags are sampled by Tier1ApplyLiveSettings at display boundaries.
Transitions invalidate SSGI/SSR, NR/SR and FG histories via existing flag-only
APIs. Default new Tier 1 switches remain off. No T2 change replaces that flow.

## Additional feature matrix (before T2-01)

| Feature | State | Local evidence / decision |
| --- | --- | --- |
| Modern map-driven DoF | Missing | No depth_of_field shader/class or graphics_dof settings; native effect_dof patch exists |
| POM cavity | Missing | No cavity shader/settings/native integration; localized POM text alone is not implementation |
| Motion-blur quality | Partially integrated | Native blur enable/temporary upscaler hold exists in graphics.cpp; no modern quality option |
| Smooth AO/GI normals | Missing | Existing normal inputs, no graphics_smooth_normals or smoothing pass |
| Sky seam/zenith fixes | Missing | No sky_seam/sky_zenith modules or host shaders |
| Native authored auto exposure | Partially integrated | Original YEBIS exposure and picture hooks exist; provider auto-exposure in DLSS/FSR is a different feature |
| GGX modes/local lights | Missing | No GGX patch/model selectors; unrelated network text is not graphics support |
| Froyok bloom | Missing | Original bloom/picture multiplier exists; no new bloom pyramid module |
| GT7/ACES tonemappers | Missing | Existing native/expand HDR curve and game tone map are not selectable GT7/ACES |
| Native SSS | Missing | No native_sss hooks or setting |
| Radiance probes | Missing | Existing G03 screen-space GI has no probe resources or probe shader |
| Wet-ground GI | Missing | No wet-ground GI control or alternate G03 response |
| Inverse-square lighting | Missing | Original light falloff preserved; no bounded-source override |
| VSM / RT lighting | Missing; research-gated | No sun_vsm/rt_shadow acceleration structure lifecycle; not imported |

The gaps are future roadmap entries, not work to implement during T2-01.
No verified AO projection regression was established in M0; T2-02 stays unstarted.

## Shared resources and minimal DoF strategy

Existing NR is evaluated before ordinary DLSS SR/DLAA at the existing temporal
upscaler trigger (default compute hash 0x9a9cf8a9); its reactive mask, composition,
1-3 independent model histories and color processing are preserved. FSR/TAA use
their original branches. HDR gain-map/UI capture and presentation stay unchanged.

DoF consumes the original YEBIS alpha-encoded map far curve and blur constants.
Capture inverse projection at shadow resolve 0xf55643de; snapshot scene depth at
alpha encode 0xfbe7c4b6; capture kernel at PS e80526cc / VS 7e956bc3; replace RGB
after the recognized native recombination PS 9e1cb278 / VS d3ca03f3. Original
passes still execute as fallback; the calibrated result overwrites their RGB,
rather than applying a second blur to the already blurred result. Alpha remains
native. DoF remains at the game's authored position before bloom/tone mapping;
no NR/SR trigger is moved. Dynamic precise draw ordering and uncommon alternate
passes remain owner gameplay checks.

Use current SceneTargets proxies/internal sizes, mapped descriptors and offsets,
UID-aware cached views, completion-based retirement and copied descriptor data.
No game/GPARAM modification, CPU depth readback or new framework is needed.
Original mode and the existing master DoF patch are startup selections; new
intensity 0..3 is live and independent of Picture tuning.

## Pinned upstream and license

Rechecked public branch heads on 12 November 2026:

- BBFSS bbfss: `42bbde58126cf0e94c6cdaae149ccc0a788e4713`.
- BBFSS beta: `306221668207a5b38c825ed6a28f66fbefed192a`.
- LanternEngine main: `573f83fd366d70bf26ce788edb9775a1b863676a`.

Use the coherent beta DoF cpp/header/shader/test, preserving the original bbfss
specification. Beta includes `184ca51997693e84d7a8c12b245809b945bf44c0` view-cache,
fixed descriptor-copy arrays and barrier improvements. No Froyok/sky/GGX code
from that commit is imported. Ported files retain GPL-3.0-or-later SPDX notices.

References: [BBFSS DoF specification](https://github.com/fromsoftserve/BBFSS/blob/42bbde58126cf0e94c6cdaae149ccc0a788e4713/engine/docs/depth-of-field.md),
[beta source](https://github.com/fromsoftserve/BBFSS/tree/306221668207a5b38c825ed6a28f66fbefed192a/engine),
[optimization](https://github.com/fromsoftserve/BBFSS/commit/184ca51997693e84d7a8c12b245809b945bf44c0).

Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine).
BloodborneX is a changed, unofficial fork. No publication or game-file edits.
