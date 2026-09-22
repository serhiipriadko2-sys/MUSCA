# MUSCA — SCI-R02 runnable activation receipt

Observed: 2026-09-19.
Status: **READY TO RUN / R02 NOT RUN**.
Branch: `science/r02-mn9-laterality`.

## Frozen preregistration

Preregistration commit:
`0a7489e81bbb91db3438a6522d10ebacd4ebf078`

Candidate:
`experiments/manifests/SCI-R02-SHIU-MN9-LATERALITY-200HZ.candidate.json`

Candidate SHA-256:
`eb54f1c605e9e39a668e196fd02e5dd5c650b24325abee99ab94899f2164c661`

Hypothesis, primary metrics, thresholds and claim boundary are inherited from
that committed candidate and are not modified by activation.

## Runnable manifest

Run manifest:
`experiments/manifests/SCI-R02-SHIU-MN9-LATERALITY-200HZ.run.json`

Pre-commit run SHA-256:
`275d20c0dd913dd34f5b82c0f676e92ff315e1c9f5a54d1b69f9c9e8163fcae6`

Activation changes are limited to:
- `status=ready_to_run`;
- concrete result root `E:\MUSCA_RESEARCH\results\SCI-R02-SHIU-MN9-LATERALITY-200HZ`;
- execution parameter `n_proc=4` for both conditions;
- source preregistration commit/hash;
- R00/R01 receipt hashes;
- frozen reference-metrics hash.

No scientific threshold was added or relaxed after preregistration.

## Required evidence

SCI-R00 receipt SHA-256:
`e4770a9b7493fb5dfee4a7ef9d0ff3a62ca8da9126ed5bcf6c4465470f9135b0`

SCI-R01 receipt SHA-256:
`38d7ed8bf191fce5f0495b8e3561483f5fcbe2da4a3140bcad069e7cdb8eec5d`

R02 reference metrics SHA-256:
`f8a61aa90783aa4f9fe137111935b1abee670640aaae332dfa1291cba004556d`

Archived right 200 Hz raw SHA-256:
`cae3cd2823dfbd88a8d957724d63aba2b288b40e6b453bfe13a75f77a8bb6357`

Archived left 200 Hz raw SHA-256:
`3fc01a93e1318479acd7584595f58345227283e238aabf8c7b314afe5a6e60e8`

## Execution boundary
The authorized R02 operation is exactly two simulations:
1. right archived sugar-GRN set, `r_poi=200 Hz`, 30 × 1000 ms;
2. left archived sugar-GRN set, `r_poi=200 Hz`, 30 × 1000 ms.

No frequency sweep, neuron silencing, second excitation class, topology
randomization, parameter tuning or game/runtime mutation is part of this run.

Expected rerun outputs are isolated under the R02 result root and must not
overwrite archived reference files.

Rollback before execution: remove only the uncommitted runnable activation.
Rollback after execution: preserve receipts; raw rerun artifacts may be archived
or removed only as a separate explicit cleanup action.

## Lifecycle

`reference-frozen → candidate-preregistered+committed → runnable-activated → NOT RUN`

## ΔDΩΛ

∆ — committed R02 preregistration now has a separate runnable envelope.
D — candidate hash/commit → R00/R01/reference receipts → n_proc=4 activation.
Ω — high for activation provenance; scientific result remains unknown.
Λ — if runnable validation, input hashes, source cleanliness or host resource
preconditions fail, do not simulate and record the blocker.
