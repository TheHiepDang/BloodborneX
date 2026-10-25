# BloodborneX 1.0.2-nr.rc5 — DLSS-G HDR test build

This is a prerelease for [issue #2](https://github.com/TheHiepDang/BloodborneX/issues/2).
It ports Awakenchan's DLSS-G HDR presenter input from commit `08173e6` onto the
BloodborneX 1.0.1 NR/FSR engine. It is **not HDR gameplay validated yet**.

## Install and test

1. Extract the entire Windows ZIP into a new folder. Keep the existing installation
   and its settings, saves, mods and NR model untouched.
2. Run `Install Streamline.cmd` in the extracted folder. It downloads NVIDIA's
   official Streamline SDK 2.14.16, checks the archive SHA-256, installs the seven
   matched runtime DLLs and their licence texts, and backs up any old Streamline
   files in `engine/out/backups`. A mixed or incomplete set is refused by the
   launcher.
3. On an RTX 40/50 GPU with an HDR display, choose DLSS Frame Generation and
   enable HDR output. Restart the game after changing the initial DLSS-G selection.
4. Test HDR10 and scRGB if your display supports both. Try NR off and on, menus,
   loading screens, fullscreen/window transitions and resizing. Play for at least
   several minutes, since the upstream comparison log encountered an asynchronous
   present failure after about 26 seconds.
5. Attach `logs/last_session.log` to issue #2 and report the GPU, driver, HDR
   format, whether generated frames appeared, visual issues, and how long the
   session stayed stable. A short video is helpful if a visual issue occurs.

DLSS-G is off by default. FSR 3.1 still refuses HDR output in this test build.
The FSR allocation-retirement fix, NR Performance mode and single NR model path
are preserved. The proprietary NR model is not included.

This package does not bundle NVIDIA's Streamline DLLs. The installer retrieves
them from NVIDIA's pinned release on the tester's machine. The fork's Discord
release notifications, launcher community links, sign-in and profile status
features have been removed.
