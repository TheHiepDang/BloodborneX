# BloodborneX v1.0.3

## Highlights

- **BloodbornePC distribution:** download `BloodbornePC-1.0.3-win64.zip`, extract the BloodbornePC folder and launch `BloodbornePC.exe`. The project remains BloodborneX, a changed/unofficial fork of Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine).
- **Updater compatibility:** published v1.0.1/v1.0.2 clients already recognize the BloodbornePC archive name. Existing internal launcher names, user settings/storage and update-family IDs are retained so discovery, installation and restart remain compatible. This is not a BloodborneX-only ZIP transition.
- **Dedicated NR page:** native and launcher controls for model input, intensity, per-pass tuning and expanded tone/detail/colour/stability adjustments.
- **Live Extra neural style:** changes apply after the slider settles, with a brief feature/history rebuild rather than a game restart. Zero restores stock coefficients; nonzero selects Old/HT. The model file on disk is unchanged, and incompatible tensor layouts are rejected safely.
- **Custom NR runtimes always allowed:** file hashes are diagnostic only. The user supplies one model at `dlss/nvngx_dlssnr.dll`; actual exports, initialization and evaluation determine availability. A missing/failing model falls back to ordinary DLSS.
- **Spec preset:** a right-aligned in-game tab with RTX 3070 SLOP and RTX 3070 - SLOP - Cinematic, plus multiple custom presets. Selectively save/export/import settings and delete saved user presets with confirmation. A read-only reference view has a fixed header, group selection and visible exclusion indicators. Applying a preset offers the existing restart action.

## Install and update

Download the complete **BloodbornePC-1.0.3-win64.zip**, not the source ZIP. Extract to a writable folder, run **BloodbornePC.exe**, select your own decrypted Bloodborne 1.09 dump and existing saves location, then Play. NR is optional and off by default. Supply your own NR model to enable it.

Existing installations can use the built-in updater. The retained `Lantern Engine.exe` and `Bloodborne Launcher.exe` are compatibility implementation details; do not delete them. Presets, settings, saves, mods and logs remain user data. The complete applied source is in **BloodbornePC-1.0.3-source.zip**. All third-party licenses and credits are retained. No game files, saves or proprietary NR model are distributed.

## Validation and limits

Native settings/preset tests, headless UI selection tests, NR style resource/lifecycle fixtures, both launcher builds and updater fixtures were exercised during development. Stable packaging additionally checks archive hashes/manifests, matching versions/metadata, executable discovery, bundled Python/native startup and Vulkan capability probing in a fresh extraction with spaces.

These checks do not certify full gameplay quality, HDR, frame-generation coexistence, original RTX 50 inference, file-browser/controller interaction or long-session stability. NR, HDR and frame generation remain experimental. See `VALIDATION.md`, `PACKAGE-CHECKS.json` and the source docs for the exact checks performed.


# Native NR controls

Included in BloodborneX v1.0.3. NR remains experimental; synthetic checks do not establish gameplay quality or stability.

The launchers and Insert/L3+R3 overlay now have a separate **NR** page. Existing
INI keys and independent pass 2/3 settings are retained. In both launcher layouts,
NR is separate from Graphics. The overlay's search and controller tab navigation
include the new tab.

## Using the new controls

Choose DLSS SR/DLAA, enable NR and provide the supported user-supplied runtime as
before. Select **Composition: Log transfer** on the NR page to enable the new
controls. **Legacy** remains the default for old and new profiles. It retains the
previous 75% luminance / 25% per-channel resolve and does not allocate edit-history
images. Log transfer leaves the existing model input, inference count and pre-SR
insertion point intact. It works at full and reduced model resolutions.

**Model intensity** remains 0–2. **Edit strength**, independently adjustable from
0–4, scales the model's log edits after inference. This can amplify an existing
edit substantially, without additional inference passes. It cannot create a new
neural detail or style when the model generated no change. Limits apply after
amplification; increase the relevant limit as well if it is constraining the edit.

Suggested first A/B: one model pass, unchanged style/intensity/input height,
Legacy versus Log transfer at edit strength 1, then 1.5–2. Keep stabilization off
until the visual effect is established. These are test starting points, not
gameplay-validated presets.

