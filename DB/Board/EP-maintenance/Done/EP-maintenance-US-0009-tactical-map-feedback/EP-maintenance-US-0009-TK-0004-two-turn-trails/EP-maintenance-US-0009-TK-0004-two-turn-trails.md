---
epic: EP-maintenance
story: EP-maintenance-US-0009-tactical-map-feedback
ticket: EP-maintenance-US-0009-TK-0004-two-turn-trails
title: Пройденный путь за два хода
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 5
serves: [AC4]
created: "2026-11-10T12:45:42.415Z"
revision: 1
---

# Пройденный путь за два хода

## Why

Тонкая непрерывная линия показывает реально пройденный путь дронов обеих сторон за два последних движения. Во время анимации линия заканчивается в текущей позе и не показывает будущие участки.

## Decisions

Пункт 4 запроса пользователя от 10 ноября 2026 года.

## Assumptions

История опирается на подтверждённые снимки текущего боя и хранится на клиенте до его закрытия.

## Code context

| Файл | Исходный участок | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | Захват кадра и обработка ответа | Совместить последний завершённый путь с пройденным участком текущей анимации. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | Публикация подтверждённого снимка | Хранить проекцию двух ходов для всех экранов одного боя, сохраняя исходный снимок отдельно. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | 88–90 | Отрисовать историю в каждой фазе одним непрерывным контуром на путь с круглыми соединениями. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs` | 12–16 | Убрать прежний штриховой слой, скрывавшийся в планировании движения. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementAnimationTests.cs` | 12–85 | Проверить ограничение в два хода, оба борта и конечную точку во время воспроизведения. |

## Public API after the change

Публичные DTO и transport API сохраняются.

## Implementation steps

1. Объединить последние два подтверждённых движения только в проекции текущего боя; сохранить историю при отказах.
2. Добавить частичную траекторию к одному завершённому ходу.
3. Отрисовать историю в каждой фазе одним непрерывным контуром на путь с круглыми соединениями.
4. Убрать прежний штриховой слой, скрывавшийся в планировании движения.
5. Проверить ограничение в два хода, оба борта и конечную точку во время воспроизведения.

## Out of scope

Изменения правил движения, расчёта попаданий, приказов Engine и сохранений.

## Invariants

Тонкая непрерывная линия показывает реально пройденный путь дронов обеих сторон за два последних движения. Во время анимации линия заканчивается в текущей позе и не показывает будущие участки.

## Tests

Проверить MovementAnimationTests, BattleMapRenderTests в Client.Tests. Запустить из корня DSS, с process-tree cap 300000 мс и абсолютным `--results-directory` в `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/<run>`.

## Definition of Done

Изменения ограничены указанными файлами. Именованные проверки, сборка, scoped format и `git diff --check` выполнены. Результаты записаны до публикации.

## Self-containment check

В контексте указаны владелец отображения или ввода, точка изменения и проверка. Зависимостей от других тикетов нет.

## Статус

Видны два последних подтверждённых пути обеих сторон. Во время анимации текущий путь заканчивается в текущей позе. Новый экран того же боя получает ту же историю.

Клиент и тесты собраны. Все 416 проверок тактического боя прошли, scoped format и git diff --check прошли. Результаты в D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/20261110124434331/tactical-final/client.log. Ручная проверка в запущенной игре не проводилась.
