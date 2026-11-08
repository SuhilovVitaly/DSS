---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0005-map-presentation
ticket: EP-0019-US-0005-TK-0002-movement-overlays
title: Слои фазы движения
stage: draft
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0005-TK-0001-markers-labels-and-edge]
files_touched: 3
serves: [US5-AC3]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Слои фазы движения

## Why

В движении карта показывает только планы своих дронов.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-11. Область достижимости, ручка и «призрак» старого рендера не рисуются; перетаскивание конца пути сохраняется (хит-тест по квадрату конца).

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | :195-245 DrawMovement | Пути и концы по DesignSpec §8.5, путь — `TacticalOrderPreview.Path`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleOrders.cs` | :161 Hit | Хит-тест конца пути по квадрату 5/4 ед. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementOrdersTests.cs` | — | Проверка перетаскивания конца пути. |

## Public API after the change

No public API change.

## Implementation steps

1. Отрисовать пути всех своих дронов (выбранный — 3 со свечением, остальные 1.3/альфа 150), квадраты концов, «Конец пути».
2. Зарезервировать квадраты и выноску для `Label`.
3. Тест: пробы сцены `1-move` (§12.4) и прямоугольники концов путей в модели раскладки.

## Out of scope

Прогноз противника.

## Invariants

Путь берётся из модели приказов, а не вычисляется в рендере.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleMapRenderTests.Move_overlays_follow_design_spec`
- `MovementOrdersTests.Endpoint_drag_still_aims_order`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleMapRenderTests|FullyQualifiedName~MovementOrdersTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us5-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleOrders.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Геометрия — DesignSpec §8.5.

## Исполнение

Не начато.
