---
epic: EP-0014-module-panels
story: EP-0014-US-0005-snapshot-compatibility
ticket: EP-0014-US-0005-TK-0002-legacy-defense-fields
title: legacy-defense-fields
stage: implemented
layer: contracts
depends_on: [EP-0014-US-0005-TK-0001-legacy-launcher-fields]
files_touched: 23
serves: [AC-02, AC-03]
created: 2026-10-22
revision: 1
---

# legacy-defense-fields

## Why
Remove retired defense snapshot fields after consumer migration; preserve save DTOs. Expand atomic constructor migration scope before edits if needed.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs` | Existing owning implementation or new named test | Remove retired defense snapshot fields after consumer migration; preserve save DTOs. Expand atomic constructor migration scope before edits if needed. |
| `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs` | Existing owning implementation or new named test | Remove retired defense snapshot fields after consumer migration; preserve save DTOs. Expand atomic constructor migration scope before edits if needed. |

## Public API after the change
Remove retired defense snapshot fields after consumer migration; preserve save DTOs. Expand atomic constructor migration scope before edits if needed. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

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
CountermeasureSaveData.cs embeds DefenseSnapshot and validates all retired auto/burst/capability values. Introduce Engine-owned DefenseCombatSaveState with the old wire/validation members, then remove those members from live DefenseSnapshot. Capture converts to save envelope with explicit empty retired burst state. Keep active projectile identity, physical deadlines and point profiles.

The original one-layer/five-file plan is superseded for this atomic contracts migration under EpicExecutionPrompt, for the same compile-time/save-validation reason as TK-0001. Allowed adapters: Engine/Scenario/CountermeasureSaveData.cs, SimulationEngine.Countermeasures.cs and SimulationEngine.PointCombat.cs; all typed DefenseSnapshot constructor/retired-member fixtures in matching Contracts/Engine/Client test projects. Existing save DTO mutations for rejection tests remain intact. No automatic defense or flight outcome changes are allowed.

## Final migration inventory

- `src/DeepSpaceSaga.Contracts/CountermeasureSnapshot.cs`
- `src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs`
- `src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs`
- `tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/CountermeasureUiFlowTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/DualDefensePanelLayoutTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/PhaseThreeCombatInfoTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0001Tests.cs`
- `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0001Tests.cs`
- `tests/DeepSpaceSaga.Client.Tests/WeaponHudControlsTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/CountermeasureSnapshotTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/DefenseLaunchPreviewTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/ModulePanelContractTests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0001Tests.cs`
- `tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0001TK0005ContractTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/CountermeasureBootstrapTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/DefensePreviewProjectionTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/ManualCountermeasureFireTests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0001Tests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0003Tests.cs`
- `tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs`

Validation: Contracts273/273 (contracts-defense-cleanup.trx), Engine539/539 including malformed legacy save rejection (engine-defense-cleanup-verified.trx), Client64/64 (client-defense-cleanup.trx). All owning scoped format checks pass. Save-only AutoEnabled/PendingBurstShots/Barrels and other validation members remain in DefenseCombatSaveState; live DTO contains no retired members. No save version or combat balance change.

## Implementation evidence

Remove retired live defense fields with dedicated legacy save validation;273 Contracts539 Engine64 Client checks pass

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/contracts-defense-cleanup.trx and contracts-defense-cleanup-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
