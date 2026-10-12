---
epic: EP-0009-module-combat
story: EP-0009-US-0003-module-health-and-damage
ticket: EP-0009-US-0003-TK-0003-ship-destruction
title: "ship-destruction"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0003-TK-0002-module-damage"]
files_touched: "ground-before-edit"
serves: ["US3-AC3"]
created: 2026-10-12
---

# EP-0009-US-0003-TK-0003-ship-destruction

STATUS: NOT STARTED

**Задача.**
1. Правило D11: N — число модулей на начало боя, порог `ceil(N × fraction − 1e-9)`.
2. `DestroyShip` объединяет создание обломка, гибель от охраны станции и терминальный переход игрока EP-0008. Lethal-условие EP-0008 переводится с HP корпуса на D11. Поражение срабатывает один раз, часы останавливаются на событии — это правило EP-0008 сохраняется.
3. Запись журнала `ShipDestroyed` с причиной `critical:<moduleId>` или `threshold:<dead>/<N>`.

**Файлы:** `SimulationEngine.Combat.cs`, `Dialogue/StationSecuritySystem.cs`, файл терминального перехода EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/ShipDestructionRulesTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs` (EP-0008; адаптировать).
**Тесты:** `Two_of_nine_survives_three_kills`, `Bridge_kills`, `Engine_kills`, `Mukha_one_module_kills`, `Npc_becomes_wreck`, `Player_module_death_triggers_ep8_defeat_once`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0003-module-health-and-damage.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
