using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0001-US-0015-TK-0001 — shape of the authoritative trade quote contract: request/response JSON,
/// curve and price reasons, the cancellable session request and the trailing market-revision fields.
/// </summary>
public class TradeQuoteContractTests
{
    private static readonly string[] HiddenStationFields =
        ["Credits", "StationCredits", "MarketBudgetCredits", "BudgetCredits", "MaxBudget"];

    private static TradeQuoteSnapshot FullQuote() => new(
        RequestId: "req-1",
        QuoteId: "quote-abc",
        MarketRevision: long.MaxValue - 1,
        StationObjectId: "SPC-0002",
        ObjectId: "SPC-0001",
        ModuleId: "MOD-PLAYER-CARGO-01",
        CommandType: TradeCommandTypes.Buy,
        ItemTypeId: "item.ice",
        RequestedQuantity: 12,
        ExecutableQuantity: 12,
        MaximumQuantity: 40,
        TotalCredits: 4_000_000_000_000L,
        Curve: [new TradePriceStep(5, 100), new TradePriceStep(7, 105)],
        DisabledReason: null,
        LimitReasons: [],
        PriceReasons:
        [
            new TradePriceReason("base_price", 1000),
            new TradePriceReason("station_profile", 1200, "profile.mining"),
            new TradePriceReason("event", 1500, "event.shortage-ice"),
        ]);

    [Fact]
    public void Quote_request_and_response_round_trip_all_binding_fields_and_reasons()
    {
        var request = new TradeQuoteRequest("req-1", "SPC-0001", "MOD-PLAYER-CARGO-01",
            TradeCommandTypes.Sell, "item.ice", long.MaxValue);

        var requestBack = JsonSerializer.Deserialize<TradeQuoteRequest>(JsonSerializer.Serialize(request));

        Assert.Equal(request, requestBack);

        var quote = FullQuote();
        var json = JsonSerializer.Serialize(quote);
        var back = JsonSerializer.Deserialize<TradeQuoteSnapshot>(json)!;

        Assert.Equal("req-1", back.RequestId);
        Assert.Equal("quote-abc", back.QuoteId);
        Assert.Equal(long.MaxValue - 1, back.MarketRevision);
        Assert.Equal("SPC-0002", back.StationObjectId);
        Assert.Equal("SPC-0001", back.ObjectId);
        Assert.Equal("MOD-PLAYER-CARGO-01", back.ModuleId);
        Assert.Equal(TradeCommandTypes.Buy, back.CommandType);
        Assert.Contains("\"CommandType\":\"trade.buy\"", json);
        Assert.Equal("item.ice", back.ItemTypeId);
        Assert.Equal(12, back.RequestedQuantity);
        Assert.Equal(12, back.ExecutableQuantity);
        Assert.Equal(40, back.MaximumQuantity);
        Assert.Equal(4_000_000_000_000L, back.TotalCredits);
        Assert.Null(back.DisabledReason);
        Assert.Equal(quote.Curve.ToArray(), back.Curve.ToArray());
        Assert.Equal(quote.PriceReasons.ToArray(), back.PriceReasons.ToArray());
        Assert.Null(back.PriceReasons[0].SourceId);
        Assert.Equal("event.shortage-ice", back.PriceReasons[2].SourceId);
        Assert.Equal(1500, back.PriceReasons[2].FactorPermille);
        Assert.True(back.LimitReasons.IsEmpty);
    }

