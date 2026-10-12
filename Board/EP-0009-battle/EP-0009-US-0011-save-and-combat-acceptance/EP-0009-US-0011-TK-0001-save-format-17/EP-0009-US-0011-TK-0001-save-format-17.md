---
epic: EP-0009-module-combat
story: EP-0009-US-0011-save-and-combat-acceptance
ticket: EP-0009-US-0011-TK-0001-save-format-17
title: "save-format-17"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0010-TK-0003-combat-journal-lines"]
files_touched: "ground-before-edit"
serves: ["US11-AC1"]
created: 2026-10-12
---

# EP-0009-US-0011-TK-0001-save-format-17

STATUS: NOT STARTED

**Файлы:** `Scenario/ScenarioData.cs`, `Scenario/ScenarioLoader.cs`, `Scenario/CombatSaveData.cs`, `Scenario/CountermeasureSaveData.cs`, `SimulationEngine.CombatPersistence.cs`. Тесты по A12, шестым файлом: `tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs`.
**Тесты:** `Roundtrip_mid_burst`, `Roundtrip_disabled_module_timer`, `Roundtrip_broken_overheat`, `Roundtrip_ai_params`, `V16_rejected_cleanly`, `Rng_counters_continue`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0011-save-and-combat-acceptance.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
