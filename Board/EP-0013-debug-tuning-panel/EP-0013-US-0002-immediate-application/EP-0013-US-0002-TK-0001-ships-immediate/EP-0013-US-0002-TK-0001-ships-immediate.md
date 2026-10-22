---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0002-immediate-application
ticket: EP-0013-US-0002-TK-0001-ships-immediate
stage: done
layer: Engine
depends_on: EP-0013-US-0001-TK-0003-apply-command
---

# EP-0013-US-0002-TK-0001-ships-immediate

Implement the matching supplied revision2 ticket: ships-immediate. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Resolved 2026-10-22 scope: SimulationEngine.DebugTuning.cs; new
SimulationEngine.DebugTuningShips.cs; new Engine.Tests/DebugTuningShipTests.cs.
Dependency US1-TK3 published/remote verified at0930c36d135254555dd3b6689156d2f495de4390.
Stage current-pose reanchoring, speed clamp, turn hold, engine-cycle continuation,
evasion return and NPC segment replacement before publishing the new registry.
Keep command/cycle IDs and RNG unchanged. Snapshot MaxSpeed/EngineMotion already
read the effective registry; docking remains bound to its station's motion.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution 2026-10-22

Stage replacement ships/evasion/NPC dictionaries before the registry commit.
Sample the old pose, clamp speed immediately, preserve position/course and turn
sign, reanchor cycles with their identities, replan Approach under the new angular
limit, and replan active NPC attack segments from current poses. Evasion escape
end is retained; outward/return phases use new limits and a recalculated return
deadline. Nonfinite/unrepresentable evasion completion rejects before publication.
Docked motion remains station-owned; effective engine limits still update.

US2-AC1/AC2 and turn/clamp partition coverage: five new tests. Affected Engine
regression **227/227 PASS**, hard cap300000ms, no timeout. Engine build **PASS**,
0warnings/errors; scoped formatter **PASS** after whitespace corrections;
`git diff --check` **PASS**. Self-review complete, independent review not claimed.
SpeedHold does not exist yet; its replan hook is required by US6-TK3.

Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`ships-regression-20261022T105741076`, `ships-build-20261022T105858810`,
`ships-format-final-20261022T105952233` (stdout/stderr/run metadata).
