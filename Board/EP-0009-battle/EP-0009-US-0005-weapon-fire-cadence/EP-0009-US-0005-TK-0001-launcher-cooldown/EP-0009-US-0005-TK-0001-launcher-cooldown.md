---
epic: EP-0009-module-combat
story: EP-0009-US-0005-weapon-fire-cadence
ticket: EP-0009-US-0005-TK-0001-launcher-cooldown
title: "launcher-cooldown"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0004-TK-0003-intercept-roll-skills"]
files_touched: "ground-before-edit"
serves: ["US5-AC1"]
created: 2026-10-12
---

# EP-0009-US-0005-TK-0001-launcher-cooldown

STATUS: NOT STARTED

**Файлы:** `SimulationEngine.Combat.cs` (guard Busy; на `99e15a2` `:40`, `:65`, `:101`, `:281`), `tests/DeepSpaceSaga.Engine.Tests/TorpedoLaunchTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileFireTests.cs` (EP-0008), `tests/DeepSpaceSaga.Engine.Tests/LauncherCooldownTests.cs` (новый).
**Тесты:** `Second_launch_after_15s_while_first_flying`, `Launch_before_ready_rejected_with_reason`, `Npc_fires_on_cooldown_not_after_flight`, `Idempotent_command_id`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0005-weapon-fire-cadence.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
