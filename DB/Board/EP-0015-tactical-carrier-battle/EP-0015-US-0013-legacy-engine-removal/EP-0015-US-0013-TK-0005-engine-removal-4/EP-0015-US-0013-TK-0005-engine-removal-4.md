---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0005-engine-removal-4
title: engine-removal-4
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0004-engine-removal-3"]
files_touched: 27
serves: [AC-02, AC-07]
created: 2026-11-05
revision: 3
---

# engine-removal-4

## Why
Engine: остаток и ядро, часть 4.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombatPersistence.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/Combat/{LaunchScatterRng,ModuleDamageMath,ModuleHitLocationRng,ShipDestructionMath}.cs` | математика старого боя | Удалить |
| `src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs` | боевые потоки | Убрать боевые имена |
| `src/DeepSpaceSaga.Engine/{SimulationEngine,SimulationEngine.Presentation,SimulationEngine.PlayerDefeat,SimulationEngine.Time}.cs` | боевые части | Убрать по инвентаризации |
| `src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj` | `InternalsVisibleTo DeepSpaceSaga.CombatAnalyzer` (`:17`) | Убрать |

## Public API after the change
—

## Implementation steps
1. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
2. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Engine.Tests регрессия. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Delete compact combat transport, presentation watchdog and obsolete clock control methods, and old Engine combat math. Debug tuning remains in normal world snapshots. Preserve the scripted station security deadline, stopped ship/world, and refusal to overwrite a save after the ship is destroyed, via a private station-specific clock boundary and ordinary world ship state. Remove the old combat outcome from Engine snapshots. Save18/19 validation still uses its historical seed keys as local schema values until the minimal legacy-reader cut.
- Delete: src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs
- Delete: src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs
- Delete: src/DeepSpaceSaga.Engine/Combat/LaunchScatterRng.cs
- Delete: src/DeepSpaceSaga.Engine/Combat/ModuleDamageMath.cs
- Delete: src/DeepSpaceSaga.Engine/Combat/ModuleHitLocationRng.cs
- Delete: src/DeepSpaceSaga.Engine/Combat/ShipDestructionMath.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CombatPresentationTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/ModuleDamageTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/ShipDestructionRulesTests.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationClock.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuning.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.Time.cs
- Edit: src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs
- Edit: src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/PointCombatContinuationValidation.cs
- Edit: src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningCommandTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/FullMapCorrectnessCorpusTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUiFixture.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DialogueTests.cs
- Delete: src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs
- Add: src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.StationSecurity.cs

## Execution evidence — 2026-11-06

Deleted the obsolete Engine compact combat stream, watchdog/clock overrides, combat outcome, runtime combat math, world combat RNG names and analyzer friend. Historical seed identities remain local to the Save18/19 validator only. The station security rule still marks the ship destroyed, stops precisely at the deadline and denies saving over an existing world; no combat outcome or wreck is produced. Debug tuning is published in ordinary world snapshots. Windows: clean solution build, scoped format and diff; 945 Engine, 223 Client and 2 EconomyBalance passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-presentation-retirement-01`.
