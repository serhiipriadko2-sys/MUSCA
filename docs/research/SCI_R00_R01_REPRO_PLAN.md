# MUSCA SCI-R00/R01 — strict reproduction gate

Status: `CANDIDATE / NOT RUN`
Date: 2026-09-16
Project: ISKRA // MUSCA
Purpose: define the first reproducible neuroscience gate before installing or modifying the MUSCA research environment.

## 1. Decision

Do **not** connect a brain-wide Drosophila model to the game yet.
First reproduce the official Shiu et al. model in a quarantined research environment, pinned to the authors' repository state and FlyWire v630 inputs used by the published work.

This gate tests reproduction engineering. It does **not** test whether biological topology is better than artificial controllers and it does not validate the MMO/game hypothesis.

## 2. Frozen upstream source

- Repository: `philshiu/Drosophila_brain_model`
- Commit: `91bdd1e7dcf193f3e7ca5a8933497fcef63b7960`
- Commit date: 2024-09-14
- Code license reported by GitHub: MIT
- Paper: Shiu et al., *A Drosophila computational brain model reveals sensorimotor processing*, Nature 634, 210–219 (2024), DOI `10.1038/s41586-024-07763-9`
- Paper data archive: DOI `10.17617/3.CZODIW`

Pinned bundled v630 inputs observed in the upstream repository:

- `2023_03_23_completeness_630_final.csv`
  - Git blob: `be745f0ce054308df21accc5c4b3883aa38498f9`
- `2023_03_23_connectivity_630_final.parquet`
  - Git blob: `8b4d9531bc0acbda7c2074ae577e6ac9e2fca166`
A Git blob ID is source provenance, not a SHA-256 checksum. After local retrieval, record ordinary SHA-256 values separately.

## 3. Environment boundary

The upstream README says `environment_full.yml` contains the package versions used in the original work. It pins, among other packages:

- Python `3.10.11`
- Brian2 `2.5.1`
- NumPy `1.22.3`
- pandas `1.4.3`
- joblib `1.2.0`
- pyarrow `11.0.0`

The smaller `environment.yml` instead asks for Python 3.10, Brian2 2.5.1 and NumPy 1.24. This is an environment drift. **Strict R00 uses `environment_full.yml` first.** If that environment cannot be solved on the target machine, preserve the failure receipt and create a separately named compatibility reproduction; never silently substitute the minimal environment.

Current host observation before this plan:

- Windows host, Intel i7-10700K, ~32 GB RAM, RTX 3070 8 GB
- Python 3.12/3.13/3.14 available; Python 3.10 not observed
- Conda / mamba / micromamba not observed
- Brian2 / FlyGym / MuJoCo not installed
- `E:` has substantially more free space than `C:`

No installer or environment mutation was executed while preparing this candidate.

## 4. Parameter drift that must be frozen

At upstream commit `91bdd1e7...`:

- executable `model.py` defines `t_run = 1000 ms`, `n_run = 30`, `r_poi = 150 Hz`;
- `example.ipynb` prose says the neurons are excited at `200 Hz` by default;
- the notebook calls `run_exp(...)` without overriding `r_poi` in its first sugar example.

Therefore the pinned executable code implies **150 Hz** for that call. R01 must not edit this to 200 Hz merely to match prose. The discrepancy is recorded as `DRIFT-SHIU-ACTIVATION-FREQ-001` and should be checked against the paper Methods and archived outputs before any claim of figure-level reproduction.

## 5. Stochasticity boundary

The upstream model uses Brian2 `PoissonInput`. No explicit random seed was observed in the pinned `model.py`/tutorial path inspected for this gate.

Consequences:

- exact spike-file hashes are **not** a valid reproduction criterion;
- exact spike counts are not assumed to match run-to-run;
- R01 records distributions, number of trials, active-neuron counts, output shape, runtime and failures;
- a later confirmatory reproduction should explicitly specify seed handling if the upstream method permits it.

## 6. R00 — environment/data smoke

### Goal

Prove that the exact upstream source, input data and original dependency environment can be materialized without modifying MUSCA runtime code.

### Planned location

