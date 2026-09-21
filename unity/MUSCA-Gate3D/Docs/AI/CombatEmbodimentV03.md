# MUSCA Combat Embodiment v0.3 — First Threshold

Status: **engineering candidate / human gameplay-feel retest pending**

This pass starts from the human review of combat-controls v0.2.

The mechanical foundation worked, but the review found a presentation gap:
- movement worked while looking like sliding;
- Sentinel telegraphs were understandable without body animation;
- lock-on helped but shared `R` with reset;
- Kael prediction was promising but still crude;
- the closed sandbox and prone, undersized boss proxy did not support the encounter.

The objective of v0.3 is therefore **embodiment before complexity**.

No new combat framework, input package, navigation package, animation package, or global manager is introduced.

## Control contract

- **WASD + mouse** — movement / view.
- **SPACE** — jump.
- **LEFT SHIFT** — committed dodge.
- **LMB** — melee strike.
- **Q** or **MMB** — lock-on.
- **K** — toggle Kael history-only prediction probe.
- **ESC** — release / capture cursor.
- **F5** — reset the legacy Gate run when that runtime is active.
The previous `R` collision is removed:
lock-on no longer owns `R`, and Gate reset now uses `F5`.

## Jump and vertical integration

Jump height is authored at 1.15 m with gravity -24 m/s².

The first runtime QA exposed a frame-step defect:
a long frame could consume most of the jump impulse before displacement was applied,
producing only about 0.39 m of observed height.

The controller now integrates vertical displacement analytically for the frame:

```text
dy = v * dt + 0.5 * g * dt²
v' = v + g * dt
```

This preserves the intended trajectory much better under long frames while keeping
the existing `CharacterController` as the sole movement authority.

Fresh runtime observation: **1.1498 m**, then successful landing.

## Player embodiment

The existing procedural `ResearcherPresentation` is extended rather than replaced.

It now uses smoothed local velocity and explicit body parts for:
- arm swing;
- thigh / shin gait;
- lateral strafe lean;
- torso counter-lean;
- jump tuck / airborne posture;
- dodge lean and body drop.

This is still procedural placeholder animation, not final authored animation or mocap.

## Sentinel / Kael proxy embodiment

The old Sentinel FBX imports with an axis-correction rotation near X=270°.

Previously the AI rotated that same FBX root for yaw, erasing the import correction
and allowing the model to appear prone.

v0.3 splits ownership:

```text
Sentinel_v01            <- gameplay / AI / collider / health / yaw
└── SentinelVisual      <- imported FBX correction / scale / visual pose
```

The gameplay root can now face the player without mutating the FBX import correction.

The visual proxy is enlarged to an authored collider height of 2.06 m and receives:
- procedural approach gait;
- telegraph wind-up pose;
- recovery pose;
- a long proxy spear;
- a three-ring prediction crown.

Scene validation measures the visual as upright:
approximately 0.99 × 2.06 × 0.48 m.
This is **Kael encounter proxy language**, not production Kael character art.

## First Threshold greybox

The closed GateLab room is hidden in the combat scene.

A new `FirstThresholdArena_v03` provides an open encounter space with:
- a 20 × 40 m collision floor;
- shallow reflective layer;
- central causeway;
- asymmetric ruined columns;
- a large threshold frame;
- concentric threshold rings;
- cyan / amber spatial lighting;
- authored player and boss spawn markers.

The legacy Gate runtime and Gate HUD are disabled in the combat scene,
so puzzle state cannot interfere with the encounter.

This is a composition / scale greybox.

It does not claim final environment art, traversal layout, water rendering,
lighting quality, or boss-arena balance.

## Prediction readability v0.3

The prediction algorithm remains deliberately simple and history-only:
- 7-dodge window;
- 4 minimum samples;
- 60% lock threshold;
- dominant completed dodge direction;
- no current input-axis or current dodge-button read.

v0.3 improves **readability before intelligence**:
- HUD exposes sample count, predicted direction, confidence and LOCK state;
- the prediction crown rotates slowly while inactive / learning;
- rotation accelerates with confidence;
- crown color changes when prediction locks;
- the strike point remains committed when the telegraph begins.

This preserves the important falsifier:
the player should be able to teach a habit, then break it and make the prediction wrong.

A more sophisticated predictor is intentionally deferred until this readable version
is retested by a human.

## Automated evidence — 2026-09-21

- Scene validator: **PASS**.
- Missing scripts: **0**.
- First Threshold arena present: **PASS**.
- Authoritative arena floor collider: **PASS**.
- Legacy Gate environments hidden: **PASS**.
- Gate runtime disabled in combat scene: **PASS**.
- Serialized bindings: **PASS** — Space jump / LeftShift dodge / Q + MMB lock-on.
- Boss visual upright: **PASS**.
- Boss/player authored overlap: **false**.
- Unity EditMode: **25/25 PASS**.
- Windows Development build: **PASS**, 0 errors, 0 warnings.
- Repository Python suite: **128/128 PASS**.
- Browser prototype Node suite: **8/8 PASS**.
- Browser room validation: **PASS**.

Runtime QA:
- grounding: **PASS**;
- jump: **PASS**, observed height 1.1498 m and landed;
- dodge/stamina: **PASS**, about 2.38 m and 100 → 72 stamina on spend;
- lock-on: **PASS**, target = `Sentinel_v01`;
- telegraph: **PASS**;
- enemy AI: **PASS**, one telegraphed hit, player 100 → 74 HP;
- melee regression: **PASS**;
- Kael prediction: **PASS**, five Right history samples → Right / 100% / LOCK.

Machine-readable evidence:
- `Docs/AI/CombatSandboxValidation.json`
- `Docs/AI/CombatSandboxBuild.json`
- `Docs/AI/CombatEmbodimentV03RuntimeQA.json`
- `Docs/AI/RuntimeQA/*-v03.{json,png}`

## Human retest gate

The next playtest should answer:
1. Does the new procedural body motion reduce the skating impression?
2. Does Shift dodge feel natural while Space behaves as jump?
3. Does Q/MMB lock-on remain useful without the old R conflict?
4. Is Sentinel's body wind-up readable before the floor marker is consciously parsed?
5. Is the proxy now upright, larger and boss-like rather than prone or toy-scale?
6. Does the First Threshold arena support the intended encounter better than the closed lab?
7. Does the crown / HUD make Kael's prediction state understandable?
8. Can the player teach a dodge habit, break it, and recognize that Kael committed to the wrong future?
### Prediction falsifier

If the player still cannot explain which completed dodge history caused the predicted
strike, or the mechanic feels like current-input reading, the predictor **fails**
regardless of automated PASS.

### Embodiment falsifier

If movement still reads primarily as sliding after the procedural pose pass,
the next step is real rigged animation / authored clips rather than further tuning
numeric speed values.

## Claim boundary

v0.3 proves a stronger executable combat greybox with corrected control mapping,
a real jump path, procedural presentation, an upright enlarged boss proxy,
a purpose-built encounter arena, and more legible prediction feedback.

It does **not** approve final combat feel, final animation, final Kael art,
final arena art, production AI, pathfinding, hit reactions, camera polish,
boss balance, or the final prediction algorithm.
