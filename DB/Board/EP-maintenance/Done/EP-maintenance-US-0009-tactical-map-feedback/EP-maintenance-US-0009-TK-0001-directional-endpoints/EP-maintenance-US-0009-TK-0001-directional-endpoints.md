---
epic: EP-maintenance
story: EP-maintenance-US-0009-tactical-map-feedback
ticket: EP-maintenance-US-0009-TK-0001-directional-endpoints
title: Треугольники на концах маршрутов
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 3
serves: [AC1]
created: "2026-11-10T12:45:42.415Z"
revision: 1
---

# Треугольники на концах маршрутов

## Why

Небольшой треугольник в плановой конечной точке ориентирован по конечному курсу. Выбранный маркер выделен, конечные точки по-прежнему перетаскиваются.

## Decisions

Пункт 1 запроса пользователя от 10 ноября 2026 года.

## Assumptions

Курс берётся из конечной позы принятого клиентом прогноза. Размер задан в логических пикселях карты.

## Code context

| Файл | Исходный участок | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Movement.cs` | 29–31 | Заменить квадрат треугольником, ориентированным по Course; сохранить прежний центр и область взаимодействия. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapMovementTests.cs` | 12–35 | Проверить пиксели носа и основания на разных курсах и сохранить проверку перетаскивания. |
| `DB/Documentation/04-Engineering/TacticalBattleSkins.md` | Таблица ролей | Зафиксировать цвет заливки треугольника как параметр скина. |

## Public API after the change

Публичные DTO и transport API сохраняются.

## Implementation steps

1. Заменить квадрат треугольником, ориентированным по Course; сохранить прежний центр и область взаимодействия.
2. Проверить пиксели носа и основания на разных курсах и сохранить проверку перетаскивания.

## Out of scope

Изменения правил движения, расчёта попаданий, приказов Engine и сохранений.

## Invariants

Небольшой треугольник в плановой конечной точке ориентирован по конечному курсу. Выбранный маркер выделен, конечные точки по-прежнему перетаскиваются.

## Tests

Проверить MapMovementTests в Client.Tests. Запустить из корня DSS, с process-tree cap 300000 мс и абсолютным `--results-directory` в `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/<run>`.

## Definition of Done

Изменения ограничены указанными файлами. Именованные проверки, сборка, scoped format и `git diff --check` выполнены. Результаты записаны до публикации.

## Self-containment check

В контексте указаны владелец отображения или ввода, точка изменения и проверка. Зависимостей от других тикетов нет.

## Статус

Конечные маркеры заменены треугольниками по конечному курсу. Проверены три курса, размер и перетаскивание.

Клиент и тесты собраны. Все 416 проверок тактического боя прошли, scoped format и git diff --check прошли. Результаты в D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/20261110124434331/tactical-final/client.log. Ручная проверка в запущенной игре не проводилась.
