---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0006-manual-maneuver
ticket: EP-0013-US-0006-TK-0005-maneuver-input
stage: done
layer: Client
depends_on: EP-0013-US-0006-TK-0004-input-schema-v2
---

# EP-0013-US-0006-TK-0005-maneuver-input

Implement the matching supplied revision2 ticket: maneuver-input. See [epic contract](../../Documentation.md).

## Code context and scope

Scope correction before edits: Maneuver partial, new ManualAxes partial,
main GameSessionScreen, GameSessionHandle, KeyboardEdgeTracker, ObjectTrailStore,
new ManualManeuverInputTests, existing US0012-TK1 tests and KeyboardEdgeTrackerTests
(nine files). State transitions use physical-key stacks per axis and last-pressed
opposite precedence; aliases do not emit extra start/stop commands. Shift latches
speed only; ready-module recovery resends held keys, focus/session/modal reset
clears all. Bare S reaches screen; Ctrl+S/C retain their existing actions.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

Implemented per-physical-key axis stacks, alias deduplication, opposite-key
fallback, latched Shift until confirmed cap, manual speed latch replacement,
continuous controls outside combat, ready-module recovery, focus/session/modal
release, bare-S braking and preserved Ctrl+S/C. Maneuvers keep aim open and work
with the debug panel; evasion clears axes until a fresh press. No old arrow-cycle
fallback remains. Trail bootstrap excludes active acceleration.
Client affected202/202, solution build0warnings/errors, scoped format PASS.
Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`manual-input-final-20261022T125235548`, `manual-input-build-20261022T125148815`,
`manual-input-format-20261022T125248823`. Self-review caught and fixed minimal
legacy snapshot without module arrays; idle reconciliation exits without lookup.
Ten-second holds and exact command counts tested through KeyboardEdgeTracker,
screen, handle and real Engine. Native physical-key/human acceptance remains open.
