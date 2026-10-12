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

STATUS: NOT STARTED

**Задача.**
1. `ship-classes.json`: удалить `hullHitPointsMax`, добавить `ship.mukha` и `ship.shmel`. `ShipClassDefinition` → `(TypeId)`, проверка `> 0` удаляется.
2. Раскладка корпуса: у Мухи 2 модуля, у Шмеля 3. Проверить ограничения клеток.
3. Изображение по `shipClassId`. Если спрайта нет, временно использовать спрайт малого корабля и записать это в evidence.

**Файлы:** `Data/Ships/ship-classes.json`, `Content/ShipClassDefinition.cs`, `GameDataRegistry.cs`, `EngineContentLoader.cs`, `tests/DeepSpaceSaga.Client.Tests/TetrarchClassContentTests.cs`.
**Тесты:** `Ship_classes_have_no_hull_hp`, `Mukha_and_shmel_load`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0002-module-and-ship-catalog.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
