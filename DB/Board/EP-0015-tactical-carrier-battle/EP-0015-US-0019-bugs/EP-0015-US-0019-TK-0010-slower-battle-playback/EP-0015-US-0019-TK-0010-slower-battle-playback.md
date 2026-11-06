---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0010-slower-battle-playback
stage: done
layer: client
depends_on: [EP-0015-US-0019-TK-0005-named-wrecks-on-impact]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovementAnimation.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementAnimationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ForecastTabTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs
---

# Замедлить движение и залп

## Контракт AC-10

Движение длится 1350 мс вместо 900, полёт снаряда 975 мс вместо 650. Интервал запуска 570 мс, предел залпа 4500 мс. Порядок попаданий, пропуск, повтор и результат боя сохраняются.

## Code context и реализация

Изменить только клиентский тайминг и перевести проверки фаз на фактические границы анимации.

## Проверка

В Dss-Main запустить проверки DeepSpaceSaga.Client.Tests `MovementAnimationTests|ResultsPhaseTests|BattleNavigationTests|BattleMapRenderTests|FirePlanningTests|ForecastTabTests|SidesAndLogTests` и `git diff --check`. Каждый тикет опубликовать отдельным коммитом. Ручную приёмку фиксировать отдельно.

## Исполнение

Коммит `b0870cef` опубликован. 29/29 проверок движения, стрельбы, навигации и карты. Проверены временные границы, пропуск, повтор и отсутствие повторной команды стрельбы. Ручная приёмка открыта.