    [Fact]
    public void Curve_default_or_empty_deserializes_safely()
    {
        var legacyShape = new TradeQuoteSnapshot("req-1", "", 1, "SPC-0002", "SPC-0001", "MOD-1",
            TradeCommandTypes.Buy, "item.ice", 1, 0, 0, 0, Curve: default, "invalid_quote", LimitReasons: default);

        var json = JsonSerializer.Serialize(legacyShape);
        var back = JsonSerializer.Deserialize<TradeQuoteSnapshot>(json)!;

        Assert.Contains("\"Curve\":[]", json);
        Assert.Contains("\"PriceReasons\":[]", json);
        Assert.False(back.Curve.IsDefault);
        Assert.True(back.Curve.IsEmpty);
        Assert.False(back.LimitReasons.IsDefault);
        Assert.False(back.PriceReasons.IsDefault);

        const string withoutArrays = """
            {"RequestId":"r","QuoteId":"","MarketRevision":1,"StationObjectId":"s","ObjectId":"o",
             "ModuleId":"m","CommandType":"trade.sell","ItemTypeId":"i","RequestedQuantity":1,
             "ExecutableQuantity":0,"MaximumQuantity":0,"TotalCredits":0,"DisabledReason":"stale_quote"}
            """;
        var missing = JsonSerializer.Deserialize<TradeQuoteSnapshot>(withoutArrays)!;

        Assert.True(missing.Curve.IsDefaultOrEmpty);
        Assert.True(missing.LimitReasons.IsDefaultOrEmpty);
        Assert.True(missing.PriceReasons.IsDefaultOrEmpty);
        Assert.Equal("stale_quote", missing.DisabledReason);
    }

    [Fact]
    public void Quote_contract_represents_disabled_and_partial_sell_without_budget_amount()
    {
        var disabled = new TradeQuoteSnapshot("req-2", "", 3, "SPC-0002", "SPC-0001", "MOD-1",
            TradeCommandTypes.Sell, "item.ice", 10, 0, 0, 0, [], CommandReasonCodes.StaleQuote, []);

        var disabledBack = JsonSerializer.Deserialize<TradeQuoteSnapshot>(JsonSerializer.Serialize(disabled))!;

        Assert.Equal(CommandReasonCodes.StaleQuote, disabledBack.DisabledReason);
        Assert.Equal(0, disabledBack.ExecutableQuantity);
        Assert.Equal(0, disabledBack.TotalCredits);
        Assert.True(disabledBack.Curve.IsEmpty);

        var partialSell = new TradeQuoteSnapshot("req-3", "quote-7", 4, "SPC-0002", "SPC-0001", "MOD-1",
            TradeCommandTypes.Sell, "item.ice", RequestedQuantity: 50, ExecutableQuantity: 7, MaximumQuantity: 7,
            TotalCredits: 7 * 90, [new TradePriceStep(7, 90)], DisabledReason: null,
            LimitReasons: ["station_budget_exceeded"]);

        var json = JsonSerializer.Serialize(partialSell);
        var back = JsonSerializer.Deserialize<TradeQuoteSnapshot>(json)!;

        Assert.Null(back.DisabledReason);
        Assert.Equal(50, back.RequestedQuantity);
        Assert.Equal(7, back.ExecutableQuantity);
        Assert.Equal(["station_budget_exceeded"], back.LimitReasons.ToArray());
        Assert.Equal(back.ExecutableQuantity, back.Curve.Sum(step => step.Quantity));
        Assert.Equal(back.TotalCredits, back.Curve.Sum(step => checked(step.Quantity * step.UnitPriceCredits)));
        Assert.DoesNotContain("Budget", json, StringComparison.Ordinal);
    }

