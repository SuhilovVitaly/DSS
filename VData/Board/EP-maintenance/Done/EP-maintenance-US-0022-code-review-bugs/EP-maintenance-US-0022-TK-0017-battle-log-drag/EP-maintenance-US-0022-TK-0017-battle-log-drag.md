---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0017-battle-log-drag
title: Перетаскивание прокрутки журнала боя прерывается
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC17]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Перетаскивание прокрутки журнала боя прерывается

[История](../../../EP-maintenance-US-0022-code-review-bugs/EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Перетаскивание ползунка журнала боя продолжается при смене высоты панелей, журнал не перестраивает перенос строк при наведении на юниты.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-maintenance-US-0018-TK-0005-battle-log (вошло в merge #113). Серьёзность: minor. Уверенность: CONFIRMED (механизм).

**Дефект.** Границы журнала — `Max(phasePanel.Bottom, hoverCard.Bottom)+6 .. 714` — пересчитываются каждый кадр. При изменении `ScrollTextPanel.Layout` вызывает `ResetInput()` (`IsDragging = false`) и заново переносит весь текст.

**Сценарий.** Игрок тянет ползунок журнала во время полёта залпа; по окончании полёта `PhasePanel.Bottom` меняется 382→366, появляется карточка отчёта, перетаскивание молча обрывается. При наведении на юниты в планировании журнал прыгает по высоте и перестраивается.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs:45-46` | Пересчёт границ журнала. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleLogTab.cs:63` | Передача границ. |
| `src/DeepSpaceSaga.Client/UI/Controls/ScrollTextPanel.cs:35-42` | `Layout` сбрасывает ввод. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Фиксированные границы журнала на фазу или сохранение состояния перетаскивания при изменении только высоты.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`OverlayShellTests`: перетаскивание через переход «полёт → итог» не прерывается.

## Definition of Done

- AC17 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: верх журнала пересчитывается каждый кадр по нижней границе панели фазы и карточки наведения, а `ScrollTextPanel.Layout` при любой смене границ сбрасывал перетаскивание и заново переносил весь текст.

- `ScrollTextPanel.Layout` сбрасывает ввод и переносит строки только при смене ширины или масштаба интерфейса. При смене одной высоты или положения перенос и перетаскивание сохраняются, прокрутка следует к низу или ограничивается.
- Изменение общее для всех панелей на `ScrollTextPanel` (журнал, вкладка прогноза, панель ошибки).
- Остаток: журнал по-прежнему меняет высоту при наведении на юниты (без перестройки строк); фиксированная высота по фазам — отдельное решение о раскладке.

Тесты: `OverlayShellTests.BattleLogDragSurvivesSalvoFlightToResultsTransition`, `WidgetsListsTests.HeightOnlyLayoutKeepsDragAndWrappedLinesButWidthChangeResetsThem`.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
