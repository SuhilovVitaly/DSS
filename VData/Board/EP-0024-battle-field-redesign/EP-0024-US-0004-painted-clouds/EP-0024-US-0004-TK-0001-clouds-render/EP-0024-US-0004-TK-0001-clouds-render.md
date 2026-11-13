---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0004-painted-clouds
ticket: EP-0024-US-0004-TK-0001-clouds-render
title: Отрисовка облаков в режиме
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0001-TK-0002-tabletop-mode]
files_touched: 4
serves: [US4-AC1, US4-AC3]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Отрисовка облаков в режиме

## Why

Облака в новом стиле — часть вида поля (D-01).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

—

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PaintedCloudLayer.cs` | — | Новый файл: отрисовка по UiSpec §4.1 (прототип — `DrawPaintedClouds`/`Layer`). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | :51-52, :265-269 `DrawAnomalies` | В режиме — новый слой, `InteractiveAnomalyField.Prepare` не вызывается. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs` | :22-24 облака в секторе | В режиме не рисовать. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopCloudTests.cs` | — | Новый файл: пробы §10.5 (цвет в центре сгустка), Classic без изменений. |

## Public API after the change

`internal sealed class PaintedCloudLayer { void Draw(SKCanvas c, TacticalBattleFrame f); }`.

## Implementation steps

1. Перенести отрисовку из прототипа.
2. Подключить в режиме вместо прежнего слоя.
3. Тесты на `2-anomaly-absorption`, `1-anomaly-move-dust`.

## Out of scope

Кэш (TK-0002).

## Invariants

Прежние слои облаков не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopCloudTests`
- `AnomalyFieldLayerTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopCloudTests|FullyQualifiedName~AnomalyFieldLayerTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us4-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PaintedCloudLayer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC1, US4-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Параметры слоёв и тона — UiSpec §4.1.

## Исполнение

Не начато.
