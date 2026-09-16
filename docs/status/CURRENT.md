# MUSCA — текущий статус

Дата проверки: 2026-09-16. Это оперативная сводка; для HEAD/CI/PR перед решением всегда делать fresh connector read-back.

## Репозиторий и инженерная линия

[FACT] Последний substantive 3D snapshot: `bdb8935989c486c46ce2a293051d7be026c93ec2` на `feature/3d-gate-prototype`.

[FACT @ GitHub] Для этого SHA push CI #29 и PR CI #30 завершились SUCCESS. В обоих runs зелёные Windows/Python 3.14.6 и Browser 3D Function/Node 24.18.1 jobs.

[FACT] Draft PR #3 открыт в `feature/visual-interface`, mergeable, не merged. `main` не затронут.

[FACT] Python baseline: **113/113 tests PASS**. Frozen pilot packager freeze+verify: PASS, 55 payload files.

[FACT] Browser Function oracle: Three.js `0.186.0`, 8/8 state tests PASS, `npm audit` 0 vulnerabilities, room validator PASS.

## Unity Gate3D v0.2

[FACT] Unity `6000.6.1f1` и Blender `5.2.0 LTS` подтверждены на authorized Windows host.

[FACT] `unity/MUSCA-Gate3D` существует как реальный Unity project. GateLab scene создаётся Editor-builder-ом, а не ручным YAML.

[FACT] Unity scene validation: PASS; missing scripts 0; camera FOV 72°; eye height 1.68 m; AMBER/COBALT x=±4.1 m, z=-7.2 m; one build scene.
[FACT] Unity Test Framework EditMode: **7/7 PASS**. Windows Development build: Succeeded, zero build errors. Standalone player production-camera QA: PASS.

[FACT] Player can navigate with WASD/mouse, inspect mirrored reagent stations, use contextual `Q` scan / `E` choice, and physically open the 4.5 m gate to visible Sector B. Hidden answer is not rendered.

[FACT] Function lifecycle = `READY_FOR_HUMAN_FUNCTION_APPROVAL`; `human_approval=null`. Blender Form = `BLOCKED_PENDING_FUNCTION_APPROVAL`.

[BOUNDARY] GitHub Actions currently does not execute Unity Editor. Unity engine evidence is local authorized-host evidence; remote CI verifies source/oracle/contracts only.

[WARN] Unity external TLS checks emit recurring `Curl error 35` during Editor shutdown. Local UPM/import/tests/build/runtime work; external package/network reliability is not claimed clean.

## Игровой эксперимент

[FACT] GATE-P01 v0.1 remains `IN PROGRESS`, 1/12 completed on frozen terminal snapshot SHA-256 `f70981ac197203ad21effcf6e9ba3ab4c620a3df72a9b1315ee8abf2ae91ae2a`.

[BOUNDARY] Gate3D v0.2 is **not** substituted into remaining GATE-P01 v0.1 participants. A visual/3D human test requires a new preregistered version.

## Научная линия

[FACT] ADR-0005 remains accepted only for strict Shiu/FlyWire-v630 reproduction baseline. Exact source/data provenance is PASS.

[FACT] Strict historical environment remains NOT SOLVED. SCI-R00 NOT PASSED; SCI-R01 NOT RUN; topology advantage UNKNOWN.

## Current gates

| Surface | State |
| --- | --- |
| Python/Tk baseline | PASS, 113/113 |
| Browser Function oracle | PASS, 8/8 |
| Unity Gate3D local runtime | PASS engineering verification |
| Unity remote engine CI | NOT IMPLEMENTED |
| Gate3D Function human approval | PENDING |
| Blender Form composition | BLOCKED pending Function approval |
| GATE-P01 v0.1 | 1/12, no aggregate verdict |
| SCI-R00 / R01 | NOT PASSED / NOT RUN |
| PR #3 | draft / mergeable / unmerged |
| main/deployment | unchanged / NOT DONE |
## Следующие gates

1. Получить explicit human Function approval текущего `gate-lab-v0.2` layout либо зафиксировать конкретные правки Function.
2. Только после approval перейти в Blender 5.2 LTS Form: authored shell/props, openings, support/contact, review renders, затем обратно в Unity runtime.
3. Не менять frozen GATE-P01 v0.1; продолжать его отдельно по preregistered assignments.
4. На research lane отдельно решить strict environment/access premise и выполнить R00.
5. Independent code/visual review PR #3 остаётся желательным до merge.

∆ — 3D surface перешла из `NOT STARTED` в verified Unity Function prototype.
D — browser oracle → Unity scene builder → 7/7 tests → standalone build/captures → push #29 + PR #30 green.
Ω — высокая для local Windows engineering execution; human spatial usability, final Form art and science remain unvalidated.
Λ — пересмотреть после Function approval/rejection, visual human test, new Unity/network evidence or R00 result.
