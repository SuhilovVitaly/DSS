---
epic: EP-0001-trading-system
story: EP-0001-US-0013-economy-balance-evidence
ticket: EP-0001-US-0013-TK-0002-balance-run-matrix
title: Headless runner фиксированной balance-матрицы
stage: approved
layer: tooling
depends_on: [EP-0001-US-0013-TK-0001-balance-diagnostic-seam]
files_touched: 4
serves: [AC-01, AC-05]
created: 2026-09-21T14:55:45Z
revision: 1
---

# Headless runner фиксированной balance-матрицы

## Why

Создать reusable tooling core, который из реального Settings/scenario строит 24 независимых deterministic cases, снимает почасовые market/map/event данные, выполняет реальные торговые стратегии и формирует типизированное evidence. End state: tests могут прогонять сокращённую matrix, а следующий CLI использует тот же runner без собственной оркестрации.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj`.

## Decisions

Единственное сообщение пользователя приведено в story и TK-0001. Новых решений пользователя нет.

## Assumptions

- `starter` использует фактические snapshot capacity/efficiency; `cargo-upgrade` меняет только tool-side maximum carried quantity множителем 2000 permille. Engine scenario/content не мутируются.
- Passive sampling и каждая strategy run используют отдельный Engine clone одного seed/checkpoint.
- Full matrix и thresholds принадлежат TK-0005; этот merge unit собирает evidence и проверяет determinism, но не объявляет баланс passed.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.EconomyBalance/DeepSpaceSaga.EconomyBalance.csproj` | Новый project; существующий tooling-project reference pattern — `tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj:1–7` | `net8.0` library, references Contracts/Engine, `InternalsVisibleTo` только matching tests |
| `tools/DeepSpaceSaga.EconomyBalance/BalanceRun.cs` | Новый файл; Engine bootstrap/capture seams — `SimulationEngine.cs:98–123,720–734,1676–1682`, scenario load/serialize — `ScenarioLoader.cs:42–60,106–109` | Request/evidence records, canonical ordering, Engine adapter, passive/strategy/save-load runners |
| `tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj` | Новый matching test project | xUnit/test SDK references и ProjectReference только на balance tool |
| `tests/DeepSpaceSaga.EconomyBalance.Tests/BalanceRunTests.cs` | Новый файл | Input validation, explicit-time samples, seed/config isolation, command-driven strategy and Save/Load equivalence tests |

## Public API after the change

Tool assembly internal API (visible to its tests):

```csharp
internal sealed record BalanceShipConfiguration(
    string Id,
    int CargoCapacityMultiplierPermille,
    int FuelEfficiencyMultiplierPermille);

internal sealed record BalanceMatrix(
    ImmutableArray<ulong> Seeds,
    ImmutableArray<BalanceShipConfiguration> ShipConfigurations,
    long HorizonGameTimeMs,
    long SampleIntervalGameTimeMs,
    long SaveLoadCheckpointGameTimeMs);

internal sealed record BalanceLedgerEvidence(
    string VoyageId, string RouteId, string ItemTypeId,
    long GrossSalesCredits, long? CostOfGoodsSoldCredits,
    long RouteFuelCostCredits, long PortFeesAssessedCredits,
    long EventCostsCredits, long PassengerPayoutCredits,
    long PassengerPenaltyCredits, long? NetProfitCredits);

internal sealed record BalanceCaseEvidence(
    ulong Seed, string ShipConfigurationId,
    ImmutableArray<BalanceHourlySample> HourlySamples,
    ImmutableArray<BalanceStrategyEvidence> Strategies,
    string ContinuousStateHash, string SaveLoadStateHash);

internal sealed class EconomyBalanceRunner
{
    internal ImmutableArray<BalanceCaseEvidence> Run(
        string settingsPath, string scenarioPath, BalanceMatrix matrix);
}
```

`BalanceHourlySample` содержит `GameTimeMs`, canonical station stock/target/max/budget, active influencing events, effective routes/cargo flows и market revisions. `BalanceStrategyEvidence` содержит origin/destination/distance class/risk/item/config, requested/executed quantities, quote/result revisions, authoritative receipts и `BalanceLedgerEvidence`; exact DTO definitions находятся в `BalanceRun.cs` и не используются production runtime.

