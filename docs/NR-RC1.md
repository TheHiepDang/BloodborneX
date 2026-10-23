# BloodborneX / Lantern Engine NR — local RC1 checkpoint

23 October 2026. Windows, Vulkan, NVIDIA GeForce RTX 3070 (8 GB).

This is a local release-candidate checkpoint, not a published release or broad
hardware certification. The user considers the current image suitable for a first
candidate. No additional rendering changes or settings changes were made during
the gameplay observation or when recording this checkpoint.

## Candidate contents

- Native Vulkan DLSS Neural Rendering before DLSS SR/DLAA, with real engine depth
  and camera/object motion, downstream UI, and fail-safe ordinary-DLSS fallback.
- One to three chained NR passes, each with its own feature/history and output.
- White-point factory default **0.125**, with exact three-decimal saving/display.
  Existing saved choices are retained. Low white point and extra passes produced
  visibly stronger changes in the user's later tests; earlier weak-effect results
  at white point 4.21 are not a general assessment of multipass.
- Circular reset-to-default arrows on all in-game sliders and both launchers'
  shared slider controls, with factory-value tooltips.
- **Customize extra passes**: independent intensity, local structure, local tone,
  skin structure, and style for enabled passes 2/3. Pass 1 uses the main controls.
  Turning customization off restores inheritance without deleting saved overrides.
- Per-pass style: Use main style (default), Standard, Natural, Cinematic. The last
  three are community labels for runtime values 0/1/2, not official NVIDIA names.
- White point remains shared because encoding occurs once. Preset and automatic
  mask remain shared. No composition/placement overhaul is included in RC1.
- FSR FG overload pause/retry no longer repeatedly switches swapchain image count.
  This fixes a hitch source, not the model's GPU cost or all reported FG stutter.

## Read-only gameplay observation

Observation began at approximately 10:01:22 and ended at 10:03:39 Asia/Saigon,
using periodic native game-window snapshots, renderer logs and GPU telemetry.
The user traversed the area, turned the camera and fought multiple enemies.
No controls were injected and no settings were changed by the assistant.

The active saved profile during this observation was **1920×1080 output, 1280×720
NR/SR input, one NR pass, DLSS preset L, frame generation off**. White point was
**3.610**, not the new factory default; this was the user's current saved choice.
Intensity/structure/skin were 2, local tone 1.98. Chromatic aberration, depth of
field, motion blur, game AA, SSAO, dynamic shadows and SSR were enabled.

- Displayed FPS samples were approximately **41–44 FPS**. Renderer reporting
  windows read during/around the observation were approximately **39.6–41.9 FPS**.
- The logs recorded intermittent **50–94 ms** worst-frame spikes. Their source
  was not isolated; they should not be described as fixed or attributed to NR alone.
- GPU utilization samples were **98–99%**. NVIDIA telemetry reported
  **6131–6507 MiB of 8192 MiB** in use, **75–78°C**,
  roughly **175–179 W**, and **1815–1950 MHz** graphics clocks. Thermal and power
  slowdown reasons were not active in the two samples that queried those reasons.
- These observations suggest a GPU-side steady-state limit. They do not provide
  a measured time split between NR, SR, scene rendering and post-processing.
- No new NR evaluation failure, device-loss report, black-screen recurrence or
  obvious HUD corruption was observed in the checked samples/log interval.
- Periodic screenshots are not continuous video: they cannot certify absence of
  brief ghosting, flicker, disocclusion errors or individual dropped frames.
- GPU memory grew as the player traversed the scene; this short run does not
  establish either a leak or long-session memory stability.

## Verification carried forward

- Engine and both Windows launchers compile; WPF retains its existing warnings.
- Settings tests cover resolution rules, precision, defaults, custom pass/style
  inheritance, independent values, INI round-trip, restoration and input bounds.
- UI composition, motion-history and upscaler-fallback tests pass.
- Production NR composition passes HDR/LDR identity, near-black, alpha-preservation
  and nonidentity-model GPU tests on the RTX 3070.
- All 37 Python patch/resolution tests pass.
- The regenerated maintained engine patch applies to a clean upstream preview tag.
- The user confirmed the reset/custom-parameter controls behaved correctly.
  Mixed-style multipass visual quality has not been exhaustively assessed.

## Deferred investigations — not blockers for this local checkpoint

1. Record separate GPU timings for scene work, NR, SR and composition; correlate
   frame-time spikes with streaming, shader compilation and synchronization.
2. Controlled same-scene A/B tests of SR preset K versus current L and 540p versus
   720p NR/render input, with visual quality checked rather than assumed equivalent.
3. Consider decoupling model-only NR resolution from scene/SR input resolution,
   using matched residual transfer. This is not implemented and has no measured
   performance gain in this build.
4. A/B expensive optional scene/post effects before optimizing their code paths.
   The observation used these effects on; no benefit from disabling them was measured.
5. Longer sessions and camera cuts, loading/death/teleport transitions, HDR displays,
   ultrawide and 1440p/4K outputs, repeated live changes, and other GPUs.
6. Separate FSR FG pacing/overload validation. Standard DLSS FG cannot be validated
   on this RTX 3070; initial Streamline activation still requires a restart.

No speculative optimization above was implemented for RC1, and no FPS improvement
is promised. The next code change should be driven by a measured bottleneck.

## Launch and distribution

The earlier **Start Bloodborne NR.cmd** test shortcut depends on the development
workspace. For the portable candidate use the package under
`releases/0.9.9-nr.rc1` and follow [NR-RELEASE.md](NR-RELEASE.md). The package has
clean defaults, not the user profile used in the observation above.

The unsigned, unofficial `nvngx_dlssnr.dll` remains user-supplied and must **not**
be committed or included in release packages. Only the open-source Vulkan
forwarder and its required GPL/RenoDX MIT notices are packaged. Repository ignore
rules and release-asset checks exclude the proprietary runtime's exact filename.

Runtime validation remains tied to the inspected version 310.9.16.0, SHA-256:
`CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED`.
