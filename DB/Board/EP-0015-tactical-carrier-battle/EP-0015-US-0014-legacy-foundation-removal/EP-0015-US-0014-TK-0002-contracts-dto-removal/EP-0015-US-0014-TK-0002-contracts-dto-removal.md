---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0014-legacy-foundation-removal
ticket: EP-0015-US-0014-TK-0002-contracts-dto-removal
title: contracts-dto-removal
stage: done
layer: contracts
depends_on: ["EP-0015-US-0014-TK-0001-motion-removal"]
files_touched: 66
serves: [AC-02, AC-05]
created: 2026-11-05
revision: 3
---

# contracts-dto-removal

## Why
DTO старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/{BattleReportSnapshot,CombatEncounterSnapshot,CombatJournalEntry,CombatPresentationUpdate,CombatSnapshot}.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Contracts/{CountermeasureSnapshot,EvasionSnapshot,HostileCombatSnapshot,WeaponOperatorSnapshot,DefenseLaunchPreview}.cs` | старый бой | Удалить |

## Public API after the change
Удаление публичных боевых DTO.

## Implementation steps
1. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
2. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Contracts.Tests; сборка решения. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before edits — 2026-11-06

Compile-time consumers must be removed with the public combat DTOs. This includes the retired harness and standalone CombatAnalyzer (originally listed in TK-0005), DTO-typed fields, dead client trajectory branches, and old pending-command fields. Keep world motion, snapshots, structure, control and economy tests. Preserve legacy wire parsing at the archive boundary.

- Delete: src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs
- Delete: src/DeepSpaceSaga.Contracts/CombatEncounterSnapshot.cs
- Delete: src/DeepSpaceSaga.Contracts/CombatJournalEntry.cs
- Delete: src/DeepSpaceSaga.Contracts/CombatPresentationUpdate.cs
- Delete: src/DeepSpaceSaga.Contracts/CombatSnapshot.cs
- Delete: src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs
- Delete: src/DeepSpaceSaga.Contracts/DefenseLaunchPreview.cs
- Delete: src/DeepSpaceSaga.Contracts/EvasionSnapshot.cs
- Delete: src/DeepSpaceSaga.Contracts/HostileCombatSnapshot.cs
- Delete: src/DeepSpaceSaga.Contracts/SessionOutcomeSnapshot.cs
- Delete: src/DeepSpaceSaga.Contracts/WeaponOperatorSnapshot.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/BattleReportSnapshotTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/CombatEventTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/CombatReportBatchTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/CountermeasureEventTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/CountermeasureSnapshotTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/DefenseLaunchPreviewTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/HostileCombatSnapshotTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0003Tests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0005ContractTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0004TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0006TK0002Tests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0001Tests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0020TK0005Tests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/SessionOutcomeSnapshotTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/TorpedoTerminationTests.cs
- Delete: tests/DeepSpaceSaga.Contracts.Tests/WeaponOperatorSnapshotTests.cs
- Delete: tests/DeepSpaceSaga.Engine.Tests/ProductionCombatBalanceHarness.cs
- Delete: tools/DeepSpaceSaga.CombatAnalyzer/DeepSpaceSaga.CombatAnalyzer.csproj
- Delete: tools/DeepSpaceSaga.CombatAnalyzer/Program.cs
- Delete: tools/DeepSpaceSaga.CombatAnalyzer/README.md
- Edit: src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs
- Edit: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs
- Edit: src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailStore.cs
- Edit: src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs
- Edit: src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs
- Edit: src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs
- Edit: src/DeepSpaceSaga.Contracts/PlayerCommand.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/LegacyCombatSaveReader.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs
- Edit: src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/CombatSessionRoundtripTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/CommandsPanelSkeletonTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0004Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/WeaponHudControlsTests.cs
- Edit: tests/DeepSpaceSaga.Contracts.Tests/ModulePanelContractTests.cs
- Edit: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0017TK0004Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/KnownMapProjectionTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0019TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/SaveMigration20Tests.cs
- Edit: tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.cs — remove the no-op clears in world projection found by the build.
- Edit: tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/WorldGuardTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs

## Execution evidence — 2026-11-06

Removed eleven public realtime combat DTO files and the consumers that prevented a compilable slice, including the old CombatAnalyzer harness. Save18/19 pending-command normalization strips retired fields, drops archived combat commands and retains the ordinary command and coordinates; the current reader still rejects retired payload. World and tactical public DTOs are separate. Scoped format, zero-warning solution build, diff check, and 175 Contracts, 1,267 Motion, 2,024 Engine (non-corpus), 1,967 Client, 42 Economy (fast partition) and 9 Performance tests passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-contract-dto-03/`. Long corpus and Economy runs remain in the full-regression gate of TK-0005.
