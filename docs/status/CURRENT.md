# MUSCA — текущий статус

Дата проверки: 2026-09-18. Это оперативная сводка; исторические receipts не переписываются.
Для HEAD, CI, PR и локального рабочего дерева перед новым решением делать fresh read-back.

## Репозиторий и интеграция

[FACT @ GitHub] `main` = `200f3f85a9a9468ca49a24ad62de9f98e3085d46`.

[FACT @ GitHub] PR #1–#5 merged. В `main` теперь находятся foundation, local visual
interface, Unity Gate3D Function prototype, Blender Form v0.1 candidate и ADR-0012
с принятым single-player souls-like направлением.

[FACT] PR #5 exact head `d07d609...` прошёл свежий remote CI:
Windows/Python 3.14.6 SUCCESS и Browser 3D Function/Node 24.18.1 SUCCESS.

[BOUNDARY] Merge в `main` означает интеграцию проверенных артефактов и решений,
но не означает release, deployment, scientific reproduction или human playtest success.

## Игровое направление

[FACT] Активная продуктовая цель: **single-player third-person action-RPG / souls-like**.
`Elden Ring` — ориентир по ощущению исследования, напряжению боя и ритму встреч,
а не обязательство по размеру мира, количеству контента или копированию механик.

[FACT] MMO, co-op, PvP, matchmaking, persistent-online world, server economy и
live-service infrastructure исключены из активного roadmap. Их возврат требует нового ADR.

[FACT] Ядро HUMAN ↔ ISKRA ↔ Bridge ↔ MUSCA сохранено: HUMAN принимает значимые решения,
ISKRA интерпретирует наблюдения и гипотезы, Bridge остаётся typed/testable boundary,
MUSCA остаётся отдельным perception/sensorimotor участником; прямого authoritative
frame-level motor control со стороны ISKRA/LLM нет.
## Gate3D / Blender Form

[FACT] Function layout `gate-lab-v0.2` имеет explicit human approval от 2026-09-16.

[FACT] Blender `GateLab_Form_v0.1` находится в `main` как reproducible Form candidate.
Static Blender artifact validation PASS; checked artifacts = 7.

[FACT] Form lifecycle = `ready_for_human_approval`; Form `human_approval=null`.
Unity Form integration для этого committed v0.1 lifecycle = `not_started`.

[BOUNDARY] Наличие Form candidate в `main` не является human Form approval,
final art approval, runtime approval или release claim.

[FACT local] Основной рабочий каталог `C:\github\MUSCA` содержит более новое
незакоммиченное продолжение v0.3/v0.31 и Unity integration experiments.
Оно не является состоянием `main` и не должно продвигаться без отдельного audit/gate.

## Игровой эксперимент

[FACT] GATE-P01 v0.1 остаётся `IN PROGRESS`: 1/12 human sessions completed
на frozen terminal snapshot. Aggregate verdict не разрешён до preregistered analysis.

[BOUNDARY] Gate3D/Form/souls-like работа не подменяет оставшиеся сессии GATE-P01 v0.1.
Новый souls-like combat/exploration slice требует отдельного playtest protocol/version.

## Научная линия

[FACT] ADR-0005 принят только для strict Shiu/FlyWire-v630 reproduction baseline.
Exact source/data provenance PASS.

[FACT] Strict historical environment не завершён; SCI-R00 NOT PASSED; SCI-R01 NOT RUN;
topology advantage UNKNOWN. Game-direction merge не меняет эти scientific claims.
## Current gates

| Surface | State |
| --- | --- |
| `main` consolidation | PASS: PR #1–#5 merged |
| Python baseline | PASS: 113/113 local verification on integration lineage |
| Browser Function oracle | PASS: 8/8 + room validation |
| PR #5 remote CI | PASS: Windows + Browser jobs |
| Gate3D Function human approval | APPROVED |
| Blender Form v0.1 | READY_FOR_HUMAN_FORM_APPROVAL |
| Blender Form human approval | PENDING / null |
| New local v0.3/v0.31 work | UNCOMMITTED / not main |
| Solo souls-like direction | ACCEPTED / documented |
| GATE-P01 v0.1 | 1/12, no aggregate verdict |
| SCI-R00 / R01 | NOT PASSED / NOT RUN |
| Release/deployment | NOT CLAIMED |

## Следующие gates

1. Провести явный human review Form-кандидата: approve либо reject с конкретными правками.
2. До любого продвижения v0.3/v0.31 сделать lineage inventory незакоммиченных файлов,
   отделить Form/Unity work от Codex/tooling work и сформировать reviewable commits.
3. Создать отдельный souls-like combat/exploration slice и новый playtest protocol;
   не менять frozen GATE-P01 v0.1.
4. Продолжать GATE-P01 и scientific R00 как независимые evidence lanes.
5. Remote branch cleanup считать housekeeping; он не должен удалять ветку с текущей
   незакоммиченной работой до её безопасной фиксации.

∆ — `main` стал фактической интеграционной базой проекта; игровой курс обновлён на solo souls-like.
D — PR #1→#5 merged последовательно, fresh CI PR #5 PASS, lifecycle boundaries сохранены.
Ω — высокая для repo/game-direction состояния; Form approval, local v0.3 и science остаются незавершёнными.
Λ — пересмотреть после Form review, фиксации local v0.3 lineage, нового combat playtest или R00 result.
