---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0004-phase-panel
ticket: EP-0019-US-0004-TK-0005-outcome-details
title: Итог боя
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0004-TK-0004-salvo-and-results-details]
files_touched: 3
serves: [US4-AC5]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Итог боя

## Why

Конец боя показывается в той же панели (A-02, Q-03).

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Причины: `CarrierDestroyed` → «носитель X уничтожен»; `NoDrones` → «у противника/у вас не осталось дронов»; `Timeout` → «достигнут лимит N ходов».

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Outcome.cs` | нет | Детали DesignSpec §6.6. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :22 _newBattle, :494 ExitAction, :239 Esc на итоге | Кнопки «Новый бой» и «Выйти [Esc]». |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleEndTests.cs` | итог боя | Перевести на новую панель. |

## Public API after the change

No public API change.

## Implementation steps

1. Перенести детали итога и зону действия.
2. «Новый бой» — прежний маршрут New battle (`CanStartNewBattle`).
3. Тест: итог seed 1 (ход 20, ПОБЕДА, 30 выстрелов, 18 попаданий, потери свои 1 · враг 6).

## Out of scope

Экран подготовки.

## Invariants

Маршрут выхода и возврата камеры мира не меняется.

## Tests (исходный план; актуальные проверки ниже)

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleEndTests.Outcome_panel_matches_seed1`
- `BattleEndTests.New_battle_and_exit_buttons`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleEndTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us4-tk5" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Outcome.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleEndTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Значения итога — проверяются тестом на реальном доигрывании.

## Исполнение

См. проверенное исполнение ниже.

## Проверенное исполнение 2026-11-08 (продолжение)

Show outcome reasons totals and preserve new battle and world return routes

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us4-tk5. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.

## Фактическая привязка проверок

PhaseOutcomeTests; BattleNavigationTests; BattleEndTests. Плановые имена тестов заменены реализацией тех же AC в указанных owning classes; результаты и paths — ExecutionStatus.md. Source scope разнесён по partial-компонентам overlay/map/phase для сохранения session ownership; никакой Engine-side реализации не добавлялось.
