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

STATUS: NOT STARTED

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
