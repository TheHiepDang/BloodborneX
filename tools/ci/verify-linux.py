#!/usr/bin/env python3
"""Inspect and launch an extracted experimental package outside the build environment."""
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tarfile
import tempfile
import time

import policy


def verify(archive, tag):
    report = {'platform': 'Ubuntu 24.04 x86_64', 'experimental': True, 'gpu_hardware_tested': False, 'elf': []}
    with tempfile.TemporaryDirectory(prefix='bloodbornex linux package ') as directory:
        extracted = Path(directory)
        with tarfile.open(archive) as source:
            seen = set()
            for entry in source:
                name = entry.name
                if (name in seen or policy.forbidden(name) or not name.startswith('LanternEngine/')
                        or not (entry.isfile() or entry.isdir())):
                    raise ValueError('Unsafe/private Linux package entry: ' + name)
                seen.add(name)
                relative = name.removeprefix('LanternEngine/')
                if policy.forbidden(relative): raise ValueError('Private Linux package file: ' + name)
                if entry.isfile() and name.lower().endswith('.md') and not policy.legal_document(name):
                    raise ValueError('Unapproved Linux package Markdown: ' + name)
            source.extractall(extracted)
        root = extracted / 'LanternEngine'
        for relative in ('Lantern Engine', 'lantern.sh', 'lantern.ini', 'LICENSE', 'NOTICE',
                         'engine/out/bb-probe', 'engine/out/bb-start', 'engine/out/bb-gpu-capabilities',
                         'engine/out/gpu/libbbgpu.so', 'engine/out/gpu/libSDL3.so.0', 'engine/run.sh'):
            if not (root / relative).is_file(): raise ValueError('Incomplete Linux package: ' + relative)
        metadata = json.loads((root / 'package.json').read_text())
        if metadata['version'] != tag[1:] or metadata['engine'] != 'lantern-native-nr-' + tag[1:]:
            raise ValueError('Linux package version mismatch')
        for file in root.rglob('*'):
            if not file.is_file(): continue
            with file.open('rb') as stream: header = stream.read(20)
            if not header.startswith(b'\x7fELF'): continue
            if header[4:6] != b'\x02\x01' or int.from_bytes(header[18:20], 'little') != 62:
                raise ValueError('Wrong ELF architecture: ' + str(file))
            result = subprocess.run(['ldd', str(file)], text=True, capture_output=True)
            output = result.stdout + result.stderr
            if result.returncode or 'not found' in output: raise ValueError('Unresolved ELF dependencies: ' + str(file) + '\n' + output)
            resolved = re.findall(r'=> (/\S+)', output)
            for dependency in resolved:
                path = Path(dependency).resolve()
                if not path.is_relative_to(root) and not str(path).startswith(('/usr/lib/', '/lib/')):
                    raise ValueError('Development dependency leaked into runtime: ' + dependency)
            report['elf'].append({'file': file.relative_to(root).as_posix(), 'resolved': resolved})
        if not report['elf']: raise ValueError('No ELF executables')
        usage = subprocess.run([str(root / 'engine/out/bb-probe')], text=True, capture_output=True, timeout=30)
        if usage.returncode != 1 or 'usage:' not in (usage.stdout + usage.stderr).lower():
            raise ValueError('Packaged engine usage smoke test failed: ' + usage.stdout + usage.stderr)
        # Isolate config and disable updates; this must never access a real game or profile.
        config = extracted / 'config'; config.mkdir()
        (config / 'settings.json').write_text(json.dumps({'check_updates': False, 'setup_shown': True, 'ui_language': 'en', 'port_dir': str(root)}))
        env = dict(os.environ, BLOODBORNEX_CONFIG_DIR=str(config), HOME=str(extracted / 'home'))
        with open('build/ci/linux-launcher.log', 'w') as log:
            process = subprocess.Popen([str(root / 'Lantern Engine')], cwd=root, env=env, stdout=log, stderr=log, start_new_session=True)
            try:
                # Require a real mapped window, not merely a process that is still alive.
                for _ in range(30):
                    if process.poll() is not None: raise ValueError('Packaged Avalonia launcher exited before readiness; see linux-launcher.log')
                    window = subprocess.run(['xdotool', 'search', '--onlyvisible', '--pid', str(process.pid)], capture_output=True, text=True)
                    if window.returncode == 0 and window.stdout.strip(): break
                    time.sleep(1)
                else: raise ValueError('Packaged Avalonia launcher did not map a visible window')
                report['startup'] = 'Avalonia mapped a visible window under isolated Xvfb; no gameplay tested'
            finally:
                import signal
                if process.poll() is None:
                    os.killpg(process.pid, signal.SIGTERM)
                    process.wait(timeout=15)
        if list(config.rglob('*crash*')): raise ValueError('Launcher produced a crash report')
    Path('build/ci/linux-elf-audit.json').write_text(json.dumps(report, indent=2) + '\n')
    print('[Linux experimental] Archive, every ELF, engine usage and isolated launcher startup verified.')


if __name__ == '__main__': verify(Path(sys.argv[1]), sys.argv[2])
