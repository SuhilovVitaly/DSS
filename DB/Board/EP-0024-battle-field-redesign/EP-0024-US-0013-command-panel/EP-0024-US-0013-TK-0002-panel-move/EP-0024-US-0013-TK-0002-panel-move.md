---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0013-command-panel
ticket: EP-0024-US-0013-TK-0002-panel-move
title: Движение в панели
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0013-TK-0001-panel-frame]
files_touched: 3
serves: [US13-AC2]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Движение в панели

## Why

В движении панель содержит скорость и поворот с кнопками и ползунками, «Прямо», «Вернуть ИИ» и подсказку (эталон `3-move`).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-19.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardMoveBlock.cs` | :8 `Height` 104; :13-26 отрисовка; :36 роль | В режиме — раскладка UiSpec §11.7 в ширине 138, высота 91 (`MoveHeight`); те же действия `speed-`, `speed+`, `turn-`, `turn+`, `slider:speed`, `slider:turn`, `straight`, `ai`; ползунки тянутся прежним `BeginSlider/DragSlider`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | :52-79 ползунки | Отступ ползунка 3 от края поля (§11.7). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardMoveBlockTests.cs` | прежние проверки блока | Режим: прямоугольники `card-command:*` внутри панели, клик по ▲ меняет скорость на +2, перетаскивание ползунка. |

## Public API after the change

`internal const float MoveHeight = 91` (режим).

## Implementation steps

1. Раскладка движения.
2. Команды и ползунки.
3. Тесты.

## Out of scope

Другие фазы.

## Invariants

В Classic блок прежний.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `PilotCardMoveBlockTests`
- `PilotCardCommandsInputTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardMoveBlockTests|FullyQualifiedName~PilotCardCommandsInputTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us13-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardMoveBlock.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US13-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Раскладка — UiSpec §11.7.

## Исполнение

Не начато.


### Результат us13-tk2

Компактная панель 150x91: два ряда значений, шаги, ползунки, Прямо/ИИ. 4/4 targeted: +2, min/max drag, возврат ИИ; Classic сохранён. Визуальная сверка move-panel-column-x2.png с Reference/panels/3-move-column-x2.png: геометрия и состав совпадают; живые портреты и BattleTypography по A-16, кнопки стрелок ui.button по UiSpec (эталон показывает светлый вариант). Evidence PNG рядом с логами. Самопроверка пройдена.

Проверки: 4/4 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us13-tk2/client.trx`, `us13-tk2-test.log`, `us13-tk2-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
