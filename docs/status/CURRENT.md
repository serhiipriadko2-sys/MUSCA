# MUSCA — текущий статус

Дата проверки: 2026-09-16. Это оперативная сводка, а не вечная истина.
Для HEAD, CI, PR и состояния рабочего дерева перед решением всегда делать fresh
GitHub/Remote Desktop read-back; исторические receipts ниже не переписываются.

## Что проверено

[FACT] Последний полностью проверенный кодовый снимок перед этим hardening diff —
`c46d814e3692c5a6330f803341e9a62d80794c73` на `docs/musca-foundations`.
На нём локально Windows/Python 3.14.6: **105 unit tests PASS**, `git diff --check`
PASS, compile PASS, четыре manifest schema PASS; dataset runnable PASS; SCI-R00
ожидаемо отклоняется как `draft_not_run`.

[FACT @ GitHub] Для exact `c46d814...` push CI #22 и PR CI #23 завершились
`success`. Draft PR #1 открыт и не слит; `main` остаётся отдельной веткой.
Green CI не означает review, merge, deployment, human pilot или neuroscience result.

## Исследовательская линия

[FACT] ADR-0005 имеет статус `accepted` только для первого strict reproduction
baseline Shiu/FlyWire-v630. Это не выбор будущего игрового neural backend.

[FACT] Exact upstream snapshot
`philshiu/Drosophila_brain_model@91bdd1e7dcf193f3e7ca5a8933497fcef63b7960`
получен локально и прошёл byte/QC verification. Четыре preregistered Git blob ID
совпали с вычисленными локально. Полные SHA-256 и размеры — в
`2026-09-16-r00-source-provenance.md`.

[FACT] Project-local micromamba 2.9.0 отвечает на version probe; real-host
preflight возвращает `READY_FOR_OPERATIONAL_APPROVAL`, source snapshot present.
Это readiness gate, не R00 PASS. Strict `environment_full.yml` не solved/installed;
R00 не пройден, R01 не запускался. Каналы и build pins не подменялись.

[BOUNDARY] Историческое окружение содержит старые Python/OpenSSL builds и должно
оставаться изолированной reproduction surface, не production/network runtime.
Контекст применимости доступа к `defaults` нужно установить до strict solve.

## Игровая линия

[FACT] GATE-P01 v0.1 подготовлен до данных: 12 fixed assignments, 0 human sessions.
Автотесты и developer checks не являются playtest evidence.

[FACT] `gate-p01-build-v2.zip` имеет SHA-256
`f70981ac197203ad21effcf6e9ba3ab4c620a3df72a9b1315ee8abf2ae91ae2a`,
55 payload files и integrity PASS. Fresh comparison показал 55/55 payload hashes
равными clean `c46d814...`. Это фиксированный pilot snapshot, даже если ветка позже
получит docs/CI hardening commits.

[BOUNDARY] v2 — facilitator/developer archive: в нём есть source и spoiler-bearing
ADR. Участнику его не выдавать. Self-run/remote pilot требует отдельного sanitized
participant package или считается нарушением blinding.

## Инженерная поверхность

[FACT] Терминальный MUSCA runtime остаётся stdlib-only Python prototype: внешний LLM,
connectome backend и biological dynamics в игровой runtime не подключены.

[FACT] На текущем Windows host Unity/UnityHub, Blender и `game-dev` CLI не обнаружены;
MUSCA не содержит Unity project markers. Поэтому Unity/3D — будущий implementation
gate, а не уже существующая часть проекта.

[FACT] CI использует только `contents: read`. В этом hardening diff official GitHub
Actions переводятся с mutable major tags на full immutable commit SHAs и добавляется
CI self-test pilot bundle tooling. Этот diff должен получить собственный CI read-back.

## Текущие границы вывода

| Поверхность | Состояние |
| --- | --- |
| Python prototype / tests | PASS на verified `c46d814...` |
| Exact c46 GitHub CI | PASS, push #22 + PR #23 |
| Independent PR review | NOT DONE |
| Pilot build v2 bytes | PASS, 55/55 = c46 payload |
| Human GATE-P01 | NOT RUN, 0 sessions |
| Shiu source/data provenance | PASS |
| Strict Shiu environment | NOT SOLVED |
| SCI-R00 / SCI-R01 | NOT PASSED / NOT RUN |
| Topology advantage | UNKNOWN |
| Unity/3D implementation | NOT STARTED |
| Merge / deployment | NOT DONE |

## Следующие gates

1. Получить CI для hardening commit и независимый review draft PR #1.
2. Провести facilitator-owned GATE-P01 по frozen v2 snapshot; не раздавать source ZIP.
3. Отдельно разрешить repository-access premise и выполнить strict R00; failure
   сохранить как результат, а compatibility lineage именовать отдельно.
4. Только после продуктового или научного discriminating signal принимать ADR о Unity
   shell / embodied backend. Shiu reproduction baseline не наследуется автоматически.

∆ — mutable status отделён от исторических receipts; claim boundaries уточнены.
D — fresh GitHub/DC read-back, 105 tests, manifest gates, artifact hash comparison.
Ω — высокая для перечисленных инженерных фактов; science/product value UNKNOWN.
Λ — пересмотреть при новом HEAD/CI, review, первой human session или R00/R01 result.
