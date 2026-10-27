#!/usr/bin/env bash
# Experimental Ubuntu 24.04 tar.gz qualification, separate from production Windows.
set -euo pipefail
repo=$PWD
engine=$repo/_ci/native-engine
export BB_JOBS=2 BB_PGO=off BB_LTO=ON BB_MARCH=
export BB_CI_LINUX_HELPER=$repo/tools/ci/linux-deps.py
python3 "$BB_CI_LINUX_HELPER" --engine "$engine" --output _ci/setup-linux.sh
sudo --preserve-env=BB_JOBS,BB_CI_LINUX_HELPER bash _ci/setup-linux.sh
sudo apt-get install -y --no-install-recommends xvfb xdotool libice6 libsm6 libfontconfig1 libfreetype6
dpkg-query -W > build/ci/linux-packages.txt
export CC=clang CXX=clang++ PKG_CONFIG_PATH=/opt/deps/lib/pkgconfig:/opt/deps/share/pkgconfig
export CMAKE_PREFIX_PATH=/opt/deps LDFLAGS=-fuse-ld=lld
dotnet publish launcher/windows/LanternLauncher/LanternLauncher.csproj -c Release -r linux-x64 \
    --self-contained true -p:RestoreLockedMode=true -p:Version="${TAG#v}" -p:DebugType=none -o _ci/linux-launcher
mkdir -p _ci/linux-template
cp tools/nr-release-default.ini _ci/linux-template/lantern.ini
cp LICENSE _ci/linux-template/LICENSE
export BB_ENGINE_ID=lantern-native-nr-${TAG#v}
bash "$engine/packaging/linux/build.sh" "${TAG#v}" "$repo/_ci/linux-template" "$repo/_ci/linux-launcher"
bash tools/ci/test-native.sh "$engine"
# Preserve fork/upstream notices and exact source/dependency provenance in the package.
package=$engine/out/package/LanternEngine
cp NOTICE "$package/NOTICE"
cp "$engine/CI-SOURCE.json" "$package/BUILD-SOURCE.json"
cp build/ci/source.json "$package/BUILD-PROVENANCE.json"
cp tools/ci/inputs.json "$package/BUILD-INPUTS.json"
mkdir -p "$package/licenses"
cp licenses/GPL-3.0.txt licenses/Chiaki-ngx-AGPL-3.0-OpenSSL.txt "$package/licenses/"
python3 - "$package" <<'PY'
import json, pathlib, sys
root = pathlib.Path(sys.argv[1])
(root / 'package.json').write_text(json.dumps({
    'version': json.loads((root / 'BUILD-PROVENANCE.json').read_text())['app_version'],
    'engine': 'lantern-native-nr-' + json.loads((root / 'BUILD-PROVENANCE.json').read_text())['app_version'],
    'repository': 'TheHiepDang/BloodborneX', 'update_family': 'bloodbornex-native-nr-v1',
    'platform': 'linux-x64', 'experimental': True}) + '\n')
PY
mkdir -p dist-linux
tar -C "$engine/out/package" -czf "dist-linux/BloodborneX-${TAG#v}-linux-x64-experimental.tar.gz" LanternEngine
# Clear development lookup paths, extract again and audit every ELF outside the build tree.
xvfb-run -a env -u LD_LIBRARY_PATH -u LD_PRELOAD -u PKG_CONFIG_PATH -u CMAKE_PREFIX_PATH PATH=/usr/bin:/bin \
    python3 tools/ci/verify-linux.py "dist-linux/BloodborneX-${TAG#v}-linux-x64-experimental.tar.gz" "$TAG"
sha256sum dist-linux/*.tar.gz > dist-linux/SHA256SUMS.txt
cp build/ci/linux-elf-audit.json dist-linux/
printf '%s\n' 'Experimental Ubuntu 24.04 x86_64 tarball. Not a production release or Steam Deck certification.' \
    'CPU tests, ELF dependency audit and isolated Xvfb launcher startup passed. No NR/FG gameplay was tested.' > dist-linux/VALIDATION.txt
