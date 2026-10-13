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

STATUS: MODULE DAMAGE IMPLEMENTED; LEGACY HULL TRANSITION OPEN

Execution scope before edit: `SimulationEngine.Combat.cs` owns impact and legacy wreck transition; `SimulationEngine.CombatPersistence.cs`, `Scenario/CombatSaveData.cs` own combat continuation; `Rng/RngStreamNames.cs` and a new combat RNG/math helper provide isolated weighted selection and D09 split; Engine tests cover damage and Save/Load. Existing hull/wreck tests require the old transition until TK-0003 consolidates destruction under A08; this ticket will not claim hull removal before that replacement. No Graphify/game/native runs.

**Задача.**
1. Применение урона в hull заменяется на `ApplyModuleHit(ship, captured, t)`.
2. Выбор модуля: живые модули в ordinal-порядке по `moduleId`, вес `massKg`. Новый поток ГСЧ `combat.hit.location.v1` (`RngStreamNames`), его состояние сохраняется.
3. Урон по D09 с параметрами, захваченными при пуске.
4. Выключение по A05; при структуре ≤ 0 модуль уничтожен.
5. Записи журнала `ModuleHit` и `ModuleDestroyed`.

**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `Combat/ModuleDamageMath.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/ModuleDamageTests.cs` (новый).
**Тесты:** `Damage_split_150_vs_light`, `Unarmored_dies_one_hit`, `Hit_location_by_mass_deterministic`, `Disable_extends_not_accumulates`.

## Implementation evidence — 2026-10-12

Impact now selects a living module in ordinal `moduleId` order, weighted by `massKg`, using isolated SplitMix64 stream `combat.hit.location.v1`. State/counter persist in combat continuation and resume after Save/Load; legacy saves without that state derive it from the master seed. Torpedoes capture armor piercing/damage percentages at launch. D09 splits damage between armor and structure; a surviving hit sets `disabledUntilMotionTimeMs` to at least impact time plus the configured 60000 ms, and structure zero marks the module destroyed. `ModuleHit`/`ModuleDestroyed` journal facts include before/after values. Tests cover 150 against light/unarmored modules, deterministic and resumed location selection, and one real light-armor hit with the one-minute disable deadline.

The old hull damage/wreck transition still runs in parallel so existing EP-0008 saves and tests remain valid until TK-0003 replaces ship destruction. Thus `ApplyModuleHit` is active but the hull path is not yet removed, and `Disable_extends_not_accumulates` across two surviving hits is OPEN (the current 150-damage catalog destroys an unarmored module in one hit). TK-0003 must remove hull state and consolidate event order/ship destruction; TK-0004 enforces disabled/destroyed command behavior. This is an intermediate commit under A08, not final US3 acceptance. Graphify/game/native NOT RUN per user.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0003-module-health-and-damage.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
