---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0006-manual-maneuver
ticket: EP-0013-US-0006-TK-0003-speed-hold-engine
stage: done
layer: Engine
depends_on: EP-0013-US-0006-TK-0002-hold-motion
---

# EP-0013-US-0006-TK-0003-speed-hold-engine

Implement the matching supplied revision2 ticket: speed-hold-engine. See [epic contract](../../Documentation.md).

## Code context and scope

Scope correction before editing: integration needs SimulationEngine.cs,
SimulationEngine.TurnHold.cs, new SimulationEngine.SpeedHold.cs, RuntimeMotion.cs,
SimulationEngine.Combat.cs, SimulationEngine.Evasion.cs,
SimulationEngine.DebugTuningShips.cs, Scenario/PointCombatContinuationValidation.cs,
Contracts/CommandResult.cs (one malformed-payload reason) and SpeedHoldEngineTests.cs.
Ten files exceed the estimate but close the real runtime, collision-bound,
ownership, interruption and tuning seams. Existing save captures only sampled
pose, not input state; test that invariant without adding a save schema field.
Pause freezes the clock while retaining accepted controls; dialogue/watchdog
still release all controls. Turn/speed hold channels preserve one another.

Further verified seams before edits: update the old pause-cancels-turn test to
the new pause-retains-controls contract; add speedHold to Data/Commands/Engine
and update ScenarioEngineTests' exact command count. Total13 files. The module
command projection must advertise the accepted control for real client input.

The affected regression exposed the category allowlist as a separate data seam:
include Data/module-types.json before adding speedHold there (total14 files).

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

Implemented speedHold dispatch/validation/zero-cost catalog definition, independent
axis starts/releases and Shift cap release, cycle/Approach/evasion cancellation,
held-motion collision bounds and damage interruption, own-only projection and
debug retiming of both channels. Saved pose excludes input and load clears both.
Pause retains controls; focus/watchdog/dialogue release remains. New hold emits
evasion Cancelled using its runtime command identity (not persisted input).
Engine affected304/304; Client149/149; solution build0warnings/errors; scoped
format PASS. Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`speed-engine-regression-20261022T123539686`, `speed-engine-client-20261022T123546532`,
`speed-engine-build-20261022T123652416`, `speed-engine-format-20261022T123658181`.
Self-review: rejected commands preserve state; both orderings and exact caps,
73ms partitions, RNG preservation, cancellation, damage and save/load checked.
Client key stacks/re-enable/resync remain TK5; no native acceptance claimed.
