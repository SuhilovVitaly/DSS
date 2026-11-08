---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0009-anomaly-resolution
ticket: EP-0020-US-0009-TK-0005-anomaly-regression
title: Регрессия боя с аномалиями и отчёт
stage: draft
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0009-TK-0004-session-anomalies]
files_touched: 3
serves: [US9-AC6]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Регрессия боя с аномалиями и отчёт

## Why

Аномалии меняют все бои. Нужно доказать совпадение путей, осознанно обновить ожидания и показать влияние аномалий.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

Числа исторических боёв не являются оракулом новой механики. Отчёт не меняет данные.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs` | :23-88 | Обновить ожидаемые числа с записью причины. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs` | :13-53 | Подписи серий; прямой = явный. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AnomalyReportTests.cs` | нет | `Trait("Category","Balance")`: отчёт US9-AC6 в `DSS_VALIDATION_ROOT/ep0020-anomalies/`. |

## Public API after the change

No API change.

## Implementation steps

1. Прогнать `DeepSpaceSaga.Engine.Tests`.
2. Обновить ожидания только по причинам «аномалии»; список записать.
3. Подписи серий до и после в artifacts.
4. Отчёт; таблицу — в «Исполнение» и review log эпика.

## Out of scope

Изменение чисел.

## Invariants

Прямой и явный пути совпадают.

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `GoldenBattleTests.NewRuleBattlesRepeatAcrossDirectAndExplicitPaths`
- `SeriesParityTests`
- `AnomalyReportTests.Report_anomaly_effect_over_series`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle|FullyQualifiedName~AnomalyReportTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us9-tk5" --logger "trx;LogFileName=engine.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC6 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Содержание отчёта задано.

## Исполнение

Не начато.
