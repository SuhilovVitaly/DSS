---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0005-map-presentation
ticket: EP-0019-US-0005-TK-0001-markers-labels-and-edge
title: Маркеры, подписи и носители за краем
stage: done
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0002-TK-0003-phase-camera-fit]
files_touched: 2
serves: [US5-AC1, US5-AC2, US5-AC5]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Маркеры, подписи и носители за краем

## Why

Подписи — самая чувствительная к расхождениям часть: алгоритм надо перенести буквально.

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Отключённый и уничтоженный дрон — прежние правила видимости; оформление отключённого — маркер с альфой 140 (в сценах проверки не встречается).

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | :312-415 юниты, :467-561 подписи, :593-683 метки и раскладка, :41-64 слои противника | Заменить на маркеры, подписи и метки за краем DesignSpec §8.2–8.3; удалить слои A-09 и старые подписи/полосы. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs` | растровые проверки карты | Обновить ожидания под новый стиль. |

## Public API after the change

No public API change.

## Implementation steps

1. Реализовать DesignSpec §8.2–8.3: маркеры, подписи (отступы `0,10,20,32,46,62,80`, стороны по типу, выноски), метки за краем; занятые прямоугольники = маркеры + `PanelRects` + края + выноски.
2. Удалить слои A-09.
3. Тест: прямоугольники подписей сцены `2-fire` в модели раскладки равны результату алгоритма §8.3, посчитанному в тесте; пробы маркеров проходят.

## Out of scope

Слои фаз (TK-0002/0003).

## Invariants

Порядок обхода подписей фиксирован: выбранный, носители, затем по `y`.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `BattleMapRenderTests.Label_layout_follows_design_spec_fire`
- `BattleMapRenderTests.Offscreen_carrier_edge_marker`
- `BattleMapRenderTests.Enemy_forecast_and_rear_layers_absent`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~BattleMapRenderTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us5-tk1" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US5-AC1, US5-AC2, US5-AC5 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Алгоритм — DesignSpec §8.2–8.3.

## Исполнение

Не начато.

Scope: прежний TacticalBattleMapRenderer.cs заменён BattleMapRenderer.cs с тем же owning type; phase layers вынесены в partial-файлы следующих тикетов. Новый renderer использует только BattleDesign/BattleTypography. HoverUnitId добавлен к immutable frame для резерва карточки.

## Проверенное исполнение 2026-11-08 (продолжение)

Replace map markers labels and carrier edge layout with design algorithm

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0019/20261108-resume/us5-tk1. Self-review выполнен; это не независимое review. Build в составе test run, scoped whitespace и diff-check проверены перед публикацией. Остаточные native/OS gates отражаются в ExecutionStatus.
