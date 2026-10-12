---
epic: EP-0009-module-combat
story: EP-0009-US-0012-project-documentation
ticket: EP-0009-US-0012-TK-0001-update-project-documents
title: "update-project-documents"
stage: draft
layer: documentation
depends_on: ["EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native"]
files_touched: "ground-before-edit"
serves: ["US12-AC1"]
created: 2026-10-12
---

# EP-0009-US-0012-TK-0001-update-project-documents

STATUS: NOT STARTED

Составить список документов, где упоминается хотя бы одно из:
- HP корпуса;
- формула EP-0008 «точность − манёвренность»;
- «навыки не влияют»;
- один снаряд на аппарат;
- auto-attempt flag;
- `ship.tetrarch` как единственный повреждаемый класс;
- числа 55/5, 200/100/200 км, 12 км/с, 60 с, 10 с.

Каждый документ обновить или указать, почему он не меняется.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0012-project-documentation.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
