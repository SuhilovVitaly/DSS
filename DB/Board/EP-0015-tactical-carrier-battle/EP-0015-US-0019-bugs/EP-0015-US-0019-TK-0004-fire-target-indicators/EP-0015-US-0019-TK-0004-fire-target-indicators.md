---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0004-fire-target-indicators
stage: approved
layer: client
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Число целей и стрелки шанса попадания

## Контракт и критерий AC-04

У каждого боеспособного дрона игрока с целями слева от подписи виден оранжевый квадрат с количеством. Для выбранного стрелка видны тёмно-серые стрелки и шансы всех допустимых вариантов. Назначенная цель сохраняет прежний цвет.

## Code context и реализация

Читать существующий FirePlan. Рисовать назначенную цель поверх остальных стрелок. Зарезервировать ширину значка при размещении подписи.

## Tests

Фильтры `BattleMapRenderTests|FirePlanningTests` в соответствующих DeepSpaceSaga.Client.Tests. Проверить изменённые файлы через `git diff --check`.

## Definition of Done

Критерий покрыт проверкой в Dss-Main. Результат проверок записан отдельно от ручной приёмки. Отдельный коммит опубликован в ветке эпика.
