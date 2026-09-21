# MUSCA // First Threshold Combat Embodiment v0.5

Date: 2026-09-21
Branch: `feature/combat-controls-ai-v02`

## 1. Human input that controls this pass

v0.5 starts from the owner's v0.4 playtest, not from automated success.

Observed human state:

- locomotion is better than v0.3, but still needs substantial embodiment work;
- dodge is better than v0.3, but still needs substantial work;
- Kael's body wind-up is readable, but should become stronger and more physical;
- Kael should be approximately 30% larger;
- dual-zone prediction is more interesting and should be preserved;
- lock-on still visibly jerks during sharp strafing.

Therefore v0.4 is not promoted to a gameplay-feel PASS. The automated green state is only the engineering baseline for v0.5.

## 2. v0.5 design goal

This pass is deliberately narrow:

1. increase visible transfer of body mass instead of adding more decorative limb oscillation;
2. make dodge read as a committed grounded evade;
3. strengthen Kael's physical anticipation and recovery;
4. scale Kael by the requested ~30%;
5. preserve the useful dual-zone prediction semantics;
6. remove one likely source of lock-on jitter: body and camera solving target yaw with different smoothing laws in the same rendered frame.

## 3. Player embodiment

The rigid proxy remains a temporary presentation rig, not a production humanoid skeleton.

v0.5 extends the scene-local pivot hierarchy with:

- pelvis pivot;
- left/right ankle pivots;
- existing shoulders, elbows, hips and knees.

Locomotion phase is now distance-driven. The gait advances from actual planar controller speed and traveled distance instead of a free-running clock. This is intended to reduce the treadmill / puppet effect during acceleration, deceleration and small corrections.

Presentation now adds:

- pelvis counter-rotation;
- ankle articulation;
- stronger hip / knee excursion;
- torso counter-rotation;
- small lateral root sway;
- direction-sensitive forward/back gait.

### Dodge pose

The v0.4 sinusoidal dodge pose is replaced by three presentation phases:

- fast engage;
- held brace;
- controlled release.

The movement authority remains `CharacterController`. The presentation rig now drops the root further, braces pelvis and ankles, and increases torso/shoulder commitment. The authored gameplay displacement remains approximately 2.38 m.

This still does not claim a production-quality roll or authored animation clip.

## 4. Kael embodiment and scale

Kael receives the same extra pelvis/ankle articulation.

Approach gait is tied to actual distance traveled by Kael instead of a free-running animation clock.

Telegraph presentation reaches the committed wind-up earlier and holds it longer. This gives the player more time to read:

- shoulder loading;
- elbow compression;
- pelvis rotation;
- knee/ankle bracing;
- torso coil;
- spear preparation.

Recovery exaggerates the follow-through before returning to approach.

### Scale

The requested multiplier is `1.30x` relative to v0.4.

Validated v0.5 dimensions:

- visual height: approximately **3.458 m**;
- capsule height: approximately **3.46 m**;
- capsule width/depth: approximately **1.46 m**;
- upright: PASS;
- player overlap at authored spawn: false.

Spear and Prediction Crown scale with Kael so accessories do not remain toy-sized.

## 5. Lock-on jitter pass

v0.4 used two independent angular systems during lock-on:

- body: `Quaternion.RotateTowards`;
- camera: `Mathf.SmoothDampAngle`.

Both were chasing the same moving target, and the target yaw could be sampled at different points in the frame.

v0.5 solves lock yaw once in `UpdateView`:

- desired yaw comes from the current target vector;
- a single `Mathf.SmoothDampAngle` state produces the authoritative frame yaw;
- a small 0.12 degree dead zone suppresses micro-corrections;
- player body receives that same yaw;
- `LateUpdate` applies the already-solved yaw to the camera instead of recomputing it.

Serialized scene tuning is explicitly written and validated:

- turn/max yaw: 420 deg/s;
- smooth time: 0.10 s;
- dead zone: 0.12 degrees.

This is an engineering correction for a plausible jitter source. Human sharp-strafe testing remains the decisive falsifier.

## 6. Prediction semantics

The v0.4 dual-zone design is intentionally preserved:

- BASE strike commits to the player's observed current location;
- FORECAST strike commits separately to the learned dodge destination when prediction is locked and fresh.

v0.5 does not make prediction more omniscient. The player can still teach a habit and then break it.

## 7. Fresh automated evidence

Unity version: **6000.6.1f1**

- C# Runtime build: PASS, 0 errors / 0 warnings;
- C# Editor build: PASS, 0 errors / 0 warnings;
- C# Tests build: PASS, 0 errors / 0 warnings;
- scene builder: PASS;
- scene validator: PASS;
- Unity EditMode: **28/28 PASS**;
- runtime QA: **8/8 PASS**;
- Python unit suite: **128/128 PASS**;
- Node browser suite: **8/8 PASS**;
- browser server check: PASS;
- browser room validation: PASS;
- Windows player build: PASS, 0 errors / 0 warnings.

Windows executable:

- bytes: **667,136**;
- SHA-256: `61695ba6e3d61056db5f10a955b41f680e78f85626ffc909d4e7a93ecb20df9e`.

Fresh runtime DLL:

- bytes: **78,848**;
- SHA-256: `0e4dc469b167edc9217fd9e4b1653b78570403cf0df2b15baa0173358675df30`.

Authoritative v0.5 machine receipt:

`Docs/AI/CombatEmbodimentV05RuntimeQA.json`

## 8. Human gate

The next playtest controls these claims:

1. Does normal locomotion show more convincing pelvis/ankle weight transfer?
2. Does dodge read as a grounded evade rather than a sideways jump?
3. Does Kael's approach and telegraph feel less puppet-like?
4. Does ~3.46 m Kael now have the intended boss presence?
5. Does dual-zone prediction remain understandable and interesting?
6. Most importantly: does Q/MMB lock-on still visibly jerk during rapid left/right strafe?

A human FAIL on any of these remains authoritative over automated PASS for gameplay feel.

## 9. Claim boundary

Automated evidence proves the new rig hierarchy exists, the requested boss scale is serialized and validated, the single-yaw lock architecture is present, combat still executes, prediction semantics remain intact, and the Windows build contains the fresh runtime assembly.

It does **not** prove:

- natural animation;
- satisfying dodge feel;
- satisfying attack weight;
- perceptually smooth camera motion;
- final boss scale;
- production-quality First Threshold environment art;
- final Kael learning/bait/deception behavior.

The current status is therefore:

**AUTOMATED v0.5 PASS / HUMAN GAMEPLAY-FEEL GATE OPEN**
