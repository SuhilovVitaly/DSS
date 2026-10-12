---
epic: EP-0009-module-combat
story: EP-0009-US-0006-torpedo-fire-modes
ticket: EP-0009-US-0006-TK-0002-set-mode-command
title: "set-mode-command"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0006-TK-0001-mode-runtime"]
files_touched: "ground-before-edit"
serves: ["US6-AC3"]
created: 2026-10-12
---

# EP-0009-US-0006-TK-0002-set-mode-command

STATUS: NOT STARTED

**Файлы:** `SimulationEngine.Combat.cs`, `Client/Data/Commands/TorpedoLauncher/commands.json`, `SimulationEngine.cs` (диспетчер), `tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs` (новый).
**Тесты:** `Out_of_combat_immediate`, `In_combat_costs_one_cycle`, `Pd_rejects_non_standard`, `Paused_switch_consistent`, `Duplicate_ignored`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0006-torpedo-fire-modes.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
