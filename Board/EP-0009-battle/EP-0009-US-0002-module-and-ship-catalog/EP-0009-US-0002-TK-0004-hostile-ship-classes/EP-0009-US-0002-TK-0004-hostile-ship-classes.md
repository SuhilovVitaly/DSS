---
epic: EP-0009-module-combat
story: EP-0009-US-0002-module-and-ship-catalog
ticket: EP-0009-US-0002-TK-0004-hostile-ship-classes
title: "hostile-ship-classes"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0002-TK-0003-structure-normalization"]
files_touched: "ground-before-edit"
serves: ["US2-AC3"]
created: 2026-10-12
---

# EP-0009-US-0002-TK-0004-hostile-ship-classes

STATUS: IMPLEMENTED; INTEGRATED SCENARIO ACCEPTANCE OPEN

Execution scope before edit: `Data/Ships/ship-classes.json`, `Content/ShipClassDefinition.cs`, `GameDataRegistry.cs`, `SimulationEngine.Combat.cs`, Client `TetrarchClassContentTests.cs`, Engine `CombatContentLoaderTests.cs`, this card and EP-0009 status records; extend test paths only for confirmed regressions. The content schema becomes TypeId-only, with legacy hull runtime state retained temporarily at the old 450-HP fallback until US-0003 migrates live damage/save consumers (A08). New ship class identities precede their US-0009 combat scenario loadouts. Planned hull cells: Mukha two adjacent cells (Strizh + torpedo), Shmel three adjacent cells (same plus one-barrel defense). No dedicated sprites exist; later scenarios use the existing small-ship `ship-zero-class.png` placeholder, recorded here rather than claiming a new asset. No Graphify/game/native runs.

**Задача.**
1. `ship-classes.json`: удалить `hullHitPointsMax`, добавить `ship.mukha` и `ship.shmel`. `ShipClassDefinition` → `(TypeId)`, проверка `> 0` удаляется.
2. Раскладка корпуса: у Мухи 2 модуля, у Шмеля 3. Проверить ограничения клеток.
3. Изображение по `shipClassId`. Если спрайта нет, временно использовать спрайт малого корабля и записать это в evidence.

**Файлы:** `Data/Ships/ship-classes.json`, `Content/ShipClassDefinition.cs`, `GameDataRegistry.cs`, `EngineContentLoader.cs`, `tests/DeepSpaceSaga.Client.Tests/TetrarchClassContentTests.cs`.
**Тесты:** `Ship_classes_have_no_hull_hp`, `Mukha_and_shmel_load`.

## Implementation evidence — 2026-10-12

`ship-classes.json` contains only `typeId` for Tetrarch, Mukha and Shmel; the Engine definition and registry validation no longer include class hull HP. Both new ids load through Settings into the Engine registry. EP-0008 hull combat temporarily reads the scenario/save maximum or defaults to 450 under A08, so existing runtime and saves continue to load while US-0003 replaces hull damage.

The specified Mukha two-cell (Strizh + torpedo) and Shmel three-cell (plus single-barrel defense) loadouts and image bindings require scenario objects; no scenario currently references either class. US-0009 scenario tickets own those objects and will verify occupancy limits, module count, and `ship-zero-class.png` placeholder. The sprite exists at `src/DeepSpaceSaga.Client/Images/CelestialObjects/Spacecraft/ship-zero-class.png`; dedicated Mukha/Shmel art is absent. US2-AC3 remains OPEN until those scenario tests. Game/native/Graphify NOT RUN per user.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0002-module-and-ship-catalog.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
