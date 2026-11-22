---
epic: EP-maintenance
story: EP-maintenance-US-0026-battle-results-steps
ticket: EP-maintenance-US-0026-TK-0001-loot-batch-contract
stage: approved
implementation_status: implemented
layer: contracts
depends_on: []
---
# Выбор группы операций

`StartLootOperationsCommand.Batch` в `src/DeepSpaceSaga.Contracts/BattleLootContracts.cs` принимает группу модулей с расходниками или металлолом. Отсутствующее значение сохраняет прежний запрос. Поле отсутствует в JSON, если равно null. Проект проверок — `DeepSpaceSaga.Contracts.Tests`, интеграционное покрытие — переходы и групповой сбор в `DeepSpaceSaga.Client.Tests`.
