---
epic: EP-0009-module-combat
story: EP-0009-US-0005-weapon-fire-cadence
ticket: EP-0009-US-0005-TK-0003-pd-reload-and-attempts
title: "pd-reload-and-attempts"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0005-TK-0002-pd-burst"]
files_touched: "ground-before-edit"
serves: ["US5-AC3"]
created: 2026-10-12
---

# EP-0009-US-0005-TK-0003-pd-reload-and-attempts

STATUS: NOT STARTED

**Задача.**
1. Перезарядка ПРО по D13. Перезарядка EP-0008 «после удаления» и её связь с MissedCoast удаляются.
2. Удаляется auto-attempt flag: поле в runtime торпеды и список в сохранении (сохранение меняется в US-0011).
3. Автоматика работает по D15 с арбитражем EP-0008.

**Файлы:** `SimulationEngine.Countermeasures.cs`, `tests/DeepSpaceSaga.Engine.Tests/AutomaticDefenseTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefenseArbitrationTests.cs` (EP-0008), `tests/DeepSpaceSaga.Engine.Tests/PdReloadTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureBootstrapTests.cs`.
**Тесты:** `Reload_from_first_shot_14_5s`, `Retry_after_miss_allowed`, `No_auto_while_cm_in_flight`, `Arbitration_unchanged`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0005-weapon-fire-cadence.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
