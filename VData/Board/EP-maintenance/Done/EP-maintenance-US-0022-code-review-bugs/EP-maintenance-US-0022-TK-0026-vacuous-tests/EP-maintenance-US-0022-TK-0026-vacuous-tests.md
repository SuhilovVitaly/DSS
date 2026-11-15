---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0026-vacuous-tests
title: Тесты, которые не могут упасть
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC26]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Тесты, которые не могут упасть

[История](../../../EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Перечисленные тесты проверяют текущее поведение и могут упасть при регрессии; пропуск по платформе виден в отчёте.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0024-US-0003-TK-0001, EP-0024-US-0009-TK-0002, EP-0028-US-0004-TK-0001, EP-0028-US-0005-TK-0002. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** (1) `TabletopPlateTests` и `TabletopPanelProbeTests` проверяют `DoesNotContain` для ключей `plate:damage:*` и `pilot-damage-armor:*`, которые код больше не пишет (рамку повреждения убрал 91dc15bc); тест «…only_damaged_boxes_are_highlighted» проверяет лишь отсутствие подсветки. (2) `TutorialHintsPreferenceTests` на не-Windows делает `return` и молча проходит. (3) `TutorialLessonFlowTests` зашивает сид 1 вместо `SkiaWindow.TutorialBattleSeed`.

**Сценарий.** Регрессия подсветки повреждений, атомарной записи настроек на Linux или смена продакшн-сида обучения не ловится тестами.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopPlateTests.cs:55, 76-77` | Проверки несуществующих ключей. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/TabletopPanelProbeTests.cs:49-50` | То же. |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHintsPreferenceTests.cs:96` | Молчаливый выход на не-Windows. |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLessonFlowTests.cs:46` | Зашитый сид. |
| `tests/DeepSpaceSaga.Client.Tests/WindowsOnlyFactAttribute.cs` | Добавлено при исполнении: `[WindowsOnlyFact]` — пропуск по платформе виден в отчёте как Skipped. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Убрать проверки несуществующих ключей и переименовать тест по текущему контракту (или проверять отсутствие рамки по пикселям). Заменить `return` на видимый пропуск (Skip/условный атрибут). Использовать `SkiaWindow.TutorialBattleSeed`.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Указанные тесты проходят; временная порча проверяемого поведения роняет каждый из них.

## Definition of Done

- AC26 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Все четыре теста на текущем main оставались в пустом виде; ни EP-0029, ни предыдущие тикеты истории их не меняли. Код продукта не менялся.

- `TabletopPlateTests`: удалены проверки отсутствия ключей `plate:damage:*`, которые код не пишет с 91dc15bc; тест переименован в `Plates_show_name_armor_and_hull_boxes_without_callouts`, остальные проверки сохранены. Пиксельная проверка золотой рамки не добавлялась: рядом с плашками шаблоны Tabletop рисуют тот же золотой цвет. Возврат рамки урона на плашки этот тест больше не ловит.
- `TabletopPanelProbeTests`: удалены проверки отсутствия `pilot-damage-armor:*` и `plate:damage:armor:*`; проверки цвета полосы брони и текста итога сохранены.
- `TutorialHintsPreferenceTests`: Windows-часть вынесена в отдельный `LockedFileWriteFailureKeepsOriginalFileAndLeavesNoTemp` с новым атрибутом `[WindowsOnlyFact]` — вне Windows он виден в отчёте как Skipped, а не как молча пройденный.
- `TutorialLessonFlowTests`: стартовый запрос использует `SkiaWindow.TutorialBattleSeed` вместо литерала `1`.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
