---
epic: EP-0009-module-combat
story: EP-0009-US-0008-hostile-flank-ai
ticket: EP-0009-US-0008-TK-0001-npc-fire-cadence
title: "npc-fire-cadence"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0007-TK-0002-defense-panels-burst-ui"]
files_touched: "ground-before-edit"
serves: ["US8-AC1"]
created: 2026-10-12
---

# EP-0009-US-0008-TK-0001-npc-fire-cadence

STATUS: NOT STARTED

**Файлы:** файл планировщика огня NPC из EP-0008 (на HEAD), `SimulationEngine.Combat.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileFireTests.cs` (EP-0008; адаптировать), `tests/DeepSpaceSaga.Engine.Tests/NpcTorpedoLifetimeTests.cs` (новый).
**Тесты:** `Npc_fires_every_15s_when_reachable`, `No_fire_when_intercept_unreachable`, `Npc_torpedo_expires_120s`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0008-hostile-flank-ai.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
