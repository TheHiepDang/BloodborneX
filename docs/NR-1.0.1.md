# BloodborneX v1.0.1

This release contains NR Performance mode, the FSR 3.1 frame-generation memory fix, and experimental support for the unmodified October 16 NR runtime on GeForce RTX 50-series.

## Changes

- **NR Performance:** the Performance checkbox beside NR input height reduces model resolution while retaining scene/SR input size. The existing 0.75 model scale evaluates a 720p NR scene at 960×540. Performance gains vary by scene and hardware.
- **FSR frame-generation memory:** transient constant buffers and descriptor sets retire after GPU completion instead of accumulating for the context lifetime. The user's roughly five-minute NR+FG test showed bounded memory and a clean exit.
- **RTX 50 NR runtime:** the actual Vulkan GPU selects compatibility handling. Both documented runtime identities are recognized; the unmodified runtime remains experimental and needs RTX 5080 inference/visual retesting.
- **One NR DLL path:** choose the correct model and install it at `dlss/nvngx_dlssnr.dll`. Use the unmodified October 16 build for GeForce RTX 50-series or the openNR FP16 compatibility build for other RTX cards. No per-GPU subfolders are searched.
- **Release and updater:** the launcher reports `v1.0.1 NR`, the package declares `1.0.1`, and GitHub uses tag `v1.0.1`. A separate `package.json` and standard `*-win64.zip` asset allow existing NR RC2 launchers to recognize this release. Update installation requires confirmation.

## Installation and updates

Download `LanternEngine-1.0.1-win64.zip` and extract the entire package. The old NR launcher can also update through Settings → Check now once this release is published. Settings, saves, mods and user-supplied model files are retained by the updater. New installations start with NR and frame generation off.

No game, saves or proprietary NR model DLL is bundled or downloaded. Accepted DLL hashes and setup are in `START-HERE.md`. Credits and license notices, including the openNR reference, are included.

## Validation and scope

The engine is the verified single-path RC2 build, SHA-256 `5A644230E56E1450E65C763854609F7581222FA7F2D130B390AEF19C36B28EE7`. Versioning and packaging do not replace it with an older engine. Release packaging checks are recorded in `VALIDATION.md`.

FSR 3.1 is the supported frame-generation path here. No unofficial DLSS FG proxy is integrated. The existing static colour behavior is retained; the unfinished live-colour prototype remains excluded. Vulkan validation layers are unavailable locally. Initialization alone does not prove NR inference or visible improvement on RTX 50-series.

The separately attached source ZIP contains the complete applied engine snapshot. Build that snapshot directly; do not reapply the historical cumulative patch.
