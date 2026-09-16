# ADR-0005 — Shiu/FlyWire-v630 as the first strict reproduction baseline

Status: `proposed`
Date: 2026-09-16
Owner: MUSCA project
Builder/package mirror: `not-needed`
Live verification: `not-needed`

## Context

MUSCA needs one reproducible neuroscience lineage before building a `ConnectomeBackend` or comparing biological topology with artificial/randomized controls.

The project already preregisters SCI-R00/R01 around the published Shiu et al. Drosophila brain model. However, `RESEARCH_PROTOCOL.md` requires an ADR before choosing a dataset family and neural model for an experiment that can affect project conclusions.

The decision must not be confused with selecting the long-term MUSCA production controller, game dataset, commercial data source, or definitive biological model.

## Decision

For the **first strict reproduction lineage only**, use:

- upstream code: `philshiu/Drosophila_brain_model`;
- pinned commit: `91bdd1e7dcf193f3e7ca5a8933497fcef63b7960`;
- dataset lineage: FlyWire materialization v630 files bundled with that pinned upstream state;
- neural dynamics: the upstream Brian2 leaky-integrate-and-fire implementation without source/scientific-parameter modification;
- dependency priority: reproduce `environment_full.yml` first, then create a separately named compatibility lineage only if strict reproduction cannot be materialized.

This decision authorizes **reproduction design**, not execution. Environment creation, downloads and simulation remain separate operational write gates.
## Why this baseline

The paper/model pair provides an unusually tight provenance chain: peer-reviewed result → official code → bundled connectivity/completeness inputs → tutorial → archived outputs. Reproducing that chain gives MUSCA a known target before introducing its own Bridge, motor decoding, body model or null graphs.

## Alternatives considered

### A. Start from current FlyWire/public v783

Rejected for the first strict reproduction because the paper used v630. A newer connectome would create a different lineage and weaken attribution of reproduction failures/successes.

### B. Start from MaleCNS v1.0

Deferred. MaleCNS is attractive for later experiments, including its more permissive stated CC-BY data license, but it is not the dataset/model used by the Shiu paper and therefore cannot answer the first reproduction question.

### C. Skip paper reproduction and build MUSCA directly

Rejected. A custom connectome controller would combine dataset choice, neural dynamics, sensor mapping, motor decoding and Bridge effects before MUSCA has demonstrated that it can reproduce a known upstream computation.

### D. Use a modernized Python/Brian2 stack immediately

Deferred to a separately named compatibility lineage. Silent modernization would make failures ambiguous between original model assumptions and dependency drift.

## Consequences / price

Benefits:

- strong provenance and a concrete falsifier;
- bounded claim ceiling;
- reproduction failure is diagnosable before MUSCA-specific adaptation;
- preserves a clean path to later biological-vs-null experiments.
Costs/risks:

- original environment is old and may be harder to solve on the current host;
- whole-brain runs are computationally slow;
- current FlyWire public-data licensing includes a non-commercial restriction that is material to future commercial distribution;
- upstream tutorial prose and executable default disagree on activation frequency (`200 Hz` prose vs `150 Hz` code);
- this baseline may be scientifically unsuitable for later embodied locomotion tasks even if reproduction succeeds.

## Tests / QA

T1 — source pin: exact upstream commit read-back.

T2 — data provenance: v630 input files present; local SHA-256 recorded after retrieval.

T3 — environment: strict full environment solved or failure receipt preserved without silent substitution.

T4 — R00: imports/data load/model-construction smoke under declared budget.

T5 — R01: official tutorial sugar activation executes 30 trials with structurally valid output and complete provenance.

T6 — claim boundary: R01 PASS must not be described as topology superiority, figure-level reproduction, game usefulness or commercial clearance.

T7 — drift: preserve `DRIFT-SHIU-ACTIVATION-FREQ-001` until a figure-specific R02 resolves it against paper Methods/archived output.

## Diff scope

This ADR and the already prepared reproduction plan/manifests. No MUSCA runtime code, Bridge contract, game content or production infrastructure changes are authorized by this ADR.
## Rollback

If this ADR is rejected, keep the preregistration as historical research material, mark it superseded/not-selected, and choose another dataset/model through a new ADR. No dataset deletion or destructive migration is required.

## Acceptance boundary

Current lifecycle: `proposed`.

Promotion to `accepted` requires project-authority acceptance of this exact bounded decision: **Shiu/FlyWire-v630 for the first strict reproduction lineage only**. Acceptance does not authorize software installation, dataset download, experiment execution, merge to `main`, or use in a commercial game.

## ΔDΩΛ

∆ — turns an implicit reproduction choice into an explicit, reversible decision candidate.
D — pinned upstream source + v630 + original LIF/environment lineage; alternatives remain separate.
Ω — high that this is an appropriate reproduction target; no claim that it is the best long-term MUSCA backend.
Λ — reconsider if upstream provenance is invalidated, strict materialization proves impossible, rights prevent the intended research use, or a better primary-source reproduction target materially changes the gate.
