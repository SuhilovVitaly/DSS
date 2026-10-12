---
epic: EP-0009-module-combat
story: EP-0009-US-0004-combat-skill-rolls
ticket: EP-0009-US-0004-TK-0003-intercept-roll-skills
title: "intercept-roll-skills"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0004-TK-0002-torpedo-hit-roll"]
files_touched: "ground-before-edit"
serves: ["US4-AC3"]
created: 2026-10-12
---

# EP-0009-US-0004-TK-0003-intercept-roll-skills

STATUS: NOT STARTED

**Задача.**
1. Перехват считается по D06. Атака захватывается при пуске противоракеты, защита — при пуске торпеды.
2. `DefenseLaunchPreview` EP-0008 показывает новый шанс и его разложение.

**Файлы:** `SimulationEngine.Countermeasures.cs`, `SimulationEngine.WeaponOperators.cs`, файл построения превью EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureResolutionTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefensePreviewTests.cs` (EP-0008; адаптировать).
**Тесты:** `Intercept_58_example`, `Preview_matches_resolution`, `Zero_and_hundred_still_draw`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0004-combat-skill-rolls.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
