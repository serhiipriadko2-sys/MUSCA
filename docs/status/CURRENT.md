# MUSCA — текущий статус

Дата свежей проверки: 2026-09-16. Для изменяемых GitHub/локальных фактов
предпочитать новый connector/read-back этому документу.

Последние подтверждённые вехи:

- [GATE 0 / первый GitHub CI](2026-09-16-gate0.md);
- [scientific preregistration](2026-09-16-science-prereg.md);
- [manifest validator](2026-09-16-manifest-validator.md);
- [SCI-R00 read-only preflight](2026-09-16-r00-preflight.md);
- [concurrent operational drift / containment](2026-09-16-concurrent-ops-drift.md).

## Репозиторий

[FACT @ GitHub/Remote Desktop] `docs/musca-foundations` опубликована и origin
сейчас указывает на `b42c125eeedd4e26b2cc6d5dcdf090e60077d4ec`.
`main` остаётся на `7c56ff5da706894e07bb3b3aa85798304c4ae925`.
Draft PR #1 открыт; merge в `main` не выполнен.

[FACT @ GitHub Actions] Для `b42c125...` push run #16 и PR run #17 завершились
`success`: tests, manifest schema, solver-gate, compile и deterministic smoke PASS.
CI подтверждает техническое состояние commit, а не человеческий consent.

## Исполняемый стенд

[FACT] Dependency-free Python prototype содержит discrete world, scripted
`MuscaBackend`, отдельный Bridge, event interpretation, human choice и «Шлюз».
Биологическая модель и внешний LLM в MUSCA runtime не реализованы.

## Проверка

[FACT @ local verification] На текущем коде Windows/Python 3.14.6:

- 98 unit tests PASS;
- registered dataset/R00/R01/GATE-P01 schemas PASS;
- R00 preflight tests PASS;
- compile и `git diff --check` проходили до containment status edit.

Full Linux/macOS suite остаётся `UNVERIFIED`, не FAIL и не PASS.

## Governance conflict

[FACT] ADR-0005 в commit `b42c125...` имеет lifecycle `accepted` и ограничивает
решение первым strict Shiu/FlyWire-v630 reproduction baseline.

[UNKNOWN] Доказуемый human/project-authority consent для этого перехода не найден
в текущем чате или PR #1 comments/reviews. GitHub author identity не закрывает
этот вопрос, потому что connected agent writes выполняются от того же аккаунта.

Поэтому текущая формулировка:

`ADR FILE = accepted / acceptance provenance = UNKNOWN`.

Это не автоматическое разрешение на новые install/download/simulation actions.

## Live R00 operational state

[FACT] Параллельная tool activity создала `E:\MUSCA_RESEARCH` и скачала
portable micromamba. Текущий containment receipt фиксирует solver bytes/hash.

[FACT] На диске также остались незавершённые upstream transfers:

- partial Git object store без valid `HEAD` и без working tree;
- `Drosophila_brain_model-91bdd1e7.zip` — `BadZipFile`;
- `Drosophila_brain_model-91bdd1e7.full.part` — `BadZipFile`;
- `source-snapshot` отсутствует;
- `mamba-root`/strict environment отсутствует.

Активные downloader sessions/processes, обнаруженные во время containment,
остановлены. Частичные bytes не удалялись и не ремонтировались.

Science status остаётся:

`PREREGISTERED / R00 NOT MATERIALIZED / R01 NOT RUN`.

Наличие solver binary или partial source bytes не является scientific execution.

## Preflight semantics

`scripts/research_r00_preflight.py` остаётся полезным read-only gate, но incident
выявил два ограничения, которые нельзя скрывать:

1. `writes/downloads/installs/simulations = 0` означает только self-effects
   процесса preflight, а не отсутствие preceding writes в общей сессии;
2. solver discovery через PATH может сообщить `host_solver`, даже когда
   project-local `E:\MUSCA_RESEARCH\tools\micromamba.exe` уже существует.

До исправления этих semantics preflight нельзя использовать как единственную
квитанцию operational no-write/solver absence.

## Game / licensing

GATE-P01 human playtest: `NOT RUN`.
MMO, persistent world, economy и большой multiplayer остаются вне текущего scope.
Проектный `LICENSE` не выбран; data/code/game-asset rights не смешиваются.

Локальный `docs/research/R00_SOLVER_DECISION_CANDIDATE.md` пока не входит в Git:
его текущие frozen facts устарели после concurrent operational writes и требуют
пересборки перед возможной публикацией.

## Следующие безопасные ворота

1. Закоммитить containment/status-only repair и получить GitHub CI read-back.
2. Не продолжать upstream download/environment creation до разрешения governance conflict.
3. Исправить preflight receipt semantics и покрыть regression tests.
4. Отдельно решить судьбу уже созданных solver/partial-source artifacts.
5. После явного operational approval заново стартовать R00 из чистого, записанного состояния.
6. Независимо провести GATE-P01 human playtest по preregistered protocol.

∆ — active concurrent R00 writes остановлены; реальное host-state отделено от preregistration/CI claims.
D — process containment → file hashes → ZIP validity → repo/ADR authority audit.
Ω — высокий для текущего filesystem/process/repo state; human intent behind ADR acceptance = UNKNOWN.
Λ — пересмотреть после explicit authority signal, artifact-disposition decision или нового verified R00 gate.
