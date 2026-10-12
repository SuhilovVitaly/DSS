---
epic: EP-0009-module-combat
story: EP-0009-US-0002-module-and-ship-catalog
ticket: EP-0009-US-0002-TK-0003-structure-normalization
title: "structure-normalization"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0002-TK-0002-engine-catalog"]
files_touched: "ground-before-edit"
serves: ["US2-AC2"]
created: 2026-10-12
---

# EP-0009-US-0002-TK-0003-structure-normalization

STATUS: NOT STARTED

**Задача.**
1. `structurePointsMax` = 150 у всех модулей `Data/Modules/**`, включая `living.quarters.mk1.three-cabins` из EP-0008.
2. `combatRole: "bridge"` у мостика.
3. Текущие `structurePoints` > 150 в сценариях исправляются в US-0009-TK-0003. Здесь только составляется список мест.

**Файлы:** JSON модулей (больше 5 — по A12, с обоснованием), `tests/DeepSpaceSaga.Client.Tests/ModuleStructureCatalogTests.cs` (новый).
**Тесты:** `All_modules_structure_150`, `Rule_one_hit_unarmored_two_hits_light`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0002-module-and-ship-catalog.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
