# Tier 1 live toggles: first batch

Cumulative candidate: 1.0.4-nr.rc5, after the complete rc4 Tier 1 supplement.
Baseline main remains 5710aeb4626cc5f86ba0730b9afc3e1de5c4442d with inherited
G01-G13 work preserved. No release or installed-game changes.

## Controls

| Feature | Overlay behavior | Implementation boundary |
| --- | --- | --- |
| G03 SSGI | Live on/off; strength already live | Separate compute passes, SSGI history reset |
| G06 contact shadows | Live on/off; strength already live | Existing shadow resolve/blur chain; independent of PCSS |
| G07 volumetric sunlight | Live on/off; intensity already live | Frame-local integration, tint and composition |
| G08 point-light fog | Live on/off; intensity already live | Light-memory and tint reset on mode transitions |
| G11 point-light shadows | Live on/off; strength already live | Per-draw snapshots/compute; original lighting when off |
| G01/G02 GTAO and quality | Restart required | Substituted cached graphics shader modules and HQ gather path |
| G04 stochastic SSR | Restart required | Three coordinated shader variants including composite/water |
| G05 PCSS, G09 filter/cascade | Restart required | Native guest hooks installed at startup |
| G10 shadow resolutions | Restart required | Guest pool and allocation hooks |
| G12/G13 correctness/optimization | Always active | No new user-facing toggle |

Use the **in-game graphics overlay** for live switches. The launchers configure
the next run; this batch does not introduce INI polling or launcher-to-engine IPC.
Both launchers mark the five switches as live in game. Existing keys, defaults,
save/load and custom preset behavior are retained. Startup snapshots remain for
compatibility but no longer gate the five live renderer paths or restart footer.
The two immutable RTX 3070 profiles remain unchanged.

## Runtime behavior

UI threads only edit existing atomic requested settings. The recording owner
latches the five flags once at the recognized display boundary; every pass in
the next scene sees that same snapshot. Several edits between frames coalesce.
Constructor initialization uses the same path. The no-camera diagnostic mode
also samples requests at display boundaries, retaining its existing missing-input
fallback rather than adding camera/upscaler resources.

Transitions cancel pending contact/point work, invalidate SSGI and SSR temporal state,
clear volumetric light memory and reset tint timing. They request the existing
NR model/look and downstream SR/FSR/TAA history reset through flag-only APIs.
Frame generation also starts its next job with a reset and skips interpolation
on that job, avoiding a generated blend across different effect settings.
Unchanged settings do not repeatedly invalidate histories. No NR implementation,
model, style coefficients, resource lifecycle or multipass pipeline is rewritten.

GPU resources/pipelines remain lazily created and cached after first use.
Turning an effect off stops its dispatch/capture; re-enabling reuses its resources
with invalid histories. Initial off settings allocate no effect images. Cached
VRAM is intentionally retained while off, preventing allocation churn. Resizing
continues to use existing completion-based retirement. No Scheduler::Finish,
GPU idle wait, cache purge, pipeline rebuild or gameplay file edit is added.
The first eligible use may compile a compute pipeline and hitch briefly.

Unsupported layouts/formats/missing inputs retain the existing fallback. A
previous GPU/pipeline failure remains latched for the session; toggling is not a
driver-recovery mechanism. The other restart-required flags remain startup-based.
Logs report the latched live flags at a frame boundary; this is not proof that
a supported scene actually dispatched every effect.

## Maintainable sources and build

Apply `tools/apply-tier1-live-toggles.py <full-engine-source>` **after** all four
Tier 1 supplements, ending with tier1-final-renderer. The maintained patch and
per-file preimage/output hashes are under engine/. Full source ZIPs already
contain the applied code and must not have older supplements reapplied.
Use existing build.sh/native CLANG64 tree and build-nr-release.ps1 packaging.

## Acceptance

1. Launch the separate rc5 candidate and choose a fixed lamp/sunlight scene.
   Toggle each of the five switches off/on repeatedly without restarting.
2. Change two or more switches together and apply custom presets containing
   them. The restart banner must remain off unless another setting needs it.
3. Check for stale SSGI/light trails, stale fog after re-enable, lighting dimming
   with point shadows off, and contact-shadow changes while PCSS stays enabled.
4. Repeat with DLAA, DLSS SR Balanced/Performance, NR off/single/multipass and FSR.
5. Resize/change presets while enabled and disabled, enter menus, change areas,
   and repeat with frame generation/HDR where supported. Check VRAM over many
   toggles; cached allocations may stay, but must not grow on every switch.
6. Save, exit and relaunch: all five values persist. Launcher edits affect the
   next launch. GTAO/SSR/PCSS/filter/resolution edits must still request restart.

Automated results and exact build path are in LIVE-TOGGLES-VALIDATION.md.
Gameplay appearance, live Vulkan synchronization and long-session stability
require the user's acceptance test.

Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine).
BloodborneX is a changed, unofficial fork; upstream license notices are retained.
