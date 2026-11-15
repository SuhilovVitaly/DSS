---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0020-cloud-surface-null
title: Облака Tabletop: нет проверки SKSurface.Create на null
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC20]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Облака Tabletop: нет проверки SKSurface.Create на null

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Если поверхность для пересборки облаков не создалась, кадр рисуется без пересборки (прежнее изображение или без облаков), без исключения в цикле отрисовки.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0024-US-0004-TK-0002-clouds-cache. Серьёзность: minor. Уверенность: PLAUSIBLE (код однозначен, отказ не воспроизводился).

**Дефект.** `SKSurface.Create(context, false, size)` и `SKSurface.Create(size)` возвращают null при потерянном GRContext или размере больше максимальной текстуры. Сразу следует `surface.Canvas.Clear(...)` — NullReferenceException в `RenderNative`.

**Сценарий.** Потеря GPU-контекста или очень большой размер окна во время боя Tabletop: исключение в цикле отрисовки на каждом кадре.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PaintedCloudLayer.cs:27-28` | Создание поверхности без проверки. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. При null пропускать пересборку и рисовать прежний `_image`, если он есть.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TabletopCloudTests`: фабрика поверхности возвращает null — отрисовка не бросает исключение.

## Definition of Done

- AC20 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
