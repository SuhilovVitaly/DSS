---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0003-pilot-cards
ticket: EP-0019-US-0003-TK-0003-pilot-card-rendering-and-selection
title: Отрисовка карточек и выбор кликом
stage: draft
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0003-TK-0002-pilot-card-projection]
files_touched: 3
serves: [US3-AC1, US3-AC2, US3-AC5]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Отрисовка карточек и выбор кликом

## Why

Колонка карточек — основной список пилотов нового экрана.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Роль скина `ui.pilotCard` с подролями `.chip`, `.bar.armor`, `.bar.hull`, `.bar.mind`.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | нет | Отрисовка карточки DesignSpec §5 по данным `PilotCardModel`; хит-тест карточек. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | Render / OnMouseDown / ObserveOrders :318-334 | Рисовать колонку после панели фазы; ЛКМ по карточке → `_orders.Select`/`_fireOrders.Select`; прямоугольник колонки в `PanelRects`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardColumnTests.cs` | нет | Растровые и входные тесты. |

## Public API after the change

`internal sealed class PilotCardColumn { void Draw(SKCanvas, IReadOnlyList<PilotCard>, PilotPortraitCatalog); string? Hit(SKPoint designPoint); SKRect Bounds; }`.

## Implementation steps

1. Реализовать карточку DesignSpec §5 буквально (координаты, размеры, цвета, подбор строки фазы 8,5→7).
2. Плашка с инициалами при отсутствии портрета (`Box`, Bold 11, инициалы имени).
3. Клик и Tab синхронны; выбранная карточка — жёлтая рамка.
4. Тест: в сцене `2-fire` раскладка колонки (§12.2) и пробы карточек (§12.4) проходят.

## Out of scope

Карточка наведения (US-0006).

## Invariants

Клик по карточке не отправляет команд.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `PilotCardColumnTests.Column_layout_and_probes_fire`
- `PilotCardColumnTests.Click_selects_drone_in_move_and_fire`
- `PilotCardColumnTests.Missing_portrait_draws_initials`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardColumnTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us3-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardColumnTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US3-AC1, US3-AC2, US3-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Геометрия — DesignSpec §5; данные — TK-0002.

## Исполнение

Не начато.
