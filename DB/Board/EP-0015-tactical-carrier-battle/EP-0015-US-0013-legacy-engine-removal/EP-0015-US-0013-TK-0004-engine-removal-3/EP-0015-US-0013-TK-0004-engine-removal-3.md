---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
ticket: EP-0015-US-0013-TK-0004-engine-removal-3
title: engine-removal-3
stage: done
layer: engine
depends_on: ["EP-0015-US-0013-TK-0003-engine-removal-2"]
files_touched: 105
serves: [AC-02, AC-07]
created: 2026-11-05
revision: 3
---

# engine-removal-3

## Why
Engine: hostile fire, NPC dodge, point aim/combat, часть 3.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.HostileFire.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.NpcDodge.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointAim.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs` | старый бой | Удалить |

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

Delete the coupled realtime execution partials: aim/launch, hostile steering and fire, NPC dodge, evasion, countermeasures, damage and journal. Remove their initialization, world-boundary, projection, command and debug retiming consumers. Keep ordinary clock, steering, security, resource-survey and snapshot behavior. Legacy save schema/assignment validation survives only for the migration and content cuts; the old presentation transport and terminal guards are owned by TK-0005.

Delete:
- src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Evasion.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.HostileFire.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.NpcDodge.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PointAim.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuningProjectiles.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuningReloads.cs

Edit:
- src/DeepSpaceSaga.Engine/SimulationEngine.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Presentation.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuning.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.DebugTuningShips.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.TurnHold.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.SpeedHold.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.WeaponOperators.cs
Station security shares DestroyShip but is a scripted docking consequence, with existing deadline and escape regressions. Keep the scripted access denial, precise deadline, and stopped world; mark the existing world ship destroyed instead of generating a combat wreck or journal event. The transitional outcome wrapper is removed in TK-0005; the station rule stays. Direct consumers and matching test:
- src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs
- src/DeepSpaceSaga.Engine/Dialogue/StationSecuritySystem.cs
- tests/DeepSpaceSaga.Engine.Tests/DialogueTests.cs
Retire suites whose behavior required the removed execution. For mixed suites remove only obsolete tests and keep common fixture/steering, startup, atomic save, physics, catalog, dialogue and panel coverage.
- Delete: tests/DeepSpaceSaga.Engine.Tests/AutomaticDefenseTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/BasicCombatEndToEndTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CombatBootstrapTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CombatReportIdentityTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CombatWreckTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CountermeasureBootstrapTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CountermeasureLifetimeTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/DebugTuningProjectileTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/DebugTuningReloadTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/DefeatedSessionSaveTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/DefeatTimeBoundaryTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/DefensePreviewProjectionTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/DisabledModuleTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/HostileCombatPathTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/HostileOrbitRuntimeTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/ManualCountermeasureFireTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PhaseThreeJournalTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveContinuationTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0003Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0004Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0003Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0005TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0005TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0006TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0003Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0008TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0005Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0003Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0005Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0020TK0005Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointForecastRuntimeParityTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/HostileAttackAiTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PhaseThreeSaveSchemaTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0005Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0014TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs
- Delete: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0006TK0003Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0009TK0004Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0019TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningShipTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningSaveTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/DebugTuningStartupTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/SpeedHoldEngineTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/CombatPresentationTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ModuleDamageTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ShipDestructionRulesTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0002TK0005Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TorpedoLauncherContentTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUiFixture.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs
Retire removed fire modes and projectile projection; preserve ordinary command panel and debug command assertions:
- tests/DeepSpaceSaga.Engine.Tests/DebugTuningCommandTests.cs
- tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs
- tests/DeepSpaceSaga.Engine.Tests/ModulePanelProjectionTests.cs
Keep structural loadout checks through scenario cut. The old balance harness is linked into Performance and CombatAnalyzer and stays until the owning tools cut.
- Edit: tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs

Client regression corrections preserve authored catalog/placement checks and local Save20 roundtrip while asserting there is no runtime combat projection:
- tests/DeepSpaceSaga.Client.Tests/CountermeasureContentTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatSessionRoundtripTests.cs
Engine regression: keep physical maneuver forecasts independent of the former hostile scheduler's synthetic 100 ms boundary; verify world module defaults and Save20 structure/armor retirement using an engine module.
- Edit: tests/DeepSpaceSaga.Engine.Tests/CombatForecastEngineManeuverTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ScenarioArmorSchemaTests.cs
Preserve exact route, pose and scanner result on Save20 without comparing the pre-migration weapon module projection:
- tests/DeepSpaceSaga.Engine.Tests/ResourceSurveyTests.cs
Keep the complete generated-world identity/motion comparison across Save20. Exclude only authored legacy AttackAi actors from the expected continuation, independently of the saved result; their source removal is TK-0006:
- tests/DeepSpaceSaga.Engine.Tests/SystemCorrectnessCorpusTests.cs
Apply the same authored hostile exclusion to full-map and cluster continuation corpora; every generated object, route and clock still compares exactly.
- tests/DeepSpaceSaga.Engine.Tests/FullMapCorrectnessCorpusTests.cs
- tests/DeepSpaceSaga.Engine.Tests/ClusterCorrectnessCorpusTests.cs
The demo integration drives many sequential actions through the real one-second world snapshot stream. Its 15-second whole-test deadline expired at varying stages (sell/refuel) with no last snapshot; leave assertions and the production period intact, allow 30 seconds for the complete dialogue and three trades:
- tests/DeepSpaceSaga.Client.Tests/StationMarketDemoContentTests.cs

## Execution evidence — 2026-11-06

Removed the realtime execution for weapons, hostile steering/fire, countermeasures, evasion, damage, journal and combat debug retiming. Scripted station security still denies docking, checks departure at the exact motion deadline, stops the world and protects the last save; it marks the world ship destroyed without combat events/wrecks. Save20, normal manual axes, scan/navigation, dialogue, world economy and the tactical sandbox were checked. Windows artifacts under D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/: 1996/1996 Client and clean build/format (`20261106-realtime-runtime-regression-06`), 2189/2189 Engine excluding long corpora (`20261106-realtime-runtime-regression-04`), 3 × 100-seed corrected corpus cases (`20261106-realtime-runtime-corpus-sample-01`), 61/61 EconomyBalance (`20261106-realtime-runtime-regression-03`), 284 Contracts, 1376 Motion and 9 Performance (`20261106-realtime-runtime-untouched-01`). A monolithic Engine run reached the process cap; remaining long corpus cases are a final regression gate, not recorded as passed here. Diff check PASS.
