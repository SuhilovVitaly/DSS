---
epic: EP-0001-trading-system
story: EP-0001-US-0015-authoritative-market-quotes
ticket: EP-0001-US-0015-TK-0001-trade-quote-contract
title: Контракт authoritative-котировки
stage: approved
layer: contracts
depends_on: [EP-0001-US-0001-TK-0005-profile-trade-presentation, EP-0001-US-0002-TK-0001-market-stock-snapshot]
files_touched: 5
serves: [AC-03, AC-05, AC-06, AC-07]
created: 2026-09-21T15:10:56Z
revision: 1
---

# Контракт authoritative-котировки

## Why

Engine, LocalClient и будущий network adapter нуждаются в одном serializable request/response contract. Он связывает точную сумму с рынком и revision, переносит curve и причины, но не раскрывает скрытый station budget. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`.

## Decisions

Пользовательских технических решений сверх исходного запроса не было. D-01, 2026-09-21T15:10:56Z: «создай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0015-authoritative-market-quotes\EP-0001-US-0015-authoritative-market-quotes.md».

## Assumptions

- DTO следует consumer seam US-0003/A-01. `PriceReasons` — trailing optional addition.
- `MarketRevision` в docked snapshot trailing nullable, чтобы старые constructors/JSON продолжали работать.
- Default interface implementation возвращает `NotSupportedException`; это сохраняет существующие test doubles до их миграции и не создаёт fake quote.
- Immutable arrays используют существующий `ImmutableArrayDefaultJsonConverter<T>` и после deserialization нормализуются как empty/default согласно текущему Contracts convention.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/TradeQuote.cs | отсутствует; поиск `TradeQuoteRequest|TradePriceStep` в Contracts не дал production API | Добавить request, curve segment, price reason и quote snapshot records |
| src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs | :10–13 содержит только StationObjectId/Items; :15–40 не раскрывает Credits | Добавить trailing optional MarketRevision и XML semantics |
| src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs | :10–55 содержит command/snapshot/save boundary без quote request-response | Добавить cancellable GetTradeQuoteAsync с compatibility default |
| src/DeepSpaceSaga.Contracts/PlayerCommand.cs | :6–50 record заканчивается Quantity, quote binding отсутствует | Добавить trailing optional QuoteId/MarketRevision для exact validator seam |
| tests/DeepSpaceSaga.Contracts.Tests/TradeQuoteContractTests.cs | отсутствует | JSON/API/shape/invariant tests нового DTO |

## Public API after the change

```csharp
public sealed record TradeQuoteRequest(
    string RequestId,
    string ObjectId,
    string ModuleId,
    string CommandType,
    string ItemTypeId,
    long Quantity);

public sealed record TradePriceStep(long Quantity, long UnitPriceCredits);

public sealed record TradePriceReason(
    string Code,
    int FactorPermille,
    string? SourceId = null);

public sealed record TradeQuoteSnapshot(
    string RequestId,
    string QuoteId,
    long MarketRevision,
    string StationObjectId,
    string ObjectId,
    string ModuleId,
    string CommandType,
    string ItemTypeId,
    long RequestedQuantity,
    long ExecutableQuantity,
    long MaximumQuantity,
    long TotalCredits,
    ImmutableArray<TradePriceStep> Curve,
    string? DisabledReason,
    ImmutableArray<string> LimitReasons,
    ImmutableArray<TradePriceReason> PriceReasons = default);

public interface IGameSessionConnection
{
    ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(
        TradeQuoteRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromException<TradeQuoteSnapshot>(
            new NotSupportedException("Trade quotes are unavailable."));
}
```

В конец `StationTradeSnapshot` после Items:

```csharp
long? MarketRevision = null
```

В конец `PlayerCommand` после Quantity:

```csharp
string? QuoteId = null,
long? MarketRevision = null
```

Оба поля либо заданы вместе для quoted trade, либо оба null для legacy/non-trade. `MarketRevision` — revision, по которой была выдана quote; Engine не доверяет полям без lookup QuoteId. Это переносит только binding prerequisite из US-0003/TK-0001; `TradeExecutionReceipt` и reason codes остаются там.

Contract invariants, документируемые, но не вычисляемые DTO: non-disabled quote имеет non-empty QuoteId, revision≥1, 0≤Executable≤Requested, Maximum≥Executable, nonnegative total; каждый step имеет Quantity>0/UnitPrice≥1, sum quantities=Executable и checked weighted sum=Total. Disabled quote имеет Executable=0, Total=0, empty Curve и непустой reason. DTO не содержит station Credits/MarketBudgetCredits.

## Implementation steps

1. Создать `TradeQuote.cs` с XML documentation для binding, curve и stable reason codes; применить converters к immutable arrays по существующему Contracts pattern.
2. Добавить `MarketRevision` trailing nullable в `StationTradeSnapshot`; null означает legacy/no revision, не revision zero.
3. Добавить trailing quote binding в `PlayerCommand`, сохранив старые positional constructors/JSON; partially specified binding остаётся представимым, чтобы Engine вернул `invalid_quote`, а не serializer error.
4. Добавить default `GetTradeQuoteAsync` в interface. Валидировать cancellation/arguments будет production adapter/Engine, не DTO constructor.
5. Проверить JSON roundtrip всех полей, default arrays, nullable source/revision, legacy command/snapshot, большие long и string enum-like command values; DTO должен оставаться graphics/Engine independent.
6. Проверить reflection-signature interface и что сериализованный snapshot/quote не содержит `Credits`, `MarketBudgetCredits` или `MaxBudget`.

## Out of scope

Формула, Engine cache/validation, LocalClient implementation, CommandResult/receipt/reason codes, trade execution, UI/localization, network adapter и persistence quote tokens.

## Invariants

- Contracts не зависит от Engine/Client: `Documentation/00-Process/CLAUDE.md:44–56`.
- Client действует только через публичный session contract: `Documentation/01-Requirements/EngineRequirements.md:379–419`.
- Budget остаётся скрыт и влияет только через derived limits: `src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:15–23`.
- Количество — целые trade units: `EngineRequirements.md:5128–5160`.

## Tests

`TradeQuoteContractTests`:

- `Quote_request_and_response_round_trip_all_binding_fields_and_reasons` — AC-03/06.
- `Curve_default_or_empty_deserializes_safely` — compatibility.
- `Quote_contract_represents_disabled_and_partial_sell_without_budget_amount` — AC-03.
- `IGameSessionConnection_declares_cancellable_trade_quote_request` — AC-06/07.
- `Quote_json_does_not_expose_station_budget_or_credits` — AC-03.

- `Market_revision_and_player_command_binding_round_trip_while_legacy_json_defaults_to_null` — AC-05/06/07.
- `Legacy_snapshot_and_command_positional_constructors_remain_source_compatible` — regression.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Contracts.Tests\DeepSpaceSaga.Contracts.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeQuoteContractTests|FullyQualifiedName~StationTradeSnapshotTests|FullyQualifiedName~CommandResultTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Contracts\DeepSpaceSaga.Contracts.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в пяти разрешённых файлах.
- AC-03/05/06/07 покрыты named tests в пределах contract layer.
- Named tests/build/format проходят либо исходное baseline failure записано отдельно.
- API/invariants/out-of-scope соблюдены; hidden budget не появился ни в одном wire DTO.
- Нет незаписанных assumptions, вопросов или изменений вне Code context.
- End state проверяем JSON roundtrip и reflection без запуска Engine.

## Self-containment check

Даны точные signatures, compatibility defaults, JSON/invariant semantics и тесты. Implementer не должен искать naming или consumer shape в US-0003; обязательный seam включён здесь полностью.
