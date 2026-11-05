---
epic: EP-0009-module-combat
story: EP-0009-US-0001-combat-rules-as-data
ticket: EP-0009-US-0001-TK-0005-crew-skills-and-armor-schema
title: "crew-skills-and-armor-schema"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0001-TK-0004-module-combat-schema"]
files_touched: "ground-before-edit"
serves: ["US1-AC3"]
created: 2026-10-12
---

# EP-0009-US-0001-TK-0005-crew-skills-and-armor-schema

STATUS: IMPLEMENTED; separate publication follows

Execution scope before edit: `Scenario/ScenarioData.cs`, `Scenario/ScenarioLoader.cs`, `SimulationEngine.cs`, `SimulationEngine.WeaponOperators.cs`, `tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorSchemaTests.cs`, new `ScenarioArmorSchemaTests.cs`, this card and EP-0009 status reports. Registry-aware `ScenarioLoader.ValidateAndNormalize` checks armor class against loaded `combatRules`; a standalone JSON read without a registry can check syntax only. Legacy skills remain accepted for EP-0008 until their later runtime removal; new skills default to zero. `armorClass` defaults to `none` and is preserved through runtime/save. No Graphify/game/native runs per user.

Implementation: `accuracySkill`/`maneuverabilitySkill` enter crew schema, runtime, and Save/Load at 0..30; legacy scenario fields default to zero. `armorClass` enters module schema, runtime, and Save/Load; registry-aware validation rejects unknown classes. Existing one-crew-per-module rule remains. Focused19/19; broad non-corpus Engine1944/1944 on clean repeat (one pause timing test failed in first broad pass, then passed individually and on full repeat); solution build0 warnings/errors; scoped format and diff checks PASS. Gameplay use of new skills/armor is in later tickets. Native/game/Graphify NOT RUN per user.

**Задача.**
1. У экипажа появляются `accuracySkill` и `maneuverabilitySkill` (0..30). Навыки, которые EP-0008 оставил «только данными» (`torpedoSkill`/`countermeasureSkill` или их переименования на HEAD), загрузчик продолжает принимать до US-0009-TK-0003, но рантайм их не читает.
2. У установленного модуля появляется необязательное поле `armorClass`, по умолчанию `none`.
3. Валидация в `ScenarioLoader`: неизвестный класс брони или навык вне 0..30 → `ScenarioException`. Правило «один член экипажа на модуль» сохраняется.

**Файлы:** `Scenario/ScenarioData.cs`, `Scenario/ScenarioLoader.cs`, `SimulationEngine.cs` (сборка), `tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorSchemaTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/ScenarioArmorSchemaTests.cs` (новый).
**Тесты:** `Unknown_armor_class_rejected`, `Skill_31_rejected`, `Legacy_scenario_defaults`, `One_crew_per_module_kept`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0001-combat-rules-as-data.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
