# Gate3D v0.2 — Unity Function verification receipt

Date: 2026-09-16.

## Lifecycle

- Branch: `feature/3d-gate-prototype`.
- Substantive code commit: `bdb8935989c486c46ce2a293051d7be026c93ec2`.
- Stacked base: `feature/visual-interface@65a1e892cbef1dcef816efaed7b0c5a1e28c72c7`.
- Draft PR: #3, open, mergeable, not merged.
- Function state: `READY_FOR_HUMAN_FUNCTION_APPROVAL`.
- Form state: `BLOCKED_PENDING_FUNCTION_APPROVAL`.
- GATE-P01 v0.1 remains frozen and unchanged.

## Local engineering verification

- Python baseline: 113/113 tests PASS.
- Frozen pilot bundle: freeze + verify PASS, 55 payload files.
- Browser/Three.js domain tests: 8/8 PASS.
- `npm audit --omit=dev`: 0 vulnerabilities.
- Room Function validator: PASS, `human_approval=null`.
- Unity version: 6000.6.1f1.
- Unity scene validator: PASS, missing scripts = 0.
- Unity EditMode: 7/7 PASS.
- Windows Development build: PASS, zero build errors.
- Standalone player QA: spawn, station interaction, outcome and physical gate-open path verified.
## Remote verification

GitHub Actions for exact substantive commit `bdb8935...`:

- push run #29: Python job SUCCESS; Browser 3D Function job SUCCESS.
- pull-request run #30: Python job SUCCESS; Browser 3D Function job SUCCESS.

GitHub CI does not execute Unity Editor. Unity compile/tests/build/runtime evidence is local authorized-host evidence; remote CI verifies the Python baseline, browser Function oracle, dependency/room contracts, and Unity source/generated-state boundary.

## Artifact

Local engineering build:

`E:\MUSCA_RESEARCH\game-builds\MUSCA-Gate3D-v0.2-function-dev.zip`

- bytes: 64,223,706
- SHA-256: `146501d277e2060bbbdb3a794ae7395a6aedcca8608f7305a72582d945531cba`

This is a Development engineering artifact, not a release candidate.

## Evidence hashes

- `UnityFunctionValidation.json`: 3,954 bytes; SHA-256 `91e9e41cef62ac6eadf916e3159818cef75938d15f5bb5925753e4b355f92863`.
- `pass5-spawn.png`: 307,955 bytes; SHA-256 `704762ac67d742eeb23c8b7b8ea4fb515ca6e6b6d3aa100ac588ec9bc10cb786`.
- `pass5-amber.png`: 441,884 bytes; SHA-256 `43498f4e1fad08e6957b750e2b506c00dea570e5d2ca5968506a59cd39db1d30`.
- `pass5-openedworld.png`: 491,180 bytes; SHA-256 `f72a9805b13b605f85a52a2c43344a385390ddb444d1f5121d7941ff4cbd4769`.
- `GateLab.unity`: 243,452 bytes; SHA-256 `56add358798b6f10211655a60b8ef87c0fc436c95356c9e8af68802c066d2900`.

## Known boundary

Unity logs recurring `Curl error 35` on external certificate checks during Editor shutdown. It did not block local package-manager startup after the scoped `PROGRAMDATA` environment repair, import, compilation, tests, build, or standalone runtime. Network/package-management reliability therefore remains a separate unresolved warning.

A successful runtime does not grant Function approval. Blender Form composition and art-lock remain blocked until explicit human approval of the reviewed layout.