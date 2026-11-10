---
epic: EP-maintenance
story: EP-maintenance-US-0009-tactical-map-feedback
ticket: EP-maintenance-US-0009-TK-0005-show-all-fire-candidates
title: Все доступные цели выбранного дрона
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 4
serves: [AC5]
created: "2026-11-10T12:45:42.415Z"
revision: 1
---

# Все доступные цели выбранного дрона

## Why

Проверка уточнения: 34 целевых теста клиента прошли. Лог: D:/DeepSpaceSaga/artifacts/validation/tactical-map-polish/20261110144815363/client-recheck/test.log.

Уточнение от 2026-11-10. При нескольких целях в правой панели появляется прокручиваемый список с вероятностями. Щелчок меняет тот же приказ, что и выбор на карте.
Обновляются соответствующий рендер, ввод или генератор, связанные проверки и затронутые растровые эталоны.

Отметить на карте все цели выбранного дрона по текущему снимку и вывести их количество в карточке. Подсветить назначенную цель и дать отличимый маркер и шанс остальным. Сектор соответствует половинному углу оружия и дальности. Приказ по-прежнему содержит одну цель.

## Decisions

Пункт 5 запроса пользователя от 10 ноября 2026 года.

## Assumptions

Доступность целей определяет Engine. Слабая подсветка обозначает альтернативу, яркая стрелка обозначает назначенный выстрел.

## Code context

| Файл | Исходный участок | Допустимое изменение |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs` | 16–55 | Рисовать альтернативы из Group.List и использовать полный размах 2 * ArcDegrees. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapFireTests.cs` | 12–23 | Проверить два кандидата, различие подсказок и переключение назначенной цели. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | 98–102 | Показывать число кандидатов и состояние удержания огня. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Acceptance.cs` | Layout, тексты и растровые пробы | Согласовать интеграционные ожидания с пятью изменениями истории. |

Для пяти исправлений этой истории workflow разрешает обновить визуальные эталоны в `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots` после сверки растровых кадров. Эталоны входят в тикет как тестовые данные.

## Public API after the change

Публичные DTO и transport API сохраняются.

## Implementation steps

1. Рисовать альтернативы из Group.List и использовать полный размах 2 * ArcDegrees.
2. Проверить два кандидата, различие подсказок и переключение назначенной цели.

## Out of scope

Изменения правил движения, расчёта попаданий, приказов Engine и сохранений.

## Invariants

Отметить на карте все цели выбранного дрона по текущему снимку. Подсветить назначенную цель и дать отличимый маркер и шанс остальным. Сектор соответствует половинному углу оружия и дальности. Приказ по-прежнему содержит одну цель.

## Tests

Проверить MapFireTests, FirePlanningTests в Client.Tests. Запустить из корня DSS, с process-tree cap 300000 мс и абсолютным `--results-directory` в `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/<run>`.

## Definition of Done

Изменения ограничены указанными файлами. Именованные проверки, сборка, scoped format и `git diff --check` выполнены. Результаты записаны до публикации.

## Self-containment check

В контексте указаны владелец отображения или ввода, точка изменения и проверка. Зависимостей от других тикетов нет.

## Статус

Карточка считает все доступные цели. Карта отмечает альтернативы и показывает назначенную цель отдельной стрелкой. Проверены две цели Юна из реального снимка, переключение выстрела, сектор, интеграционные пробы и 15 растровых эталонов.

Клиент и тесты собраны. Все 416 проверок тактического боя прошли, scoped format и git diff --check прошли. Результаты в D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0009/20261110124434331/tactical-final/client.log. Ручная проверка в запущенной игре не проводилась.
