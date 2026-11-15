---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0009-reticle-colours
title: Карта: прицел выбора и прицел наведения почти одного цвета
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC9]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Карта: прицел выбора и прицел наведения почти одного цвета

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Прицел выбранного объекта и прицел объекта под курсором различимы на неподвижном кадре по цвету или форме; тест это проверяет.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0026-US-0003-TK-0001-world-field. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Выбор теперь рисуется `TabletopStyle.Gold` (255,210,74), наведение — `TacticalMapPalette.Target` (255,209,138). Оба тёплые жёлтые, отличаются только синим каналом и направлением вращения, хотя комментарий метода обещает оранжевый оттенок для отличия. Тест `Active_object_reticle_uses_orange_tint` проверяет лишь R > G > B и пропускает новое золото.

**Сценарий.** Выбран один объект, курсор над другим: на скриншоте прицелы выглядят одинаково. Нарушен критерий US-0003 о различении выбора и наблюдения.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs:272, 286` | Цвета прицелов выбора и наведения. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Развести состояния по цвету (или форме) с заметной разницей оттенка/яркости.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

Тест: пиксели колец выбора и наведения различаются по оттенку или яркости не меньше заданного порога; усилить `Active_object_reticle_uses_orange_tint`.

## Definition of Done

- AC9 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
