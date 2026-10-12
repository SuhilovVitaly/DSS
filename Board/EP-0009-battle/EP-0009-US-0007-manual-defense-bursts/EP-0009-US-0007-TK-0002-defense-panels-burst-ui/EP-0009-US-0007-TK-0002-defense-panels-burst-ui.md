---
epic: EP-0009-module-combat
story: EP-0009-US-0007-manual-defense-bursts
ticket: EP-0009-US-0007-TK-0002-defense-panels-burst-ui
title: "defense-panels-burst-ui"
stage: draft
layer: client
depends_on: ["EP-0009-US-0007-TK-0001-manual-fire-burst"]
files_touched: "ground-before-edit"
serves: ["US7-AC2"]
created: 2026-10-12
---

# EP-0009-US-0007-TK-0002-defense-panels-burst-ui

STATUS: NOT STARTED

**Файлы:** панель ПРО из EP-0008 (на HEAD), `GameSessionScreen.Countermeasures.cs`, `ObjectInfoPanel.cs`, `Data/Locale/Russian.json`, `tests/DeepSpaceSaga.Client.Tests/DefenseCommandPanelTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0007-manual-defense-bursts.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
