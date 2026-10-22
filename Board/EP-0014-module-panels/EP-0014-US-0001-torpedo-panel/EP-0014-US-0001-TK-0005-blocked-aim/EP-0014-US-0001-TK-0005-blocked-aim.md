---
epic: EP-0014-module-panels
story: EP-0014-US-0001-torpedo-panel
ticket: EP-0014-US-0001-TK-0005-blocked-aim
title: blocked-aim
stage: implemented
layer: client
depends_on: [EP-0014-US-0001-TK-0004-reload-ring]
files_touched: 3
serves: [AC-06, AC-07]
created: 2026-10-22
revision: 1
---

# blocked-aim

## Why
Allow preparation with unavailable weapon, dim and label reason while preventing launch; red cursor/status feedback.

## Decisions
Execution explicitly authorized by user invoking epic-executor on EP-0014 on2026-10-22. Workflow approval is not human product acceptance.

## Assumptions
Preserve Save19 and migration18. Decimal penetration retains catalog precision. Own destruction limit is root snapshot data, nullable for old producers. Client core is only radius times authoritative fraction. Destroyed weapons may enter aim for preparation; Engine remains final launch authority. Zero-allocation gate covers warmed CommandsPanel.Render with stable inputs, not the entire screen.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Aiming.cs` | Existing owning implementation or new named test | Allow preparation with unavailable weapon, dim and label reason while preventing launch; red cursor/status feedback. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` | Existing owning implementation or new named test | Allow preparation with unavailable weapon, dim and label reason while preventing launch; red cursor/status feedback. |
| `tests/DeepSpaceSaga.Client.Tests/ModulePanelProfileTests.cs` | Existing owning implementation or new named test | Allow preparation with unavailable weapon, dim and label reason while preventing launch; red cursor/status feedback. |

## Public API after the change
Allow preparation with unavailable weapon, dim and label reason while preventing launch; red cursor/status feedback. Exact signatures must be grounded in owning source before implementation. Existing APIs remain unless this ticket explicitly migrates them.

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
Include GameSessionScreen.cs to inject preparation-only visual state and PointCombatUiFixture.cs for real unavailable-module scenarios; RU/EN keys mirror localized rejection reasons under Combat.Panel.Reason. Seven implementation files. Aim entry is permitted for destroyed/no-operator/disabled/reloading module with profiles, but map submission still passes PointReadyReason before transport. Button Enabled denotes preparation click; PreparationOnly explicitly denotes dimmed non-launchable state.

Regression scope: update PointCombatUS0011TK0002Tests.cs to explicitly select slow for existing geometric9km/10s/600-coordinate assertions. Medium is the new default; expected geometry assertions remain unchanged. Reload-block fixture waits for admitted readyAt, not merely command-result delivery.

Existing PointCombatUS0011TK0001Tests reload feedback assertion now checks exact Reloading40s, retaining no-command assertions while reflecting required countdown in cursor feedback.

Full owning-suite impact: include Localization.cs for generic fallback of Combat.Panel.Reason keys; update BasicCombatUiFlowTests.cs and PointCombatUS0011TK0004Tests.cs to assert required reload countdown; update CombatPaletteContentTests.cs exact root-key count/default values; update ApproachCameraIntegrationTests.cs to explicitly open Navigation after required weapon priority. These preserve independent command/geometry/schema assertions and reflect intentional changed contracts. Full-suite timeout/report failures must be reproduced separately before attributing them.

Additional exact migration: PhaseThreeLocaleTests now checks separate HoverText and retained Ready; PointCombatUS0016TK0001Tests checks localized tab columns/marker and unbracketed binding; PointCombatUS0013TK0002Tests checks operator reference sigma. Small viewport regression revealed reserve128 was stale after hover row addition; corrected to minimum weapon body144, retaining reachable captions/buttons/status at320px.

Full parallel Client run2307 cases:17 failures, including intentional presentation expectations and two runtime timing failures. Focused rerun repaired all except one remaining obsolete text assertion and reload-report wait. Reload-report test passes1/1 in isolation; trading timeout did not reproduce in focused repair. No baseline cause is asserted. Codex managed baseline worktree creation returned Not a git repository because chat workspace is the parent directory; no manual checkout/copy created. Retain failures as evidence and rerun owning suite after integration.

## Implementation evidence

Allow preparation on blocked launchers with red reason feedback and no launch side effects;165 affected checks pass

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-torpedo-story-final.trx and client-torpedo-story-final-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
