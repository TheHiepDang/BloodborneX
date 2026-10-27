# BloodborneX

BloodborneX is an independently modified fork based on Lantern Engine by Awaken (https://github.com/Awakenchan/LanternEngine). It is not an official Lantern Engine release. This fork adds experimental native Vulkan Neural Rendering and frame-generation work on Windows. The upstream [NOTICE](NOTICE) and separate third-party licence terms are retained.

**No game files are included.** You need your own decrypted Bloodborne dump (CUSA03173) with update 1.09.

## Install

1. Download the Windows package from the [latest release](https://github.com/TheHiepDang/BloodborneX/releases/latest) and unzip it.
2. If using Neural Rendering, supply your own runtime at **`dlss/nvngx_dlssnr.dll`**. Choose the unmodified October 16 DLL for **GeForce RTX 50-series**, or the **openNR FP16 compatibility** DLL for other RTX cards. Install one DLL directly in `dlss/`; the engine detects the GPU for runtime handling. The DLL is **not included**. Accepted model hashes are listed in `START-HERE.md` inside the package.
3. DLSS Frame Generation's matched NVIDIA Streamline 2.14.16 runtime is included in the Windows package. Select it in Graphics if you have a supported RTX GPU. HDR and NR still need hardware-specific testing; read the packaged guide.
4. During the naming transition, run `Lantern Engine.exe`, select your game folder, and press **Play**. Enable Neural Rendering or DLSS Frame Generation in the graphics settings as desired.

Read `START-HERE.md` and `RELEASE-NOTES.md` in the downloaded package for requirements and limitations.

## Prepare a release

Double-click **Prepare Release.cmd** in the project root. Enter the new version and a short description. The first run also asks for the applied engine source folder (the folder with `build.sh`); it remembers that path locally. PowerShell 7.4+, .NET SDK 8+ and MSYS2 CLANG64 build dependencies are required.

The script compiles the engine and launcher, verifies matching versions and metadata, runs package checks, and creates **`releases/<version>/Upload`**. Upload every file in that folder without renaming anything. It does not publish automatically or replace your game installation. Use the pinned source reconstruction helper before selecting an engine source tree.

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


## Automated builds and releases

Pull requests, weekly checks and manual validation build the Windows x64 portable ZIP and
source ZIP, with matching `package.json`, checksums and provenance. After review and merge
into `main`, a tag such as `v1.0.2` or `v1.0.2-nr.rc8` triggers verified, draft-first publication.
Manual and PR runs never publish. Linux x86_64 is a separate experimental validation target;
it does not block Windows releases or promise RTX/NR feature parity.

Native revisions, ordered patches and dependency hashes are in `tools/ci/inputs.json`.
For local source preparation, run `python tools/ci/reconstruct.py --destination _ci/native-engine
--report _ci/engine-source.json` (one command), then use `Prepare Release.ps1` with that engine
folder. CI supplies .NET 8.0.426, MSYS2 CLANG64 and the licensed Visual Studio 2022 runtime
redistributable files required for a portable Streamline package. No game files or NR models
are included. Internal development notes are kept locally and excluded from Git and archives.

### Maintainer commands

Enable GitHub Actions and permit the SHA-pinned checkout, MSYS2, .NET and artifact actions.
The build jobs use read-only permissions; only the tag-triggered publisher requests
`contents: write` through the built-in `GITHUB_TOKEN`. No PAT or repository secret is needed.
In branch protection, require **Validate BloodborneX / build / prepare** and
**Validate BloodborneX / build / windows**. Keep **Linux experimental** optional.
Scheduled/manual workflows become available after this change reaches the default branch.

For validation without publishing, open a PR or run:

```sh
gh workflow run validate.yml --repo TheHiepDang/BloodborneX --ref main
gh run list --repo TheHiepDang/BloodborneX
gh run watch <run-id> --repo TheHiepDang/BloodborneX --exit-status
```

When a reviewed commit is on `main`, release from a clean, current checkout. Replace the
example version with an unused, higher version. Stable tags use `v1.0.3`; prereleases use
`v1.0.3-nr.rc1`, matching the launcher's existing version rules.

```sh
git switch main
git pull --ff-only
git status --short
git tag -a v1.0.3-nr.rc1 -m "BloodborneX v1.0.3-nr.rc1"
git push origin refs/tags/v1.0.3-nr.rc1
```

Before tagging, ensure the development/native changes and CI changes have both completed
normal review and reached `main`; a historical release tag is not proof of matching NR source.
The release workflow checks tag/commit identity and `main` ancestry, compiles fresh source,
runs CPU and portable startup checks, creates a draft, then verifies uploaded assets by
release/asset ID and downloaded SHA-256 before publishing. RC tags become prereleases.
It refuses existing releases/drafts, incomplete assets and changed tags.

For a build failure before draft creation, inspect `gh run view <run-id> --log-failed` and
the diagnostics artifact; rerun only after the cause is understood. Source fixes need a new
reviewed commit and a fresh version tag. Never move an existing tag. If creation or upload
failed, inspect the release ID printed in the job summary with
`gh api repos/TheHiepDang/BloodborneX/releases/<id>` as a maintainer. An omitted draft in a
release listing is not proof that none exists. Do not retry publication, overwrite or delete
the draft automatically; resolve it deliberately and use a fresh version for the next release.

Verify downloaded files using `sha256sum -c SHA256SUMS.txt` (Linux) or
`Get-FileHash <downloaded-file> -Algorithm SHA256` (PowerShell), and match the source commit
in `BUILD-PROVENANCE.json` to the tag. A successful hosted run checks package startup and
dependencies; NR/FG/HDR gameplay still requires a compatible GPU and a user-supplied game.
The Ubuntu 24.04 Linux tarball remains experimental, independently audited and tested under
Xvfb; Steam Deck and RTX feature parity require further hardware validation. AppImage is
not used because the inherited Nix/AppImage dependency inputs are not yet pinned.
