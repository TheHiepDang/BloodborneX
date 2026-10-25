# BloodborneX

BloodborneX builds on [Awakenchan's Lantern Engine](https://github.com/Awakenchan/LanternEngine), with its own experimental native Vulkan Neural Rendering and frame-generation work on Windows. The original launcher and engine work remain credited to Awakenchan and upstream contributors.

**No game files are included.** You need your own decrypted Bloodborne dump (CUSA03173) with update 1.09.

## Install

1. Download the Windows package from the [latest release](https://github.com/TheHiepDang/BloodborneX/releases/latest) and unzip it.
2. If using Neural Rendering, supply your own runtime at **`dlss/nvngx_dlssnr.dll`**. Choose the unmodified October 16 DLL for **GeForce RTX 50-series**, or the **openNR FP16 compatibility** DLL for other RTX cards. Install one DLL directly in `dlss/`; the engine detects the GPU for runtime handling. The DLL is **not included**. See [accepted builds and hashes](docs/NR-RELEASE.md).
3. DLSS Frame Generation's matched NVIDIA Streamline 2.14.16 runtime is included in the Windows package. Select it in Graphics if you have a supported RTX GPU. See the [HDR test instructions](docs/DLSS-FG-HDR-PRERELEASE.md).
4. Run `Lantern Engine.exe`, select your game folder, and press **Play**. Enable Neural Rendering or DLSS Frame Generation in the graphics settings as desired.

See [release notes](docs/NR-RELEASE.md) for requirements and limitations, or the [development handoff](docs/DLSSNR-HANDOFF.md) for technical context.

## Prepare a release

Double-click **Prepare Release.cmd** in the project root. Enter the new version and a short description. The first run also asks for the applied engine source folder (the folder with `build.sh`); it remembers that path locally. PowerShell 7.4+, .NET SDK 8+ and MSYS2 CLANG64 build dependencies are required.

The script compiles the engine and launcher, verifies matching versions and metadata, runs package checks, and creates **`releases/<version>/Upload`**. Upload every file in that folder without renaming anything. It does not publish automatically or replace your game installation. See [release preparation](docs/RELEASING.md).

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
