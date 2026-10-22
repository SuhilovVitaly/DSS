---
epic: EP-0014-module-panels
story: EP-0014-US-0003-engine-panel
ticket: EP-0014-US-0003-TK-0001-engine-panel
title: engine-panel
stage: approved
layer: client
depends_on: [EP-0014-US-0002-TK-0001-countermissile-panel]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-10-22
revision: 1
---

# engine-panel

## Why
Render snapshot speed/max bar, acceleration, turn and real durations; unavailable dimming and finite zero limits.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Existing owning implementation or new named test | Render snapshot speed/max bar, acceleration, turn and real durations; unavailable dimming and finite zero limits. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` | Existing owning implementation or new named test | Render snapshot speed/max bar, acceleration, turn and real durations; unavailable dimming and finite zero limits. |
| `tests/DeepSpaceSaga.Client.Tests/ModulePanelEngineTests.cs` | Existing owning implementation or new named test | Render snapshot speed/max bar, acceleration, turn and real durations; unavailable dimming and finite zero limits. |

## Public API after the change
Render snapshot speed/max bar, acceleration, turn and real durations; unavailable dimming and finite zero limits. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

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
