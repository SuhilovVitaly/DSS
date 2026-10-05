using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>Command-driven, monotonic-time voyage through the shipped map and command catalog.</summary>
internal sealed class TradingVoyageFixture : IDisposable
{
    private const string ShipId = "SPC-0001";
    private const string BridgeId = "MOD-PLAYER-BRIDGE-01";
    private const string EngineId = "MOD-PLAYER-ENGINE-01";
    private const string CargoId = "MOD-PLAYER-CARGO-01";
    private const long DefaultCalendarRatio = 300;

    private long _motionTime;
    private long _nextCommand;
    private readonly long _calendarRatio;

    private TradingVoyageFixture(SimulationEngine engine, string origin, string destination,
        string outboundItem, string returnItem, long calendarRatio)
    {
        Engine = engine;
        Origin = origin;
        Destination = destination;
        OutboundItem = outboundItem;
        ReturnItem = returnItem;
        _calendarRatio = calendarRatio;
        Snapshot = Capture();
    }

    internal SimulationEngine Engine { get; }
    internal AuthoritativeSnapshot Snapshot { get; private set; }
    internal string Origin { get; }
    internal string Destination { get; }
    internal string OutboundItem { get; }
    internal string ReturnItem { get; }
    internal long MotionTime => _motionTime;
    internal List<string> VoyageIds { get; } = [];
    internal List<double> ApproachSpeeds { get; } = [];
    internal PlayerCommand? LastDockCommand { get; private set; }

