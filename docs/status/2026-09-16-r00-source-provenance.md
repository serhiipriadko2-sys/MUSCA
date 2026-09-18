# MUSCA — SCI-R00 solver + source/data provenance receipt

Observed: 2026-09-16 on authorized Windows research host.
Branch at start of this receipt: `docs/musca-foundations`.
Pre-receipt HEAD: `b42c125eeedd4e26b2cc6d5dcdf090e60077d4ec`.

Status: `SOLVER MATERIALIZED / SOURCE+DATA VERIFIED / STRICT ENV NOT SOLVED / R01 NOT RUN`.

## Governance boundary

[FACT] ADR-0005 is `accepted` and is bounded to Shiu/FlyWire-v630 as the first
strict reproduction lineage. Acceptance is not R00/R01 scientific success and
does not grant dataset redistribution or commercial-use rights.

[FACT] Push CI #16 and PR CI #17 for `b42c125...` completed successfully.

## Portable solver receipt

Project-local micromamba was acquired without admin installation, shell init or
persistent PATH mutation:

- version: `2.9.0`;
- archive: `E:\MUSCA_RESEARCH\downloads\micromamba-win-64-latest.tar.bz2`;
- archive bytes: `4572807`;
- archive SHA-256: `97a336f4ab794bd96a6a4da5e6ed63e75a1d31830414a182419b23d3b36f3fe0`;
- executable: `E:\MUSCA_RESEARCH\tools\micromamba.exe`;
- executable bytes: `11454464`;
- executable SHA-256: `a6d804394b2418991c4e29562853eaace2f2ce9d9da661a98e74e02e8dbb44b0`.

Micromamba is used through the project-local path. `MAMBA_ROOT_PREFIX` is intended
for `E:\MUSCA_RESEARCH\mamba-root`; no environment has been created there yet.

## Exact upstream retrieval

Target upstream state:

`philshiu/Drosophila_brain_model@91bdd1e7dcf193f3e7ca5a8933497fcef63b7960`

Normal clone and shallow Git fetch both stalled on this host's Git HTTPS path.
Those incomplete transfers were stopped and are not treated as provenance.
Retrieval then used GitHub codeload for the exact commit archive.

Authoritative archive:

- path: `E:\MUSCA_RESEARCH\downloads\Drosophila_brain_model-91bdd1e7.zip`;
- bytes: `190814359`;
- SHA-256: `0dc3778bd3b668d8c48d0a98e2ef33e2468335e6cd374f95a37426ce1fe5c4b0`;
- ZIP members: `22` total / `19` files;
- extracted snapshot: `E:\MUSCA_RESEARCH\shiu-91bdd1e7\source-snapshot`;
- snapshot: `19` files / `197068374` bytes.

Artifact QC: PASS — zero traversal paths, duplicate members, case-fold collisions
or reparse points; all `19/19` file hashes matched archive-to-snapshot round trips.
Maximum observed uncompressed/compressed ratio: `6.864`.

Local artifact-QC receipt:

- `E:\MUSCA_RESEARCH\receipts\2026-09-16-r00-source-artifact-qc.json`;
- bytes: `883`;
- SHA-256: `ab296dfa330b84ec475ef1b5db8aef55da62921c9bd5fba7bfbe2cd4a8549fd2`.

## Key file receipts

| File | bytes | SHA-256 | computed Git blob |
| --- | ---: | --- | --- |
| `model.py` | 11900 | `fc45837d7122c6ce2a7f3f2f23c515992e4b232aadb919efabb72337fac88e4e` | `5ba7083cf55bf6092967f8d9065e86cd677efed1` |
| `example.ipynb` | 10282 | `1737c3043af700504c4791c4bfc02d1856bbeb0d36c469832e4b9399dfddda3a` | `bc479485c42c18ef50762d9ab50817bc96d65c8d` |
| `environment.yml` | 174 | `52991bcc78eb5a18534c2c02e8240330f8ad376f31f63546756ad0724924a667` | `27599c545d10fc702777cb372ab4333e73c225b8` |
| `environment_full.yml` | 3445 | `402fcef4968c99397b0e908b3323ab9e6ef86af5583e46e6427f96f3d1091adf` | `1428b314d40bf8b7dc2cb3991db213c12033fdfc` |
| `2023_03_23_completeness_630_final.csv` | 3057611 | `e6b71e17671a9bdb05f55e4bc6774640a1418cb7a05125e0fc994ad40f9bfdfb` | `be745f0ce054308df21accc5c4b3883aa38498f9` |
| `2023_03_23_connectivity_630_final.parquet` | 86630944 | `94db8c650533bc36ffa3223f2e62325d5648b8d6bd31c3a4e1c804628c7557b3` | `8b4d9531bc0acbda7c2074ae577e6ac9e2fca166` |

The four preregistered Git object IDs for `model.py`, `environment_full.yml` and
the two v630 inputs match the locally computed Git-blob IDs.

## Strict environment boundary

`environment_full.yml` is preserved unmodified. It pins Windows/Python 3.10.11
package builds and declares channels in this order: `defaults`, then `conda-forge`.
No strict solve, package download or dependency import has been started.

Current Anaconda Terms distinguish free-use categories from usage requiring a
paid plan, and Anaconda.org distinguishes Anaconda-maintained channels from
community channels. The project does not currently contain enough authority/context
to classify the intended `defaults` repository access. Therefore this receipt
stops before contacting that package repository rather than silently changing
channels or assuming eligibility.

This is an operational/compliance boundary, not evidence that the environment is
unsatisfiable. It is not legal advice.

Official references checked 2026-09-16:

- https://www.anaconda.com/legal/terms/terms-of-service
- https://www.anaconda.com/legal/terms/anaconda-org

## Quarantine / failed transfers

Earlier incomplete Git/codeload attempts remain outside the authoritative source
path under `E:\MUSCA_RESEARCH\quarantine\2026-09-16-source-transfer-failures`.
They are retained only as failure/audit evidence and are not input to R00.

The earlier containment receipt remains historical: its observations were true at
containment time, before the later verified codeload retrieval completed.

## Claim ceiling

- dataset/source retrieval: `VERIFIED LOCAL`;
- portable strict solver: `MATERIALIZED / VERIFIED EXECUTABLE`;
- strict environment: `NOT SOLVED`;
- dependency imports: `NOT RUN`;
- v630 data load under strict environment: `NOT RUN`;
- model construction: `NOT RUN`;
- SCI-R00: `NOT PASSED`;
- SCI-R01 sugar experiment: `NOT RUN`;
- published-result reproduction: `NOT CLAIMED`.

Rollback for new host artifacts is bounded to the project-local
`E:\MUSCA_RESEARCH` tree; no system PATH/shell initialization was persisted.

∆ — exact source/data bytes and portable solver are now independently receipted.
D — accepted ADR → solver hash → exact codeload → ZIP QC → blob/SHA-256 verification.
Ω — high for local source/data provenance; environment/scientific result remain unknown.
Λ — revise only after package-repository access is appropriately resolved and the unmodified strict environment is attempted.
