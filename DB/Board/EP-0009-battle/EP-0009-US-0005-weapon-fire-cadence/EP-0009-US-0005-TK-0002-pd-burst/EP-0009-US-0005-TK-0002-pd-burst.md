---
epic: EP-0009-module-combat
story: EP-0009-US-0005-weapon-fire-cadence
ticket: EP-0009-US-0005-TK-0002-pd-burst
title: "pd-burst"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0005-TK-0001-launcher-cooldown"]
files_touched: "Contracts defense snapshot; Engine combat scheduler, countermeasures, save validation; Engine/Client fixtures and tests"
serves: ["US5-AC2"]
created: 2026-10-12
---

# EP-0009-US-0005-TK-0002-pd-burst

STATUS: IMPLEMENTED; publication pending.

Execution scope before edit (2026-10-12): `SimulationEngine.Countermeasures.cs` currently launches one countermeasure, marks the torpedo attempted, and stores only `ActiveProjectileId`; `SimulationEngine.Combat.cs` schedules contacts, launches and expiries but no pending barrel event. `DefenseSnapshot.PendingBurstShots` and module `Barrels`/`BurstIntervalMs` are already declared. A real second barrel can overlap the first countermeasure, so this ticket also needs the defense active-flight contract, `Scenario/CountermeasureSaveData.cs` validation/normalization, and affected Engine/Contracts/Client test fixtures. The following TK-0003 will own reload-from-first-shot and the new attempt policy. No Graphify/game/native runs.

Implementation evidence (2026-10-12): first launch queues barrel two for +2000 MotionTimeMs; the combat event loop processes it after contacts/expiry and before new launches, ordered by module ID. It rechecks the live torpedo and module, emits `BurstShotSkipped` when unavailable, and preserves auto/manual mode. Defense Save/Load supports overlapping countermeasure IDs and pending shots, including a target removed before the pending shot; old single-flight saves remain loadable. Production two-barrel and frozen one-barrel tests cover second launch, skip, partitioned Save/Load, and historical behavior. Engine non-corpus 1973/1973; Contracts192/192; Client1859/1859; solution build0 warnings/errors; scoped format/diff PASS. Reload-from-first-shot and attempt policy belong to TK-0003. Game/native NOT RUN, Graphify rebuild SKIPPED by user instruction. Separate publication follows.

**Файлы:** `SimulationEngine.Countermeasures.cs`, `SimulationEngine.Combat.cs` (события очереди), `tests/DeepSpaceSaga.Engine.Tests/PdBurstTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureEndToEndTests.cs`.
**Тесты:** `Second_barrel_after_2s_same_torpedo`, `Skipped_if_torpedo_dead`, `Single_barrel_fires_once`, `Disabled_pd_drops_pending`, `Burst_partition_invariance`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0005-weapon-fire-cadence.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
