---
epic: EP-maintenance
story: EP-maintenance-US-0025-debug-kill-all
ticket: EP-maintenance-US-0025-TK-0001-enemy-side-request
stage: approved
implementation_status: implemented
layer: contracts
depends_on: []
---
# EP-maintenance-US-0025-TK-0001-enemy-side-request

Расширить существующий запрос `DebugDestroyTacticalUnitRequest` режимом `AllEnemies`. В этом режиме `UnitId` пуст. Сериализация обычного запроса сохраняет прежнюю форму.

Файлы реализации: `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs`.

Проект проверок: `DeepSpaceSaga.Contracts.Tests`. Интеграционное покрытие: `KillAllOpensLootFromAnActiveEncounter`.
