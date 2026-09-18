"""Fail-closed tests for MUSCA dataset/experiment manifest validation."""

import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location(
    "validate_manifests", ROOT / "scripts/validate_manifests.py"
)
validator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)


def dataset_payload(**changes):
    section = {
        "name": "fixture",
        "organism": "Drosophila melanogaster",
        "sex": "female",
        "anatomical_scope": "test",
        "release": "v1",
        "source": "fixture-source",
        "retrieved_at": "2026-09-16T00:00:00Z",
        "citation": "fixture citation",
        "license": "test-only",
        "content_hash_or_source_snapshot": "sha256=" + "a" * 64,
        "local_storage": "E:\\fixture\\data",
        "transformations": [],
    }
    section.update(changes)
    return {"dataset": section}


def experiment_payload(**changes):
    section = {
        "id": "EXP-TEST",
        "question": "Does the fixture execute?",
        "hypothesis": "It executes under frozen conditions.",
        "falsifier": "Execution fails.",
        "code_commit": "b" * 40,
        "dataset_manifest": "fixture-dataset.json",
        "model": "fixture model",
        "task": "fixture task",
        "controls": [],
        "fixed_conditions": ["frozen fixture"],
        "train_worlds": [],
        "validation_worlds": [],
        "test_worlds": [],
        "random_seeds": [],
        "repetitions": 1,
        "metrics": ["exit_status"],
        "compute_budget": "bounded",
        "analysis_plan": "report result",
        "success_criterion": "exit_status == 0",
        "result_path": "experiments/results/fixture",
        "study_type": "fixture",
        "trainable_parameters": [],
        "confounds": [],
        "status": "ready_to_run",
    }
    section.update(changes)
    return {"experiment": section}


class ManifestValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def write_json(self, relative, payload):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(payload), encoding="utf-8")
        return path

    def test_current_registered_manifests_are_schema_valid(self):
        paths = [
            ROOT / "data/manifests/SCI-DATA-SHIU-FW630.candidate.json",
            ROOT / "experiments/manifests/SCI-R00-SHIU-ENV-DATA.candidate.json",
            ROOT / "experiments/manifests/SCI-R01-SHIU-SUGAR.candidate.json",
            ROOT / "experiments/manifests/SCI-R01-SHIU-SUGAR.run.json",
            ROOT / "experiments/manifests/SCI-R02-SHIU-MN9-LATERALITY-200HZ.candidate.json",
        ]
        for path in paths:
            with self.subTest(path=path.name):
                validator.validate_path(path, level="schema", repo_root=ROOT)

    def test_templates_fail_schema_because_required_values_are_null(self):
        paths = [
            ROOT / "data/manifests/dataset.template.json",
            ROOT / "experiments/manifests/experiment.template.json",
        ]
        for path in paths:
            with self.subTest(path=path.name):
                with self.assertRaises(validator.ManifestError):
                    validator.validate_path(path, level="schema", repo_root=ROOT)

    def test_retrieved_dataset_is_runnable_with_fixture_storage(self):
        # Real-host dataset readiness is a separate integration command, not a
        # prerequisite for running this suite on a clean checkout or CI host.
        payload = json.loads((ROOT / "data/manifests/SCI-DATA-SHIU-FW630.candidate.json").read_text(encoding="utf-8"))
        storage = self.root / "retrieved-dataset"
        storage.mkdir()
        payload["dataset"]["local_storage"] = str(storage)
        path = self.write_json("retrieved.json", payload)
        validator.validate_dataset(path, runnable=True)

    def test_experiment_candidate_is_not_runnable_while_draft(self):
        path = ROOT / "experiments/manifests/SCI-R01-SHIU-SUGAR.candidate.json"
        with self.assertRaises(validator.ManifestError):
            validator.validate_experiment(path, runnable=True, repo_root=ROOT)

    def test_missing_required_field_fails_closed(self):
        payload = dataset_payload()
        del payload["dataset"]["citation"]
        path = self.write_json("missing.json", payload)
        with self.assertRaises(validator.ManifestError):
            validator.validate_dataset(path)

    def test_blank_required_string_fails_closed(self):
        path = self.write_json("blank.json", dataset_payload(source="   "))
        with self.assertRaises(validator.ManifestError):
            validator.validate_dataset(path)

    def test_bool_repetitions_is_rejected(self):
        path = self.write_json("bool.json", experiment_payload(repetitions=True))
        with self.assertRaises(validator.ManifestError):
            validator.validate_experiment(path)

    def test_runnable_experiment_checks_linked_dataset(self):
        storage = self.root / "retrieved-dataset"
        storage.mkdir()
        dataset = self.write_json(
            "data/manifests/fixture-dataset.json",
            dataset_payload(local_storage=str(storage)),
        )
        experiment = self.write_json(
            "experiments/manifests/fixture.json", experiment_payload()
        )
        validator.validate_dataset(dataset, runnable=True)
        validator.validate_experiment(experiment, runnable=True, repo_root=self.root)

    def test_runnable_experiment_rejects_pending_dataset_hash(self):
        self.write_json(
            "data/manifests/fixture-dataset.json",
            dataset_payload(content_hash_or_source_snapshot="sha256=PENDING_RETRIEVAL"),
        )
        experiment = self.write_json(
            "experiments/manifests/fixture.json", experiment_payload()
        )
        with self.assertRaises(validator.ManifestError):
            validator.validate_experiment(experiment, runnable=True, repo_root=self.root)

    def test_not_applicable_dataset_is_allowed_for_runnable_non_dataset_study(self):
        experiment = self.write_json(
            "game.json",
            experiment_payload(dataset_manifest="not_applicable: no external dataset"),
        )
        validator.validate_experiment(experiment, runnable=True, repo_root=self.root)
    def test_runnable_dataset_requires_existing_local_storage(self):
        path = self.write_json(
            "dataset.json",
            dataset_payload(local_storage=str(self.root / "missing-dataset")),
        )
        with self.assertRaises(validator.ManifestError):
            validator.validate_dataset(path, runnable=True)

    def test_not_applicable_dataset_requires_reason(self):
        experiment = self.write_json(
            "game-no-reason.json",
            experiment_payload(dataset_manifest="not_applicable"),
        )
        with self.assertRaises(validator.ManifestError):
            validator.validate_experiment(experiment, runnable=True, repo_root=self.root)

    def test_cli_returns_nonzero_for_runnable_draft(self):
        path = ROOT / "experiments/manifests/SCI-R01-SHIU-SUGAR.candidate.json"
        exit_code = validator.main(
            ["--level", "runnable", "--repo-root", str(ROOT), str(path)]
        )
        self.assertEqual(exit_code, 1)


if __name__ == "__main__":
    unittest.main()
