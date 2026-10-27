#!/usr/bin/env python3
"""Fresh native engine reconstruction; no binary substitution or patch fuzz."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
LOCK = Path(__file__).with_name('inputs.json')


def sha_bytes(data): return hashlib.sha256(data).hexdigest()
def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), '-c', 'core.autocrlf=false',
                                    '-c', 'core.longpaths=true', *args], text=True).strip()


def normalize_target(root, relative):
    path = root / relative
    if not path.resolve().is_relative_to(root.resolve()): raise ValueError('Escaping patch path: ' + relative)
    if not path.is_file(): return None
    raw = path.read_bytes()
    if b'\r\n' not in raw: return None
    if b'\0' in raw: raise ValueError('Refusing binary normalization: ' + relative)
    raw.decode('utf-8')
    canonical = raw.replace(b'\r\n', b'\n')
    path.write_bytes(canonical)
    return {'path': relative, 'original_sha256': sha_bytes(raw), 'canonical_sha256': sha_bytes(canonical)}


def apply_patch(root, source, expected):
    raw = source.read_bytes()
    if sha_bytes(raw) != expected: raise ValueError('Patch/support checksum mismatch: ' + str(source))
    canonical = raw.replace(b'\r\n', b'\n')
    targets = re.findall(r'^\+\+\+ b/(.+)$', canonical.decode('utf-8'), re.M)
    normalized = [result for name in targets if (result := normalize_target(root, name))]
    with tempfile.NamedTemporaryFile(suffix='.patch', delete=False) as stream:
        stream.write(canonical); staged = Path(stream.name)
    try:
        git(root, 'apply', '--check', str(staged))
        git(root, 'apply', str(staged))
    finally:
        staged.unlink()
    return {'path': source.name, 'sha256': expected, 'canonical_sha256': sha_bytes(canonical), 'normalized_targets': normalized}


def reconstruct(destination, report):
    lock = json.loads(LOCK.read_text())
    config = lock['engine']
    if destination.exists(): raise ValueError('Refusing to overwrite an existing source directory')
    destination.mkdir(parents=True)
    git(destination, 'init')
    git(destination, 'config', 'core.autocrlf', 'false')
    git(destination, 'config', 'core.longpaths', 'true')
    git(destination, 'remote', 'add', 'origin', config['repository'])
    git(destination, 'fetch', '--depth', '1', 'origin', config['commit'])
    git(destination, 'checkout', '--detach', 'FETCH_HEAD')
    if git(destination, 'rev-parse', 'HEAD') != config['commit']: raise ValueError('Native source revision mismatch')
    git(destination, 'submodule', 'update', '--init', '--recursive', '--depth', '1')
    actual = {line.strip().split()[1]: line.strip().split()[0] for line in git(destination, 'submodule', 'status', '--recursive').splitlines()}
    if actual != config['submodules']: raise ValueError('Recursive source dependency revisions differ from lock')
    approved = set(git(destination, 'ls-files', '*.md').splitlines())
    for folder in config['submodules']:
        approved.update(folder + '/' + name for name in git(destination / folder, 'ls-files', '*.md').splitlines())
    # Additional inherited documentation introduced by Lantern's cumulative port.
    approved.update(lock.get('inherited_engine_markdown', []))
    applied = [apply_patch(destination, ROOT / item['path'], item['sha256']) for item in config['patches']]
    builtin = [apply_patch(destination / 'gpu/third_party/fsr-vulkan', destination / item['path'], item['sha256'])
               for item in config['builtin_fsr_patches']]
    for item in config['support_files']:
        raw = (ROOT / item['path']).read_bytes()
        if sha_bytes(raw) != item['sha256']: raise ValueError('Support file changed: ' + item['path'])
        target = destination / item['target']; target.parent.mkdir(parents=True, exist_ok=True); target.write_bytes(raw)
    cmake = destination / 'gpu/CMakeLists.txt'
    text = cmake.read_text()
    for name, item in lock['cmake_git'].items():
        old = 'GIT_TAG ' + item['tag'] + ' GIT_SHALLOW TRUE'
        if text.count(old) != 1: raise ValueError('Dependency pin precondition changed: ' + name)
        text = text.replace(old, 'GIT_TAG ' + item['commit'] + ' GIT_SHALLOW FALSE')
    cmake.write_text(text, newline='\n')
    result = {'schema': 1, 'upstream': config['repository'], 'commit': config['commit'], 'submodules': actual,
              'patches': applied, 'builtin_fsr_patches': builtin, 'support_files': config['support_files'], 'cmake_git': lock['cmake_git'],
              'cmake_sha256': sha_bytes(cmake.read_bytes()), 'approved_markdown': sorted(approved),
              'inputs_sha256': sha_bytes(LOCK.read_bytes())}
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text(json.dumps(result, indent=2) + '\n')
    (destination / 'CI-SOURCE.json').write_text(json.dumps(result, indent=2) + '\n')
    print('[Source] Exact pinned native source and ordered patches reconstructed; no released executables used.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--destination', required=True, type=Path)
    parser.add_argument('--report', required=True, type=Path)
    args = parser.parse_args()
    reconstruct(args.destination.resolve(), args.report.resolve())
