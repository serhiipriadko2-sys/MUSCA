# MUSCA Combat Controls v0.2 + Kael Prediction Probe

Status: **engineering candidate / human gameplay-feel review pending**

This slice extends the merged combat sandbox in the exact staged order requested by the owner:

```text
stable movement
→ dodge / stamina
→ enemy telegraph
→ lock-on
→ simple enemy AI
→ Kael history-only prediction prototype
```

The Kael prototype is deliberately downstream of the ordinary combat foundation. It is **disabled by default** and must not be used as evidence that the prediction mechanic feels fair until a human playtest says so.

## Player controls

- **WASD + mouse** — movement / view.
- **SPACE** — dodge.
- **LMB** — melee strike.
- **R** or **MMB** — toggle lock-on.
- **K** — toggle the Kael prediction probe.
- **ESC** — release / capture cursor.

## Stable movement

The existing `CharacterController` remains the sole owner of player displacement.

v0.2 adds planar acceleration/deceleration rather than replacing the controller, and preserves the collision-regression fix from v0.1. Grounding QA still resolves the authoritative `Function_Environment/Floor` collision proxy at Y=0.

## Dodge and stamina

Current prototype values:

- stamina: 100;
- dodge cost: 28;
- regeneration: 28 / second;
- regeneration delay: 0.65 s;
- dodge speed: 8.5 m/s;
- committed dodge duration: 0.28 s;
- invulnerability window: 0.18 s.

A QA run exposed a second movement defect: under a large frame step the original time-window implementation could terminate the dodge before consuming its intended movement, producing only about 0.4 m of displacement.

The controller now consumes the **full remaining committed dodge duration**, capped by the remaining time, so the nominal unobstructed travel is approximately 2.38 m even when one frame is longer than the remaining dodge window.

That is an engineering fix, not a balance approval.

## Enemy telegraph

The Sentinel now has a minimal explicit state machine:

```text
Idle → Approach → Telegraph → Recovery → Approach
```

The telegraph is an orange floor marker. The current prototype waits 0.7 s before a strike, then enters 0.85 s recovery. The strike resolves against the committed marker point rather than silently retargeting the player's current position after the warning begins.

This is intentionally simple. It exists to establish readable danger before adding prediction.

## Lock-on

Lock-on searches live `CombatDamageReceiver` targets only when the player requests a target.

Current prototype limits:

- maximum distance: 14 m;
- maximum angle: 82°;
- centered targets receive more weight than peripheral targets.

While locked, the player root rotates toward the target; movement remains local to the player facing so lateral input becomes combat strafing.

## Simple enemy AI

The Sentinel:

1. acquires the player inside its aggro range;
2. approaches directly;
3. stops at melee distance;
4. exposes the telegraph;
5. resolves one deterministic strike;
6. recovers.

Current strike damage is 26. The AI is a sandbox state machine, not navigation/pathfinding and not a production enemy brain.

## Kael prediction prototype

The prototype lives in `KaelPredictionModel` + `KaelPredictionProbe`.

### Hard boundary

The model receives only **completed dodge-direction history** from `PlayerDodgeController.Dodged`.

It does **not** read the current dodge key, current input axes, or a future input event when choosing a predicted direction.

Current model:

- sliding history window: 7 dodges;
- minimum evidence before lock: 4 dodges;
- lock confidence threshold: 60%;
- predicted direction: dominant historical dodge direction;
- ties are resolved by the most recently observed direction among the tied counts;
- prediction probe is OFF at scene start;
- **K** toggles it and clears the old history.

When prediction locks, the Sentinel telegraph moves from the player's current point to a projected post-dodge point in the historically dominant direction.

This is the smallest falsifiable form of the Kael idea: **habit prediction, not input reading**.

## Automated evidence — 2026-09-21

- Scene validation: **PASS**; zero missing scripts.
- Collision floor enabled: **PASS**.
- Sentinel/player authored overlap: **false**.
- EditMode: **23/23 PASS**.
- Windows Development build: **PASS**, 0 errors, 0 warnings.
- Grounding: **PASS**, settled Y ≈ 0.08, floor = `Function_Environment/Floor`.
- Dodge/stamina: **PASS**, 2.38 m committed displacement, stamina 100 → 72 on spend.
- Lock-on: **PASS**, target = `Sentinel_v01`.
- Telegraph: **PASS**, visible in `Telegraph` state.
- Simple AI: **PASS**, one telegraphed strike, player HP 100 → 74.
- Existing melee regression: **PASS**, one strike, Sentinel HP 100 → 66.
- Kael history-only probe: **PASS**, five recorded right dodges → direction Right, confidence 100%, prediction lock true, projected strike point applied.

Machine-readable receipt:

- `Docs/AI/CombatControlsV02RuntimeQA.json`
- screenshots / per-view receipts: `Docs/AI/RuntimeQA/*-v02.{png,json}`

## Human review gate

Automated tests cannot establish combat feel.

The next human review should answer four questions:

1. Does ordinary movement and dodge feel controllable and consistent?
2. Is the Sentinel telegraph early and clear enough to react to without guessing?
3. Does lock-on help rather than fight the camera/movement?
4. After repeating one dodge direction and enabling **K**, does Kael's predicted strike feel like a readable consequence of the player's habit, or does it feel like the game cheated?

### Falsifier

If the player cannot explain **which past habit** caused the predicted strike, or consistently reports that Kael appears to read the current button press, the prediction mechanic **fails this version** even if every automated test remains green.

## Claim boundary

This slice validates implementation and deterministic runtime behavior only.

It does **not** approve final balance, animation, hit reaction, camera polish, production enemy art, final boss behavior, or the Kael concept as fun/fair gameplay.