Dependency command path фиксирован `EP-0001-US-0006-repeatable-trading-voyage.md:50–56`: `GetTradeQuote(TradeQuoteRequest)`, quote-bound `trade.buy`/`trade.sell`, `PlayerCommand(...,"navigation.undock",TargetObjectId:destination)`, explicit-time snapshots до Docked, затем latest matching `VoyageFinanceSnapshot`. Tool не вызывает Client.

## Implementation steps

1. Создать library/test projects с nullable+implicit usings и теми же package versions, что existing test projects. Не добавлять проекты в solution до TK-0005.
2. Валидировать matrix до Engine bootstrap: nonempty unique IDs/seeds, positive permille, positive divisible horizon/sample, checkpoint строго внутри horizon и кратен interval. Вернуть contextual `BalanceConfigurationException`.
3. Загрузить registry/default trading scenario через existing Engine content loader, заменить только `GameState.MasterSeed`, создать новый Engine на каждый case/branch. Input arrays canonicalize ordinal/numeric; caller order не влияет на output.
4. Passive branch снимает `t=0` и 240 post-boundary hourly states через explicit-time seam. Из save/snapshot копировать только primitive immutable evidence; не хранить live Engine/Scenario references.
5. Save/Load branch идёт до 120 часов, capture → `ScenarioLoader.Serialize/LoadFromJson(..., true)` → новый Engine `LoadScenario(..., isSave:true)` → 240 часов. Хеш строить из canonical economy/map/event/voyage/ledger payload, не из raw JSON property order.
6. Strategy driver для каждого выбранного route/item создаёт fresh case, запрашивает authoritative quote, отправляет buy, undock, продвигает explicit time до terminal Docked, запрашивает destination sell и читает matching `VoyageFinanceSnapshot`. Rejection/partial/unknown COGS сохранять как evidence, не превращать в exception.
7. Batch ceiling = min(authoritative quote maximum, floor(actual usable cargo capacity × config multiplier / 1000)); fuel multiplier остаётся 1000 для обеих текущих configs. Не пересчитывать цены, fuel или ledger.
8. Повторный `Run` на тех же inputs и Run с reversed seed/config input должны давать `SequenceEqual` evidence после canonical ordering.

## Out of scope

Threshold verdicts, CLI/JSON, solution/CI wiring, content tuning, UI, новый Engine API, автоматическая установка module upgrade и собственная price/fuel/net formula.

## Invariants

- Tool управляет authoritative Engine, а не параллельной экономикой: `Documentation/00-Process/CLAUDE.md:88–112`.
- Explicit-time update остаётся единым Engine path: `SimulationEngine.cs:393–421,1676–1682`.
- Trade/voyage path и finance DTO принадлежат upstream stories; mismatch возвращает ticket в review: story Dependencies.
- Четыре files, только tooling layer и dedicated matching test project.

## Tests

`BalanceRunTests`:

- `Matrix_rejects_duplicate_seed_id_nonpositive_multiplier_or_misaligned_time` (AC-01/05).
- `One_seed_two_configs_collect_0_through_240_hour_samples_in_canonical_order` (AC-01).
- `Passive_and_strategy_branches_are_isolated_clones` (AC-01).
- `Strategy_evidence_uses_quote_receipt_route_and_matching_authoritative_ledger` (AC-05).
- `Continuous_and_midpoint_save_load_hashes_match` (AC-01/05).
- `Repeated_and_reordered_runs_are_sequence_equal` (AC-01).
- `Rejected_partial_and_unknown_cogs_results_remain_contextual_evidence` (AC-05).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.EconomyBalance.Tests\DeepSpaceSaga.EconomyBalance.Tests.csproj --no-restore --filter FullyQualifiedName~BalanceRunTests
dotnet build D:\DeepSpaceSaga\DSS\tools\DeepSpaceSaga.EconomyBalance\DeepSpaceSaga.EconomyBalance.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены в четырёх allowed files; проекты ещё не добавлены в solution.
- AC-01/05 покрыты 241 samples, two configs, canonical ordering, isolation и midpoint Save/Load tests.
- Tool/test build и named tests проходят; solution format проходит либо baseline failure записано отдельно.
- Evidence использует actual Engine quotes/commands/ledger, не tool arithmetic.
- Нет hidden temp data, wall-clock fields, unrecorded mutation или файлов вне `Code context`.
- Result воспроизводится прямым test/build commands.

## Self-containment check

Project structure, records, matrix validation, sampling times, clone strategy, command sequence, capacity assumption и hash domains заданы. Следующие evaluators получают один стабильный `BalanceCaseEvidence` и не ищут Engine internals.
