---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0009-disabled-drone-cross
stage: done
layer: client
depends_on: [EP-0015-US-0019-TK-0004-fire-target-indicators]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Красный крест выведенного из строя дрона

## Контракт AC-09

Во всех фазах, когда на карте показан выведенный из строя дрон, слева от его подписи виден красный крест размером со стандартный квадрат числа целей. Обозначения сбитых и исправных дронов сохраняются.

## Code context и реализация

Использовать место значка числа целей и тот же базовый размер. Состояние Disabled определяется снимком; маркер не зависит от списка целей.

## Tests и Definition of Done

В Dss-Main проходят целевые проверки DeepSpaceSaga.Client.Tests с фильтром `BattleMapRenderTests|FirePlanningTests` и `git diff --check`. Опубликовать отдельный коммит ветки эпика. Ручную приёмку записать отдельно.

## Исполнение

Коммит `7e5840f9` опубликован. 35/35 в итоговой выборке клиента. Растровая проверка красного креста слева от имени в движении и стрельбе, с сохранением отдельного маркера для сбитых. Ручная приёмка открыта.
