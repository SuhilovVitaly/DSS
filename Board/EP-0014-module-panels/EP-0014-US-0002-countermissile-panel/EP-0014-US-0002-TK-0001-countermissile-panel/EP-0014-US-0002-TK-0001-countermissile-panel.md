---
epic: EP-0014-module-panels
story: EP-0014-US-0002-countermissile-panel
ticket: EP-0014-US-0002-TK-0001-countermissile-panel
title: countermissile-panel
stage: implemented
layer: client
depends_on: [EP-0014-US-0001-TK-0005-blocked-aim]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04, AC-05]
created: 2026-10-22
revision: 1
---

# countermissile-panel

## Why
Reuse weapon presentation without core; sigma at10km, common admitted intercept point, profile arrival and unreachable only on valid prediction.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Existing owning implementation or new named test | Reuse weapon presentation without core; sigma at10km, common admitted intercept point, profile arrival and unreachable only on valid prediction. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CounterAimPreview.cs` | Existing owning implementation or new named test | Reuse weapon presentation without core; sigma at10km, common admitted intercept point, profile arrival and unreachable only on valid prediction. |
| `tests/DeepSpaceSaga.Client.Tests/ModulePanelCounterTests.cs` | Existing owning implementation or new named test | Reuse weapon presentation without core; sigma at10km, common admitted intercept point, profile arrival and unreachable only on valid prediction. |

## Public API after the change
Reuse weapon presentation without core; sigma at10km, common admitted intercept point, profile arrival and unreachable only on valid prediction. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

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
Actual preview path is GameSessionScreen.CounterAimPreview.cs. Add RU/EN locale keys. Use ProjectCounterAim for each profile with an optional locked preview point/observed ID so every row shares the live admitted target and reachability model; no hidden Engine data or duplicated interception rules. Base common components already provide10km sigma,50s reload, per-module memory, ring and blocks.

## Implementation evidence

Compare countermeasure profiles at the shared observed interception point;56 checks pass

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-counter-panel-final.trx and client-counter-panel-final-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
