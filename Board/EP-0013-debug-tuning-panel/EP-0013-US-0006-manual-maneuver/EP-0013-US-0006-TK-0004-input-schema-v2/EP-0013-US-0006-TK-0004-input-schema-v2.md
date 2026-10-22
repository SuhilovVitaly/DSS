---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0006-manual-maneuver
ticket: EP-0013-US-0006-TK-0004-input-schema-v2
stage: done
layer: Client
depends_on: EP-0013-US-0006-TK-0003-speed-hold-engine
---

# EP-0013-US-0006-TK-0004-input-schema-v2

Implement the matching supplied revision2 ticket: input-schema-v2. See [epic contract](../../Documentation.md).

## Code context and scope

Scope correction before edits: CombatInputSettings, shipped combat-input.json,
SkiaWindow warning callback, Maneuver partial's binding-array membership adapters,
new CombatInputSchemaV2Tests, and affected point-input regression tests (US0011
TK1/TK2/TK3/TK4 and US0012-TK1). Ten files exceed the estimate but keep the
published schema change buildable and tests aligned with E/Z/C. No stack/Shift
behavior is moved from TK5. v1 scalar arrow defaults expand to v2 aliases;
custom scalar bindings remain custom, conflicts with maneuver axes warn/remap.

Affected fixture seam verified during tests: PointCombatUiFixture.Aim also owns
the old counter key. Include that shared fixture before correcting it (11 files).

Broadened regression exposed US0019-TK5's exact pre-speedHold command-list
assertion. Include that catalog assertion before updating it (12 files).

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

Implemented strict schema2 one/two-key arrays, WASD/arrows, E/Z/C/F12 defaults,
S allowed/I reserved, collision/reserved/unknown/duplicate/required-key checks.
v1 retains custom bindings, expands default arrows, supplies new axes and emits
path+field warnings when old aim/evade keys conflict with maneuvering. Native
startup logs migration warnings. Client affected161/161, build0warnings/errors,
scoped format PASS. Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`input-v2-final-20261022T124355826`, `input-v2-build-20261022T124309643`,
`input-v2-format-20261022T124408048`. Self-review complete; S native delivery,
per-axis stacks and continuous controls outside combat remain TK5.
