using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

internal sealed class ClusterVoyageFixture : IDisposable
{
    internal const string Ship = "SPC-0001", Bridge = "MOD-PLAYER-BRIDGE-01", EngineModule = "MOD-PLAYER-ENGINE-01", Cargo = "MOD-PLAYER-CARGO-01";
    internal SimulationEngine Engine { get; }
    internal AuthoritativeSnapshot Snapshot { get; private set; } = null!;
    internal long Time { get; private set; }
    private long _id;
    private long _flightStartTime;
    private double _straightDays;
    internal string Origin { get; }
    internal string Destination { get; }
    internal string OutboundItem => "item.iron-ore";
    internal string ReturnItem => "item.energy-cells";
    internal List<(double StraightDays, double ActualDays)> Flights { get; } = [];
    internal List<TradeExecutionReceipt> Receipts { get; } = [];

    internal ClusterVoyageFixture(ulong seed, bool intercluster, long? initialCredits = null)
    {
        Engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"))!;
        var source = SeededWorldBootstrapTests.Scenario("Docked");
        Engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0", PlayerTokens = initialCredits ?? source.GameState.PlayerTokens } }, generation: config);
        Capture();
        Origin = Player.DockedStationObjectId!;
        var map = Snapshot.ClusterMap!;
        string home = map.Stations.Single(s => s.ObjectId == Origin).ClusterId;
        Destination = map.Stations.Where(s => s.MarketProfileId == "market.industrial" && (s.ClusterId != home) == intercluster &&
            Snapshot.Voyage!.RouteOptions.Any(r => r.DestinationStationObjectId == s.ObjectId))
            .OrderBy(s => s.ObjectId, StringComparer.Ordinal).First().ObjectId;
        Assert.Equal(initialCredits ?? source.GameState.PlayerTokens, Snapshot.PlayerCredits);
    }

    private ClusterVoyageFixture(ScenarioFile saved, ClusterVoyageFixture source, SimulationEngine? restoredEngine)
    {
        Engine = restoredEngine ?? new SimulationEngine(SeededWorldBootstrapTests.Registry());
        if (restoredEngine is null) Engine.LoadScenario(saved, true);
        Time = saved.GameState.MotionTimeMs; _id = source._id;
        Origin = source.Origin; Destination = source.Destination;
        _flightStartTime = source._flightStartTime; _straightDays = source._straightDays;
        Receipts.AddRange(source.Receipts); Capture();
    }
    internal ClusterVoyageFixture Reload(ScenarioFile? saved = null, SimulationEngine? restoredEngine = null) =>
        new(saved ?? ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Save()), true), this, restoredEngine);

    internal ObjectMotionSnapshot Player => Snapshot.Objects.Single(o => o.ObjectId == Ship);
    internal AuthoritativeSnapshot Capture() => Snapshot = Engine.CaptureSnapshotForTests(Time * 300, SimulationSpeed.Speed0, Time);
    internal AuthoritativeSnapshot Advance(long delta) { Assert.True(delta >= 0); Time += delta; return Capture(); }
    internal ScenarioFile Save() => Engine.CaptureSaveStateForTests(Time * 300, SimulationSpeed.Speed0, Time);

    internal CommandResult? Send(string module, string type, string? target = null, string? item = null, TradeQuoteSnapshot? quote = null)
    {
        string id = $"cluster-voyage-{++_id}";
        Engine.ReceiveCommand(new(id, (ulong)_id, Ship, module, type, TargetObjectId: target, ItemTypeId: item,
            Quantity: quote?.RequestedQuantity, QuoteId: quote?.QuoteId, MarketRevision: quote?.MarketRevision));
        Capture();
        var result = Snapshot.CommandResults.SingleOrDefault(r => r.CommandId == id);
        Assert.True(result?.Status != CommandResultStatus.Rejected, $"{type}: {result?.ReasonCode}; time={Time}, credits={Snapshot.PlayerCredits}");
        return result;
    }

    internal TradeExecutionReceipt Trade(string type, string item)
    {
        var quote = Engine.GetTradeQuote(new($"cluster-quote-{++_id}", Ship, Cargo, type, item, 1));
        Assert.Null(quote.DisabledReason); Assert.Equal(1, quote.ExecutableQuantity);
        long before = Snapshot.PlayerCredits;
        var result = Send(Cargo, type, item: item, quote: quote);
        var receipt = Assert.IsType<TradeExecutionReceipt>(result?.TradeReceipt);
        Assert.Equal(Player.DockedStationObjectId, receipt.StationObjectId);
        Assert.Equal(type == TradeCommandTypes.Buy ? before - receipt.TotalCredits : before + receipt.TotalCredits, Snapshot.PlayerCredits);
        Receipts.Add(receipt); return receipt;
    }

    internal void FlyTo(string destination, Action<ClusterVoyageFixture>? midpoint = null)
    {
        for (int hour = 0; !Snapshot.Voyage!.RouteOptions.Single(r => r.DestinationStationObjectId == destination).IsAvailable && hour < 168; hour++)
            Advance(12000);
        _flightStartTime = Time;
        var target = Snapshot.Objects.Single(o => o.ObjectId == destination);
        _straightDays = double.Hypot(target.X - Player.X, target.Y - Player.Y) / 10 / Player.MaxSpeedKmS!.Value * 300 / 86400;
        var position = Player;
        Send(Bridge, NavigationComputerCommandTypes.Undock, destination);
        Assert.False(Player.IsDocked); Assert.Null(Snapshot.DockedStationTrade);
        Assert.Equal(position.X, Player.X); Assert.Equal(position.Y, Player.Y);
        Send(EngineModule, ShipEngineCommandTypes.Accelerate);
        for (int step = 0; Player.SpeedKmS < Player.MaxSpeedKmS && step < 5000; step++) Advance(1000);
        Assert.Equal(Player.MaxSpeedKmS, Player.SpeedKmS);
        Send(EngineModule, NavigationComputerCommandTypes.Approach, destination);
        for (int step = 0; Player.ApproachRoute is null && step < 5000; step++) Advance(1000);
        var route = Assert.IsType<ApproachRoute>(Player.ApproachRoute);
        double speed = Player.SpeedKmS;
        long remaining = (long)Math.Ceiling(route.DurationMs - route.ElapsedMs);
        Assert.InRange(remaining, 1, 1000 * 288000L);
        Advance(remaining / 2); midpoint?.Invoke(this); FinishFlightTo(destination);
    }

    internal void FinishFlightTo(string destination)
    {
        var route = Assert.IsType<ApproachRoute>(Player.ApproachRoute);
        double speed = Player.SpeedKmS;
        Advance((long)Math.Ceiling(route.DurationMs - route.ElapsedMs));
        Assert.Equal(speed, Player.SpeedKmS);
        foreach (string command in new[] { ShipEngineCommandTypes.SpeedSynchronization, ShipEngineCommandTypes.DirectionSynchronization })
        {
            Send(EngineModule, command, destination);
            var cycle = Engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Ship).Modules.Single(m => m.ModuleId == EngineModule).ActiveCycle;
            if (cycle is not null) Advance(Math.Max(1, cycle.DurationMs));
        }
        Send(Bridge, NavigationComputerCommandTypes.Dock, destination);
        foreach (string choice in new[] { "truthful_id", "accept_fee", "continue" })
        {
            var dialogue = Assert.IsType<DialogueState>(Snapshot.ActiveDialogue);
            var selected = dialogue.Choices.Single(c => c.ChoiceId == choice);
            Assert.True(selected.Enabled, $"{choice}: {selected.DisabledReasonKey}; time={Time}, credits={Snapshot.PlayerCredits}");
            string id = $"cluster-dialogue-{++_id}";
            var command = new DialogueCommand(id, DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice);
            Engine.ReceiveDialogueCommand(command); Capture();
            long credits = Snapshot.PlayerCredits;
            Engine.ReceiveDialogueCommand(command); Capture(); Assert.Equal(credits, Snapshot.PlayerCredits);
        }
        Assert.True(Player.IsDocked); Assert.Equal(destination, Player.DockedStationObjectId);
        Assert.Null(Snapshot.ActiveVoyage);
        Flights.Add((_straightDays, (Time - _flightStartTime) / 288000d));
    }
    public void Dispose() => Engine.Dispose();
}

