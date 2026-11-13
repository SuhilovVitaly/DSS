---
epic: EP-maintenance
story: EP-maintenance-US-0019-battle-accuracy-metrics
ticket: EP-maintenance-US-0019-TK-0004-chance-display
title: Показ факторов на панелях Tabletop
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-maintenance-US-0019-TK-0002-chance-engine, EP-maintenance-US-0019-TK-0005-chance-locales]
serves: [AC4]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.FireTemplates.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardFireBlock.Slim.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardSalvoBlock.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleFormat.cs
---

# Показ факторов на панелях Tabletop

[История](../EP-maintenance-US-0019-battle-accuracy-metrics.md)

Мелкий шрифт подсказки, факторы выбранной цели и фактического выстрела. Прогноз и завершённый выстрел используют свои снимки.

## Проверка

Целевые проверки проекта, регрессия снимка боя и эталоны Tabletop для видимых изменений.
