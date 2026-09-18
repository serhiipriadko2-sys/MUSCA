"""Negative integrity cases with temporary fixtures; never launches Blender."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location(
    'validate_blender_form', Path(__file__).resolve().parents[1] / 'scripts/validate_blender_form.py')
validator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)


class BlenderFormValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.form = Path(self.temp.name) / 'form'
        self.evidence = Path(self.temp.name) / 'evidence'
        self.evidence.mkdir()
        (self.form / 'receipts').mkdir(parents=True)
        (self.evidence / 'room-layout.json').write_text('{"fixture": true}', encoding='utf-8')
        artifacts = {}
        for name, relative in validator.ARTIFACTS.items():
            path = self.form / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(('fixture:' + name).encode())
            artifacts[name] = {'bytes': path.stat().st_size, 'sha256': validator.sha256(path)}
        self.documents = {
            'form-build.json': {
                'schema': 'musca.blender-form-build.v1', 'room_id': 'gate-lab-v0.2', 'status': 'PASS',
                'checks': dict.fromkeys(validator.BUILD_CHECKS, True), 'route_blockers': [],
                'exports': {'environment_fbx': 'PASS', 'musca_fbx': 'PASS'},
                'source_function_layout_sha256': validator.sha256(self.evidence / 'room-layout.json'),
                'artifacts': artifacts,
            },
            'form-readback.json': {
                'schema': 'musca.blender-form-readback.v1', 'status': 'PASS',
                'checks': dict.fromkeys(validator.READBACK_CHECKS, True), 'route_blockers': [],
                **{key: artifacts[name]['sha256'] for key, name in validator.READBACK_HASHES.items()},
            },
            'form-visual-qc.json': {
                'schema': 'musca.blender-form-visual-qc.v1', 'room_id': 'gate-lab-v0.2',
                'status': 'PASS_AS_FORM_CANDIDATE', 'checks': dict(validator.VISUAL_CHECKS),
                'reviewed_renders': {name: artifacts[name]['sha256'] for name in validator.RENDERS},
            },
        }
        self.reviews = {'reviews': {
            'function': {'status': 'approved', 'human_approval': {'status': 'approved'}},
            'form': {'status': 'ready_for_human_approval', 'human_approval': None},
        }}
        self.write_documents()

    def write_documents(self):
        for name, document in self.documents.items():
            (self.form / 'receipts' / name).write_text(json.dumps(document), encoding='utf-8')
        (self.evidence / 'milestone-reviews.json').write_text(json.dumps(self.reviews), encoding='utf-8')

    def assert_fails(self):
        result = validator.validate(self.form, self.evidence)
        self.assertEqual('FAIL', result['status'], result)
        self.assertTrue(result['errors'])

    def test_complete_fixture_passes(self):
        result = validator.validate(self.form, self.evidence)
        self.assertEqual('PASS', result['status'], result)
        self.assertEqual(7, result['checked_artifacts'])

    def test_empty_artifacts_and_render_lists_fail(self):
        for name, key in [('form-build.json', 'artifacts'), ('form-visual-qc.json', 'reviewed_renders')]:
            for empty in ({}, []):
                with self.subTest(name=name, empty=empty):
                    original = self.documents[name][key]
                    self.documents[name][key] = empty
                    self.write_documents()
                    self.assert_fails()
                    self.documents[name][key] = original

    def test_false_or_missing_required_checks_fail(self):
        for name in ('form-build.json', 'form-readback.json'):
            for value in (False, 'true', 1):
                with self.subTest(name=name, value=value):
                    self.documents[name]['checks']['route_clear'] = value
                    self.write_documents()
                    self.assert_fails()
            del self.documents[name]['checks']['route_clear']
            self.write_documents()
            self.assert_fails()
            self.documents[name]['checks']['route_clear'] = True

    def test_additional_failed_check_is_not_ignored(self):
        self.documents['form-readback.json']['checks']['new_geometry_gate'] = False
        self.write_documents()
        self.assert_fails()

    def test_line_ending_only_layout_change_preserves_receipt(self):
        layout = self.evidence / 'room-layout.json'
        layout.write_bytes(b'{\r\n  "fixture": true\r\n}\r\n')
        self.documents['form-build.json']['source_function_layout_sha256'] = validator.sha256(layout)
        self.write_documents()
        layout.write_bytes(b'{\n  "fixture": true\n}\n')
        result = validator.validate(self.form, self.evidence)
        self.assertEqual('PASS', result['status'], result)

    def test_changed_layout_invalidates_receipt(self):
        (self.evidence / 'room-layout.json').write_text('{"fixture": false}', encoding='utf-8')
        self.assert_fails()

    def test_tampered_artifact_bytes_fail(self):
        path = self.form / validator.ARTIFACTS['form_spawn.png']
        content = path.read_bytes()
        path.write_bytes(b'!' + content[1:])
        self.assert_fails()

    def test_missing_artifact_fails_without_exception(self):
        (self.form / validator.ARTIFACTS['form_spawn.png']).unlink()
        self.assert_fails()

    def test_readback_must_match_build_artifact(self):
        self.documents['form-readback.json']['blend_sha256'] = '0' * 64
        self.write_documents()
        self.assert_fails()

    def test_visual_negative_condition_cannot_be_true(self):
        self.documents['form-visual-qc.json']['checks']['mirrored_or_upside_down_signage'] = True
        self.write_documents()
        self.assert_fails()

    def test_malformed_receipt_fails_without_exception(self):
        for malformed in ('{', '[]', 'null'):
            with self.subTest(malformed=malformed):
                (self.form / 'receipts/form-build.json').write_text(malformed, encoding='utf-8')
                self.assert_fails()

    def test_technical_only_validation_explicitly_skips_visuals(self):
        (self.form / 'receipts/form-visual-qc.json').unlink()
        self.assert_fails()
        result = validator.validate(self.form, self.evidence, require_visual=False)
        self.assertEqual('PASS', result['status'], result)
        self.assertEqual('not_run', result['visual_validation'])

    def test_lifecycle_cannot_silently_gain_approval(self):
        self.reviews['reviews']['form']['human_approval'] = {
            'status': 'approved',
            'date': '2026-09-18',
            'source': 'explicit_user_chat_approval',
            'scope': 'prototype only',
        }
        self.write_documents()
        self.assert_fails()

    def test_approved_form_requires_complete_typed_receipt(self):
        self.reviews['reviews']['form']['status'] = 'approved'
        self.reviews['reviews']['form']['human_approval'] = {'status': 'approved'}
        self.write_documents()
        self.assert_fails()

    def test_approved_form_with_typed_receipt_passes(self):
        self.reviews['reviews']['form']['status'] = 'approved'
        self.reviews['reviews']['form']['human_approval'] = {
            'status': 'approved',
            'date': '2026-09-18',
            'source': 'explicit_user_chat_approval',
            'scope': 'GateLab Form v0.31 prototype only',
        }
        self.write_documents()
        result = validator.validate(self.form, self.evidence)
        self.assertEqual('PASS', result['status'], result)


if __name__ == '__main__':
    unittest.main()
