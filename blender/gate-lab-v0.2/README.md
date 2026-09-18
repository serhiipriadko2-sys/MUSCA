# Gate Lab v0.2 — Blender Form candidate

Status: `READY_FOR_HUMAN_FORM_APPROVAL`.

This directory is the reproducible Blender Form authoring surface for the approved
`gate-lab-v0.2` Function layout. Blender may add visual hierarchy and modular detail,
but it must not silently change the approved route, gate opening, station positions,
or interaction semantics.

## Frozen Function anchors

- room containment: x = ±7.15 m, z/long axis = -13.0..13.4 m
- player spawn eye: 1.68 m at long-axis position 12 m
- Gate A-1 opening: 4.5 m wide × 3.5 m high
- AMBER station: x = -4.1 m, long-axis = -7.2 m
- COBALT station: x = +4.1 m, long-axis = -7.2 m
- axial cyan route and critical sightlines remain readable

Blender maps Function X → Blender X, Function Z/long-axis → Blender Y, and height → Blender Z.
## Contents

- `GateLab_Form_v0.1.blend` — authoritative Form candidate source
- `exports/GateLab_Form_v0.1.fbx` — environment export candidate for Unity
- `exports/MUSCA_FormProxy_v0.1.fbx` — non-final companion Form proxy
- `renders/` — spawn, hero, station and open-gate QA renders
- `scripts/build_gate_lab_form.py` — deterministic scene builder/exporter
- `scripts/validate_gate_lab_form.py` — Blender read-back validator
- `receipts/form-build.json` — build hashes, counts and Function-anchor checks
- `receipts/form-readback.json` — reopened `.blend` verification
- `receipts/form-visual-qc.json` — visual candidate review; not human approval

Collections deliberately separate shell, gate, stations, route, props, MUSCA/player
proxies, lighting, cameras, signage and the Sector B preview. Do not collapse them
into one opaque mesh before Unity integration.

## Rebuild

Run from the repository root with Blender 5.2.0 LTS:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python blender\gate-lab-v0.2\scripts\build_gate_lab_form.py
```
Then read back the saved file with a new Blender process:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background blender\gate-lab-v0.2\GateLab_Form_v0.1.blend --python blender\gate-lab-v0.2\scripts\validate_gate_lab_form.py
py -3.14 scripts\validate_blender_form.py
```

Do not treat Blender process exit code as sufficient evidence: Blender 5.2 can exit
with code 0 after a Python exception. The JSON receipts are the authoritative
postconditions.

## Current Form boundary

This pass uses procedural geometry/materials and zero external/paid mesh assets.
The visual references supplied in chat guide language and composition only; they are
not copied into the repository or treated as exact production assets.

The player and MUSCA are scale/Form proxies. Character production, creature rigging,
production textures, animation, audio, VFX, accessibility, and Unity Form integration
remain outside this candidate.

Explicit human Form approval is required before this asset is promoted into the Unity
runtime branch as the new presentation layer.
