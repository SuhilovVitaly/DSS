---
epic: EP-0009-module-combat
story: EP-0009-US-0002-module-and-ship-catalog
ticket: EP-0009-US-0002-TK-0002-engine-catalog
title: "engine-catalog"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0002-TK-0001-weapon-catalog"]
files_touched: "ground-before-edit"
serves: ["US2-AC1"]
created: 2026-10-12
---

# EP-0009-US-0002-TK-0002-engine-catalog

STATUS: NOT STARTED

**Задача.**
1. `module.engine.basic`: `maneuverability` 10, `maxSpeedMps` 6000 (O2).
2. Новый `module.engine.strizh` со значениями D26.
3. Проверки typeId `"module.engine.basic"` в Engine (на `99e15a2`: `SimulationEngine.cs:2955`, `:3108`) и выбор двигателя контроллером EP-0008 (A05: стабильный ordinal) переводятся на категорию `module.engine`. Тикет расширен на Engine по A12.
4. Регрессия рейсов, топлива и экономики (скорость 6 км/с) и орбиты ИИ EP-0008 (лимиты двигателя).

**Файлы:** `modules-engine.json`, `SimulationEngine.cs`, `tests/DeepSpaceSaga.Engine.Tests/EngineCategoryDispatchTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/VoyageFuelAccountingTests.cs`.
**Тесты:** `Strizh_accepts_engine_commands`, `Basic_engine_six_kms`, регрессия рейсов и орбиты.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0002-module-and-ship-catalog.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
