import hashlib
import importlib.util
import json
from pathlib import Path
import re
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[3]


class InputTests(unittest.TestCase):
    def test_native_inputs_have_exact_hashes_and_public_commit_pins(self):
        lock = json.loads((ROOT / 'tools/ci/inputs.json').read_text())
        for item in lock['engine']['patches'] + lock['engine']['support_files']:
            with self.subTest(path=item['path']):
                self.assertEqual(hashlib.sha256((ROOT / item['path']).read_bytes()).hexdigest(), item['sha256'])
        for item in [lock['engine'], *lock['cmake_git'].values(), *lock['linux_git'].values()]:
            self.assertRegex(item['commit'], r'^[0-9a-f]{40}$')
        for digest in lock['msys']['packages'].values(): self.assertRegex(digest, r'^[0-9a-f]{64}$')
        for item in lock['downloads'].values():
            self.assertTrue(item['url'].startswith('https://'))
            self.assertRegex(item['sha256'], r'^[0-9a-f]{64}$')
            self.assertNotIn('nvngx_dlssnr', item['url'].lower())
        self.assertEqual(json.loads((ROOT / 'global.json').read_text())['sdk']['version'], '8.0.426')

    def test_workflows_cannot_publish_from_validation_and_actions_are_sha_pinned(self):
        workflows = ROOT / '.github/workflows'
        for file in workflows.glob('*.yml'):
            content = file.read_text()
            self.assertNotIn('macos-', content)
            self.assertNotIn('pull_request_target', content)
            self.assertNotIn('continue-on-error:', content)
            for action in re.findall(r'uses:\s*(\S+)', content):
                self.assertTrue(action.startswith('./.github/workflows/') or re.fullmatch(r'[\w.-]+/[\w.-]+@[0-9a-f]{40}', action), action)
            if file.name != 'release.yml':
                self.assertNotIn('contents: write', content)
                self.assertNotIn('release.py publish', content)
        release = (workflows / 'release.yml').read_text()
        self.assertIn("if: github.event_name == 'push' && startsWith(github.ref, 'refs/tags/v') && github.repository == 'TheHiepDang/BloodborneX'", release)
        self.assertIn('needs: build', release)
        self.assertIn('name: release-windows', release)
        self.assertNotIn('experimental-linux', release)

    def test_nuget_locks_and_public_document_inputs_exist(self):
        for project in ('BloodborneLauncher', 'LanternLauncher', 'tests/UpdateChecks', 'tests/NrRuntimeChecks',
                        'tests/NrOptimizationChecks', 'tests/StreamlineRuntimeChecks'):
            data = json.loads((ROOT / 'launcher/windows' / project / 'packages.lock.json').read_text())
            self.assertIn('dependencies', data)
        package = (ROOT / 'tools/build-nr-release.ps1').read_text()
        self.assertIn('nr-start-here.template.txt', package)
        self.assertNotIn('docs\\NR-', package)
        self.assertNotIn('nr-start-here.template.md', package)
        self.assertTrue((ROOT / 'tools/nr-start-here.template.txt').is_file())


if __name__ == '__main__': unittest.main()
