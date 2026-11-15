---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0010-dropdown-skin-fallback
title: Выпадающий список в боевых скинах Classic и Xenon Star
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC10]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Выпадающий список в боевых скинах Classic и Xenon Star

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

В бою со скином без роли `control.dropdown` выпадающий список рисуется в стиле этого скина, подпись не заходит под шеврон.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0002-TK-0001-menu-chrome. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Без стиля `control.dropdown` `DropDown.Draw` падает в `XenonComboBox.DrawClosed`, который теперь рисует поле Tabletop с шевроном у `Right-14`. Подпись обрезается многоточием до `Right-12·s` и уходит под шеврон; левые 44 px пусты. Classic и Xenon Star этой роли не имеют. US-0001-TK-0001 требует, чтобы внутри активного боевого скина сохранялись его роли.

**Сценарий.** Бой в Classic или Xenon Star, панель огня с длинным названием цели: текст подписи перекрыт шевроном, контрол выглядит как Tabletop внутри другого скина.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Controls/XenonComboBox.cs:66-79` | `DrawClosed` после переделки. |
| `src/DeepSpaceSaga.Client/UI/Controls/DropDown.cs:177-183` | Ветка без стиля скина. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFirePanel.cs:72` | Использование в панели огня. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. При активном боевом скине без роли сохранить прежнюю отрисовку Xenon либо резервировать правые 28 px под шеврон.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Тест DropDown под `TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin.Classic)` с длинной подписью: прямоугольник подписи не пересекает шеврон.

## Definition of Done

- AC10 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
