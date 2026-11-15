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
files_touched: 8
serves: [AC3]
created: "2026-11-10T12:45:42.415Z"
revision: 2
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
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopCameraTests.cs` | Начальное кадрирование и смена хода | Проверить сохранение кадра после залпа и новую подгонку при смене BattleId. |
| `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md` | Управление картой и Tabletop | Описать правило одного начального кадрирования для всех скинов. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots/tabletop/5-outcome.png` | Эталон итогов с автоподгонкой камеры | Обновить после визуальной сверки кадра с сохранённой камерой. |

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

## Регрессия Tabletop от 13 ноября 2026 года

Кадрирование из [EP-0024-US-0008-TK-0001](../../../../EP-0024-battle-field-redesign/EP-0024-US-0008-camera/EP-0024-US-0008-TK-0001-camera/EP-0024-US-0008-TK-0001-camera.md) вернуло подгонку камеры по паре `(Phase, Turn)` до первого ручного изменения вида. По запросу от 13 ноября восстановить AC3 для Tabletop. Первый кадр нового боя использует текущие правила стартовой подгонки скина. Движение, стрельба, полёт и результаты залпа, следующий ход и изменение размеров сохраняют центр и масштаб. Проверки переходов выполняются в `PhaseCameraTests` через экран боя для Classic и Tabletop с ручным изменением камеры и со стартовым видом. `TabletopCameraTests` проверяет начальный кадр и сброс при смене `BattleId`.

Исправление реализовано в `TacticalBattleCamera.Update`. Сборка Client и Client.Tests, scoped `dotnet format whitespace` и `git diff --check` прошли. В прогоне тактического боя прошли 560 из 561 проверок. Единственное расхождение было со старым эталоном Tabletop `5-outcome.png`, где итог автоматически центрировал карту. Эталон обновлён после сверки двух кадров. Финальный прогон проверок камеры и снимков Tabletop прошёл 18 из 18, включая обновлённый эталон. Ручная проверка в игре не проводилась.

- Тактический прогон до обновления эталона: `D:/DeepSpaceSaga/artifacts/validation/maintenance-battle-camera/20261113-camera-stability-01/client.trx`.
- Финальные проверки: `D:/DeepSpaceSaga/artifacts/validation/maintenance-battle-camera/20261113-camera-stability-01/final/camera-and-tabletop.trx`.
