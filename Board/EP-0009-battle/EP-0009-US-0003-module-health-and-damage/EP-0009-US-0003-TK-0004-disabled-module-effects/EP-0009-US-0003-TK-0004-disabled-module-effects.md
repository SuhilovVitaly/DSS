---
epic: EP-0009-module-combat
story: EP-0009-US-0003-module-health-and-damage
ticket: EP-0009-US-0003-TK-0004-disabled-module-effects
title: "disabled-module-effects"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0003-TK-0003-ship-destruction"]
files_touched: "ground-before-edit"
serves: ["US3-AC4"]
created: 2026-10-12
---

# EP-0009-US-0003-TK-0004-disabled-module-effects

STATUS: IMPLEMENTED; automated validation PASS; native/game acceptance NOT RUN

Execution scope before edit: command readiness is shared in `SimulationEngine.cs` and called by `SimulationEngine.Combat.cs`, `SimulationEngine.ManualDefense.cs`, `SimulationEngine.Countermeasures.cs`, trade/navigation paths and `SimulationEngine.HostileCombat.cs`; module hits occur in `SimulationEngine.Combat.cs`. Runtime motion and Engine cycle completion are in `RuntimeMotion.cs`/`SimulationEngine.cs`. Rejection codes live in Contracts `CommandResult.cs`; focused Engine tests will be added. No Graphify/game/native runs.

Execution evidence (2026-10-12): command gates now reject destroyed or physically disabled modules with distinct reason codes and restore readiness at the exact timer deadline. Combat hits interrupt an active cycle while preserving inertial motion, clear a disabled PD's pending burst queue, and stop NPC maneuver planning while the engine is disabled; NPC planning resumes after expiry. Hostile Save/Load validates at restored physical time. New `DisabledModuleTests` cover launcher rejection/recovery, destroyed reason, PD rejection and queue cancellation, engine coasting/AI resumption, and real-hit cycle interruption. Engine non-corpus and Client headless suites, solution build, scoped formatter, and diff check recorded in `ImplementationStatus.md`; Graphify/game/native NOT RUN by user instruction. Publication SHA recorded by the next ticket after verified push.

**Задача.**
1. Условие готовности модуля (`CanExecuteModuleCommand`) дополнительно требует `!Destroyed && t ≥ DisabledUntilMs`. Новые коды причин: `ModuleDisabled`, `ModuleDestroyed`.
2. Корабль с выключенным двигателем летит по инерции.
3. Контроллер ИИ EP-0008 пропускает решения о движении, пока двигатель выключен. Решения о стрельбе продолжаются, если аппарат жив.
4. Активный цикл выключенного модуля прерывается.

**Файлы:** `SimulationEngine.cs`, `SimulationEngine.Countermeasures.cs`, файл контроллера ИИ EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/DisabledModuleTests.cs` (новый).
**Тесты:** `Disabled_launcher_rejects_fire`, `Disabled_pd_cancels_burst`, `Disabled_engine_coasts_and_ai_holds`, `Module_back_after_60s`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0003-module-health-and-damage.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
