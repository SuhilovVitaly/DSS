---
epic: EP-maintenance
story: EP-maintenance-US-0009-tactical-map-feedback
ticket: EP-maintenance-US-0009-TK-0002-selection-only-zones
title: Зоны только у выбранного дрона
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 5
serves: [AC2]
created: "2026-11-10T12:45:42.415Z"
revision: 1
---

# Зоны только у выбранного дрона

## Why

Щелчок по выбранному дрону на карте снимает выбор и скрывает обе зоны. Повторный выбор показывает зоны этого дрона. Готовые приказы и плановые линии сохраняются.

## Decisions

Пункт 2 запроса пользователя от 10 ноября 2026 года.

## Assumptions

Сохраняется штатный начальный выбор. Снять выбор можно повторным щелчком по маркеру дрона.

## Code context

| Файл | Исходный участок | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleOrders.cs` | 64–80 | Снятие и возврат выбора, включая Tab после снятия. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs` | 57–74, 109–130 | Снятие выбора с маркера своего дрона; при повторном выборе доступны прежние приказы. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | 284–296 | Обрабатывать щелчок по выбранному дрону отдельно от жеста редактирования конца пути. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapMovementTests.cs` | Зоны и выбор в Move и Fire | Проверить снятие выбора, сохранение приказов и доступность кнопки фазы в обоих режимах. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.cs` | Переключение по фазам | Сохранить кнопку выполнения фазы при снятом выборе. |

## Public API after the change

Публичные DTO и transport API сохраняются.

## Implementation steps

1. Снятие и возврат выбора, включая Tab после снятия.
2. Снятие выбора с маркера своего дрона; при повторном выборе доступны прежние приказы.
3. Обрабатывать щелчок по выбранному дрону отдельно от жеста редактирования конца пути.
4. Проверить отсутствие обеих зон после снятия выбора с сохранением планов.
5. Проверить снятие и восстановление выбора в фазе огня.

## Out of scope

Изменения правил движения, расчёта попаданий, приказов Engine и сохранений.

## Invariants

Щелчок по выбранному дрону на карте снимает выбор и скрывает обе зоны. Повторный выбор показывает зоны этого дрона. Готовые приказы и плановые линии сохраняются.

## Tests

Проверить MapMovementTests, MapFireTests в Client.Tests. Запустить из корня DSS, с process-tree cap 300000 мс и абсолютным `--results-directory` в `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/<run>`.

## Definition of Done

Изменения ограничены указанными файлами. Именованные проверки, сборка, scoped format и `git diff --check` выполнены. Результаты записаны до публикации.

## Self-containment check

В контексте указаны владелец отображения или ввода, точка изменения и проверка. Зависимостей от других тикетов нет.

## Статус

Повторный щелчок по выбранному дрону скрывает зоны. Штатный начальный выбор, приказы и кнопка выполнения фазы сохранены. Проверены Move и Fire, включая близкую цель у маркера своего дрона.

Клиент и тесты собраны. Все 416 проверок тактического боя прошли, scoped format и git diff --check прошли. Результаты в D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/20261110124434331/tactical-final/client.log. Ручная проверка в запущенной игре не проводилась.
