---
epic: EP-maintenance
story: EP-maintenance-US-0019-battle-accuracy-metrics
ticket: EP-maintenance-US-0019-TK-0003-battle-metrics
title: Выгрузка метрик боя
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-maintenance-US-0019-TK-0002-chance-engine]
serves: [AC3]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMetrics.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMetricsTests.cs
---

# Выгрузка метрик боя

[История](../EP-maintenance-US-0019-battle-accuracy-metrics.md)

Обновлять JSON на бой по принятому журналу. Различать попадание, промах, поглощение и пропуск. Экспортировать показатели по сторонам и отдельным выстрелам.

## Проверка

Целевые проверки проекта, регрессия снимка боя и эталоны Tabletop для видимых изменений.
