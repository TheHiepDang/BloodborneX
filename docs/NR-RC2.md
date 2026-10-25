# BloodborneX 0.9.9-nr.rc2

Built 25 October 2026. This candidate includes the RTX 50 NR runtime-selection fix and the FSR 3.1 frame-generation memory fix.

## Fixed

- **Original NR runtime on GeForce RTX 50-series:** the engine recognizes the unmodified October 16 runtime, rather than rejecting it because it differs from the openNR FP16 compatibility build. Users choose the appropriate DLL and place it at `dlss/nvngx_dlssnr.dll`: unmodified October 16 for GeForce RTX 50-series, openNR FP16 compatibility for other RTX cards. The actual Vulkan GPU selects compatibility handling; subfolder DLLs are not searched.
- **NR diagnostics:** logs identify the selected runtime path, size, SHA-256, identity and GPU route. Original-runtime support is labelled experimental; unknown files remain rejected by default.
- **FSR FG memory growth:** per-frame descriptor sets and constant buffers now receive frame IDs and retire after GPU completion. They no longer accumulate for the context lifetime.

## Installation

Extract the full Windows package into a new folder. Keep your existing game and save folders. Supply your own NR runtime; neither the original nor the compatibility model DLL is bundled or downloaded by BloodborneX. Use the included `START-HERE.md` for exact accepted hashes and file locations.

FSR 3.1 is the supported frame-generation path for this candidate. NR and FG start disabled in a fresh configuration. No unofficial DLSS FG proxy was integrated. The current static colour behavior is retained; the unfinished live-colour prototype is excluded.

## Validation and limits

- The FSR leak regression fails with the old lifecycle behavior and passes after the fix for RGBA8 and RGBA16F over 64 frames each. The user's roughly five-minute NR+FSR FG session showed memory falling from 3609 to 3109 MiB, no texture-memory pressure reports and a clean exit.
- Both actual NR DLLs pass identity, required-export and Vulkan-initialization checks. C++/C# routing checks, NR LDR/HDR composition/scaled shader checks and both launcher builds pass. Original-runtime inference and visible effect on the reporting RTX 5080 still require a fresh gameplay retest.
- The local Vulkan validation layers are unavailable. Successful GPU execution and initialization are not validation-layer coverage or proof of visual correctness.
- Package verification is recorded in the accompanying `VALIDATION.md`. This is a local release candidate; no GitHub publication or push is performed by building it.

The source archive contains the filtered applied engine snapshot and current launcher source. Build the applied snapshot directly rather than reapplying the historical cumulative patch. Credits and license notices are included, including openNR's compatibility-runtime reference.
