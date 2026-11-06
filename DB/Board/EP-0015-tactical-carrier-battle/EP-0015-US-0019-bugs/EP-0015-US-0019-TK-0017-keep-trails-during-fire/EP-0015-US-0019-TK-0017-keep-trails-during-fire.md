---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0017-keep-trails-during-fire
stage: approved
layer: client
depends_on: [EP-0015-US-0019-TK-0016-live-movement-trails]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Сохранять траектории во время стрельбы

## Контракт AC-17

Последние пройденные траектории видны при планировании стрельбы, в анимации, повторе и результатах залпа, поверх заливки секторов и под маркерами и вспышками.

## Проверка

Целевые тесты в Dss-Main: `BattleMapRenderTests|ResultsPhaseTests|FirePlanningTests`. Проверить `git diff --check`, отдельно зафиксировать ручную приёмку.