| Control | Range/default | Meaning |
| --- | --- | --- |
| Adjust NR's look | On | Off uses bounded neutral log transfer and bypasses look/stabilization settings |
| Edit strength | 0–4 / 1 | Scales neural edits; zero produces no edit |
| Brightening, Darkening | 0–4 / 1 | Independently scale positive/negative luminance edits |
| Colour change | 0–4 / 1 | Scales relative colour edits |
| Hue shift | 0–2 / 1 | Scales the component of the colour edit perpendicular to the source colour direction; does not rotate the whole frame |
| Shadows, Midtones, Highlights | 0–4 / 1 | Smooth source-luminance weights on neural edits |
| Max brightening, Max darkening | 0–8 stops / 2 | Final log-luminance bounds; **zero prevents that direction of luminance change** |
| Max colour change | 0–8 stops / 2 | Final per-channel chroma-edit bound; zero removes chroma edits |
| Near-black protection | 0–0.1 / 0.001 | Fade edits to zero near black; zero disables this gate |
| Large-scale tone, Fine detail | 0–3 / 1 | Separately scale a guided broad edit and its residual |
| Detail radius | 0.1–5% / 1.5% | Sparse nine-tap filter radius relative to the smaller model dimension |
| Halo suppression | 0–1 / 0 | Constrain amplified edits to neighbourhood edit extrema on the same depth surface |
| Halo next to dark objects | 0–1 / 0 | Reduce the fine edit beside darker source neighbours |
| Detail stability | Auto / Off / Always | Source-edge constraint on amplified detail; Auto acts when detail gain exceeds 1. This is not an inferred copy of the add-on's algorithm |
| Stabilize | Off / Static / Motion; default Off | Static accepts motion below 0.25 model pixels; Motion reprojects previous edits |
| Stabilize time | 0–500 ms / 60 | Exponential history decay using elapsed real-frame time; zero uses the current answer |
| Stabilize detail | On | Off retains the current fine residual while smoothing the remaining edit |

Limits are separate luminance/chroma constraints: a pixel channel's total gain can
exceed the luminance limit when a chroma edit is also present. Final log edits are
bounded to ±16 and composed RGB is bounded to finite RGBA16F magnitude. Alpha is
copied unchanged.

These controls are independently written equivalents. They do not claim identical
RenoDX behavior or defaults. The **Extra neural style** prototype edits loaded model coefficients, with zero
as a bypass. It applies live after a GPU drain and feature rebuild without restarting the game. The model file on disk is unchanged; no model binary or weights are bundled or downloaded. After-SR insertion and new HDR proxy/exposure
strategies are not part of this change. The live caller still supplies `hdr=false`;
synthetic HDR shader tests do not establish live HDR proxy use.

## Rendering and resource handling

Log transfer runs one compute edit stage after the final model pass. It separates
log luminance and relative colour, applies optional spatial/tonal adjustments, then
optionally reprojects edit history. Original scene colour is never blurred.
Composition applies the edit to the untouched scene before ordinary SR/DLAA and UI.
The reduced-size resolve retains depth-guided reconstruction and interpolates log
edits on that path. The raw model outputs remain available for plane capture and
the full-size four-band diagnostic.

Motion uses the engine's previous-minus-current convention in model pixels. Edit
history rejects offscreen/nonfinite motion, depth changes and incompatible settings.
It resets on engine history resets, resource size/model changes, return from Legacy
and frame gaps above 250 ms. Historical edits are neighbourhood-constrained. These
are heuristic rejection rules; they need moving-character, camera-cut, particles,
occlusion and dark-scene gameplay tests.

The new images allocate lazily on first Log transfer use: one RGBA16F edit image
and two RGBA32F history images, approximately **40 bytes per model pixel** (about
35.2 MiB at 1280×720). They are retained after returning to Legacy until model
resources are rebuilt. Spatial/temporal GPU overhead is not yet benchmarked at
gameplay resolutions. Overall NR timing includes this stage in the resolve segment.

## Source integration

Launcher changes live directly in this repository. Native changes require a
complete applied engine checkout. The durable native changes are:

- `engine/nr-look-controls.patch`: supplemental source patch, including GPU and settings regressions.
- `engine/nr-look-controls-baseline.json`: exact baseline and modified hashes for each affected file.

Apply the supplement to the compatible **applied** source, after matching its
baseline hashes. It is not a patch for the patch-only `engine/` directory. Do not
apply it to an installed game or an unrelated engine revision. For a matching
checkout:

```powershell
git -C 'C:\path\to\applied-engine' apply --check 'C:\path\to\BloodborneX\engine\nr-look-controls.patch'
git -C 'C:\path\to\applied-engine' apply 'C:\path\to\BloodborneX\engine\nr-look-controls.patch'
```

Build and package the launcher and this modified engine together before game
testing. An older engine ignores the new settings. The v1.0.3 source archive
contains the complete applied engine; do not reapply the supplement to it.


## Presets and validation

The right-aligned Spec preset tab includes two provided RTX 3070 profiles and a separate custom library. Save or export selected settings through the read-only reference view; graphics default included. Import validates the complete preset before application. Apply, overwrite and deletion require confirmation.

Native settings, headless preset UI, style fixtures and GPU shader checks were exercised during development. Release archive and portable startup checks do not certify gameplay, HDR, controller interaction or long-session stability. See the versioned release notes and packaged validation reports.
