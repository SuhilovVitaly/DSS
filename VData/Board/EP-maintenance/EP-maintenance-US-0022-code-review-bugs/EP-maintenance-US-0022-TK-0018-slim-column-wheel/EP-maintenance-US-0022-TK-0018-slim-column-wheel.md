---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0018-slim-column-wheel
title: Колесо мыши над картой у узкой колонки прокручивает колонку
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC18]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Колесо мыши над картой у узкой колонки прокручивает колонку

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

В узкой колонке Tabletop колесо прокручивает колонку только над карточками и панелью; над картой правее карточек оно меняет масштаб. Полоса прокрутки колонки не рисуется поверх панели.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0024-US-0012-TK-0001-slim-layout, EP-0024-US-0013-TK-0001-panel-frame. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** В узком режиме карточки занимают x 6…156, но `PilotCardColumn.Scroll` проверяет старую область `PilotColumnLayout.Viewport` (x до 196). Клики и панорама в полосе x 156…196 уходят на карту (в OverlayRects только карточки и панели), а колесо там перехватывает колонка раньше `_camera.Zoom`. Полоса прокрутки рисуется на x 198…200, на правом краю панели (до x 201).

**Сценарий.** Колонка длиннее экрана (после залпа у каждого стрелявшего открыта панель итога 71+). Курсор в (175, 400) над картой, колесо → колонка прокручивается на 40, масштаб не меняется.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs:96-103, 188-197` | `Scroll` и полоса прокрутки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotColumnLayout.cs:9` | `Viewport = (6,6,196,714)`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs:384-385` | Порядок обработки колеса. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. В узком режиме проверять реальные прямоугольники карточек (`Layout.Cards`, обрезанные по Viewport) и панелей; полосу прокрутки ставить у правого края карточек (x ≈ 158).
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TabletopColumnLayoutTests` или `TabletopCommandPanelTests`: колонка с переполнением, `OnMouseWheel(175, 400)` меняет вид камеры, а не прокрутку колонки.

## Definition of Done

- AC18 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
