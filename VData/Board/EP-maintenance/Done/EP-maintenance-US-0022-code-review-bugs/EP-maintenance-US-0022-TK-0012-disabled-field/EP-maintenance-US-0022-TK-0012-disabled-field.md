---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0012-disabled-field
title: Недоступный контрол вне боя выглядит как доступный
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC12]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Недоступный контрол вне боя выглядит как доступный

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Вне боевого скина недоступные DropDown, TabStrip, Toggle и ScrollTextPanel отличаются от доступных не только цветом подписи; всплывающая подсказка не получает золотую рамку фокуса.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0001-TK-0001-steel-controls. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** При `SkinStyle.Current is null` `ControlVisuals.Frame` вызывает `TabletopStyle.Field(canvas, rect, active && enabled, scale)`: `enabled` лишь гасит золото, рамка недоступного поля совпадает с обычной. Раньше была отдельная рамка. Правило эпика — недоступность показывать подписью, формой или значком, а не только цветом. `DropDown.cs:336` рисует подсказки с `active: true`, поэтому у каждой подсказки золотая рамка фокуса.

**Сценарий.** Экран настройки боя (`TacticalBattleSetupScreen`) без скина: заблокированный выпадающий список неотличим от доступного, кроме цвета текста.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Controls/TabStrip.cs:78-82` | `ControlVisuals.Frame` без скина. |
| `src/DeepSpaceSaga.Client/UI/Controls/DropDown.cs:336` | Подсказка с `active: true`. |
| `src/DeepSpaceSaga.Client/UI/Controls/TabletopStyle.cs` | Добавлено при исполнении (названо в шаге 2): `Field(..., enabled)` — недоступное поле получает заливку `Disabled` и никогда не получает золотую кромку. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Добавить в `TabletopStyle.Field` отдельное оформление недоступного состояния (заливка `Disabled` или штриховка); подсказку рисовать с `active: false`.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TabletopStyleTests`: пиксели недоступного и обычного поля различаются; подсказка без золотой рамки.

## Definition of Done

- AC12 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: `ControlVisuals.Frame` без скина вызывал `TabletopStyle.Field` с заливкой `Inset` независимо от доступности, `ControlText.Tooltip` рисовал рамку с `active: true` и получал золотую кромку.

- `TabletopStyle.Field` принимает `enabled`: недоступное поле залито `Disabled` (как недоступные кнопки), без золотой кромки. Остальные вызовы не изменились.
- `ControlVisuals.Frame` без скина передаёт `enabled` — это покрывает DropDown, TabStrip, Toggle, ShortcutButton.
- `ControlText.Tooltip` вне боевого скина рисуется неактивным; в боевых скинах подсказка не изменена (буквальный `active: false` поменял бы её цвета в Classic и Xenon Star).
- Остаток: `ScrollTextPanel` не имеет состояния доступности и всегда рисуется как доступный; добавление такого состояния — новое поведение, не делалось.

Тесты (`TabletopStyleTests`): `Disabled_field_differs_from_normal_field_and_never_gets_the_gold_edge`, `Tooltip_outside_battle_skin_has_no_gold_focus_edge`.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
