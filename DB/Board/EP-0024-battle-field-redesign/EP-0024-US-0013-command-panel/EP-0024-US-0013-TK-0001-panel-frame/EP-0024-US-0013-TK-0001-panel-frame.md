---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0013-command-panel
ticket: EP-0024-US-0013-TK-0001-panel-frame
title: Панель команд: положение, плита, ввод
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0012-TK-0002-slim-card]
files_touched: 3
serves: [US13-AC1]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Панель команд: положение, плита, ввод

## Why

Панель команд выбранного пилота открывается снизу справа с отступом 30 % (14.03), карточки ниже сдвигаются.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-19. Команды регистрируются прежним `RegisterCommand`; меняется только прямоугольник.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | :182-196 `DrawExpanded`; :32-50 `RegisterCommand`, `HitCommand` | В режиме блок рисуется в панели `(card.Left + 45, card.Bottom + 3)` шириной 150 со своей плитой и язычком (UiSpec §11.6); внутренние поля x 6…144; `Rects["pilot-panel:<id>"]`; обрезка областью колонки как сейчас. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` | :19-28 `OverlayRects`; :46-78 клики; :319-327 колесо | Прямоугольник панели входит в `OverlayRects`; клик по панели не уходит на карту; колесо над панелью — список целей, затем колонка. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopCommandPanelTests.cs` | — | Новый файл: положение и размер панели (проба §11.13.1), язычок, клик по панели не выбирает юнит на карте. |

## Public API after the change

—

## Implementation steps

1. Плита и язычок панели.
2. Перенос блока в панель, регистрация команд.
3. Ввод: `OverlayRects`, колесо.
4. Тесты.

## Out of scope

Содержимое блоков (TK-0002…TK-0004).

## Invariants

Имена команд и клавиши прежние.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopCommandPanelTests`
- `PilotCardCommandsInputTests`
- `OverlayBoundaryTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopCommandPanelTests|FullyQualifiedName~PilotCardCommandsInputTests|FullyQualifiedName~OverlayBoundaryTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us13-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US13-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Геометрия — UiSpec §11.6, ввод — §11.11.

## Исполнение

Не начато.
