---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0006-manual-maneuver
ticket: EP-0013-US-0006-TK-0001-speed-hold-contracts
stage: done
layer: Contracts
depends_on: EP-0013-US-0005-TK-0004-localization
---

# EP-0013-US-0006-TK-0001-speed-hold-contracts

Implement the matching supplied revision2 ticket: speed-hold-contracts. See [epic contract](../../Documentation.md).

## Code context and scope

Four files: ShipEngineCommandTypes.cs (SpeedDirection enum and command ID),
PlayerCommand.cs (optional direction/Latched), ObjectMotionSnapshot.cs (optional
signed acceleration and false-by-default latch), SpeedHoldContractTests.cs.
Active is shared with turnHold; new optional fields omit null/default JSON and
do not change ordinary commands/snapshots. Engine ownership masking follows TK3.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

Implemented optional speed direction/latch command payload and signed own-motion
acceleration/latch projection fields. Full Contracts269/269, solution build0warnings/errors,
scoped formatter PASS. Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`speed-contracts-final-20261022T122505090`, `speed-contracts-build-20261022T122427545`
(build run.json), `speed-contracts-format-20261022T122507289`.
Self-review: ordinary JSON omits new absent fields; turn+speed payload roundtrips.
Runtime ownership/save/input acceptance belongs to following tickets.
