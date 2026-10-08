using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Trading;

namespace DeepSpaceSaga.EconomyBalance;

internal sealed record BalanceShipConfiguration(string Id, int CargoCapacityMultiplierPermille, int FuelEfficiencyMultiplierPermille);
internal sealed record BalanceMatrix(ImmutableArray<ulong> Seeds, ImmutableArray<BalanceShipConfiguration> ShipConfigurations,
    long HorizonGameTimeMs, long SampleIntervalGameTimeMs, long SaveLoadCheckpointGameTimeMs);
internal sealed class BalanceConfigurationException(string message) : Exception(message);
internal sealed record BalanceLedgerEvidence(string VoyageId, string RouteId, string ItemTypeId,
    long GrossSalesCredits, long? CostOfGoodsSoldCredits, long RouteFuelCostCredits,
    long PortFeesAssessedCredits, long EventCostsCredits, long PassengerPayoutCredits,
    long PassengerPenaltyCredits, long? NetProfitCredits);
internal sealed record BalanceStock(string ItemTypeId, long Stock, long? Target, long? Maximum,
    long BuyMaximum, long SellMaximum, ImmutableArray<string> InfluencingEventIds);
internal sealed record BalanceStation(string StationId, long Budget, long MaximumBudget, long Revision, ImmutableArray<BalanceStock> Stocks);
internal sealed record BalanceEvent(string StationId, string EventId, string DefinitionId, long StartedGameTimeMs, long EndsGameTimeMs,
    ImmutableArray<string> InfluencedItems, bool InfluencesRoute);
internal sealed record BalanceRoute(string Origin, string Destination, string DistanceClass, string RiskProfileId,
    TradingRouteRisk Risk, TradingRouteAvailability Availability, long TravelTimeMs, int FuelMultiplierPermille,
    ImmutableArray<string> EventIds);
internal sealed record BalanceFlow(string Origin, string Destination, string ItemTypeId);
internal sealed record BalanceHourlySample(long GameTimeMs, ImmutableArray<BalanceStation> Stations,
    ImmutableArray<BalanceRoute> Routes, ImmutableArray<BalanceFlow> CargoFlows, ImmutableArray<BalanceEvent> Events);
internal sealed record BalanceQuoteEvidence(string RequestId, string StationId, string ItemTypeId, string CommandType,
    long Revision, long RequestedQuantity, long ExecutableQuantity, long MaximumQuantity, long TotalCredits, string? DisabledReason);
internal sealed record BalanceReplayEvidence(string CommandId, string VoyageId, long RevisionBefore, long RevisionAfter,
    int PostingsBefore, int PostingsAfter, long? NetBefore, long? NetAfter, string? StaleQuoteReason);
