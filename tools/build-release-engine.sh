#!/usr/bin/env bash
set -euo pipefail
export MSYSTEM=CLANG64
export PATH=/clang64/bin:/usr/bin:$PATH
export CC=clang CXX=clang++ BB_PGO=off BB_LTO=ON BB_MARCH=
export BB_JOBS="$2"
cd -- "$1"
bash ./build.sh
if grep -q 'add_executable(nr-runtime-identity-test' gpu/CMakeLists.txt; then
    ninja -C out/gpu -j "$BB_JOBS" nr-runtime-identity-test
    ./out/gpu/nr-runtime-identity-test.exe
fi
