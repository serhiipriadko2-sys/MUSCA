"""Tests for the read-only SCI-R00 preflight gate."""

import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest import mock


ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location(
    "research_r00_preflight", ROOT / "scripts/research_r00_preflight.py"
)
preflight = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preflight)


DATASET = ROOT / "data/manifests/SCI-DATA-SHIU-FW630.candidate.json"
R00 = ROOT / "experiments/manifests/SCI-R00-SHIU-ENV-DATA.candidate.json"


def host_command_probe(*present_names):
    present = set(present_names)

    def probe(name, args=("--version",)):
        return {
            "present": name in present,
            "executable": name if name in present else None,
            "version": f"{name} fixture" if name in present else None,
        }

    return probe


class R00PreflightTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def accepted_adr(self):
        path = self.root / "ADR.md"
        path.write_text("# ADR\n\nStatus: `accepted`\n", encoding="utf-8")
        return path

    def ready_storage(self, target=None):
        return {
            "target": str(target or self.root / "research"),
            "target_exists": False,
            "anchor": str(self.root),
            "anchor_exists": True,
            "free_bytes": 50 * 1024**3,
        }

    def test_current_r00_manifest_is_schema_valid(self):
        preflight.validator.validate_experiment(R00, runnable=False, repo_root=ROOT)

    def test_proposed_adr_blocks_even_when_host_is_ready(self):
        with (
            mock.patch.object(preflight, "_probe_command", side_effect=host_command_probe("git", "conda", "uv")),
            mock.patch.object(preflight, "_probe_python310", return_value={"present": False}),
            mock.patch.object(preflight, "_probe_storage", side_effect=self.ready_storage),
        ):
            proposed = self.root / "proposed-ADR.md"
            proposed.write_text("# ADR\n\nStatus: `proposed`\n", encoding="utf-8")
            receipt = preflight.assess(repo_root=ROOT, research_root=self.root / "research", adr_path=proposed)
        codes = {item["code"] for item in receipt["blockers"]}
        self.assertIn("governance", codes)
        self.assertEqual(receipt["disposition"], "BLOCKED")
        self.assertEqual(receipt["self_effects"]["writes_performed"], 0)
        self.assertEqual(receipt["self_effects"]["scope"], "preflight_process_only")

    def test_accepted_adr_with_strict_solver_reaches_approval_boundary(self):
        with (
            mock.patch.object(preflight, "_probe_command", side_effect=host_command_probe("git", "conda", "uv")),
            mock.patch.object(preflight, "_probe_python310", return_value={"present": False}),
            mock.patch.object(preflight, "_probe_storage", side_effect=self.ready_storage),
        ):
            receipt = preflight.assess(repo_root=ROOT, research_root=self.root / "research", adr_path=self.accepted_adr())
        self.assertEqual(receipt["disposition"], "READY_FOR_OPERATIONAL_APPROVAL")
        self.assertEqual(receipt["blockers"], [])
        self.assertEqual(receipt["host"]["selected_strict_solver"], "conda")

    def test_missing_conda_compatible_solver_blocks_strict_lineage(self):
        with (
            mock.patch.object(preflight, "_probe_command", side_effect=host_command_probe("git", "uv")),
            mock.patch.object(preflight, "_probe_python310", return_value={"present": False}),
            mock.patch.object(preflight, "_probe_storage", side_effect=self.ready_storage),
        ):
            receipt = preflight.assess(repo_root=ROOT, research_root=self.root / "research", adr_path=self.accepted_adr())
        codes = {item["code"] for item in receipt["blockers"]}
        self.assertIn("host_solver", codes)
        self.assertEqual(receipt["disposition"], "BLOCKED")

    def test_project_local_micromamba_satisfies_solver_gate(self):
        research_root = self.root / "research"
        local_probe = {
            "present": True,
            "path": str(research_root / "tools" / "micromamba.exe"),
            "bytes": 123,
            "runnable": True,
            "version": "2.9.0",
            "exit_code": 0,
        }
        with (
            mock.patch.object(preflight, "_probe_command", side_effect=host_command_probe("git", "uv")),
            mock.patch.object(preflight, "_probe_python310", return_value={"present": False}),
            mock.patch.object(preflight, "_probe_storage", side_effect=self.ready_storage),
            mock.patch.object(preflight, "_probe_executable_path", return_value=local_probe),
        ):
            receipt = preflight.assess(repo_root=ROOT, research_root=research_root, adr_path=self.accepted_adr())
        self.assertEqual(receipt["disposition"], "READY_FOR_OPERATIONAL_APPROVAL")
        self.assertEqual(receipt["host"]["selected_strict_solver"], "micromamba")
        self.assertEqual(receipt["host"]["strict_solvers"]["micromamba"]["source"], "project_local")

    def test_missing_storage_anchor_blocks(self):
        missing = {
            "target": "Z:\\MUSCA_RESEARCH",
            "target_exists": False,
            "anchor": "Z:\\",
            "anchor_exists": False,
            "free_bytes": None,
        }
        with (
            mock.patch.object(preflight, "_probe_command", side_effect=host_command_probe("git", "conda")),
            mock.patch.object(preflight, "_probe_python310", return_value={"present": False}),
            mock.patch.object(preflight, "_probe_storage", return_value=missing),
        ):
            receipt = preflight.assess(repo_root=ROOT, research_root=self.root / "research", adr_path=self.accepted_adr())
        self.assertIn("host_storage", {item["code"] for item in receipt["blockers"]})

    def test_invalid_dataset_manifest_blocks(self):
        bad_dataset = self.root / "bad-dataset.json"
        bad_dataset.write_text(json.dumps({"dataset": {"name": "only-name"}}), encoding="utf-8")
        with (
            mock.patch.object(preflight, "_probe_command", side_effect=host_command_probe("git", "conda")),
            mock.patch.object(preflight, "_probe_python310", return_value={"present": False}),
            mock.patch.object(preflight, "_probe_storage", side_effect=self.ready_storage),
        ):
            receipt = preflight.assess(
                repo_root=ROOT,
                research_root=self.root / "research",
                adr_path=self.accepted_adr(),
                dataset_manifest=bad_dataset,
                r00_manifest=R00,
            )
        self.assertIn("manifest", {item["code"] for item in receipt["blockers"]})

    def test_storage_probe_does_not_create_target(self):
        target = self.root / "does-not-exist" / "research"
        self.assertFalse(target.exists())
        result = preflight._probe_storage(target)
        self.assertTrue(result["anchor_exists"])
        self.assertFalse(target.exists())

    def test_upstream_pin_metadata_is_frozen(self):
        self.assertEqual(
            preflight.UPSTREAM_COMMIT,
            "91bdd1e7dcf193f3e7ca5a8933497fcef63b7960",
        )
        self.assertEqual(
            preflight.EXPECTED_UPSTREAM_FILES["2023_03_23_connectivity_630_final.parquet"]["bytes"],
            86630944,
        )

    def test_receipt_scopes_self_effects_and_removes_ambiguous_legacy_key(self):
        with (
            mock.patch.object(preflight, "_probe_command", side_effect=host_command_probe("git", "conda")),
            mock.patch.object(preflight, "_probe_python310", return_value={"present": False}),
            mock.patch.object(preflight, "_probe_storage", side_effect=self.ready_storage),
        ):
            receipt = preflight.assess(
                repo_root=ROOT,
                research_root=self.root / "research",
                adr_path=self.accepted_adr(),
            )
        self.assertEqual(receipt["receipt_version"], 2)
        self.assertNotIn("effects", receipt)
        self.assertEqual(receipt["self_effects"]["scope"], "preflight_process_only")
        self.assertEqual(receipt["self_effects"]["writes_performed"], 0)

    def test_preexisting_state_observes_project_local_solver_without_selecting_it(self):
        research = self.root / "research"
        tool = research / "tools" / ("micromamba.exe" if preflight.platform.system() == "Windows" else "micromamba")
        tool.parent.mkdir(parents=True)
        tool.write_bytes(b"not-an-executable")
        state = preflight._observe_preexisting_state(research)
        observed = state["project_solver_candidates"]["micromamba"]
        self.assertTrue(state["research_root_exists"])
        self.assertTrue(observed["present"])
        self.assertEqual(observed["bytes"], len(b"not-an-executable"))
        self.assertFalse(observed["runnable"])

    def test_cli_can_require_current_host_solver_blocker(self):
        code = preflight.main(["--repo-root", str(ROOT), "--research-root", str(self.root / "research"), "--require-blocker", "host_solver"])
        self.assertEqual(code, 0)

    def test_cli_require_ready_fails_closed_on_current_state(self):
        code = preflight.main(["--repo-root", str(ROOT), "--research-root", str(self.root / "research"), "--require-ready"])
        self.assertEqual(code, 2)


if __name__ == "__main__":
    unittest.main()
