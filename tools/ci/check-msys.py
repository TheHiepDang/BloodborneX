#!/usr/bin/env python3
"""Reject compiler/dependency drift after pacman installs the locked closure."""
import json
from pathlib import Path
import subprocess

lock = json.loads(Path(__file__).with_name('inputs.json').read_text())['msys']['packages']
expected = {}
for filename in lock:
    name, version, release, arch = filename.removesuffix('.pkg.tar.zst').rsplit('-', 3)
    if arch != 'any' and arch != 'x86_64': raise ValueError('Unexpected package architecture: ' + filename)
    expected[name] = version + '-' + release
installed = dict(line.split(maxsplit=1) for line in subprocess.check_output(['pacman', '-Q'], text=True).splitlines())
errors = [name for name, version in expected.items() if installed.get(name) != version]
errors += [name for name in installed if name.startswith('mingw-w64-clang-x86_64-') and name not in expected]
if errors: raise SystemExit('[Dependencies] Unlocked or missing MSYS2 packages: ' + ', '.join(sorted(set(errors))))
print(f'[Dependencies] Verified all {len(expected)} exact CLANG64 package versions; no unpinned fallback.')
