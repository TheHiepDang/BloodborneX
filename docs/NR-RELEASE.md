# BloodborneX native Neural Rendering — RC1

Local Windows x64 candidate, 23 October 2026. Version `0.9.9-nr.rc1` identifies this
fork's candidate; it does not claim to supersede upstream Lantern version numbers.

## Start here

1. Extract the entire `LanternEngine-0.9.9-nr.rc1-win64.zip` to a writable folder.
   Keep it separate from older engine packages. Do not run inside the ZIP.
2. Run **Lantern Engine.exe**, select your own decrypted Bloodborne 1.09 game
   folder, and explicitly choose your existing saves folder if continuing a save.
   This package contains no game files or saves. Back up saves before testing.
3. To use NR, put your own `nvngx_dlssnr.dll` in the extracted package's `dlss`
   folder, beside the included `nvngx.dll_dlssnr.dll` forwarder. The model is not
   included, downloaded or modified by this package.
4. Select DLSS, enable DLSS Neural Rendering, and start with one pass and 720p
   NR input. NR ships **off**, frame generation **off**, and white point **0.125**.
   Native AA/DLAA forces native input. NR is bypassed for FSR/TAA and invalid scenes.
5. The in-game menu uses Insert or L3+R3. For Moonlight from a Mac, a controller's
   L3+R3 avoids the missing Insert key. Each slider's circular arrow restores its
   factory value. Customize extra passes enables independent pass 2/3 controls.

The validated model is x64 DVS production version `310.9.16.0`, 276,162,560 bytes:

`CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED` (SHA-256).

Missing/rejected models disable NR only. Ordinary DLSS should remain usable.
Use this unofficial unsigned model only if you trust its provenance. No model
download link or redistribution permission is implied.

## Package and updates

Python and .NET runtimes and native DLL dependencies are included; MSYS2, the .NET
SDK and a separate Python installation are not required to play. A compatible
Vulkan GPU driver is required. The tested hardware is an RTX 3070, not all RTX cards.

The launcher checks **TheHiepDang/BloodborneX**, not Awakenchan/LanternEngine.
NR packages accept only full Windows packages whose attached `package.json`
declares `update_family: bloodbornex-native-nr-v1` and an engine ID beginning
`lantern-native-nr-`. Keep `LOCAL-NR-RELEASE` in place. Source ZIPs and launcher-only
updates are not accepted as NR runtime packages. Nothing has been published by
the local packaging operation. Until compatible releases exist, there is no update.

For publishing later, attach the `*-win64.zip`, `package.json` and `SHA256SUMS.txt`
to a GitHub release tagged `v0.9.9-nr.rc1` (mark it prerelease). Attach the source
ZIP as a separate source asset. Increment the version for the next candidate.
Release publication and license/security review are separate approval steps.

## Scope and limitations

- One to three actual chained model evaluations, with independent temporal history.
  More passes cost GPU time; photorealism or stronger changes are not guaranteed.
- The current composition deliberately retains much of the original image. It is
  not the complete modern OptiScaler/RenoDX composition pipeline.
- The selected NR resolution is the scene/SR input resolution, not an independently
  downscaled model-only input. White point, preset and automatic mask are shared.
- Automatic mask should stay on. Style names are community labels, not NVIDIA names;
  undocumented preset values can alias.
- RC1 deliberately uses the NR-aware Python starter. Do not add `bb-start.exe` from
  an older/native build: its render-size preparation does not yet have NR parity.
- Ordinary DLSS SR and built-in FSR 3 are included. Optional Streamline/DLSS-G,
  FSR 4/4.1.1 assets and bbbridge online helper are not bundled in this candidate.
  Do not expect those optional modes to work simply because the launcher lists them.
- Standard DLSS frame generation does not support the tested RTX 3070. FSR FG had
  reported stutter; its swapchain-churn fix is not a general pacing guarantee.
- Current validation is a local RC, not broad GPU/HDR/ultrawide certification.

See `NR-RC1.md` for measured observations and `DLSSNR-HANDOFF.md` for development
context. `BUILD-PROVENANCE.json` and `FILES-SHA256.txt` identify package inputs.
