# MUSCA — текущий статус

Дата проверки: 2026-09-22. Это оперативная сводка; исторические receipts не переписываются.
Перед новым решением по HEAD, CI, PR или локальному workspace делать fresh read-back.

## Репозиторий и интеграция

[FACT @ GitHub, 2026-09-22] Текущий `main` =
`b3de4438a4b5400a1e639ca0fd6c4f41ca13eb46` — merge PR #12
`Unity: First Threshold v0.6.1 Cinemachine camera hotfix`.

[FACT @ GitHub] PR #12 merged 2026-09-22. Его head =
`38e45af5ba3bb9136fea3d2a720888077f6cfa0a`. GitHub Actions run #94 на head завершён
`SUCCESS` для Windows/Python 3.14.6 и Browser 3D Function/Node 24.18.1.
На merge commit `b3de443...` отдельный workflow run при fresh read-back не найден,
поэтому post-merge CI именно merge commit не заявляется.

[FACT @ GitHub] Remote branch `feature/combat-controls-ai-v02` после merge всё ещё существует.
Открытых PR при read-back нет.

[BOUNDARY] В body PR #12 human gate включал четыре ручные проверки: отсутствие self-spin,
визуально корректное движение вперёд по `W`, стабильный lock-on `A-D-A-D` и отсутствие
скачка `lock -> unlock`. Merge сам по себе не доказывает, что эти четыре пункта были
отдельно зафиксированы человеком; GitHub review/comment read-back такого подтверждения не содержит.

[BOUNDARY] Merge означает интеграцию проверенного инженерного кандидата. Он не означает release,
deployment, scientific validation, production-art approval или автоматически подтверждённый gameplay feel.

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

[FACT local, 2026-09-22] Основной `C:\github\MUSCA` сохранён на
`science/r02-mn9-laterality@a43a5d4`; его research history не переписывалась.

[FACT local] Две локальные generated-настройки (`.vscode/mcp.json` и
`unity/MUSCA-Gate3D/ProjectSettings/PackageManagerSettings.asset`) оставлены на диске,
но скрыты только через локальный `.git/info/exclude`. `ProjectAuditorSettings.asset`
восстановлен; его working blob совпадает с index blob.

[FACT local] Безопасный integration candidate подготовлен в отдельном worktree
`C:\github\MUSCA-r02-main-probe` на ветке `science/r02-main-sync-candidate`.
Он сохраняет исходные R00-R02 commit hashes через merge, а не cherry-pick.

[FACT local] Отдельный combat worktree содержит незакоммиченные post-v0.6.1 изменения;
они не входят в `main` и не входят в science integration candidate.

## Local Codex Runtime

[FACT local] Codex Runtime остаётся отдельной незавершённой линией в dedicated worktrees.

[BOUNDARY] P4/P6 gates не закрыты; Runtime не активирован этим Form/gameplay merge.

## Игровой эксперимент и science

[FACT] GATE-P01 v0.1 остаётся `IN PROGRESS`: 1/12 human sessions completed.
Aggregate verdict до preregistered analysis не разрешён.

[BOUNDARY] Souls-like / Form gameplay не подменяет frozen GATE-P01 v0.1.

[FACT] ADR-0005 принят только для strict Shiu/FlyWire-v630 reproduction baseline.

[FACT local] SCI-R00 strict environment/data smoke **PASSED** on the authorized
research host. Operational receipt SHA-256:
`e4770a9b7493fb5dfee4a7ef9d0ff3a62ca8da9126ed5bcf6c4465470f9135b0`.
The historical R00 preregistration candidate remains unchanged.

[FACT local] SCI-R01 strict tutorial sugar execution **PASSED** from frozen
`SCI-R01-SHIU-SUGAR.run.json` at commit
`583e0874c1d2fa0cc70c21e6f56d698bce0bc988`: 30/30 trials completed,
`406978` spike rows, `430` active neurons. Output SHA-256:
`657f4ae3d54f90bb0c2a5f13db4156449fec0b24cff74efb84afe03035e6c9e2`.
Execution receipt SHA-256:
`38d7ed8bf191fce5f0495b8e3561483f5fcbe2da4a3140bcad069e7cdb8eec5d`.
Independent artifact verification: PASS, SHA-256
`d932e6b31d4e1519dff37f31677f59bc411358276d44671ab46853e5a774f7bf`.

[BOUNDARY] R01 PASS establishes local executable/structural validity only.

[FACT local] SCI-R02 bilateral 200 Hz MN9 laterality reproduction **PASSED** from the
frozen preregistration at `0a7489e81bbb91db3438a6522d10ebacd4ebf078` and activation
commit `713d784d21862180a7700a29b991cb96b1d654b6`. Both 30-trial hemisphere
conditions satisfied every preregistered primary criterion. Execution receipt SHA-256:
`91016e6339a17ad658f48ba814d47d7e51f9866f7e010537bc0a57b86399668d`.
Independent artifact verification: PASS, SHA-256
`1437673a23eb0650a39f100108953a79e64cf0e6b418060ac3c57d6180a086fe`.
Details: `docs/status/2026-09-19-r02-execution.md`.

[BOUNDARY] R02 PASS reproduces only the frozen bilateral 200 Hz MN9-laterality
prediction in this Shiu/FlyWire-v630 model lineage. It does not establish biological
validity or connectome-topology superiority. Topology advantage remains UNKNOWN.

## Current gates

| Surface | State |
| --- | --- |
| `main` | INTEGRATED through PR #12 / First Threshold v0.6.1 (`b3de443...`) |
| Remote PRs | 0 open |
| Remote feature branches | `feature/combat-controls-ai-v02` still present; science R02 branch is local-only |
| Gate3D Function human approval | APPROVED |
| Form v0.31 human approval | APPROVED — PROTOTYPE ONLY |
| Unity Form integration | PROTOTYPE VERIFIED |
| Runtime/gameplay human approval | PR #12 human-feel gate not independently recorded in GitHub read-back |
| Final production art | NOT APPROVED |
| Local Codex Runtime | SEPARATE / NOT ACTIVE |
| GATE-P01 v0.1 | 1/12, no aggregate verdict |
| SCI-R00 / R01 / R02 | PASS / PASS / PASS (R02 = bilateral 200 Hz MN9 prediction only) |
| Release/deployment | NOT CLAIMED |

## Следующие gates

1. Перейти от Form gate к следующему игровому milestone: souls-like combat/exploration
   prototype и отдельный human runtime/gameplay-feel review.
2. Не повышать prototype Form approval до production-art approval без нового explicit decision.
3. Codex Runtime продолжать только в dedicated worktrees по собственным P4/P6 gates.
4. GATE-P01 продолжать независимо; следующий science gate после R02 — только отдельный preregistered matched-control topology experiment. R02 PASS не повышать до topology-advantage или biological-validity claim.

∆ — SCI-R00, SCI-R01 и узкий preregistered SCI-R02 локально PASSED; topology comparison ещё не запускался.
D — R02 frozen prereg → activation → two 30-trial 200 Hz runs → raw/receipt hashes → independent recomputation PASS.
Ω — высокая для local executability/provenance и узкой R02 MN9 reproduction; topology advantage и biological validity остаются UNKNOWN.
Λ — пересмотреть после matched-control topology experiment, remote CI/PR verification, raw/reference/hash drift или выявленной ошибки анализа.
