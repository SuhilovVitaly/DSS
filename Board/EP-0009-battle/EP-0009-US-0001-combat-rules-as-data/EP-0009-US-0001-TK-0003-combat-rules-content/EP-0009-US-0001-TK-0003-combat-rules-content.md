---
epic: EP-0009-module-combat
story: EP-0009-US-0001-combat-rules-as-data
ticket: EP-0009-US-0001-TK-0003-combat-rules-content
title: "combat-rules-content"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0001-TK-0002-fire-contracts"]
files_touched: "ground-before-edit"
serves: ["US1-AC1"]
created: 2026-10-12
---

# EP-0009-US-0001-TK-0003-combat-rules-content

STATUS: IMPLEMENTED — content loading/validation; runtime consumption follows

**Задача.**
1. `Settings.json` получает ключи `typeData.combatRules` → `Data/Combat/combat-rules.json` и `typeData.fireModes` → `Data/Combat/fire-modes.json`.
   ```json
   {"chance":{"base":50,"min":0,"max":100},
    "damage":{"hitDisableMs":60000,"hitLocationWeighting":"mass"},
    "shipDestruction":{"criticalRoles":["bridge","engine"],"destroyedModuleFraction":0.3333},
    "armorClasses":{"none":0,"light":60}}
   ```
2. В `fire-modes.json` поля: `modeId`, `displayName`, `accuracyBonus`, `maneuverabilityBonus`, `breakdownChancePercent`, `reloadMultiplier`, `appliesTo[]`. Значения D16. «Стандарт» доступен торпедному аппарату и ПРО, остальные режимы — только торпедному аппарату.
3. `CombatRulesDefinition` и `FireModeDefinition` в `Engine/Content/`. Загрузка в `EngineContentLoader` рядом с `LoadShipClasses`, регистрация в `GameDataRegistry`, валидация по US1-AC1.

**Файлы:** `Engine/Content/CombatRulesDefinition.cs` (новый), `EngineContentLoader.cs`, `GameDataRegistry.cs`, `Client/Settings.json`, `tests/DeepSpaceSaga.Engine.Tests/CombatRulesContentTests.cs` (новый; тесты на временной копии контента, как в `TorpedoLauncherContentTests`).
**Тесты:** `Rules_load`, `Invalid_chance_bounds_rejected`, `Unknown_role_rejected`, `Mode_for_unknown_category_rejected`, `Missing_settings_key_names_file`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0001-combat-rules-as-data.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.

## Grounded scope before implementation — 2026-10-12

TK-0002 2efad4619528e97af27fd1131f4040e5fe8774d8 published/remote verified. Settings paths live in EngineContentLoader.TypeDataPaths, registry properties in GameDataRegistry. Planned scope omits two required JSON files. Exact scope: new src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs; EngineContentLoader.cs; GameDataRegistry.cs; src/DeepSpaceSaga.Client/Settings.json; new src/DeepSpaceSaga.Client/Data/Combat/combat-rules.json and fire-modes.json; new tests/DeepSpaceSaga.Engine.Tests/CombatRulesContentTests.cs; this card, ImplementationStatus.md and PublicationStatus.md. Existing test fixtures omit new keys, so loader keeps absent pair as legacy-test-only empty registry; if either key is declared, both must be present/read/validated. Production Settings declares both. No combat runtime consumption until subsequent tickets.

## Execution evidence — 2026-10-12

Settings declares real `combatRules` and `fireModes` files. EngineContentLoader validates paired paths and reads strict JSON, GameDataRegistry retains rules/modes, and content validation rejects invalid bounds, roles, classes, category targets and missing files. The declared pair is mandatory; older miniature test fixtures omitting both keep an empty registry until their content is migrated. Production uses the pair.

Focused real-content/negative tests9/9 PASS. Full non-corpus Engine1931/1931 PASS before final additional none=0/standard-applicability validation; focused9/9 and solution build0 warnings/errors PASS after it. First scoped format found LF in two new files; normalized to project CRLF, scoped formatter and diff-check PASS. Native/game/Graphify not run. No combat gameplay consumption claimed until later tickets. Self-review found no unresolved loader defect; new files and exact scope were recorded before edits. Separate commit/push follows; SHA recorded after remote verification.
