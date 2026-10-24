---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0005-tactical-panel
ticket: EP-0013-US-0005-TK-0001-panel-layout
stage: done
layer: Client
depends_on: EP-0013-US-0004-TK-0002-defaults-file
---

# EP-0013-US-0005-TK-0001-panel-layout

Implement the matching supplied revision2 ticket: panel-layout. See [epic contract](../../Documentation.md).

## Code context and scope

Four implementation/test files: new Controls/DebugTuningPanel.cs, new
GameSessionScreen.DebugTuning.cs, existing GameSessionScreen.cs, and
DebugTuningPanelLayoutTests.cs. Snapshot defaults supply the 25 enumerated
parameters (the source's 29 is a counting error already corrected in Board).
The current palette has no `styles.Warning`; use its existing orange accent
for modified values, and record this actual style seam instead of adding a
parallel style system. Cached fonts/paints/text blobs are owned by the panel;
session Closing disposes them, temporary modal deactivation preserves them.
Input/status/reset/localization remain the following three tickets.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

Implemented 25 catalog-backed rows, five collapsible sections, logical 360px
width, clipped scrolling, modified/default values and owned cached Skia resources.
AC2/AC8 automated proof: 4/4 tests, including 100 unchanged Prepare+Render
frames with exactly zero managed bytes; scales 1/1.5/2 verified. Affected Client
regression 78/78; Client build 0 warnings/errors; scoped format PASS.
Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep0013/panel-layout-regression-20261022T120705791`,
`panel-layout-build-20261022T120711059`, `panel-layout-format-20261022T120738579`.
Self-review: resources survive modal deactivation and close with the session;
no LINQ/string formatting/font allocation occurs on unchanged frames. Native
visual acceptance remains open; activation/input comes in TK2.
