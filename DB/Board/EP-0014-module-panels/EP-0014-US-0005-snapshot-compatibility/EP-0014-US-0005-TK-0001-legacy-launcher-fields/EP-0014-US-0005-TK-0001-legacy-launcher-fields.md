---
epic: EP-0014-module-panels
story: EP-0014-US-0005-snapshot-compatibility
ticket: EP-0014-US-0005-TK-0001-legacy-launcher-fields
title: legacy-launcher-fields
stage: implemented
layer: contracts
depends_on: [EP-0014-US-0003-TK-0001-engine-panel]
files_touched: 25
serves: [AC-02, AC-03]
created: 2026-10-22
revision: 1
---

# legacy-launcher-fields

## Why
Remove retired launcher/installed snapshot fields after consumer migration; keep required save DTO fields. Expand atomic constructor migration scope before edits if needed.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Existing owning implementation or new named test | Remove retired launcher/installed snapshot fields after consumer migration; keep required save DTO fields. Expand atomic constructor migration scope before edits if needed. |
| `src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs` | Existing owning implementation or new named test | Remove retired launcher/installed snapshot fields after consumer migration; keep required save DTO fields. Expand atomic constructor migration scope before edits if needed. |
| `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs` | Existing owning implementation or new named test | Remove retired launcher/installed snapshot fields after consumer migration; keep required save DTO fields. Expand atomic constructor migration scope before edits if needed. |

## Public API after the change
Remove retired launcher/installed snapshot fields after consumer migration; keep required save DTO fields. Expand atomic constructor migration scope before edits if needed. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

## Implementation steps
Read current owning functions and matching tests; implement the stated result; preserve neighboring behaviors; add observable production-path checks.

## Out of scope
Combat balance, hidden NPC futures, unrelated refactors and save-version changes.

## Invariants
Engine owns outcomes/RNG/commands; Client uses admitted immutable snapshots and shared Motion math. Render performs no synchronous Engine access. No new hidden information.

## Tests
Owning project: tests/DeepSpaceSaga.Contracts.Tests. Focus named ModulePanel tests, then affected regression; build, scoped format and git diff --check. Test processes capped at300000ms, absolute results directory under D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/.

## Definition of Done
Implemented criteria and tests pass, diff reviewed, actual evidence recorded, separate ticket commit immediately pushed and remote SHA verified. Native and human gates remain distinct from automated tests.

## Self-containment check
Parent story defines numeric fixtures and full criteria; epic contains source specification. Current Code context must be corrected before any expanded edits, as authorized by EpicExecutionPrompt.

## Atomic migration scope before implementation
Source evidence: Scenario/CombatSaveData.cs LauncherSaveData.State embeds LauncherCombatSnapshot and validates retired values; SimulationEngine.CombatPersistence.cs captures it directly. Deleting fields without a dedicated save DTO would silently drop Save18/19 validation. Introduce Engine-owned LauncherCombatSaveState preserving save wire shape and validation; convert from clean live snapshot at capture. ActiveTorpedoIds is retained: PointCombat.cs uses it at launch/removal and PointCombatPersistence.cs validates continuation ownership.

This contracts migration must atomically update primary DTOs, Engine save adapter/producers/range consumer, Client readiness display and positional constructor fixtures across matching test projects. The original one-layer/five-file plan is explicitly superseded for this breaking compile-time migration under EpicExecutionPrompt authority, rather than adding a temporary legacy public constructor or leaving an unbuildable commit. Engine/Contracts/Client ownership and gameplay outcomes remain unchanged.

Allowed additional paths: src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs; src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs; src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs; src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs; all typed LauncherCombatSnapshot constructor/removed-member fixtures identified under tests/DeepSpaceSaga.Contracts.Tests, tests/DeepSpaceSaga.Engine.Tests, tests/DeepSpaceSaga.Client.Tests. Preserve Content.ModuleTypeDefinition.TorpedoMaxFlightTimeMs and loader validation: that separate content schema explicitly rejects retired catalogs, and is not the removed InstalledModuleSnapshot member.

Additional typed consumers found by compilation: Dialogue/SimulationEngine.Dialogue.cs contains a retired docking reset (always false for current point state); remove it. Matching Engine fixtures CombatReportsScenarioFixtures.cs, ModuleCombatEndToEndTests.cs, FireModeRuntimeTests.cs and PointCombatUS0018TK0002Tests.cs migrate retired readiness checks to actual module destruction/readyAt and assert schema absence.

SetFireModeTests.cs and restored launcher schema assertions also migrate to absence of ModeId while preserving rejected command, RNG, deadline and flight roundtrip assertions.

PirateScenarioTests.cs and TorpedoCommandPanelTests.cs migrate direct old launcher member assertions to profile capabilities and absent retired schema.

## Final migration inventory

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs`
- `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs`
- `src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs`
- `src/DeepSpaceSaga.Engine/Dialogue/SimulationEngine.Dialogue.cs`
- `src/DeepSpaceSaga.Engine/Scenario/CombatSaveData.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs`
- `tests/DeepSpaceSaga.Client.Tests/CombatMetricsWriterTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/PirateScenarioTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0002Tests.cs`
- `tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/WeaponHudControlsTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/CombatForecastContractTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/ModulePanelContractTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0005ContractTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/WeaponOperatorSnapshotTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/CombatBootstrapTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/CombatReportsScenarioFixtures.cs`
- `tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0003Tests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs`

Validation: Contracts272/272 (contracts-launcher-cleanup.trx), Engine448/448 (engine-launcher-cleanup-verified.trx), Client85/85 (client-launcher-cleanup-final.trx). All under the execution evidence root; scoped format checks for all three owning projects passed. Dedicated save envelope preserves legacy validation and active flight continuation; no schema version change.

## Implementation evidence

Remove retired live launcher fields and preserve legacy save validation in dedicated DTO;272 Contracts448 Engine85 Client checks pass

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/contracts-launcher-cleanup.trx and contracts-launcher-cleanup-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
