# BloodborneX

BloodborneX is a direct fork of [Awakenchan's Lantern Engine](https://github.com/Awakenchan/LanternEngine), adding experimental native Vulkan DLSS Neural Rendering on Windows. This is a changed, unofficial fork. The original launcher and engine work are by Awakenchan and the upstream contributors.

Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine)

**No game files are included.** You need your own decrypted Bloodborne dump (CUSA03173) with update 1.09.

## Install

1. Download **`BloodbornePC-1.0.3-win64.zip`** from the [latest release](https://github.com/TheHiepDang/BloodborneX/releases/latest) and extract the entire **BloodbornePC** folder to a writable location.
2. Run **`BloodbornePC.exe`**, select your own decrypted Bloodborne 1.09 game folder and existing save location, then press **Play**.
3. For optional Neural Rendering, supply your own **`dlss/nvngx_dlssnr.dll`** and enable it under **NR** in the launcher or the in-game menu (**Insert / L3+R3**). The model is never bundled or downloaded. Custom builds are always allowed; missing exports or failed initialization/evaluation fall back to ordinary DLSS. Extra neural style additionally requires a compatible tensor layout.

For RTX 30-series, the project's runtime recommendation is [DLSS5-RTX-3080-laptop](https://github.com/lmxxf/DLSS5-RTX-3080-laptop). Supply the model yourself; it is not included.

## What's new in v1.0.3

- Separate NR page, expanded look/detail/colour/stability controls and live Extra neural style.
- Right-aligned **Spec preset** tab, two provided RTX 3070 profiles, and multiple custom presets with selective save/export/import and delete confirmation.
- Read-only preset selection view with a fixed header, grouped settings and visible inclusion/exclusion indicators.
- BloodborneX product branding and **BloodbornePC** distribution names.

See [v1.0.3 notes](https://github.com/TheHiepDang/BloodborneX/releases/tag/v1.0.3) for details and validation limits. NR, frame generation and HDR remain experimental; build/portable tests do not establish gameplay stability on every GPU.

## Automatic updates and naming

**BloodbornePC is the distribution name; BloodborneX remains the project name.** The published v1.0.1/v1.0.2 updater already recognizes `BloodbornePC-*-win64.zip`, so existing installations can discover v1.0.3. This release does not require a second legacy-named ZIP.

`BloodbornePC.exe` is the new entry point. The internal `Lantern Engine.exe` and `Bloodborne Launcher.exe` names remain deliberately compatible with old updater executable discovery and shortcuts. Existing configuration filenames, per-user storage locations and the `lantern-native-nr-` metadata family remain unchanged. New versions continue accepting the older archive prefixes as well.

## Video showcase

[![Watch the video](https://img.youtube.com/vi/E719AZr_Za4/hqdefault.jpg)](https://www.youtube.com/watch?v=E719AZr_Za4)

## Credits

| | |
|---|---|
| **Lantern Engine** | [**Awakenchan**](https://github.com/Awakenchan/LanternEngine): original launcher, engine development and performance work |
| Engine starting point | [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc) (built on [bbport](https://github.com/deadinside28/bloodborne_pc) and [shadPS4](https://github.com/shadps4-emu/shadPS4)) |
| VRAM cleanup and frame pacing | adapted from [IFreemz/shadPS4-Bloodborne-DLSS-FSR](https://github.com/IFreemz/shadPS4-Bloodborne-DLSS-FSR) |
| Crash and controller fixes | adapted from [AwesomeObserver](https://github.com/AwesomeObserver/bloodborne_pc), [Supermedo](https://github.com/Supermedo/bloodborne_pc) and [deadinside28](https://github.com/deadinside28/bloodborne_pc) |
| Cheats, online sign-in, occlusion culling and trophy research | [droogie/bbhost](https://github.com/droogie/bbhost) |
| Online servers | [The Hunter's Dream](https://thehuntersdream.com) |
| FSR 3.1 and FSR 4 on Vulkan | [FireBurn/FSR-Vulkan](https://github.com/FireBurn/FSR-Vulkan) and the AMD FidelityFX SDK |
| FSR 4.1.1 | [fsr4vk](https://github.com/dvj5411/fsr4vk), with the AMD driver fix from [0xCydral](https://github.com/0xCydral/bloodborne_pc) |
| DLSS | NVIDIA DLSS SDK |
| NR FP16 compatibility runtime reference | [clshortfuse/openNR](https://github.com/clshortfuse/openNR); user-supplied runtime, not bundled |
| Game patches | Kyo, Lance McDonald, auser1337, illusion, emoose and other community members |
| Sound | [LibAtrac9](https://github.com/Thealexbarney/LibAtrac9) |
| In-game menu | [Dear ImGui](https://github.com/ocornut/imgui) |
| Controller mappings | [SDL_GameControllerDB](https://github.com/mdqinc/SDL_GameControllerDB) |
| Trophy list | [pyL1nx/bloodborne_pc](https://github.com/pyL1nx/bloodborne_pc) |
| UI library | [WPF-UI](https://github.com/lepoco/wpfui) |

## License

The launcher is GPL-2.0-or-later (see [LICENSE](LICENSE)). The engine is GPL-3.0-or-later; its licenses are in the package's `licenses/` folder.
