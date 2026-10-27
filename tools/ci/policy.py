#!/usr/bin/env python3
"""Tracked-document and archive policy, independent of local ignore configuration."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[2]
POLICY = Path(__file__).with_name('documentation-policy.json')
PRIVATE_PARTS = {'.git', '.agents', '.codex', 'agents_space', '.release-work', '.cache', '__pycache__'}
PLAYER_PARTS = {'game', 'games', 'user', 'saves', 'savedata', 'profiles', 'mods', 'logs', 'shader-cache'}
PRIVATE_NAMES = {'agents.md', 'agents.txt', '.release-settings.json', 'prepared.json', 'game_dir.txt',
                 'mods.json', 'patches.json', 'nvngx_dlssnr.dll'}


def sha(path):
    digest = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''): digest.update(block)
    return digest.hexdigest()


def forbidden(name):
    path = PurePosixPath(name.replace('\\', '/'))
    parts = tuple(part.lower() for part in path.parts)
    if path.is_absolute() or '..' in parts or any(':' in p for p in parts): return True
    if any(p in PRIVATE_PARTS for p in parts): return True
    if parts and parts[0] in PLAYER_PARTS: return True
    base = path.name.lower()
    return (base in PRIVATE_NAMES or base.startswith(('eboot.', '.env'))
            or bool(re.match(r'^cusa\d{5}$', parts[0] if parts else ''))
            or bool(re.search(r'\.(log|dmp|pdb|bak|orig|tmp|pyc)$', base)))


def tracking_errors(root, policy):
    tracked = subprocess.check_output(['git', '-C', str(root), 'ls-files', '-z'], text=True).split('\0')
    approved = {policy['root_readme'], *policy['inherited_markdown']}
    errors = []
    for name in filter(None, tracked):
        if forbidden(name): errors.append('Private/player material tracked: ' + name)
        if name.lower().endswith('.md') and name not in approved:
            errors.append('Unapproved Markdown tracked: ' + name)
    for name, origin in policy['inherited_markdown'].items():
        path = root / name
        if not path.is_file() or hashlib.sha256(path.read_bytes().replace(b'\r\n', b'\n')).hexdigest() != origin['sha256']:
            errors.append('Inherited document changed; review origin policy explicitly: ' + name)
    return errors


def legal_document(name):
    path = PurePosixPath(name)
    return ('licenses' in (part.lower() for part in path.parts)
            or bool(re.match(r'^(license|notice|copying|third.?party.?notices)', path.name, re.I)))


def source_allowed(name, approved_markdown):
    if forbidden(name): return False
    parts = PurePosixPath(name).parts
    if any(p.lower() in {'out', 'bin', 'obj', 'build', 'pgo', 'releases', 'dist', 'downloads', 'node_modules'} for p in parts):
        return False
    if re.search(r'\.(dll|exe|so|dylib|pdb|obj|o|a|lib|zip|7z|mp4|cache)$', name, re.I): return False
    if name.lower().endswith('.md') and name not in approved_markdown and not legal_document(name): return False
    return True


def archive_errors(path, kind, policy):
    errors, seen = [], set()
    approved_repo = {policy['root_readme'], *policy['inherited_markdown']}
    with zipfile.ZipFile(path) as archive:
        # The public source manifest records proven upstream origins from reconstruction.
        source_record = next((i for i in archive.infolist() if i.filename.endswith('/engine-source/CI-SOURCE.json')), None)
        source_docs = set(json.loads(archive.read(source_record))['approved_markdown']) if source_record else set()
        for item in archive.infolist():
            if item.is_dir(): continue
            raw = item.filename.replace('\\', '/')
            if forbidden(raw) or (item.external_attr >> 16) & 0o170000 == 0o120000:
                errors.append('Forbidden archive path: ' + raw); continue
            name = raw.partition('/')[2]
            if not name or name in seen:
                errors.append('Missing root or duplicate archive path: ' + raw)
            seen.add(name)
            relative = name
            if kind == 'source' and name.startswith('BloodborneX/'):
                relative = name.removeprefix('BloodborneX/')
            elif kind == 'source' and name.startswith('engine-source/'):
                relative = name.removeprefix('engine-source/')
            if forbidden(relative): errors.append('Private archive material: ' + raw)
            if kind == 'windows' and any(p.lower() in PLAYER_PARTS for p in PurePosixPath(relative).parts):
                errors.append('Player data in portable archive: ' + raw)
            if name.lower().endswith('.md'):
                allowed = legal_document(name)
                if kind == 'source':
                    allowed |= (name.startswith('BloodborneX/') and relative in approved_repo)
                    allowed |= (name.startswith('engine-source/') and relative in source_docs)
                    allowed |= name == 'BUILD-FROM-SNAPSHOT.md'
                else:
                    allowed |= name in {'README.md', 'START-HERE.md', 'RELEASE-NOTES.md'}
                if not allowed: errors.append('Unapproved archive Markdown: ' + raw)
            if kind == 'source' and not source_allowed(relative, source_docs | approved_repo | {'BUILD-FROM-SNAPSHOT.md'}):
                errors.append('Non-source material in source archive: ' + raw)
    return sorted(set(errors))


def stage_source(source, destination, kind, engine_root):
    source, destination, engine_root = source.resolve(), destination.resolve(), engine_root.resolve()
    policy = json.loads(POLICY.read_text())
    prefix = ''
    if kind == 'repo':
        approved = {policy['root_readme'], *policy['inherited_markdown']}
    else:
        prefix = source.relative_to(engine_root).as_posix()
        if prefix == '.': prefix = ''
        record = engine_root / 'CI-SOURCE.json'
        approved = set(json.loads(record.read_text())['approved_markdown']) if record.is_file() else set(
            json.loads(Path(__file__).with_name('inputs.json').read_text())['inherited_engine_markdown'])
    if (source / '.git').exists():
        files = subprocess.check_output(['git', '-C', str(source), 'ls-files', '-z', '--cached', '--others', '--exclude-standard'], text=True).split('\0')
    else:
        files = [p.relative_to(source).as_posix() for p in source.rglob('*') if p.is_file()]
    count = 0
    for name in sorted(set(filter(None, files))):
        full = '/'.join(filter(None, (prefix, name)))
        if kind == 'engine' and full.startswith('.github/'): continue
        if not source_allowed(full, approved): continue
        file = source / name
        if not file.is_file(): continue  # Gitlinks are copied by the explicit submodule calls.
        if not file.resolve().is_relative_to(source): raise ValueError('Escaping source symlink: ' + name)
        target = destination / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(file, target)
        count += 1
    print(f'[Policy] Staged {count} approved source files ({kind}); private notes and binaries excluded.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path)
    parser.add_argument('--kind', choices=('windows', 'source'))
    parser.add_argument('--stage-from', type=Path)
    parser.add_argument('--stage-to', type=Path)
    parser.add_argument('--source-kind', choices=('repo', 'engine'))
    parser.add_argument('--engine-root', type=Path)
    args = parser.parse_args()
    if args.stage_from:
        stage_source(args.stage_from, args.stage_to, args.source_kind, args.engine_root)
        return
    policy = json.loads(POLICY.read_text())
    errors = archive_errors(args.archive, args.kind, policy) if args.archive else tracking_errors(ROOT, policy)
    for error in errors: print('[Policy] ' + error)
    print(f'[Policy] {len(errors)} tracking/archive violations')
    raise SystemExit(bool(errors))


if __name__ == '__main__': main()
