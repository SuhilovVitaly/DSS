---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0006-decoy-resolution
ticket: EP-0020-US-0006-TK-0002-decoy-hit-transform
title: Преобразование шанса ловушкой
stage: draft
layer: motion
test_project: tests/DeepSpaceSaga.Motion.Tests
depends_on: [EP-0020-US-0006-TK-0001-decoy-contracts]
files_touched: 2
serves: [US6-AC2]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Преобразование шанса ловушкой

## Why

Эффект ловушки — множитель шанса промаха, а не слагаемое; он применяется к итоговому шансу одной функцией для движка и прогноза.

## Decisions

Применяются [D-15–D-23](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-16: преобразование применяется после ограничения 5–95 % и не опускает шанс ниже `MinChance`; пример: 47 % → 20,5 %, 56 % → 34 %, 30 % → 5 %.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Motion/TacticalBattle/TacticalHitChance.cs` | :17-39 `Terms`/`Chance` | Новый `public static double WithDecoy(double chance, double missFactor, double minChance)`. |
| `tests/DeepSpaceSaga.Motion.Tests/TacticalBattle/LimitsAndChanceTests.cs` | :51-80, :105-133 V4 trace | Новые проверки; trace без изменений. |

## Public API after the change

`TacticalHitChance.WithDecoy(double, double, double)`.

## Implementation steps

1. Реализовать функцию.
2. Тесты: значения A-16, `missFactor = 1` не меняет шанс, нижняя граница `MinChance`, 95 % → 92,5 %.

## Out of scope

Где применяется (TK-0003).

## Invariants

`AllPublishedSeedOneShotChancesMatchAtOriginalSixDecimalPrecision` проходит без изменения допуска.

## Tests

Проект `tests/DeepSpaceSaga.Motion.Tests`. Именованные проверки:

- `LimitsAndChanceTests.Decoy_multiplies_miss_chance_after_clamp`
- `LimitsAndChanceTests.Decoy_never_goes_below_min_chance`
- `LimitsAndChanceTests.AllPublishedSeedOneShotChancesMatchAtOriginalSixDecimalPrecision`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~LimitsAndChanceTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us6-tk2" --logger "trx;LogFileName=motion.trx"
dotnet format whitespace src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj --no-restore --include src/DeepSpaceSaga.Motion/TacticalBattle/TacticalHitChance.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US6-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Формула задана D-17.

## Исполнение

Не начато.
