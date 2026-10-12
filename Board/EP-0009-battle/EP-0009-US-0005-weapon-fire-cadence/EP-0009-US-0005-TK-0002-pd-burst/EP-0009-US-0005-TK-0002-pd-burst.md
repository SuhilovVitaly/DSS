---
epic: EP-0009-module-combat
story: EP-0009-US-0005-weapon-fire-cadence
ticket: EP-0009-US-0005-TK-0002-pd-burst
title: "pd-burst"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0005-TK-0001-launcher-cooldown"]
files_touched: "ground-before-edit"
serves: ["US5-AC2"]
created: 2026-10-12
---

# EP-0009-US-0005-TK-0002-pd-burst

STATUS: NOT STARTED

**Файлы:** `SimulationEngine.Countermeasures.cs`, `SimulationEngine.Combat.cs` (события очереди), `tests/DeepSpaceSaga.Engine.Tests/PdBurstTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureEndToEndTests.cs`.
**Тесты:** `Second_barrel_after_2s_same_torpedo`, `Skipped_if_torpedo_dead`, `Single_barrel_fires_once`, `Disabled_pd_drops_pending`, `Burst_partition_invariance`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0005-weapon-fire-cadence.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
