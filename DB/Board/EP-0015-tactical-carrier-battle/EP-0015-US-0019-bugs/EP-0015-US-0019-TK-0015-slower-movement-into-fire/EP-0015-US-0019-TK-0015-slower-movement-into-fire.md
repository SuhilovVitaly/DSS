---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0015-slower-movement-into-fire
stage: approved
layer: client
depends_on: [EP-0015-US-0019-TK-0010-slower-battle-playback]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovementAnimation.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementAnimationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ForecastTabTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs
---

# Замедлить движение перед стрельбой

## Контракт AC-15

Перед ручным или непустым автоматическим залпом дроны движутся с 70% прежней скорости. Клиент показывает те же точки за 1929 мс вместо 1350. Ход без залпа сохраняет обычный тайминг.

## Проверка

Целевые тесты в Dss-Main: `MovementAnimationTests|ResultsPhaseTests|BattleNavigationTests|FirePlanningTests|ForecastTabTests|SidesAndLogTests`. Проверить `git diff --check`, отдельно зафиксировать ручную приёмку.
