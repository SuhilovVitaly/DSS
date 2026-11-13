---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0009-view-regression
ticket: EP-0024-US-0009-TK-0002-panel-snapshots
title: Снимки и пробы панелей `tabletop`
stage: draft
layer: client-tests
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0009-TK-0001-snapshots, EP-0024-US-0011-TK-0003-role-colours, EP-0024-US-0013-TK-0002-panel-move, EP-0024-US-0013-TK-0003-panel-fire, EP-0024-US-0013-TK-0004-panel-salvo]
files_touched: 3
serves: [US9-AC3]
created: 2026-11-12T17:00:00Z
revision: 1
---

# Снимки и пробы панелей `tabletop`

## Why

Снимки `tabletop` должны показывать ту же картинку, что эталон панелей, а проверка равенства раскладки скинов — не ломаться от узкой колонки.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) §11; всё, что там не названо, остаётся как в Classic на базе. Эталон — [Materials/Reference/panels](../../Materials/Reference/panels). Утверждение — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-13, A-17.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | :31-109 равенство `Layout.Rects` и текстов с Classic, `phase` bottom, пиксель (360, 92), высота выбранной > 82 | Для `tabletop` из сравнения исключаются ключи `pilot:*`, `pilot-panel:*`, `card-command:*`, `pilot-decoy:*`; проверка «выбранная выше 82» заменяется пробами §11.13.1; остальное — прежнее. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/TabletopPanelProbeTests.cs` | — | Новый файл: пробы UiSpec §11.13.1–§11.13.5. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots/tabletop` | снимки US-0009-TK-0001 | Пересоздать с панелями по `DSS_EP0024_ACCEPT_SNAPSHOTS=1` после просмотра и сверки с `Materials/Reference/panels`. |

## Public API after the change

—

## Implementation steps

1. Исключения в `Ep0022SkinScenesTests`.
2. Пробы панелей.
3. Пересоздать и просмотреть снимки `tabletop`; записать сверку в ExecutionStatus.

## Out of scope

Снимки Classic.

## Invariants

15 снимков Classic не пересоздаются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `Ep0022SkinScenesTests`
- `TabletopPanelProbeTests`
- `TabletopSnapshots`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~Ep0022SkinScenesTests|FullyQualifiedName~TabletopPanelProbeTests|FullyQualifiedName~TabletopSnapshots" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us9-tk2" --logger "trx;LogFileName=client.trx"

git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Получившийся кадр сверен на глаз с `Materials/Reference/panels` (тот же кадр и вырезка ×2), расхождения записаны в «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Пробы — UiSpec §11.13; эталон — `Materials/Reference/panels`.

## Исполнение

Не начато.
