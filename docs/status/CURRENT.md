# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/локальных фактов всегда
предпочитать новый read-back этому документу.

Последние подтверждённые вехи:

- [GATE 0 / первый GitHub CI](2026-09-16-gate0.md);
- [scientific preregistration](2026-09-16-science-prereg.md);
- [сборка игрового пилота](2026-09-16-pilot-build.md);
- [игровой этап «Шлюз»](2026-09-16-gate.md).

## Репозиторий

[FACT @ GitHub/Remote Desktop] Рабочая ветка `docs/musca-foundations` синхронизирована
с origin. Опубликованный HEAD перед manifest-validator commit:
`3f54cb3dc7cdfde74ab3f796ddbb076ef38c7e2c`.

[FACT] `main` остаётся на `7c56ff5da706894e07bb3b3aa85798304c4ae925`.
Draft PR #1 `Bootstrap MUSCA research and game foundation` открыт против `main`;
merge не выполнен.

## Текущая стадия

[FACT] Реализован dependency-free Python стенд: дискретный мир, скриптовый
`MuscaBackend`, отдельный Bridge, интерпретатор событий, терминальный human choice
и загадка «Шлюз». Биологической модели и внешнего LLM нет.

## CI и проверка

[FACT @ GitHub Actions] Четыре опубликованных push-runs завершили job
`Windows / Python 3.14.6` успешно:

- run `35098073671` (#1), commit `4280963...`;
- run `35098789752` (#2), commit `67c95259...`;
- run `35099417750` (#3), commit `089553ab...`;
- run `35099842609` (#4), commit `3f54cb3...`.

На каждом run unit-test, compile и deterministic-smoke steps завершились `success`.

[FACT @ local verification] После добавления manifest validator локальный suite:
`86 tests / PASS`. Compile и `git diff --check` также PASS.

**UNVERIFIED — full cross-platform suite:** в отдельной Linux/container среде
предыдущие 75 тестов прошли по модулям, но общий `unittest discover` не завершился
в доступном execution window. Это не Linux FAIL и не Linux PASS.

## Manifest validation

`scripts/validate_manifests.py` разделяет два уровня:

- `schema` — обязательные поля, типы и непустые значения;
- `runnable` — дополнительно fail-closed блокирует draft, незакреплённый код,
  неполученные данные и linked dataset без runnable provenance.

[FACT @ local verification] SCI dataset candidate, SCI-R01 и GATE-P01 проходят
`schema`. SCI-R01 и GATE-P01 обязаны завершаться FAIL на `runnable`, пока их
статус `draft_not_run`. Это ожидаемый safety gate.

GATE-P01 приведён к текущей manifest-форме: добавлены `validation_worlds`,
`trainable_parameters` и явное `dataset_manifest=not_applicable`; гипотеза,
метрики, assignments и success criteria не изменены.

## Scientific lineage

Статус: `PREREGISTERED / NOT RUN`.

Опубликованы R00/R01 plan, dataset candidate и experiment candidate. Они фиксируют
Shiu upstream commit, FlyWire-v630 lineage, original-environment gate,
`150 Hz executable code vs 200 Hz tutorial prose` drift, falsifier и claim ceiling.

[PROPOSED] ADR-0005 выбирает Shiu/FlyWire-v630 только для первой strict
reproduction lineage. Он не выбирает production/game controller и не разрешает
installation, download или experiment run. До project-authority acceptance он
остаётся `proposed`.

## Game lineage

GATE-P01 с добровольцами: `NOT RUN`. Скриптовый `light` vs `light_chemical` —
инженерная фикстура, не доказательство biological advantage и не доказательство fun.
MMO, persistent world, экономика и большой multiplayer остаются вне текущего scope.

## Лицензирование

Проектный `LICENSE` не выбран. `docs/LICENSING_DECISION.md` остаётся decision support.
Public repository не трактуется как автоматически open-source; лицензии собственного
кода, third-party code, connectome/data и будущих игровых assets разделяются.

## Следующие ворота

1. Commit/push manifest validator и получить GitHub CI read-back нового HEAD.
2. Обновить draft PR #1 результатом validator CI; merge оставить отдельным решением.
3. После явного принятия ADR-0005 отдельно открыть operational write-gate SCI-R00:
   isolated environment + pinned source/data + SHA-256 receipts.
4. Независимо провести GATE-P01 human playtest по preregistered protocol.

∆ — manifests получили исполняемый fail-closed gate; научный результат не повышен.
D — validator candidate + 86 локальных тестов + четыре зелёных GitHub CI runs.
Ω — высокий для Windows/repo provenance; science/game/cross-platform bounded unknown.
Λ — пересмотреть после validator CI, ADR acceptance, player dataset или R00/R01 run.
