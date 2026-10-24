---
epic: EP-0009-module-combat
story: EP-0009-US-0005-weapon-fire-cadence
ticket: EP-0009-US-0005-TK-0003-pd-reload-and-attempts
title: "pd-reload-and-attempts"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0005-TK-0002-pd-burst"]
files_touched: "Contracts motion snapshot; Engine defense scheduling, manual gating, persistence and validation; affected headless tests"
serves: ["US5-AC3"]
created: 2026-10-12
---

# EP-0009-US-0005-TK-0003-pd-reload-and-attempts

STATUS: IMPLEMENTED; publication pending.

Execution scope before edit (2026-10-12): `SimulationEngine.Countermeasures.cs` starts reload in `RemoveCountermeasure` and gates automatic targets by `CountermeasureAttempted`; `SimulationEngine.ManualDefense.cs` gates on the old reload field. `SimulationEngine.CombatPersistence.cs`, `Scenario/CountermeasureSaveData.cs`, and `ObjectMotionSnapshot` persist the attempted flag/list. D13/D15 therefore require runtime, contract, Save/Load and affected Engine/Client tests beyond the original file list. TK-0002 introduced overlapping active flights and a pending second shot; this ticket must keep its queued timing while moving readiness to first launch. No Graphify/game/native runs.

Implementation evidence (2026-10-12): first barrel sets `ReadyAtMotionTimeMs` at launch to `t1 + max(reload, interval × (barrels − 1))`; removing a countermeasure no longer extends that deadline. A defense becomes Ready when the deadline has passed and no active flight remains. Automatic targeting excludes torpedoes currently pursued by any live guiding countermeasure; a missed/coasting countermeasure no longer permanently marks the torpedo, and a retry can launch after readiness. The obsolete runtime `CountermeasureAttempted` field was removed; v16 Save retains an empty legacy `AttemptedTorpedoIds` collection until the US-0011 schema migration, and old missing `ReadyAt` values are reconstructed on load. Production 14500 ms boundary and retry/partition/Save-Load coverage passed. Engine non-corpus1972/1972, Client1859/1859, Contracts192/192; solution build0 warnings/errors; scoped format/diff PASS. Game/native NOT RUN, Graphify rebuild SKIPPED by user instruction. Separate publication follows.

**Задача.**
1. Перезарядка ПРО по D13. Перезарядка EP-0008 «после удаления» и её связь с MissedCoast удаляются.
2. Удаляется auto-attempt flag: поле в runtime торпеды и список в сохранении (сохранение меняется в US-0011).
3. Автоматика работает по D15 с арбитражем EP-0008.

**Файлы:** `SimulationEngine.Countermeasures.cs`, `tests/DeepSpaceSaga.Engine.Tests/AutomaticDefenseTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefenseArbitrationTests.cs` (EP-0008), `tests/DeepSpaceSaga.Engine.Tests/PdReloadTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureBootstrapTests.cs`.
**Тесты:** `Reload_from_first_shot_14_5s`, `Retry_after_miss_allowed`, `No_auto_while_cm_in_flight`, `Arbitration_unchanged`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0005-weapon-fire-cadence.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
