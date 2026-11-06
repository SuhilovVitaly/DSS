---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0014-nested-battle-grid
stage: done
layer: client
depends_on: [EP-0015-US-0008-TK-0003-battle-map-render]
files_touched:
  - src/DeepSpaceSaga.Client/UI/GridRenderer.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Масштабируемая сетка поля боя

## Контракт AC-14

Сетка боя использует алгоритм карты солнечной системы. Крупная клетка содержит 5×5 мелких клеток. При отдалении мелкие линии плавно исчезают, а их общие с крупной сеткой границы остаются на месте. Линии привязаны к координатам боя и перемещаются вместе с камерой. Минимальный шаг равен 8 боевым единицам. Фон боя и линейка масштаба сохраняются.

## Реализация и проверка

Переиспользовать GridRenderer для обоих экранов с параметрами проекции, базового шага и фона. Прогнать GridRendererTests, TacticalMapViewTests, BattleMapRenderTests и BattleCameraTests в Dss-Main. Проверить растр у двух порогов видимости и при смещении камеры.

## Исполнение

Сетка боя и карта системы используют один алгоритм. 67/67 целевых тестов прошли. Растр проверен на двух уровнях затухания и при смещении камеры. Ручная приёмка открыта. Журнал `D:/DeepSpaceSaga/artifacts/validation/ep0015-battle-grid/20261106-1793974620600/grid-final.log`.
