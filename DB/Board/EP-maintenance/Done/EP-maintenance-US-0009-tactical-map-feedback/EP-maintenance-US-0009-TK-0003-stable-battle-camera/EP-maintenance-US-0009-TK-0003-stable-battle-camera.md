---
epic: EP-maintenance
story: EP-maintenance-US-0009-tactical-map-feedback
ticket: EP-maintenance-US-0009-TK-0003-stable-battle-camera
title: Стабильная камера тактической карты
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 5
serves: [AC3]
created: "2026-11-10T12:45:42.415Z"
revision: 1
---

# Стабильная камера тактической карты

## Why

Одна начальная подгонка на новый бой. Execute movement, анимация, фазы, ходы и изменение размера сохраняют центр и масштаб. В текущем бою камера меняется колесом или перетягиванием.

## Decisions

Пункт 3 запроса пользователя от 10 ноября 2026 года.

## Assumptions

Первая подгонка нового боя задаёт начальные условия. Размер окна изменяет видимую область карты.

## Code context

| Файл | Исходный участок | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleCamera.cs` | 25–49 | Подгонять только при смене BattleId, хранить масштаб при изменении размера. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | 109, 154–180 | Убрать подгонку по клавише 0 и аргумент заморозки фазы. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/OverlayCameraInteractionTests.cs` | 12–29 | Проверить пользовательскую камеру, смену фазы, хода и изменение размеров. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PhaseCameraTests.cs` | 12–24 | Проверить первичную подгонку и прохождение команды Execute movement. |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | Раздел о проекции TacticalBattleFrame | Обновить описание управления камерой. |

## Public API after the change

Публичные DTO и transport API сохраняются.

## Implementation steps

1. Подгонять только при смене BattleId, хранить масштаб при изменении размера.
2. Убрать подгонку по клавише 0 и аргумент заморозки фазы.
3. Проверить пользовательскую камеру, смену фазы, хода и изменение размеров.
4. Проверить первичную подгонку и прохождение команды Execute movement.

## Out of scope

Изменения правил движения, расчёта попаданий, приказов Engine и сохранений.

## Invariants

Одна начальная подгонка на новый бой. Execute movement, анимация, фазы, ходы и изменение размера сохраняют центр и масштаб. В текущем бою камера меняется колесом или перетягиванием.

## Tests

Проверить PhaseCameraTests, OverlayCameraInteractionTests в Client.Tests. Запустить из корня DSS, с process-tree cap 300000 мс и абсолютным `--results-directory` в `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/<run>`.

## Definition of Done

Изменения ограничены указанными файлами. Именованные проверки, сборка, scoped format и `git diff --check` выполнены. Результаты записаны до публикации.

## Self-containment check

В контексте указаны владелец отображения или ввода, точка изменения и проверка. Зависимостей от других тикетов нет.

## Статус

Камера подгоняется один раз для каждого боя. Проверены переходы Move → Fire, анимация, смена хода, изменение размеров, масштабирование колесом и перетягивание.

Клиент и тесты собраны. Все 416 проверок тактического боя прошли, scoped format и git diff --check прошли. Результаты в D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/20261110124434331/tactical-final/client.log. Ручная проверка в запущенной игре не проводилась.