public sealed class ClusterVoyageIntegrationTests
{
    private static void RoundTrip(ulong seed, bool intercluster)
    {
        using var voyage = new ClusterVoyageFixture(seed, intercluster);
        voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem);
        voyage.FlyTo(voyage.Destination);
        voyage.Trade(TradeCommandTypes.Sell, voyage.OutboundItem);
        voyage.Trade(TradeCommandTypes.Buy, voyage.ReturnItem);
        voyage.FlyTo(voyage.Origin);
        voyage.Trade(TradeCommandTypes.Sell, voyage.ReturnItem);
        Assert.Equal(voyage.Origin, voyage.Player.DockedStationObjectId);
        Assert.Equal(4, voyage.Receipts.Count);
        Assert.Equal(2, voyage.Snapshot.VoyageFinances.Length);
        Assert.All(voyage.Snapshot.VoyageFinances, f => { Assert.NotNull(f.CompletedGameTimeMs); Assert.True(f.RouteFuelCostCredits >= 0); });
        Assert.All(voyage.Save().GameState.SpaceObjects.Where(o => o.MarketProfileId is not null), o => Assert.True(o.MarketBudgetCredits >= 0));
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(42UL)]
    public void LocalTradeRoundTripThroughCommands(ulong seed) => RoundTrip(seed, false);

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(42UL)]
    public void InterclusterReturnVoyageThroughCommands(ulong seed) => RoundTrip(seed, true);

    [Fact]
    public void OrbitVoyagePreservesApproachAndLedger()
    {
        using var voyage = new ClusterVoyageFixture(42, true);
        voyage.Trade(TradeCommandTypes.Buy, voyage.OutboundItem);
        voyage.FlyTo(voyage.Destination, v =>
        {
            Assert.Equal(4, v.Player.SpeedKmS);
            Assert.NotNull(v.Player.ApproachRoute); Assert.False(v.Player.IsDocked);
            Assert.Equal(v.Destination, v.Snapshot.ActiveVoyage!.DestinationStationObjectId);
        });
        var flight = Assert.Single(voyage.Flights);
        Assert.True(flight.ActualDays >= flight.StraightDays);
        Assert.NotEqual(flight.StraightDays, flight.ActualDays);
        var receipt = voyage.Trade(TradeCommandTypes.Sell, voyage.OutboundItem);
        var finance = Assert.Single(voyage.Snapshot.VoyageFinances);
        Assert.Equal(receipt.TotalCredits, finance.GrossSalesCredits);
        Assert.Equal(voyage.Receipts[0].TotalCredits, finance.CostOfGoodsSoldCredits);
        Assert.Equal(finance.GrossSalesCredits - finance.CostOfGoodsSoldCredits - finance.RouteFuelCostCredits - finance.PortFeesAssessedCredits - finance.EventCostsCredits + finance.PassengerPayoutCredits - finance.PassengerPenaltyCredits, finance.NetProfitCredits);
    }
}
