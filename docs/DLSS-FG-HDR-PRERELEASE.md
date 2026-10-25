# BloodborneX 1.0.2-nr.rc6 — DLSS-G HDR test build

This is a prerelease for [issue #2](https://github.com/TheHiepDang/BloodborneX/issues/2).
It ports Awakenchan's DLSS-G HDR presenter input from commit `08173e6` onto the
BloodborneX 1.0.1 NR/FSR engine. It is **not HDR gameplay validated yet**.

## Install and test

1. In a 1.0.1 installation, set **Update channel** to **Beta**, then use **Check now**
   and **Update now** after this prerelease is published. Alternatively, extract
   the entire Windows ZIP into a new folder. Keep saves and mods backed up.
2. The Windows package includes NVIDIA's matched Streamline 2.14.16 runtime and
   licence texts. There is no separate Streamline setup step. On the first launch
   after an in-place update, older extra Streamline plugins are backed up under
   `engine/out/backups` so they cannot be loaded with the new set.
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

The fork's Discord release notifications, launcher community links, sign-in and
profile status features have been removed.
