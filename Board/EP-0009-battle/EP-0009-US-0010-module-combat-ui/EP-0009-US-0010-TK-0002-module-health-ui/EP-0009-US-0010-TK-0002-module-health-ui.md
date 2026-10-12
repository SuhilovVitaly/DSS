---
epic: EP-0009-module-combat
story: EP-0009-US-0010-module-combat-ui
ticket: EP-0009-US-0010-TK-0002-module-health-ui
title: "module-health-ui"
stage: draft
layer: client
depends_on: ["EP-0009-US-0010-TK-0001-defeat-from-module-rule"]
files_touched: "ground-before-edit"
serves: ["US10-AC2"]
created: 2026-10-12
---

# EP-0009-US-0010-TK-0002-module-health-ui

STATUS: NOT STARTED

**Файлы:** `ObjectInfoPanel.cs`, `ObjectLabelRenderer.cs`, `GameSessionScreen.cs`, `MapView.cs`, `tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0010-module-combat-ui.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
