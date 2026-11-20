---
epic: EP-maintenance
story: EP-maintenance-US-0025-debug-kill-all
ticket: EP-maintenance-US-0025-TK-0002-resolve-enemy-side
stage: approved
implementation_status: implemented
layer: engine
depends_on: [EP-maintenance-US-0025-TK-0001-enemy-side-request]
---
# EP-maintenance-US-0025-TK-0002-resolve-enemy-side

Уничтожить вражеские носители за одну ревизию. Случайно уничтожить часть активных дронов, затем отключить оставшихся штатной проверкой победы. Победа через эту команду открывает лут в пиратской встрече и в «Начальном бою». Часть запроса с `UnitId` и `AllEnemies` одновременно недопустима.

Файлы реализации: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs`.

Проект проверок: `DeepSpaceSaga.Engine.Tests`. Интеграционное покрытие: `KillAllOpensLootFromAnActiveEncounter`.
