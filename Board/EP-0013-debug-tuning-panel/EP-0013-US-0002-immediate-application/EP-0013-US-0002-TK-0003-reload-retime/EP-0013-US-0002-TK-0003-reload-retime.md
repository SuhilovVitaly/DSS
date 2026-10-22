---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0002-immediate-application
ticket: EP-0013-US-0002-TK-0003-reload-retime
stage: done
layer: Engine
depends_on: EP-0013-US-0002-TK-0002-projectiles-retime
---

# EP-0013-US-0002-TK-0003-reload-retime

Implement the matching supplied revision2 ticket: reload-retime. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Resolved scope 2026-10-22: new SimulationEngine.DebugTuningReloads.cs;
SimulationEngine.DebugTuning.cs; SimulationEngine.PointCombat.cs (selective
completion notification); new Engine.Tests/DebugTuningReloadTests.cs. Four files
instead of three are necessary to preserve exactly-once player reload cues when
shortening finishes a reload immediately. Dependency31ee6113cec56a1a0d12714892e400945e4f15e4
was published and remote verified. Retiming is staged alongside ships/flights.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution 2026-10-22

Torpedo/CM reloads stage `max(now, oldReady-oldDuration+newDuration)` only when
still reloading; already-ready launchers stay ready. Out-of-time-range deadlines
reject atomically. Next launches use the new registry duration. Shortened player
torpedo reloads emit the existing completion event exactly once, with a selective
module filter that cannot replay other modules completing at the same instant.

US2-AC5/AC6: six new cases cover remaining/completed/ready reload, CM reload and
identical seeded command sequences under different snapshot partitions. Affected
regression **118/118 PASS**; Engine build0warnings/errors, scoped format/diff
check **PASS**. Self-review complete, independent review not claimed.

Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`reload-regression-final-20261022T110814872`, `reload-build-20261022T110836400`,
`reload-format-20261022T110824672`.
