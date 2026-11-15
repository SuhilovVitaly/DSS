---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0005-tutorial-abandon-metrics
title: Брошенное обучение оставляет отчёт боя в статусе active
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC5]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Брошенное обучение оставляет отчёт боя в статусе active

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Выход из обучения в главное меню записывает отчёт боя со статусом `exited`, и журнал обучения отмечает отказ, так что брошенный бой отличается от сбоя.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0027-US-0001-TK-0001 (AC3), EP-0028-US-0006-TK-0001. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Выход из обучения идёт MainMenu → `ReturnToMainMenuAsync`, сессия освобождается без `Flow.ExitAsync()`. `ExportMetrics(..., exited: true)` вызывается только при принятом выходе, поэтому отчёт остаётся `status = "active"`; в `.tutorial.json` нет события отказа.

**Сценарий.** Игрок на втором ходу обучения нажимает Esc и подтверждает выход. Файл метрик выглядит как незавершённый бой; отличить отказ от падения нельзя. Обычный бой по F8 при выходе на карту помечается `exited`.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1857-1874` | `ReturnToMainMenuAsync` без выхода из боя. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs:92-96` | Экспорт метрик с `exited: true` только на принятом выходе. |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | Добавлено при исполнении: `RecordAbandoned()` — запись отказа по последнему наблюдению боя. |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialMetrics.cs` | Добавлено при исполнении: событие `abandoned` в `.tutorial.json`. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. В ветке MainMenu для обучения перед `ReturnToMainMenuAsync` выполнить `await modal.Flow.ExitAsync()` (без падения при ошибке) или записать метрики с `exited: true`; записать событие отказа в журнал обучения.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialFlowTests.TutorialEscapeConfirmationLeadsToMainMenu` под `DSS_VALIDATION_ROOT` читает `TacticalBattleMetrics.ReportPath(...)` и проверяет `"status": "exited"`.

## Definition of Done

- AC5 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. На текущем main дефект подтверждён частично: статус `exited` в отчёте боя уже пишется после `1cd25d22` (EP-0029 US-0012 TK-0003, `Flow.ExitAsync()` перед возвратом в меню); отметки отказа в журнале обучения не было.

- `SkiaWindow`: при принятом выходе из незавершённого обучающего боя в главное меню вызывается `TutorialSession.RecordAbandoned()`. Отклонённый выход (повтор с контрольной точки) ничего не пишет.
- `TutorialMetrics.RecordAbandoned`: событие `{ step: null, kind: "abandoned" }` с фазой, ходом и временем последнего наблюдения; накопленные события шагов записываются раньше него.

Тест: `TutorialFlowTests.TutorialEscapeConfirmationLeadsToMainMenu` дополнительно проверяет `"status": "exited"` в отчёте боя и последнее событие `abandoned` журнала обучения с тем же `BattleId`.

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
