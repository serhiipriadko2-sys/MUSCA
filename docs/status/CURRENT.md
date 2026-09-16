# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/host фактов новый
connector/read-back имеет приоритет над этим документом.

Последние подтверждённые вехи:

- [SCI-R00 preregistration](../research/SCI_R00_R01_REPRO_PLAN.md);
- [R00 read-only preflight](2026-09-16-r00-preflight.md);
- [operational containment snapshot](2026-09-16-concurrent-ops-drift.md);
- [R00 solver/source provenance](2026-09-16-r00-source-provenance.md).

## Repository / governance

[FACT] Branch `docs/musca-foundations` published HEAD before this pending patch:
`b42c125eeedd4e26b2cc6d5dcdf090e60077d4ec`.
`main` remains `7c56ff5da706894e07bb3b3aa85798304c4ae925`; draft PR #1 is open and unmerged.

[FACT] ADR-0005 lifecycle is `accepted`, bounded to Shiu/FlyWire-v630 as the first
strict reproduction lineage only. Push CI #16 and PR CI #17 for `b42c125...` were
`success` across tests, manifest schema, solver-gate, compile and smoke.

## Verified local R00 inputs

[FACT] Portable micromamba `2.9.0` exists project-locally at
`E:\MUSCA_RESEARCH\tools\micromamba.exe` without persistent PATH/shell-init.
Executable SHA-256:
`a6d804394b2418991c4e29562853eaace2f2ce9d9da661a98e74e02e8dbb44b0`.

[FACT] Exact upstream commit snapshot was retrieved through GitHub codeload after
Git transport stalled. Authoritative archive:

`E:\MUSCA_RESEARCH\downloads\Drosophila_brain_model-91bdd1e7.zip`

- bytes: `190814359`;
- SHA-256: `0dc3778bd3b668d8c48d0a98e2ef33e2468335e6cd374f95a37426ce1fe5c4b0`;
- artifact QC: PASS, 22 entries / 19 files, 19/19 round-trip hashes match;
- extracted snapshot: `E:\MUSCA_RESEARCH\shiu-91bdd1e7\source-snapshot`.

[FACT] Preregistered Git blob IDs match local `model.py`, `environment_full.yml`
and both v630 input files. Dataset manifest is now locally retrievable/runnable
as a dataset receipt; this does not make R00 runnable.

## Preflight / tests

[FACT @ local verification] Pending patch state:

- `101` unit tests PASS;
- R00 preflight tests PASS;
- dataset runnable validation PASS;
- SCI-R00 runnable validation FAIL as required on `status=draft_not_run`;
- compile PASS;
- `git diff --check` PASS.

Preflight receipt semantics are explicit: `self_effects` describe only the
preflight process, while `observed_preexisting_state` reports project-local
solver/source artifacts. A runnable project-local micromamba now satisfies the
solver-discovery gate without temporary PATH mutation.

Real-host preflight disposition: `READY_FOR_OPERATIONAL_APPROVAL`.
This is readiness for a separate strict-environment operation, not R00 PASS.

## Strict environment boundary

`environment_full.yml` remains unmodified and declares `defaults` before
`conda-forge` with exact historical Windows build strings. No strict solve or
package download has started.

Current Anaconda repository terms make access to Anaconda-maintained repositories
context-dependent. The project has not established enough usage/organization
context to classify `defaults` access, so the strict solve is intentionally stopped
before that repository boundary. Channels are not silently rewritten.

## Science / game claim ceiling

- source/data: `VERIFIED LOCAL`;
- strict solver executable: `VERIFIED LOCAL`;
- strict environment: `NOT SOLVED`;
- SCI-R00: `NOT PASSED`;
- SCI-R01: `NOT RUN`;
- published-result reproduction: `NOT CLAIMED`;
- GATE-P01 human playtest: `NOT RUN`;
- Linux/macOS full suite: `UNVERIFIED`;
- `main` merge/deployment: `NOT DONE`.

## Next gate

1. Commit/push this provenance + preflight semantic repair and obtain push/PR CI.
2. Keep PR #1 draft; merge remains a separate decision.
3. Resolve the Anaconda-maintained repository access boundary for the intended use.
4. Only then attempt the unmodified `environment_full.yml`, preserving solver
   failure without dependency/channel substitution.

∆ — R00 moved from preregistration-only to verified local solver + source/data provenance.
D — artifact QC + 101 tests + runnable dataset gate; strict environment untouched.
Ω — high for bytes/provenance; environment and scientific conclusions remain unknown.
Λ — revise after CI of this patch or a governed strict-environment attempt.
