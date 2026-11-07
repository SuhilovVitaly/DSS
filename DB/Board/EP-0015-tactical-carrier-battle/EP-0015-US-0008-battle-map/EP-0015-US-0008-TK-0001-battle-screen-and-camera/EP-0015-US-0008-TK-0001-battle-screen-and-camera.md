---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0008-battle-map
ticket: EP-0015-US-0008-TK-0001-battle-screen-and-camera
title: battle-screen-and-camera
stage: done
layer: client
depends_on: ["EP-0015-US-0007-TK-0004-battle-keys-guard"]
files_touched: 5
serves: [AC-01]
created: 2026-11-05
revision: 3
---

# battle-screen-and-camera

## Why
Экран и камера.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | new | Экран, фазы, панель, вкладки, кадр-снимок |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleCamera.cs` | new | fit, zoom, pan, userView, resize на основе CameraState |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleCameraTests.cs` | new | Вписывание, масштаб, сдвиг, сохранение вида между фазами, resize |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | compact battle factory | Connect battle screen |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs` | compact outcome buttons | Hit actual right panel New button |

## Public API after the change
`TacticalBattleFrame` — неизменяемый кадр для отрисовки.

## Implementation steps
1. Z = единицы карты на пиксель с учётом uiScale.
2. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Five Client files: three planned plus the actual SkiaWindow factory and existing navigation test whose New button moves to the right panel. Camera uses CameraState for pan/zoom and captures an immutable transform with the admitted battle snapshot. Fit includes live positions and reachable far poses calculated using shared Motion. Zoom limits are width multiples 1/8–4 as stated in Display §4. Right drag/wheel/Fit affect only the tactical camera. A minimal snapshot display keeps the phase actions working while TK-0002/3 add the full panel and map renderer. No parallel reference implementation.

## Execution evidence — 2026-11-06

AC-01: actual battle factory uses the scaled map and full-height 400 logical px panel. Immutable frame carries admitted snapshot, projection and panel status; display does not query Engine. Camera fits carriers, live drones and own Motion endpoints with 70-unit margin, width at least 600. Window mouse routes actual right drag, release and wheel; test asserts anchored zoom bounds, preserved center/width on resize, manual-view retention across phases, auto-fit when a live drone leaves, 0 reset and unchanged world camera.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-battle-camera-01/: format/build/diff/self-review PASS; Client158, Engine378, Motion source guard19 PASS. Native/physical/human acceptance remains open. Panel and map detail continue in TK-0002/3.
