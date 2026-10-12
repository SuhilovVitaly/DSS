---
epic: EP-0009-module-combat
story: EP-0009-US-0006-torpedo-fire-modes
ticket: EP-0009-US-0006-TK-0001-mode-runtime
title: "mode-runtime"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0005-TK-0003-pd-reload-and-attempts"]
files_touched: "ground-before-edit"
serves: ["US6-AC1", "US6-AC2"]
created: 2026-10-12
---

# EP-0009-US-0006-TK-0001-mode-runtime

STATUS: NOT STARTED

**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `Combat/FireModeMath.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/TorpedoHitRollTests.cs`.
**Тесты:** `Maneuverability_mode_lowers_intercept_to_48`, `Overheat_breakdown_deterministic`, `Nonstandard_reload_18_75s`, `Broken_launcher_rejects_until_dock`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0006-torpedo-fire-modes.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
