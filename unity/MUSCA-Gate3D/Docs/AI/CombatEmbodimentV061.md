# MUSCA // First Threshold Camera Hotfix v0.6.1

Date: 2026-09-21
Branch: `feature/combat-controls-ai-v02`
Base: v0.6 / commit `c8d1186cd016a347c6d293a3be34f9238c231f58`

## Human regression report

The owner reported two immediate v0.6 regressions:

1. the free camera rotated continuously without stopping;
2. pressing `W` made the character appear to move backwards.

Both were treated as blocking gameplay regressions. v0.6 is not a valid human gameplay-feel pass.

## Root cause

### Continuous camera rotation

The v0.6 Cinemachine controller updated the free orbit yaw from mouse input, then at the end of the same frame copied the already-computed Cinemachine output-camera yaw back into the free-orbit state.

That created a positive feedback loop:

`free pivot yaw → Cinemachine output yaw → free pivot yaw → ...`

The output camera is a result of Cinemachine composition, so it must not be fed back as the authoritative orbit input every frame.

### Forward input / body orientation

v0.6 also used the Cinemachine output camera transform as a movement/body-facing reference.

The output camera is a composed result, not the control-space authority. This allowed the character body to face a direction inconsistent with the intended movement basis.

## Hotfix

### Free camera authority

The free camera now uses one authoritative state:

- `_freeYaw` / FreeCameraPivot yaw.

The Cinemachine output camera is never copied back into that state every frame.

Yaw is synchronized only once on a real mode transition:

- free → lock: lock yaw starts from free yaw;
- lock → free: free yaw resumes from the last lock yaw.

### Movement authority

When the external Cinemachine driver is active:

- movement basis comes from `MuscaCinemachineController.MovementYaw`;
- `W` at yaw 0 resolves to world +Z;
- unlocked body-facing follows the actual desired movement vector;
- locked body-facing still follows the selected target.

The output Camera transform is therefore presentation, not movement authority.

## New regression coverage

Two EditMode tests were added:

- `ExternalForwardInputUsesPivotYaw`;
- `BodyFacingYawMatchesDesiredTravelDirection`.

A new runtime scenario `cameraidle` was added.

It lets Cinemachine settle, samples free-camera yaw, waits one second with no camera input, and fails if drift exceeds 0.75 degrees.

Fresh result:

- active virtual camera: `CM_Free`;
- camera idle stable: true;
- measured yaw drift: **0.0 degrees**.

## Fresh verification

Unity: **6000.6.1f1**

- Runtime C# build: PASS, 0 errors / 0 warnings;
- Tests C# build: PASS, 0 errors / 0 warnings;
- Scene validator: PASS;
- Cinemachine: 6.6.0;
- Unity EditMode: **32/32 PASS**;
- runtime QA: **9/9 PASS**;
- cameraidle: PASS, yaw drift **0.0°**;
- lock-on: PASS, active camera `CM_Lock`;
- Python: **128/128 PASS**;
- Node: **8/8 PASS**;
- browser check: PASS;
- room validation: PASS;
- Windows build: PASS, 0 errors / 0 warnings.

Fresh Runtime DLL:

- bytes: **86,016**;
- SHA-256: `a70c14683110ba7093c9b9057e9297486e93737ffa468cdb8421c6f0d3844dfc`.

Machine receipt:

`Docs/AI/CombatEmbodimentV061RuntimeQA.json`

## Human retest

The automated gate now directly covers the self-spinning regression, but the human retest remains authoritative for:

1. whether the camera remains stable while actively moving the mouse;
2. whether `W` visually reads as forward movement;
3. rapid `A-D-A-D` under Q/MMB lock-on;
4. lock → unlock camera transition;
5. dodge under lock-on.

Current state:

**AUTOMATED v0.6.1 HOTFIX PASS / HUMAN RETEST OPEN**