internal sealed record BalanceStrategyEvidence(long StateGameTimeMs, bool EventState, string Origin, string Destination,
    string DistanceClass, string RiskProfileId, string ItemTypeId, string ShipConfigurationId,
    long ActualCapacityKg, long AnalyticalCapacityKg, long RequestedQuantity, long ExecutedBuyQuantity, long ExecutedSellQuantity,
    long BuyGameTimeMs, long SellGameTimeMs, string Outcome, string? Reason,
    BalanceQuoteEvidence? BuyQuote, BalanceQuoteEvidence? SellQuote, TradeExecutionReceipt? BuyReceipt, TradeExecutionReceipt? SellReceipt,
    BalanceLedgerEvidence? Ledger, ImmutableArray<string> PostingIds, ImmutableArray<BalanceReplayEvidence> Replays, int CompletedRoundTrips = 0)
{
    public ImmutableArray<BalanceStrategyEvidence> RoundTripLegs { get; init; } = [];
    public string? RoundTripStopReason { get; init; }
    public long BatchCeilingQuantity { get; init; }
    public long ReservedArrivalFeeCredits { get; init; }
    public BalanceRoute? DepartureRoute { get; init; }
    public long? DepartureGameTimeMs { get; init; }
}
internal sealed record BalanceCaseEvidence(ulong Seed, string ShipConfigurationId, ImmutableArray<BalanceHourlySample> HourlySamples,
    ImmutableArray<BalanceStrategyEvidence> Strategies, string ContinuousStateHash, string SaveLoadStateHash)
{
    public bool Equals(BalanceCaseEvidence? other) => other is not null && BalanceCanonical.Json(this) == BalanceCanonical.Json(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(BalanceCanonical.Json(this));
}

internal static class BalanceCanonical
{
    internal static string Json<T>(T value) => JsonSerializer.Serialize(value);
    internal static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    internal static TradeExecutionReceipt? Receipt(TradeExecutionReceipt? receipt) => receipt is null ? null : receipt with { QuoteId = null };
    internal static string State(ScenarioFile save) => Hash(SimulationEngine.NormalizeTradingContinuationForTests(save.GameState));
}

internal sealed class EconomyBalanceRunner
{
    internal static void Validate(BalanceMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Seeds.IsDefaultOrEmpty || matrix.Seeds.Distinct().Count() != matrix.Seeds.Length)
            throw new BalanceConfigurationException("matrix.seeds: expected nonempty unique seeds.");
        if (matrix.ShipConfigurations.IsDefaultOrEmpty || matrix.ShipConfigurations.Any(c => c is null ||
                string.IsNullOrWhiteSpace(c.Id) || c.CargoCapacityMultiplierPermille <= 0 || c.FuelEfficiencyMultiplierPermille <= 0) ||
            matrix.ShipConfigurations.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != matrix.ShipConfigurations.Length)
            throw new BalanceConfigurationException("matrix.shipConfigurations: expected unique IDs and positive multipliers.");
        if (matrix.HorizonGameTimeMs <= 0 || matrix.SampleIntervalGameTimeMs <= 0 ||
            matrix.SampleIntervalGameTimeMs % GameCalendar.HourMs != 0 || matrix.HorizonGameTimeMs % matrix.SampleIntervalGameTimeMs != 0 ||
            matrix.SaveLoadCheckpointGameTimeMs <= 0 || matrix.SaveLoadCheckpointGameTimeMs >= matrix.HorizonGameTimeMs ||
            matrix.SaveLoadCheckpointGameTimeMs % matrix.SampleIntervalGameTimeMs != 0)
            throw new BalanceConfigurationException("matrix.time: expected positive aligned hourly horizon and interior checkpoint.");
        // Current configurations deliberately use the real fuel owner without a tool-side fuel formula.
        if (matrix.ShipConfigurations.Any(c => c.FuelEfficiencyMultiplierPermille != 1000))
            throw new BalanceConfigurationException("matrix.shipConfigurations.fuelEfficiency: only authoritative 1000 is supported.");
        if (matrix.HorizonGameTimeMs / matrix.SampleIntervalGameTimeMs > 10000)
            throw new BalanceConfigurationException("matrix.time: sampling limit is 10000 intervals.");
    }

    internal ImmutableArray<BalanceCaseEvidence> Run(string settingsPath, string scenarioPath, BalanceMatrix matrix)
    {
        Validate(matrix);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
        var cases = ImmutableArray.CreateBuilder<BalanceCaseEvidence>();
        foreach (ulong seed in matrix.Seeds.Order())
        {
            using var passive = Create(settingsPath, scenarioPath, scenario, seed);
            var samples = ImmutableArray.CreateBuilder<BalanceHourlySample>();
            var checkpoints = new SortedDictionary<long, ScenarioFile>();
            SimulationEngine? reload = null;
            var continuousTrace = new List<string>(); var loadedTrace = new List<string>();
            bool hadEvent = false, retainedNormal = false;
            try
            {
                for (long time = 0; ; time = checked(time + matrix.SampleIntervalGameTimeMs))
                {
                    passive.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, 0);
                    var save = passive.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, 0);
                    var sample = Sample(passive, registry, save);
                    samples.Add(sample);
                    bool eventState = sample.Events.Length > 0;
                    // Initial, midpoint, final, first event and first following normal state form a fixed
                    // observable comparison set. Every route/item at each retained state is attempted.
                    if (time == 0 || time == matrix.SaveLoadCheckpointGameTimeMs || time == matrix.HorizonGameTimeMs ||
                        eventState && !hadEvent || !eventState && hadEvent && !retainedNormal)
                        checkpoints.Add(time, save);
                    if (!eventState && hadEvent) retainedNormal = true;
                    hadEvent |= eventState;
                    if (time == matrix.SaveLoadCheckpointGameTimeMs)
                    {
                        reload = new SimulationEngine(registry);
                        reload.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
                    }
                    if (reload is not null)
                    {
                        reload.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, 0);
                        var loadedSave = reload.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, 0);
                        continuousTrace.Add(BalanceCanonical.Hash(new { Sample = sample, State = BalanceCanonical.State(save) }));
                        loadedTrace.Add(BalanceCanonical.Hash(new { Sample = Sample(reload, registry, loadedSave), State = BalanceCanonical.State(loadedSave) }));
                    }
                    if (time == matrix.HorizonGameTimeMs) break;
                }
                foreach (var config in matrix.ShipConfigurations.OrderBy(c => c.Id, StringComparer.Ordinal))
                {
                    var strategies = ImmutableArray.CreateBuilder<BalanceStrategyEvidence>();
                    bool replayProofAttempted = false;
                    foreach (var (time, save) in checkpoints)
                    {
                        var sample = samples.Single(s => s.GameTimeMs == time);
                        foreach (var route in sample.Routes.OrderBy(r => r.Origin, StringComparer.Ordinal).ThenBy(r => r.Destination, StringComparer.Ordinal))
                        {
                            var source = sample.Stations.Single(s => s.StationId == route.Origin);
                            foreach (var item in source.Stocks.Where(s => s.Target is not null).OrderBy(s => s.ItemTypeId, StringComparer.Ordinal))
                            {
                                using var driver = new BalanceDriver(registry, save);
                                var strategy = driver.Run(sample, route, item.ItemTypeId, config);
                                if (!replayProofAttempted && strategy.Outcome == "completed")
                                {
                                    strategy = driver.ProveRoundTrips(strategy, sample, route, config);
                                    replayProofAttempted = strategy.CompletedRoundTrips == 3;
                                }
                                strategies.Add(strategy);
                            }
                        }
                    }
                    cases.Add(new(seed, config.Id, samples.ToImmutable(), strategies.ToImmutable(),
                        BalanceCanonical.Hash(continuousTrace), BalanceCanonical.Hash(loadedTrace)));
                }
            }
            finally { reload?.Dispose(); }
        }
        return cases.ToImmutable();
    }

    private static SimulationEngine Create(string settings, string scenarioPath, ScenarioFile scenario, ulong seed)
    {
        // Bootstrap once through the shipped loader so resource/portrait configuration is retained.
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(settings, scenarioPath);
        try
        {
            engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = seed } },
                generation: null);
            return engine;
        }
        catch { engine.Dispose(); throw; }
    }

    internal static ImmutableArray<string> InfluencedItems(StationEventData evt, IEnumerable<(string ItemId, string Category)> inventory)
    {
        var affected = (evt.ItemEffects ?? []).Where(i => i.ProductionMultiplierPermille != 1000 || i.DemandMultiplierPermille != 1000 ||
            i.PriceMultiplierPermille != 1000 || i.ActivationStockDelta != 0).Select(i => i.ItemTypeId);
        var factors = (evt.PriceFactors ?? []).Where(f => f.Factor != 1000).ToArray();
        return affected.Concat(inventory.Where(i => factors.Any(f => f.ItemTypeId is { } item ? item == i.ItemId :
            f.Category is null || string.Equals(f.Category, i.Category, StringComparison.OrdinalIgnoreCase))).Select(i => i.ItemId))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
    }

    internal static BalanceHourlySample Sample(SimulationEngine engine, GameDataRegistry registry, ScenarioFile save, TradingMapStateData? geography = null)
    {
        long time = save.GameState.GameTimeMs;
        var map = geography ?? save.GameState.TradingMap ?? throw new BalanceConfigurationException("scenario.tradingMap: a materialized trading map is required.");
        var events = save.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.Station)
            .OrderBy(o => o.ObjectId, StringComparer.Ordinal).SelectMany(o => (o.Events ?? []).Where(e => e.StartedGameTimeMs <= time &&
                (e.DurationMs is null || e.StartedGameTimeMs + e.DurationMs > time)).OrderBy(e => e.EventId, StringComparer.Ordinal)
                .Select(e => new BalanceEvent(o.ObjectId, e.EventId, e.DefinitionId ?? "legacy", e.StartedGameTimeMs,
                    e.DurationMs is null ? long.MaxValue : checked(e.StartedGameTimeMs + e.DurationMs.Value),
                    InfluencedItems(e, (o.Inventory ?? []).Select(i =>
                    {
                        var item = registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(i.ItemTypeId));
                        return (item.TypeId, item.Category.ToString());
                    })),
                    e.RouteEffect?.FromStationObjectId is not null))).ToImmutableArray();
        var modifiers = save.GameState.SpaceObjects.SelectMany(o => (o.Events ?? []).Where(e => e.StartedGameTimeMs <= time &&
                (e.DurationMs is null || e.StartedGameTimeMs + e.DurationMs > time) && e.RouteEffect?.FromStationObjectId is not null)
            .Select(e =>
            {
                var effect = e.RouteEffect!;
                var definition = registry.StationMarketEvents.GetDefinition(registry.StationMarketEvents.GetIndex(e.DefinitionId!));
                return new TradingRouteModifier(e.EventId, e.DefinitionId!, definition.Priority, e.StartedGameTimeMs,
                    effect.FromStationObjectId!, effect.ToStationObjectId!, Enum.Parse<TradingRouteAvailability>(effect.Availability),
                    effect.TravelTimeMultiplierPermille, effect.FuelMultiplierPermille, definition.EffectSummaryKey);
            })).ToArray();
        var routes = TradingRouteEvaluator.Evaluate(map, modifiers, geography is not null).SelectMany(r => new[]
        {
            new BalanceRoute(r.BaseEdge.FromStationObjectId, r.BaseEdge.ToStationObjectId, r.BaseEdge.DistanceClass,
                r.BaseEdge.RiskProfileId, r.Risk, r.Availability, r.EffectiveTravelEstimateGameTimeMs, r.EffectiveFuelMultiplierPermille, r.ActiveEventIds),
            new BalanceRoute(r.BaseEdge.ToStationObjectId, r.BaseEdge.FromStationObjectId, r.BaseEdge.DistanceClass,
                r.BaseEdge.RiskProfileId, r.Risk, r.Availability, r.EffectiveTravelEstimateGameTimeMs, r.EffectiveFuelMultiplierPermille, r.ActiveEventIds),
        }).OrderBy(r => r.Origin, StringComparer.Ordinal).ThenBy(r => r.Destination, StringComparer.Ordinal).ToImmutableArray();
        var stations = engine.CaptureMarketDiagnosticsForTests().Select(s => new BalanceStation(s.Market.StationObjectId,
            s.Budget, s.MaximumBudget, s.Market.MarketRevision ?? 0, s.Market.Items.Where(i => i.TargetStock is not null)
            .OrderBy(i => i.ItemTypeId, StringComparer.Ordinal).Select(i => new BalanceStock(i.ItemTypeId, i.StockQuantity,
                i.TargetStock, i.MaxStock, i.StockQuantity, i.MaxSellableQuantity, events.Where(e => e.StationId == s.Market.StationObjectId &&
                    e.InfluencedItems.Contains(i.ItemTypeId)).Select(e => e.EventId).ToImmutableArray())).ToImmutableArray())).ToImmutableArray();
        var flows = map.CargoFlows.SelectMany(f => f.ItemTypeIds.Select(i => new BalanceFlow(f.FromStationObjectId, f.ToStationObjectId, i)))
            .OrderBy(f => f.Origin, StringComparer.Ordinal).ThenBy(f => f.Destination, StringComparer.Ordinal).ThenBy(f => f.ItemTypeId, StringComparer.Ordinal).ToImmutableArray();
        return new(time, stations, routes, flows, events);
    }
}

