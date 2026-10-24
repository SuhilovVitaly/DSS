---
epic: EP-0009-module-combat
story: EP-0009-US-0011-save-and-combat-acceptance
ticket: EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native
title: "simulator-on-game-data-and-native"
stage: draft
layer: tooling
depends_on: ["EP-0009-US-0011-TK-0002-combat-acceptance-and-balance"]
files_touched: "src/DeepSpaceSaga.CombatSim/combatsim/{__init__,__main__,gamedata,core}.py; src/DeepSpaceSaga.CombatSim/tests/test_core.py; src/DeepSpaceSaga.CombatSim/README.md; src/DeepSpaceSaga.CombatSim/.gitignore; DB/Board/EP-0009-battle/evidence/native.md; DB/Board/EP-0009-battle/ImplementationStatus.md; DB/Board/EP-0009-battle/PublicationStatus.md"
serves: ["US11-AC3"]
created: 2026-10-12
---

# EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native

STATUS: SIMULATOR HEADLESS PASS; NATIVE ACCEPTANCE OPEN BY USER INSTRUCTION

**Задача.**
1. `src/DeepSpaceSaga.CombatSim/combatsim/gamedata.py` читает `Data/Modules`, `Data/Combat`, `Data/Ships` и `Scenarios/Combat*`.
2. `Data/CombatDesign/` удаляется.
3. Тесты симулятора обновляются.
4. Нативная матрица по US11-AC3: каждый пункт PASS / FAIL / NOT RUN с причиной.

**Файлы:** `gamedata.py`, `tests/test_core.py`, `README.md` симулятора, удаление `Data/CombatDesign/*.json`, `DB/Board/EP-0009-module-combat/evidence/native.md`.

## Actual scope and evidence

The old `src/DeepSpaceSaga.CombatSim` source and `Client/Data/CombatDesign` tree are absent on this checkout; there is nothing to delete. Created a new independent Python sampler and CLI which load the production `Data/Modules`, `Data/Combat`, `Data/Ships` and both `Scenarios/Combat*` files. The sampler resolves ship/module/operator references, uses current damage, armor, chance, reload, modes and threshold values, and fails if a production data source is missing. It deliberately approximates movement, interception and flight; it is not the lost historical `AttackAI` simulator or the authoritative Engine. The planned evidence path is corrected to the real `DB/Board/EP-0009-battle/evidence` directory.

Headless Python unittest 4/4 PASS; CLI 100 seeds each PASS: new sampler 73/100 player wins for Mukha, 34/100 for Shmel. These are **new model outputs**, not historical or Engine acceptance numbers. The actual Engine 100-seed evidence remains [balance.md](../../evidence/balance.md). The [native matrix](../../evidence/native.md) marks all requested real-window checks NOT RUN by explicit user instruction; US11-AC3 and the story's native acceptance stay OPEN. Graphify rebuild SKIPPED by user instruction.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0011-save-and-combat-acceptance.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
