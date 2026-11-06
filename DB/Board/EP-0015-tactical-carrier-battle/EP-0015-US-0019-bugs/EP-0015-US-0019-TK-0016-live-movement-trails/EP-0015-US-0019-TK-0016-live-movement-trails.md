---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0016-live-movement-trails
stage: approved
layer: client
depends_on: [EP-0015-US-0019-TK-0015-slower-movement-into-fire]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMovementAnimation.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementAnimationTests.cs
---

# Рисовать след по мере движения

## Контракт AC-16

С первого кадра после начала движения след доходит до текущей позиции дрона и постепенно удлиняется. Непройденная часть маршрута не появляется раньше дрона.

## Проверка

Целевые тесты в Dss-Main: `MovementAnimationTests|BattleMapRenderTests`. Проверить `git diff --check`, отдельно зафиксировать ручную приёмку.
