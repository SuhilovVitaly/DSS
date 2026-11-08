---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0004-phase-panel
ticket: EP-0019-US-0004-TK-0003-fire-details-and-target-click
title: Детали стрельбы и назначение цели
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0004-TK-0001-phase-panel-container-and-header]
files_touched: 4
serves: [US4-AC3]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Детали стрельбы и назначение цели

## Why

Панель стрельбы заменяет `TacticalBattleFirePanel` и выпадающий список целей (A-10).

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Q-04 по умолчанию: цель назначается ЛКМ по дрону противника из списка кандидатов выбранного стрелка; повторный клик по той же цели ничего не меняет; «Не стрелять» снимает цель.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs` | нет | Детали DesignSpec §6.3. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs` | :59 Choose, :83-106 ChooseAt | Клик по противнику → `Choose(selected, target)`; «Не стрелять» → `Choose(selected, null)`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :364-391 хит-тест карты | Привязать клик в фазе огня. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs` | выбор цели | Перевести на клик по карте и новую панель. |

## Public API after the change

No public API change.

## Implementation steps

1. Перенести детали стрельбы.
2. Поле брони цели: «Сначала броня ({n}), затем корпус» / «Броня снята — урон в корпус».
3. Зона действия: `{K} стреляют · {M} без цели` / «План готов к отправке» / «Выполнить залп [Enter]».
4. Тесты: значения Марта → Таг-4 (56 %, 108,1, 16,1°, Меч I 50–75), клик по Таг-6 вне списка не меняет цель, «Не стрелять» убирает стрелка из запроса.

## Out of scope

Отрисовка сектора и стрелок (US-0005).

## Invariants

Запрос огня формирует `BuildTargets()`; список кандидатов — из снимка.

## Tests (исходный план; актуальные проверки ниже)

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `FirePlanningTests.Fire_panel_shows_reference_values_for_marta`
- `FirePlanningTests.Map_click_assigns_candidate_only`
- `FirePlanningTests.Hold_fire_removes_target`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~FirePlanningTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us4-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Fire.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFireOrders.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/FirePlanningTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Значения — DesignSpec §12.3.

## Исполнение

См. проверенное исполнение ниже.

## Проверенное исполнение 2026-11-08 (продолжение)

Show fire details and assign only admissible targets by map click

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us4-tk3. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.

## Фактическая привязка проверок

PhaseFireInputTests; FirePlanningTests. Плановые имена тестов заменены реализацией тех же AC в указанных owning classes; результаты и paths — ExecutionStatus.md. Source scope разнесён по partial-компонентам overlay/map/phase для сохранения session ownership; никакой Engine-side реализации не добавлялось.
