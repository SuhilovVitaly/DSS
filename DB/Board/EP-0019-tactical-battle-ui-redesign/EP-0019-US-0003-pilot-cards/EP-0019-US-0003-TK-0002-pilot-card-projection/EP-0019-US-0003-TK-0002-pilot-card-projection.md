---
epic: EP-0019-tactical-battle-ui-redesign
story: EP-0019-US-0003-pilot-cards
ticket: EP-0019-US-0003-TK-0002-pilot-card-projection
title: Данные карточки по фазам
stage: draft
layer: client
test_project: DeepSpaceSaga.Client.Tests
depends_on: [EP-0019-US-0003-TK-0001-pilot-portrait-data]
files_touched: 3
serves: [US3-AC2, US3-AC3]
created: 2026-11-08T09:31:05Z
revision: 1
---

# Данные карточки по фазам

## Why

Отрисовка не должна вычислять игровые значения. Нужна чистая проекция «снимок + приказы + презентация → данные карточек».

## Decisions

Применяются [D-01–D-09](../../Documentation.md#решения-пользователя). Нормативный вид — [DesignSpec](../../DesignSpec.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Урон «за залп» — из `BeforeSalvoUnits` и `Shots`; «за бой» — из `Log` (`ShotHit` + следующий `DamageApplied` того же хода), DesignSpec §5.

## Code context

Строки — `main` @ `8c6f4e2a`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs` | нет | Records `PilotStat(Kind, Label, Value, Tone, Fill?)`, `PilotCard(UnitId, Name, Weapon, PilotId, Selected, Stats, Chip, ChipAccent)`; `PilotCardModel.Project(TacticalBattleFrame, presentation)`. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleOrders.cs, TacticalBattleFireOrders.cs` | Capture() :145 / :80 | Источник текущих приказов и целей (вместо предложений ИИ — фактический приказ игрока). |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardModelTests.cs` | нет | Тесты на seed 1. |

## Public API after the change

`internal static class PilotCardModel { IReadOnlyList<PilotCard> Project(...); }`.

## Implementation steps

1. Реализовать правила таблицы DesignSpec §5 с текущими приказами вместо предложений (чип `ИИ`, пока приказ не изменён).
2. Тоны значений: Yellow/Orange/Red/Text/Muted по DesignSpec §5.
3. Тесты: значения и чипы для пяти состояний seed 1 совпадают с DesignSpec §12.3.

## Out of scope

Рисование.

## Invariants

Проекция без побочных эффектов и без вызовов сессии.

## Tests

Проект `DeepSpaceSaga.Client.Tests`. Именованные проверки:

- `PilotCardModelTests.Move_cards_seed1`
- `PilotCardModelTests.Fire_cards_seed1_include_initiative`
- `PilotCardModelTests.Salvo_and_results_cards_seed1`
- `PilotCardModelTests.Outcome_totals_from_log`
- `PilotCardModelTests.Mind_is_placeholder_100`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PilotCardModelTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0019/<run>/us3-tk2" --logger "trx;LogFileName=focused.trx"
dotnet format whitespace src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardModel.cs --verify-no-changes
dotnet format whitespace tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --include tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PilotCardModelTests.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US3-AC2, US3-AC3 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты; ожидания DesignSpec не изменены.

## Self-containment check

Правила целиком в DesignSpec §5.

## Исполнение

Не начато.
