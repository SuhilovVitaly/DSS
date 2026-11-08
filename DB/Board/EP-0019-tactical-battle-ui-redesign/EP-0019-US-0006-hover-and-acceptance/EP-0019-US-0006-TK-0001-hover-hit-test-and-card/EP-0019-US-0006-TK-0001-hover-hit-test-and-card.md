---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0006-hover-and-acceptance
ticket: EP-0019-US-0006-TK-0001-hover-hit-test-and-card
title: Хит-тест наведения и карточка
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0003-TK-0003-pilot-card-rendering-and-selection, EP-0019-US-0004-TK-0005-outcome-details, EP-0019-US-0005-TK-0003-fire-salvo-and-results-overlays]
files_touched: 4
serves: [US6-AC1, US6-AC3]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Хит-тест наведения и карточка

## Why

Сейчас наведения на юниты нет (`TacticalBattleScreen.cs:415-442`).

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Курсор над панелью — без наведения на карту. Внутри панели фазы и колонки карточек наведение не показывается.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :415-442 OnMouseMove, internal `HoverUnitId` (US-0001) | Хит-тест юнитов по экранным позициям; `HoverUnitId` управляется мышью; `PanelRects` учитывает карточку наведения. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs` | нет | Карточка DesignSpec §7 (противник/пилот, портрет, нижняя строка). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | — | Метка наведения DesignSpec §8.9 (уголки 14/5). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MouseRoutingTests.cs` | — | Проверки наведения. |

## Public API after the change

No public API change.

## Implementation steps

1. Хит-тест (ближайший юнит в радиусе), игнорировать точки над панелями.
2. Реализовать карточку §7 и метку §8.9.
3. Тесты: наведение на Таг-6 и Олега, уход курсора, выбор и приказы не меняются.

## Out of scope

Наведение на элементы панелей.

## Invariants

Наведение не вызывает `ObserveOrders` и не меняет `_orders`/`_fireOrders`.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `MouseRoutingTests.Hover_enemy_shows_card_and_corners`
- `MouseRoutingTests.Hover_own_pilot_shows_portrait_card`
- `MouseRoutingTests.Hover_does_not_change_selection_or_orders`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~MouseRoutingTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us6-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/HoverCard.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MouseRoutingTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC1, US6-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Геометрия — DesignSpec §7, §8.9.

## Исполнение

Не начато.

## Проверенное исполнение 2026-11-08 (продолжение)

Show unit hover card and map corners without changing selection or orders

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us6-tk1. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.
