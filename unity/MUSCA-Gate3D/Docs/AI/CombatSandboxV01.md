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
- Runtime QA: `Docs/AI/RuntimeQA/combat-v01.*` and `combatstrike-v01.*`

## Automated evidence

Pre-fix evidence at commit `5e92a1b982584133a28fc758ca2cb2168b0cd4c7` showed:

- Blender procedural Sentinel validation: PASS.
- Unity scene validation: PASS, but the validator did not assert floor collision.
- EditMode tests: 11/11 PASS, including four combat-foundation tests.
- Windows Development build: PASS, 0 errors, 0 warnings.
- Runtime QA confirmed the melee path, but it disabled player movement and therefore did not exercise gravity/collision.

Human playtesting then falsified the implied floor-collision assumption: the player could fall through the visible floor. The collision proxy, validator and runtime grounding QA have been corrected on the feature branch. Current Unity validation, Windows rebuild and grounding runtime QA are **pending rerun**.

See:

- `Docs/AI/CombatSandboxValidation.json`
- `Docs/AI/CombatSandboxBuild.json`
- `Docs/AI/CombatSandboxRuntimeQA.json`

## Claim boundary

The pre-fix build proved the executable melee foundation but did not prove player grounding. The current collision fix must be rebuilt and revalidated before the updated branch can claim runtime PASS.

It does **not** prove:

- enemy AI quality;
- dodge, parry, stamina or lock-on design;
- attack animation or hit-reaction quality;
- final character/enemy art;
- performance targets;
- human gameplay feel;
- that the result meets the project's souls-like direction.

The next product gate is human runtime/gameplay-feel review before expanding the system.
