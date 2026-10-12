---
epic: EP-0009-module-combat
story: EP-0009-US-0006-torpedo-fire-modes
ticket: EP-0009-US-0006-TK-0003-mode-selector-ui
title: "mode-selector-ui"
stage: draft
layer: client
depends_on: ["EP-0009-US-0006-TK-0002-set-mode-command"]
files_touched: "ground-before-edit"
serves: ["US6-AC3"]
created: 2026-10-12
---

# EP-0009-US-0006-TK-0003-mode-selector-ui

STATUS: NOT STARTED

**Задача.**
1. На панели торпедного аппарата (EP-0006/EP-0008): переключатель трёх режимов, отложенная смена с таймером, состояние «Сломан до дока».
2. Локализация названий режимов.

**Файлы:** `GameSessionScreen.Combat.cs`, панель торпедного аппарата, `Data/Locale/English.json`, `Data/Locale/Russian.json`, `tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0006-torpedo-fire-modes.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
