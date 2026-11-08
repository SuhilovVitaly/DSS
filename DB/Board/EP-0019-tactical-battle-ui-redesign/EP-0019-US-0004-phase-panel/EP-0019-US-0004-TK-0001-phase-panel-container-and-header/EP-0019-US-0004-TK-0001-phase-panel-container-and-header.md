---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0004-phase-panel
ticket: EP-0019-US-0004-TK-0001-phase-panel-container-and-header
title: Контейнер панели и шапка
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0002-TK-0004-localization-strings]
files_touched: 4
serves: [US4-AC1]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Контейнер панели и шапка

## Why

Все фазы делят одну рамку, шапку и зону действия.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Для состояний вне сцен DesignSpec (ожидание ответа сессии, ошибка) — шапка текущей фазы и статус в поле `BoxText` зоны действия.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.cs` | нет | `Layout(phase)` → прямоугольники шапки, деталей (+24) и действия; `DrawHeader` (`TopBar`), `DrawDetailsHead`, `DrawAction(title, status, primary, secondary?)`; хит-тест кнопок. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattlePanelHeader.cs` | Capture :19-63 | Источник хода, фазы, потерь, статуса; отрисовку старой шапки удалить. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | Render | Рисовать `PhasePanel`; его прямоугольник — в `PanelRects`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PhasePanelTests.cs` | нет | Тесты. |

## Public API after the change

`internal sealed class PhasePanel` с `Bounds`, `Draw(...)`, `HitButton(SKPoint)`.

## Implementation steps

1. Реализовать каркас DesignSpec §6.1 и таблицу `DB`.
2. Строка сценария и счёта: `Сценарий · свои A · враг B · потери X:Y` (`FitSize` 10.5).
3. Тест: раскладка и пробы шапки в сцене `1-move` (§12.2, §12.4).

## Out of scope

Содержимое деталей.

## Invariants

Состояние ожидания блокирует кнопки как `TurnBusy` (`TacticalBattleScreen.cs:76-84`).

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `PhasePanelTests.Header_layout_and_probes`
- `PhasePanelTests.Panel_height_follows_phase`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PhasePanelTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us4-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattlePanelHeader.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PhasePanelTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Геометрия — DesignSpec §6.1.

## Исполнение

Не начато.

## Проверенное исполнение 2026-11-08 (продолжение)

Render shared phase header and phase sized action container

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us4-tk1. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.

## Проверенное исполнение 2026-11-08 (продолжение)

Render shared phase header and sized action container

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us4-tk1. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.
