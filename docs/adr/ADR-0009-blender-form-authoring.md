# ADR-0009: Blender 5.2 LTS as the Gate Lab Form authoring surface

## Status

Accepted for `gate-lab-v0.2` Form authoring after explicit human approval of the
Function layout on 2026-09-16. This ADR does not approve the resulting Form candidate,
does not replace Unity as the verified runtime, and does not select a final production
art pipeline for the whole MUSCA game.

## Context

The browser and Unity grayboxes established the Function contract: route, dimensions,
interaction positions, sightlines and gate world-response. The next question is no
longer whether the room works spatially, but whether the same Function can carry the
MUSCA visual identity without becoming a different level.

The authorized Windows host has Blender 5.2.0 LTS installed. An interactive Blender
process may contain unrelated or unsaved user work, so automation must not take over
that process. The supplied visual references establish a target language: dark research
architecture, cyan navigation, AMBER/COBALT contrast, industrial panels, restrained
biophilic pockets, human scale and a persistent MUSCA companion.
## Decision

Use Blender 5.2.0 LTS in a separate `--background --factory-startup` process as the
Form authoring surface for this room. Keep Unity 6000.6.1f1 as the authoritative
interactive runtime and preserve the approved Function evidence as upstream truth.

The Form builder is code-generated and reproducible. It creates named collections for
shell, gate, stations, route, props, MUSCA/player proxies, lighting, cameras, signage
and a non-interactive Sector B preview. It writes a `.blend`, FBX exports, QA renders
and typed receipts.

Hard invariants:

- Gate A-1 remains 4.5 m × 3.5 m and blocks the same approved route.
- AMBER and COBALT stay at x = -4.1/+4.1 m and long-axis = -7.2 m.
- spawn eye height remains 1.68 m for Function sightline validation.
- Form decoration may not obstruct the central route or hide reagent identity.
- MUSCA/player meshes are proxies and cannot be promoted as final character assets.
- no external or paid mesh assets enter this pass.
- no Unity Form integration is claimed until a later explicit gate.
## Alternatives

1. **Model directly in Unity.** Rejected for this milestone: good for runtime assembly,
   weaker for deliberate modular Form authoring and reusable mesh export.
2. **Drive the already-open interactive Blender session.** Rejected because it risks
   overwriting unrelated unsaved user state and makes automation non-reproducible.
3. **Manual unscripted Blender scene.** Rejected because approval geometry could drift
   without a repeatable source of truth or machine-verifiable receipts.
4. **Generate production art externally first.** Rejected before Form approval because
   it would spend asset cost before composition and route readability are locked.

## Consequences / price

The repository now contains binary `.blend`/FBX Form artifacts in addition to the
procedural builder. Current files are small enough for ordinary Git; future large
texture/scan/character payloads require a separate storage/LFS decision rather than
silently growing repository history.

Blender 5.2 reports `Material.use_nodes` as deprecated for Blender 6.0. The current LTS
build is verified, but a future Blender-major upgrade requires an explicit compatibility
pass instead of assuming builder parity.

Sector B geometry is a visual destination preview outside the approved room bounds; it
is not evidence that a second gameplay room has been designed or validated.
## Tests / QA

Before describing the asset as a Form candidate:

- builder and validator Python syntax must compile;
- Blender background build must emit `form-build.json` with `status=PASS`;
- the saved `.blend` must reopen in a second Blender process and pass read-back;
- AMBER/COBALT positions, spawn height and route clearance must match Function anchors;
- environment and MUSCA FBX exports must exist and match receipt hashes;
- spawn, hero, station and open-gate renders must pass visual review;
- open-gate preview must show a physically clear passage and readable Sector B target;
- static CI validation must reproduce bytes/hashes without requiring Blender on GitHub.

## Diff scope

`blender/gate-lab-v0.2/`, this ADR, lifecycle evidence, Blender ignore/binary rules,
and CI/static integrity checks. No GATE-P01 participant records, scientific manifests,
raw data, `main`, or frozen Function coordinates are changed.

## Rollback

Delete/revert the Form feature branch. The Unity Function prototype and browser oracle
remain intact on the parent branch. A rejected Form candidate must be revised from the
same frozen Function evidence rather than silently editing the approved layout.

## ΔDΩΛ

∆ — the approved Function room gains a reproducible Blender Form candidate.
D — Blender 5.2 build/read-back receipts, FBX/hash parity, visual QA and frozen layout anchors.
Ω — high for authoring reproducibility; medium for visual direction; human Form approval is pending.
Λ — reconsider after explicit Form review, Unity import evidence, or a Blender-major/toolchain change.
