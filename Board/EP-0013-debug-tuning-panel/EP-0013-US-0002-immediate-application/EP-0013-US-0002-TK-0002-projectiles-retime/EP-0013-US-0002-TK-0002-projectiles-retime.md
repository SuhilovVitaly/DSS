---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0002-immediate-application
ticket: EP-0013-US-0002-TK-0002-projectiles-retime
stage: done
layer: Engine
depends_on: EP-0013-US-0002-TK-0001-ships-immediate
---

# EP-0013-US-0002-TK-0002-projectiles-retime

Implement the matching supplied revision2 ticket: projectiles-retime. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Resolved scope 2026-10-22: new SimulationEngine.DebugTuningProjectiles.cs;
SimulationEngine.DebugTuning.cs; SimulationEngine.PointCombat.cs; SimulationEngine.cs;
new Engine.Tests/DebugTuningProjectileTests.cs. Five files are necessary to stage
flight changes, retain original launch/type identity, clear session caches and
release them when projectiles disappear. US2-TK1 dependency published at
7e3e25de2f2593c9950c3b64e56ce531290249df, remote verified. US3-TK1 will persist
the original launch/type provenance before validating retimed saved flights.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution 2026-10-22

Live flight changes are staged before registry publication. Speed changes sample
old P(t), retain the scattered endpoint and use `t+distance/newSpeed`. Radius-only
changes retain the integration anchor/deadline. Launch type and original launch
facts remain private, independent of the mutable flight anchor and surviving the
launcher's disappearance; caches are cleared at session replacement/removal.
Due-now flights use the existing CM-first/ordinal detonation resolver. Foreign
observations retain their restricted schema; identities/scatter RNG are unchanged.

US2-AC3/AC4 and simultaneous-CM ordering: three new tests; affected regression
**107/107 PASS**. Engine build0warnings/errors; scoped format/diff check **PASS**.
Self-review complete, independent review not claimed. Persistence of the private
provenance and saved tuning overlay is the following US3, not completed here.

Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`projectiles-regression-20261022T110420048`, `projectiles-build-20261022T110454327`,
`projectiles-format-20261022T110444643`.
