---
epic: EP-0009-module-combat
story: EP-0009-US-0008-hostile-flank-ai
ticket: EP-0009-US-0008-TK-0003-ai-params-from-scenario
title: "ai-params-from-scenario"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0008-TK-0002-flank-approach-math"]
files_touched: "ground-before-edit"
serves: ["US8-AC2", "US8-AC3"]
created: 2026-10-12
---

# EP-0009-US-0008-TK-0003-ai-params-from-scenario

STATUS: NOT STARTED

**Файлы:** `Scenario/ScenarioData.cs` (блок `ai` у NPC), `Scenario/ScenarioLoader.cs` (валидация), файл контроллера ИИ EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/HostileAttackAiTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/PirateScenarioTests.cs`.
**Тесты:** `Mukha_closes_to_50km_and_orbits`, `Flank_offsets_spread_attackers`, `Missing_ai_block_uses_ep8_defaults`, `Partition_invariance`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0008-hostile-flank-ai.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
