---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0006-remove-salvo-map-summary
stage: done
layer: client
depends_on: [EP-0015-US-0019-TK-0005-named-wrecks-on-impact]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Убрать всплывающую сводку урона

## Контракт AC-06

Карта не показывает текстовую сводку «броня … корпус …» после залпа. Метки сбитых, краткие индикаторы попаданий, журнал и подробности результатов продолжают отображаться.

## Code context и реализация

TacticalBattleMapRenderer.DrawSalvo рисует сводку из salvo.Damage. Убрать этот слой карты и проверить одинаковый растр при наличии и отсутствии сводки в данных.

## Tests и Definition of Done

В Dss-Main проходят целевые проверки DeepSpaceSaga.Client.Tests с фильтром `BattleMapRenderTests|ForecastTabTests` и `git diff --check`. Опубликовать отдельный коммит ветки эпика. Ручную приёмку записать отдельно.

## Исполнение

Коммит `f7c2b994` опубликован. 9/9 в BattleMapRenderTests и ForecastTabTests. Растр карты идентичен при наличии и отсутствии сводки урона в кадре. Ручная приёмка открыта.
