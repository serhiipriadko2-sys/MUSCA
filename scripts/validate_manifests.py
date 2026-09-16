"""Fail-closed validation for MUSCA dataset and experiment manifests."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


class ManifestError(ValueError):
    """Raised when a manifest violates the requested validation level."""


DATASET_FIELDS = {
    "name": str,
    "organism": str,
    "sex": str,
    "anatomical_scope": str,
    "release": str,
    "source": str,
    "retrieved_at": str,
    "citation": str,
    "license": str,
    "content_hash_or_source_snapshot": str,
    "local_storage": str,
    "transformations": list,
}

EXPERIMENT_FIELDS = {
    "id": str,
    "question": str,
    "hypothesis": str,
    "falsifier": str,
    "code_commit": (str, type(None)),
    "dataset_manifest": (str, type(None)),
    "model": str,
    "task": str,
    "controls": list,
    "fixed_conditions": list,
    "train_worlds": list,
    "validation_worlds": list,
    "test_worlds": list,
    "random_seeds": list,
    "repetitions": int,
    "metrics": list,
    "compute_budget": str,
    "analysis_plan": str,
    "success_criterion": str,
    "result_path": str,
    "study_type": str,
    "trainable_parameters": list,
    "confounds": list,
    "status": str,
}

BLOCKING_MARKERS = (
    "PENDING",
    "PLANNED:",
    "NOT_RETRIEVED",
    "NOT_RUN",
    "DRAFT",
)


def _load(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ManifestError(f"{path}: unreadable JSON: {exc}") from exc
    if not isinstance(value, dict):
        raise ManifestError(f"{path}: top level must be an object")
    return value


def _validate_fields(
    path: Path, section: dict[str, Any], fields: dict[str, Any]
) -> None:
    for name, expected in fields.items():
        if name not in section:
            raise ManifestError(f"{path}: missing required field {name!r}")
        value = section[name]
        if not isinstance(value, expected):
            raise ManifestError(
                f"{path}: field {name!r} has invalid type {type(value).__name__}"
            )
        if isinstance(value, str) and not value.strip():
            raise ManifestError(f"{path}: field {name!r} must not be blank")
    repetitions = section.get("repetitions")
    if isinstance(repetitions, bool):
        raise ManifestError(f"{path}: repetitions must be an integer, not bool")


def _has_blocking_marker(value: str) -> bool:
    upper = value.upper()
    return any(marker in upper for marker in BLOCKING_MARKERS)


def validate_dataset(path: Path, *, runnable: bool = False) -> dict[str, Any]:
    data = _load(path)
    section = data.get("dataset")
    if not isinstance(section, dict):
        raise ManifestError(f"{path}: missing object 'dataset'")
    _validate_fields(path, section, DATASET_FIELDS)
    if not runnable:
        return data
    for name in ("retrieved_at", "content_hash_or_source_snapshot", "local_storage"):
        if _has_blocking_marker(section[name]):
            raise ManifestError(f"{path}: runnable dataset blocked by {name!r}")
    return data


def _is_frozen_code_ref(value: str) -> bool:
    candidate = value.rsplit("@", 1)[-1]
    return len(candidate) == 40 and all(char in "0123456789abcdefABCDEF" for char in candidate)


def _resolve_dataset(repo_root: Path, reference: str) -> Path:
    direct = repo_root / "data" / "manifests" / Path(reference).name
    if not direct.is_file():
        raise ManifestError(f"dataset manifest not found: {direct}")
    return direct


def validate_experiment(
    path: Path, *, runnable: bool = False, repo_root: Path | None = None
) -> dict[str, Any]:
    data = _load(path)
    section = data.get("experiment")
    if not isinstance(section, dict):
        raise ManifestError(f"{path}: missing object 'experiment'")
    _validate_fields(path, section, EXPERIMENT_FIELDS)
    if not runnable:
        return data
    if section["repetitions"] <= 0:
        raise ManifestError(f"{path}: repetitions must be positive")
    if not section["metrics"]:
        raise ManifestError(f"{path}: runnable experiment requires metrics")
    if not section["fixed_conditions"]:
        raise ManifestError(f"{path}: runnable experiment requires fixed_conditions")
    status = section["status"]
    if _has_blocking_marker(status):
        raise ManifestError(f"{path}: runnable experiment blocked by status={status!r}")
    code_ref = section["code_commit"]
    if not isinstance(code_ref, str) or not _is_frozen_code_ref(code_ref):
        raise ManifestError(f"{path}: runnable experiment requires a frozen 40-hex code ref")
    result_path = section["result_path"]
    if _has_blocking_marker(result_path):
        raise ManifestError(f"{path}: runnable experiment has unresolved result_path")
    dataset_ref = section["dataset_manifest"]
    if not isinstance(dataset_ref, str):
        raise ManifestError(f"{path}: runnable experiment requires dataset_manifest")
    if dataset_ref.lower().startswith("not_applicable"):
        return data
    if repo_root is None:
        raise ManifestError(f"{path}: --repo-root is required to verify dataset linkage")
    validate_dataset(_resolve_dataset(repo_root, dataset_ref), runnable=True)
    return data


def validate_path(path: Path, *, level: str, repo_root: Path | None) -> str:
    data = _load(path)
    runnable = level == "runnable"
    if "dataset" in data:
        validate_dataset(path, runnable=runnable)
        return "dataset"
    if "experiment" in data:
        validate_experiment(path, runnable=runnable, repo_root=repo_root)
        return "experiment"
    raise ManifestError(f"{path}: expected top-level 'dataset' or 'experiment'")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="+", type=Path)
    parser.add_argument("--level", choices=("schema", "runnable"), default="schema")
    parser.add_argument("--repo-root", type=Path)
    args = parser.parse_args(argv)
    failures = 0
    for path in args.paths:
        try:
            kind = validate_path(path, level=args.level, repo_root=args.repo_root)
            print(f"PASS {args.level} {kind}: {path}")
        except ManifestError as exc:
            failures += 1
            print(f"FAIL {args.level}: {exc}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
