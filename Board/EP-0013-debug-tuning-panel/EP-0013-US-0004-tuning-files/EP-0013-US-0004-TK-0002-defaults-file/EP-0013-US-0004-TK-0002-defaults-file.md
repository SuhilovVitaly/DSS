---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0004-tuning-files
ticket: EP-0013-US-0004-TK-0002-defaults-file
stage: done
layer: Client
depends_on: EP-0013-US-0004-TK-0001-file-writer
---

# EP-0013-US-0004-TK-0002-defaults-file

Implement the matching supplied revision2 ticket: defaults-file. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Resolved scope2026-10-22: Engine/Content/EngineContentLoader.cs;
LocalClient/LocalGameSessionConnection.cs; Client/DebugTuningFileWriter.cs;
Client/Program.cs; new Client.Tests/DebugTuningDefaultsFileTests.cs. Five files
instead of two preserve the Client->LocalClient->Engine boundary while loading
original catalog defaults without creating a session or reading an overlay file.
Export once per application launch, atomically, with source:catalog. The current
tuning file and the previous reference file never supply default values.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution2026-10-22

Program exports `DebugTuning/debug-tuning.defaults.json` before opening the window
on every application launch. EngineContentLoader reads only the original registry;
LocalClient returns a graphics-free Contracts snapshot. Client atomically exports
Defaults (never current Values) in schema1 with UTC timestamp/source:catalog.
The current file is untouched, and previous reference-file content is ignored.

US4-AC3: two new tests verify overwrite, invalid current-file independence, all25
values/source tag and use of original defaults from a modified snapshot.
Affected Client regression **44/44 PASS**. Whole solution build0warnings/errors,
scoped formatter and ticket-scoped diff check **PASS**. Self-review complete.
Application Main wiring is source/build verified; native app launch is not claimed.

Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`defaults-regression-20261022T115436652`, `defaults-build-20261022T115647422`,
`defaults-format-20261022T115552643`.
