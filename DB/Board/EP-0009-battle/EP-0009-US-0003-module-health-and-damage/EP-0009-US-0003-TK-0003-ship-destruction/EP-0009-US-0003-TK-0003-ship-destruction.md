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

STATUS: IMPLEMENTED; automated validation PASS; native/game acceptance NOT RUN

Execution scope before edit: `SimulationEngine.Combat.cs` handles impacts/wrecks, `SimulationEngine.cs` stages hull state and Save/Load, `Scenario/CombatSaveData.cs` validates hull continuation, `Dialogue/StationSecuritySystem.cs` handles security death, `SimulationEngine.PlayerDefeat.cs` owns terminal clock. `ObjectLabelRenderer.cs` consumes old hull projection. Engine and Client combat regressions will need migration to D11; list confirmed changes before commit. No Graphify/game/native runs.

Execution evidence (2026-10-12): D11 threshold/critical rules and unified `DestroyShip` replace runtime hull HP for classified ships; station security uses the same wreck/journal/terminal transition. Engine module damage, snapshot, Save/Load and historical combat tests were migrated; Client presentation/loadout/combat tests now assert module events rather than hull HP. New D11 regressions cover 2/9 vs 3/9, both critical roles, Mukha, NPC wreck and one-time player defeat. Engine non-corpus 1956/1956; Client 1859/1859; solution build 0 warnings/errors; scoped formatter and `git diff --check` PASS. The Client total has two fewer parameter cases because completed-impact resume now tests one completed hit; the two-hit wreck/UI path remains in BasicCombatUiFlowTests. Graphify rebuild, game launch, and native runs NOT RUN per user. Publication SHA recorded by the next ticket after verified push.

**Задача.**
1. Правило D11: N — число модулей на начало боя, порог `ceil(N × fraction − 1e-9)`.
2. `DestroyShip` объединяет создание обломка, гибель от охраны станции и терминальный переход игрока EP-0008. Lethal-условие EP-0008 переводится с HP корпуса на D11. Поражение срабатывает один раз, часы останавливаются на событии — это правило EP-0008 сохраняется.
3. Запись журнала `ShipDestroyed` с причиной `critical:<moduleId>` или `threshold:<dead>/<N>`.

**Файлы:** `SimulationEngine.Combat.cs`, `Dialogue/StationSecuritySystem.cs`, файл терминального перехода EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/ShipDestructionRulesTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/PlayerDefeatTests.cs` (EP-0008; адаптировать).
**Тесты:** `Two_of_nine_survives_three_kills`, `Bridge_kills`, `Engine_kills`, `Mukha_one_module_kills`, `Npc_becomes_wreck`, `Player_module_death_triggers_ep8_defeat_once`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0003-module-health-and-damage.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
