---
epic: EP-0009-module-combat
story: EP-0009-US-0009-combat-scenarios
ticket: EP-0009-US-0009-TK-0001-scenario-combat-mukha
title: "scenario-combat-mukha"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0008-TK-0003-ai-params-from-scenario"]
files_touched: "ground-before-edit"
serves: ["US9-AC1"]
created: 2026-10-12
---

# EP-0009-US-0009-TK-0001-scenario-combat-mukha

STATUS: NOT STARTED

**Файлы:** `Client/Scenarios/CombatMukha/scenario.json` (новый), `tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs` (новый), `Client/DeepSpaceSaga.Client.csproj` (только если копирование `Scenarios\**` не подхватит новую папку).
**Тесты:** `Mukha_scenario_listed_and_loads`, `Positions_ai_armor_match_spec`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0009-combat-scenarios.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
