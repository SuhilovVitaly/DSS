---
epic: EP-0014-module-panels
story: EP-0014-US-0004-common-panel
ticket: EP-0014-US-0004-TK-0005-dead-code
title: dead-code
stage: implemented
layer: client
depends_on: [EP-0014-US-0004-TK-0004-module-health]
files_touched: 3
serves: [AC-05]
created: 2026-10-22
revision: 1
---

# dead-code

## Why
Remove only proven unused OwnModuleDiagram/ActivePanels/HoveredWeaponPanel consumers after semantic search.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Existing owning implementation or new named test | Remove only proven unused OwnModuleDiagram/ActivePanels/HoveredWeaponPanel consumers after semantic search. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` | Existing owning implementation or new named test | Remove only proven unused OwnModuleDiagram/ActivePanels/HoveredWeaponPanel consumers after semantic search. |
| `tests/DeepSpaceSaga.Client.Tests/ModulePanelHealthTests.cs` | Existing owning implementation or new named test | Remove only proven unused OwnModuleDiagram/ActivePanels/HoveredWeaponPanel consumers after semantic search. |

## Public API after the change
Remove only proven unused OwnModuleDiagram/ActivePanels/HoveredWeaponPanel consumers after semantic search. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

## Implementation steps
Read current owning functions and matching tests; implement the stated result; preserve neighboring behaviors; add observable production-path checks.

## Out of scope
Combat balance, hidden NPC futures, unrelated refactors and save-version changes.

## Invariants
Engine owns outcomes/RNG/commands; Client uses admitted immutable snapshots and shared Motion math. Render performs no synchronous Engine access. No new hidden information.

## Tests
Owning project: tests/DeepSpaceSaga.Client.Tests. Focus named ModulePanel tests, then affected regression; build, scoped format and git diff --check. Test processes capped at300000ms, absolute results directory under D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/.

## Definition of Done
Implemented criteria and tests pass, diff reviewed, actual evidence recorded, separate ticket commit immediately pushed and remote SHA verified. Native and human gates remain distinct from automated tests.

## Self-containment check
Parent story defines numeric fixtures and full criteria; epic contains source specification. Current Code context must be corrected before any expanded edits, as authorized by EpicExecutionPrompt.

## Scope refinement before implementation
Search confirms ObjectInfoPanel.OwnModuleDiagram never assigned, HoveredWeaponPanel never read, ActivePanels never entered. Removing right-side path also makes ModuleDiagram.Render/PreferredHeight unused; remove those in Controls/ModuleDiagram.cs. Update PointCombatUS0013TK0001Tests.cs and PointCombatUS0020TK0002Tests.cs to assert the preserved image/data behavior rather than an absent member. Five implementation files; CommandsPanel.OwnModuleDiagram remains active and is preserved.

Legacy diagram-only raster tests exercised a renderer never reached by the live screen. Migrated them to current CommandsPanel row health/critical text and retained own/foreign hit,800ms expiry and frozen-disabled assertions; no claim of obsolete right-side blink behavior.

## Implementation evidence

Remove unreachable diagram and panel paths; common-panel regression162 of162 passes

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-common-final.trx and client-common-final-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
