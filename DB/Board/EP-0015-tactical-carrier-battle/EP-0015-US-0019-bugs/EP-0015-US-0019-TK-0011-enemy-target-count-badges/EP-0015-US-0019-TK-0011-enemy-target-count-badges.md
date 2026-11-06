---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0011-enemy-target-count-badges
stage: approved
layer: client
depends_on: [EP-0015-US-0019-TK-0004-fire-target-indicators]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Показывать число целей у тэгов

## Контракт AC-11

В планировании стрельбы исправные тэги с доступными целями получают тот же оранжевый счётчик. Считать живые цели противоположной стороны, включая носитель, по публичным дальности и сектору стрельбы.

## Code context и реализация

Вычислять визуальное число по снимку. Incoming содержит только назначенную цель, поэтому его недостаточно. Не открывать дополнительные приказы ИИ и не менять набор целей игрока.

## Проверка

В Dss-Main запустить проверки DeepSpaceSaga.Client.Tests `BattleMapRenderTests|FirePlanningTests` и `git diff --check`. Каждый тикет опубликовать отдельным коммитом. Ручную приёмку фиксировать отдельно.
