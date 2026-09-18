# ADR-0007: локальный browser-3D runtime для Function graybox

## Status

Accepted for the limited `gate-lab-v0.2` Function prototype after the user asked to embody the approved visual direction as a real navigable 3D space. This ADR does **not** select the final MUSCA game engine and does not modify frozen GATE-P01 v0.1.

## Context

The Tk visual shell proves a clearer presentation, but it remains a dashboard. The next product question is spatial: can a player understand where to go, what can be inspected, what evidence means, and how the gate responds while freely navigating a small 3D room?

The authorized Windows host has Node 24.18.1 and modern Chromium browsers. Unity and Blender were not part of the verified project toolchain. Adding them now would increase setup cost before circulation and interaction are validated.

## Decision

Create `prototype3d/` as a local first-person WebGL graybox using:

- Node.js 24 for a project-local static server and tests;
- `three@0.186.0`, pinned exactly in `package-lock.json`;
- official `PointerLockControls` for mouse-look;
- procedural geometry only at Function gate;
- WASD navigation, collision bounds, contextual `Q` scan and `E` interaction;
- the existing MUSCA evidence semantics: FACT / HYP / UNKNOWN / INTERP.

The room evidence package lives under `prototype3d/room-evidence/gate-lab-v0.2/`.

## Dependency gate

Purpose: rapid interactive 3D Function validation. Three.js r186 was released 2026-09-08 and is actively maintained. The official package is ESM and MIT-licensed; PointerLockControls is documented for first-person 3D games. The project pins `three@0.186.0` and uses no bundler or second runtime dependency.

Platform: verified target is desktop Windows with a current Chromium browser. The prototype is not yet a mobile, gamepad, accessibility, Linux, or macOS claim.

Reproducibility: `package-lock.json`, Node major-range metadata, local static server, `npm test`, `npm run check`, and room-evidence validation are required before a branch may be described as runnable.

Security/cost: the runtime serves only localhost by default, performs no uploads, uses no credentials, and the Function gate has a paid-service budget of zero. `npm audit --omit=dev` is recorded during dependency intake.

Exit cost: low. Game-state and room manifests are project-owned modules; the renderer can later be replaced by Unity, another browser renderer, or a custom runtime without changing GATE-P01 v0.1.

## Alternatives

1. **Unity now.** Better final-game tooling, but adds a large engine/toolchain before layout readability is validated.
2. **Blender interactive prototype.** Strong authoring surface, weaker as the actual player runtime and unavailable on the verified host.
3. **Raw WebGL.** Minimizes dependency count but spends effort rebuilding camera, materials, geometry, and scene abstractions irrelevant to the current product question.
4. **Keep Tk only.** Rejected for this milestone because it cannot test circulation, sight lines, spatial interaction, or first-person navigation.

## Consequences / price

This introduces a second prototype runtime beside the Python contract stand. It must not become a silent replacement for the Python research/game contract, and the two tracks must not be used to prove each other.

The graybox is deliberately low-art: walls, gate, stations, light strips, route markers, and MUSCA companion are procedural. Asset generation and final composition remain blocked until the Function layout receives explicit human approval.

The scene uses first-person desktop controls. Motion sickness, keyboard remapping, localization breadth, color-blind support, gamepad support, and final audio remain future UX work.

## Tests / QA

Before claiming the Function prototype is runnable:

- `npm test` must pass player-state and hidden-answer tests;
- `npm run check` must resolve the pinned Three.js module and PointerLockControls;
- `npm run validate:room` must PASS while keeping Function human approval pending;
- JavaScript syntax checks must pass;
- localhost health and main-module requests must return 200;
- real browser renders must show spawn, decision bay, and gate without hidden layout information;
- collision/route behavior and gate world-response must be exercised in the runtime;
- Python MUSCA tests and frozen pilot packaging must remain unaffected.

## Diff scope

`prototype3d/`, this ADR, documentation, `.gitignore`, and CI checks for Node/Three Function prototype. No participant receipts, scientific manifests, raw connectome data, or frozen pilot payload changes.

## Rollback

Close/delete the feature branch and remove `prototype3d/node_modules`; the prior Tk/Python prototype remains intact. If the layout fails Function review, preserve evidence and revise the room manifest before any Form work.

## ΔDΩΛ

∆ — MUSCA gains a real navigable 3D Function graybox without selecting a final engine.
D — current host capability, pinned Three.js dependency, room manifests, runtime tests, and browser QA.
Ω — high for the limited local graybox runtime; game usability and final art quality remain unvalidated.
Λ — reconsider the runtime after Function review, comparative player evidence, or a requirement that needs an engine feature the browser graybox cannot supply.
