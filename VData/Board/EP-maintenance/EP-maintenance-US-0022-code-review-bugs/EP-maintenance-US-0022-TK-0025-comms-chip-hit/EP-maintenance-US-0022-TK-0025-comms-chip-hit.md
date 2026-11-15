---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0025-comms-chip-hit
title: Свёрнутая панель связи ловит клики шире кнопок
stage: draft
implementation_status: not_started
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

Не начат.
