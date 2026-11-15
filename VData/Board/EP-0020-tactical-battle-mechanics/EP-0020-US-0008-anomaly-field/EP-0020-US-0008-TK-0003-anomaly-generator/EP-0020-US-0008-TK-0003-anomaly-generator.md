---
epic: EP-0020-tactical-battle-mechanics
story: EP-0020-US-0008-anomaly-field
ticket: EP-0020-US-0008-TK-0003-anomaly-generator
title: Генерация аномалий из seed боя
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0020-US-0008-TK-0002-anomaly-math, EP-0020-US-0006-TK-0005-decoy-regression]
files_touched: 7
serves: [US8-AC3, US8-AC4]
created: 2026-11-08T11:46:28Z
revision: 1
---

# Генерация аномалий из seed боя

## Why

Аномалии каждого боя создаются из его seed (D-31) и не должны сдвигать основной поток RNG, иначе изменятся все прежние числа.

## Decisions

Применяются [D-24–D-33](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md) и DesignSpec EP-0019. Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-25: средняя полоса — прямоугольник по x от `xA + (1 − bandFraction)/2 · (xB − xA)` до `xB − (1 − bandFraction)/2 · (xB − xA)`, где `xA`, `xB` — средние x стартов дронов сторон после разброса; по y — от min до max y всех стартов дронов. Порядок: сначала все температурные, затем пылевые; для каждой: число попыток до `maxAttempts`, центр равномерно в полосе, отказ при расстоянии < `clearance` до носителя или старта дрона; пятна: центр `N(центр, spread·centerSpread)`, амплитуда, σ и поворот `U[0, π)`. Нормальное распределение — Box–Muller на двух `Random()` потока аномалий.

## Code context

Строки — `main` @ `8c6f4e2a`, файлы EP-0019 — по его карточкам; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalAnomalyGenerator.cs` | нет | Генератор по A-25. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs` | :61-69 после разброса | Вызвать генератор; передать аномалии в `TacticalBattle`. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs` | :68-70 `TacticalBattle` | `ImmutableArray<TacticalAnomaly> Anomalies`. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | :54-56 правила, :79-85 снимок | `TacticalAnomalySnapshot`, `TacticalClumpSnapshot`, `TacticalAnomalyRulesSnapshot`; поля US8-AC4. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | :64-66 `Rules` | Аномалии и правила в снимок. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AnomalyGeneratorTests.cs` | нет | Тесты. |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | :46-92 | Сериализация и чтение старых нагрузок. |

## Public API after the change

`TacticalAnomalyGenerator.Generate(TacticalBattleCatalog, long seed, IReadOnlyList<TacticalDrone>, IReadOnlyList<TacticalCarrier>)`; `TacticalBattle.Anomalies`; поля снимка US8-AC4.

## Implementation steps

1. Генератор по A-25.
2. Вызов после разброса; основной поток не трогать.
3. Снимок и контракты.
4. Тесты: одинаковый seed → одинаковые аномалии; число в диапазонах; все центры в полосе и с зазором; `RandomCalls` основного потока после сборки прежние; на 1000 сидов генерация не падает.

## Out of scope

Эффекты на стрельбу.

## Invariants

Основной поток RNG и исходы боёв до US-0009 не меняются (аномалии пока без эффекта).

## Tests

Проект `tests/DeepSpaceSaga.Engine.Tests`. Именованные проверки:

- `AnomalyGeneratorTests.Same_seed_same_anomalies`
- `AnomalyGeneratorTests.Counts_and_placement_follow_rules`
- `AnomalyGeneratorTests.Main_random_stream_is_untouched`
- `AnomalyGeneratorTests.Thousand_seeds_generate`
- `TacticalBattleContractTests.OldPayloadsWithoutAnomaliesRemainReadable`
- `SeriesParityTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~AnomalyGeneratorTests|FullyQualifiedName~SeriesParityTests|FullyQualifiedName~GoldenBattleTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us8-tk3" --logger "trx;LogFileName=engine.trx"
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TacticalBattleContractTests" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0020/<run>/us8-tk3" --logger "trx;LogFileName=contracts.trx"
dotnet format whitespace src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore --include src/DeepSpaceSaga.Engine/TacticalBattle/TacticalAnomalyGenerator.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs --verify-no-changes
dotnet format whitespace src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --no-restore --include src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs --verify-no-changes
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US8-AC3, US8-AC4 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Алгоритм и места вставки заданы.

## Исполнение

В процессе. Scope: Motion TacticalAnomaly получает диагностические init CenterX/CenterY для проверки anchor placement (не влияют на поле и не входят в обязательный DTO); Session.Capture передаёт материализованный список. Техническая фиксация порядка нового RNG: lower32 DeriveStreamSeed(unchecked ulong seed,"tactical-anomalies"); сначала оба count(drawHeat,drawDust), затем все Heat и Dust; после принятого центра spread,clumpCount; одна Box-Muller пара на X/Y (ровно2Random, radialUniform=1−Random исключает log0), затем amplitude/sigmaX/sigmaY/rotation. Это уточнение воспроизводимости, не новое пользовательское решение.

Завершено. Engine103/103PASS (генерация1000сидов, независимый FNV/Mulberry anchor, clearance/полоса/пропуски, основной42draws и initiative state неизменны); Contracts28/28PASS. Series5/5PASS:1000×5 direct/explicit и побайтовое before/after совпадение всех5000 итогов, статистики, состояний и RNG. Scopedformat5проектов и diffcheckPASS. Evidence `D:/DeepSpaceSaga/artifacts/validation/ep-0020/20261108-execution/us8-tk3/` (focus/tests.trx,contracts/tests.trx,before/after/tests.trx,series-comparison.json,focus/anomaly-generation.csv,focus/anomalies-seed1.json). Самопроверка: immutable field materialization; Session.Capture без draw; эффекты не подключены. US8-AC3/4 выполнены; тактический бой не сохраняется, Save20 не меняется.

Финальная сверка US13 исправила пропуск US8-AC4 в serves/Ticket map: контракт уже реализован этим тикетом и подтверждён Contracts28/Engine103 в исполнении выше, runtime не менялся.
