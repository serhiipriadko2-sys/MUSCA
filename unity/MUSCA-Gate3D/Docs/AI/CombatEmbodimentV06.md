# MUSCA // First Threshold Combat Embodiment v0.6

Date: 2026-09-21
Branch: `feature/combat-controls-ai-v02`
Base: v0.5 / commit `d54721be3e7e0b623baeba56e62d242dde4aa8ee`

## Human input controlling this pass

The owner asked to continue all v0.5 improvement points and explicitly approved moving directly to Cinemachine. The previous human gate remains authoritative:

- movement improved but still reads too much like a puppet;
- dodge improved but still needs stronger grounded weight;
- Kael wind-up is readable but needs continued physical improvement;
- Kael scale around 30% larger than v0.4 should be retained;
- First Threshold space improved but still reads as prototype;
- lock-on still visibly jerked under sharp strafe.

v0.6 therefore targets architecture and presentation rather than another coefficient-only camera tweak.

## Camera migration

The combat scene now uses Cinemachine 6.6.0 as resolved by Unity 6000.6.1f1 on this host.

Architecture:

- the real Unity output Camera is detached from the player body;
- a CinemachineBrain drives the output Camera;
- `CM_Free` handles free third-person framing;
- `CM_Lock` handles lock-on framing;
- independent free/lock pivots are not children of the player's body yaw;
- player movement becomes camera-relative while the external driver is active;
- player body facing is solved independently from the output camera transform;
- CinemachineBrain uses SmartUpdate and LateUpdate blending.

This removes the previous feedback loop where body yaw and camera yaw could recursively influence movement axes while strafing.

### Lock acquisition correction

The first runtime migration test correctly failed lock-on. The cause was not Cinemachine switching itself: old target acquisition measured range from the camera position. A detached third-person camera moves several metres behind the player, so camera placement was incorrectly affecting whether a target was considered in range.

v0.6 now:

- measures lock range from the player;
- measures view angle from the camera;
- lets Cinemachine settle before the QA acquisition step.

Fresh runtime evidence confirms:

- `lockOnAcquired = true`;
- target = `Sentinel_v01`;
- CinemachineBrain present;
- `cinemachineLockActive = true`;
- active virtual camera = `CM_Lock`.

## Locomotion and dodge

The procedural proxy remains temporary.

v0.6 improves foot presentation by counter-rotating ankle pivots against hip/knee swing. The goal is to keep the foot closer to level during a stride instead of letting it behave as a rigid pendulum.

The dodge distance remains about 2.38 m, but the displacement curve changes from cubic ease-out to quadratic ease-out. At 25% of the dodge, normalized distance is now 0.4375 instead of the previous very front-loaded launch. This preserves commitment while reducing the near-teleport first frames.

This still does not claim humanoid-quality locomotion or final dodge animation.

## Kael

Kael remains approximately 3.46 m tall.

Fresh validated dimensions:

- visual height: ~3.458 m;
- capsule height: ~3.46 m;
- capsule width/depth: ~1.46 m;
- upright: true;
- spawn overlap with player: false.

The v0.5 body wind-up and recovery remain. Approach foot articulation gets the same ankle counter-rotation principle as the player proxy.

## Prediction deception feedback

v0.6 makes a failed forecast visible as an event.

When a FORECAST strike was actually committed but misses the player:

- `LastPredictionBroken` becomes true;
- `BrokenPredictionCount` increments;
- a short `PredictionBreakPulse` is emitted;
- the Crown changes to a bright break color and reverses/spikes its rotation briefly;
- the HUD exposes `FUTURE BROKEN #N`.

The predictive model itself is not made more omniscient. BASE and FORECAST remain separate.

Human readability of this feedback is still pending.

## First Threshold composition

The primary combat lane remains clean, but the encounter silhouette gains another greybox layer:

- side terraces on both sides;
- tilted threshold shards;
- rear wall masses behind the encounter;
- existing monolith/frame composition retained.

These objects are kept outside the main player/boss combat lane. This is still greybox work, not production environment art.

## Animation research boundary

The procedural presentation is now explicitly treated as a bridge, not the target solution. A separate asset evaluation identifies Unity-6-compatible humanoid animation candidates and the official Animation Rigging package. Licensed third-party assets are not silently copied into the repository.

See `Docs/AI/AnimationAssetEvaluationV01.md`.

## Fresh automated evidence

Unity: **6000.6.1f1**

- Runtime C# build: PASS, 0 errors / 0 warnings;
- Editor C# build: PASS, 0 errors / 0 warnings;
- Tests C# build: PASS, 0 errors / 0 warnings;
- scene validator: PASS;
- Cinemachine version resolved in scene: **6.6.0**;
- Cinemachine Brain/free/lock pipelines: PASS;
- output camera detached from player: PASS;
- Unity EditMode: **30/30 PASS**;
- runtime QA: **8/8 PASS**;
- lock runtime active camera: **CM_Lock**;
- Python: **128/128 PASS**;
- Node: **8/8 PASS**;
- browser server check: PASS;
- browser room validation: PASS;
- Windows player build: PASS, 0 errors / 0 warnings.

Windows executable:

- bytes: **667,136**;
- SHA-256: `61695ba6e3d61056db5f10a955b41f680e78f85626ffc909d4e7a93ecb20df9e`.

Fresh runtime DLL:

- bytes: **84,480**;
- SHA-256: `b3ab9255516976cb7a58946c1063e2a5aa8aa5f7054b51eb3d69edd0759b6feb`.

Machine-readable receipt:

`Docs/AI/CombatEmbodimentV06RuntimeQA.json`

## Human gate

The next playtest controls the claims automation cannot settle:

1. Does Cinemachine remove the visible Q/MMB + A-D-A-D lock-on jerk?
2. Does the free camera feel stable and intentional rather than detached/sluggish?
3. Does the quadratic dodge start read as grounded instead of a sideways jump?
4. Do ankle/foot changes make either character feel less puppet-like?
5. Is Kael's 3.46 m scale still correct in the new camera framing?
6. Is `FUTURE BROKEN` visible enough to feel like the player fooled Kael?
7. Does the additional threshold silhouette make the encounter less laboratory-like?

## Claim boundary

The current state is:

**AUTOMATED v0.6 PASS / HUMAN GAMEPLAY-FEEL GATE OPEN**

Automated evidence proves the new Cinemachine path exists and activates at runtime, the camera is structurally detached from the player body, combat and prediction still execute, build integrity remains green, and the scene contains the new greybox layer.

It does not prove perceptual camera smoothness, natural humanoid animation, satisfying dodge weight, readable deception feedback, or production-ready environment art.
