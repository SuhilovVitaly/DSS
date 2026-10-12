---
epic: EP-0009-module-combat
story: EP-0009-US-0010-module-combat-ui
ticket: EP-0009-US-0010-TK-0001-defeat-from-module-rule
title: "defeat-from-module-rule"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0009-TK-0003-scenarios-migrate-schema"]
files_touched: "ground-before-edit"
serves: ["US10-AC1"]
created: 2026-10-12
---

# EP-0009-US-0010-TK-0001-defeat-from-module-rule

STATUS: NOT STARTED

**Задача.** Доказать граничные случаи сквозного пути «D11 → терминальный переход EP-0008 → snapshot поражения». Сам переход сделан в US3-TK3.
**Файлы:** файл терминального перехода EP-0008, `tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefeatClockBoundaryTests.cs` (EP-0008).
**Тесты:** `Threshold_death_stops_clock_at_event`, `Critical_engine_death_once`, `No_events_after_defeat_in_same_interval`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0010-module-combat-ui.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
