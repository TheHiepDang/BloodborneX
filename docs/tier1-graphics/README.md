# Tier 1 graphics

The rc5 candidate adds frame-boundary live switching for SSGI, contact shadows,
volumetric sunlight, point fog and point shadows. See [LIVE-TOGGLES.md](LIVE-TOGGLES.md)
for the exact live/restart matrix and acceptance checks.

The rc4 candidate completes the reviewed G11-G13 scope: point-light shadows,
conservative occlusion and conditional camera work. See [G11-G13.md](G11-G13.md).
Gameplay acceptance remains separate from build and synthetic verification.

The rc3 candidate adds **G06-G10: contact shadows, directional volumes, point
fog, cascade filtering and shadow resolutions**. See [G06-G10.md](G06-G10.md).

The earlier rc2 candidate combines **G02 HQ GTAO, G03 SSGI, G04 stochastic SSR
and G05 PCSS** after the user's short M1 gameplay check. See [G02-G05.md](G02-G05.md)
for controls, source compatibility and the combined acceptance checklist.

The M1 baseline and implementation are recorded below.

Baseline: BloodborneX `main` at `5710aeb4626cc5f86ba0730b9afc3e1de5c4442d`.
Development is on the existing main checkout; no new refs, commits or releases.
The authoritative engine remains a full applied source tree, locally
`.release-work/nr-controls-engine`, with reproducible supplements in `engine/`.
Before this change all 24 files in `nr-look-controls-baseline.json` matched their
recorded modified hashes. The applied folder has no independent Git history;
its parent repository HEAD must not be mistaken for an engine commit.

## Sources and strategy

