---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0010-anomaly-ui
ticket: EP-0020-US-0010-TK-0001-anomaly-field-layer
title: Слой поля аномалий на карте
stage: draft
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0020-US-0009-TK-0005-anomaly-regression, EP-0020-US-0007-TK-0004-decoy-view-regression]
files_touched: 4
serves: [US10-AC1]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Слой поля аномалий на карте

## Why

Вид аномалий — главный носитель «заранее читаемых эффектов»; он должен совпасть с эталоном EP-0010 по палитре, фактуре и тени.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-30: слой считается на CPU в растр размера окна и кешируется по (камера, размер, набор аномалий); фактура — value-шум §16.3 по мировым координатам, поэтому не плывёт при смене камеры.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/AnomalyFieldLayer.cs` | нет | Растр по §16; кеш. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | EP-0019 §8.1: фон, сетка | Слой после заливки `MapBg`, сетка с `Lighten`; повтор внутри сектора в фазе стрельбы. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | токены | Токены и рампы §15. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/AnomalyFieldLayerTests.cs` | нет | Контрольные пиксели, кеш, мировая привязка. |

## Public API after the change

`AnomalyFieldLayer.Render(IReadOnlyList<TacticalAnomalySnapshot>, TacticalAnomalyRulesSnapshot, Camera, SKSizeI)`.

## Implementation steps

1. Реализовать §16.
2. Встроить в порядок слоёв §16.5.
3. Тесты: 20 точек (центры пятен, края, фон) против формулы; одинаковый результат при сдвиге камеры на целое число пикселей для мировых точек; повторный вызов берёт кеш.

## Out of scope

Остальные элементы (TK-0002, TK-0003).

## Invariants

Без аномалий карта побитно как в EP-0019.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `AnomalyFieldLayerTests.Control_pixels_match_formula`
- `AnomalyFieldLayerTests.Texture_is_world_anchored`
- `AnomalyFieldLayerTests.Layer_is_cached_per_camera`
- `AnomalyFieldLayerTests.No_anomalies_leaves_map_unchanged`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~AnomalyFieldLayerTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us10-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/AnomalyFieldLayer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US10-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Формулы и порядок слоёв в UiSpec §16.

## Исполнение

Не начато.
