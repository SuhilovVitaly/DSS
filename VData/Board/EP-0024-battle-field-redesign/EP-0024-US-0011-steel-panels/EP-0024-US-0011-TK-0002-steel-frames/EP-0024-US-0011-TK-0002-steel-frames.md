---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0011-steel-panels
ticket: EP-0024-US-0011-TK-0002-steel-frames
title: Прямые углы, тёмный скос и тень плит
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0011-TK-0001-steel-roles, EP-0024-US-0001-TK-0002-tabletop-mode]
files_touched: 4
serves: [US11-AC2]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Прямые углы, тёмный скос и тень плит

## Why

Стальная тема — прямые углы и тёмные кромки со скосом (D-11, «бордеры делай не светлыми а тоже темными»); сейчас `Round` рисует скруглённый прямоугольник с одной рамкой.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-17. Признак режима — метрика `ui.style.steel`; доступ — через `TacticalBattleSkinRenderScope` (как признак `Tabletop` из US-0001-TK-0002).

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs` | доступ к ролям, признак `Tabletop` | `internal static bool Steel` (метрика `ui.style` → `steel` = 1). |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | :109-128 `Round`, `PanelBox`; :129-143 `Tag`, `BoxText`, `Button` | При `Steel`: радиус 0; рамка — `stroke` роли (плиты 1,2, кнопки и поля 0,8); скос по UiSpec §11.3 из роли `ui.panel.bevel` (у `ui.box`, `ui.tag`, `ui.box.build-slot` — наоборот); тень плит `#00000080` (+2,4; +3,2); без `*.glow`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | :198-203 `CommandButton` | При `Steel` — те же правила, что у `Button`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopPanelFrameTests.cs` | — | Новый файл: пиксели скоса и рамки окна фазы и кнопки в `tabletop` (UiSpec §11.13.4), Classic без изменений. |

## Public API after the change

`internal static bool TacticalBattleSkinRenderScope.Steel`.

## Implementation steps

1. Добавить признак `Steel`.
2. Ветка `Steel` в `Round` и `PanelBox`: углы, рамка, скос, тень, без свечения.
3. Проверить кнопки, ярлыки и поля окна фазы, сборки, итога, наведения и итога боя на эталонах.
4. Тесты пикселей.

## Out of scope

Цвета, зашитые в коде (TK-0003); узкая колонка (US-0012).

## Invariants

Без метрики `ui.style.steel` вид панелей прежний; `Layout.Rects` окон не меняются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopPanelFrameTests`
- `Ep0022SkinScenesTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopPanelFrameTests|FullyQualifiedName~Ep0022SkinScenesTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us11-tk2" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSkinRenderScope.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US11-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Цвета и толщины — UiSpec §11.2–§11.3.

## Исполнение

Не начато.
