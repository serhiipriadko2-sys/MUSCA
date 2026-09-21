# MUSCA Combat Sandbox v0.1

## Purpose

`CombatSandbox_v01` is a deliberately small gameplay-feel probe. It converts the verified Form v0.3 scene into the first executable combat slice without changing the scientific Bridge/controller claims.

The sandbox is not a claim that MUSCA already has souls-like combat. Its purpose is to establish a reviewable baseline that a human can run, criticize, and replace.

## Player loop

- WASD + mouse: move / look with the existing third-person controller.
- Left mouse button: short-range melee strike.
- Sentinel: 100 HP.
- One strike: 34 damage.
- On death the Sentinel is hidden/disabled and respawns at its authored spawn after 1.75 seconds.
- MUSCA companion and the Form v0.3 environment remain present.
- The Gate HUD is disabled for this isolated combat probe.

## Assets

- Scene: `Assets/MUSCA/Scenes/CombatSandbox_v01.unity`
- Sentinel source: `blender/gate-lab-v0.2/Combat_Sentinel_v0.1.blend`
- Sentinel Unity FBX: `Assets/MUSCA/Art/CombatV01/Sentinel_FormProxy_v0.1.fbx`
- Runtime QA: `Docs/AI/RuntimeQA/combat-v01.*`, `combatstrike-v01.*` and `grounding-v01.*`

## Automated evidence

Human playtesting falsified the original floor-collision assumption: the player could fall below the visible floor.

The audit found two collision defects:

- the hidden Function floor collider had been disabled under the Form v0.3 visual layer;
- the decisive runtime fault was the Sentinel collider: the imported FBX root has `lossyScale = 100`, so a locally authored 0.42 m × 1.76 m capsule became an 84 m × 176 m × 84 m world collider and overlapped the player spawn.

The corrected builder preserves the Function collision proxy and converts desired Sentinel world dimensions into local collider dimensions using the imported root scale. The validator now rejects an oversized Sentinel collider or a Sentinel/player spawn overlap.

Current evidence:

- Blender procedural Sentinel validation: PASS.
- Unity scene validation: PASS; zero missing scripts; Function floor collider enabled.
- Sentinel collider world bounds: approximately 0.84 m × 1.76 m × 0.84 m; player-spawn overlap: false.
- EditMode tests: 11/11 PASS.
- Windows Development build: PASS, 0 errors, 0 warnings.
- Runtime gameplay DLL fingerprint: `50effb82a14504b033556f3bd9355ef517cc5c8aecd89aeda096907485629c21`.
- Grounding runtime QA: PASS; player settles at y ≈ 0.08 with `grounded=true`, `collisionSafe=true`, and a physics probe resolves `Function_Environment/Floor` at y = 0.
- Melee runtime QA: PASS; exactly one strike registers and Sentinel health changes 100 → 66.

See:

- `Docs/AI/CombatSandboxValidation.json`
- `Docs/AI/CombatSandboxBuild.json`
- `Docs/AI/CombatSandboxRuntimeQA.json`

## Claim boundary

The rebuilt development executable passes the automated grounding regression and deterministic melee regression. On 2026-09-21 the owner explicitly confirmed that the original fall-through no longer reproduces during human free movement. That confirmation is scoped to the collision regression; it is not overall gameplay-feel approval.

It does **not** prove:

- enemy AI quality;
- dodge, parry, stamina or lock-on design;
- attack animation or hit-reaction quality;
- final character/enemy art;
- performance targets;
- human gameplay feel;
- that the result meets the project's souls-like direction.

The collision-regression human gate is closed. The next product gate is the staged combat-controls slice: stable movement → dodge/stamina → telegraph → lock-on → simple enemy AI, with a separate human gameplay-feel review before any Kael prediction mechanic is treated as validated.
