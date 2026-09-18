"""Fail-closed Form receipt integrity gate. Does not execute Blender or approve gameplay."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FORM = ROOT / 'blender/gate-lab-v0.2'
EVIDENCE = ROOT / 'prototype3d/room-evidence/gate-lab-v0.2'
RENDERS = {'form_spawn.png', 'form_hero.png', 'form_amber.png', 'form_gate_open.png'}
ARTIFACTS = {
    'GateLab_Form_v0.1.blend': 'GateLab_Form_v0.1.blend',
    'GateLab_Form_v0.1.fbx': 'exports/GateLab_Form_v0.1.fbx',
    'MUSCA_FormProxy_v0.1.fbx': 'exports/MUSCA_FormProxy_v0.1.fbx',
    **{name: 'renders/' + name for name in RENDERS},
}
BUILD_CHECKS = {'room_id', 'gate_size', 'amber_position', 'cobalt_position', 'spawn', 'route_clear'}
READBACK_CHECKS = {'collections_present', 'objects_present', 'amber_position', 'cobalt_position',
    'gate_closed_width', 'spawn_camera_height', 'route_clear', 'blend_exists',
    'environment_fbx_exists', 'musca_fbx_exists'}
VISUAL_CHECKS = {
    'spawn_to_gate_sightline_readable': True, 'amber_and_cobalt_labels_readable': True,
    'route_markers_readable': True, 'open_gate_is_visually_clear': True,
    'sector_b_destination_visible': True, 'mirrored_or_upside_down_signage': False,
    'form_permanent_geometry_blocks_route': False,
}
READBACK_HASHES = {'blend_sha256': 'GateLab_Form_v0.1.blend',
    'environment_fbx_sha256': 'GateLab_Form_v0.1.fbx', 'musca_fbx_sha256': 'MUSCA_FormProxy_v0.1.fbx'}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def text_sha256_line_ending_variants(path: Path) -> set[str]:
    """Return raw/LF/CRLF SHA-256 variants without tolerating other text changes."""
    raw = path.read_bytes()
    variants = {hashlib.sha256(raw).hexdigest()}
    text = raw.decode('utf-8-sig')
    normalized = text.replace('\r\n', '\n').replace('\r', '\n')
    for newline in ('\n', '\r\n'):
        encoded = normalized.replace('\n', newline).encode('utf-8')
        variants.add(hashlib.sha256(encoded).hexdigest())
    return variants


def validate(form_root: Path, evidence_root: Path, require_visual: bool = True) -> dict:
    form_root, evidence_root = Path(form_root), Path(evidence_root)
    errors = []

    def load(path):
        try:
            document = json.loads(path.read_text(encoding='utf-8'))
            if not isinstance(document, dict):
                raise ValueError('expected a JSON object')
            return document
        except (OSError, UnicodeError, ValueError) as exc:
            errors.append(f'invalid document {path.name}: {exc}')
            return {}

    def mapping(document, key, label):
        value = document.get(key)
        if not isinstance(value, dict):
            errors.append(f'{label}.{key} must be an object')
            return {}
        return value

    build = load(form_root / 'receipts/form-build.json')
    readback = load(form_root / 'receipts/form-readback.json')
    reviews = load(evidence_root / 'milestone-reviews.json')
    for label, document, required in [('build', build, BUILD_CHECKS), ('readback', readback, READBACK_CHECKS)]:
        if document.get('schema') != f'musca.blender-form-{label}.v1':
            errors.append(f'{label} schema mismatch')
        if document.get('status') != 'PASS':
            errors.append(f'{label} receipt not PASS')
        checks = mapping(document, 'checks', label)
        if not required.issubset(checks):
            errors.append(f'{label} required checks missing')
        if any(value is not True for value in checks.values()):
            errors.append(f'{label} checks must all be true')
        if document.get('route_blockers') != []:
            errors.append(f'{label} route blockers must be an empty list')
    if build.get('room_id') != 'gate-lab-v0.2':
        errors.append('build room_id mismatch')
    if build.get('exports') != {'environment_fbx': 'PASS', 'musca_fbx': 'PASS'}:
        errors.append('both exports must be PASS')
    try:
        expected_layout_hash = build.get('source_function_layout_sha256')
        if expected_layout_hash not in text_sha256_line_ending_variants(evidence_root / 'room-layout.json'):
            errors.append('source Function layout hash mismatch')
    except (OSError, UnicodeError) as exc:
        errors.append(f'cannot hash Function layout: {exc}')
    artifacts = mapping(build, 'artifacts', 'build')
    if set(artifacts) != set(ARTIFACTS):
        errors.append('build must enumerate exactly the seven required artifacts')
    hashes = {}
    checked = 0
    for name, relative in ARTIFACTS.items():
        metadata = artifacts.get(name)
        if not isinstance(metadata, dict):
            errors.append(f'missing or invalid artifact metadata: {name}')
            continue
        try:
            path = form_root / relative
            size = path.stat().st_size
            hashes[name] = sha256(path)
            checked += 1
            if type(metadata.get('bytes')) is not int or size <= 0 or metadata['bytes'] != size:
                errors.append(f'byte mismatch: {name}')
            if metadata.get('sha256') != hashes[name]:
                errors.append(f'hash mismatch: {name}')
        except OSError as exc:
            errors.append(f'unreadable artifact {name}: {exc}')
    for field, name in READBACK_HASHES.items():
        if name not in hashes or readback.get(field) != hashes[name]:
            errors.append(f'readback hash mismatch: {name}')
    if require_visual:
        visual = load(form_root / 'receipts/form-visual-qc.json')
        if visual.get('schema') != 'musca.blender-form-visual-qc.v1':
            errors.append('visual schema mismatch')
        if visual.get('room_id') != 'gate-lab-v0.2':
            errors.append('visual room_id mismatch')
        if visual.get('status') != 'PASS_AS_FORM_CANDIDATE':
            errors.append('visual receipt not candidate PASS')
        renders = mapping(visual, 'reviewed_renders', 'visual')
        if set(renders) != RENDERS:
            errors.append('visual must enumerate exactly the four required renders')
        for name in RENDERS:
            if name not in hashes or renders.get(name) != hashes[name]:
                errors.append(f'visual QC hash mismatch: {name}')
        checks = mapping(visual, 'checks', 'visual')
        for name, expected in VISUAL_CHECKS.items():
            if checks.get(name) is not expected:
                errors.append(f'visual check mismatch: {name}')
        if any(value is not True for name, value in checks.items() if name not in VISUAL_CHECKS):
            errors.append('additional visual checks must be true')
    review_map = mapping(reviews, 'reviews', 'milestone')
    function = mapping(review_map, 'function', 'reviews')
    form = mapping(review_map, 'form', 'reviews')
    approval = function.get('human_approval')
    if function.get('status') != 'approved':
        errors.append('Function gate not approved')
    if not isinstance(approval, dict) or approval.get('status') != 'approved':
        errors.append('Function human approval receipt missing or invalid')
    form_status = form.get('status')
    form_approval = form.get('human_approval')
    if form_status not in {'ready_for_human_approval', 'approved'}:
        errors.append('Form lifecycle must be ready_for_human_approval or approved')
    if form_status == 'ready_for_human_approval':
        if 'human_approval' not in form or form_approval is not None:
            errors.append('Pending Form must explicitly keep human approval null')
    if form_status == 'approved':
        if not isinstance(form_approval, dict) or form_approval.get('status') != 'approved':
            errors.append('Approved Form requires typed human approval')
        elif not all(form_approval.get(key) for key in ('date', 'source', 'scope')):
            errors.append('Approved Form human approval receipt incomplete')
    if any(form_root.rglob('*.blend[0-9]')):
        errors.append('Blender backup files present')
    return {'status': 'FAIL' if errors else 'PASS', 'errors': errors, 'checked_artifacts': checked,
        'visual_validation': 'checked' if require_visual else 'not_run'}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--form-root', type=Path, default=FORM)
    parser.add_argument('--evidence-root', type=Path, default=EVIDENCE)
    parser.add_argument('--skip-visual', action='store_true', help='Technical receipts only; does not approve visuals')
    args = parser.parse_args(argv)
    result = validate(args.form_root, args.evidence_root, require_visual=not args.skip_visual)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result['status'] == 'PASS' else 1


if __name__ == '__main__':
    raise SystemExit(main())
