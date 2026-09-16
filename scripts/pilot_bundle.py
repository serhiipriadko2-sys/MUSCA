"""Create or inspect a bounded local pilot snapshot; never execute archived code."""

import argparse
from collections import Counter
import hashlib
import io
import json
from pathlib import Path
import posixpath
import re
import stat
import sys
import zipfile
import zlib


FORMAT = 'musca-local-pilot-v2'
MANIFEST = 'experiments/manifests/gate-p01.json'
MAX_FILE_BYTES = 256 * 1024
MAX_ARCHIVE_BYTES = 8 * 1024 * 1024
# Deliberately explicit: adding a file to the repository does not put it in a bundle.
V1_FILES = (
    '.gitignore', '.python-version', 'AGENTS.md', 'README.md',
    'data/manifests/dataset.template.json',
    'docs/GAME_VISION.md', 'docs/PILOT_BUILD.md', 'docs/PLAYTEST_PROTOCOL.md',
    'docs/PROJECT_CHARTER.md', 'docs/RESEARCH_EVIDENCE.md', 'docs/RESEARCH_PROTOCOL.md',
    'docs/adr/ADR-0001-project-boundaries.md',
    'docs/adr/ADR-0002-local-prototype-runtime.md',
    'docs/adr/ADR-0003-bridge-contract.md',
    'docs/adr/ADR-0004-terminal-gate-puzzle.md',
    'docs/status/2026-09-16-audit.md', 'docs/status/2026-09-16-gate.md',
    'docs/status/2026-09-16-pilot-build.md', 'docs/status/CURRENT.md',
    'experiments/manifests/experiment.template.json', MANIFEST,
    'musca/__init__.py', 'musca/__main__.py', 'musca/backends.py',
    'musca/bridge.py', 'musca/contracts.py', 'musca/interpretation.py',
    'musca/puzzle.py', 'musca/simulation.py', 'musca/world.py',
    'scripts/pilot_bundle.py', 'tests/test_bundle.py', 'tests/test_cli.py',
    'tests/test_contracts.py', 'tests/test_puzzle.py', 'tests/test_simulation.py',
)
PAYLOAD_FILES = V1_FILES + (
    '.github/workflows/ci.yml',
    'data/manifests/SCI-DATA-SHIU-FW630.candidate.json',
    'docs/LICENSING_DECISION.md',
    'docs/adr/ADR-0005-shiu-v630-reproduction-baseline.md',
    'docs/research/SCI_R00_R01_REPRO_PLAN.md',
    'docs/status/2026-09-16-concurrent-ops-drift.md',
    'docs/status/2026-09-16-gate0-candidate.md',
    'docs/status/2026-09-16-gate0.md',
    'docs/status/2026-09-16-manifest-validator.md',
    'docs/status/2026-09-16-r00-preflight.md',
    'docs/status/2026-09-16-r00-source-provenance.md',
    'docs/status/2026-09-16-science-prereg.md',
    'docs/status/2026-09-16-integrated-audit.md',
    'experiments/manifests/SCI-R00-SHIU-ENV-DATA.candidate.json',
    'experiments/manifests/SCI-R01-SHIU-SUGAR.candidate.json',
    'scripts/research_r00_preflight.py', 'scripts/validate_manifests.py',
    'tests/test_manifests.py', 'tests/test_research_r00_preflight.py',
)
PROFILES = {'musca-local-pilot-v1': V1_FILES, FORMAT: PAYLOAD_FILES}


class BundleError(ValueError):
    """The snapshot violates its bounded, explicit format."""


def _sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _json(data: object) -> bytes:
    return (json.dumps(data, sort_keys=True, ensure_ascii=False, indent=2) + '\n').encode('utf-8')


def _unique_object(pairs: list) -> dict:
    result = {}
    for key, value in pairs:
        if key in result:
            raise BundleError('duplicate JSON key')
        result[key] = value
    return result


