---
epic: EP-maintenance
story: EP-maintenance-US-0010-tactical-readability-and-results
ticket: EP-maintenance-US-0010-TK-0005-round-shot-report
title: Список выстрелов раунда справа внизу
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
depends_on: []
files_touched: 5
serves: [AC5]
created: "2026-11-10T14:03:31.830Z"
---

# Список выстрелов раунда справа внизу

## Why

В результатах залпа справа внизу отображается прокручиваемый список выполненных выстрелов обеих сторон в порядке залпа. Наведение на дрон или Таг временно заменяет список карточкой этого юнита.

## Decisions

Отдельное исправление по запросу пользователя с коммитом и отправкой в текущую ветку.

## Assumptions

Отчёт включает фактически выполненные выстрелы, включая промахи и поглощения.

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCardModel.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BuildCardModelTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Acceptance.cs`

Общие растровые эталоны тактических сцен обновляются в этом тикете после сверки кадров всех пяти UI-изменений и нового поля. Проверка стиля формы исключает только область мигающей каретки, значение seed проверяется отдельно.

## Public API after the change

Форматы команд и сохранений прежние.

## Implementation steps

Изменить указанную точку отрисовки, ввода или генерации, затем проверить соответствующий критерий через существующую фикстуру.

## Out of scope

Изменения боевой формулы, камеры и правил сохранений.

## Invariants

В результатах залпа справа внизу отображается прокручиваемый список выполненных выстрелов обеих сторон в порядке залпа. Наведение на дрон или Таг временно заменяет список карточкой этого юнита.

## Tests

`BuildCardModelTests|Ep0019ViewTests` в Client.Tests. Результаты под абсолютным каталогом `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/<run>`. Лимит процесса 300000 мс.

## Definition of Done

Критерий подтверждён целевыми проверками, `git diff --check` проходит, статус и результаты записаны перед публикацией.

## Self-containment check

В контексте указаны файлы реализации и тестов.

## Статус

419/419 тестов тактического клиента. Проверены выстрелы обеих сторон, смена отчёта на карточки своего и вражеского дрона при наведении, возврат отчёта и прокрутка до последнего выстрела. Сверены 15 растровых эталонов. Логи и кадры: D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/client-final/test.log, D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/capture/contact.png. Ручной запуск не выполнялся.
