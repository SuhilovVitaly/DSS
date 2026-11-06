---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0008-offscreen-unit-arrows
stage: approved
layer: client
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Указатели объектов за краем карты

## Контракт AC-08

Дрон в воздухе, выведенный из строя дрон и действующий носитель вне видимой карты получают метку имени и стрелку у границы. Стрелка показывает направление к объекту. Метки уходят при возвращении объектов в видимую область.

## Code context и реализация

Использовать геометрию проекции TacticalBattleView и границы карты без правой панели. Цвет соответствует стороне/состоянию. Плотные метки разнести по краю; не менять камеру и hit testing.

## Tests и Definition of Done

В Dss-Main проходят целевые проверки DeepSpaceSaga.Client.Tests с фильтром `BattleMapRenderTests|BattleCameraTests` и `git diff --check`. Опубликовать отдельный коммит ветки эпика. Ручную приёмку записать отдельно.
