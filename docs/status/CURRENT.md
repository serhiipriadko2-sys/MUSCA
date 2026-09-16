# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/локальных фактов
предпочитать новый connector/read-back этому документу.

Последние подтверждённые вехи:

- [GATE 0 / первый GitHub CI](2026-09-16-gate0.md);
- [scientific preregistration](2026-09-16-science-prereg.md);
- [manifest validator](2026-09-16-manifest-validator.md);
- [SCI-R00 read-only preflight](2026-09-16-r00-preflight.md).

## Репозиторий

[FACT @ GitHub/Remote Desktop] `docs/musca-foundations` опубликована и
синхронизирована с origin. Последний CI-verified pre-acceptance HEAD:
`00151f7babe2f70b764d8ec06466dd9de86ff806`.

[FACT] `main` остаётся на
`7c56ff5da706894e07bb3b3aa85798304c4ae925`.
Draft PR #1 открыт; merge в `main` не выполнен.

## Исполняемый стенд

[FACT] Dependency-free Python prototype содержит discrete world, scripted
`MuscaBackend`, отдельный Bridge, event interpretation, human choice и «Шлюз».
Биологической модели и внешнего LLM нет.

## Проверка

[FACT @ local verification] Windows/Python 3.14.6:

- 98 unit tests PASS;
- R00 preflight module: 10 tests PASS;
- registered dataset/R00/R01/GATE-P01 schemas PASS;
- compile PASS;
- `git diff --check` PASS.

[FACT @ GitHub Actions] Для commit `99fc611...`:

- PR run `35106510813` (#12): success;
- push run `35106513906` (#13): success;
- unit tests, manifest schema, R00 approval-gate, compile и deterministic smoke
  завершились `success` на обоих runs.

Full Linux/macOS suite остаётся `UNVERIFIED`, не FAIL и не PASS.

## Manifest / R00 gates

`scripts/validate_manifests.py` разделяет `schema` и `runnable`.
SCI-R01 и GATE-P01 остаются `draft_not_run` и fail-closed на runnable.

`scripts/research_r00_preflight.py` является read-only gate перед R00.
Он не создаёт environment/storage, не скачивает данные и не запускает simulation.
Максимальный положительный статус: `READY_FOR_OPERATIONAL_APPROVAL`.

Current real-host preflight disposition after ADR acceptance: `BLOCKED`.

- `host_solver`: `micromamba/mamba/conda` не обнаружены;
- Python 3.10 runtime не установлен;
- `uv 0.10.10` присутствует, но не считается заменой strict conda YAML solver;
- `E:\` доступен, ~354 GB free; `E:\MUSCA_RESEARCH` ещё не создан.

Effects reported by preflight: writes/downloads/installs/simulations = `0`.

## Science / governance

Science status: `PREREGISTERED / NOT RUN`.
ADR-0005: `ACCEPTED` — Shiu/FlyWire-v630 только как первый strict reproduction
baseline. Acceptance снимает governance blocker, но не является R00/R01 PASS и
не даёт коммерческих прав на dataset.

R00 имеет отдельный candidate manifest; R01 по-прежнему не запускался.

## Game / licensing

GATE-P01 human playtest: `NOT RUN`.
MMO, persistent world, economy и большой multiplayer остаются вне текущего scope.
Проектный `LICENSE` не выбран; data/code/game-asset rights не смешиваются.

## Solver decision candidate

[FACT] Подготовлен `docs/research/R00_SOLVER_DECISION_CANDIDATE.md`.
Статус: `CANDIDATE / NOT SELECTED / NO INSTALL`.

[INTERP] Первый предпочтительный operational candidate после отдельного ADR acceptance —
portable micromamba с project-local `MAMBA_ROOT_PREFIX` на `E:`. Это не выбор и
не разрешение на установку. Conda/Miniforge остаётся discriminating second path;
`uv` допускается только в отдельно названной compatibility lineage.

Отдельный blind spot: upstream `environment_full.yml` использует `defaults` вместе
с `conda-forge`, поэтому channel/repository terms проверяются отдельно от лицензии
самого solver.

## Следующие ворота

1. Зафиксировать solver decision candidate и получить GitHub CI/read-back нового HEAD.
2. Draft PR #1 оставить review surface; merge — отдельное решение.
3. Project authority отдельно принимает или отклоняет ADR-0005.
4. Только после acceptance выбрать solver и отдельно разрешить его acquisition/install.
5. R00 environment/data materialization остаётся следующим отдельным operational gate.
6. Независимо провести GATE-P01 human playtest по preregistered protocol.

∆ — R00 preflight верифицирован; solver choice разложен на строгие альтернативы без установки.
D — `00151f7...`; Actions push #14 + PR #15 green; PR #1 обновлён; solver candidate создан локально.
Ω — высокий для repo/Windows/preflight; solver materialization/science/game остаются bounded unknown.
Λ — пересмотреть после solver-candidate CI, ADR decision, operational install, player dataset или R00/R01 run.