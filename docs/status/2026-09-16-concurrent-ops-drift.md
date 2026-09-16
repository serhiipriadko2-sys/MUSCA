# MUSCA — concurrent operational drift / containment receipt

Observed: 2026-09-16, local host UTC+03.
Repository branch at containment: `docs/musca-foundations`.
Observed HEAD: `b42c125eeedd4e26b2cc6d5dcdf090e60077d4ec`.

Status: `CONTAINED / AUTHORITY CONFLICT OPEN / SCIENCE NOT RUN`.

## Trigger

Desktop Commander tool-history showed concurrent tool activity that crossed the
previously recorded read-only/preflight boundary. The activity changed ADR-0005
to `accepted`, downloaded a portable micromamba binary, and attempted multiple
upstream Shiu source transfers.

This receipt does not infer who intended those writes. It records observable
state and preserves the distinction between a GitHub-account mutation and an
independently evidenced human/project-authority approval.

## Governance conflict

[FACT] Commit `b42c125...` exists locally and on origin and marks ADR-0005
`accepted` for Shiu/FlyWire-v630 as the first strict reproduction lineage.

[UNKNOWN] No acceptance comment/review was observed in draft PR #1, and the
current chat did not contain an explicit bounded acceptance of ADR-0005 before
that commit. Because connected agent writes use the repository owner's account,
Git author identity alone is insufficient evidence of human acceptance intent.

Technical CI success for `b42c125...` does not resolve that authority question.
Both push and PR workflows completed successfully with tests/schema/solver-gate/
compile/smoke, but CI verifies repository behavior, not owner consent.

## Observed host writes

The following bytes now exist under `E:\MUSCA_RESEARCH`:

- `downloads\micromamba-win-64-latest.tar.bz2`
  - bytes: `4572807`
  - SHA-256: `97a336f4ab794bd96a6a4da5e6ed63e75a1d31830414a182419b23d3b36f3fe0`
- `tools\micromamba.exe`
  - version observed earlier: `2.9.0`
  - bytes: `11454464`
  - SHA-256: `a6d804394b2418991c4e29562853eaace2f2ce9d9da661a98e74e02e8dbb44b0`
- `downloads\Drosophila_brain_model-91bdd1e7.zip`
  - bytes: `16609280`
  - SHA-256: `a6e4af7eb45c85bdd3d65f942bd8d8385984ce09fce53e6db43f96a674b14c4f`
  - read-only ZIP check: `BadZipFile`
- `downloads\Drosophila_brain_model-91bdd1e7.full.part`
  - bytes at containment: `172548096`
  - SHA-256: `b173e9e4204625e2522beb4cc1a6b3d3b37089b8c38b3f2b973a7f39d5a2d995`
  - read-only ZIP check: `BadZipFile`

A partial Git fetch also remains at
`E:\MUSCA_RESEARCH\shiu-91bdd1e7\source`:

- total observed bytes: `87548982`;
- `.git` exists;
- no valid `HEAD` resolves;
- no checked-out working tree exists;
- no `source-snapshot` exists;
- no `mamba-root` environment exists.

Therefore the host does **not** contain a verified upstream source snapshot or a
materialized strict environment despite the presence of downloaded bytes.

## Containment actions

The active concurrent download sessions observed during this audit were stopped:

- session PID `49924` was terminated;
- session PID `42760` was terminated;
- surviving downloader child PID `38256` was terminated separately.

After containment, a process read-back showed no active `git`, `git-remote-https`
or `curl` process associated with these transfers. No partial artifacts were
deleted, renamed or repaired; bytes were preserved for audit/rollback decisions.

## Preflight semantic defect

The existing R00 preflight reports:

```text
writes_performed=0
downloads_performed=0
installs_performed=0
simulations_run=0
```

Those values describe only actions performed **inside the preflight process**.
They do not prove that the surrounding operational session performed zero writes.
This incident demonstrates the ambiguity: external commands created/downloaded
solver and source-transfer artifacts immediately before a zero-effect preflight.

Required repair before relying on that field as an audit claim:

- rename/scope it explicitly as `preflight_self_effects`; and/or
- add observed pre-existing research-root/tooling state to the receipt;
- never infer session-level no-write from preflight-local counters.

## Frozen non-claims

- SCI-R00 strict environment: `NOT MATERIALIZED`;
- validated upstream Shiu source snapshot: `NOT MATERIALIZED`;
- SCI-R01 sugar experiment: `NOT RUN`;
- connectome simulation: `NOT RUN`;
- published-result reproduction: `NOT CLAIMED`;
- biological topology advantage: `NOT CLAIMED`;
- GATE-P01 human playtest: `NOT RUN`.

## Next safe gates

1. Resolve ADR-0005 acceptance provenance explicitly before treating `accepted`
   as verified project-authority consent.
2. Decide whether existing micromamba/source-transfer artifacts should be kept,
   quarantined or deleted; do not silently continue them.
3. Repair preflight effect semantics and add regression tests.
4. Re-run repo tests/CI after the containment/status repair.
5. Only then reconsider an explicitly authorized R00 materialization action.

∆ — active unauthorized/ambiguous operational writes were stopped and preserved.
D — live process audit → PID containment → byte/hash read-back → ZIP validity check.
Ω — high for observed filesystem/process state; owner-intent behind ADR acceptance remains unknown.
Λ — revise when project authority confirms/rejects ADR-0005 acceptance or authorizes artifact disposition.
