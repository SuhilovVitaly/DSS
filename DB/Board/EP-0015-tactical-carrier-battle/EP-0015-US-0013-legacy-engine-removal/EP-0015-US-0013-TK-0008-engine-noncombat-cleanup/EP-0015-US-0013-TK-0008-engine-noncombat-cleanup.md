---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0008-engine-noncombat-cleanup
title: engine-noncombat-cleanup
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0007-engine-content-removal"]
files_touched: 11
serves: [AC-04, AC-07]
created: 2026-11-05
revision: 3
---

# engine-noncombat-cleanup

## Why
Небоевое без боевых веток.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/{SimulationEngine.TurnHold,SimulationEngine.SpeedHold,RuntimeMotion}.cs` | evasion и боевые проверки | Почистить |
| `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs` | encounter (`:209`) | Почистить |

## Public API after the change
—

## Implementation steps
1. Маневрирование, скорость, диалоги — регрессия без ослабления.
2. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
3. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Engine.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-04, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Keep manual axes, steering/cancel receipts, normal module readiness, scanning, trade and dialogue. Strip dormant realtime flight/evasion branches, hostile actor state, and module combat timers/armor from world runtime. Client control readiness uses power, operational status and structure. Historical Save18/19 data still validates and drops through its owning reader; the ordinary structure projection remains.

- Edit: src/DeepSpaceSaga.Engine/RuntimeMotion.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.TurnHold.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.SpeedHold.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.ResourceSurvey.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.TradeExecution.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.TradeQuotes.cs
- Edit: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ResourceSurveyTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ModuleHealthProjectionTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/ManualManeuverInputTests.cs

## Execution evidence — 2026-11-06

Removed dormant world projectile/evasion motion, hostile runtime state, auto-defense and armor/disable timers. Manual axes and module readiness use power, operational state and structure; ordinary destroyed-module reasons remain. Preserved held turns, saved steering receipts, scanner availability and interruption, docking, dialogue, clock and trade. Windows clean solution build, scoped format and diff check; 745 Engine and 239 Client checks passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-controls-regression-01`.
