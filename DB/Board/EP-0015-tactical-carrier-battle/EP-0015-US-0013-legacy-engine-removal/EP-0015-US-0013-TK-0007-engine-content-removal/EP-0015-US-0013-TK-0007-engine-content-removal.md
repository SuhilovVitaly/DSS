---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0007-engine-content-removal
title: engine-content-removal
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0006-scenario-module-cleanup"]
files_touched: 65
serves: [AC-03, AC-07]
created: 2026-11-05
revision: 3
---

# engine-content-removal

## Why
Контент Engine без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Content/{CombatRulesDefinition,GameDataRegistry,ModuleTypeDefinition,EngineContentLoader}.cs` | боевые правила и типы | Удалить/убрать |
| `src/DeepSpaceSaga.Engine/DebugTuningState.cs` | боевые поля | Убрать |
| `tools/DeepSpaceSaga.EconomyBalance/ClusterBalanceRun.cs` | `combatRules` (`:141`) | Убрать |

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
Engine.Tests; сборка EconomyBalance. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03, AC-07 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Strict Settings and module directory loaders require simultaneous removal of obsolete schema fields and registered JSON. Move that owning cut forward from US14 TK4; keep its full content inventory/deployment check and remaining ship-class cleanup there. Preserve world engines, module structure, docking, crew, markets, and TacticalBattle content. Economy catalog identity hashes only item economics; the five removed definition files and fields do not participate, and the historical fixture fingerprint must continue to match. Remove battle tuning values now; the version bump and retired-file diagnostics belong to TK9.

- Delete: src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs
- Delete: src/DeepSpaceSaga.Client/Data/Combat/combat-rules.json
- Delete: src/DeepSpaceSaga.Client/Data/Commands/CountermeasureLauncher/commands.json
- Delete: src/DeepSpaceSaga.Client/Data/Commands/TorpedoLauncher/commands.json
- Delete: src/DeepSpaceSaga.Client/Data/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json
- Delete: src/DeepSpaceSaga.Client/Data/Modules/TorpedoLauncher/modules-torpedolauncher.json
- Edit: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs
- Edit: src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs
- Edit: src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs
- Edit: src/DeepSpaceSaga.Engine/DebugTuningState.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.cs
- Edit: src/DeepSpaceSaga.Client/Settings.json
- Edit: src/DeepSpaceSaga.Client/Data/module-types.json
- Edit: src/DeepSpaceSaga.Client/Data/Commands/Engine/commands.json
- Edit: src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json
- Edit: src/DeepSpaceSaga.Client/Data/Modules/NavigationComputer/modules-navigationcomputer.json
- Edit: tools/DeepSpaceSaga.EconomyBalance/ClusterBalanceRun.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ProductionCombatBalanceHarness.cs
Test scope: retire obsolete catalog rules and only their weapon fixtures. Reuse the real world registry in preserved steering, economy, dialogue, loadout, Save20 and debug checks. Keep malformed/duplicate/optional ship-class and world module bootstrap coverage.
- Delete: tests/DeepSpaceSaga.Engine.Tests/CombatRulesContentTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CountermeasureContentLoaderTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0004Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0005Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCounterFixture.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/CountermeasureContentTests.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0004Tests.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0005Tests.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/TorpedoLauncherContentTests.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/TorpedoCommandContentTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/CombatForecastEngineManeuverTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DialogueEffectTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/EconomyTimeContinuityTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/QuotedTradeExecutionTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/RouteRiskIntegrationTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/TradingEconomySaveSchemaTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/VoyageProfitAccountingTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/VoyageFuelLifecycleTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/VoyageFuelAccountingTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningStateTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0006TK0003Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0004Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningSaveTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningStartupTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ModulePanelProjectionTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ScenarioArmorSchemaTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/ModuleStructureCatalogTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TetrarchClassContentTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0003Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/DebugTuningDefaultsFileTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/EngineCategoryDispatchTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/DebugTuningFileWriterTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/DebugTuningPanelInputTests.cs
The recursive loader also found the unused laser implementation after its category was removed. Delete that obsolete definition in this same coherent cut:
- src/DeepSpaceSaga.Client/Data/Modules/CombatLaser/modules-combatlaser.json

## Execution evidence — 2026-11-06

Removed live rules loading, weapon/rating fields and validators, old weapon/laser definitions, obsolete engine command ids and the Settings key. The world catalog retains module structure, propulsion, ship class identity, the tactical catalog, and unchanged item economics; all historical Save18/19 fixtures and catalog guards passed without an identity bypass. Economy tool uses the world registry. Debug catalog now has six propulsion values; schema migration is TK9. Windows build clean, format/diff PASS; broad run 2021 Engine, 1965 Client, 44 EconomyBalance passes, with the four identified expectation/path defects corrected. The fresh targeted pass covers all four cases plus historical saves, catalog, tactical, and debug: 544 Engine and 191 Client. Evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-catalog-regression-03` and `20261106-world-catalog-regression-04`.
