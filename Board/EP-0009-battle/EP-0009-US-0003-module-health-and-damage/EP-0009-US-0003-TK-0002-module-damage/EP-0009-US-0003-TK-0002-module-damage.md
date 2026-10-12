---
epic: EP-0009-module-combat
story: EP-0009-US-0003-module-health-and-damage
ticket: EP-0009-US-0003-TK-0002-module-damage
title: "module-damage"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0003-TK-0001-module-health-runtime"]
files_touched: "ground-before-edit"
serves: ["US3-AC2"]
created: 2026-10-12
---

# EP-0009-US-0003-TK-0002-module-damage

STATUS: NOT STARTED

**Задача.**
1. Применение урона в hull заменяется на `ApplyModuleHit(ship, captured, t)`.
2. Выбор модуля: живые модули в ordinal-порядке по `moduleId`, вес `massKg`. Новый поток ГСЧ `combat.hit.location.v1` (`RngStreamNames`), его состояние сохраняется.
3. Урон по D09 с параметрами, захваченными при пуске.
4. Выключение по A05; при структуре ≤ 0 модуль уничтожен.
5. Записи журнала `ModuleHit` и `ModuleDestroyed`.

**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `Combat/ModuleDamageMath.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/ModuleDamageTests.cs` (новый).
**Тесты:** `Damage_split_150_vs_light`, `Unarmored_dies_one_hit`, `Hit_location_by_mass_deterministic`, `Disable_extends_not_accumulates`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0003-module-health-and-damage.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
