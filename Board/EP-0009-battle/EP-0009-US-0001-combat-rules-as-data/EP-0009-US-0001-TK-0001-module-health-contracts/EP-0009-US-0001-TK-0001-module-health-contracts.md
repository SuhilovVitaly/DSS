---
epic: EP-0009-module-combat
story: EP-0009-US-0001-combat-rules-as-data
ticket: EP-0009-US-0001-TK-0001-module-health-contracts
title: "module-health-contracts"
stage: draft
layer: contracts
depends_on: ["EP-0008 delivered under user-approved graph/native deferral"]
files_touched: "ground-before-edit"
serves: ["US1-AC1", "US1-AC3"]
created: 2026-10-12
---

# EP-0009-US-0001-TK-0001-module-health-contracts

STATUS: NOT STARTED

**Задача.**
1. `InstalledModuleSnapshot` получает поля `ArmorCurrent`, `ArmorMax`, `StructureCurrent`, `StructureMax`, `DisabledUntilMotionTimeMs?`, `IsDestroyed`, `CombatRole?`.
2. `CombatEventType` получает новые значения после существующих (включая значения EP-0008): `TorpedoHitRoll`, `ModuleHit`, `ModuleDestroyed`, `ShipDestroyed`, `BurstShotSkipped`, `LauncherBreakdown`.
3. `CombatJournalEntry` получает поля `Attack`, `Defense`, `Roll`, `HitModuleId?`, `ArmorBefore/After`, `StructureBefore/After`, `DestroyReason?`. Поля EP-0008 (`Accuracy`, `TargetManeuverability`, `LaunchMode`, `TerminationKind`) остаются; в doc-комментариях их смысл уточняется как составляющих атаки и защиты.
4. Hull-поля удаляются из snapshot (`HullCombatSnapshot` или эквивалент на HEAD).

**Файлы:** файл с `InstalledModuleSnapshot`, `CombatJournalEntry.cs`, `CombatSnapshot.cs`, `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs`, `tests/DeepSpaceSaga.Contracts.Tests/CombatEventTests.cs`.
**Тесты:** `Module_health_fields_roundtrip_json`, `New_event_types_after_ep8_values`, `Journal_roll_breakdown_roundtrip`, `No_hull_fields_in_snapshot`.
**DoD:** изменены только Contracts. Потребители hull, которые сломаются, чинятся в US-0003; это фиксируется в evidence.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0001-combat-rules-as-data.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
