---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0001-contracts-and-state
ticket: EP-0013-US-0001-TK-0003-apply-command
stage: done
layer: Engine
depends_on: EP-0013-US-0001-TK-0002-tuning-state
---

# EP-0013-US-0001-TK-0003-apply-command

Implement the matching supplied revision2 ticket: apply-command. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Scope correction 2026-10-22: actual compact transport embeds AuthoritativeSnapshot
and SnapshotBuffer.MergeScene merges its deltas. To deliver the required live
values, this integration ticket must cross the Contracts/Engine/Client boundary:
AuthoritativeSnapshot.cs; SimulationEngine.cs; SimulationEngine.Presentation.cs;
new SimulationEngine.DebugTuning.cs; SnapshotBuffer.Presentation.cs; new
DebugTuningCommandTests.cs in Engine.Tests and new DebugTuningTransportTests.cs in
Client.Tests. No graphics dependency enters Engine. This expansion is explicitly
authorized by EpicExecutionPrompt.md. TK2 dependency was published as
67f6c6ab53edc70bf969dd1f0f3cc6e8543a9828 and remote-verified.

Validation-driven compatibility fix in the already scoped SimulationEngine.cs:
minimal historical engine definitions without motion limits must project a null
EngineMotion instead of throwing Nullable.Value. The dialogue regression exposed
this pre-existing projection defect; preserving these supported fixtures is needed
to test and retain debug command acceptance during dialogue. Production definitions
continue publishing their complete limits.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution 2026-10-22

Normal player-command dispatch now owns debug application, before dock/dialogue
gates. Non-running/unloaded/disposed/defeated sessions reject with the debug reason;
invalid patches leave registry/revision unchanged. Successful commands refresh
launcher profile projections without RNG or command identity changes. Existing
command deduplication prevents duplicate application. Full snapshots always carry
the confirmed overlay; compact frames carry only changed revisions, and client
scene merges retain values when the delta omits them. Live motion/flight/reload
retiming remains US2, not claimed by this ticket.

- Engine affected regression **112/112 PASS** (debug state/commands, catalog,
  launcher roundtrip, category dispatch, dialogue).
- Client transport/SnapshotBuffer/presentation regression **23/23 PASS**.
- `dotnet build DeepSpaceSaga.sln --no-restore`: **PASS**, 0 warnings/errors.
- Solution scoped formatter for the seven changed source/test files: **PASS**.
- `git diff --check`: **PASS**. Self-review complete; independent review not claimed.

All dotnet checks use the bounded runner and `--results-directory` under
`D:/DeepSpaceSaga/artifacts/validation/ep0013/`. Evidence runs:
`command-regression-20261022T105054348`, `command-client-transport-20261022T105111070`,
`command-build-20261022T105136937`, `command-format-20261022T105135325`.
The initial failing regression exposed the documented projection compatibility
defect; the complete affected filter passed after its correction.
