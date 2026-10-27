#!/usr/bin/env bash
set -euo pipefail
export MSYSTEM=CLANG64
export PATH=/clang64/bin:/usr/bin:$PATH
export CC=clang CXX=clang++ BB_PGO=off BB_LTO=ON BB_MARCH=
export BB_JOBS="$2"
ci_tools=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/ci" && pwd)
cd -- "$1"
export CXXFLAGS="-include \"$PWD/gpu/win_compat.h\" ${CXXFLAGS:-}"
bash ./build.sh
bash "$ci_tools/test-native.sh" "$PWD"
