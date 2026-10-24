---
epic: EP-0014-module-panels
story: EP-0014-US-0001-torpedo-panel
ticket: EP-0014-US-0001-TK-0001-profile-memory
title: profile-memory
stage: implemented
layer: client
depends_on: [EP-0014-US-0004-TK-0005-dead-code]
files_touched: 4
serves: [AC-02]
created: 2026-10-22
revision: 1
---

# profile-memory

## Why
Remember profile per owner/module/session; configurable medium/wide defaults and invalid/removed-profile fallback.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Existing owning implementation or new named test | Remember profile per owner/module/session; configurable medium/wide defaults and invalid/removed-profile fallback. |
| `src/DeepSpaceSaga.Client/CombatVisualSettings.cs` | Existing owning implementation or new named test | Remember profile per owner/module/session; configurable medium/wide defaults and invalid/removed-profile fallback. |
| `src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json` | Existing owning implementation or new named test | Remember profile per owner/module/session; configurable medium/wide defaults and invalid/removed-profile fallback. |
| `tests/DeepSpaceSaga.Client.Tests/ModulePanelProfileTests.cs` | Existing owning implementation or new named test | Remember profile per owner/module/session; configurable medium/wide defaults and invalid/removed-profile fallback. |

## Public API after the change
Remember profile per owner/module/session; configurable medium/wide defaults and invalid/removed-profile fallback. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

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

## Code context correction before implementation
Actual settings file is src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs (not Client root). Default profile keys are optional root fields in schema1 combat-visuals.json, preserving existing files. Aiming state is keyed by owner/module and cleared with presentation session reset. Test production entry via PointCombatUiFixture; parser and invalid/removed profile fallback share the same resolver.

## Implementation evidence

Remember launcher profiles per owner and session with configurable medium and wide defaults;36 checks pass

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-profiles-memory-final.trx and client-profiles-memory-final-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
