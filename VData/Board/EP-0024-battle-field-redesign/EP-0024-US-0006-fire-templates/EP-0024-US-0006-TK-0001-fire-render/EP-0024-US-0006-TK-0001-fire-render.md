---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0006-fire-templates
ticket: EP-0024-US-0006-TK-0001-fire-render
title: Сектор, ленты, угрозы и прицелы
stage: draft
layer: client-ui
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0003-TK-0001-plates]
files_touched: 4
serves: [US6-AC1]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Сектор, ленты, угрозы и прицелы

## Why

План стрельбы в новом виде — ленты и прицелы (D-01).

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

—

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.FireTemplates.cs` | — | Новый файл по образцу прототипа: `Ribbon`, `Reticle`, отрисовка плана (UiSpec §6.2–§6.6, §6.8); ленты и прицелы кладут прямоугольники в занятые (§6.3, §1.4), слой фазы рисуется до моделей и плашек. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs` | :12-63 | В режиме — новая отрисовка вместо `DrawFireSector`, `Arrow`, `DrawAlternativeTargets`; выноски пыли и поглощения сохраняются. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs` | :192-202 `CreateFireSector` | Без изменений, используется для сектора. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TabletopFireTests.cs` | — | Новый файл: наличие лент и прицелов, цвета прицелов (§10.2), отрезки поглощения на `2-anomaly-absorption`. |

## Public API after the change

—

## Implementation steps

1. Перенести ленты и прицелы, сектор с затемнением.
2. Угрозы из `Incoming`.
3. Отрезки поглощения на ленте выбранного; сохранить выноски пыли и поглощения с размещением по занятым.
4. Тесты на `2-fire-hover-own`, `2-anomaly-absorption`, `2-no-target`, `2-evasion-plan`.

## Out of scope

Значки (TK-0002).

## Invariants

В Classic стрельба прежняя.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopFireTests`
- `MapFireTests`
- `FirePlanningTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopFireTests|FullyQualifiedName~MapFireTests|FullyQualifiedName~FirePlanningTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us6-tk1" --logger "trx;LogFileName=client.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.FireTemplates.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC1 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Геометрия и цвета — UiSpec §6.

## Исполнение

Не начато.
