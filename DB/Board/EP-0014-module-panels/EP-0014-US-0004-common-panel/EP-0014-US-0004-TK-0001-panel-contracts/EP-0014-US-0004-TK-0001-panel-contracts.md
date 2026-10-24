---
epic: EP-0014-module-panels
story: EP-0014-US-0004-common-panel
ticket: EP-0014-US-0004-TK-0001-panel-contracts
title: panel-contracts
stage: implemented
layer: contracts
depends_on: []
files_touched: 3
serves: [AC-04]
created: 2026-10-22
revision: 1
---

# panel-contracts

## Why
Add optional decimal ArmorPiercingPercent to launcher and optional int DestroyedModuleLimit to own AuthoritativeSnapshot; roundtrip precision and legacy absence.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/CombatSnapshot.cs` | Existing owning implementation or new named test | Add optional decimal ArmorPiercingPercent to launcher and optional int DestroyedModuleLimit to own AuthoritativeSnapshot; roundtrip precision and legacy absence. |
| `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs` | Existing owning implementation or new named test | Add optional decimal ArmorPiercingPercent to launcher and optional int DestroyedModuleLimit to own AuthoritativeSnapshot; roundtrip precision and legacy absence. |
| `tests/DeepSpaceSaga.Contracts.Tests/ModulePanelContractTests.cs` | Existing owning implementation or new named test | Add optional decimal ArmorPiercingPercent to launcher and optional int DestroyedModuleLimit to own AuthoritativeSnapshot; roundtrip precision and legacy absence. |

## Public API after the change
Add optional decimal ArmorPiercingPercent to launcher and optional int DestroyedModuleLimit to own AuthoritativeSnapshot; roundtrip precision and legacy absence. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

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

## Implementation evidence
Added LauncherCombatSnapshot.ArmorPiercingPercent (decimal?) and AuthoritativeSnapshot.DestroyedModuleLimit (int?, player ship only). Optional properties omit null; no save schema change. Reviewed exact diff: appended optional parameters preserve positional consumers, DTO remains immutable, old producers do not invent values.

Validation2026-10-22: focused ModulePanelContractTests2/2; Contracts regression271/271. Commands: dotnet test tests/DeepSpaceSaga.Contracts.Tests --no-restore --filter FullyQualifiedName~ModulePanelContractTests; then --no-build regression, both with absolute results directory and TRX. Owning build succeeded without warnings. Scoped dotnet format verify and git diff --check PASS after CRLF correction. Sandbox run failed testhost connection; elevated reruns passed. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/contracts-panel-elevated.trx, contracts-regression.trx, contracts-format.log. Native/UI: not applicable to schema ticket. Human acceptance and independent review not claimed. Engine population follows TK-0002.
