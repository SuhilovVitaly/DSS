---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0002-overlay-layout-shell
ticket: EP-0019-US-0002-TK-0003-phase-camera-fit
title: Камера по фазам
stage: draft
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0002-TK-0002-full-screen-map-shell]
files_touched: 2
serves: [US2-AC4]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Камера по фазам

## Why

Подписи и маркеры встанут по спецификации, только если камера ставит поле по формуле DesignSpec §8.4.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Безопасная область `202…Wd−246 × 6…714` макета; поле 50 (движение) и 70 (остальные фазы).

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleCamera.cs` | :22-23 поле/минимум, :33-91 Update/Fit, :96-122 Zoom/Pan | Fit по DesignSpec §8.4: охват по фазе, единый масштаб по осям, центр охвата в центре безопасной области; автоматический Fit при смене фазы и по `0`; минимальную ширину 600 убрать для нового Fit. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleCameraTests.cs` | проверки Fit/Zoom/Pan | Добавить проверку на снимках seed 1. |

## Public API after the change

`TacticalBattleCamera.Fit(TacticalBattleSnapshot, TacticalBattlePhase, BattleViewport, IReadOnlyList<Pose> forecastEnds)`.

## Implementation steps

1. Реализовать Fit DesignSpec §8.4: движение — все юниты, `Paths`, концы прогнозов своих дронов; остальные фазы — дроны и `Paths`.
2. Вызвать Fit при входе в каждую фазу и при `0`; пользовательский зум/пан до следующей смены фазы сохраняется.
3. Тест: для снимков ходов 1 и 3 seed 1 масштаб и центр совпадают с формулой DesignSpec §8.4, посчитанной в тесте из данных `battle-data-seed1.txt`.

## Out of scope

Анимация камеры.

## Invariants

Колесо (fitted×4 … ×0,125) и панорама ПКМ работают как раньше.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleCameraTests.Phase_fit_matches_formula_for_seed1_move`
- `BattleCameraTests.Phase_fit_matches_formula_for_seed1_fire`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleCameraTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us2-tk3" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleCamera.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleCameraTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US2-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Алгоритм полностью в DesignSpec §8.4.

## Исполнение

Не начато.
