---
epic: EP-0009-module-combat
story: EP-0009-US-0009-combat-scenarios
ticket: EP-0009-US-0009-TK-0003-scenarios-migrate-schema
title: "scenarios-migrate-schema"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0009-TK-0002-scenario-combat-shmel"]
files_touched: "ground-before-edit"
serves: ["US9-AC3"]
created: 2026-10-12
---

# EP-0009-US-0009-TK-0003-scenarios-migrate-schema

STATUS: NOT STARTED

**Файлы:** `Scenarios/{Default, Default_500, Docked, Undocked, MarketProfiles, PlayerShipOnly}/scenario.json` (больше 5 — по A12), `tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs`; удаление устаревших навыков из `ScenarioData.cs` (по A12).
**Тесты:** `All_scenarios_load`, `Player_tetrarch_light_armor`, `No_legacy_skill_fields`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0009-combat-scenarios.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
