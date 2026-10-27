#!/usr/bin/env bash
# CPU regressions must run; HDR/NR shader execution requires a suitable GPU.
set -euo pipefail
cd -- "${1:?applied native source required}"
tests=(pipeline-cache-blob-test pipeline-archive-test motion-history-test occlusion-test
       motion-shader-test image-overlap-test settings-test ui-composition-test engine-hooks-test
       engine-menu-memory-test rebirth-refund-test pc-input-test widescreen-test
       nr-runtime-identity-test upscaler-support-test)
ninja -C out/gpu -j "${BB_JOBS:-2}" "${tests[@]}" hdr-native-shader-test
suffix=
case $(uname -s) in MINGW*|MSYS*|CYGWIN*) suffix=.exe ;; esac
for test in "${tests[@]}"; do "./out/gpu/$test$suffix"; done
for suite in test_prepare.py test_patches.py test_game_check.py test_windows_frame_rate.py \
             test_engine_assets.py test_online.py test_upscaler_assets.py test_fsr4cap_dll_info.py; do
    test -f "tests/$suite"
    python3 -m unittest discover -s tests -p "$suite" -v
done
echo '[Test] CPU native/Python regressions passed. HDR shader test compiled; GPU execution and NR/FG gameplay are not validated on hosted CI.'