- Primary: [LanternEngine 0.9.42](https://github.com/Awakenchan/LanternEngine/tree/573f83fd366d70bf26ce788edb9775a1b863676a),
  `573f83fd366d70bf26ce788edb9775a1b863676a`, verified 11 November 2026.
- Initial lighting integration: `4889a8a8bd72d74adc7ce49b89ddd951eba48bd2`.
- HQ AO / PR #94 reference: `9244581284e2c5f315c9febb1f513a19dcc731b8`.
- Secondary [BBFSS bbfss](https://github.com/fromsoftserve/BBFSS/tree/42bbde58126cf0e94c6cdaae149ccc0a788e4713):
  `42bbde58126cf0e94c6cdaae149ccc0a788e4713`.
- BBFSS beta reference only: `d2e40c3b0096332e54f5797bc466ed8e58e9b2cc`.

Selectively port effects into the existing renderer. Upstream's effect dispatcher
also owns SSGI, SSR, shadow and volumetric systems absent here; adopting it whole
would bring unrelated resources and newer recording-thread APIs into the NR
baseline. Regular GTAO only needs shader substitution and existing bindings.
HQ AO additionally needs gather tracking, denoising, full-resolution dispatch,
image transitions and lifecycle management; reserve those for M2.

## M1 behavior

`graphics_gtao=0` is the safe default. `1` replaces fragment shader `0xcbbca5dc`
in the game's existing SSAO pass after restart. Keep `effect_ssao=1`; enabling
GTAO does not force SSAO or any other effect on. No extra images, passes,
descriptors, history or GPU synchronization are introduced.

The shader is the pinned upstream `gtao.frag`, compiled through the existing
host-shader CMake target. It retains the game's depth/normal/noise bindings,
packed depth output, constant-buffer offset packing and internal scene-size
push constant. HQ code remains inactive at compile time.

BloodborneX already applies `graphics_ao_strength` to the game's AO constants in
`engine/graphics.cpp::AoDraw` with Picture tuning enabled. The port fixes the
shader's extra `ao_k` multiplier at one, preserving this existing 0..3 UI range
(typed native values up to 5) and preventing double application of strength.
When Picture tuning is off, the area's original strength applies.

Both WPF and Avalonia launchers expose **GTAO (restart required)** under Ambient
occlusion. The native overlay exposes the same switch with its stable INI key;
its existing restart footer tracks changes. Serialization and custom presets
include the new key. The two immutable RTX 3070 profiles remain unchanged.

Replacement requires a fragment stage, zero starting bindings, three uniform
buffers, three images, three samplers, no fmasks, dword uniforms and non-exact
UBO bases. An unsupported layout or failed shader-module creation retains the
original SSAO. This is not a claim of recovery from arbitrary driver/pipeline
failures. PipelineCache owns the replacement module through its existing lifetime.

Cold compilation and cache loading apply the same substitution. Persisted SPIR-V
stays the original game shader, so toggling GTAO in either direction on restart
does not poison the cache. Existing shader/cache IDs and metadata formats stay
unchanged. No NR code, model, NR presets, updater metadata or package naming is
modified. GTAO feeds the existing lighting pipeline before NR/SR.

## Reproduce the engine change

Start with the complete applied v1.0.3 engine including the NR-controls supplement.
Do not apply the historical cumulative patch to an already-applied source ZIP.

```powershell
python tools/apply-tier1-gtao.py C:\path\to\applied-engine --check
python tools/apply-tier1-gtao.py C:\path\to\applied-engine
```

The helper verifies every preimage hash before invoking `git apply --check` and
`git apply`; these commands require no Git refs. Already-applied matching sources
are accepted. `engine/tier1-gtao.patch` and its hash manifest are the maintained
source supplement, not just local build-directory edits. Build with the existing
`build.sh` / release workflow. The local configured CLANG64 tree can use native
`ninja.exe -C out/gpu bbgpu gtao-test settings-test preset-ui-test`.

## Integration roadmap

Statuses below refer to this applied BloodborneX baseline, not public upstream
feature claims. L = pinned LanternEngine `573f83fd...`; I = initial integration
`4889a8a8...`; H = HQ reference `92445812...`. M1 ported only G01; the subsequent combined batch adds G02-G05.

| ID | Feature | Status here | Source | Dependencies / conflicts | Difficulty | Order |
| --- | --- | --- | --- | --- | --- | --- |
| G01 | Regular GTAO | Implemented; GPU synthetic tests passed, gameplay pending | L shader and replacement guard | SSAO pass enabled; UBO layout guards; existing AO strength applied once | Low | 1 |
| G02 | HQ GTAO | Implemented in the combined candidate; gameplay pending | H, then L fixes | Spatial filter, full internal-resolution AO, gather/upscale tracking; preserve packed depth | Medium | 2 / M2 |
| G03 | SSGI | Implemented in the combined candidate; gameplay pending | I (pre-RT) `vk_ssgi` | Camera, scene/depth/normal inputs, history, composition versus NR | High | 7 |
| G04 | Stochastic SSR | Modern replacement and temporal trace implemented; gameplay pending | L / I `vk_ssr_temporal`, effect shaders | Exact shader/layout guards, temporal history and composite, reduced resolution | High | 6 |
| G05 | PCSS soft shadows | Implemented in the combined candidate; gameplay pending | I (pre-RT) graphics hooks and contact-shadow system | Directional shadow constants/filter, GPU resource contracts | Medium-high | 5 |
| G06 | Contact shadows | Implemented in rc3 candidate; gameplay pending | L / I `vk_contact_shadow` | Camera/depth, shadow-resolve/blur matching, barriers | Medium-high | 5 |
| G07 | Directional volumetric lighting | Implemented in rc3 candidate; gameplay pending | L / I `vk_volumetric_light` | Camera and directional shadows, HDR/NR composition | High | 8 |
| G08 | Point-light volumetric fog | Implemented in rc3 candidate; gameplay pending | L / I `point_volumetric.comp` | Point-light capture, scene depth and volumetric integration | High | 9 |
| G09 | Directional shadow filtering | Implemented in rc3 candidate; gameplay pending | L / I `shim/engine/graphics.cpp` | Version-checked guest hooks, cascade seams, shadow texel scale | Medium-high | 4 |
| G10 | Directional/local shadow resolution | Directional/local resolution controls implemented in rc3; gameplay pending | L / I graphics hooks | Allocation dimensions and dependent shader constants must agree | Medium-high | 4 |
| G11 | Point-light shadows | Screen-space local-light shadows implemented in rc4; gameplay pending | L / I `vk_point_shadow` | Matched pre-RT shader/descriptor ABI; snapshots, proxies, retirement; no off-screen geometry | High | 10 |
| G12 | Occlusion correctness fixes | Fresh/complete visibility policy implemented in rc4; gameplay pending | L / I `occlusion_model`, `vk_occlusion` | Frame age, query availability, owned late writes, existing hysteresis | Medium | 3 |
| G13 | Output-preserving renderer optimizations | Reviewed scope implemented: conditional camera inverse; prior optimizations retained | I camera inverse; existing baseline optimizations | Preserve matrix/toggle/log output; no performance claim | Variable | Continuous, separate from visual effects |

Retain credits: Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine).
BloodborneX is a changed, unofficial fork; upstream SPDX notices remain in ported code.

## Manual gameplay acceptance

Use the separate local test package, not an existing installation. Choose a dump
without moving or modifying it and a separate save folder. Supply your own NR
model only if testing NR. No model is shipped.

1. Start with GTAO off and SSAO on; compare with stable v1.0.3 in a fixed scene.
2. Enable GTAO, restart, confirm the GTAO module log and visible AO change. A module
   log alone does not prove the game's draw used it.
3. With Picture tuning on, compare AO strength 0, 0.5 and 1; verify no double AO.
4. Repeat native rendering, DLAA, DLSS Balanced/Performance, FSR, NR off and NR
   single/multipass. Inspect silhouettes, packed-depth edges, foliage and sky.
5. Restart with GTAO off again using the same pipeline cache; original SSAO should
   return. Disable SSAO itself and verify neither AO implementation is forced on.
6. Test resizing, area transitions, menus, frame generation and SDR/HDR where
   supported; review validation messages and run an extended session.

See `VALIDATION.md` beside this file for actual build/test results and output paths.
