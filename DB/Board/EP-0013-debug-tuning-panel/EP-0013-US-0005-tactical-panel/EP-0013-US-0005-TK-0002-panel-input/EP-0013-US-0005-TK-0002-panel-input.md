---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0005-tactical-panel
ticket: EP-0013-US-0005-TK-0002-panel-input
stage: done
layer: Client
depends_on: EP-0013-US-0005-TK-0001-panel-layout
---

# EP-0013-US-0005-TK-0002-panel-input

Implement the matching supplied revision2 ticket: panel-input. See [epic contract](../../Documentation.md).

## Code context and scope

Scope correction before edits: seven files are necessary for the real input
path: panel, screen partial, main screen routing, GameSessionHandle command API,
CombatInputSettings, combat-input.json and input tests. This exceeds the supplied
four-file estimate and is authorized by EpicExecutionPrompt's scope correction.
Add the optional schema1 debugPanel binding now; US6-TK4 owns schema2 migration.
Use SnapshotBuffer.FindCommandResult retained receipts and confirmed Latest
values; pending targets accumulate rapid clicks without mutating the snapshot.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

Implemented configurable F12 toggle, aim-first Escape, +/-/Ctrl/wheel editing,
normal sequenced commands, pending italic values and retained receipt handling.
Mouse routing precedes aim/threat/zoom handling; opening never changes speed.
AC1/AC3/AC5: input tests at supported scales1/1.5 prove command counts and values,
rapid accumulation5→5.5→5.6→6.1, authoritative value remains5 until confirmation,
map capture, alternate binding, close and resource disposal. Bound clamping and
unchanged allocation tests also pass. Affected Client130/130; build0warnings/errors;
scoped format PASS. Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`panel-input-regression-20261022T121145053`, `panel-input-build-20261022T121219112`,
`panel-input-format-20261022T121220289`.
Self-review: no synchronous awaits in rendering; input settings validate collisions.
Native physical-input acceptance remains open.
