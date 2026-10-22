---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0005-tactical-panel
ticket: EP-0013-US-0005-TK-0004-localization
stage: done
layer: Client data
depends_on: EP-0013-US-0005-TK-0003-panel-status-reset
---

# EP-0013-US-0005-TK-0004-localization

Implement the matching supplied revision2 ticket: localization. See [epic contract](../../Documentation.md).

## Code context and scope

Four files: current Data/Locale/Russian.json and English.json plus
DebugTuningLocalizationTests.cs and existing input tests (assert translated
status instead of the previously untranslated key). All Debug labels, status/reasons, profile captions
and the two help caveats are explicit locale entries. Strict locale-key coverage
and actual raster render evidence in both languages supplement AC7; they are
not native-window or human acceptance.

Visual QA found Verdana renders the reset arrow as a missing-glyph box. Expand
to five files before fixing: draw that existing reset control as cached-resource
Skia geometry in DebugTuningPanel.cs, retaining zero per-frame allocations.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

RU/EN complete. Strict static+catalog-derived key coverage fails on missing keys.
Both locales render and preserve zero managed bytes over100 unchanged frames.
Affected Client126/126; Client build0warnings/errors; scoped format PASS.
Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`panel-localization-final-20261022T122206571`, `panel-localization-build-20261022T122236032`,
`panel-localization-format-20261022T122237233`. Visually inspected raster images
`panel-locales-20261022/Russian-top.png` and `English-help.png`; fixed the missing
reset glyph and re-inspected Russian top. Layout/help/default emphasis readable.
These are offscreen raster evidence; native physical-input/human acceptance open.
