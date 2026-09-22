# MUSCA Combat Embodiment v0.4 — Pivot Rig / Prediction / Camera

Status: **engineering PASS / human gameplay-feel retest pending**

This pass is a direct response to the owner's v0.3 playtest.

## Human findings that control this pass

1. Jump works.
2. Player body motion still reads as almost absent / puppet-like.
3. Sentinel telegraph is visible but still reads as a puppet.
4. Boss is upright but still too small.
5. First Threshold reads as a prototype, not a finished encounter space.
6. Repeated left dodge teaches Kael to attack left, after which he can ignore the player's current position.
7. Prediction therefore feels like a permanent directional replacement, not a useful prediction layer.
8. Lock-on / strafe camera needs more work.

v0.4 does not dispute these findings because automated v0.3 PASS did not measure human motion quality.

## Root cause — proxy animation

The current Researcher and Sentinel FBX assets are assembled from separate rigid mesh objects.
They do not provide a production humanoid armature / skinned skeleton.

v0.3 rotated individual mesh objects around their own origins.
That changes transforms but does not create convincing shoulder→elbow→hand or hip→knee→foot chains.

v0.4 therefore adds a scene-local **pivot rig adapter** around the existing proxy meshes instead of merely increasing animation angles.
## Player pivot rig

The combat scene builder now creates:

```text
P04_ProxyRig
├── P04_TorsoPivot
│   ├── P04_HeadPivot
│   ├── P04_Shoulder_L
│   │   └── P04_Elbow_L
│   └── P04_Shoulder_R
│       └── P04_Elbow_R
├── P04_Hip_L
│   └── P04_Knee_L
└── P04_Hip_R
    └── P04_Knee_R
```

Existing P03 mesh parts are reparented under these pivots while preserving world transforms.

The gameplay CharacterController remains authoritative.
The presentation layer is visual only.

The procedural pose now drives:
- shoulder swing;
- elbow flex;
- hip swing;
- knee flex;
- torso counter-rotation;
- head stabilization;
- gait bob;
- airborne tuck;
- dodge crouch / lean / brace.

## Dodge v0.4

The dodge still travels the same authored total distance.

Its displacement profile changes from constant-speed sliding to a cubic ease-out:

```text
progress(t) = 1 - (1 - t)^3
```

This produces a fast committed launch followed by deceleration.

The visual pose is explicitly grounded and crouched:
- body drops;
- torso leans into the dodge vector;
- knees brace;
- arms tuck;
- lateral dodge adds roll / yaw rather than a jump-like symmetric airborne pose.

The dodge remains CharacterController-driven and does not use root motion.


## Sentinel pivot rig

Kael now uses a matching scene-local pivot hierarchy for torso, shoulders, elbows,
hips and knees. `SentinelPresentation` drives approach gait, attack wind-up,
recovery and spear presentation from combat state and normalized state progress.

The purpose is not to pretend the rigid proxy is a final animation asset. It is to
make anticipation readable through the body while preserving gameplay authority in
`SentinelCombatBrain`.

## Prediction semantics v0.4

Prediction no longer replaces the ordinary strike point.

At telegraph start Kael now commits two independent zones:

1. **base strike** — the player's observed current position;
2. **forecast strike** — the predicted dodge destination, only when the K-probe is
   locked and its dodge history is still fresh.

Damage succeeds if the player is inside either committed zone. The forecast is
therefore an additional bet by Kael, not permission to ignore the player.

The prediction record also expires after a bounded age (`3.2 s` by default).
A stale learned direction cannot keep redirecting attacks forever.

This is still a deliberately simple frequency predictor, not the final
learning → bait → prediction commitment → deception system.


## Boss scale and encounter space

The v0.4 validator requires Kael to remain upright and verifies a world-space
visual/collider height of approximately `2.66 m`. This is intentionally more
dominant than v0.3 while remaining compatible with the current proxy mesh.

First Threshold receives additional encounter dressing and composition work in the
builder, but it remains a **greybox encounter**, not production environment art.
The human finding “looks like a prototype” is therefore not closed by automation.

## Lock-on camera v0.4

The player body still turns toward the lock target during `Update` so movement
axes remain coherent. Camera follow is now evaluated separately in `LateUpdate`,
after gameplay movement has completed for the frame.

Lock yaw uses `Mathf.SmoothDampAngle` and collision-follow position uses
`Vector3.SmoothDamp` while locked. This targets the previous strafe jerk
without adding a new camera package during the embodiment pass.

Cinemachine remains a possible later migration, not a dependency silently added to
this pass.

## Why v0.4 does not add Animation Rigging yet

The imported player/Sentinel proxies are rigid mesh-part assemblies rather than a
production humanoid/skinned armature. Unity Animation Rigging constraints operate
through a rig graph around an Animator; adding the package now would not create the
missing anatomical deformation by itself.

The production migration gate is therefore: obtain a real skinned skeleton/Avatar,
then evaluate Animator Controller / Blend Trees, authored clips or root motion, and
IK/Animation Rigging constraints for hands, feet and weapon contact.


## Unity engineering basis

Checked against current official Unity documentation during this pass:

- Unity 6 `CharacterController.Move`: collision-constrained delta movement;
  gravity must be supplied by game code.
- Unity `LateUpdate`: runs after all `Update` calls and is the documented
  place for a follow camera tracking an object moved in `Update`.
- Unity 6 `Mathf.SmoothDampAngle`: spring-damper angular smoothing designed
  to approach the target without overshoot.
- Animation Rigging 1.2 `TwoBoneIKConstraint` exists for a proper rigged
  chain; it is a future production-rig tool, not evidence that the current rigid
  proxy has a usable skeleton.

## Fresh automated evidence

The authoritative machine-readable receipt for this pass is:

`Docs/AI/CombatEmbodimentV04RuntimeQA.json`

Fresh v0.4 verification after the camera ordering fix:

- Unity EditMode: `26/26 PASS`;
- scene validator: `PASS`;
- runtime capture views: `8/8 PASS`;
- Python unit tests: `128/128 PASS`;
- Node unit tests: `8/8 PASS`;
- browser room validation: `PASS`;
- Windows build: `PASS`, zero build errors/warnings.

## Human gate

Automation does **not** close these claims:

- natural-looking locomotion;
- dodge reading as a grounded lunge/evade rather than a sideways jump;
- convincing attack weight;
- comfortable lock-on/strafe camera;
- production-quality arena art;
- whether Kael's wrong forecast is perceptible and satisfying when the player breaks
  the learned pattern.

Those remain the controlling criteria for the next human playtest.
