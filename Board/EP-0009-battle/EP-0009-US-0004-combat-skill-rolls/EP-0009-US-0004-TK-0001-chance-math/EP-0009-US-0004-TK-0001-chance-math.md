---
epic: EP-0009-module-combat
story: EP-0009-US-0004-combat-skill-rolls
ticket: EP-0009-US-0004-TK-0001-chance-math
title: "chance-math"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0003-TK-0004-disabled-module-effects"]
files_touched: "ground-before-edit"
serves: ["US4-AC1"]
created: 2026-10-12
---

# EP-0009-US-0004-TK-0001-chance-math

STATUS: NOT STARTED

**Задача.**
1. Функция шанса EP-0008 (на HEAD — наследник `InterceptionMath.ChanceTenths`) заменяется на `CombatChanceMath.ChanceTenths(attack, defense, rules)`.
2. Убирается обнуление влияния навыков (EP-0008 TK-0005): навыки прибавляются напрямую. Формулу `base × skill/50` из EP-0007 не возвращать.

**Файлы:** `Combat/CombatChanceMath.cs` (переименование), `SimulationEngine.WeaponOperators.cs`, `SimulationEngine.Countermeasures.cs` (вызовы), `tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs` (переписать).
**Тесты:** `Equal_gives_500`, `Clamp_bounds`, `Rules_base_changes_result`, `Skills_change_result`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0004-combat-skill-rolls.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
