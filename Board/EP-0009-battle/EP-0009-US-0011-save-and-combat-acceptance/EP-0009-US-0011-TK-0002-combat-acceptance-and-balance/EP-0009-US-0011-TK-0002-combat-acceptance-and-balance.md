---
epic: EP-0009-module-combat
story: EP-0009-US-0011-save-and-combat-acceptance
ticket: EP-0009-US-0011-TK-0002-combat-acceptance-and-balance
title: "combat-acceptance-and-balance"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0011-TK-0001-save-format-17"]
files_touched: "ground-before-edit"
serves: ["US11-AC2"]
created: 2026-10-12
---

# EP-0009-US-0011-TK-0002-combat-acceptance-and-balance

STATUS: NOT STARTED

**Файлы:** `tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs` (новый; категория `Balance`, вне быстрого набора), `Board/EP-0009-module-combat/evidence/balance.md`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0011-save-and-combat-acceptance.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
