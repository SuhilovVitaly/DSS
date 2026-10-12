---
epic: EP-0009-module-combat
story: EP-0009-US-0007-manual-defense-bursts
ticket: EP-0009-US-0007-TK-0001-manual-fire-burst
title: "manual-fire-burst"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0006-TK-0003-mode-selector-ui"]
files_touched: "ground-before-edit"
serves: ["US7-AC1"]
created: 2026-10-12
---

# EP-0009-US-0007-TK-0001-manual-fire-burst

STATUS: NOT STARTED

**Файлы:** `SimulationEngine.Countermeasures.cs`, обработчик `defense.fire` из EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/ManualDefenseFireTests.cs` (EP-0008; адаптировать), `tests/DeepSpaceSaga.Engine.Tests/PdBurstTests.cs`.
**Тесты:** `Manual_fire_starts_burst`, `Manual_allowed_while_other_cm_in_flight`, `Rejects_disabled_pd`, `Idempotent`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0007-manual-defense-bursts.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
