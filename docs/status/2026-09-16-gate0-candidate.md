# MUSCA — GATE 0 candidate receipt

Observed: 2026-09-16. This is a candidate receipt prepared from fresh GitHub and
Remote Desktop read-back. It does not claim that the candidate patch has been
committed, pushed, merged or invoked by GitHub Actions.

## Frozen pre-mutation state

- remote repository: `serhiipriadko2-sys/MUSCA`;
- default branch `main`: `7c56ff5da706894e07bb3b3aa85798304c4ae925`;
- candidate branch: `docs/musca-foundations`;
- branch HEAD: `2ba60a95c888ade77fdfafcbe76bca8dabe27d26`;
- local checkout observed clean and aligned with `origin/docs/musca-foundations`;
- the branch is one commit ahead of `main` and zero commits behind;
- no pull request or issue was observed;
- no GitHub Actions run was associated with the branch HEAD;
- project license remains undecided and no project `LICENSE` file is added here.

## Verified baseline

On the authorized Windows machine with Python 3.14.6:

```text
py -3.14 -m unittest discover -s tests -q
Ran 75 tests in 10.291s
OK
TEST_EXIT=0
```

`git diff --check` also returned exit 0 and the working tree was clean.

An isolated Linux/container check gave additional but bounded evidence: each test
module passed when run separately (`14 + 7 + 20 + 18 + 16 = 75` tests), but the
combined discovery command did not finish within the container execution limit.
Therefore cross-platform full-suite status remains `UNVERIFIED`, not FAIL and not
PASS.

## Candidate mutation

This GATE 0 candidate changes repository operations only:

1. add `.github/workflows/ci.yml`;
2. refresh `docs/status/CURRENT.md` so it no longer describes the branch as
   uncommitted/unpublished;
3. add this receipt.

No runtime, Bridge, world, puzzle, experiment manifest, scientific dependency,
dataset, model, player result or research conclusion is changed.

### CI scope

The initial workflow intentionally tests only the already verified reference
surface: `windows-latest` + exact Python `3.14.6`. It uses read-only repository
permissions, runs the 75-test discovery command, compiles Python sources and runs
one deterministic smoke demo.

Linux/macOS are not silently declared supported. A later cross-platform gate must
resolve or reproduce the combined-discovery timeout before adding them as required
CI surfaces.

## Dependency check for CI actions

The proposed workflow uses GitHub-maintained `actions/checkout@v7` and
`actions/setup-python@v7`. Both are official GitHub actions and MIT-licensed.
`setup-python` supports exact patch versions and its current test matrix includes
Python 3.14.6. The workflow grants `contents: read` only.

These observations justify a small CI dependency surface; they do not make third
party actions generally trusted.

## Postconditions required before promotion

After an authorized commit/push, GATE 0 is not PASS until all of the following are
read back from GitHub:

1. the new commit contains exactly the intended status/CI changes;
2. a workflow run exists for that commit;
3. `Windows / Python 3.14.6` completes successfully;
4. no runtime files changed unexpectedly;
5. `CURRENT.md` is refreshed with the new commit SHA and CI run result;
6. the license decision remains explicit rather than being silently inferred.

## Rollback

The change is reversible: remove `.github/workflows/ci.yml`, restore the previous
`CURRENT.md`, and remove this receipt in a follow-up commit. No data migration or
external service state is involved.

## Boundary

Status: `CANDIDATE / TESTED LOCALLY / NOT COMMITTED BY THIS RECEIPT / NOT CI-INVOKED`.

∆ — GATE 0 is reduced to a small, reversible repository patch.
D — fresh branch state, Windows tests and CI dependencies were checked before the patch.
Ω — high for the frozen repository state; CI execution remains unknown until GitHub read-back.
Λ — revise immediately after the candidate is committed/pushed or if the branch HEAD_changes.