def _parse(data: bytes) -> dict:
    try:
        result = json.loads(data.decode('utf-8'), object_pairs_hook=_unique_object)
    except (UnicodeError, ValueError) as exc:
        raise BundleError('invalid JSON metadata') from exc
    if not isinstance(result, dict):
        raise BundleError('JSON root must be an object')
    return result


def _validate_manifest(data: bytes) -> dict:
    experiment = _parse(data).get('experiment')
    if not isinstance(experiment, dict):
        raise BundleError('missing experiment object')
    if (experiment.get('id') != 'GATE-P01-v0.1'
            or experiment.get('status') != 'draft_not_run'
            or experiment.get('study_type') != 'exploratory_game_pilot'
            or experiment.get('repetitions') != 12):
        raise BundleError('unsupported pilot version or status')
    assignments = experiment.get('assignments')
    if not isinstance(assignments, list) or len(assignments) != 12:
        raise BundleError('pilot requires 12 assignments')
    counts = Counter()
    for slot, assignment in enumerate(assignments, 1):
        if not isinstance(assignment, dict) or set(assignment) != {'slot', 'mode', 'layout'}:
            raise BundleError('invalid assignment fields')
        if type(assignment['slot']) is not int or assignment['slot'] != slot:
            raise BundleError('assignment slots must be ordered integers 1..12')
        mode, layout = assignment['mode'], assignment['layout']
        if mode not in ('light', 'light_chemical') or layout not in ('a', 'b'):
            raise BundleError('invalid assignment condition')
        counts[(mode, layout)] += 1
    if len(counts) != 4 or set(counts.values()) != {3}:
        raise BundleError('assignments must have three slots per condition')
    for field in ('question', 'hypothesis', 'falsifier', 'analysis_plan', 'success_criterion'):
        if not isinstance(experiment.get(field), str) or not experiment[field].strip():
            raise BundleError('missing pilot question, criterion or analysis plan')
    return experiment


def _validate_links(payload: dict[str, bytes]) -> None:
    for name, data in payload.items():
        if not name.endswith('.md'):
            continue
        for target in re.findall(r'\[[^\]]+\]\(([^)]+)\)', data.decode('utf-8')):
            if re.match(r'[a-zA-Z][a-zA-Z0-9+.-]*:', target) or target.startswith('#'):
                continue
            resolved = posixpath.normpath(posixpath.join(posixpath.dirname(name), target.split('#')[0]))
            if resolved not in payload:
                raise BundleError(f'local Markdown link is absent from bundle: {name} -> {target}')


def _index(payload: dict[str, bytes], format_name: str = FORMAT) -> dict:
    if format_name == FORMAT:
        _validate_links(payload)
    experiment = _validate_manifest(payload[MANIFEST])
    python_version = payload['.python-version'].decode('utf-8').strip()
    if not re.fullmatch(r'\d+\.\d+\.\d+', python_version):
        raise BundleError('invalid reference Python version')
    hashes = {name: _sha(data) for name, data in sorted(payload.items())}
    return {
        'format': format_name,
        'pilot_id': experiment['id'],
        'pilot_status': 'not_run',
        'python_reference': python_version,
        'manifest_sha256': hashes[MANIFEST],
        'snapshot_id': _sha(_json(hashes)),
        'payload_sha256': hashes,
        'tests': 'not_run_by_packager',
    }


def _receipt(raw: bytes, index: dict) -> dict:
    return {
        'integrity': 'PASS', 'format': index['format'],
        'archive_sha256': _sha(raw), 'snapshot_id': index['snapshot_id'],
        'manifest_sha256': index['manifest_sha256'],
        'payload_files': len(index['payload_sha256']), 'pilot_status': 'not_run',
        'tests': 'not_run_by_packager',
    }


