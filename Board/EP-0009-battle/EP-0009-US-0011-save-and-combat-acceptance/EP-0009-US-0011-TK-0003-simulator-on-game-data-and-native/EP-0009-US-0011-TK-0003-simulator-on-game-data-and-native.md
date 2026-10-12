---
epic: EP-0009-module-combat
story: EP-0009-US-0011-save-and-combat-acceptance
ticket: EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native
title: "simulator-on-game-data-and-native"
stage: draft
layer: tooling
depends_on: ["EP-0009-US-0011-TK-0002-combat-acceptance-and-balance"]
files_touched: "ground-before-edit"
serves: ["US11-AC3"]
created: 2026-10-12
---

# EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native

STATUS: NOT STARTED

**Задача.**
1. `src/DeepSpaceSaga.CombatSim/combatsim/gamedata.py` читает `Data/Modules`, `Data/Combat`, `Data/Ships` и `Scenarios/Combat*`.
2. `Data/CombatDesign/` удаляется.
3. Тесты симулятора обновляются.
4. Нативная матрица по US11-AC3: каждый пункт PASS / FAIL / NOT RUN с причиной.

**Файлы:** `gamedata.py`, `tests/test_core.py`, `README.md` симулятора, удаление `Data/CombatDesign/*.json`, `Board/EP-0009-module-combat/evidence/native.md`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0011-save-and-combat-acceptance.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