/// <summary>All movements, docking, quotes and cash postings are accepted by the real Engine.</summary>
internal sealed class BalanceDriver : IDisposable
{
    private readonly bool _clusterRun;
    private readonly Action<SimulationEngine, ScenarioFile>? _hourly;
    private readonly long _cap;
    internal long MinimumDepartureHoldGameTimeMs { get; set; }
    private readonly SimulationEngine _engine;
    private readonly GameDataRegistry _registry;
    private readonly string _ship, _bridge, _engineModule, _cargo;
    private long _calendar, _motion, _sequence;
    private string? _pendingCommandId;
    private long _pendingStartedMotionTime;
    private readonly Dictionary<string, CommandResult> _results = new(StringComparer.Ordinal);
    private AuthoritativeSnapshot _snapshot;
    internal BalanceDriver(GameDataRegistry registry, ScenarioFile save, bool clusterRun = false,
        Action<SimulationEngine, ScenarioFile>? hourly = null, long cap = long.MaxValue)
    {
        _clusterRun = clusterRun; _hourly = hourly; _cap = cap;
        _registry = registry; _engine = new(registry);
        _engine.LoadScenario(save, true);
        _calendar = save.GameState.GameTimeMs; _motion = save.GameState.SimulationTimeMs ?? _calendar;
        _snapshot = Capture(); _ship = _snapshot.PlayerShipObjectId ?? throw new BalanceConfigurationException("scenario.playerShip: required.");
        string Module(string command) => _snapshot.InstalledModules.FirstOrDefault(m => !m.Commands.IsDefaultOrEmpty &&
            m.Commands.Any(c => c.CommandTypeId == command))?.ModuleId ?? throw new BalanceConfigurationException($"scenario.module: missing {command}.");
        _bridge = Module(NavigationComputerCommandTypes.Dock); _engineModule = Module(NavigationComputerCommandTypes.Approach);
        _cargo = Module(TradeCommandTypes.Buy);
    }
    private AuthoritativeSnapshot Capture()
    {
        _snapshot = _engine.CaptureSnapshotForTests(_calendar, SimulationSpeed.Speed0, _motion);
        foreach (var result in _snapshot.CommandResults) _results[result.CommandId] = result;
        return _snapshot;
    }
    private bool MotionCommandCompleted => _pendingCommandId is not null && _results.TryGetValue(_pendingCommandId, out var result) &&
        result.Status == CommandResultStatus.Executed && (result.EffectiveGameTimeMs > _pendingStartedMotionTime ||
            !_engine.RuntimeObjects.Where(o => o.InitialMotion.ObjectId == _ship).SelectMany(o => o.Modules)
                .Any(m => m.ActiveCycle?.CommandId == _pendingCommandId));
    private ObjectMotionSnapshot Ship => _snapshot.Objects.Single(o => o.ObjectId == _ship);
    private ScenarioFile Save() => _engine.CaptureSaveStateForTests(_calendar, SimulationSpeed.Speed0, _motion);
    internal ScenarioFile CurrentSave => Save();
    internal AuthoritativeSnapshot CurrentSnapshot => _snapshot;
    internal BalanceHourlySample CurrentSample => EconomyBalanceRunner.Sample(_engine, _registry, Save(), _clusterRun ? _engine.CaptureClusterVoyageMapForTools() : null);
    private void Advance(long ms)
    {
        while (ms > 0)
        {
            long step = _hourly is null ? ms : Math.Min(ms, (GameCalendar.HourMs - _calendar % GameCalendar.HourMs) / 300);
            if (step <= 0) step = 1;
            if (checked(_calendar + step * 300) > _cap) throw new BalanceConfigurationException("cluster_horizon_cap_reached");
            _calendar = checked(_calendar + step * 300); _motion = checked(_motion + step); ms -= step; Capture();
            if (_calendar % GameCalendar.HourMs == 0) _hourly?.Invoke(_engine, Save());
        }
    }
    private (PlayerCommand Command, CommandResult? Result) Send(string module, string type, string? target = null,
        string? item = null, long? quantity = null, TradeQuoteSnapshot? quote = null)
    {
        long next = ++_sequence;
        var command = new PlayerCommand($"balance-command-{next}", (ulong)next, _ship, module, type,
            TargetObjectId: target, ItemTypeId: item, Quantity: quantity, QuoteId: quote?.QuoteId, MarketRevision: quote?.MarketRevision);
        _engine.ReceiveCommand(command); Capture(); _pendingCommandId = command.CommandId; _pendingStartedMotionTime = _motion;
        return (command, _snapshot.CommandResults.SingleOrDefault(r => r.CommandId == command.CommandId));
    }
    private string? Wait(Func<bool> ready, string phase)
    {
        for (int i = 0; i < 5000; i++)
        {
            if (ready()) return null;
            _results.TryGetValue(_pendingCommandId ?? "", out var result);
            if (result?.Status is CommandResultStatus.Rejected or CommandResultStatus.Cancelled or CommandResultStatus.Failed) return result.ReasonCode ?? phase + "_rejected";
            Advance(1000);
        }
        return $"{phase}_timeout";
    }
    private static string? Rejected(CommandResult? result, string phase) => result?.Status == CommandResultStatus.Rejected ? result.ReasonCode ?? phase : null;
    private string? DockAt(string station)
    {
        if (Ship.IsDocked) return Ship.DockedStationObjectId == station ? null : "already_docked_elsewhere";
        // A departing orbital station can leave a tiny nonzero drift speed. Execute a real
        // acceleration step before Approach rather than mistaking that drift for cruise.
        if (_clusterRun || _snapshot.ActiveVoyage is not null || Ship.SpeedKmS <= 0.0041)
        {
            var acceleration = Send(_engineModule, ShipEngineCommandTypes.Accelerate);
            if (Rejected(acceleration.Result, "acceleration_rejected") is { } accelerationReason) return accelerationReason;
            if (Wait(() => MotionCommandCompleted && (_clusterRun ? Ship.SpeedKmS >= Ship.MaxSpeedKmS : Ship.SpeedKmS > 0), "acceleration") is { } waitReason) return waitReason;
        }
        var approach = Send(_engineModule, NavigationComputerCommandTypes.Approach, station);
        if (Rejected(approach.Result, "approach_rejected") is { } approachReason) return approachReason;
        if (Wait(() => Ship.ApproachRoute is not null, "approach") is { } planReason) return planReason;
        long duration = (long)Math.Ceiling(Ship.ApproachRoute!.DurationMs);
        if (duration < 0 || duration > 60 * GameCalendar.DayMs) return "approach_outside_diagnostic_limit";
        Advance(duration);
        var synchronize = Send(_engineModule, ShipEngineCommandTypes.SpeedSynchronization, station);
        if (Rejected(synchronize.Result, "speed_sync_rejected") is { } speedReason) return speedReason;
        if (Wait(() => MotionCommandCompleted, "speed_sync") is { } stopReason) return stopReason;
        double targetDirection = _snapshot.Objects.Single(o => o.ObjectId == station).Direction;
        if (Math.Abs(Ship.Direction - targetDirection) > 1e-6)
        {
            var direction = Send(_engineModule, ShipEngineCommandTypes.DirectionSynchronization, station);
            if (Rejected(direction.Result, "direction_sync_rejected") is { } directionReason) return directionReason;
            if (Wait(() => MotionCommandCompleted, "direction_sync") is { } turnReason) return turnReason;
        }
        var docking = Send(_bridge, NavigationComputerCommandTypes.Dock, station);
        if (Rejected(docking.Result, "dock_rejected") is { } dockingReason) return dockingReason;
        for (int step = 0; step < 10 && _snapshot.ActiveDialogue is { } dialogue; step++)
        {
            string? choice = new[] { "truthful_id", "accept_fee", "continue" }.FirstOrDefault(id => dialogue.Choices.Any(c => c.ChoiceId == id && c.Enabled));
            if (choice is null) return "docking_dialogue_no_enabled_path";
            _engine.ReceiveDialogueCommand(new($"balance-dialogue-{++_sequence}", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice)); Capture();
        }
        return Ship.IsDocked && Ship.DockedStationObjectId == station ? null : "docking_incomplete";
    }
    private string? Fly(string station)
    {
        if (_clusterRun)
            for (int hour = 0; _snapshot.Voyage?.RouteOptions.FirstOrDefault(r => r.DestinationStationObjectId == station)?.IsAvailable == false && hour < 168; hour++) Advance(12000);
        var departure = Send(_bridge, NavigationComputerCommandTypes.Undock, station);
        if (_clusterRun && departure.Result?.Status != CommandResultStatus.Rejected && _calendar < MinimumDepartureHoldGameTimeMs)
            Advance((MinimumDepartureHoldGameTimeMs - _calendar + 299) / 300);
        return Rejected(departure.Result, "undock_rejected") ?? DockAt(station);
    }
    private string? PositionAt(string origin)
    {
        if (!Ship.IsDocked) return DockAt(origin);
        if (Ship.DockedStationObjectId == origin) return null;
        var sample = CurrentSample;
        var queue = new Queue<string>(); queue.Enqueue(Ship.DockedStationObjectId!);
        var previous = new Dictionary<string, string?>(StringComparer.Ordinal) { [Ship.DockedStationObjectId!] = null };
        while (queue.TryDequeue(out var station))
        {
            foreach (var route in sample.Routes.Where(r => r.Origin == station && r.Availability != TradingRouteAvailability.Unavailable).OrderBy(r => r.Destination, StringComparer.Ordinal))
                if (previous.TryAdd(route.Destination, station)) queue.Enqueue(route.Destination);
        }
        if (!previous.ContainsKey(origin)) return "origin_unreachable";
        var path = new Stack<string>(); string current = origin;
        while (previous[current] is { } parent) { path.Push(current); current = parent; }
        foreach (string destination in path) if (Fly(destination) is { } reason) return reason;
        return null;
    }
    private TradeQuoteSnapshot Quote(string type, string item, long quantity) => _engine.GetTradeQuote(new($"balance-quote-{++_sequence}", _ship, _cargo, type, item, quantity));
    private static BalanceQuoteEvidence Terms(TradeQuoteSnapshot q) => new(q.RequestId, q.StationObjectId, q.ItemTypeId, q.CommandType,
        q.MarketRevision, q.RequestedQuantity, q.ExecutableQuantity, q.MaximumQuantity, q.TotalCredits, q.DisabledReason);
    private BalanceReplayEvidence Replay(PlayerCommand command, TradeQuoteSnapshot quote, string voyageId)
    {
        var before = Save().GameState; var station = before.SpaceObjects.Single(o => o.ObjectId == quote.StationObjectId);
        int count = (before.VoyageLedgers ?? []).Sum(l => l.Postings.Count);
        long? net = before.VoyageLedgers?.FirstOrDefault(l => l.Finance.VoyageId == voyageId)?.Finance.NetProfitCredits;
        _engine.ReceiveCommand(command); Capture();
        var stale = Send(_cargo, command.CommandType, item: command.ItemTypeId, quantity: command.Quantity, quote: quote);
        var after = Save().GameState;
        return new(command.CommandId, voyageId, station.MarketRevision ?? 0, after.SpaceObjects.Single(o => o.ObjectId == station.ObjectId).MarketRevision ?? 0,
            count, (after.VoyageLedgers ?? []).Sum(l => l.Postings.Count), net,
            after.VoyageLedgers?.FirstOrDefault(l => l.Finance.VoyageId == voyageId)?.Finance.NetProfitCredits, stale.Result?.ReasonCode);
    }
    internal BalanceStrategyEvidence Run(BalanceHourlySample state, BalanceRoute route, string item, BalanceShipConfiguration config)
    {
        long capacity = 0, analytical = 0, requested = 0, ceiling = 0, arrivalFee = 0, buyTime = _calendar, sellTime = _calendar;
        BalanceRoute? departureRoute = null; long? departureTime = null;
        TradeQuoteSnapshot? buyQuote = null, sellQuote = null; TradeExecutionReceipt? buy = null, sell = null;
        BalanceLedgerEvidence? ledger = null; ImmutableArray<string> postingIds = []; var replays = ImmutableArray.CreateBuilder<BalanceReplayEvidence>();
        BalanceStrategyEvidence Evidence(string outcome, string? reason) => new(state.GameTimeMs, state.Events.Length > 0,
            route.Origin, route.Destination, route.DistanceClass, route.RiskProfileId, item, config.Id,
            capacity, analytical, requested, buy?.ExecutedQuantity ?? 0, sell?.ExecutedQuantity ?? 0, buyTime, sellTime,
            outcome, reason, buyQuote is null ? null : Terms(buyQuote), sellQuote is null ? null : Terms(sellQuote),
            BalanceCanonical.Receipt(buy), BalanceCanonical.Receipt(sell), ledger, postingIds, replays.ToImmutable())
        { BatchCeilingQuantity = ceiling, ReservedArrivalFeeCredits = arrivalFee, DepartureRoute = departureRoute, DepartureGameTimeMs = departureTime };
        if (PositionAt(route.Origin) is { } positioningReason) return Evidence("rejected", positioningReason);
        var module = _snapshot.InstalledModules.Single(m => m.ModuleId == _cargo);
        capacity = module.AvailableCapacityKg ?? 0;
        // Cluster configurations already install the upgraded capacity in the registry.
        analytical = _clusterRun ? capacity : checked((long)((Int128)capacity * config.CargoCapacityMultiplierPermille / 1000));
        buyQuote = Quote(TradeCommandTypes.Buy, item, 1);
        long unitMass = _snapshot.DockedStationTrade?.Items.FirstOrDefault(i => i.ItemTypeId == item)?.UnitMassKg ?? 1;
        ceiling = Math.Min(buyQuote.MaximumQuantity, unitMass > 0 ? analytical / unitMass : long.MaxValue);
        if (_clusterRun) ceiling = Math.Min(ceiling, 1);
        if (ceiling <= 0 || buyQuote.DisabledReason is not null) return Evidence("rejected", buyQuote.DisabledReason ?? "zero_batch_ceiling");
        // Batch ceiling is an upper bound. Keep the destination's declared first port fee
        // available instead of deliberately creating irrecoverable debt on every maximal Buy.
        // All prefix prices/affordability still come from authoritative quotes.
        arrivalFee = Save().GameState.SpaceObjects.Single(o => o.ObjectId == route.Destination).PortFeeCreditsPerDay ?? 0;
        long budget = arrivalFee > _snapshot.PlayerCredits ? 0 : _snapshot.PlayerCredits - arrivalFee;
        long low = 0, high = ceiling;
        while (low < high)
        {
            long mid = low + (long)(((Int128)high - low + 1) / 2);
            var candidate = Quote(TradeCommandTypes.Buy, item, mid);
            if (candidate.DisabledReason is null && candidate.TotalCredits <= budget) low = mid;
            else high = mid - 1;
        }
        requested = low;
        if (requested <= 0) return Evidence("rejected", "arrival_fee_reserve_leaves_no_buy_budget");
        buyQuote = Quote(TradeCommandTypes.Buy, item, requested); buyTime = _calendar;
        var buyResult = Send(_cargo, TradeCommandTypes.Buy, item: item, quantity: requested, quote: buyQuote); buy = buyResult.Result?.TradeReceipt;
        if (buyResult.Result?.Status != CommandResultStatus.Executed) return Evidence("rejected", buyResult.Result?.ReasonCode ?? "buy_incomplete");
        replays.Add(Replay(buyResult.Command, buyQuote, "predeparture"));
        departureTime = _calendar;
        departureRoute = CurrentSample.Routes.Single(r => r.Origin == route.Origin && r.Destination == route.Destination);
        if (Fly(route.Destination) is { } flightReason) return Evidence("rejected", flightReason);
        string? voyageId = _snapshot.VoyageFinances.LastOrDefault(v => v.OriginStationObjectId == route.Origin && v.DestinationStationObjectId == route.Destination)?.VoyageId;
        if (voyageId is null) return Evidence("unknown", "matching_ledger_missing");
        long carried = _snapshot.InstalledModules.Single(m => m.ModuleId == _cargo).Cargo.Where(c => c.ItemTypeId == item).Sum(c => c.Quantity);
        if (carried <= 0) return Evidence("rejected", "bought_cargo_consumed_in_flight");
        sellQuote = Quote(TradeCommandTypes.Sell, item, Math.Min(buy!.ExecutedQuantity, carried)); sellTime = _calendar;
        var sold = Send(_cargo, TradeCommandTypes.Sell, item: item, quantity: sellQuote.RequestedQuantity, quote: sellQuote); sell = sold.Result?.TradeReceipt;
        var savedLedger = Save().GameState.VoyageLedgers?.SingleOrDefault(l => l.Finance.VoyageId == voyageId);
        if (savedLedger is not null)
        {
            var f = savedLedger.Finance;
            ledger = new(f.VoyageId, route.Origin + "/" + route.Destination, item, f.GrossSalesCredits, f.CostOfGoodsSoldCredits,
                f.RouteFuelCostCredits, f.PortFeesAssessedCredits, f.EventCostsCredits, f.PassengerPayoutCredits, f.PassengerPenaltyCredits, f.NetProfitCredits);
            postingIds = savedLedger.Postings.Select(p => p.PostingId).Order(StringComparer.Ordinal).ToImmutableArray();
        }
        if (sold.Result?.Status != CommandResultStatus.Executed) return Evidence("rejected", sold.Result?.ReasonCode ?? "sell_incomplete");
        replays.Add(Replay(sold.Command, sellQuote, voyageId));
        return Evidence(sell!.ExecutedQuantity != buy!.ExecutedQuantity ? "partial" : ledger?.CostOfGoodsSoldCredits is null ? "unknown" : "completed", null);
    }
    internal BalanceStrategyEvidence ProveRoundTrips(BalanceStrategyEvidence first, BalanceHourlySample state,
        BalanceRoute outbound, BalanceShipConfiguration config)
    {
        var legs = ImmutableArray.CreateBuilder<BalanceStrategyEvidence>(); legs.Add(first);
        int completed = 0; string? stopped = null;
        var reverse = state.Routes.Single(r => r.Origin == outbound.Destination && r.Destination == outbound.Origin);
        for (int trip = 0; trip < 3; trip++)
        {
            if (trip > 0)
            {
                var next = Run(state, outbound, first.ItemTypeId, config); legs.Add(next);
                if (next.Outcome == "rejected") { stopped = next.Reason; break; }
            }
            var accepted = _engine.CaptureMarketDiagnosticsForTests().Single(s => s.Market.StationObjectId == outbound.Origin).Market.Items;
            string? item = _snapshot.DockedStationTrade?.Items.Where(i => i.TargetStock is not null && i.StockQuantity > 0 &&
                    accepted.Any(a => a.ItemTypeId == i.ItemTypeId))
                .OrderBy(i => i.ItemTypeId, StringComparer.Ordinal).Select(i => i.ItemTypeId).FirstOrDefault();
            if (item is null) { stopped = "return_stock_unavailable"; break; }
            var returning = Run(state, reverse, item, config); legs.Add(returning);
            if (returning.Outcome == "rejected") { stopped = returning.Reason; break; }
            if (Ship.DockedStationObjectId != outbound.Origin || returning.ExecutedBuyQuantity <= 0 || returning.ExecutedSellQuantity <= 0)
            { stopped = "return_trade_incomplete"; break; }
            completed++;
        }
        return first with { CompletedRoundTrips = completed, RoundTripLegs = legs.ToImmutable(), RoundTripStopReason = stopped };
    }
    public void Dispose() => _engine.Dispose();
}
