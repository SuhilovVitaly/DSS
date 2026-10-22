---
epic: EP-0014-module-panels
story: EP-0014-US-0001-torpedo-panel
ticket: EP-0014-US-0001-TK-0002-profile-rows
title: profile-rows
stage: implemented
layer: client
depends_on: [EP-0014-US-0001-TK-0001-profile-memory]
files_touched: 3
serves: [AC-01, AC-04]
created: 2026-10-22
revision: 1
---

# profile-rows

## Why
Render bound keys, localized marker, radius/core and aligned columns with cursor flight time and sigma; range rejection.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Existing owning implementation or new named test | Render bound keys, localized marker, radius/core and aligned columns with cursor flight time and sigma; range rejection. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Existing owning implementation or new named test | Render bound keys, localized marker, radius/core and aligned columns with cursor flight time and sigma; range rejection. |
| `tests/DeepSpaceSaga.Client.Tests/ModulePanelProfileTests.cs` | Existing owning implementation or new named test | Render bound keys, localized marker, radius/core and aligned columns with cursor flight time and sigma; range rejection. |

## Public API after the change
Render bound keys, localized marker, radius/core and aligned columns with cursor flight time and sigma; range rejection. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

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
Include Data/Locale/English.json and Russian.json so symbols/units ship localized; include GameSessionScreen.TorpedoAimPreview.cs to keep aim preview real-time conversion consistent while paused. Seven implementation files. Columns are explicit tab-delimited field slots drawn at fixed offsets, not proportional-space padding. Cursor distance uses the same admitted own pose and world scale as live preview; no damage/rules simulation added.

Numeric fixture correction: current PlayerShipOnly crew accuracy is0, so live sigma at25km is0.75, not specification example0.42 at skill20. Extend tests/DeepSpaceSaga.Client.Tests/PointCombatUiFixture.cs with optional initial accuracySkill override for explicit skill20 fixture; production catalog and scenario remain unchanged. The original expected0.42 assertion is retained.

## Implementation evidence

Render bound profile columns and core radii with cursor flight time and sigma;30 checks pass

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-profile-rows-final.trx and client-profile-rows-final-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
