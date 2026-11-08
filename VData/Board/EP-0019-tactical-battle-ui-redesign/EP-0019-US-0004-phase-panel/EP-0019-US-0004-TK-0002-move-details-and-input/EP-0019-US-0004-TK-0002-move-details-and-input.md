---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0004-phase-panel
ticket: EP-0019-US-0004-TK-0002-move-details-and-input
title: Детали движения и ввод
stage: draft
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0004-TK-0001-phase-panel-container-and-header]
files_touched: 3
serves: [US4-AC2]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Детали движения и ввод

## Why

Панель движения заменяет `TacticalBattleMovePanel`.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Ползунки перетаскиваются мышью и меняются стрелками (как `TacticalBattleScreen.cs:263-269`); «Вернуть ИИ» = прежнее «Предложение ИИ».

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Move.cs` | нет | Детали DesignSpec §6.2, ползунки с перетаскиванием, кнопки, подсказка. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleOrders.cs` | :86-90 Adjust/Straight/Suggest, :145 Capture | Источник значений и действий. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementOrdersTests.cs` | приказы движения | Перевести на новую панель. |

## Public API after the change

No public API change.

## Implementation steps

1. Перенести отрисовку деталей движения.
2. Перетаскивание ползунков → `Adjust`; кнопки → `Straight`/`Suggest`.
3. Зона действия: `{N} предложения ИИ` (кол-во нетронутых приказов) / статус «Движение не выполнено» / «Выполнить движение [Enter]».
4. Тесты: значения и пределы для Веры (77,5; 50,0–77,5; ±38,9°) и отправленный приказ после перетаскивания.

## Out of scope

Перетаскивание конца пути на карте (остаётся как есть, US-0005).

## Invariants

Пределы — `TacticalLimits`; панель не дублирует формулы движка (предел поворота берётся из модели приказов).

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `MovementOrdersTests.Move_panel_shows_reference_values_for_vera`
- `MovementOrdersTests.Slider_drag_changes_issued_order`
- `MovementOrdersTests.Straight_and_restore_ai`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~MovementOrdersTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us4-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Move.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleOrders.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MovementOrdersTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Числа — DesignSpec §12.3 и battle-data-seed1.txt.

## Исполнение

Не начато.
