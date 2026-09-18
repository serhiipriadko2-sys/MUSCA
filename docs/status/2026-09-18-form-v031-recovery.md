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

[FACT] Fresh clean-worktree Unity verification now passes on Unity 6000.6.1f1.
A clean Library/Temp import resolved packages through the ordinary UPM path, compiled
the MUSCA runtime/editor/test assemblies, and completed **7/7 EditMode tests PASS**
with Unity exit code 0.

[FACT] The earlier UPM IPC block was reproduced to the UPM executable itself:
`getLocalConfigFolder()` received an undefined path because the Remote Desktop
Commander process environment omitted `ProgramData`. Setting
`ProgramData=C:\ProgramData` only for the verification process made the same UPM
binary start its IPC server normally. No system environment or project package config
was changed.

[FACT] Raw local verification evidence is preserved outside the Git worktree under
`E:\MUSCA_RESEARCH\evidence\2026-09-18-pr7-unity-fresh`. The test XML reports
total=7, passed=7, failed=0, skipped=0.

## Repository hygiene decisions

[FACT] Twenty Unity PNG files under `Assets/MUSCA/Art/FormV03` were QA screenshots,
not runtime assets: their meta GUIDs had zero references from scenes/materials/prefabs/
scripts. They were intentionally not added to this branch and remain in the safety snapshot.

[FACT] `com.coplaydev.unity-mcp` and ADR-0010 were not authored by this game lane.
After synchronizing PR #7 with current `main`, the accepted tooling dependency is inherited
from `main` but remains outside the PR #7 gameplay/Form diff.

[FACT] Local Codex Runtime files were intentionally excluded; the dedicated Codex
worktree contains a newer candidate state.

## QA

- Python: **128 tests PASS** after prototype-approval lifecycle coverage.
- Blender static integrity: **PASS**, 7 required v0.1 artifacts checked; v0.3/v0.31
  candidate receipt hashes independently matched.
- Browser Function oracle: **8/8 PASS**.
- Three.js server check: **PASS**.
- Fresh Unity 6000.6.1f1 clean-worktree compile: **PASS**.
- Fresh Unity EditMode: **7/7 PASS**, 0 failed, 0 skipped; Unity exit code 0.
- Fresh UPM resolve: **PASS**; IPC server started and resolve-packages returned 200.
- Room lifecycle validator: **PASS**; Function approved; Form v0.31 prototype human approval recorded on 2026-09-18.
- npm audit: **0 vulnerabilities**.
- `git diff origin/main..HEAD --check`: **PASS**.
- staged high-confidence secret patterns: **0 hit files**.
- Post-main-sync fresh Unity test XML SHA-256:
  `92ee06865368841e8d6098332667c8e46857061257440fd314b666ffdb1fcd54`.

## Lifecycle boundary

Function approval remains recorded as approved.

[FACT] On 2026-09-18 the Owner explicitly approved **FORM v0.31 as a prototype**.
The authoritative Form lifecycle is now `approved` with typed scope
`GateLab Form v0.31 prototype only`.

This approval does **not** approve final production art, final character design,
runtime/gameplay feel, final camera/combat behavior, release readiness, scientific
reproduction, or deployment. Runtime human approval remains null.

## ΔDΩΛ

∆ — mixed local Form/Unity work was separated into a clean game lineage based on current main.
D — external byte snapshot → selective recovery → receipt/hash checks → Python/Node/static QA →
fresh Unity import/compile → UPM resolve PASS → EditMode 7/7 PASS → explicit prototype approval →
current-main sync → fresh Unity 7/7 PASS again.
Ω — high for recovered bytes and current clean-worktree engineering verification.
Λ — revise after a later production-art/runtime approval decision or new gameplay/runtime evidence.