    [Fact]
    public void IGameSessionConnection_declares_cancellable_trade_quote_request()
    {
        var method = typeof(IGameSessionConnection).GetMethod(nameof(IGameSessionConnection.GetTradeQuoteAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(ValueTask<TradeQuoteSnapshot>), method!.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(TradeQuoteRequest), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
        Assert.False(method.IsAbstract);
    }

    [Fact]
    public async Task IGameSessionConnection_default_trade_quote_is_not_supported()
    {
        IGameSessionConnection connection = new LegacyConnection();

        var request = new TradeQuoteRequest("req-1", "SPC-0001", "MOD-1", TradeCommandTypes.Buy, "item.ice", 1);

        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await connection.GetTradeQuoteAsync(request, CancellationToken.None));
    }

    [Fact]
    public void Quote_json_does_not_expose_station_budget_or_credits()
    {
        Type[] wireTypes =
        [
            typeof(TradeQuoteRequest), typeof(TradePriceStep), typeof(TradePriceReason),
            typeof(TradeQuoteSnapshot), typeof(StationTradeSnapshot), typeof(StationInventoryItemSnapshot),
        ];
        foreach (var type in wireTypes)
        {
            var names = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);
            Assert.Empty(names.Intersect(HiddenStationFields, StringComparer.Ordinal));
        }

        var quoteJson = JsonSerializer.Serialize(FullQuote());
        var stationJson = JsonSerializer.Serialize(new StationTradeSnapshot("SPC-0002",
            [new StationInventoryItemSnapshot("item.ice", 10, 15, 4)], MarketRevision: 9));
        foreach (var json in new[] { quoteJson, stationJson })
        {
            using var document = JsonDocument.Parse(json);
            var propertyNames = CollectPropertyNames(document.RootElement).ToList();
            Assert.Empty(propertyNames.Intersect(HiddenStationFields, StringComparer.Ordinal));
            Assert.DoesNotContain(propertyNames, name => name.Contains("Budget", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Market_revision_and_player_command_binding_round_trip_while_legacy_json_defaults_to_null()
    {
        var station = new StationTradeSnapshot("SPC-0002", [], MarketRevision: long.MaxValue);
        var stationBack = JsonSerializer.Deserialize<StationTradeSnapshot>(JsonSerializer.Serialize(station))!;
        Assert.Equal(long.MaxValue, stationBack.MarketRevision);

        var command = new PlayerCommand("cmd-1", 7, "SPC-0001", "MOD-1", TradeCommandTypes.Buy,
            ItemTypeId: "item.ice", Quantity: 3, QuoteId: "quote-abc", MarketRevision: 42);
        var commandBack = JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command))!;
        Assert.Equal(command, commandBack);
        Assert.Equal("quote-abc", commandBack.QuoteId);
        Assert.Equal(42, commandBack.MarketRevision);

        var legacyStation = JsonSerializer.Deserialize<StationTradeSnapshot>(
            """{"StationObjectId":"SPC-0002","Items":[]}""")!;
        Assert.Null(legacyStation.MarketRevision);

        var legacyCommand = JsonSerializer.Deserialize<PlayerCommand>(
            """{"CommandId":"cmd-1","ClientSequence":1,"ObjectId":"o","ModuleId":"m","CommandType":"trade.buy","ItemTypeId":"item.ice","Quantity":2}""")!;
        Assert.Null(legacyCommand.QuoteId);
        Assert.Null(legacyCommand.MarketRevision);
        Assert.Equal(2, legacyCommand.Quantity);
    }

    [Fact]
    public void Legacy_snapshot_and_command_positional_constructors_remain_source_compatible()
    {
        var items = ImmutableArray.Create(new StationInventoryItemSnapshot("item.ice", 10, 15, 4));
        var station = new StationTradeSnapshot("SPC-0002", items);
        Assert.Null(station.MarketRevision);
        Assert.Equal(items, station.Items);
        Assert.Null(new StationTradeSnapshot("SPC-0002").MarketRevision);

        var command = new PlayerCommand("cmd-1", 1, "SPC-0001", "MOD-1", TradeCommandTypes.Sell,
            null, null, null, "item.ice", 5);
        Assert.Equal(5, command.Quantity);
        Assert.Null(command.QuoteId);
        Assert.Null(command.MarketRevision);

        var quote = new TradeQuoteSnapshot("req-1", "q", 1, "SPC-0002", "SPC-0001", "MOD-1",
            TradeCommandTypes.Buy, "item.ice", 1, 1, 1, 15, [new TradePriceStep(1, 15)], null, []);
        Assert.True(quote.PriceReasons.IsDefault);
        Assert.Null(new TradePriceReason("base_price", 1000).SourceId);
    }

    private static IEnumerable<string> CollectPropertyNames(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Name;
                    foreach (var nested in CollectPropertyNames(property.Value))
                        yield return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nested in CollectPropertyNames(item))
                        yield return nested;
                }
                break;
        }
    }

    /// <summary>Connection written before quotes existed: it relies on the interface default.</summary>
    private sealed class LegacyConnection : IGameSessionConnection
    {
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
