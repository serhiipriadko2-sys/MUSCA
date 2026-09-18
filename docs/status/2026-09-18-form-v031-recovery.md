# Form v0.3 / v0.31 + Unity integration — recovery receipt

Date: 2026-09-18
Branch: `feature/form-v031-unity-gameplay`
Base: `main@ab9a8c14b19087f27c2da5b60f984dc359cd1a9d`
Blender commit: `64654cf82d5730944c2f8bec9014b29e6670bf28`
Unity candidate commit: `3cd578eb2f389dc85e592f630d1002d2f701ee67`

## Purpose

This branch recovers the mixed local working tree into one reviewable **game/Form lane**.
It does not import the concurrent Local Codex Runtime candidate or the GUI-MCP package
dependency. Those are separate tooling/governance surfaces.

[FACT] The source working tree contained 158 visible Git changes: 10 tracked modifications
and 148 untracked files. Before cleanup, those bytes were copied to an external local
snapshot with a SHA-256 manifest.

[FACT] This branch was rebuilt from current `main`, rather than rebasing the dirty
legacy worktree in place.

## Blender candidate

[FACT] `GateLab_Form_v0.3.blend` and its three v0.3 exports match
`form-v03-readback.json` byte counts and SHA-256 values.

[FACT] `GateLab_Form_v0.31.blend`, `Researcher_FormProxy_v0.31.fbx` and
`MUSCA_FormProxy_v0.31.fbx` match `character-art-v031.json`.

[FACT] The static Form integrity gate passes on the clean branch.

[FACT] The integrity gate was corrected for a portability defect: the original Function
layout receipt hashed Windows CRLF worktree bytes, while a clean checkout can contain
the same Git content with LF. Validation now accepts only raw/LF/CRLF variants of the
same text; semantic/content changes still fail. A regression test covers this case.

## Unity candidate

[FACT] The branch contains the v0.3/v0.31 FBX assets, Unity materials, preview/playable
scenes, Form gate visual bridge, third-person presentation/controller changes and
MUSCA behavior driver.

[FACT] Static scene reference audit found only Unity built-in GUIDs outside the project
meta set; no project asset GUID reference was missing.

[FACT] The copied C# sources are byte-identical to the source working tree. In that
source Unity project, the latest runtime/editor assemblies were built after the latest
corresponding source files and the project Editor log contained no recent C# compiler
errors.

[WARN] A new batchmode EditMode run from the clean worktree did **not** complete because
Unity Package Manager failed to open its local IPC stream after 30 seconds. No new
test-result XML was produced. This is recorded as an environment/toolchain block, not
as a fresh Unity test PASS.

[FACT] Earlier local receipts recorded Unity compile errors = 0 and 7/7 EditMode tests
for the v0.3 gameplay/third-person candidate. They remain historical evidence and are
not silently upgraded to a fresh clean-branch engine run.

## Repository hygiene decisions

[FACT] Twenty Unity PNG files under `Assets/MUSCA/Art/FormV03` were QA screenshots,
not runtime assets: their meta GUIDs had zero references from scenes/materials/prefabs/
scripts. They were intentionally not added to this branch and remain in the safety snapshot.

[FACT] `com.coplaydev.unity-mcp` manifest/lock changes and ADR-0010 were intentionally
excluded from this game branch; they belong to a separate tooling lane.

[FACT] Local Codex Runtime files were intentionally excluded; the dedicated Codex
worktree contains a newer candidate state.

## QA

- Python: **126 tests PASS**.
- Blender static integrity: **PASS**, 7 required v0.1 artifacts checked; v0.3/v0.31
  candidate receipt hashes independently matched.
- Browser Function oracle: **8/8 PASS**.
- Three.js server check: **PASS**.
- Room lifecycle validator: **PASS**; Function approved, Form human approval remains null.
- npm audit: **0 vulnerabilities**.
- `git diff origin/main..HEAD --check`: **PASS**.
- staged high-confidence secret patterns: **0 hit files**.

## Lifecycle boundary

Function approval remains recorded as approved.

Form v0.1/v0.3/v0.31 engineering artifacts do **not** create Form human approval:
`human_approval=null` remains authoritative until an explicit typed human review.

This branch does not prove final character art, final camera behavior, final combat feel,
release readiness, scientific reproduction, or deployment.

## ΔDΩΛ

∆ — mixed local Form/Unity work was separated into a clean game lineage based on current main.
D — external byte snapshot → selective recovery → receipt/hash checks → Python/Node/static QA →
two reviewable commits; Unity fresh engine rerun recorded as UPM-blocked.
Ω — high for recovered bytes and non-Unity QA; medium for current Unity engine verification.
Λ — revise after a successful clean-branch Unity EditMode/runtime run or explicit human Form review.
