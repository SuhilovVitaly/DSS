---
epic: EP-0009-module-combat
story: EP-0009-US-0010-module-combat-ui
ticket: EP-0009-US-0010-TK-0003-combat-journal-lines
title: "combat-journal-lines"
stage: draft
layer: client
depends_on: ["EP-0009-US-0010-TK-0002-module-health-ui"]
files_touched: "ground-before-edit"
serves: ["US10-AC3"]
created: 2026-10-12
---

# EP-0009-US-0010-TK-0003-combat-journal-lines

STATUS: NOT STARTED

**Файлы:** `CombatJournalPanel.cs`, `Data/Locale/Russian.json`, `Data/Locale/English.json`, `tests/DeepSpaceSaga.Client.Tests/CombatJournalPanelTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0010-module-combat-ui.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
