---
epic: EP-0009-module-combat
story: EP-0009-US-0009-combat-scenarios
ticket: EP-0009-US-0009-TK-0003-scenarios-migrate-schema
title: "scenarios-migrate-schema"
stage: implemented
layer: content-data
depends_on: ["EP-0009-US-0009-TK-0002-scenario-combat-shmel"]
files_touched: "six legacy Client scenarios; ScenarioData.cs; SimulationEngine.cs; SimulationEngine.WeaponOperators.cs; TetrarchLoadoutTests.cs; WeaponOperatorSchemaTests.cs; WeaponOperatorRuntimeTests.cs; AutomaticDefenseTests.cs; CountermeasureResolutionTests.cs; ScenarioEngineTests.cs; ScenarioGroupTranslationTests.cs; Board evidence"
serves: ["US9-AC3"]
created: 2026-10-12
---

# EP-0009-US-0009-TK-0003-scenarios-migrate-schema

STATUS: IMPLEMENTED — focused headless verified, publication pending

Execution note from US-0002-TK-0003: six shipped scenarios already had `structurePoints` 400/180 lowered to150 because the normalized module catalog otherwise made standard bootstrap fail. This ticket must preserve that completed subset and still implement the remaining combat loadouts, armorClass/crew skill schema migration and required scenario validations.

## Actual scope before editing

The six original scenario files still contain `torpedoSkill`/`countermeasureSkill`; the DTO, runtime/save projection and several Engine/Client tests also reference them. The current combat math already reads only `accuracySkill`/`maneuverabilitySkill`, so remove the obsolete fields from schema and runtime together and adapt those fixtures. Set explicit zero new skills on legacy scenario crews to preserve their current effective rating, and retain `none` armor for those scenarios. The `light` Tetrarch rule in US9-AC1/AC2 applies to the new CombatMukha/CombatShmel scenarios only. Existing `structurePoints <= 150` work remains intact. This necessary scope exceeds the draft file count under the epic's authorized A12 expansion.

The first broad Engine run exposed an over-broad application of `light` to the legacy scenarios (invalid armor carryover in market fixtures and behavior changes in battle fixtures), so that edit was corrected before publication. The same run found `ScenarioEngineTests` still expecting six living-quarters/catalog module types; the published four-cabin variant makes the correct count seven. Update that assertion and re-run.

The broad repeat found `ScenarioGroupTranslationTests.AllShippedScenariosCovered` still asserting exactly six shipped scenario directories. Its literal coverage list needs the two published combat scenarios; the six-scenario solar translation matrix remains unchanged because the combat starts are local encounters.

## Evidence and self-review

All six older scenarios now use explicit `accuracySkill`/`maneuverabilitySkill` zero for crew and explicit `armorClass: none` for modules. Previously normalized structure values stay within 150. The production crew DTO/runtime/save projection no longer contain legacy skill fields, and the strict JSON loader rejects them. CombatMukha/CombatShmel alone keep the specified light-armored Tetrarch and skill20 crews. Focused Engine operator/defense38/38, scenario/catalog/market21/21, cross-domain sample4/4, Client scenario36/36, solution build0 warnings/errors, scoped format and diff checks PASS. Three broad Engine attempts were interrupted after multi-minute execution: the first found an initially over-broad armor change and stale passive-type count, the second found a stale six-scenario list, and the third reported no failures before interruption. Confirmed failures were corrected and passed in targeted repeat, but a complete post-fix Engine suite is NOT COMPLETE and remains an epic acceptance check. Native/game NOT RUN, Graphify rebuild SKIPPED per user.

**Файлы:** `Scenarios/{Default, Default_500, Docked, Undocked, MarketProfiles, PlayerShipOnly}/scenario.json` (больше 5 — по A12), `tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs`; удаление устаревших навыков из `ScenarioData.cs` (по A12).
**Тесты:** `All_scenarios_load`, `Player_tetrarch_light_armor`, `No_legacy_skill_fields`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0009-combat-scenarios.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
