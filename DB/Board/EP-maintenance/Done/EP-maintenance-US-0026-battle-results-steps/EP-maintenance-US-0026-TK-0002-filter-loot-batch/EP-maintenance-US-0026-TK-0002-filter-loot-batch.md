---
epic: EP-maintenance
story: EP-maintenance-US-0026-battle-results-steps
ticket: EP-maintenance-US-0026-TK-0002-filter-loot-batch
stage: approved
implementation_status: implemented
layer: engine
depends_on: [EP-maintenance-US-0026-TK-0001-loot-batch-contract]
---
# Обработка группы движком

`SimulationEngine.LootOperations.cs` запускает только строки запрошенной группы и сохраняет их исходные приоритеты. Сочетание группы с отдельной строкой отклоняется. Проект проверок — `DeepSpaceSaga.Engine.Tests`. В интеграционных тестах клиента отдельно проверяются группы модулей и металлолома.
