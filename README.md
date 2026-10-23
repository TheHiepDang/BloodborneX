# BloodborneX

BloodborneX is a direct fork of [Awakenchan's Lantern Engine](https://github.com/Awakenchan/LanternEngine), adding experimental native Vulkan DLSS Neural Rendering on Windows. The original launcher and engine work are by Awakenchan and the upstream contributors.

**No game files are included.** You need your own decrypted Bloodborne dump (CUSA03173) with update 1.09.

## Install

1. Download the Windows package from the [latest release](https://github.com/TheHiepDang/BloodborneX/releases/tag/release) and unzip it.
2. Supply your own `nvngx_dlssnr.dll` in the extracted `dlss/` folder. This unofficial NVIDIA runtime is **not included**.
3. Run `Lantern Engine.exe`, select your game folder, and press **Play**. Enable Neural Rendering in the DLSS settings.

See [release notes](docs/NR-RELEASE.md) for requirements and limitations, or the [development handoff](docs/DLSSNR-HANDOFF.md) for technical context.

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
| Game patches | Kyo, Lance McDonald, auser1337, illusion, emoose and other community members |
| Sound | [LibAtrac9](https://github.com/Thealexbarney/LibAtrac9) |
| In-game menu | [Dear ImGui](https://github.com/ocornut/imgui) |
| Controller mappings | [SDL_GameControllerDB](https://github.com/mdqinc/SDL_GameControllerDB) |
| Trophy list | [pyL1nx/bloodborne_pc](https://github.com/pyL1nx/bloodborne_pc) |
| UI library | [WPF-UI](https://github.com/lepoco/wpfui) |

## License

The launcher is GPL-2.0-or-later (see [LICENSE](LICENSE)). The engine is GPL-3.0-or-later; its licenses are in the package's `licenses/` folder.
