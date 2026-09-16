# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/локальных фактов всегда
предпочитать новый read-back этому документу.

GATE 0 candidate receipt хранится в репозитории как `docs/status/2026-09-16-gate0-candidate.md`,
но намеренно не входит в frozen pilot payload.
Сборка игрового пилота — [2026-09-16-pilot-build.md](2026-09-16-pilot-build.md).
Игровой этап «Шлюз» — [2026-09-16-gate.md](2026-09-16-gate.md).
Исходный аудит — [2026-09-16-audit.md](2026-09-16-audit.md).

## Текущая стадия

[FACT] Реализован технический стенд: дискретный мир, скриптовый `MuscaBackend`,
отдельный Bridge, интерпретатор событий и терминальный выбор человека. Есть
законченная терминальная загадка «Шлюз» с платной пробой, двумя скрытыми
вариантами и последствиями выбора. Биологической модели и внешнего LLM нет.
Игровая приёмка с добровольцами и научный connectome-эксперимент не проведены.

[FACT @ fresh read-back] До GATE 0 patch candidate удалённая ветка
`docs/musca-foundations` имела HEAD
`2ba60a95c888ade77fdfafcbe76bca8dabe27d26`; локальный checkout был чистым и
совпадал с `origin/docs/musca-foundations`. `main` оставался на
`7c56ff5da706894e07bb3b3aa85798304c4ae925`; candidate branch была на один commit
впереди и не отставала от `main`. Это заменяет старую формулировку про
«незакоммиченные/неопубликованные изменения».

[FACT @ fresh read-back] PR и issues не наблюдались. Для HEAD `2ba60a95...` не
наблюдалось GitHub Actions workflow run. Публичная лицензия проекта не выбрана.
[CANDIDATE] GATE 0 предлагает добавить минимальный Windows CI для точной версии
Python 3.14.6. До commit/push и GitHub read-back этот workflow имеет статус
`not invoked`; наличие YAML в candidate не является CI PASS.

## Что реализовано и где

| Файлы | Назначение |
| --- | --- |
| [contracts.py](../../musca/contracts.py), [bridge.py](../../musca/bridge.py) | Намерения, сроки, телеметрия и семантические события |
| [world.py](../../musca/world.py), [backends.py](../../musca/backends.py) | Локальные сенсоры, скриптовый нижний контроллер и защита тела |
| [simulation.py](../../musca/simulation.py), [interpretation.py](../../musca/interpretation.py) | Backend substitution, журнал, fail-closed обратная телеметрия и объяснение событий |
| [__main__.py](../../musca/__main__.py), [__init__.py](../../musca/__init__.py) | Терминальный запуск пакета |
| [puzzle.py](../../musca/puzzle.py), [test_puzzle.py](../../tests/test_puzzle.py) | Загадка «Шлюз», наблюдение, цена пробы, выбор и проверки |
| [test_contracts.py](../../tests/test_contracts.py), [test_simulation.py](../../tests/test_simulation.py), [test_cli.py](../../tests/test_cli.py) | Контрактные, интеграционные и process tests |
| [ADR-0002](../adr/ADR-0002-local-prototype-runtime.md), [ADR-0003](../adr/ADR-0003-bridge-contract.md) | Локальный runtime и Bridge boundary |
| [README](../../README.md), [charter](../PROJECT_CHARTER.md), [protocol](../RESEARCH_PROTOCOL.md), [vision](../GAME_VISION.md), [ADR-0001](../adr/ADR-0001-project-boundaries.md) | Архитектура, scientific boundary и game track |
| [ADR-0004](../adr/ADR-0004-terminal-gate-puzzle.md), [PLAYTEST_PROTOCOL](../PLAYTEST_PROTOCOL.md), [gate-p01.json](../../experiments/manifests/gate-p01.json) | Игровой пилот до сбора данных |
| [pilot_bundle.py](../../scripts/pilot_bundle.py), [test_bundle.py](../../tests/test_bundle.py), [PILOT_BUILD](../PILOT_BUILD.md) | Проверяемый локальный snapshot |

`AGENTS.md` остаётся операционным контрактом проекта. Шаблоны датасета и
эксперимента остаются шаблонами и не доказывают выполненный научный опыт.

## Проверка

**PASS — reference Windows surface:** Python 3.14.6, 75 тестов, exit 0. Свежий
Remote Desktop read-back также подтвердил clean working tree и `git diff --check`
без ошибок.

**UNVERIFIED — full cross-platform suite:** в отдельной Linux/container среде все
пять test modules прошли по отдельности (в сумме 75 тестов), однако общий
`unittest discover` не завершился внутри доступного execution window. Это не
доказанный дефект Linux, но и не основание объявлять Linux PASS.

**NOT RUN — GitHub CI для GATE 0 candidate:** до commit/push и run read-back.

## Научная и игровая граница

- [UNKNOWN] Биологическая гипотеза не проверена: `ConnectomeBackend` отсутствует.
- [UNKNOWN] Интерес игроков не проверен: GATE-P01 с добровольцами не запускался.
- Скриптовый контраст `light` vs `light_chemical` является инженерной фикстурой,
  а не доказательством biological advantage или fun.
- Научные зависимости и connectome dataset пока не закреплены в реальном experiment manifest.
- MMO, persistent world, экономика и большой multiplayer остаются вне текущего scope.

## Открытые GATE 0 вопросы

1. Авторизовать и применить маленький status/CI patch, затем получить GitHub
   workflow read-back.
2. Выбрать лицензионную политику отдельно: отсутствие `LICENSE` сейчас означает,
   что проект нельзя считать open-source только потому, что repository public.
3. После CI решить Linux combined-suite uncertainty, не выдавая module-wise smoke
   за полный cross-platform PASS.

## Следующие продуктовые/исследовательские шаги после GATE 0

1. Провести GATE-P01 как отдельный human playtest с заранее согласованными
   условиями участия и хранения результатов.
2. Создать первый pinned scientific reproduction manifest: конкретная публикация,
   dataset snapshot, environment, expected observation и falsifier.
3. Только после воспроизведения известного результата переходить к первому
   biological-vs-null MUSCA experiment.

∆ — статус приведён к свежему branch read-back и явно отделён от candidate CI.
D — GitHub + Remote Desktop + isolated artifact checks; runtime code не менялся.
Ω — высокий для Windows baseline и branch state; cross-platform/CI/science/game остаются bounded unknowns.
Λ — обновить после любого нового commit, workflow run, player dataset или scientific experiment.
