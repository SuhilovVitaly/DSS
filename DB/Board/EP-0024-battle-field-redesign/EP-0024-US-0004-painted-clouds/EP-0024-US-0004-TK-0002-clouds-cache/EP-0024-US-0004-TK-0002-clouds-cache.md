---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0004-painted-clouds
ticket: EP-0024-US-0004-TK-0002-clouds-cache
title: Кэш облаков и бюджет времени
stage: approved
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0004-TK-0001-clouds-render]
files_touched: 3
serves: [US4-AC2]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Кэш облаков и бюджет времени

## Why

Шум и размытие на весь экран дорогие (R-2).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-07.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PaintedCloudLayer.cs` | отрисовка (TK-0001) | Кэш: изображение размером видимой области в пикселях устройства, ключ UiSpec §4.2, счётчик сборок `BuildCount` для тестов; освобождение при выходе из экрана. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | :135-147 владение `InteractiveAnomalyField` | Владение слоем облаков режима и его освобождение. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopCloudTests.cs` | — | Повторный кадр не пересобирает кэш; смена камеры пересобирает; замер времени пересборки (информационный, с записью). |

## Public API after the change

`internal int PaintedCloudLayer.BuildCount { get; }`.

## Implementation steps

1. Добавить кэш и счётчик.
2. Освобождение ресурсов вместе с экраном.
3. Тесты и замер времени при 1920×1080.

## Out of scope

—

## Invariants

Изображение кэша не используется после смены скина.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopCloudTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopCloudTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us4-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PaintedCloudLayer.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US4-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Ключ и бюджет — UiSpec §4.2.

## Исполнение

Расширение Code context перед реализацией: `BattleMapRenderer.cs` получает принадлежащий экрану слой; `UI/SkiaWindow.cs` передаёт существующий GRContext в RenderNative. Это необходимо для кэша в ресурсах текущего GPU-контекста, без глобального кэша и медленной CPU-пересборки в игровом окне. Headless сохраняет CPU-путь. Контекст не принадлежит слою и не освобождается им.


### Результат us4-tk2

Кэш по view/scale/clumps, GPU-context path, освобождение экраном проверены. 12 ms gate НЕ закрыт: CPU raster 1627.97/1519.05/1558.23 ms; native GPU NOT RUN. Evidence: cloud-raster-timing.json. Проба центра уточнена на максимальный видимый сгусток: глобальный максимум вне кадра; цвет ближе к тону жара, чем фону.

Проверки: 7/7 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us4-tk2/client.trx`, `us4-tk2-test.log`, `us4-tk2-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
