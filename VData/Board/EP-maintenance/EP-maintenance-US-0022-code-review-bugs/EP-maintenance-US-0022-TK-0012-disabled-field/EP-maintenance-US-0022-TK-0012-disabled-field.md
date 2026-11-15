---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0012-disabled-field
title: Недоступный контрол вне боя выглядит как доступный
stage: draft
implementation_status: not_started
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

Не начат.
