---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0011-save-screen-accents
title: Окно сохранения без акцентов окна загрузки
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC11]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Окно сохранения без акцентов окна загрузки

[История](../../../EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

В окне сохранения подтверждение удаления окрашено цветом опасности, кнопка «Сохранить» — основная, как «Загрузить» в окне загрузки.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0002-TK-0001-menu-chrome. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** LoadScreen красит «CONFIRM?» в `TabletopStyle.Danger` и помечает LOAD как primary. SaveScreen показывает «CONFIRM?» обычным цветом, SAVE не primary. Тикет требует «подтверждение удаления — цветом опасности» для меню и слотов.

**Сценарий.** Игрок удаляет слот в окне сохранения: подтверждение выглядит как обычная кнопка в состоянии наведения.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Save/SaveScreen.cs:311-319` | Кнопки слота и сохранения. |
| `src/DeepSpaceSaga.Client/UI/Screens/Load/LoadScreen.cs:305-312` | Образец окна загрузки. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Передать `TabletopStyle.Danger` для подтверждения и `primary: true` для «Сохранить».
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Визуальный тест SaveScreen: пиксели подтверждения в цвете опасности, кнопка сохранения в основном стиле.

## Definition of Done

- AC11 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: в `SaveScreen.DrawBottomButtons` подтверждение удаления рисовалось обычными чернилами, кнопка «Сохранить» не была основной (в `LoadScreen` — `TabletopStyle.Danger` и `primary: true`).

- `SaveScreen`: подпись подтверждения удаления окрашена `TabletopStyle.Danger`, кнопка сохранения/перезаписи рисуется с `primary: true`. Оранжевая подпись «Перезаписать» сохранена.
- Для визуальной проверки: оранжевая подпись «Перезаписать» на золотой основной кнопке может читаться слабо.

Тесты (`SaveScreenVisualTests`): `Delete_confirmation_label_uses_the_danger_color_like_the_load_window`, `Plain_delete_label_does_not_use_the_danger_color`, `Save_button_uses_the_primary_style_like_the_load_button`.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
