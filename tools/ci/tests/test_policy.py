import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

def module(name, file):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).resolve().parents[1] / file)
    loaded = importlib.util.module_from_spec(spec); spec.loader.exec_module(loaded); return loaded

policy = module('source_policy', 'policy.py')
source = module('source_reconstruction', 'reconstruct.py')


class PolicyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.policy = {'root_readme': 'README.md', 'inherited_markdown': {}}

    def test_newly_tracked_private_markdown_is_rejected_without_deleting_it(self):
        private = self.root / 'AGENTS.md'; private.write_text('local instructions')
        with patch.object(policy.subprocess, 'check_output', return_value='README.md\0AGENTS.md\0docs/HANDOFF.md\0'):
            errors = policy.tracking_errors(self.root, self.policy)
        self.assertTrue(any('AGENTS.md' in e for e in errors))
        self.assertTrue(any('HANDOFF.md' in e for e in errors))
        self.assertEqual(private.read_text(), 'local instructions')

    def test_root_readme_and_proven_upstream_document_are_allowed(self):
        file = self.root / 'engine/README.md'; file.parent.mkdir(); file.write_bytes(b'inherited\r\n')
        self.policy['inherited_markdown']['engine/README.md'] = {'sha256': hashlib.sha256(b'inherited\n').hexdigest()}
        with patch.object(policy.subprocess, 'check_output', return_value='README.md\0engine/README.md\0'):
            self.assertEqual(policy.tracking_errors(self.root, self.policy), [])
            file.write_text('replaced by internal notes')
            self.assertTrue(policy.tracking_errors(self.root, self.policy))

    def test_archive_rejects_internal_documents_player_data_models_and_traversal(self):
        for name in ('AGENTS.md', 'docs/RELEASING.md', '.codex/context.json', 'saves/player.dat',
                     'engine/out/saves/player.dat', 'dlss/nvngx_dlssnr.dll', 'eboot.bin', '../outside.txt'):
            with self.subTest(name=name):
                path = self.root / 'package.zip'
                with zipfile.ZipFile(path, 'w') as archive: archive.writestr('LanternEngine/' + name, b'private')
                self.assertTrue(policy.archive_errors(path, 'windows', self.policy))

    def test_archive_allows_public_guides_and_required_license_markdown(self):
        path = self.root / 'package.zip'
        with zipfile.ZipFile(path, 'w') as archive:
            for name in ('README.md', 'START-HERE.md', 'RELEASE-NOTES.md', 'licenses/engine/NOTICE.md'):
                archive.writestr('LanternEngine/' + name, 'public')
        self.assertEqual(policy.archive_errors(path, 'windows', self.policy), [])

    def test_source_archive_disallows_development_binaries_and_unapproved_docs(self):
        for name in ('engine-source/out/bb-probe.exe', 'BloodborneX/docs/HANDOFF.md'):
            with self.subTest(name=name):
                path = self.root / 'source.zip'
                with zipfile.ZipFile(path, 'w') as archive: archive.writestr('LanternEngine-source/' + name, 'private')
                self.assertTrue(policy.archive_errors(path, 'source', self.policy))

    def test_essential_source_and_license_inputs_are_not_filtered(self):
        for name in ('gpu/shadps4/core/libraries/save_data/save_data.cpp', 'tools/nr-release-default.ini',
                     'licenses/NOTICE.md', 'gpu/third_party/fsr-vulkan/generated/shader.spv'):
            with self.subTest(name=name): self.assertTrue(policy.source_allowed(name, set()))
        self.assertFalse(policy.source_allowed('docs/local-notes.md', set()))

    def test_reconstruction_rejects_changed_patch_before_applying_anything(self):
        patch_file = self.root / 'changed.patch'; patch_file.write_text('not approved')
        with patch.object(source, 'git') as git, self.assertRaises(ValueError):
            source.apply_patch(self.root, patch_file, 'a' * 64)
        git.assert_not_called()

    def test_reconstruction_only_normalizes_explicit_text_targets(self):
        file = self.root / 'sample.cpp'; file.write_bytes(b'line\r\n')
        result = source.normalize_target(self.root, 'sample.cpp')
        self.assertEqual(file.read_bytes(), b'line\n')
        self.assertNotEqual(result['original_sha256'], result['canonical_sha256'])
        file.write_bytes(b'\0binary\r\n')
        with self.assertRaises(ValueError): source.normalize_target(self.root, 'sample.cpp')
        with self.assertRaises(ValueError): source.normalize_target(self.root, '../outside.cpp')


if __name__ == '__main__': unittest.main()
