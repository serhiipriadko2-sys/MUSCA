# MUSCA — текущий статус

Дата проверки: 2026-09-18. Это оперативная сводка; исторические receipts не переписываются.
Для HEAD, CI, PR и локального рабочего дерева перед новым решением делать fresh read-back.

## Репозиторий и интеграция

[FACT @ GitHub] `main` = `ab9a8c14b19087f27c2da5b60f984dc359cd1a9d`.

[FACT @ GitHub] PR #1–#6 merged. В `main` находятся foundation, visual interface,
Unity Gate3D Function, Blender Form v0.1 candidate, ADR-0012 solo souls-like direction
и актуализированный integration status.

[FACT @ GitHub] Итоговый `main@ab9a8c1...` прошёл Windows/Python и Browser/Node CI.

[BOUNDARY] Merge в `main` не означает release, deployment, scientific reproduction,
human playtest success или Form human approval.

## Игровое направление

[FACT] Активная продуктовая цель: **single-player third-person action-RPG / souls-like**.
`Elden Ring` — ориентир по ощущению исследования, напряжению боя и ритму встреч,
а не обязательство по размеру мира, контенту или копированию конкретных механик.

[FACT] MMO, co-op, PvP, matchmaking, persistent-online world, server economy и
live-service infrastructure исключены из активного roadmap. Их возврат требует нового ADR.

[FACT] HUMAN ↔ ISKRA ↔ Bridge ↔ MUSCA остаётся архитектурным ядром. HUMAN принимает
значимые решения; ISKRA интерпретирует наблюдения и гипотезы; Bridge остаётся typed/testable;
MUSCA остаётся отдельным perception/sensorimotor участником. ISKRA/LLM не получает
authoritative frame-level motor control.

## Form v0.3 / v0.31 recovery lane

[FACT @ GitHub] Draft PR #7 `feature/form-v031-unity-gameplay` открыт от `main`;
head = `abb6f5c2841705d16e70b1ac2693d8222323d1ed`; mergeable = true.

[FACT] PR #7 восстанавливает Blender Form v0.3/v0.31 и Unity gameplay/third-person
candidate из прежнего смешанного локального рабочего дерева. Он не включает Codex runtime
или GUI-MCP tooling.

[FACT] PR #7 remote CI: Windows/Python SUCCESS и Browser/Node SUCCESS.

[FACT] Local QA для PR #7: Python 126 tests PASS; Blender static integrity PASS;
Browser Function 8/8 PASS; Three.js check PASS; lifecycle validator PASS; npm audit
0 vulnerabilities; diff check PASS.

[FACT] v0.3/v0.31 Blender artifacts совпали со своими receipt byte/SHA-256 значениями.

[WARN] Fresh Unity batchmode EditMode run на clean PR #7 worktree не завершился:
Unity Package Manager не открыл local IPC stream за 30 секунд. Нового clean-branch
EditMode XML нет. Это environment/toolchain block, а не новый Unity test PASS или code FAIL.

[FACT] Более ранние v0.3/v0.31 Unity receipts фиксировали compile errors = 0 и 7/7
EditMode tests, но остаются historical evidence.

[BOUNDARY] Form lifecycle остаётся `ready_for_human_approval`;
Form `human_approval=null`. PR #7 не должен молча повышать Form до approved.

## GUI tooling lane

[FACT @ GitHub] Draft PR #8 `tooling/visible-gui-mcp-connectors` открыт от `main`;
head = `3201eecb5537d6ab2047c02743effb4f611f07e5`; mergeable = true.

[FACT] PR #8 содержит только ADR-0010 и pinned Unity MCP package/lock changes.
Remote CI: Windows/Python SUCCESS и Browser/Node SUCCESS.

[FACT local] В авторизованном исходном Unity project package cache наблюдался
`com.coplaydev.unity-mcp` version `10.2.0` и соответствующие runtime/editor assemblies.

[BOUNDARY] PR #8 — authoring/tooling surface; он не является gameplay approval,
Form approval, remote exposure, deployment или Codex runtime activation.

## Workspace hygiene

[FACT local] Основной каталог `C:\github\MUSCA` теперь находится на `main@ab9a8c1...`
и имеет **0 Source Control changes**.

[FACT] Прежние 158 dirty entries были до очистки сохранены в external local snapshot:
`E:\MUSCA_RESEARCH\worktree-snapshots\2026-09-18-feature-blender-form-dirty-158`.

[FACT] Snapshot manifest SHA-256:
`8ba8c693f4db6ccc6e174455179c5d098dbfc25ffaaf4722322073a884a9d993`.

[FACT] Legacy remote/local branch `feature/blender-form-gate-lab-v0.2` удалена только
после доказательства, что её commit уже является предком `main`, а новая v0.3/v0.31
линия сохранена в PR #7.

[FACT] Twenty Unity PNG QA screenshots из старого dirty tree имели 0 runtime asset GUID
references; они не добавлены в PR #7 и сохранены в safety snapshot.

## Local Codex Runtime

[FACT local] Codex Runtime остаётся отдельной незавершённой линией в dedicated worktrees.
Старые копии Codex-файлов из смешанного game tree не продвигались в PR #7/#8.

[BOUNDARY] Codex runtime не активирован этим cleanup. P4/P6 gates не считаются закрытыми.

## Игровой эксперимент и science

[FACT] GATE-P01 v0.1 остаётся `IN PROGRESS`: 1/12 human sessions completed.
Aggregate verdict до preregistered analysis не разрешён.

[BOUNDARY] Gate3D/Form/souls-like work не подменяет frozen GATE-P01 v0.1.

[FACT] ADR-0005 принят только для strict Shiu/FlyWire-v630 reproduction baseline.
Strict historical environment не завершён; SCI-R00 NOT PASSED; SCI-R01 NOT RUN;
topology advantage UNKNOWN.

## Current gates

| Surface | State |
| --- | --- |
| `main` | CLEAN / integrated at `ab9a8c1...` |
| PR #7 Form v0.3/v0.31 + Unity candidate | DRAFT / CI PASS / fresh Unity engine rerun BLOCKED by UPM IPC |
| PR #8 visible GUI MCP tooling | DRAFT / CI PASS |
| Gate3D Function human approval | APPROVED |
| Form human approval | PENDING / null |
| Primary VS Code worktree | CLEAN / 0 changes |
| Local Codex Runtime | SEPARATE / NOT ACTIVE |
| GATE-P01 v0.1 | 1/12, no aggregate verdict |
| SCI-R00 / R01 | NOT PASSED / NOT RUN |
| Release/deployment | NOT CLAIMED |

## Следующие gates

1. Для PR #7 добиться fresh clean-worktree Unity compile/EditMode verification после
   устранения UPM IPC block; затем провести явный human Form review.
2. PR #7 не merge до завершения соответствующего gate; CI alone недостаточно.
3. PR #8 рассматривать отдельно как tooling decision; не смешивать его с gameplay PR.
4. Codex Runtime продолжать только в dedicated worktrees и по собственным P4/P6 gates.
5. GATE-P01 и scientific R00 продолжать как независимые evidence lanes.

∆ — 158 смешанных local changes разобраны на game/Form, GUI-tooling, Codex и snapshot-only evidence.
D — external snapshot → clean branches → PR #7/#8 → remote CI PASS → primary worktree cleaned to main.
Ω — высокая для repository hygiene и сохранности bytes; current Unity clean-branch engine verification остаётся неполной.
Λ — пересмотреть после clean Unity rerun, explicit Form review, PR #7/#8 decisions или Codex P4/P6 evidence.
