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

STATUS: MODULE RUNTIME IMPLEMENTED; HULL REMOVAL DEFERRED TO TK-0002 UNDER A08

Execution scope before edit: `SimulationEngine.cs` and `SimulationEngine.Combat.cs` stage runtime and project snapshots; `Scenario/ScenarioData.cs` plus `ScenarioLoader.cs` define/validate persisted module state; focused Engine tests and this card/status records. The existing hull hit path still consumes `_hullCombat` until TK-0002 migrates damage, so under A08 its deletion moves into that migration to keep every per-ticket commit buildable. No Graphify/game/native runs.

**Задача.**
1. Runtime установленного модуля (`InstalledModuleRuntime`) получает `Armor`, `ArmorMax`, `DisabledUntilMs`, `Destroyed`.
2. Броня берётся из класса брони в сценарии.
3. Новые поля проецируются в snapshot.
4. Hull-состояние и его сборка удаляются (на `99e15a2`: `Combat.cs:10`, `BuildCombatState` `:453-494`).

**Файлы:** `SimulationEngine.cs`, `SimulationEngine.Combat.cs`, `tests/DeepSpaceSaga.Engine.Tests/CombatBootstrapTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/BasicCombatEndToEndTests.cs`.
**Тесты:** `Light_armor_module_starts_60_150`, `Legacy_modules_no_armor`, `No_hull_state`.

## Implementation evidence — 2026-10-12

`InstalledModuleRuntime` now holds armor current/max, disable-until physical time and destroyed state; structure current remains the existing `StructurePoints`, with max read from the type. Armor is initialized from `combat-rules.json` by `armorClass`, or from a persisted current value. Snapshot projection includes armor/structure, combat role and disable/destroy flags. Scenario/save fields `armor`, `disabledUntilMotionTimeMs`, `destroyed` preserve these values and validate current armor and finite time. Legacy modules without armor continue at zero. `ModuleHealthBootstrapTests` cover light60/structure150, legacy defaults and JSON Save/Load continuation.

`No_hull_state` is intentionally OPEN: the active impact path still reads `_hullCombat`, so deleting it before replacing that path would break the intermediate ticket build. TK-0002 will replace the damage path and delete hull state/DTO consumers under A08; TK-0001 makes no claim that hull combat is already removed. No Graphify/game/native runs per user.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0003-module-health-and-damage.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
