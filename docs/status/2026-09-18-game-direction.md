# MUSCA game direction — 2026-09-18

Status: **ACCEPTED DOCUMENTATION DECISION**
Branch: `docs/solo-soulslike-direction`
Base: `origin/feature/blender-form-gate-lab-v0.2@2b84406be1e6a5f70d423174f9cde522a2b32bfc`

## Decision

[FACT] Owner explicitly changed the game target from the earlier MMO concept to a
single-player game in the souls-like / Elden-Ring-like family.

[FACT] Owner explicitly required that the existing behavior and felt identity of
ISKRA/MUSCA remain.

[INTERP] The smallest faithful project translation is:

- single-player third-person action-RPG / souls-like as the active product direction;
- `Elden Ring` as a feel/reference point, not a scope or content-copy requirement;
- MMO/multiplayer removed from the active roadmap;
- HUMAN ↔ ISKRA ↔ Bridge ↔ MUSCA architecture preserved;
- existing perception/hypothesis/decision loop preserved inside the action game.

## Preserved boundaries

No direct LLM frame-level motor control is introduced.
Bridge remains first-class and typed.
`MuscaBackend` remains substitutable.
Research truth does not become game truth and vice versa.
GATE-P01 v0.1 remains its own frozen perception/decision pilot. It is not renamed or
retroactively interpreted as souls-like combat evidence.

SCI-R00/R01 and connectome-topology claims are unaffected by this game-direction decision.

## What this does not prove

This documentation decision does not prove:

- combat implementation;
- acceptable camera or controls;
- enemy AI quality;
- boss design;
- death/checkpoint/progression rules;
- open-world feasibility;
- final art quality;
- commercial readiness;
- merge, deployment or release.

## Documentation surfaces

Updated living surfaces:

- `AGENTS.md`
- `README.md`
- `docs/PROJECT_CHARTER.md`
- `docs/GAME_VISION.md`
- `docs/adr/ADR-0012-single-player-soulslike-direction.md`

Historical dated audits/receipts are intentionally left unchanged.

## Next gate

The next game-specific evidence should be a bounded single-player combat/exploration
slice with a new playtest protocol. It should test both souls-like game feel and whether
ISKRA/MUSCA remain distinct, useful partners rather than decorative UI.

∆ — roadmap changed; architectural identity preserved.
D — direct Owner decision mirrored into living documentation through ADR-0012.
Ω — high for intended direction; implementation and game feel remain unverified.
Λ — revise after a real combat prototype or a future replacement ADR.
