---
epic: EP-0009-module-combat
story: EP-0009-US-0009-combat-scenarios
ticket: EP-0009-US-0009-TK-0002-scenario-combat-shmel
title: "scenario-combat-shmel"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0009-TK-0001-scenario-combat-mukha"]
files_touched: "ground-before-edit"
serves: ["US9-AC2"]
created: 2026-10-12
---

# EP-0009-US-0009-TK-0002-scenario-combat-shmel

STATUS: NOT STARTED

**Файлы:** `Client/Scenarios/CombatShmel/scenario.json` (новый), `CombatScenariosContentTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileAttackAiTests.cs` (фикстура).
**Тесты:** `Shmel_scenario_loads`, `Shmel_has_single_barrel_pd_and_operator`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0009-combat-scenarios.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
