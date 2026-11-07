---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0008-battle-map
ticket: EP-0015-US-0008-TK-0004-movement-orders
title: movement-orders
stage: done
layer: client
depends_on: ["EP-0015-US-0008-TK-0003-battle-map-render"]
files_touched: 5
serves: [AC-04]
created: 2026-11-05
revision: 3
---

# movement-orders

## Why
Ввод приказов движения.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleOrders.cs` | new | Приказы, тронутые, aimAt, достижимость, автовыбор |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovePanel.cs` | new | Блок управления движением |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementOrdersTests.cs` | new | Начальное предложение, только тронутые в запросе, aimAt, клавиши, ползунки, приоритет мыши, автовыбор |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | actual input routing | Dispatch view controls and only touched orders |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | immutable renderer | Draw captured order geometry, end handles and selected sector |

## Public API after the change
—

## Implementation steps
1. Ключи этого тикета — в `Data/Locale/Russian.json` (рабочий язык, D-USER-11) и теми же ключами в `English.json` черновым переводом без вычитки (правило R-07).

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before implementation — 2026-11-06

Five Client code files; Motion limits, integration and path functions remain the shared authority. The UI samples 13 speeds and 61 turns for cursor aim, caches that geometry per unit and phase, and uses stable first match within 1e-6. Form changes are local, draft input is shown as immutable render data, and the actual Engine command includes edited rows only. Selecting a drone does not touch it; ending an order gesture can advance to the closest untouched drone. AI-all clears touched state. Panel card/slider input remains accessible with scrolling at larger UI scale.

## Execution evidence — 2026-11-06

AC-04: exact AI suggestions populate the movement panel and preview paths without touching orders. Real keyboard, sliders, body/card selection, handle drag and AI-all exercise the actual battle screen. Release advances to nearest untouched; body/card selection does not. Aim mesh tests include an exact interior candidate and an equal-endpoint first tie. Real Engine paths match the edited preview for the changed row and the original proposals for all other rows; next phase clears the draft.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-movement-orders-01/: format/build/diff/self-review PASS; Client166, Engine378, Motion guard19 PASS. Visual inspection identified missing arrow glyphs. Final D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-movement-orders-02/: named key hints and wrapped limits; Client166, build/format/diff PASS; final raster inspected.
