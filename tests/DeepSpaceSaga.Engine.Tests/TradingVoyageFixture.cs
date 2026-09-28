using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>Command-driven, monotonic-time voyage through the shipped map and command catalog.</summary>
internal sealed class TradingVoyageFixture : IDisposable
{
    private const string ShipId = "SPC-0001";
    private const string BridgeId = "MOD-PLAYER-BRIDGE-01";
    private const string EngineId = "MOD-PLAYER-ENGINE-01";
    private const string CargoId = "MOD-PLAYER-CARGO-01";
    private const long TimeRatio = 300;

    private long _motionTime;
    private long _nextCommand;

    private TradingVoyageFixture(SimulationEngine engine, string origin, string destination,
        string outboundItem, string returnItem)
    {
        Engine = engine;
        Origin = origin;
        Destination = destination;
        OutboundItem = outboundItem;
        ReturnItem = returnItem;
        Snapshot = Capture();
    }

    internal SimulationEngine Engine { get; }
    internal AuthoritativeSnapshot Snapshot { get; private set; }
    internal string Origin { get; }
    internal string Destination { get; }
    internal string OutboundItem { get; }
    internal string ReturnItem { get; }
    internal long MotionTime => _motionTime;

    internal static TradingVoyageFixture Create(ulong seed = 1, bool controlled = true)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "Docked", "scenario.json"));
        scenario = scenario with { GameState = scenario.GameState with { MasterSeed = seed, PlayerTokens = 1_000_000 } };
        var engine = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        engine.LoadScenario(scenario);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        string origin = save.GameState.SpaceObjects.Single(o => o.ObjectId == ShipId).DockedStationObjectId!;
        var map = save.GameState.TradingMap!;
        var neighbors = map.Edges.Select(e => e.FromStationObjectId == origin ? e.ToStationObjectId :
                e.ToStationObjectId == origin ? e.FromStationObjectId : null)
            .Where(id => id is not null).Order(StringComparer.Ordinal).ToArray();
        foreach (string neighbor in neighbors!)
        {
            var originStation = save.GameState.SpaceObjects.Single(o => o.ObjectId == origin);
            var destinationStation = save.GameState.SpaceObjects.Single(o => o.ObjectId == neighbor);
            var outbound = (originStation.Inventory ?? []).Where(i => i.Quantity > 0 && i.ItemTypeId != "item.fuel")
                .OrderBy(i => i.ItemTypeId, StringComparer.Ordinal).Select(i => i.ItemTypeId).FirstOrDefault();
            var returning = (destinationStation.Inventory ?? []).Where(i => i.Quantity > 0 && i.ItemTypeId != "item.fuel" &&
                i.ItemTypeId != outbound).OrderBy(i => i.ItemTypeId, StringComparer.Ordinal)
                .Select(i => i.ItemTypeId).FirstOrDefault();
            if (outbound is null || returning is null) continue;
            if (!controlled)
                return new TradingVoyageFixture(engine, origin, neighbor, outbound, returning);

            var controlledSave = save with
            {
                GameState = save.GameState with
                {
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectType == SpaceObjectType.Station
                        ? o with { PortFeeCreditsPerDay = 100 } : o.ObjectId == ShipId
                        ? o with { Crew = [], Passengers = [] } : o).ToArray(),
                }
            };
            engine.Dispose();
            var prepared = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
            prepared.LoadScenario(controlledSave, isSave: true);
            return new TradingVoyageFixture(prepared, origin, neighbor, outbound, returning);
        }
        engine.Dispose();
        throw new Xunit.Sdk.XunitException($"Seed {seed}: no adjacent A/B stock for reciprocal trades from {origin}; " +
            $"neighbors={string.Join(',', neighbors)}.");
    }

    internal AuthoritativeSnapshot Capture() =>
        Snapshot = Engine.CaptureSnapshotForTests(checked(_motionTime * TimeRatio),
            SimulationSpeed.Speed0, _motionTime);

    internal AuthoritativeSnapshot Advance(long motionDeltaMs)
    {
        _motionTime = checked(_motionTime + motionDeltaMs);
        return Capture();
    }

    internal (PlayerCommand Command, CommandResult? Result) Send(string module, string type,
        string? target = null, string? item = null, long? quantity = null,
        TradeQuoteSnapshot? quote = null)
    {
        long sequence = ++_nextCommand;
        var command = new PlayerCommand($"voyage-command-{sequence}", (ulong)sequence,
            ShipId, module, type, TargetObjectId: target, ItemTypeId: item,
            Quantity: quantity, QuoteId: quote?.QuoteId, MarketRevision: quote?.MarketRevision);
        Engine.ReceiveCommand(command);
        var snapshot = Capture();
        return (command, snapshot.CommandResults.SingleOrDefault(r => r.CommandId == command.CommandId));
    }

    internal TradeExecutionReceipt Trade(string type, string item, long quantity = 1)
    {
        var quote = Engine.GetTradeQuote(new TradeQuoteRequest($"voyage-quote-{++_nextCommand}",
            ShipId, CargoId, type, item, quantity));
        Xunit.Assert.Null(quote.DisabledReason);
        Xunit.Assert.True(quote.ExecutableQuantity > 0);
        Xunit.Assert.Equal(CurrentStationId, quote.StationObjectId);
        var before = Capture();
        var (_, result) = Send(CargoId, type, item: item, quantity: quantity, quote: quote);
        Xunit.Assert.Equal(CommandResultStatus.Executed, result?.Status);
        var receipt = Xunit.Assert.IsType<TradeExecutionReceipt>(result?.TradeReceipt);
        Xunit.Assert.Equal(item, receipt.ItemTypeId);
        Xunit.Assert.Equal(CurrentStationId, receipt.StationObjectId);
        Xunit.Assert.Equal(type == TradeCommandTypes.Buy
            ? before.PlayerCredits - receipt.TotalCredits
            : before.PlayerCredits + receipt.TotalCredits, Snapshot.PlayerCredits);
        return receipt;
    }

    internal string CurrentStationId => Snapshot.Objects.Single(o => o.ObjectId == ShipId).DockedStationObjectId!;

    internal void FlyTo(string destination)
    {
        var (_, undock) = Send(BridgeId, NavigationComputerCommandTypes.Undock, target: destination);
        Xunit.Assert.Equal(CommandResultStatus.Executed, undock?.Status);
        Xunit.Assert.Null(Snapshot.DockedStationTrade);
        Xunit.Assert.Equal(destination, Snapshot.ActiveVoyage?.DestinationStationObjectId);

        var (_, acceleration) = Send(EngineId, ShipEngineCommandTypes.Accelerate);
        Xunit.Assert.NotEqual(CommandResultStatus.Rejected, acceleration?.Status);
        WaitUntil(s => Ship(s).SpeedKmS > 0, "acceleration");
        double speed = Ship(Snapshot).SpeedKmS;
        Send(EngineId, NavigationComputerCommandTypes.Approach, target: destination);
        WaitUntil(s => Ship(s).ApproachRoute is not null, "approach plan");
        var route = Ship(Snapshot).ApproachRoute!;
        long end = checked(_motionTime + (long)Math.Ceiling(route.DurationMs));
        if (end - _motionTime > 30 * GameCalendar.DayMs / TimeRatio)
            throw new Xunit.Sdk.XunitException("Approach exceeds 30 calendar days.");
        Advance(end - _motionTime);
        Xunit.Assert.Equal(speed, Ship(Snapshot).SpeedKmS);
        Xunit.Assert.False(Ship(Snapshot).IsDocked);

        Send(EngineId, ShipEngineCommandTypes.SpeedSynchronization, target: destination);
        WaitUntil(s => Ship(s).SpeedKmS == 0, "speed synchronization");
        if (Math.Abs(Ship(Snapshot).Direction) > 1e-6)
        {
            Send(EngineId, ShipEngineCommandTypes.DirectionSynchronization, target: destination);
            WaitUntil(s => Math.Abs(Ship(s).Direction) < 1e-6, "direction synchronization");
        }
        var (_, docking) = Send(BridgeId, NavigationComputerCommandTypes.Dock, target: destination);
        Xunit.Assert.Equal(CommandResultStatus.Executed, docking?.Status);
        foreach (string choice in new[] { "truthful_id", "accept_fee", "continue" })
        {
            var active = Xunit.Assert.IsType<DialogueState>(Snapshot.ActiveDialogue);
            var selected = active.Choices.Single(c => c.ChoiceId == choice);
            Xunit.Assert.True(selected.Enabled,
                $"{choice} disabled: {selected.DisabledReasonKey}; station={destination}, " +
                $"ship={Ship(Snapshot)}, time={_motionTime}");
            Engine.ReceiveDialogueCommand(new($"dialogue-{++_nextCommand}", DialogueAction.Choose,
                active.InstanceId, active.Revision, choice));
            Capture();
        }
        Xunit.Assert.Null(Snapshot.ActiveDialogue);
        Xunit.Assert.True(Ship(Snapshot).IsDocked);
        Xunit.Assert.Equal(destination, CurrentStationId);
        Xunit.Assert.Null(Snapshot.ActiveVoyage);
    }

    private void WaitUntil(Func<AuthoritativeSnapshot, bool> predicate, string phase)
    {
        for (int cycle = 0; cycle < 5000; cycle++)
        {
            if (predicate(Snapshot)) return;
            Advance(1000);
        }
        throw new Xunit.Sdk.XunitException($"{phase} timeout at physical={_motionTime}, calendar={_motionTime * TimeRatio}, " +
            $"ship={Ship(Snapshot)}, voyage={Snapshot.Voyage}");
    }

    private static ObjectMotionSnapshot Ship(AuthoritativeSnapshot snapshot) =>
        snapshot.Objects.Single(o => o.ObjectId == ShipId);

    public void Dispose() => Engine.Dispose();
}
