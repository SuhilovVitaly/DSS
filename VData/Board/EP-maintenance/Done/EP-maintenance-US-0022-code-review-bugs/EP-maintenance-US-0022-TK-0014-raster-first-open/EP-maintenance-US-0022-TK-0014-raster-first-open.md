---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0014-raster-first-open
title: Растровый замер: firstMs не измеряет первое открытие
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: tooling
test_project: tools/DeepSpaceSaga.Performance
serves: [AC14]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Растровый замер: firstMs не измеряет первое открытие

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Отчёт `TabletopClientRaster` в поле первого открытия показывает время первого рендера после смены масштаба на холодных кэшах; тёплые прогоны помечены как тёплые.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0009-TK-0001-route-validation. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Перед замером каждого LOD инструмент уже вызывает `screen.Render` для проверки `MapDetail`, затем `Measure` замеряет повторный рендер того же масштаба как `firstMs`. Во второй конфигурации (1920×1080/RU) меню и станция используют кэши, прогретые первым проходом.

**Сценарий.** Отчёт выдаёт время на тёплых кэшах за время первого открытия; на эти числа ссылается AcceptanceCoverage EP-0026 (критерий 4).

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `tools/DeepSpaceSaga.Performance/TabletopClientRaster.cs:41, 50-53, 58-61` | Предварительный рендер и `Measure`. |
| `tools/DeepSpaceSaga.Performance/README.md` | Добавлено при исполнении: описание полей `firstMs` и `cache`. |
| `tools/DeepSpaceSaga.Performance.Tests/TabletopClientRasterReportTests.cs` | Добавлено при исполнении: новый тест отчёта. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Замерять первый рендер после `SetZoom`, `MapDetail` проверять после. Холодным считать только первый проход процесса или запускать конфигурации в отдельных процессах. Перепроверить числа в AcceptanceCoverage EP-0026.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Прогон инструмента: firstMs карты больше её p95; отчёт различает холодный и тёплый проход.

## Definition of Done

- AC14 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: после `SetZoom` выполнялся проверочный рендер, и `firstMs` фиксировал уже второй рендер уровня; холодный и тёплый проходы не различались.

- `TabletopClientRaster`: лишний рендер перед замером убран, `firstMs` — первый рендер после смены масштаба; проверка `MapDetail` выполняется после него через обратный вызов `Measure`. У каждого результата поле `cache`: `cold` для первого прохода процесса, `warm` для последующего. Класс и `Run` стали `public` для тестового проекта.
- Начальный рендер при создании `GameSessionScreen` сохранён (нужен для камеры), поэтому уровень с масштабом по умолчанию может быть частично прогрет.

Тест: `TabletopClientRasterReportTests.FirstOpenIsMeasuredOnFirstRenderAndPassesAreMarkedColdOrWarm` (12 результатов, метки cold/warm, `firstMs > p95Ms` для уровней карты холодного прохода; критерий зависит от времени).

Проверки: `dotnet build tools/DeepSpaceSaga.Performance.Tests` — 0 предупреждений, 0 ошибок. Тесты и повторный замер для чисел EP-0026 AcceptanceCoverage выполняются после завершения истории; результат записан в документе истории.
