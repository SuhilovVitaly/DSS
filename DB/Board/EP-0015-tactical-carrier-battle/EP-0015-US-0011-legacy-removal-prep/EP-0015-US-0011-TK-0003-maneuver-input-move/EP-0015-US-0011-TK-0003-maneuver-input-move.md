---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0011-legacy-removal-prep
ticket: EP-0015-US-0011-TK-0003-maneuver-input-move
title: maneuver-input-move
stage: done
layer: client
depends_on: ["EP-0015-US-0011-TK-0002-save-fixtures"]
files_touched: 18
serves: [AC-03]
created: 2026-11-05
revision: 3
---

# maneuver-input-move

## Why
Не сломать маневрирование при удалении `CombatInputSettings`.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/UI/maneuver-input.json` | new | Клавиши маневрирования и debugPanel |
| `src/DeepSpaceSaga.Client/ManeuverInputSettings.cs` | new | Чтение и проверка |
| `src/DeepSpaceSaga.Client/CombatInputSettings.cs` | клавиши боя и маневрирования | Только боевые клавиши |
| `src/DeepSpaceSaga.Client/Data/UI/combat-input.json` | schema 2 | Без клавиш маневрирования |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ManeuverInputSettingsTests.cs` | new | Перенос, конфликты, маневрирование работает |

## Public API after the change
`ManeuverInputSettings`.

## Implementation steps
1. `TacticalBattleInputSettings` проверяет конфликты с новым файлом.
2. Потребители клавиш маневрирования переключены.

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

In addition to the two input JSONs, the two binding classes and owning test, route the distinct bindings through Client files `TacticalBattleInputSettings.cs`, `UI/KeyboardEdgeTracker.cs`, `UI/SkiaWindow.cs`, `UI/Screens/GameSession/{GameSessionScreen,GameSessionScreen.ManualAxes,GameSessionScreen.DebugTuning}.cs`, `UI/Screens/GameSession/Controls/DebugTuningPanel.cs` and `UI/Screens/TacticalBattle/TacticalBattleFlow.cs`. Update test consumers `CombatInputSchemaV2Tests.cs`, `DebugTuningPanelInputTests.cs`, `ManeuverHintsTests.cs`, `PointCombatUS0012TK0001Tests.cs` and `TacticalBattle/TacticalBattleInputSettingsTests.cs`. This extension is required to compile the Client and existing regressions with one source of maneuver keys. Remappable DebugPanel stays blocked within the tactical modal. A new strict combat-only schema ships during the transition; old schema1/2 movement properties may be present but are ignored and never applied. No legacy production combat is removed in this ticket.

## Execution evidence — 2026-11-06

AC-03: a separate strict maneuver-input.json owns turn left/right, accelerate, brake, full speed and the world debug key. GameSessionScreen, native edge polling and help use that captured configuration. Tactical launch validates actual world/remaining legacy mappings, and blocks the actual remapped debug key in the modal. Combat schema3 no longer reads movement settings; schema1/2 historical fields are ignored. Existing real remapped J input still reaches Engine TurnHold.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-maneuver-input-01/: build/format/diff/self-review PASS; 218 Client +380 Engine +19 Motion guard PASS. New hint rasters and scratch settings use the current run path.
