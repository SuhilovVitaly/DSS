---
epic: EP-0009-module-combat
story: EP-0009-US-0010-module-combat-ui
ticket: EP-0009-US-0010-TK-0001-defeat-from-module-rule
title: "defeat-from-module-rule"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0009-TK-0003-scenarios-migrate-schema"]
files_touched: "PlayerDefeatTests.cs; DefeatTimeBoundaryTests.cs; Board evidence"
serves: ["US10-AC1"]
created: 2026-10-12
---

# EP-0009-US-0010-TK-0001-defeat-from-module-rule

STATUS: IMPLEMENTED — headless verified, publication pending

## Actual scope before editing

The production `ShipDestroyReason`/`DestroyShip` path already calls `RecordPlayerDefeat`, and EP-0008 clocks/save guards already consume `SessionOutcome`. The historical file is `DefeatTimeBoundaryTests.cs` (not the draft's `DefeatClockBoundaryTests.cs`). Add focused end-to-end boundary assertions there and in `PlayerDefeatTests.cs`; leave production unchanged unless a regression exposes a fault. The synthetic `TorpedoImpactTests` registry deliberately clears combat roles, so critical-role coverage needs a test registry that restores the production engine role while keeping its deterministic weapon fixture.

## Evidence and self-review

The existing terminal path was confirmed without production changes. A three-of-nine module death records `threshold:3/9` and stops at the event millisecond. Restoring the production `engine` critical role to the deterministic test registry proves a one-engine critical death records `critical:MOD-PLAYER-ENGINE-01` and latches exactly one EP-0008 defeat. A large-step regression proves no combat event after the death timestamp, no further clock advancement, and disabled saving. Related Engine17/17, solution build0 warnings/errors, scoped format and diff checks PASS. The defeat window/button interaction remains native NOT RUN by user instruction; Graphify rebuild SKIPPED.

**Задача.** Доказать граничные случаи сквозного пути «D11 → терминальный переход EP-0008 → snapshot поражения». Сам переход сделан в US3-TK3.
**Файлы:** файл терминального перехода EP-0008, `tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefeatClockBoundaryTests.cs` (EP-0008).
**Тесты:** `Threshold_death_stops_clock_at_event`, `Critical_engine_death_once`, `No_events_after_defeat_in_same_interval`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0010-module-combat-ui.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
