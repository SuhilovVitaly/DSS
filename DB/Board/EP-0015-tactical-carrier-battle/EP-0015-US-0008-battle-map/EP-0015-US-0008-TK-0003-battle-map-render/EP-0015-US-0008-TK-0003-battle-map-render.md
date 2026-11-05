---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0008-battle-map
ticket: EP-0015-US-0008-TK-0003-battle-map-render
title: battle-map-render
stage: done
layer: client
depends_on: ["EP-0015-US-0008-TK-0002-panel-header"]
files_touched: 3
serves: [AC-03]
created: 2026-11-05
revision: 3
---

# battle-map-render

## Why
Отрисовка карты.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | new | Отрисовка поля по кадру |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs` | new | Raster на фикстуре кадра: сетка, линейка, цвета от playerSide, состояния |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | existing frame and panel | Connect map renderer, presentation flags and toggle/tooltips |

## Public API after the change
—

## Implementation steps
1. Подготовленная геометрия, без вызовов Engine из Render.
2. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Use the approved Markdown and Engine snapshots. The source mentions the reference HTML ruler ladder but does not list its lengths; use a 1–2–5 decimal ruler below 120 logical pixels, with actual projected length and localized units. Forecast path and gun sector use captured course/speed/range/half arc; rear arc uses the captured rule and the Display criterion radius 55. Selection ring is rendered when frame supplies an ID (selection input in TK-0004). Fit tooltip is wired now; AI-all tooltip key will be used with the actual button in TK-0004.

## Execution evidence — 2026-11-06

AC-03 renderer: grid and scale ruler, oriented carrier and drone markers, player-relative color, active/disabled/destroyed/jammed treatments, HUD bars and collision-aware labels, last-motion trails, forecast and sectors. Selection ring uses frame selection; actual selection and the AI-all button arrive in TK-0004. Actual panel toggles change raster pixels without changing the Engine state. The approved Markdown omits the legacy ruler length ladder; use the documented decimal 1–2–5 rule.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-map-render-01/: format/build/diff/self-review PASS; Client162, Engine378, Motion source guard19 PASS; inspected default map and status-marker CPU rasters. Native/physical/human checks remain open.
