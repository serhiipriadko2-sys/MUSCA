# MUSCA — scientific preregistration receipt

Observed: 2026-09-16.
Status: `PASS AS PREREGISTRATION / NOT RUN AS SCIENCE`.

## Repository state

Scientific reproduction planning was committed and pushed as:

`67c95259c32f25cd787ff1adc229e0f0d1114297`

Commit message: `research: preregister first drosophila reproduction gate`.

The commit added/updated only documentation and experiment/dataset manifests:

- `docs/research/SCI_R00_R01_REPRO_PLAN.md`;
- `data/manifests/SCI-DATA-SHIU-FW630.candidate.json`;
- `experiments/manifests/SCI-R01-SHIU-SUGAR.candidate.json`;
- `docs/LICENSING_DECISION.md`;
- `docs/status/2026-09-16-gate0.md`;
- `docs/status/CURRENT.md`;
- `README.md`.

No `musca/`, `scripts/` or `tests/` runtime/test file changed in this commit.

## Local pre-push verification

- dataset manifest JSON — valid;
- experiment manifest JSON — valid;
- `git diff --check` — PASS;
- Windows/Python 3.14.6 unit suite — 75 tests PASS;
- staged secret-pattern scan — PASS;
- working tree after push — clean.

## GitHub Actions read-back

Workflow run `35098789752` (`ci`, run #2) was created by push of commit
`67c95259...`. Job `Windows / Python 3.14.6` completed with conclusion `success`.
The unit-test, compile and deterministic-smoke steps all completed successfully.

## Scientific claim ceiling

This receipt verifies that the **preregistration artifacts are versioned and the
existing prototype still passes CI**. It does not verify the neuroscience model.

Not yet performed:

- installation of the strict Python 3.10.11/Brian2 2.5.1 reproduction environment;
- retrieval and SHA-256 read-back of the FlyWire-v630 files;
- R00 environment/data smoke;
- R01 30-trial sugar-stimulation run;
- R02 figure/prediction reproduction;
- biological-vs-null MUSCA comparison.

The recorded `150 Hz executable code vs 200 Hz tutorial prose` conflict remains an
explicit drift, not silently reconciled.

## Lifecycle
`preregistered → locally-validated → committed → pushed → CI-verified`.

Not claimed: `environment-created`, `dataset-retrieved`, `experiment-run`,
`scientifically-reproduced`, `connectome-advantage-demonstrated`.

∆ — first MUSCA neuroscience lineage now exists as versioned preregistration.
D — commit `67c95259...` plus GitHub Actions run `35098789752`.
Ω — high for repository/provenance state; scientific result remains unknown.
Λ — revise only after R00/R01 execution evidence or an upstream source/rights change.