def freeze(root: Path, output: Path) -> dict:
    root = root.resolve(strict=True)
    payload = {}
    for name in PAYLOAD_FILES:
        path = root / name
        resolved = path.resolve(strict=True)
        if not resolved.is_relative_to(root):
            raise BundleError('source path escapes project root')
        for component in (path, *path.parents):
            if component == root:
                break
            if component.is_symlink() or component.is_junction():
                raise BundleError('linked source files or directories are not allowed')
        if not path.is_file() or path.stat().st_size > MAX_FILE_BYTES:
            raise BundleError('source is not a bounded regular file')
        data = path.read_bytes()
        if len(data) > MAX_FILE_BYTES:
            raise BundleError('source grew beyond file size limit')
        payload[name] = data
    index = _index(payload)
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, 'w', compression=zipfile.ZIP_STORED) as archive:
        for name, data in sorted({**payload, 'bundle.json': _json(index)}.items()):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            archive.writestr(info, data)
    raw = stream.getvalue()
    if len(raw) > MAX_ARCHIVE_BYTES:
        raise BundleError('archive exceeds size limit')
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open('xb') as destination:
        destination.write(raw)
    return _receipt(raw, index)


def verify(path: Path, expected_sha256: str | None = None) -> dict:
    if expected_sha256 is not None and not re.fullmatch(r'[0-9a-fA-F]{64}', expected_sha256):
        raise BundleError('expected SHA-256 must contain 64 hexadecimal characters')
    with path.open('rb') as stream:
        raw = stream.read(MAX_ARCHIVE_BYTES + 1)
    if len(raw) > MAX_ARCHIVE_BYTES:
        raise BundleError('archive exceeds size limit')
    if expected_sha256 is not None and _sha(raw) != expected_sha256.lower():
        raise BundleError('archive does not match the external SHA-256')
    try:
        with zipfile.ZipFile(io.BytesIO(raw)) as archive:
            entries = archive.infolist()
            names = [entry.filename for entry in entries]
            if len(names) > len(PAYLOAD_FILES) + 1 or len(names) != len(set(names)) or 'bundle.json' not in names:
                raise BundleError('duplicate, missing or unexpected archive path')
            if any(entry.file_size > MAX_FILE_BYTES or entry.flag_bits & 1 for entry in entries):
                raise BundleError('oversized or encrypted member')
            if any(stat.S_IFMT(entry.external_attr >> 16) not in (0, stat.S_IFREG) for entry in entries):
                raise BundleError('archive members must be regular files')
            if any(entry.compress_type not in (zipfile.ZIP_STORED, zipfile.ZIP_DEFLATED) for entry in entries):
                raise BundleError('unsupported compression')
            if sum(entry.file_size for entry in entries) > MAX_ARCHIVE_BYTES:
                raise BundleError('total uncompressed size exceeds limit')
            index = _parse(archive.read('bundle.json'))
            format_name = index.get('format')
            if not isinstance(format_name, str) or format_name not in PROFILES:
                raise BundleError('unknown bundle format')
            expected = set(PROFILES[format_name]) | {'bundle.json'}
            if set(names) != expected:
                raise BundleError('missing or unexpected archive path')
            payload = {name: archive.read(name) for name in PROFILES[format_name]}
    except (zipfile.BadZipFile, RuntimeError, EOFError, zlib.error) as exc:
        raise BundleError('invalid or unreadable ZIP archive') from exc
    computed = _index(payload, format_name)
    if index != computed:
        raise BundleError('payload hashes or metadata do not match')
    return _receipt(raw, computed)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    create = commands.add_parser('freeze', help='save an explicit local snapshot without overwriting')
    create.add_argument('--output', type=Path, required=True)
    inspect = commands.add_parser('verify', help='inspect integrity without extraction or execution')
    inspect.add_argument('archive', type=Path)
    inspect.add_argument('--sha256', help='previously recorded external ZIP hash')
    args = parser.parse_args(argv)
    try:
        if args.command == 'freeze':
            result = freeze(Path(__file__).resolve().parent.parent, args.output)
        else:
            result = verify(args.archive, args.sha256)
    except (OSError, BundleError, UnicodeError) as exc:
        print(f'Bundle rejected: {exc}', file=sys.stderr)
        return 2
    print(_json(result).decode('utf-8'), end='')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
