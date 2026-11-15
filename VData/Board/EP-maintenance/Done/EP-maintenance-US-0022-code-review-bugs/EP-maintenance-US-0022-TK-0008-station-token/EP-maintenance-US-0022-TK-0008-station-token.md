---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0008-station-token
title: Карта мира: жетон станции стал мелким и почти без цвета
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC8]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Карта мира: жетон станции стал мелким и почти без цвета

[История](../../../EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Жетон станции на карте мира не меньше прочих маркеров (около 10 px) и заметно окрашен цветом станции; видимая фишка совпадает с областью наведения.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0003-TK-0001-world-field. Серьёзность: minor. Уверенность: CONFIRMED (геометрия).

**Дефект.** Для станции `r = 5`, `halfSide = 0.68r`, плашка 6,8 px. `TabletopStyle.Draw` снимает 1 px рамки и 1 px фаски с каждой стороны, остаётся около 2,8 × 2,8 px цвета станции.

**Сценарий.** На полной детализации прежний маркер — шар 10 px; сейчас — почти чёрный квадрат 7 px с точкой ~3 px. Область наведения осталась радиусом 10 px, то есть больше видимой фишки. Противоречит критерию US-0003 «читаемые фишки».

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs:182-189` | `DrawStationToken`. |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:1299-1302` | Вызов для станций. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Размер плашки около `2r`, либо рисовать жетон этого размера без двойной фаски.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Пиксельный тест рядом с `TacticalMapSelectionReticleTests`: `DrawStationToken` с r = 5 даёт не меньше заданного числа пикселей цвета станции.

## Definition of Done

- AC8 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: `DrawStationToken` строил плашку со стороной `2·r·0,68`, после кромки и фаски `TabletopStyle.Draw` при r = 5 оставалось около 2×2 px цвета станции.

- `TacticalMapDepthRenderer.DrawStationToken`: плашка занимает весь маркер `2r` (10 px при r = 5) и совпадает с областью наведения; цветная грань 6×6 px. Оформление с фаской сохранено.

Тесты (новый `TacticalMapStationTokenTests`): `Station_token_keeps_readable_area_of_station_colour`, `Station_token_spans_full_marker_diameter_like_hover_area`.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
