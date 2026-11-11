---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0002-move-in-card
ticket: EP-0023-US-0002-TK-0001-move-block
title: Блок движения в карточке: отрисовка и управление
stage: done
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0023-US-0001-TK-0003-card-commands-input]
files_touched: 8
serves: [US2-AC1, US2-AC2, US2-AC3]
created: 2026-11-11T14:30:00Z
revision: 1
---

# Блок движения в карточке: отрисовка и управление

## Why

Команды движения выбранного дрона должны быть видны и доступны в его карточке (D-02).

## Decisions

Применяются [D-01–D-04](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в EP-0019, EP-0020 и EP-0022. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-04, A-06. Шаги кнопок равны шагам стрелок; ползунки карточки перетаскиваются (а не только кнопками).

## Code context

Строки — `main` @ `8b0bf6ce`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardMoveBlock.cs` | — | Новый файл: высота (UiSpec §5) и отрисовка блока, регистрация команд `speed-`, `speed+`, `turn-`, `turn+`, `straight`, `ai`, ползунков `slider:speed`, `slider:turn`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | функция высоты блока (US-0001-TK-0001) | Для фазы движения возвращать высоту блока движения у выбранной карточки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Move.cs` | :33-40 `Drag` (формула значения по ползунку) | Вынести формулу в общую функцию и вызывать из карточки; панель пока не меняется. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementOrdersTests.cs` | сценарии ползунков и кнопок панели | Дублировать сценарии для карточки (`CardCommandBounds`); проверка равенства `EditedOrders()`. |

Дополнительно: TacticalBattleScreen.cs передаёт кадр в раскладку до карты; PilotCardColumnTests.cs и OverlayBoundaryTests.cs получают новые проверяемые координаты движения; PilotCardMoveBlockTests.cs проверяет рабочий ввод, включая сравнение прикидок с клавиатурой.

## Public API after the change

`internal static class PilotCardMoveBlock { float Height(TacticalOrderPreview o); void Draw(SKCanvas, SKRect, PilotCard, TacticalOrderPreview, PilotCardCommands); }`; `TacticalBattleOrders.ValueAt(...)` или аналог общей формулы.

## Implementation steps

1. Реализовать блок по UiSpec §5 (значения через `F`/`Deg`, строки `Speed`, `TurnLabel`, `Straight`, `RestoreAi`, `MoveHelp`).
2. Подключить команды к маршрутизации US-0001-TK-0003.
3. Тесты: шаги кнопок, перетаскивание, «Прямо», «Вернуть ИИ», равенство с клавишами, блокировка при `Pending`.

## Out of scope

Удаление элементов движения из правой панели (US-0005).

## Invariants

Значения приказов и их границы считаются существующим `TacticalBattleOrders`.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `MovementOrdersTests`
- `PilotCardMoveBlockTests`
- `KeysDuringBattleTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~MovementOrdersTests|FullyQualifiedName~PilotCardMoveBlockTests|FullyQualifiedName~KeysDuringBattleTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0023/<run>/us2-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardMoveBlock.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Move.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC1, US2-AC2, US2-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Геометрия — UiSpec §5; действия и шаги перечислены в критериях истории.

## Исполнение

US2-AC1–AC3: блок 196, кнопки шагов и перетаскивание работают через TacticalBattleOrders. Сверены координаты speed+/ai, равенство с клавиатурой, Прямо, восстановление предложения, предел ползунка и отказ для устаревшего выбора. В существующем контракте восстановление ИИ выдаёт явно заданный приказ со значением Suggestion. Build, 23/23 тестов, scoped format и diff check: D:/DeepSpaceSaga/artifacts/validation/ep-0023/20261111-execution-01/us2-tk1-02/. Самопроверка diff выполнена. Правая панель остаётся промежуточной до US-0005.
