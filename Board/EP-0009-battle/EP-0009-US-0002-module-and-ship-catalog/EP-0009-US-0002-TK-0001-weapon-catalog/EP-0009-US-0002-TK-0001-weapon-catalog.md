---
epic: EP-0009-module-combat
story: EP-0009-US-0002-module-and-ship-catalog
ticket: EP-0009-US-0002-TK-0001-weapon-catalog
title: "weapon-catalog"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0001-TK-0005-crew-skills-and-armor-schema"]
files_touched: "ground-before-edit"
serves: ["US2-AC1"]
created: 2026-10-12
---

# EP-0009-US-0002-TK-0001-weapon-catalog

STATUS: NOT STARTED

**Задача.**
1. Торпедный аппарат:
   - `accuracy` 40;
   - `torpedoManeuverability` 50 (было 5);
   - урон 150;
   - 10 км/с (было 3), поворот 90;
   - дальность пуска 150 км (было 200);
   - `armorPiercingPercent` 20, `armorDamagePercent` 100;
   - `launcherReloadMs` 15000;
   - `torpedoMaxFlightTimeMs` 120000.
2. ПРО:
   - `accuracy` 58 (было 55);
   - дальность авто/ручная 150/150 (было 100/200, O7);
   - 20 км/с (было 12);
   - `maxFlightTimeMs` 30000 (было 60000);
   - `countermeasureReloadMs` 14500 (было 10000);
   - `barrels` 2, `burstIntervalMs` 2000.
3. Новый модуль `module.countermeasure.launcher.single` («ПРО одноствольная»): те же значения, но `barrels` 1. Категория та же, поэтому команды общие.
4. Удалить `*BaseRating`, если он остался.

**Файлы:** `modules-torpedolauncher.json`, `modules-countermeasurelauncher.json`, `EngineContentLoader.cs` (удаление полей), `tests/DeepSpaceSaga.Client.Tests/TorpedoLauncherContentTests.cs`.
**Тесты:** `Launcher_catalog_matches_ep9`, `Pd_catalog_matches_ep9`, `Single_barrel_pd_exists`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0002-module-and-ship-catalog.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
