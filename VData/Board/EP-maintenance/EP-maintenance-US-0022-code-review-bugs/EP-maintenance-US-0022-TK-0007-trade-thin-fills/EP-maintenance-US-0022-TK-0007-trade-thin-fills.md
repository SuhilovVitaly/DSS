---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0007-trade-thin-fills
title: Торговля: тонкие полосы и полоска выбора теряют цвет
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC7]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Торговля: тонкие полосы и полоска выбора теряют цвет

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Полоска выбора строки видна золотом, полосы заполнения (бак, количество, груз, топливо) показывают цвет по всей высоте, малые изменения в предпросмотре видны цветом.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0005-TK-0001-station-trade. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** `TradePainter.Box` и `Bar` рисуют через `TabletopStyle.Draw`, который снимает рамку и фаску `line = min(scale, min(w,h)/4)` с каждой стороны дважды. Цвет занимает `w - 4·line` × `h - 4·line`.

**Сценарий.** Полоска выбора 3 px: line 0,75 → ширина заливки 0, золото не рисуется. Полосы 5 px: виден 1 px цвета. Отрезок предпросмотра уже ~4 px (около 0,8 % полосы 520 px, покупка нескольких единиц) показывается тёмной зарубкой.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradePainter.cs:19-21, 71-79` | `Box` и `Bar` через `TabletopStyle.Draw`. |
| `src/DeepSpaceSaga.Client/UI/Controls/TabletopStyle.cs:50-64` | Двойное сжатие прямоугольника рамкой и фаской. |
| `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeScreen.Render.cs:96, 127, 164, 192` | Полоска выбора и полосы 5 px. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Тонкие прямоугольники (меньшая сторона < ~8 px) и отрезки `Bar` рисовать плоской заливкой без фаски, например через новый `TabletopStyle.Fill`. Проверить FinanceScreen, который тоже использует `Box`.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Пиксельный тест торговли: золото внутри полоски выбора; цвет Success в средней строке полосы при изменении около 1 % ширины.

## Definition of Done

- AC7 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
