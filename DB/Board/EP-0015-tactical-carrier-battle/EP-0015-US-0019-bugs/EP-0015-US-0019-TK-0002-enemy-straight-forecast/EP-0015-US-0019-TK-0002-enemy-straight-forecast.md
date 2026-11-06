---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0002-enemy-straight-forecast
stage: approved
layer: client
depends_on: []
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Точка прогноза и кормовой сектор

## Контракт и критерий AC-02

В конце прямого прогноза видна точка. Кормовой сектор тёмно-красный, полупрозрачный, с боковыми границами. В движении он привязан к прогнозу, в стрельбе — к текущей позиции.

## Code context и реализация

Использовать существующие курс, скорость и угол RearArc. Прогноз не раскрывает приказы ИИ. Сохранить текущие радиусы визуализации.

## Tests

Фильтры `BattleMapRenderTests` в соответствующих DeepSpaceSaga.Client.Tests. Проверить изменённые файлы через `git diff --check`.

## Definition of Done

Критерий покрыт проверкой в Dss-Main. Результат проверок записан отдельно от ручной приёмки. Отдельный коммит опубликован в ветке эпика.
