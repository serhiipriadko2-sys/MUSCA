# MUSCA Gate3D v0.2 — Unity Function prototype

Status: `READY_FOR_HUMAN_FUNCTION_APPROVAL`, not Form-approved and not a release candidate.

## Purpose

This prototype answers a spatial product question: can a player understand where to go, what can be investigated, what evidence means, and how the world responds while freely moving in a small 3D space?

It does not replace GATE-P01 v0.1. The existing human pilot remains frozen on its terminal build.

## Runtime contract

- Unity `6000.6.1f1`, Built-in Render Pipeline.
- Windows desktop, keyboard + mouse.
- WASD moves; mouse looks; `Q` performs chemical scan; `E` selects the nearby reagent; `TAB` opens the observation log; `R` resets; `ESC` releases the cursor.
- `GateDomain` owns the deterministic puzzle state.
- Player-facing state never includes hidden layout/answer information.
- The gate physically opens after the verified neutral choice and exposes a visible Sector B corridor.

## Scene contract

Room bounds: 16 × 30 × 5.2 m.

Primary circulation is axial from spawn to the gate. AMBER and COBALT are mirrored around the route. The gate opening is 4.5 m wide. The production camera uses FOV 72° and 1.68 m eye height.
## Source layout

- `unity/MUSCA-Gate3D/Assets/MUSCA/Runtime/` — gameplay/domain/presentation runtime.
- `unity/MUSCA-Gate3D/Assets/MUSCA/Editor/GateSceneBuilder.cs` — deterministic scene construction and Windows build.
- `unity/MUSCA-Gate3D/Assets/MUSCA/Editor/GateSceneValidator.cs` — scene parity and serialization checks.
- `unity/MUSCA-Gate3D/Assets/MUSCA/Tests/EditMode/` — deterministic domain tests.
- `prototype3d/` — browser Function reference and CI-friendly room/state validation.
- `prototype3d/room-evidence/gate-lab-v0.2/` — room brief, layout, openings, props and review state.

## Local validation

Current validated milestone:

- Unity compile/import: PASS.
- Scene validator: PASS, missing scripts = 0.
- EditMode: 7/7 PASS.
- Windows Development build: PASS, build errors = 0.
- Standalone runtime QA: spawn, station, gate outcome and HUD-free opened-world captures PASS.
- Browser state tests: 8/8 PASS.
- Browser dependency/room checks: PASS.

The Unity build itself is ignored by Git and is distributed separately as an engineering artifact.

## Function / Form boundary

Machine evidence has moved the room to `ready_for_human_approval`. It has **not** set `human_approval`.

Do not begin final Blender composition, paid/generated prop production, or art-lock on this room until the layout receives explicit human Function approval. After approval, Blender 5.2 LTS can author the structural shell/props while Unity remains the runtime validation surface.