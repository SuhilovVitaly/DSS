---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0003-save-and-startup
ticket: EP-0013-US-0003-TK-0001-save-field
stage: done
layer: Engine
depends_on: EP-0013-US-0002-TK-0003-reload-retime
---

# EP-0013-US-0003-TK-0001-save-field

Implement the matching supplied revision2 ticket: save-field. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Resolved scope 2026-10-22: ScenarioData.cs, ScenarioLoader.cs,
PointCombatSaveData.cs, PointCombatContinuationValidation.cs,
SimulationEngine.cs, SimulationEngine.PointCombatPersistence.cs,
new SimulationEngine.DebugTuningPersistence.cs, new Engine.Tests/DebugTuningSaveTests.cs.
Also update six existing Engine/Client tests that assert the writer version:
CatalogCompatibilityTests, CombatReportsSaveSchemaTests, TradingEconomySaveSchemaTests,
PointCombatUS0009TK0003Tests, LocalSessionIntegrationTests, PointCombatUS0002TK0005Tests.
This expansion preserves strict integrity and explicitly tests the new writer;
the former four-file cap is superseded by the execution prompt.

Save decision: ScenarioLoader.JsonOptions uses UnmappedMemberHandling.Disallow.
Therefore writer becomes Save19; Save18 is explicitly migrated without a tuning
overlay/provenance, while Save17 and older remain atomically rejected. Restore
catalog+overlay under the world lock before building/validating runtime objects;
on failed staging restore the previous registry. Preserve original launch facts
separately from retimed flight anchors and persist launch module type identity.

Regression-grounded scope addition: SimulationEngine.DebugTuningProjectiles.cs.
Existing catalog-edit continuation intentionally retains captured flight parameters
while future launches use the edited catalog. Retiming must therefore touch only
profile fields actually changed by the debug command, not silently synchronize
every captured flight with the catalog during an unrelated tuning patch.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution 2026-10-22

Save19 writes only nondefault tuning values; default state omits `debugTuning`.
Load stages original catalog + saved overlay before validating/building runtime
objects, rolls the effective registry back on failed preflight, and resets tuning
when a new scenario replaces a session. Retimed flights persist private launch
module identity and original immutable flight facts, validated against the launch
journal separately from the current integration anchor. Captured flights still
survive catalog edits; only explicitly changed profile fields are retimed.
Save18 migrates explicitly without tuning/provenance; Save17 remains rejected.

US3-AC1/AC2: filesystem midflight roundtrip preserves the detonation at3000ms;
old saves with/without an active flight load with defaults. Tests also reject a
tampered original endpoint atomically and verify registry rollback after staging.

- Engine persistence/catalog/debug regression: **492/492 PASS**,48s, under300000ms.
- Final focused integrity checks: **7/7 PASS**.
- Client/LocalClient session save/load regression: **42/42 PASS**.
- Whole solution build: **PASS**,0warnings/errors.
- Scoped format/diff check: **PASS**. Self-review complete; independent review not claimed.

Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`save-regression-20261022T111701314`, `save-integrity-20261022T112005298`,
`save-client-20261022T111846093`, `save-build-20261022T112027576`,
`save-format-20261022T111812819`, `save-integrity-format-20261022T112013256`.
