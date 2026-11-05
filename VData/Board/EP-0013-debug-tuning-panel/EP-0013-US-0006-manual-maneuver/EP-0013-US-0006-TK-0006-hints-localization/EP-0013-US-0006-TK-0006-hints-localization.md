---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0006-manual-maneuver
ticket: EP-0013-US-0006-TK-0006-hints-localization
stage: done
layer: Client + data
depends_on: EP-0013-US-0006-TK-0005-maneuver-input
---

# EP-0013-US-0006-TK-0006-hints-localization

Implement the matching supplied revision2 ticket: hints-localization. See [epic contract](../../Documentation.md).

## Code context and scope

Scope correction before edits: ThreatPanel and its screen partial, DebugTuningPanel
and its screen partial, RU/EN locales, and ManeuverHintsTests (seven files).
Threat actions show actual counter/evade keys; debug help explains all manual
axes and Shift using actual bindings. Existing launcher aim labels already use
CombatInputs and therefore show E. Cache binding-derived text, localize all prose
and the new malformed-speed reason. Raster proof supplements automated checks.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

Actual binding labels show E/Z/C (or custom R/V/B), launcher aim labels already
follow configuration, help explains aliases/Shift and debug toggle. RU/EN prose
and malformed-speed reason translated. Raster QA found missing arrow glyphs in
Verdana help; localized arrow key names replace them. Inspected corrected
`D:/DeepSpaceSaga/artifacts/validation/ep0013/hints-20261022/Russian.png`.
Client affected133/133; build0warnings/errors; scoped format PASS. Evidence root:
`D:/DeepSpaceSaga/artifacts/validation/ep0013/`: `maneuver-hints-final-20261022T125732255`,
`maneuver-hints-build-20261022T125744633`, `maneuver-hints-format-20261022T125845265`.
Self-review complete. Raster QA does not close native/physical-input/human gates.
