# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/локальных фактов всегда
предпочитать новый read-back этому документу.

Последние подтверждённые вехи:

- [GATE 0 / первый GitHub CI](2026-09-16-gate0.md);
- [scientific preregistration](2026-09-16-science-prereg.md);
- [сборка игрового пилота](2026-09-16-pilot-build.md);
- [игровой этап «Шлюз»](2026-09-16-gate.md).

## Текущая стадия

[FACT] Реализован технический стенд: дискретный мир, скриптовый `MuscaBackend`,
отдельный Bridge, интерпретатор событий и терминальный выбор человека. Есть
терминальная загадка «Шлюз». Биологической модели и внешнего LLM нет. Игровая
приёмка с добровольцами и научный connectome-эксперимент ещё не проведены.

[FACT @ GitHub read-back] `docs/musca-foundations` опубликована и содержит
последовательность GATE 0 → scientific preregistration → status receipt.
До предлагаемого ADR текущий HEAD:
`089553ab5d00e62893f063a0582931bec1509421`.

[FACT @ GitHub read-back] `main` остаётся на
`7c56ff5da706894e07bb3b3aa85798304c4ae925`. Merge в `main` не заявляется.
## CI и проверка

[FACT @ GitHub Actions] Три последовательных push-runs завершили job
`Windows / Python 3.14.6` успешно:

- run `35098073671` (#1), GATE 0 commit `4280963...`;
- run `35098789752` (#2), scientific-preregistration commit `67c95259...`;
- run `35099417750` (#3), status-receipt commit `089553ab...`.

На каждом опубликованном проверенном commit unit-test, compile и deterministic
smoke steps завершились `success`.

**PASS — reference Windows surface:** Python 3.14.6, 75 unit tests локально и
независимый GitHub-hosted Windows CI.

**UNVERIFIED — full cross-platform suite:** в отдельной Linux/container среде
пять test modules прошли по отдельности (суммарно 75 тестов), но общий
`unittest discover` не завершился внутри доступного execution window. Это не
доказанный дефект Linux и не Linux PASS.

## Scientific lineage

Опубликованы preregistration plan и candidate manifests для SCI-R00/R01. Они
фиксируют Shiu upstream commit, FlyWire-v630 lineage, original-environment gate,
`150 Hz executable code vs 200 Hz tutorial prose` drift, falsifier и claim ceiling.

Science status: `PREREGISTERED / NOT RUN`.
## Governance

[PROPOSED] `ADR-0005-shiu-v630-reproduction-baseline.md` ограничивает первый
научный baseline точным Shiu/FlyWire-v630 reproduction lineage. ADR не выбирает
будущий production/game controller и не разрешает installation/download/run.
До project-authority acceptance он остаётся `proposed`.

## Game lineage

GATE-P01 с добровольцами остаётся `NOT RUN`. Скриптовый `light` vs
`light_chemical` — инженерная фикстура, не доказательство biological advantage
и не доказательство fun. MMO, persistent world, экономика и большой multiplayer
остаются вне текущего scope.

## Лицензирование

Проектный `LICENSE` не выбран. Decision support сохранён в
[LICENSING_DECISION.md](../LICENSING_DECISION.md). Лицензии собственного кода,
third-party code, connectome/data и будущих игровых assets не смешиваются.

## Следующие ворота

1. Опубликовать proposed ADR-0005 и получить CI read-back нового HEAD.
2. После явного принятия ADR отдельно решить operational write-gate для SCI-R00:
   isolated environment + pinned source/data + SHA-256 receipts, без изменения runtime.
3. Провести GATE-P01 как независимый human playtest.
4. Merge в `main` — отдельное review/PR решение, не побочный эффект.

∆ — repo baseline + preregistration имеют тройной зелёный CI; dataset/model choice вынесен в ADR.
D — commits `4280963...`, `67c95259...`, `089553ab...` и Actions runs #1–#3.
Ω — высокий для Windows/repo provenance; science/game/cross-platform bounded unknown.
Λ — обновить после ADR/CI, player dataset, R00/R01 или merge/release transition.
