"""Read-only SCI-R00 preflight for the strict Shiu/FlyWire-v630 lineage.

This tool never creates environments, directories, downloads, or simulations.
It reports whether the repository/host is ready to request a separate operational
approval for R00 materialization.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import platform
from pathlib import Path
import re
import shutil
import subprocess
from typing import Any


ROOT = Path(__file__).resolve().parent.parent
ADR_PATH = ROOT / "docs/adr/ADR-0005-shiu-v630-reproduction-baseline.md"
DATASET_MANIFEST = ROOT / "data/manifests/SCI-DATA-SHIU-FW630.candidate.json"
R00_MANIFEST = ROOT / "experiments/manifests/SCI-R00-SHIU-ENV-DATA.candidate.json"
DEFAULT_RESEARCH_ROOT = Path(r"E:\MUSCA_RESEARCH")
STRICT_SOLVERS = ("micromamba", "mamba", "conda")
UPSTREAM_COMMIT = "91bdd1e7dcf193f3e7ca5a8933497fcef63b7960"


spec = importlib.util.spec_from_file_location(
    "validate_manifests", ROOT / "scripts/validate_manifests.py"
)
validator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)

EXPECTED_UPSTREAM_FILES = {
    "2023_03_23_completeness_630_final.csv": {
        "git_blob": "be745f0ce054308df21accc5c4b3883aa38498f9",
        "bytes": 3057611,
    },
    "2023_03_23_connectivity_630_final.parquet": {
        "git_blob": "8b4d9531bc0acbda7c2074ae577e6ac9e2fca166",
        "bytes": 86630944,
    },
    "environment_full.yml": {
        "git_blob": "1428b314d40bf8b7dc2cb3991db213c12033fdfc",
        "bytes": 3445,
    },
    "model.py": {
        "git_blob": "5ba7083cf55bf6092967f8d9065e86cd677efed1",
        "bytes": 11900,
    },
}


def _read_adr_status(path: Path) -> str:
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as exc:
        raise RuntimeError(f"cannot read ADR: {path}: {exc}") from exc
    match = re.search(r"^Status:\s*`([^`]+)`", text, flags=re.MULTILINE)
    if not match:
        raise RuntimeError(f"ADR status not found: {path}")
    return match.group(1).strip().lower()


def _probe_command(name: str, args: tuple[str, ...] = ("--version",)) -> dict[str, Any]:
    executable = shutil.which(name)
    if executable is None:
        return {"present": False, "executable": None, "version": None}
    try:
        completed = subprocess.run(
            [executable, *args],
            capture_output=True,
            text=True,
            timeout=5,
            check=False,
        )
    except (OSError, subprocess.SubprocessError) as exc:
        return {"present": True, "executable": executable, "version": None, "error": str(exc)}
    output = (completed.stdout or completed.stderr).strip().splitlines()
    return {
        "present": True,
        "executable": executable,
        "version": output[0] if output else None,
        "exit_code": completed.returncode,
    }


def _probe_executable_path(path: Path, args: tuple[str, ...] = ("--version",)) -> dict[str, Any]:
    result: dict[str, Any] = {
        "present": path.is_file(),
        "path": str(path),
        "bytes": None,
        "runnable": False,
        "version": None,
    }
    if not result["present"]:
        return result
    try:
        result["bytes"] = path.stat().st_size
    except OSError as exc:
        result["stat_error"] = str(exc)
        return result
    try:
        completed = subprocess.run(
            [str(path), *args],
            capture_output=True,
            text=True,
            timeout=5,
            check=False,
        )
    except (OSError, subprocess.SubprocessError) as exc:
        result["run_error"] = str(exc)
        return result
    output = (completed.stdout or completed.stderr).strip().splitlines()
    result["exit_code"] = completed.returncode
    result["runnable"] = completed.returncode == 0
    result["version"] = output[0] if output else None
    return result


def _observe_file(path: Path) -> dict[str, Any]:
    result: dict[str, Any] = {"present": path.is_file(), "path": str(path), "bytes": None}
    if result["present"]:
        try:
            result["bytes"] = path.stat().st_size
        except OSError as exc:
            result["stat_error"] = str(exc)
    return result


def _observe_preexisting_state(research_root: Path) -> dict[str, Any]:
    tools = research_root / "tools"
    downloads = research_root / "downloads"
    source_root = research_root / "shiu-91bdd1e7"
    micromamba_name = "micromamba.exe" if platform.system() == "Windows" else "micromamba"
    return {
        "research_root_exists": research_root.exists(),
        "mamba_root_exists": (research_root / "mamba-root").exists(),
        "source_dir_exists": (source_root / "source").exists(),
        "source_snapshot_exists": (source_root / "source-snapshot").exists(),
        "project_solver_candidates": {
            "micromamba": _probe_executable_path(tools / micromamba_name),
        },
        "known_transfer_artifacts": {
            "micromamba_archive": _observe_file(downloads / "micromamba-win-64-latest.tar.bz2"),
            "source_zip_authoritative": _observe_file(downloads / "Drosophila_brain_model-91bdd1e7.zip"),
            "source_qc_receipt": _observe_file(research_root / "receipts" / "2026-09-16-r00-source-artifact-qc.json"),
        },
    }


def _probe_python310() -> dict[str, Any]:
    launcher = shutil.which("py")
    if launcher is None:
        return {"present": False, "launcher_present": False, "launcher": None, "version": None}
    probe = _probe_command("py", ("-3.10", "--version"))
    probe["launcher_present"] = probe["present"]
    probe["launcher"] = launcher
    probe["present"] = bool(probe["present"] and probe.get("exit_code") == 0)
    return probe


def _probe_storage(target: Path) -> dict[str, Any]:
    anchor = Path(target.anchor) if target.anchor else target
    anchor_exists = anchor.exists()
    result: dict[str, Any] = {
        "target": str(target),
        "target_exists": target.exists(),
        "anchor": str(anchor),
        "anchor_exists": anchor_exists,
        "free_bytes": None,
    }
    if anchor_exists:
        try:
            result["free_bytes"] = shutil.disk_usage(anchor).free
        except OSError as exc:
            result["error"] = str(exc)
    return result


def _blocker(code: str, message: str) -> dict[str, str]:
    return {"code": code, "message": message}


def assess(
    *,
    repo_root: Path = ROOT,
    research_root: Path = DEFAULT_RESEARCH_ROOT,
    adr_path: Path | None = None,
    dataset_manifest: Path | None = None,
    r00_manifest: Path | None = None,
) -> dict[str, Any]:
    adr_path = adr_path or repo_root / ADR_PATH.relative_to(ROOT)
    dataset_manifest = dataset_manifest or repo_root / DATASET_MANIFEST.relative_to(ROOT)
    r00_manifest = r00_manifest or repo_root / R00_MANIFEST.relative_to(ROOT)
    blockers: list[dict[str, str]] = []
    warnings: list[str] = []

    manifest_state: dict[str, Any] = {"dataset_schema": "unknown", "r00_schema": "unknown"}
    try:
        validator.validate_dataset(dataset_manifest, runnable=False)
        manifest_state["dataset_schema"] = "pass"
    except validator.ManifestError as exc:
        manifest_state["dataset_schema"] = "fail"
        blockers.append(_blocker("manifest", str(exc)))
    try:
        validator.validate_experiment(r00_manifest, runnable=False, repo_root=repo_root)
        manifest_state["r00_schema"] = "pass"
    except validator.ManifestError as exc:
        manifest_state["r00_schema"] = "fail"
        blockers.append(_blocker("manifest", str(exc)))

    try:
        adr_status = _read_adr_status(adr_path)
    except RuntimeError as exc:
        adr_status = "unreadable"
        blockers.append(_blocker("governance", str(exc)))
    if adr_status != "accepted":
        blockers.append(
            _blocker(
                "governance",
                f"ADR-0005 must be accepted before SCI-R00 materialization; observed status={adr_status!r}",
            )
        )

    git_probe = _probe_command("git")
    if not git_probe["present"] or git_probe.get("exit_code") != 0:
        blockers.append(_blocker("host_git", "git version probe did not complete successfully"))

    solver_probes = {name: _probe_command(name) for name in STRICT_SOLVERS}
    strict_solver = next((name for name, probe in solver_probes.items() if probe["present"] and probe.get("exit_code") == 0), None)
    if strict_solver is None:
        local_name = "micromamba.exe" if platform.system() == "Windows" else "micromamba"
        local_probe = _probe_executable_path(research_root / "tools" / local_name)
        if local_probe.get("runnable"):
            solver_probes["micromamba"] = {
                "present": True,
                "executable": local_probe["path"],
                "version": local_probe.get("version"),
                "exit_code": local_probe.get("exit_code"),
                "source": "project_local",
            }
            strict_solver = "micromamba"
    if strict_solver is None:
        blockers.append(
            _blocker(
                "host_solver",
                "strict environment_full.yml requires a runnable conda-compatible solver on PATH or under the project-local research tools directory",
            )
        )

    python310_probe = _probe_python310()
    if not python310_probe["present"]:
        warnings.append(
            "Python 3.10 is not preinstalled; this is not a blocker if the strict conda-compatible solver can materialize Python 3.10.11."
        )

    uv_probe = _probe_command("uv")
    if uv_probe["present"] and strict_solver is None:
        warnings.append(
            "uv is present but is not treated as a substitute for solving the pinned conda environment_full.yml."
        )

    storage_probe = _probe_storage(research_root)
    preexisting_state = _observe_preexisting_state(research_root)
    if not storage_probe["anchor_exists"]:
        blockers.append(
            _blocker(
                "host_storage",
                f"planned research storage anchor does not exist: {storage_probe['anchor']}",
            )
        )

    warnings.append(
        "Network reachability and upstream byte retrieval are intentionally not tested by this read-only preflight."
    )

    disposition = "READY_FOR_OPERATIONAL_APPROVAL" if not blockers else "BLOCKED"
    return {
        "receipt_version": 2,
        "kind": "SCI-R00-read-only-preflight",
        "disposition": disposition,
        "blockers": blockers,
        "warnings": warnings,
        "governance": {"adr": str(adr_path), "status": adr_status},
        "manifests": manifest_state,
        "host": {
            "platform": platform.platform(),
            "python": platform.python_version(),
            "git": git_probe,
            "strict_solvers": solver_probes,
            "selected_strict_solver": strict_solver,
            "python310": python310_probe,
            "uv": uv_probe,
            "storage": storage_probe,
        },
        "upstream": {
            "repository": "philshiu/Drosophila_brain_model",
            "commit": UPSTREAM_COMMIT,
            "expected_files": EXPECTED_UPSTREAM_FILES,
        },
        "observed_preexisting_state": preexisting_state,
        "self_effects": {
            "scope": "preflight_process_only",
            "writes_performed": 0,
            "downloads_performed": 0,
            "installs_performed": 0,
            "simulations_run": 0,
        },
        "claim_boundary": "Preflight self_effects describe only this process; they do not prove the surrounding session performed zero writes. Preflight readiness is not experiment execution and does not reproduce a neuroscience result.",
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=ROOT)
    parser.add_argument("--research-root", type=Path, default=DEFAULT_RESEARCH_ROOT)
    parser.add_argument(
        "--require-blocker",
        help="Return non-zero unless a blocker with this exact code is present.",
    )
    parser.add_argument(
        "--require-ready",
        action="store_true",
        help="Return non-zero unless disposition is READY_FOR_OPERATIONAL_APPROVAL.",
    )
    args = parser.parse_args(argv)
    receipt = assess(repo_root=args.repo_root, research_root=args.research_root)
    print(json.dumps(receipt, ensure_ascii=False, indent=2, sort_keys=True))

    if args.require_blocker is not None:
        observed = {item["code"] for item in receipt["blockers"]}
        if args.require_blocker not in observed:
            return 3
    if args.require_ready and receipt["disposition"] != "READY_FOR_OPERATIONAL_APPROVAL":
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
