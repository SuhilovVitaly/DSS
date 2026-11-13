---
epic: EP-maintenance
story: EP-maintenance-US-0017-tactical-minimap
ticket: EP-maintenance-US-0017-TK-0001-render-minimap
title: Отрисовать миникарту и рамку обзора
stage: approved
approval_kind: workflow
implementation_status: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
serves: [AC1, AC2, AC3]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMinimap.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.cs
---

# Отрисовать миникарту и рамку обзора

[User story](../EP-maintenance-US-0017-tactical-minimap.md)

## Code context

`TacticalBattleScreen.RenderFrame` рисует карту и панели из одного `TacticalBattleFrame`. `TacticalBattleView.Unproject` даёт мировые координаты углов области камеры. Миникарта использует общую проекцию для углов и позиций объектов с сохранением пропорций. Она участвует в hit-testing панелей и размещении подписей.

## Проверки

Headless-проверки Client для состава маркеров, пропорций рамки, размещения и блокировки ввода. Изменение растровых эталонов активного боя проверяется в существующих сценах Classic и Tabletop.

Решение по макету от 13 ноября 2026 года. Миникарта переносится в правый верхний угол и получает ширину правой колонки. Растровые эталоны обновляются для нового расположения правых панелей и шкал в US-0018. Геометрию, масштаб и ввод независимо проверяет `OverlayShellTests`.

## Уточнение интерфейса

Левый клик на миникарте центрирует камеру на выбранной мировой точке с сохранением зума и без изменения приказов. Правая кнопка и колесо над миникартой по-прежнему блокируют ввод на карте.

Headless-проверка прошла, 66/66 тестов, включая 23 растровые сцены. Проверка форматирования изменённых файлов клиента прошла. Результаты: D:/DeepSpaceSaga/artifacts/validation/tactical-battle-hud-refinement/20261113T144625876Z/verify.
