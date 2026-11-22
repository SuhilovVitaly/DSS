---
epic: EP-maintenance
story: EP-maintenance-US-0026-battle-results-steps
ticket: EP-maintenance-US-0026-TK-0003-results-navigation
stage: approved
implementation_status: implemented
layer: client
depends_on: [EP-maintenance-US-0026-TK-0002-filter-loot-batch]
---
# Последовательность экранов

`BattleResultsScreen.cs`, `BattleResultsScreen.Sources.cs` и `BattleResultsScreen.LootRows.cs` управляют шагами, текущими карточками, групповым сбором и выходом. Заголовки и кнопки добавлены в `Russian.json` и `English.json`. Проект проверок — `DeepSpaceSaga.Client.Tests`. Покрываются пропуск шага ареста, переходы, ограничения при активной добыче, восстановление результатов и подтверждение выхода.
