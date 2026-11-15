---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0009-reticle-colours
title: Карта: прицел выбора и прицел наведения почти одного цвета
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC9]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Карта: прицел выбора и прицел наведения почти одного цвета

[История](../../../EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-code-review-bugs.md)

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

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: прицел выбора `TabletopStyle.Gold` (оттенок ~45°) и прицел наведения `TacticalMapPalette.Target` (~36°) различались примерно на 9°, тест проверял только R > G > B.

- `TacticalMapDepthRenderer.DrawActiveObjectReticle` рисует отдельным оранжевым `ActiveReticleColor = (255, 128, 32, 230)`, оттенок ~26°. Прицел выбора и `TacticalMapPalette.Target` (используется в `GameSessionScreen.MapView`) не изменены.

Тесты (`TacticalMapSelectionReticleTests`): `Active_object_reticle_uses_orange_tint` проверяет оттенок 15–32°; новый `Selected_and_active_reticles_differ_in_hue_on_a_still_frame` требует разницу оттенков не меньше 15° на неподвижном кадре.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
