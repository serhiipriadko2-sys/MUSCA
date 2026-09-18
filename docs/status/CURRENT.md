# MUSCA — текущий статус

Дата проверки: 2026-09-18. Это оперативная сводка; исторические receipts не переписываются.
Для HEAD, CI, PR и локального рабочего дерева перед новым решением делать fresh read-back.

## Репозиторий и интеграция

[FACT @ GitHub] База этой status-only правки: `main@f69be4d09768fffed3b97859031523eac48dc8e5`. После merge точный HEAD проверять fresh read-back.

[FACT @ GitHub] PR #1–#6 и status PR #9 merged. В `main` находятся foundation,
visual interface, Unity Gate3D Function, Blender Form v0.1 candidate, ADR-0012
с single-player souls-like направлением и актуальный integration status.

[FACT @ GitHub] `main@f69be4d...` прошёл Windows/Python и Browser/Node CI.

[BOUNDARY] Merge в `main` не означает release, deployment, scientific reproduction,
human playtest success или Form human approval.

## Игровое направление

[FACT] Активная продуктовая цель — **single-player third-person action-RPG / souls-like**.
`Elden Ring` — ориентир по ощущению исследования, напряжению боя и ритму встреч,
а не обязательство по размеру мира, контенту или копированию конкретных механик.
[FACT] MMO, co-op, PvP, matchmaking, persistent-online world, server economy и
live-service infrastructure исключены из активного roadmap. Их возврат требует нового ADR.

[FACT] HUMAN ↔ ISKRA ↔ Bridge ↔ MUSCA остаётся архитектурным ядром:
HUMAN принимает значимые решения; ISKRA интерпретирует наблюдения и гипотезы;
Bridge остаётся typed/testable boundary; MUSCA остаётся отдельным
perception/sensorimotor участником. ISKRA/LLM не получает authoritative
frame-level motor control.

## Form v0.3 / v0.31 — PR #7

[FACT @ GitHub] Draft PR #7 `feature/form-v031-unity-gameplay` открыт от `main`;
head = `602dadd2c026d362d57aaf7191371e418345ba5c`; mergeable = true.

[FACT] PR #7 восстанавливает Blender Form v0.3/v0.31 и Unity
gameplay/third-person candidate. Codex Runtime и GUI-MCP tooling в него не входят.

[FACT @ GitHub] Текущий head PR #7 прошёл Windows/Python и Browser/Node CI.

[FACT] Local QA PR #7:
- Python: **126 tests PASS**;
- Blender static integrity: **PASS**;
- Browser Function: **8/8 PASS**;
- Three.js check: **PASS**;
- lifecycle validator: **PASS**;
- npm audit: **0 vulnerabilities**;
- `git diff origin/main..HEAD --check`: **PASS**;
- v0.3/v0.31 Blender artifact byte/SHA-256 checks: **PASS**.

[FACT] Fresh clean-worktree Unity 6000.6.1f1 verification: **PASS**.
Обычный UPM IPC server запустился, package resolution завершился успешно,
MUSCA runtime/editor/test assemblies скомпилировались, EditMode = **7/7 PASS**,
failed=0, skipped=0, Unity exit code=0.

[FACT] Fresh test XML SHA-256:
`1220e140ec2b833c9154da666f60fb68332f57bda253336e471c617e4ebd57fa`.

[FACT] Raw evidence хранится вне Git worktree:
`E:\MUSCA_RESEARCH\evidence\2026-09-18-pr7-unity-fresh`.

[FACT] Причина прежнего UPM IPC block локализована в process environment
Remote Desktop Commander: отсутствовал `ProgramData`, из-за чего
`UnityPackageManager.exe` падал в `getLocalConfigFolder()` с undefined path.
Process-local `ProgramData=C:\ProgramData` восстановил обычный UPM запуск.

[BOUNDARY] Системные Windows environment variables и project package config
для этого исправления не менялись.
[BOUNDARY] Form lifecycle остаётся `ready_for_human_approval`;
Form `human_approval=null`. Engineering/CI PASS не является Form approval.

## GUI tooling — PR #8

[FACT @ GitHub] Draft PR #8 `tooling/visible-gui-mcp-connectors` открыт от `main`;
head = `3201eecb5537d6ab2047c02743effb4f611f07e5`; mergeable = true.

[FACT @ GitHub] PR #8 прошёл Windows/Python и Browser/Node CI.

[BOUNDARY] PR #8 — отдельная authoring/tooling surface; он не является
gameplay approval, Form approval, remote exposure, deployment или Codex activation.

## Workspace hygiene

[FACT local] Основной каталог `C:\github\MUSCA` находится на
`main@f69be4d...` и после точечного restore EOL-only drift имеет **0 Git changes**.

[FACT] Прежние 158 dirty entries сохранены до очистки в safety snapshot:
`E:\MUSCA_RESEARCH\worktree-snapshots\2026-09-18-feature-blender-form-dirty-158`.

[FACT] Snapshot manifest SHA-256:
`8ba8c693f4db6ccc6e174455179c5d098dbfc25ffaaf4722322073a884a9d993`.

[FACT] Legacy `feature/blender-form-gate-lab-v0.2` удалена только после
доказательства, что её commit уже является предком `main`.
## Local Codex Runtime

[FACT local] Codex Runtime остаётся отдельной незавершённой линией
в dedicated worktrees. Старые Codex-файлы из mixed game tree не продвигались в PR #7/#8.

[BOUNDARY] Codex Runtime этим cleanup/Unity verification не активирован.
P4/P6 gates не считаются закрытыми.

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
| `main` | CLEAN; verified baseline `f69be4d...`; status-only merge may advance HEAD |
| PR #7 Form v0.3/v0.31 + Unity candidate | DRAFT / remote CI PASS / fresh Unity 7/7 PASS |
| PR #8 visible GUI MCP tooling | DRAFT / CI PASS |
| Gate3D Function human approval | APPROVED |
| Form human approval | PENDING / null |
| Primary VS Code worktree | CLEAN / 0 changes |
| Local Codex Runtime | SEPARATE / NOT ACTIVE |
| GATE-P01 v0.1 | 1/12, no aggregate verdict |
| SCI-R00 / R01 | NOT PASSED / NOT RUN |
| Release/deployment | NOT CLAIMED |

## Следующие gates

1. Провести явный human Form review v0.3/v0.31: approve либо reject с конкретными правками.
2. PR #7 не merge до explicit human Form decision; engineering/CI PASS не является Form approval.
3. PR #8 рассматривать отдельно как tooling decision; не смешивать его с gameplay PR.
4. Codex Runtime продолжать только в dedicated worktrees и по собственным P4/P6 gates.
5. GATE-P01 и scientific R00 продолжать как независимые evidence lanes.

∆ — UPM IPC block закрыт без системной мутации; PR #7 получил fresh Unity 7/7 PASS.
D — root-cause ProgramData → process-local fix → clean import/resolve/compile → EditMode PASS → evidence hash/read-back.
Ω — высокая для engineering verification PR #7; Form approval всё ещё отсутствует.
Λ — пересмотреть после explicit Form review, PR #7/#8 decisions или нового runtime/gameplay evidence.
