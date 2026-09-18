"""Snapshot identity, safe archive inspection and isolated runtime checks."""

import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile
import unittest
import zipfile


ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('pilot_bundle', ROOT / 'scripts/pilot_bundle.py')
bundle = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bundle)


class BundleTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / 'source'
        for name in bundle.PAYLOAD_FILES:
            target = self.root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, target)
        self.archive = Path(self.temp.name) / 'build.zip'

    def mutate_zip(self, transform):
        with zipfile.ZipFile(self.archive) as archive:
            entries = {name: archive.read(name) for name in archive.namelist()}
        transform(entries)
        output = Path(self.temp.name) / 'changed.zip'
        with zipfile.ZipFile(output, 'w') as archive:
            for name, data in entries.items():
                archive.writestr(name, data)
        return output

    def test_freeze_is_deterministic_and_excludes_unlisted_private_files(self):
        (self.root / '.env').write_text('not a real secret', encoding='utf-8')
        first = bundle.freeze(self.root, self.archive)
        second_path = self.archive.with_name('second.zip')
        second = bundle.freeze(self.root, second_path)
        self.assertEqual(first, second)
        self.assertEqual(self.archive.read_bytes(), second_path.read_bytes())
        with zipfile.ZipFile(self.archive) as archive:
            self.assertNotIn('.env', archive.namelist())
            index = json.loads(archive.read('bundle.json'))
            self.assertEqual(index['pilot_status'], 'not_run')
            self.assertEqual(index['tests'], 'not_run_by_packager')
        self.assertEqual(bundle.verify(self.archive, first['archive_sha256']), first)

    def test_existing_archive_is_preserved(self):
        self.archive.write_bytes(b'keep original')
        with self.assertRaises(FileExistsError):
            bundle.freeze(self.root, self.archive)
        self.assertEqual(self.archive.read_bytes(), b'keep original')

    def test_missing_input_creates_no_archive(self):
        # Rename a file in this test's own temporary fixture only.
        (self.root / '.python-version').rename(self.root / 'version.saved')
        with self.assertRaises((OSError, bundle.BundleError)):
            bundle.freeze(self.root, self.archive)
        self.assertFalse(self.archive.exists())

    def test_invalid_assignment_does_not_become_frozen_pilot(self):
        manifest_path = self.root / 'experiments/manifests/gate-p01.json'
        manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
        manifest['experiment']['assignments'][0]['slot'] = True
        manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
        with self.assertRaises(bundle.BundleError):
            bundle.freeze(self.root, self.archive)
        self.assertFalse(self.archive.exists())

    def test_missing_markdown_target_blocks_new_bundle(self):
        with (self.root / 'README.md').open('a', encoding='utf-8') as stream:
            stream.write('\n[missing](docs/missing.md)\n')
        with self.assertRaises(bundle.BundleError):
            bundle.freeze(self.root, self.archive)
        self.assertFalse(self.archive.exists())

    def test_legacy_v1_remains_verifiable(self):
        payload = {name: (self.root / name).read_bytes() for name in bundle.V1_FILES}
        # Construct the documented v1 JSON/ZIP directly; old snapshots did not
        # require closure of links to later research documents.
        hashes = {name: hashlib.sha256(data).hexdigest() for name, data in sorted(payload.items())}
        table = (json.dumps(hashes, sort_keys=True, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
        metadata = {
            'format': 'musca-local-pilot-v1', 'pilot_id': 'GATE-P01-v0.1',
            'pilot_status': 'not_run', 'python_reference': payload['.python-version'].decode().strip(),
            'manifest_sha256': hashes[bundle.MANIFEST],
            'snapshot_id': hashlib.sha256(table).hexdigest(), 'payload_sha256': hashes,
            'tests': 'not_run_by_packager',
        }
        with zipfile.ZipFile(self.archive, 'w') as archive:
            for name, data in payload.items():
                archive.writestr(name, data)
            archive.writestr('bundle.json', json.dumps(metadata))
        self.assertEqual(bundle.verify(self.archive)['format'], 'musca-local-pilot-v1')

    def test_changed_payload_fails_internal_hash_check(self):
        bundle.freeze(self.root, self.archive)
        changed = self.mutate_zip(lambda files: files.update({'musca/world.py': b'changed'}))
        with self.assertRaises(bundle.BundleError):
            bundle.verify(changed)

    def test_external_hash_rejects_repack_with_unchanged_payload(self):
        frozen = bundle.freeze(self.root, self.archive)
        repacked = self.mutate_zip(lambda files: None)
        self.assertEqual(bundle.verify(repacked)['snapshot_id'], frozen['snapshot_id'])
        with self.assertRaises(bundle.BundleError):
            bundle.verify(repacked, frozen['archive_sha256'])

    def test_unknown_paths_and_missing_entries_are_rejected(self):
        bundle.freeze(self.root, self.archive)
        for extra in ['../escape.py', '/absolute.py', 'musca/extra.py', '.env']:
            changed = self.mutate_zip(lambda files: files.update({extra: b'x'}))
            with self.subTest(extra=extra), self.assertRaises(bundle.BundleError):
                bundle.verify(changed)
        changed = self.mutate_zip(lambda files: files.pop('musca/world.py'))
        with self.assertRaises(bundle.BundleError):
            bundle.verify(changed)

    def test_duplicate_entries_and_metadata_keys_are_rejected(self):
        bundle.freeze(self.root, self.archive)
        with zipfile.ZipFile(self.archive) as source:
            raw = source.read('bundle.json')
        changed = self.mutate_zip(lambda files: files.update({'bundle.json': b'{"format":"x",' + raw[1:]}))
        with self.assertRaises(bundle.BundleError):
            bundle.verify(changed)
        duplicated = Path(self.temp.name) / 'duplicate.zip'
        import warnings
        with warnings.catch_warnings():
            warnings.simplefilter('ignore', UserWarning)
            with zipfile.ZipFile(duplicated, 'w') as archive:
                archive.writestr('bundle.json', raw)
                archive.writestr('bundle.json', raw)
        with self.assertRaises(bundle.BundleError):
            bundle.verify(duplicated)

    def test_oversized_member_is_rejected_before_reading(self):
        bundle.freeze(self.root, self.archive)
        changed = self.mutate_zip(lambda files: files.update({'musca/world.py': b'x' * (bundle.MAX_FILE_BYTES + 1)}))
        with self.assertRaises(bundle.BundleError):
            bundle.verify(changed)

    def test_corrupt_compressed_member_is_rejected_without_traceback(self):
        bundle.freeze(self.root, self.archive)
        compressed = Path(self.temp.name) / 'compressed.zip'
        with zipfile.ZipFile(self.archive) as source, zipfile.ZipFile(compressed, 'w', compression=zipfile.ZIP_DEFLATED) as target:
            for name in source.namelist():
                target.writestr(name, source.read(name))
        raw = bytearray(compressed.read_bytes())
        with zipfile.ZipFile(compressed) as archive:
            entry = archive.getinfo('musca/world.py')
            header = entry.header_offset
        name_length, extra_length = struct.unpack_from('<HH', raw, header + 26)
        data_offset = header + 30 + name_length + extra_length
        raw[data_offset] = 0x07  # Final block with reserved DEFLATE block type.
        compressed.write_bytes(raw)
        process = subprocess.run(
            [sys.executable, str(ROOT / 'scripts/pilot_bundle.py'), 'verify', str(compressed)],
            capture_output=True, encoding='utf-8', timeout=15,
        )
        self.assertEqual(process.returncode, 2)
        self.assertNotIn('Traceback', process.stderr)

    def test_archive_symlink_metadata_is_rejected(self):
        bundle.freeze(self.root, self.archive)
        linked = Path(self.temp.name) / 'linked.zip'
        with zipfile.ZipFile(self.archive) as source, zipfile.ZipFile(linked, 'w') as target:
            for entry in source.infolist():
                if entry.filename == 'musca/world.py':
                    entry.external_attr = 0o120777 << 16
                target.writestr(entry, source.read(entry.filename))
        with self.assertRaises(bundle.BundleError):
            bundle.verify(linked)

    def test_freeze_captures_bytes_instead_of_following_later_worktree_edits(self):
        frozen = bundle.freeze(self.root, self.archive)
        with (self.root / 'musca/world.py').open('a', encoding='utf-8') as stream:
            stream.write('\n# later change\n')
        self.assertEqual(bundle.verify(self.archive), frozen)
        second = bundle.freeze(self.root, self.archive.with_name('later.zip'))
        self.assertNotEqual(frozen['snapshot_id'], second['snapshot_id'])

    def test_local_archive_runs_outside_original_repository(self):
        bundle.freeze(self.root, self.archive)
        bundle.verify(self.archive)
        isolated = Path(self.temp.name) / 'isolated'
        # Only our just-created, verified archive; never an arbitrary downloaded file.
        with zipfile.ZipFile(self.archive) as archive:
            archive.extractall(isolated)
        process = subprocess.run(
            [sys.executable, '-m', 'musca', '--demo', '--json', '--mode', 'light_chemical'],
            cwd=isolated, capture_output=True, encoding='utf-8', timeout=15,
        )
        self.assertEqual(process.returncode, 0, process.stderr)
        receipt = json.loads(process.stdout)
        self.assertEqual(receipt['result']['end_reason'], 'beacon')
        for name, digest in receipt['provenance']['source_sha256'].items():
            self.assertEqual(hashlib.sha256((isolated / name).read_bytes()).hexdigest(), digest)

    def test_cli_does_not_report_success_on_corrupt_file(self):
        self.archive.write_bytes(b'not a zip')
        process = subprocess.run(
            [sys.executable, str(ROOT / 'scripts/pilot_bundle.py'), 'verify', str(self.archive)],
            capture_output=True, encoding='utf-8', timeout=15,
        )
        self.assertEqual(process.returncode, 2)
        self.assertNotIn('Traceback', process.stderr)
        self.assertEqual(process.stdout, '')


if __name__ == '__main__':
    unittest.main()
