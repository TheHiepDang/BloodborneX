#!/usr/bin/env python3
"""Reuse the pinned native Ubuntu recipe, replacing tag clones with locked commits."""
import argparse
import json
from pathlib import Path
import re
import subprocess

LOCK = Path(__file__).with_name('inputs.json')


def fetch(name, tag, url):
    item = json.loads(LOCK.read_text())['linux_git'][name]
    if (item['tag'], item['repository']) != (tag, url): raise ValueError('Linux dependency recipe drift: ' + name)
    target = Path(name)
    if target.exists(): raise ValueError('Refusing existing Linux dependency source: ' + name)
    target.mkdir()
    def git(*args): return subprocess.check_output(['git', '-C', str(target), *args], text=True).strip()
    git('init'); git('remote', 'add', 'origin', url)
    git('fetch', '--depth', '1', 'origin', item['commit']); git('checkout', '--detach', 'FETCH_HEAD')
    if git('rev-parse', 'HEAD') != item['commit']: raise ValueError('Linux dependency checkout mismatch')
    # Recursive gitlinks are immutable parts of the pinned dependency commits.
    git('submodule', 'update', '--init', '--recursive', '--depth', '1')
    print('[Dependencies] ' + name + ' ' + item['commit'], flush=True)


def adapt(engine, output):
    recipe = (engine / 'packaging/linux/setup-ubuntu.sh').read_text()
    old = 'fetch() { # name tag url [--recursive]\n    rm -rf "$1"\n    git clone -q --depth 1 -b "$2" ${4:-} "$3" "$1"\n}'
    if recipe.count(old) != 1: raise ValueError('Pinned Linux recipe fetch precondition changed')
    # The source recipe still owns all build/configure/install commands. Do not hide errors.
    replacement = 'fetch() { python3 "$BB_CI_LINUX_HELPER" --fetch "$1" "$2" "$3"; }'
    recipe = recipe.replace(old, replacement).replace(' >/dev/null', '')
    if 'git clone' in recipe: raise ValueError('Unpinned dependency clone left in Linux recipe')
    expected = set(json.loads(LOCK.read_text())['linux_git'])
    names = set(re.findall(r'^(?:cmake_dep|    fetch) ([a-z0-9_-]+) ', recipe, re.M))
    if names != expected: raise ValueError('Linux dependency coverage changed: ' + str(names ^ expected))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(recipe, newline='\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fetch', nargs=3)
    parser.add_argument('--engine', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.fetch: fetch(*args.fetch)
    else: adapt(args.engine, args.output)
