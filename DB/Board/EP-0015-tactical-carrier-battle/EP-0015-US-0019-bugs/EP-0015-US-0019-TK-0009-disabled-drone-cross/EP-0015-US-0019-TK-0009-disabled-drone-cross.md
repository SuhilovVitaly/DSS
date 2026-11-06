---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0009-disabled-drone-cross
stage: approved
layer: client
depends_on: [EP-0015-US-0019-TK-0004-fire-target-indicators]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Красный крест выведенного из строя дрона

## Контракт AC-09

В фазе планирования стрельбы у выведенного из строя дрона слева от подписи красный крест размером со стандартный квадрат числа целей. Обозначения сбитых и исправных дронов сохраняются.

## Code context и реализация

Использовать место значка числа целей и тот же базовый размер. Состояние Disabled определяется снимком; маркер не зависит от списка целей.

## Tests и Definition of Done

В Dss-Main проходят целевые проверки DeepSpaceSaga.Client.Tests с фильтром `BattleMapRenderTests|FirePlanningTests` и `git diff --check`. Опубликовать отдельный коммит ветки эпика. Ручную приёмку записать отдельно.
