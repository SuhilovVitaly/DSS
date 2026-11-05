---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0003-save-and-startup
ticket: EP-0013-US-0003-TK-0002-startup-file
stage: done
layer: Engine + LocalClient
depends_on: EP-0013-US-0003-TK-0001-save-field
---

# EP-0013-US-0003-TK-0002-startup-file

Implement the matching supplied revision2 ticket: startup-file. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Resolved scope2026-10-22: Contracts/DebugTuning.cs and new DebugTuningFileFormat.cs;
new Engine/SimulationEngine.DebugTuningStartup.cs; SimulationEngine.DebugTuning.cs;
SimulationEngine.cs; LocalClient/LocalGameSessionConnection.cs; Client/Program.cs;
new Engine.Tests/DebugTuningStartupTests.cs. The shared JSON codec is in Contracts
so the subsequent Client writer reuses the exact schema without referencing Engine.
Actual SessionConnectionLoader owns background adoption only; production factory
wiring is Program.LocalGameSessionFactory -> LocalGameSessionConnection.
Startup errors travel in optional DebugTuningSnapshot.StartupError and are logged
through a callback to InterfaceLog. Catalog defaults fill omitted file fields.
The file is applied via the normal queued command phase before the connection loop.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution 2026-10-22

Shared schema1 codec rejects unknown/duplicate keys, wrong types, invalid schema,
unknown type/profile/field and nonfinite numbers; Engine retains range/integer
authority. Partial files fill original catalog defaults. Optional `source:catalog`
allows copying the reference file. Startup-only application uses the ordinary
queue phase, deterministic noncolliding command identity and live retiming after
saved state restore. It preserves pending receipts/journal for the first transport
snapshot. Invalid files leave state unchanged, emit path/reason through the
factory logger and carry StartupError for the first-open panel message. No watcher.

US3-AC3/AC4/AC5: 20 new startup cases including actual LocalClient first snapshot,
strict rejection, no live reload, defaults copy, saved-overlay precedence and
pending receipt retention. Final Engine affected regression **128/128 PASS**;
Contracts **264/264 PASS**; Client/LocalClient regression **38/38 PASS**;
solution build **PASS**,0warnings/errors; scoped formatter/diff check **PASS**.
Self-review complete; native panel display remains US5/native acceptance.

Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`startup-regression-final-20261022T113229769`, `startup-client-contracts-20261022T113111835`,
`startup-client-20261022T113114297`, `startup-build-20261022T113259372`,
`startup-format-20261022T112820080`, `startup-format-final-20261022T113258418`.
