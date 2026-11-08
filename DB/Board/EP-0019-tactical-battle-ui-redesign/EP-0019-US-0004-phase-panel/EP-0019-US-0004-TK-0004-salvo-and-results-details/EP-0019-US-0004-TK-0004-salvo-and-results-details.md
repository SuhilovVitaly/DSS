---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0004-phase-panel
ticket: EP-0019-US-0004-TK-0004-salvo-and-results-details
title: Детали залпа и результатов
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0004-TK-0001-phase-panel-container-and-header]
files_touched: 4
serves: [US4-AC4]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Детали залпа и результатов

## Why

Воспроизведение залпа и его итог получают новые панели.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Активная строка очереди — текущий `FireStep` из `TacticalBattleResults.Project(now)`; «Выстрел k из n» — номер текущего фактического выстрела.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs` | нет | Детали DesignSpec §6.4 и §6.5. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | :56 InFlight, :58 Project, :84 Capture | Источник активного шага и итогов. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :486 AdvanceSalvo, :501 ReplaySalvo | Кнопки и Enter. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | воспроизведение залпа | Новые проверки панели. |

## Public API after the change

No public API change.

## Implementation steps

1. Реализовать очередь огня DesignSpec §6.4 (строка 13,6, плашка активной строки, цвета).
2. Перенести результаты выстрела своего стрелка (первый свой выстрел залпа; если своих выстрелов нет — первый выстрел залпа).
3. Зоны действия: «Снаряды в полёте» / «Выстрел k из n» / «Пропустить полёт [Enter]»; «Залп завершён» / «N выстрела · M попадание» / «↺ Повтор» + «К движению [Enter]» (после последнего хода — «К итогу боя [Enter]»).
4. Тесты: активная строка следует часам; значения результатов хода 3 seed 1.

## Out of scope

Анимация снаряда на карте (US-0005).

## Invariants

Тайминги `TacticalBattleResults` не меняются.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `ResultsPhaseTests.Fire_queue_highlights_active_step`
- `ResultsPhaseTests.Results_panel_matches_seed1_turn3`
- `ResultsPhaseTests.Replay_and_continue_buttons`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~ResultsPhaseTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us4-tk4" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Salvo.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Геометрия — DesignSpec §6.4–6.5.

## Исполнение

Не начато.

## Проверенное исполнение 2026-11-08 (продолжение)

Present admitted fire queue results replay and continue actions

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us4-tk4. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.
