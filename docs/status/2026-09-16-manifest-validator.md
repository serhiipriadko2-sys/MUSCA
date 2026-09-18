# MUSCA — manifest validator receipt

Observed: 2026-09-16.
Status: `PASS / COMMITTED / PUSHED / CI-VERIFIED` for the validator feature.
Scientific experiments remain `NOT RUN`.

## Substantive commits

`f70537347c533379fae60de90562b140db17d66d`
— `research: add fail-closed manifest validation`.

`357664106296e31e005ef9a7db57dc7a356764a2`
— `research: harden manifest execution gate`.

The validator introduces two distinct claims:

- `schema`: required shape, field types and non-blank required strings;
- `runnable`: additional execution-readiness checks.

`schema PASS` is explicitly not an authorization to run an experiment.

## Runnable hardening

A runnable experiment is blocked when its status remains draft/not-run, when its
code reference is not frozen, when required metrics/fixed conditions are empty,
or when its dataset linkage cannot be verified.

A runnable dataset must not contain pending/planned/not-retrieved markers and its
`local_storage` path must exist. `not_applicable` dataset references require an
explicit reason.

## Contract alignment

GATE-P01 was brought to the current experiment-manifest shape by adding:

- `validation_worlds: []`;
- `trainable_parameters: []`;
- an explicit `dataset_manifest=not_applicable` reason.

Its hypothesis, assignments, metrics and success criteria were not changed.

## Verification

Local Windows/Python 3.14.6:

- 88 unit tests PASS;
- manifest test module: 13 tests PASS;
- registered SCI dataset candidate, SCI-R01 and GATE-P01: schema PASS;
- SCI-R01 runnable check: expected FAIL while `draft_not_run`;
- compile PASS;
- `git diff --check` PASS;
- staged secret-pattern scan PASS.

GitHub Actions on commit `3576641...`:

- push run `35102476429` (#8): success;
- pull-request run `35102480304` (#9): success;
- unit tests, manifest schema, compile and deterministic smoke: success on both.

## Non-claims

No dataset was downloaded, no research environment was installed, no connectome
model was executed, no player session was run and no scientific result was promoted.

∆ — manifests moved from prose-only discipline to an executable fail-closed gate.
D — commits `f705373...` and `3576641...`, local 88 tests, Actions runs #8/#9.
Ω — high for validator behavior on the verified Windows surface; science remains unknown.
Λ — revise if manifest schema changes, a runnable experiment is registered, or R00 begins.
