# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/локальных фактов всегда
предпочитать новый read-back этому документу.

Последняя подтверждённая репозиторная веха — [GATE 0](2026-09-16-gate0.md).
Предшествующий candidate receipt сохранён как
[2026-09-16-gate0-candidate.md](2026-09-16-gate0-candidate.md).
Сборка игрового пилота — [2026-09-16-pilot-build.md](2026-09-16-pilot-build.md).
Игровой этап «Шлюз» — [2026-09-16-gate.md](2026-09-16-gate.md).

## Текущая стадия

[FACT] Реализован технический стенд: дискретный мир, скриптовый `MuscaBackend`,
отдельный Bridge, интерпретатор событий и терминальный выбор человека. Есть
терминальная загадка «Шлюз» с платной пробой, двумя скрытыми вариантами и
последствиями выбора. Биологической модели и внешнего LLM нет. Игровая приёмка
с добровольцами и научный connectome-эксперимент ещё не проведены.

[FACT @ GitHub read-back] GATE 0 commit
`428096325dc03bad9770266ae7328cc40f6f52d1` находится в
`docs/musca-foundations` и запушен в `origin/docs/musca-foundations`.
`main` остаётся на `7c56ff5da706894e07bb3b3aa85798304c4ae925`; merge в `main` не заявляется.

[FACT @ GitHub read-back] GitHub Actions run `35098073671` (`ci`, run #1) для
commit `4280963...` завершился `success`. Job `Windows / Python 3.14.6` и его
unit-test, compile и deterministic-smoke шаги завершились успешно.
## Проверка и границы

**PASS — reference Windows surface:** Python 3.14.6, 75 тестов локально, compile
PASS, deterministic smoke PASS; отдельный GitHub-hosted Windows CI также PASS.

**UNVERIFIED — full cross-platform suite:** в отдельной Linux/container среде
пять test modules прошли по отдельности (суммарно 75 тестов), однако общий
`unittest discover` не завершился внутри доступного execution window. Это не
доказанный дефект Linux, но и не основание объявлять Linux PASS.

**[UNKNOWN] Science:** `ConnectomeBackend` отсутствует; pinned R00/R01
preregistration подготовлен, но исследовательская среда, dataset и модель ещё не
исполнялись как MUSCA experiment.

**[UNKNOWN] Game:** GATE-P01 с добровольцами не запускался. Скриптовый контраст
`light` vs `light_chemical` остаётся инженерной фикстурой, а не доказательством
biological advantage или fun.

MMO, persistent world, экономика и большой multiplayer остаются вне текущего scope.

## Научный следующий gate

Подготовлены candidate-файлы первой строгой репликации Shiu et al.:

- `docs/research/SCI_R00_R01_REPRO_PLAN.md`;
- `data/manifests/SCI-DATA-SHIU-FW630.candidate.json`;
- `experiments/manifests/SCI-R01-SHIU-SUGAR.candidate.json`.

Они фиксируют upstream commit, FlyWire-v630 lineage, strict original environment,
150-vs-200-Hz drift, falsifier и claim ceiling. Их наличие не означает RUN/PASS.
## Лицензирование

Проектный `LICENSE` пока не выбран. Отдельный decision note хранится в
`docs/LICENSING_DECISION.md`; public repository не трактуется как автоматически
open-source. Лицензии кода и connectome/data рассматриваются отдельно.

## Следующие ворота

1. Закоммитить preregistration/licensing документы отдельным commit и получить
   новый CI read-back для финального HEAD.
2. Провести GATE-P01 как отдельный human playtest с заранее согласованными
   условиями участия и хранения результатов.
3. После отдельного environment/data write-gate выполнить SCI-R00; только после
   воспроизведения известного upstream результата переходить к biological-vs-null
   эксперименту MUSCA.

∆ — GATE 0 удалённо подтверждён; science lineage переведена в preregistration.
D — commit `4280963...`, Actions run `35098073671`, локальные 75 tests и manifests.
Ω — высокий для Windows baseline; science/game/cross-platform остаются bounded unknowns.
Λ — обновить после нового commit/CI, player dataset или scientific run.
