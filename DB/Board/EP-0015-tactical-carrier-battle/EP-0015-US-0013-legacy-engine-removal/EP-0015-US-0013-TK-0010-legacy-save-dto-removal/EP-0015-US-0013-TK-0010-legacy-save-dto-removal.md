---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0010-legacy-save-dto-removal
title: legacy-save-dto-removal
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0009-debug-tuning-cleanup"]
files_touched: 22
serves: [AC-06, AC-07]
created: 2026-11-05
revision: 3
---

# legacy-save-dto-removal

## Why
Save-DTO старого боя не нужны writer'у.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Scenario/{CombatReactionSaveData,CombatReportsSaveData,CombatReportsSaveValidation,CombatSaveData,CountermeasureSaveData,HostileCombatSaveData,PointCombatContinuationValidation,PointCombatSaveData}.cs` | save DTO старого боя | Удалить |
| `src/DeepSpaceSaga.Engine/Scenario/LegacyCombatSaveReader.cs` | new | Минимальное чтение старых секций для миграции |
| `src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs` | из US-0011 | Читает через legacy-reader |

## Public API after the change
—

## Implementation steps
1. Фикстуры US-0011 проходят.
2. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
3. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Engine.Tests SaveMigration20Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-06, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Retire all eight typed combat save schemas and move archival validation into one read-only JSON boundary. Save18/19 must still validate archive versions, flight/owner/deadline geometry, launch evidence, named RNG counters, actor identities and reports before dropping obsolete payloads. Save20 rejects old combat sections and writes no continuation. The normal world validation, economic manifest and catalog guards remain authoritative; tests use the unchanged historical bytes with controlled mutations rather than constructing runtime combat DTOs.

- Delete: src/DeepSpaceSaga.Engine/Scenario/CombatReactionSaveData.cs
- Delete: src/DeepSpaceSaga.Engine/Scenario/CombatReportsSaveData.cs
- Delete: src/DeepSpaceSaga.Engine/Scenario/CombatReportsSaveValidation.cs
- Delete: src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs
- Delete: src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs
- Delete: src/DeepSpaceSaga.Engine/Scenario/HostileCombatSaveData.cs
- Delete: src/DeepSpaceSaga.Engine/Scenario/PointCombatContinuationValidation.cs
- Delete: src/DeepSpaceSaga.Engine/Scenario/PointCombatSaveData.cs
- Add: src/DeepSpaceSaga.Engine/Scenario/LegacyCombatSaveReader.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/CombatReportsSaveSchemaTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/TradingEconomySaveSchemaTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/SaveFixturesTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/SaveMigration20Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/EconomyTimeContinuityTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/QuotedTradeExecutionTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/VoyageFuelAccountingTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0003Tests.cs

## Execution evidence — 2026-11-06

Removed the eight typed real-time combat save files and all six combat section members from GameStateData. A JSON-only archival reader validates envelope versions, known identities, finite flight geometry/deadlines, launch facts, RNG cursors, hostile/evasion/dodge history and report metadata before removing those sections. The world loader still validates the preserved economic continuation, clocks and catalog. Original active/inactive Save18/19 fixtures loaded and continued deterministically; 18 controlled defects rejected atomically, as did older unsupported versions and combat sections under Save20. Windows build and scoped format/diff clean; 637 Engine and 201 Client passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-legacy-save-reader-02`. Extended full corpus is tracked separately for final validation.
