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

STATUS: IMPLEMENTED; separate publication follows

Execution scope before edit: the two weapon catalog JSON files, Client `TorpedoLauncherContentTests.cs` and `CountermeasureContentTests.cs`, Engine `CountermeasureContentLoaderTests.cs` and any focused regressions still asserting old catalog values, plus this card and EP-0009 status records. These test files extend the planned list because they assert the exact old production values. No module-content `*BaseRating` field exists on HEAD; the EP-0008 runtime operator `BaseRating` and save data are not catalog fields and will be migrated with the combat runtime, not removed in this content-only ticket. Existing scenarios still use basic launchers; single-barrel content is a new selectable catalog type. No Graphify/game/native runs.

Scope adjustment from regression evidence: actual changed paths are the two weapon JSON files; English/Russian locale JSON (new combat event enum broke existing Client locale completeness test); Client content, preview, session, UI, loadout, and pirate scenario tests; Engine legacy-combat fixture and affected save/registry/order tests; this card, parent US-0001 story, ImplementationStatus and PublicationStatus. `CountermeasureContentLoaderTests.cs` was inspected but its independent JSON fixture remains unchanged. The EP-0008 regression registry now pins its original weapon physics, while Client content tests assert shipped EP-0009 data. This preserves meaningful legacy mechanics coverage and makes the new balance data explicit.

Implementation: launcher 40 accuracy, 50 maneuverability, 150 km range, 10 km/s speed, 20% piercing, 100% armor damage, 15 s reload, 120 s TTL; defense 58 accuracy, 150 km ranges, 20 km/s speed, 30 s TTL, 14.5 s reload, 2 barrels/2 s interval; new one-barrel defense variant. Existing operator `BaseRating` remains in EP-0008 runtime/save only, pending runtime migration. Focused Client catalog19/19; Engine non-corpus1944/1944; Client full1857/1857; solution build0 warnings/errors; scoped format and diff checks PASS. First broad Engine88 and Client30 failures were outdated EP-0008 fixture/value assumptions and were resolved without game/native/Graphify runs. Runtime burst/multiflight behavior remains later-ticket work.

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
