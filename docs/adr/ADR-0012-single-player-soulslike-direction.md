# ADR-0012: Single-player souls-like direction

Status: **accepted**
Date: 2026-09-18
Owner: Семён
Scope: game product direction; no science claim and no runtime activation claim
Supersedes: long-term MMO-roadmap portions of prior game-direction documents; historical receipts remain unchanged
Builder/package mirror: not-needed
Live verification: pending future gameplay implementation; not implied by this documentation decision

## Context

The original long-term game concept included an MMO path. The Owner has now rejected
that path as unnecessarily complex for the current project and selected a solo game.

The new reference genre is a third-person action-RPG / souls-like, with `Elden Ring`
used as an inspiration for combat pressure, exploration feel and encounter cadence.

This decision must not erase the distinctive MUSCA identity. The project already has
explicit architectural boundaries for HUMAN, ISKRA, Bridge and MUSCA, plus a separate
scientific lane. Those boundaries remain load-bearing.

## Decision

MUSCA's active game direction is a **single-player third-person action-RPG / souls-like**.

MMO, co-op, PvP, matchmaking, persistent online world, server economy, live-service
backend and large-scale multiplayer networking are removed from the active roadmap.

`Elden Ring` is a reference, not a scope contract. This ADR does not commit MUSCA to
an Elden-Ring-sized open world, its progression systems, content count or budget.
The existing ISKRA/MUSCA experiential core is preserved:

- HUMAN owns meaningful decisions and may reject ISKRA advice.
- ISKRA interprets observations, frames hypotheses, uncertainty, risks and intentions.
- Bridge remains an explicit typed and testable translation boundary.
- MUSCA remains a distinct perception / sensorimotor participant with its own lower loop.
- ISKRA/LLM does not gain direct authoritative frame-level motor control.
- Contradiction, uncertainty and revision remain visible parts of the experience.
- Game-design truth and neuroscience truth remain separate.
- `MuscaBackend` substitution remains an architectural requirement.

The existing perception/decision loop therefore becomes part of the action game rather
than being replaced by combat.

## Not decided by this ADR

This ADR does not silently adopt the standard mechanics of another souls-like.

The following remain open design decisions:

- stamina and resource model;
- lock-on, parry, dodge and invulnerability timing;
- death penalty and checkpoint rules;
- currency / experience structure;
- equipment and character progression;
- boss count and encounter structure;
- open-world versus compact interconnected world;
- final combat animation, hit-reaction and camera model;
- final production engine choice beyond currently verified prototypes.

## Alternatives considered

1. **Continue toward MMO.**
   Rejected for the active roadmap because networking, persistence, server authority,
   operations and live-service concerns multiply risk before the core game is proven.

2. **Make co-op the next scale step.**
   Not selected. It would reintroduce synchronization and network-design cost before
   single-player combat and ISKRA/MUSCA integration are validated.

3. **Keep MUSCA only as a puzzle/exploration prototype.**
   Not selected as the product direction. Existing puzzle/perception experiments remain
   useful evidence, but the Owner wants a full solo action-RPG experience.

## Consequences / price

Benefits:

- removes multiplayer infrastructure from the critical path;
- keeps development focused on player experience and a bounded vertical slice;
- makes it possible to test ISKRA/MUSCA in combat and exploration without server scale;
- preserves negative scientific results without threatening the game path.

Costs and risks:

- souls-like combat still demands strong camera, animation, collision, enemy telegraphing,
  hit reaction, encounter design and human playtesting;
- eliminating MMO complexity does not make the project small by itself;
- copying genre conventions without proving their fit could dilute MUSCA's identity;
- existing GATE-P01 evidence does not validate the new combat direction.

## Tests / QA

The documentation change is acceptable only if:

- root `AGENTS.md` no longer treats MMO as a planned maturity stage;
- `GAME_VISION.md` names the single-player souls-like direction and preserves the
  perception / hypothesis / human-decision loop;
- `PROJECT_CHARTER.md` keeps HUMAN ↔ ISKRA ↔ Bridge ↔ MUSCA boundaries unchanged;
- `README.md` surfaces the new direction without claiming combat implementation;
- historical receipts and status documents are not rewritten as if they predicted this change;
- GATE-P01 v0.1 remains frozen as its own experiment and is not retrofitted into a
  souls-like playtest;
- no source/runtime behavior is claimed changed by documentation alone.

## Diff scope

Living project documentation and this ADR only. No merge, deployment, multiplayer
code, scientific manifest, experiment result, or gameplay implementation is authorized
by this decision.

## Rollback

A future change of product direction requires a new ADR that explicitly replaces this
one. Do not silently restore MMO/multiplayer language in roadmap documents.

## Lifecycle

accepted = Owner selected the direction in chat on 2026-09-18.
implemented = documentation can reflect the decision; combat implementation remains separate.
merged/deployed/verified-live = not implied by this ADR.

## ΔDΩΛ

∆ — active game direction changed from possible MMO scaling to a solo third-person souls-like.
D — direct Owner decision + updated living docs; historical experiments remain separate.
Ω — high for product direction and preserved ISKRA/MUSCA invariants; combat design remains unverified.
Λ — reconsider only if a tested single-player build shows the genre shell damages the core identity,
or if a future multiplayer value proposition justifies a new ADR and its engineering cost.