    internal static TradingVoyageFixture Create(ulong seed = 1, bool controlled = true,
        long initialDebt = 0, long? destinationBudget = null,
        long calendarRatio = DefaultCalendarRatio, long initialCredits = 1_000_000,
        Func<ScenarioFile, ScenarioFile>? adjust = null, GameDataRegistry? registry = null)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "Docked", "scenario.json"));
        scenario = scenario with { GameState = scenario.GameState with { MasterSeed = seed, PlayerTokens = initialCredits } };
        var engine = new SimulationEngine(registry ?? QuotedTradeExecutionTests.RealRegistry());
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
                return new TradingVoyageFixture(engine, origin, neighbor, outbound, returning,
                    calendarRatio);

            var controlledSave = save with
            {
                GameState = save.GameState with
                {
                    DefenseState = save.GameState.DefenseState! with
                    {
                        Launchers = save.GameState.DefenseState.Launchers.Select(l =>
                        l.OwnerObjectId == ShipId ? l with { State = l.State with { Operator = null, State = DefenseState.NoOperator } } : l).ToArray()
                    },
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectType == SpaceObjectType.Station
                        ? o with
                        {
                            PortFeeCreditsPerDay = 100,
                            Credits = o.ObjectId == neighbor && destinationBudget is not null
                                ? destinationBudget : o.Credits,
                            MarketBudgetCredits = o.ObjectId == neighbor && destinationBudget is not null
                                ? destinationBudget : o.MarketBudgetCredits,
                        } : o.ObjectId == ShipId
                        ? o with { Crew = [], Passengers = [], PortFeeDebt = initialDebt, Modules = o.Modules?.Select(m => m with { OperatorCrewId = null }).ToArray() } : o).ToArray(),
                }
            };
            engine.Dispose();
            var prepared = new SimulationEngine(registry ?? QuotedTradeExecutionTests.RealRegistry());
            prepared.LoadScenario(adjust is null ? controlledSave : adjust(controlledSave), isSave: true);
            return new TradingVoyageFixture(prepared, origin, neighbor, outbound, returning,
                calendarRatio);
        }
        engine.Dispose();
        throw new Xunit.Sdk.XunitException($"Seed {seed}: no adjacent A/B stock for reciprocal trades from {origin}; " +
            $"neighbors={string.Join(',', neighbors)}.");
    }

    internal AuthoritativeSnapshot Capture() =>
        Snapshot = Engine.CaptureSnapshotForTests(checked(_motionTime * _calendarRatio),
            SimulationSpeed.Speed0, _motionTime);

    internal AuthoritativeSnapshot Advance(long motionDeltaMs)
    {
        _motionTime = checked(_motionTime + motionDeltaMs);
        return Capture();
    }

    internal ScenarioFile Save() => Engine.CaptureSaveStateForTests(
        checked(_motionTime * _calendarRatio), SimulationSpeed.Speed0);

    internal long Cargo(string item) => Save().GameState.SpaceObjects.Single(o => o.ObjectId == ShipId)
        .Modules!.Single(module => module.ModuleId == CargoId).Cargo?
        .Where(stack => stack.ItemTypeId == item).Sum(stack => stack.Quantity) ?? 0;

    private IReadOnlyList<(string Item, long Quantity)> OtherCargo(string item) =>
        Save().GameState.SpaceObjects.Single(o => o.ObjectId == ShipId).Modules!
            .SelectMany(module => module.Cargo ?? [])
            .Where(stack => stack.ItemTypeId != item)
            .GroupBy(stack => stack.ItemTypeId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (group.Key, group.Sum(stack => stack.Quantity))).ToArray();

    internal long Stock(string station, string item) => Save().GameState.SpaceObjects
        .Single(o => o.ObjectId == station).Inventory!
        .Single(stock => stock.ItemTypeId == item).Quantity;

    internal CommandResult? Replay(PlayerCommand command)
    {
        Engine.ReceiveCommand(command);
        return Capture().CommandResults.SingleOrDefault(result => result.CommandId == command.CommandId);
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
        long cargoBefore = Cargo(item);
        long stockBefore = Stock(CurrentStationId, item);
        var otherCargoBefore = OtherCargo(item);
        var (_, result) = Send(CargoId, type, item: item, quantity: quantity, quote: quote);
        Xunit.Assert.Equal(CommandResultStatus.Executed, result?.Status);
        var receipt = Xunit.Assert.IsType<TradeExecutionReceipt>(result?.TradeReceipt);
        Xunit.Assert.Equal(item, receipt.ItemTypeId);
        Xunit.Assert.Equal(CurrentStationId, receipt.StationObjectId);
        Xunit.Assert.Equal(type == TradeCommandTypes.Buy
            ? before.PlayerCredits - receipt.TotalCredits
            : before.PlayerCredits + receipt.TotalCredits, Snapshot.PlayerCredits);
        long direction = type == TradeCommandTypes.Buy ? 1 : -1;
        Xunit.Assert.Equal(cargoBefore + direction * receipt.ExecutedQuantity, Cargo(item));
        Xunit.Assert.Equal(stockBefore - direction * receipt.ExecutedQuantity,
            Stock(CurrentStationId, item));
        Xunit.Assert.Equal(otherCargoBefore, OtherCargo(item));
        return receipt;
    }

    internal string CurrentStationId => Snapshot.Objects.Single(o => o.ObjectId == ShipId).DockedStationObjectId!;

    internal void FlyTo(string destination, bool splitSnapshots = false)
    {
        // Temporary route closures require waiting at port; they do not invalidate the
        // command-driven round-trip proof or permit bypassing the authoritative gate.
        for (int hour = 0; Snapshot.TradingRoutes.Single(r => r.DestinationStationObjectId == destination).Availability ==
            TradingRouteAvailability.Unavailable && hour < 168; hour++)
            Advance((GameCalendar.HourMs + _calendarRatio - 1) / _calendarRatio);
        Xunit.Assert.NotEqual(TradingRouteAvailability.Unavailable,
            Snapshot.TradingRoutes.Single(r => r.DestinationStationObjectId == destination).Availability);
        var (_, undock) = Send(BridgeId, NavigationComputerCommandTypes.Undock, target: destination);
        Xunit.Assert.Equal(CommandResultStatus.Executed, undock?.Status);
        Xunit.Assert.Null(Snapshot.DockedStationTrade);
        Xunit.Assert.Equal(destination, Snapshot.ActiveVoyage?.DestinationStationObjectId);
        VoyageIds.Add(Snapshot.ActiveVoyage!.VoyageId!);

        FinishFlightTo(destination, splitSnapshots);
    }

    internal void FinishFlightTo(string destination, bool splitSnapshots = false,
        Action<TradingVoyageFixture>? beforeDialogue = null)
    {
        var (_, acceleration) = Send(EngineId, ShipEngineCommandTypes.Accelerate);
        Xunit.Assert.NotEqual(CommandResultStatus.Rejected, acceleration?.Status);
        WaitUntil(s => Ship(s).SpeedKmS > 0, "acceleration");
        double speed = Ship(Snapshot).SpeedKmS;
        Send(EngineId, NavigationComputerCommandTypes.Approach, target: destination);
        WaitUntil(s => Ship(s).ApproachRoute is not null, "approach plan");
        var route = Ship(Snapshot).ApproachRoute!;
        long end = checked(_motionTime + (long)Math.Ceiling(route.DurationMs));
        if (end - _motionTime > 30 * GameCalendar.DayMs / _calendarRatio)
            throw new Xunit.Sdk.XunitException("Approach exceeds 30 calendar days.");
        if (splitSnapshots)
        {
            long half = (end - _motionTime) / 2;
            if (half > 0) Advance(half);
        }
        Advance(end - _motionTime);
        Xunit.Assert.Equal(speed, Ship(Snapshot).SpeedKmS);
        ApproachSpeeds.Add(speed);
        Xunit.Assert.False(Ship(Snapshot).IsDocked);

        Send(EngineId, ShipEngineCommandTypes.SpeedSynchronization, target: destination);
        WaitUntil(s => Ship(s).SpeedKmS == 0, "speed synchronization");
        if (Math.Abs(Ship(Snapshot).Direction) > 1e-6)
        {
            Send(EngineId, ShipEngineCommandTypes.DirectionSynchronization, target: destination);
            WaitUntil(s => Math.Abs(Ship(s).Direction) < 1e-6, "direction synchronization");
        }
        var (dockCommand, docking) = Send(BridgeId, NavigationComputerCommandTypes.Dock,
            target: destination);
        LastDockCommand = dockCommand;
        Xunit.Assert.Equal(CommandResultStatus.Executed, docking?.Status);
        beforeDialogue?.Invoke(this);
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
        throw new Xunit.Sdk.XunitException($"{phase} timeout at physical={_motionTime}, calendar={_motionTime * _calendarRatio}, " +
            $"ship={Ship(Snapshot)}, voyage={Snapshot.Voyage}");
    }

    private static ObjectMotionSnapshot Ship(AuthoritativeSnapshot snapshot) =>
        snapshot.Objects.Single(o => o.ObjectId == ShipId);

    public void Dispose() => Engine.Dispose();
}
