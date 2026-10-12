---
epic: EP-0009-module-combat
story: EP-0009-US-0001-combat-rules-as-data
ticket: EP-0009-US-0001-TK-0004-module-combat-schema
title: "module-combat-schema"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0001-TK-0003-combat-rules-content"]
files_touched: "ground-before-edit"
serves: ["US1-AC2"]
created: 2026-10-12
---

# EP-0009-US-0001-TK-0004-module-combat-schema

STATUS: IMPLEMENTED; separate publication follows

Execution scope before edit: `src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs`, `EngineContentLoader.cs`, `GameDataRegistry.cs`, `tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs`, this card, `Board/EP-0009-battle/ImplementationStatus.md`, `PublicationStatus.md`. New fields are optional for existing catalog records until US-0002 migrates the production catalog; any supplied value is validated, and fields may only occur on their owning module category. Existing required EP-0008 fields remain required. No Graphify, game, or native runs per current user instruction.

Implementation: all nine schema/DTO fields load through JSON into module definitions; registry validation rejects invalid ranges, unknown combat roles, and category mismatches. Existing countermeasure `countermeasureAccuracy` remains the defense accuracy source. Focused content tests 63/63 after final test addition; non-corpus Engine 1936/1936 before that test-only addition; solution build 0 warnings/errors; scoped format and diff check passed before the final test-only addition and rechecked before publication. Production catalog migration and runtime consumption remain in later tickets. Native/game/Graphify: NOT RUN per user.

**Задача.**
1. В `ModuleTypeDefinition` и DTO загрузчика к полям EP-0008 добавить:
   - торпедный аппарат: `accuracy`, `armorPiercingPercent`, `armorDamagePercent`, `launcherReloadMs`, `torpedoMaxFlightTimeMs`;
   - ПРО: `barrels`, `burstIntervalMs`;
   - двигатель: `maneuverability`;
   - любой модуль: `combatRole`.
2. Расширить валидацию торпедного аппарата и ПРО. На `99e15a2` это `ValidateTorpedoParameters` и `GameDataRegistry.ValidateWeaponRatings`, на HEAD — их наследники из EP-0008-US-0001-TK-0003.
3. Если после EP-0008 остался `*BaseRating`, он удаляется в US-0002-TK-0001.

**Файлы:** `ModuleTypeDefinition.cs`, `EngineContentLoader.cs`, `GameDataRegistry.cs`, `tests/DeepSpaceSaga.Engine.Tests/CombatContentLoaderTests.cs`.
**Тесты:** `Launcher_new_fields_validated_for_launcher_only`, `Barrels_zero_rejected`, `Piercing_over_100_rejected`, `Engine_maneuverability_negative_rejected`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0001-combat-rules-as-data.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
