"""Apply the GTAO supplement to a matching full engine, without touching Git refs."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('engine', type=Path, help='Complete applied v1.0.3 NR engine source')
parser.add_argument('--check', action='store_true', help='Validate without changing files')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[1]
root = args.engine.resolve()
manifest = json.loads((repo / 'engine/tier1-gtao-baseline.json').read_text())
if not (root / 'build.sh').is_file():
    parser.error('Expected complete applied engine source, not the patch-only engine directory')
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
states = [(entry, digest(root / entry['path'])) for entry in manifest['files']]
if all(actual == entry['modified_sha256'] for entry, actual in states):
    print('GTAO supplement already applied; all source hashes match')
    raise SystemExit(0)
bad = [entry['path'] for entry, actual in states if actual != entry['baseline_sha256']]
if bad:
    parser.error('Source differs from the recorded pre-GTAO baseline; no changes made: ' + ', '.join(bad))
patch = repo / 'engine/tier1-gtao.patch'
# Applied snapshots often live below the packaging repo. Stop discovery at their
# parent so Git cannot silently skip paths as outside its current subdirectory.
environment = os.environ.copy()
for key in ('GIT_DIR', 'GIT_WORK_TREE', 'GIT_COMMON_DIR', 'GIT_INDEX_FILE'):
    environment.pop(key, None)
# Patch LF-normalized staging copies, not the user's CRLF preimages. Verify the
# entire result before copying any changed source back, including in --check mode.
with tempfile.TemporaryDirectory(prefix='.gtao-patch-', dir=root) as temporary:
    staging = Path(temporary)
    environment['GIT_CEILING_DIRECTORIES'] = str(root)
    for entry in manifest['files']:
        source = root / entry['path']
        if source.is_file():
            target = staging / entry['path']
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(source.read_bytes().replace(b'\r\n', b'\n'))
    subprocess.run(['git', '-c', 'core.autocrlf=false', 'apply', '--check', str(patch)],
                   cwd=staging, env=environment, check=True)
    subprocess.run(['git', '-c', 'core.autocrlf=false', 'apply', str(patch)],
                   cwd=staging, env=environment, check=True)
    if not all(digest(staging / entry['path']) == entry['modified_sha256'] for entry in manifest['files']):
        raise SystemExit('Patched source hash mismatch; original engine has not been changed')
    if args.check:
        print('PASS: preimage, patch replay and all output hashes; original source unchanged')
    else:
        for entry in manifest['files']:
            target = root / entry['path']
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(staging / entry['path'], target)
        print('GTAO supplement applied and source hashes verified')
