---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0025-comms-chip-hit
title: Свёрнутая панель связи ловит клики шире кнопок
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC25]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Свёрнутая панель связи ловит клики шире кнопок

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Свёрнутая панель связи перехватывает клики только по значку и кнопке «Подсказки»; зазор между ними и полосы над и под кнопкой пропускают клик на карту.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0028-US-0006-TK-0004. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** При включённых подсказках `Bounds = SKRect.Union(chip, HintsBounds)` включает зазор 6 px и полосы над и под кнопкой (кнопка 20 px при значке 30). `OnMouseDown` возвращает true для любой точки в `Bounds`, даже когда `Hit` вернул null.

**Сценарий.** Левый клик в x ≈ 396…402 или y 684…689 / 709…714 справа от значка поглощается без действия; клик по карте или юниту там не проходит, наведение подавлено.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialCommsPanel.cs:117-123` | `Bounds` как объединение. |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs:126-134` | `OnMouseDown`. |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialScreenIntegrationTests.cs` | Добавлено при исполнении: `AssertPanelClear` проверяет каждую область панели вместо единственного объединённого прямоугольника. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Отдавать значок и кнопку отдельными прямоугольниками (`TutorialSession.Bounds` уже `IEnumerable`) или возвращать false при `Hit == null` вне значка.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialCommsPanelTests`: клик в зазоре не перехватывается.

## Definition of Done

- AC25 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: `DrawChip` задавал `Bounds = SKRect.Union(chip, HintsBounds)`, и `TutorialSession.OnMouseDown` перехватывал любой клик в этом прямоугольнике, в том числе в зазоре и полосах над и под кнопкой; та же область блокировала наведение.

- `TutorialCommsPanel`: в свёрнутом виде `Bounds` — только значок; новые `Areas` (развёрнутая панель либо значок и кнопка «Подсказки») и `Contains`.
- `TutorialSession.Bounds` отдаёт `Panel.Areas`, `OnMouseDown` проверяет `Panel.Contains`. Наведение и панорамирование (`BlocksPan`, `OverlayRects`) получают раздельные области без изменений в экране боя.

Тесты: новый `TutorialCommsPanelTests.CollapsedChipCapturesOnlyChipAndHintsButtonNotTheGapAround`; `TutorialCommsPanelTests.WithoutLineThePanelCollapsesToTheCommsChip` ожидает `[chip, hints]` вместо объединения; `TutorialScreenIntegrationTests.AssertPanelClear` проверяет каждую область.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
