---
epic: EP-0009-module-combat
story: EP-0009-US-0003-module-health-and-damage
ticket: EP-0009-US-0003-TK-0001-module-health-runtime
title: "module-health-runtime"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0002-TK-0004-hostile-ship-classes"]
files_touched: "ground-before-edit"
serves: ["US3-AC1"]
created: 2026-10-12
---

# EP-0009-US-0003-TK-0001-module-health-runtime

STATUS: NOT STARTED

**Задача.**
1. Runtime установленного модуля (`InstalledModuleRuntime`) получает `Armor`, `ArmorMax`, `DisabledUntilMs`, `Destroyed`.
2. Броня берётся из класса брони в сценарии.
3. Новые поля проецируются в snapshot.
4. Hull-состояние и его сборка удаляются (на `99e15a2`: `Combat.cs:10`, `BuildCombatState` `:453-494`).

**Файлы:** `SimulationEngine.cs`, `SimulationEngine.Combat.cs`, `tests/DeepSpaceSaga.Engine.Tests/CombatBootstrapTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/BasicCombatEndToEndTests.cs`.
**Тесты:** `Light_armor_module_starts_60_150`, `Legacy_modules_no_armor`, `No_hull_state`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0003-module-health-and-damage.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
