---
epic: EP-maintenance
story: EP-maintenance-US-0010-tactical-readability-and-results
ticket: EP-maintenance-US-0010-TK-0001-held-movement-zone
title: Зона движения при удержании мыши
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
depends_on: []
files_touched: 3
serves: [AC1]
created: "2026-11-10T14:03:31.830Z"
---

# Зона движения при удержании мыши

## Why

Область достижимости выбранного дрона видна при удержании левой кнопки на карте для задания точки или перетаскивания конца маршрута. Отпускание, отмена и смена фазы скрывают область. Прогноз стрельбы остаётся привязан к выбору.

## Decisions

Отдельное исправление по запросу пользователя с коммитом и отправкой в текущую ветку.

## Assumptions

Удержание относится к жесту выбора конечной точки на карте. Сектор прогнозируемого огня сохраняет прежнее правило.

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Movement.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapMovementTests.cs`

## Public API after the change

Форматы команд и сохранений прежние.

## Implementation steps

Изменить указанную точку отрисовки, ввода или генерации, затем проверить соответствующий критерий через существующую фикстуру.

## Out of scope

Изменения боевой формулы, камеры и правил сохранений.

## Invariants

Область достижимости выбранного дрона видна при удержании левой кнопки на карте для задания точки или перетаскивания конца маршрута. Отпускание, отмена и смена фазы скрывают область. Прогноз стрельбы остаётся привязан к выбору.

## Tests

`MapMovementTests` в Client.Tests. Результаты под абсолютным каталогом `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/<run>`. Лимит процесса 300000 мс.

## Definition of Done

Критерий подтверждён целевыми проверками, `git diff --check` проходит, статус и результаты записаны перед публикацией.

## Self-containment check

В контексте указаны файлы реализации и тестов.

## Статус

9/9 MapMovementTests. Проверены удержание левой кнопки на конце пути, отпускание и сброс ввода. Лог: D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/tk1/test.log. Общие растровые эталоны обновляются в завершающем UI-тикете.
