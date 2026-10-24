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

STATUS: IMPLEMENTED; separate publication follows

Execution scope before edit: `src/DeepSpaceSaga.Client/Data/Modules/Engine/modules-engine.json`, `src/DeepSpaceSaga.Engine/SimulationEngine.cs`, `SimulationEngine.CombatPersistence.cs`, new `tests/DeepSpaceSaga.Engine.Tests/EngineCategoryDispatchTests.cs`, `VoyageFuelAccountingTests.cs`, this card and EP-0009 status reports. Current Engine has no runtime `module.engine.basic` comparison; dispatch already uses command membership/propulsion fields. This ticket makes `module.engine` category explicit in dispatch, speed selection, and hostile save validation, allowing `strizh` without ID-specific branches. D26 leaves fuel, turn-step, and base-cycle unspecified, so Strizh inherits the basic engine's corresponding values. Additional tests asserting 4 km/s or a single engine type will be included and documented if broad regression finds them. No Graphify/game/native runs.

Scope adjustment from actual regressions: `Scenario/SolarSystemGenerator.cs` also selects operational engine Vmax and now uses category metadata. Engine tests for system generation, cluster distances, voyage, hostile pursuit, and type counts, plus Client content/default-system tests, required updates for basic speed 6 km/s and the new Strizh type. Null category is accepted only for legacy programmatically constructed registries; a loaded explicit non-engine category is excluded from propulsion dispatch. An overbroad headless generator filter entered the long corpus and was canceled; the corrected non-corpus focused run passed15/15. No native/game/Graphify runs.

Implementation: basic `maxSpeedMps` 6000 and `maneuverability` 10; Strizh 8000/800/12, mass 4000 and maneuverability 40, with inherited fuel/turn-step/cycle defaults. Actual Strizh command and system Vmax verified. Focused command/approach114/114, Engine non-corpus1946/1946, Client full1857/1857, solution build0 warnings/errors, scoped format/diff PASS. Production scenario loadouts are updated in later tickets; combat maneuverability consumption also remains later runtime work.

**Задача.**
1. `module.engine.basic`: `maneuverability` 10, `maxSpeedMps` 6000 (O2).
2. Новый `module.engine.strizh` со значениями D26.
3. Проверки typeId `"module.engine.basic"` в Engine (на `99e15a2`: `SimulationEngine.cs:2955`, `:3108`) и выбор двигателя контроллером EP-0008 (A05: стабильный ordinal) переводятся на категорию `module.engine`. Тикет расширен на Engine по A12.
4. Регрессия рейсов, топлива и экономики (скорость 6 км/с) и орбиты ИИ EP-0008 (лимиты двигателя).

**Файлы:** `modules-engine.json`, `SimulationEngine.cs`, `tests/DeepSpaceSaga.Engine.Tests/EngineCategoryDispatchTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/VoyageFuelAccountingTests.cs`.
**Тесты:** `Strizh_accepts_engine_commands`, `Basic_engine_six_kms`, регрессия рейсов и орбиты.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0002-module-and-ship-catalog.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
