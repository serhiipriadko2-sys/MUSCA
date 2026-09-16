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

[FACT @ GitHub read-back] `docs/musca-foundations` содержит GATE 0 commit
`428096325dc03bad9770266ae7328cc40f6f52d1` и scientific-preregistration commit
`67c95259c32f25cd787ff1adc229e0f0d1114297`; оба запушены.

[FACT @ GitHub read-back] `main` остаётся на
`7c56ff5da706894e07bb3b3aa85798304c4ae925`. Candidate branch на три commits
впереди `main` и не отстаёт. Merge в `main` не заявляется.
## CI и проверка

[FACT @ GitHub Actions] Run `35098073671` (#1) для GATE 0 commit `4280963...`
завершился `success`.

[FACT @ GitHub Actions] Run `35098789752` (#2) для scientific-preregistration
commit `67c95259...` также завершил job `Windows / Python 3.14.6` с conclusion
`success`; unit-test, compile и deterministic-smoke steps — success.

**PASS — reference Windows surface:** Python 3.14.6; 75 unit tests локально;
GitHub-hosted Windows CI подтверждён на обоих опубликованных commits.

**UNVERIFIED — full cross-platform suite:** в отдельной Linux/container среде
пять test modules прошли по отдельности (суммарно 75 тестов), но общий
`unittest discover` не завершился внутри доступного execution window. Это не
доказанный дефект Linux и не Linux PASS.

## Scientific lineage

В репозитории опубликованы:

- `docs/research/SCI_R00_R01_REPRO_PLAN.md`;
- `data/manifests/SCI-DATA-SHIU-FW630.candidate.json`;
- `experiments/manifests/SCI-R01-SHIU-SUGAR.candidate.json`.

Они фиксируют upstream commit, FlyWire-v630 lineage, original-environment gate,
`150 Hz executable code vs 200 Hz tutorial prose` drift, falsifier и claim ceiling.
Статус science: `PREREGISTERED / NOT RUN`. Наличие manifests не означает
созданную среду, полученный dataset, выполненный R00/R01 или научное подтверждение.

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

1. После этого status-only commit получить новый CI read-back для актуального HEAD.
2. Провести GATE-P01 как отдельный human playtest с заранее согласованными
   условиями участия и хранения результатов.
3. Отдельным environment/data write-gate выполнить SCI-R00; только после R00/R01
   и воспроизведения известного результата переходить к biological-vs-null MUSCA.
4. Merge в `main` делать отдельным review/PR решением, не как побочный эффект.

∆ — repo baseline и scientific preregistration опубликованы и CI-проверены.
D — commits `4280963...` + `67c95259...`, Actions runs #1/#2, 75 local tests.
Ω — высокий для Windows/repo provenance; science/game/cross-platform bounded unknown.
Λ — обновить после нового CI, player dataset, R00/R01 или merge/release transition.
