# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/локальных фактов всегда
предпочитать новый read-back этому документу.

Последние подтверждённые вехи:

- [GATE 0 / первый GitHub CI](2026-09-16-gate0.md);
- [scientific preregistration](2026-09-16-science-prereg.md);
- [manifest validator](2026-09-16-manifest-validator.md);
- [сборка игрового пилота](2026-09-16-pilot-build.md).

## Репозиторий

[FACT @ GitHub/Remote Desktop] `docs/musca-foundations` опубликована и синхронизирована
с origin. Последний substantive HEAD:
`357664106296e31e005ef9a7db57dc7a356764a2`.

[FACT] `main` остаётся на `7c56ff5da706894e07bb3b3aa85798304c4ae925`.
Draft PR #1 открыт и mergeable; merge не выполнен.

## Исполняемый стенд

[FACT] Dependency-free Python prototype содержит discrete world, scripted
`MuscaBackend`, отдельный Bridge, event interpretation, human choice и «Шлюз».
Биологической модели и внешнего LLM нет.

## Проверка

[FACT @ local verification] Windows/Python 3.14.6: `88 tests / PASS`, compile PASS,
manifest schema PASS и `git diff --check` PASS.

[FACT @ GitHub Actions] Для validator hardening commit `3576641...`:

- push run `35102476429` (#8): success;
- PR run `35102480304` (#9): success;
- unit tests, manifest-schema step, compile и deterministic smoke: success на обоих.

Full Linux/macOS suite остаётся `UNVERIFIED`, не FAIL и не PASS.

## Manifest gate

`scripts/validate_manifests.py` разделяет `schema` и `runnable`.
SCI dataset candidate, SCI-R01 и GATE-P01 проходят schema.
SCI-R01 и GATE-P01 остаются `draft_not_run` и обязаны fail-closed на runnable.

Runnable dataset требует существующий `local_storage`; `not_applicable` допустим
только с явной причиной. GATE-P01 приведён к текущей форме без изменения
гипотезы, assignments, метрик или success criteria.

## Science / governance

Science status: `PREREGISTERED / NOT RUN`.
ADR-0005 остаётся `PROPOSED`: Shiu/FlyWire-v630 только как первый strict
reproduction baseline; installation/download/run им не разрешены.

## Game / licensing

GATE-P01 human playtest: `NOT RUN`.
MMO, persistent world, economy и большой multiplayer остаются вне текущего scope.

Проектный `LICENSE` не выбран. `docs/LICENSING_DECISION.md` остаётся decision support;
public repository не трактуется как автоматически open-source.

## Следующие ворота

1. Commit/push этого status receipt и получить его CI read-back.
2. Draft PR #1 оставить review surface; merge — отдельное решение.
3. После явного принятия ADR-0005 отдельно открыть operational write-gate SCI-R00:
   isolated environment + pinned source/data + SHA-256 receipts.
4. Независимо провести GATE-P01 human playtest по preregistered protocol.

∆ — manifest discipline теперь исполняема и CI-проверяема.
D — 88 tests; validator runs #8/#9 green; draft PR #1 открыт.
Ω — высокий для repo/Windows provenance; science/game/cross-platform bounded unknown.
Λ — пересмотреть после status CI, ADR acceptance, player dataset или R00/R01 run.