`E:\MUSCA_RESEARCH\shiu-91bdd1e7\`

The path is a plan, not a claim that the directory already exists.

### Steps after explicit write/install approval
1. Clone or fetch `philshiu/Drosophila_brain_model` and checkout detached commit `91bdd1e7...`.
2. Verify clean tree and record commit SHA.
3. Compute SHA-256 for `model.py`, `example.ipynb`, both v630 data files, `environment.yml` and `environment_full.yml`.
4. Create a **separate** research environment from `environment_full.yml`.
5. Record solved package list and platform metadata.
6. Import Brian2 and the upstream model.
7. Load v630 completeness/connectivity inputs without running the 30-trial experiment.
8. Construct the model once if resource use is acceptable; record wall time and peak memory.
9. Do not change source, dataset or scientific parameters.

### R00 PASS

- exact upstream commit observed;
- required files present and readable;
- SHA-256 receipt written;
- original/full environment solved and imports succeed;
- v630 data load succeeds;
- no source mutation;
- resource use fits the declared local budget.

### R00 PARTIAL

The source/data are verified but the exact environment does not solve or model construction exceeds the initial budget. Preserve the error and design a separately named compatibility path.

### R00 FAIL

Wrong source state, corrupted/missing inputs, silent dependency substitution, or unrecorded source/data mutation.

## 7. R01 — official-code sugar activation smoke
R01 begins only after R00 PASS or an explicitly versioned compatibility decision.

### Stimulus

Use the 21 right-hemisphere sugar-sensing FlyWire neuron IDs from the pinned `example.ipynb`.

### Frozen baseline

- FlyWire v630 files bundled with the upstream source
- upstream `model.py` unchanged
- `t_run = 1000 ms`
- `n_run = 30`
- first sugar call uses executable default `r_poi = 150 Hz`
- no silenced neurons
- no second excitation class
- record `n_proc` as an execution parameter

### Primary R01 question

Can the unmodified pinned upstream model execute the tutorial sugar-stimulation experiment on the local research host and produce a structurally valid 30-trial spike result?

### R01 is **not** yet a paper-result claim

A successful run shows that the upstream computational experiment is locally executable. It does not by itself reproduce a specific Nature figure, validate the biological predictions, or show that MUSCA topology beats a control.

### Measurements

- process exit / exception status
- wall time
- peak host RAM if measurable
- output file size + SHA-256 (artifact integrity only)
- trial identifiers present
- total spike rows
- number of active neurons
- distribution of spike counts/rates across trials where practical
- exact source/data/environment receipts

### R01 PASS

- 30 requested trials complete;
- output is readable and schema-valid;
- all provenance receipts are present;
- no source/data/scientific-parameter mutation occurred;
- stochastic numerical results are reported as observations, not forced to a preselected exact count.

## 8. R02 — later figure/prediction reproduction

R02 is deliberately out of this gate. It should choose one explicit published figure/prediction, resolve the 150/200 Hz drift for that exact experiment, pin the analysis code and compare the locally derived statistic with the paper/archive under a predeclared tolerance or uncertainty rule.

Only after an R02-class result should MUSCA claim reproduction of a published scientific result rather than execution of the authors' model.

## 9. Licensing and distribution boundary

The upstream **code repository** reports MIT. The current FlyWire public-release guideline states that public FlyWire data are CC BY-NC 4.0. Those are different rights layers.

For this reason:

- keep the research dataset outside the game distribution;
- do not assume the MIT code license grants commercial rights to the underlying connectome data;
- confirm file-level/data-release rights before redistribution or any commercial derivative use;
- MaleCNS v1.0 is published under CC-BY and is a promising later dataset candidate, but it is not a drop-in substitute for reproducing the Shiu/FlyWire-v630 paper.

This document is not legal advice.

## 10. Next decision after R01

If R01 succeeds: design R02, then a topology experiment with matched artificial/randomized controls.

If R01 fails for environment reasons: preserve the exact failure and create a compatibility reproduction without overwriting the strict lineage.

If R01 fails because the pinned upstream source/data are internally inconsistent: stop and resolve that upstream inconsistency before adapting the model to MUSCA.

## Verification state

`NOT RUN` — this file is a pre-registration candidate only. No research environment, package, dataset or upstream repository was installed/downloaded by this plan.

## 11. Read-only R00 preflight

`scripts/research_r00_preflight.py` is the non-mutating gate before any R00
materialization. It validates the R00/dataset manifest shape, reads ADR-0005,
probes host tooling/storage and reports typed blockers.

The tool must not create `E:\MUSCA_RESEARCH`, install packages, download upstream
bytes or execute a neural simulation. Its maximum positive disposition is
`READY_FOR_OPERATIONAL_APPROVAL`, not `READY_TO_RUN`.

Current policy requires ADR-0005 to be `accepted` before materialization. Strict
`environment_full.yml` also requires an observed conda-compatible solver
(`micromamba`, `mamba` or `conda`); `uv` is not silently treated as equivalent.
A missing preinstalled Python 3.10 is informational rather than a blocker when a
strict solver can materialize the pinned Python 3.10.11 environment.

The CI invocation currently requires the `governance` blocker while ADR-0005 is
`proposed`. Accepting that ADR therefore requires an intentional CI/gate update;
it cannot silently turn the preregistration into execution authorization.
