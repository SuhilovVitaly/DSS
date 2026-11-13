---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0012-slim-column
ticket: EP-0024-US-0012-TK-0001-slim-layout
title: Раскладка узкой колонки
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0011-TK-0002-steel-frames]
files_touched: 5
serves: [US12-AC1]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Раскладка узкой колонки

## Why

Узкие карточки 150×40 и панель под выбранной требуют своей раскладки колонки: высоты, сдвиг карточек под панелью, показ выбранной вместе с панелью.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-17. Признак — метрика `ui.pilot-column.slim`. Высота панели берётся из блоков US-0013 (`MoveHeight`, `FireHeight`, `SalvoHeight` в режиме); до них — постоянная 91.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs` | признаки режима | `internal static bool SlimPilots` (метрика `ui.pilot-column` → `slim` = 1). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotColumnLayout.cs` | :7-43 `BaseHeight` 82, `Gap` 5, `Viewport`, раскладка и показ выбранной | Режим узкой колонки: карточки 150×40 от (6, 6), шаг 45; после выбранной — вставка `panelHeight + 4`; `Visible`, `Bounds`, `CompactBottom` учитывают панель; правило показа выбранной — карточка вместе с панелью (UiSpec §11.4). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | :92-129 `Arrange`, `ExpandedHeight` | В режиме: высота карточки 40, высота панели — отдельно, передаётся в раскладку. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | :98-101 занятые прямоугольники колонки | В режиме — прямоугольник колонки и прямоугольник панели (UiSpec §11.4). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopColumnLayoutTests.cs` | — | Новый файл: положения карточек и вставка панели при выборе 1-й, 2-й и 4-й карточки; прокрутка при 8 дронах; показ выбранной с панелью. |

## Public API after the change

`PilotColumnLayout` с режимом узких карточек (конструктор принимает высоту панели выбранной карточки).

## Implementation steps

1. Признак `SlimPilots`.
2. Раскладка узких карточек и вставка панели.
3. Занятые прямоугольники карты.
4. Тесты раскладки.

## Out of scope

Отрисовка карточки (TK-0002) и панели (US-0013).

## Invariants

Без метрики раскладка прежняя: `BaseHeight` 82, Gap 5.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopColumnLayoutTests`
- `PilotCardColumnTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopColumnLayoutTests|FullyQualifiedName~PilotCardColumnTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us12-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotColumnLayout.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US12-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Размеры — UiSpec §11.4, §11.6; пробы — §11.13.1.

## Исполнение

Не начато.
