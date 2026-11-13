---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0009-view-regression
ticket: EP-0024-US-0009-TK-0001-snapshots
title: Снимки `tabletop` и проверка Classic
stage: draft
layer: client-tests
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0007-TK-0002-torpedo-fx, EP-0024-US-0007-TK-0001-salvo, EP-0024-US-0008-TK-0001-camera, EP-0024-US-0004-TK-0002-clouds-cache, EP-0024-US-0005-TK-0001-templates]
files_touched: 4
serves: [US9-AC1, US9-AC2]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Снимки `tabletop` и проверка Classic

## Why

Новый вид поля нужно зафиксировать, не меняя эталонов Classic.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-13.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/TabletopSnapshots.cs` | — | Новый файл по образцу `Ep0019Snapshots`: рендер сцен в скине `tabletop`, сравнение, принятие по `DSS_EP0024_ACCEPT_SNAPSHOTS`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots/tabletop` | — | 8 PNG. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | :38-80 список скинов | Добавить `tabletop`. |
| `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj` | :26 `TacticalBattle\View\Snapshots\*.png` | Добавить правило для `TacticalBattle\View\Snapshots\tabletop\*.png`. |

## Public API after the change

—

## Implementation steps

1. Тест снимков нового скина.
2. Принять снимки после просмотра и сверки с эталонами.
3. Прогнать категории Classic без принятия.

## Out of scope

—

## Invariants

15 снимков Classic не пересоздаются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopSnapshots`
- `Ep0022SkinScenesTests`
- `Ep0019ViewTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
$env:DSS_EP0024_ACCEPT_SNAPSHOTS = "1"; dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopSnapshots" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us9-accept"; Remove-Item Env:DSS_EP0024_ACCEPT_SNAPSHOTS
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopSnapshots|FullyQualifiedName~Ep0022SkinScenesTests|Category=Ep0019View|Category=Ep0020View" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us9-verify" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC1, US9-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Сцены и механизм принятия указаны; эталоны — в материалах.

## Исполнение

Не начато.
