# MUSCA — текущий статус

Дата проверки: 2026-09-18. Это оперативная сводка; исторические receipts не переписываются.
Перед новым решением по HEAD, CI, PR или локальному workspace делать fresh read-back.

## Репозиторий и интеграция

[FACT @ GitHub] Проверенный integration baseline перед этой status-only правкой:
`main@988088c20442a1db97fbc4a93c2d5e0c317f96dc`.

[FACT @ GitHub] PR #7 (Form v0.3/v0.31 + Unity gameplay candidate) и PR #8
(visible Unity/Blender MCP tooling) merged. Открытых PR после merge #7 нет.

[FACT @ GitHub] Remote feature-ветка PR #7 удалена после проверки, что её head является
предком `main`. На remote остаётся только `main`.

[FACT @ GitHub] Merge commit `988088c...` прошёл свежий CI:
Windows/Python 3.14.6 SUCCESS и Browser 3D Function/Node 24.18.1 SUCCESS.

[BOUNDARY] Merge означает интеграцию проверенного прототипа. Он не означает release,
deployment, scientific validation или финальное художественное качество.

## Игровое направление

[FACT] Активная продуктовая цель — **single-player third-person action-RPG / souls-like**.
`Elden Ring` — ориентир по ощущению исследования, напряжению боя и ритму встреч,
а не обязательство по масштабу мира или копированию конкретных механик.

[FACT] MMO, co-op, PvP, matchmaking, persistent-online world, server economy и
live-service infrastructure исключены из активного roadmap. Их возврат требует нового ADR.

[FACT] HUMAN ↔ ISKRA ↔ Bridge ↔ MUSCA остаётся архитектурным ядром.
HUMAN принимает значимые решения; ISKRA интерпретирует наблюдения и гипотезы;
Bridge остаётся typed/testable boundary; MUSCA остаётся отдельным
perception/sensorimotor участником. ISKRA/LLM не получает authoritative
frame-level motor control.

## Form v0.3 / v0.31 и Unity

[FACT] Form v0.31 получил explicit human approval 2026-09-18 со scope:
`GateLab Form v0.31 prototype only`.

[FACT] Authoritative Form lifecycle:
- `form.status = approved`;
- `approval_kind = prototype_form_only`;
- `unity_form_integration = prototype_verified`.

[FACT] В `main` интегрированы Blender v0.3/v0.31 artifacts/exports, Unity materials,
preview/playable scenes, third-person presentation, Form gate bridge и MUSCA behavior candidate.

[FACT] QA перед merge и после синхронизации с текущим main:
- Python: **128 tests PASS**;
- Blender static integrity: **PASS**;
- Browser Function oracle: **8/8 PASS**;
- Three.js check: **PASS**;
- room lifecycle validator: **PASS**;
- npm audit: **0 vulnerabilities**;
- fresh Unity 6000.6.1f1 import/compile: **PASS**;
- ordinary UPM resolution: **PASS**;
- fresh Unity EditMode: **7/7 PASS**, failed=0, skipped=0.

[FACT] Post-main-sync Unity test XML SHA-256:
`92ee06865368841e8d6098332667c8e46857061257440fd314b666ffdb1fcd54`.

[FACT] Raw evidence:
`E:\MUSCA_RESEARCH\evidence\2026-09-18-form-v031-human-approval\post-main-merge-unity`.

[BOUNDARY] Prototype Form approval does **not** approve final production art,
final character design, animation/VFX/audio, final camera/combat feel,
runtime/gameplay human approval, release readiness or deployment.

[FACT] Runtime state remains engineering-verified while runtime/gameplay
`human_approval = null`.

## GUI tooling

[FACT] ADR-0010 / PR #8 merged. Unity MCP dependency is pinned and available as
an authoring/tooling surface.

[BOUNDARY] GUI tooling merge does not activate Codex Runtime, approve gameplay,
permit remote exposure, or make tooling part of shipped gameplay runtime.

## Workspace hygiene

[FACT local] Основной каталог `C:\github\MUSCA` синхронизирован с merged main
перед этой status-only правкой.

[FACT] Отдельный `MUSCA-form-v031` worktree удалён после merge. Локальная divergent
ветка была удалена только после проверки: против merged remote candidate отличался
один файл лишь EOL-представлением; `git diff --ignore-space-at-eol` = PASS.

[FACT] Прежние 158 dirty entries сохранены в safety snapshot:
`E:\MUSCA_RESEARCH\worktree-snapshots\2026-09-18-feature-blender-form-dirty-158`.

[FACT] Snapshot manifest SHA-256:
`8ba8c693f4db6ccc6e174455179c5d098dbfc25ffaaf4722322073a884a9d993`.

## Local Codex Runtime

[FACT local] Codex Runtime остаётся отдельной незавершённой линией в dedicated worktrees.

[BOUNDARY] P4/P6 gates не закрыты; Runtime не активирован этим Form/gameplay merge.

## Игровой эксперимент и science

[FACT] GATE-P01 v0.1 остаётся `IN PROGRESS`: 1/12 human sessions completed.
Aggregate verdict до preregistered analysis не разрешён.

[BOUNDARY] Souls-like / Form gameplay не подменяет frozen GATE-P01 v0.1.

[FACT] ADR-0005 принят только для strict Shiu/FlyWire-v630 reproduction baseline.
SCI-R00 NOT PASSED; SCI-R01 NOT RUN; topology advantage UNKNOWN.

## Current gates

| Surface | State |
| --- | --- |
| `main` | INTEGRATED: Form/Unity PR #7 + tooling PR #8 |
| Remote PRs | 0 open |
| Remote feature branches | 0; `main` only |
| Gate3D Function human approval | APPROVED |
| Form v0.31 human approval | APPROVED — PROTOTYPE ONLY |
| Unity Form integration | PROTOTYPE VERIFIED |
| Runtime/gameplay human approval | PENDING / null |
| Final production art | NOT APPROVED |
| Local Codex Runtime | SEPARATE / NOT ACTIVE |
| GATE-P01 v0.1 | 1/12, no aggregate verdict |
| SCI-R00 / R01 | NOT PASSED / NOT RUN |
| Release/deployment | NOT CLAIMED |

## Следующие gates

1. Перейти от Form gate к следующему игровому milestone: souls-like combat/exploration
   prototype и отдельный human runtime/gameplay-feel review.
2. Не повышать prototype Form approval до production-art approval без нового explicit decision.
3. Codex Runtime продолжать только в dedicated worktrees по собственным P4/P6 gates.
4. GATE-P01 и scientific R00 продолжать как независимые evidence lanes.

∆ — Form v0.31 prototype approved, PR #7 merged, feature branch/worktree cleaned.
D — typed approval → lifecycle QA → main sync → fresh Unity 7/7 → remote CI PASS →
merge → ancestry/semantic cleanup.
Ω — высокая для repository/Form engineering state; gameplay feel и production art ещё открыты.
Λ — пересмотреть после human gameplay-feel review, production-art gate, Codex P4/P6 или new science evidence.
