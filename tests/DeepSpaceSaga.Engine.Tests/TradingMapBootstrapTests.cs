using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingMapBootstrapTests
{
    private enum StartingScenario
    {
        Default,
        Docked,
        Undocked,
    }

    [Fact]
    public void New_game_materializes_exactly_five_stations_and_preserves_ship_state()
    {
        using var engine = new SimulationEngine(Registry());

        engine.LoadScenario(NewGameScenario());

        var snapshot = engine.CaptureSnapshotForTests();
        var stations = snapshot.Objects.Where(obj => obj.ObjectType == "Station").ToArray();
        Assert.Equal(5, stations.Length);
        Assert.Equal(700, snapshot.Objects.Single(obj => obj.ObjectId == "SPC-0001").SpeedKmS * 1000, precision: 6);
        Assert.Equal(11000, stations.Single(obj => obj.ObjectId == "SPC-0002").X);
        Assert.Equal(11000, stations.Single(obj => obj.ObjectId == "SPC-0002").Y);

        var save = engine.CaptureSaveState();
        Assert.Null(save.GameState.TradingMapGeneration);
        Assert.NotNull(save.GameState.TradingMap);
        Assert.Equal(9, save.SaveFormatVersion);
    }

    [Fact]
    public void Default_docked_and_undocked_share_same_seeded_map()
    {
        var shapes = new List<string>();
        foreach (var variant in Enum.GetValues<StartingScenario>())
        {
            using var engine = new SimulationEngine(RegistryWithApproach());
            engine.LoadScenario(NewGameScenario(variant, withApproachModule: true));
            var save = engine.CaptureSaveState();
            shapes.Add(MapShape(save));

            var ship = save.GameState.SpaceObjects.Single(obj => obj.ObjectId == "SPC-0001");
            Assert.Equal(variant == StartingScenario.Docked, ship.IsDocked);
            Assert.Equal(
                variant == StartingScenario.Docked ? "SPC-0002" : null,
                ship.DockedStationObjectId);
            Assert.Equal(variant == StartingScenario.Default ? 700 : 0, ship.SpeedMps);
            var module = Assert.Single(ship.Modules!);
            Assert.Equal("MOD-ENGINE-01", module.ModuleId);
            Assert.Equal("module.engine.basic", module.ModuleTypeId);
            Assert.Equal([new CargoStackData("item.ore", 3)], module.Cargo);
        }

        Assert.Equal(3, shapes.Count);
        Assert.All(shapes.Skip(1), shape => Assert.Equal(shapes[0], shape));
    }

    [Fact]
    public void Save_load_restores_map_coordinates_edges_and_rng_without_generation()
    {
        using var original = new SimulationEngine(Registry());
        original.LoadScenario(NewGameScenario());
        var saved = original.CaptureSaveState();

        using var restored = new SimulationEngine(Registry());
        restored.LoadScenario(saved, isSave: true);
        var roundTrip = restored.CaptureSaveState();

        Assert.Equal(MapShape(saved), MapShape(roundTrip));
        Assert.Equal(saved.GameState.TradingMap!.SchemaVersion, roundTrip.GameState.TradingMap!.SchemaVersion);
        Assert.Equal(saved.GameState.TradingMap.TemplateId, roundTrip.GameState.TradingMap.TemplateId);
        Assert.Equal(saved.GameState.TradingMap.Edges.Select(EdgeShape), roundTrip.GameState.TradingMap.Edges.Select(EdgeShape));
        Assert.Equal(saved.GameState.TradingMap.CargoFlows.Select(FlowShape), roundTrip.GameState.TradingMap.CargoFlows.Select(FlowShape));
        Assert.Equal(saved.GameState.TradingMap.RngStreams.Select(StreamShape), roundTrip.GameState.TradingMap.RngStreams.Select(StreamShape));
        Assert.Equal(
            saved.GameState.SpaceObjects.Select(ObjectShape),
            roundTrip.GameState.SpaceObjects.Select(ObjectShape));
        Assert.Equal(5, roundTrip.GameState.SpaceObjects.Count(obj => obj.ObjectType == "Station"));
        Assert.Null(roundTrip.GameState.TradingMapGeneration);
    }

    [Fact]
    public void Invalid_map_does_not_replace_existing_world()
    {
        using var engine = new SimulationEngine(Registry());
        engine.LoadScenario(LegacyScenario());
        var before = engine.CaptureSnapshotForTests();

        var invalidRules = Rules() with
        {
            Templates =
            [Rules().Templates[0] with
            {
                Offsets = Rules().Templates[0].Offsets
                    .Select(offset => offset.StationObjectId == "SPC-0005" ? offset with { X = 0, Y = 0 } : offset)
                    .ToArray()
            }]
        };

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(NewGameScenario(rules: invalidRules)));

        var after = engine.CaptureSnapshotForTests();
        Assert.Equal(before.Objects.Select(MotionShape), after.Objects.Select(MotionShape));
        Assert.Null(engine.CaptureSaveState().GameState.TradingMap);
    }

    [Fact]
    public void Legacy_scenario_without_map_fields_keeps_existing_object_count()
    {
        using var engine = new SimulationEngine(Registry());

        engine.LoadScenario(LegacyScenario());

        var save = engine.CaptureSaveState();
        Assert.Equal(4, save.GameState.SpaceObjects.Count);
        Assert.Null(save.GameState.TradingMapGeneration);
        Assert.Null(save.GameState.TradingMap);
    }

    [Fact]
    public void Approach_to_each_materialized_station_keeps_ship_speed()
    {
        using var first = new SimulationEngine(RegistryWithApproach());
        first.LoadScenario(NewGameScenario(withApproachModule: true));
        var stationIds = first.CaptureSnapshotForTests().Objects
            .Where(obj => obj.ObjectType == "Station")
            .Select(obj => obj.ObjectId)
            .ToArray();

        foreach (var stationId in stationIds)
        {
            using var engine = new SimulationEngine(RegistryWithApproach());
            engine.LoadScenario(NewGameScenario(withApproachModule: true));
            engine.SetSpeed(SimulationSpeed.Speed1);
            engine.ReceiveCommand(new PlayerCommand(
                "approach-" + stationId,
                1,
                "SPC-0001",
                "MOD-ENGINE-01",
                NavigationComputerCommandTypes.Approach,
                TargetObjectId: stationId));

            var snapshot = engine.CaptureSnapshotForTests();
            var ship = snapshot.Objects.Single(obj => obj.ObjectId == "SPC-0001");
            Assert.Equal(0.7, ship.SpeedKmS, precision: 9);
            Assert.True(ship.ApproachRoute is not null,
                string.Join(", ", snapshot.CommandResults.Select(result => $"{result.Status}:{result.ReasonCode}")));
        }
    }

    private static ScenarioFile NewGameScenario(
        StartingScenario variant = StartingScenario.Default,
        TradingMapGenerationData? rules = null,
        bool withApproachModule = false)
    {
        bool docked = variant == StartingScenario.Docked;
        double shipSpeedMps = variant == StartingScenario.Default ? 700 : 0;
        var ship = new SpaceObjectData(
            "SPC-0001", "PlayerShip", "Permanent", "Player Ship", 10000, 10000, shipSpeedMps, 0,
            shipSpeedMps > 0 ? "Linear" : "Stationary",
            null, null, withApproachModule ? NavigationModules() : null,
            IsKnown: true,
            HullLayout: withApproachModule ? new HullLayoutData(3, 3, [new HullCellCoordinate(1, 1)]) : null,
            IsDocked: docked,
            DockedStationObjectId: docked ? "SPC-0002" : null);
        var objects = BaseObjects(ship, profiledStart: true);
        return new(
            new ScenarioMetadata("trading-map", "Trading map"),
            new GameStateData(
                0, "Speed0", "SPC-0001", null, objects,
                MasterSeed: 123,
                TradingMapGeneration: rules ?? Rules()));
    }

    private static ScenarioFile LegacyScenario() => new(
        new ScenarioMetadata("legacy", "Legacy"),
        new GameStateData(
            0, "Speed0", "SPC-0001", null,
            BaseObjects(new SpaceObjectData(
                "SPC-0001", "PlayerShip", "Permanent", "Player Ship", 10000, 10000, 700, 0, "Linear",
                null, null, null, IsKnown: true), profiledStart: false)));

    private static SpaceObjectData[] BaseObjects(SpaceObjectData ship, bool profiledStart)
    {
        var start = new SpaceObjectData(
            "SPC-0002", "Station", "Permanent", "Start Station", 11000, 11000, 0, 0, "Stationary",
            null, null, null, IsKnown: true,
            MarketProfileId: profiledStart ? "market.transit" : null,
            StationSize: profiledStart ? "Large" : null);
        return
        [
            ship,
            start,
            new("SPC-0003", "Asteroid", "Temporary", null, 10400, 10000, 600, 256, "Linear", 1_000_000, "Silicate", null, IsKnown: true),
            new("SPC-0004", "Asteroid", "Temporary", null, 10000, 10450, 1200, 22, "Linear", 1_000_000, "Ice", null)
        ];
    }

    private static string MapShape(ScenarioFile scenario) => string.Join("|", new[]
    {
        scenario.GameState.MasterSeed?.ToString(),
        scenario.GameState.TradingMap?.TemplateId,
        scenario.GameState.TradingMap?.QuarterTurns.ToString(),
        string.Join(";", scenario.GameState.TradingMap?.Edges.Select(edge =>
            $"{edge.FromStationObjectId}>{edge.ToStationObjectId}:{edge.DistanceKm:R}:{edge.TravelEstimateGameTimeMs}:{edge.DistanceClass}:{edge.RiskProfileId}") ?? []),
        string.Join(";", scenario.GameState.SpaceObjects.Where(obj => obj.ObjectType == "Station").Select(ObjectShape)),
        string.Join(";", scenario.GameState.TradingMap?.RngStreams.Select(stream =>
            $"{stream.Name}:{stream.Seed}:{stream.Counter}") ?? [])
    });

    private static string ObjectShape(SpaceObjectData obj) =>
        $"{obj.ObjectId}:{obj.ObjectType}:{obj.PositionX:R}:{obj.PositionY:R}:{obj.SpeedMps:R}:{obj.IsDocked}:{obj.DockedStationObjectId}";

    private static string EdgeShape(TradingMapEdgeData edge) =>
        $"{edge.FromStationObjectId}>{edge.ToStationObjectId}:{edge.DistanceKm:R}:{edge.TravelEstimateGameTimeMs}:{edge.DistanceClass}:{edge.FuelMultiplierPermille}:{edge.RiskProfileId}";

    private static string FlowShape(TradingMapCargoFlowData flow) =>
        $"{flow.FromStationObjectId}>{flow.ToStationObjectId}:{string.Join(',', flow.ItemTypeIds)}";

    private static string StreamShape(TradingMapRngData stream) =>
        $"{stream.Name}:{stream.Seed}:{stream.Counter}";

    private static string MotionShape(ObjectMotionSnapshot obj) =>
        $"{obj.ObjectId}:{obj.ObjectType}:{obj.X:R}:{obj.Y:R}:{obj.SpeedKmS:R}:{obj.IsDocked}:{obj.DockedStationObjectId}";

    private static TradingMapGenerationData Rules() => new(
        1, "SPC-0002", 700, 21_600_000, 64_800_000, 129_600_000, 5, 2,
        [
            new("SPC-0002", "market.transit", "Transit", "Large"),
            new("SPC-0005", "market.mining", "Mining", "Medium"),
            new("SPC-0006", "market.industrial", "Industrial", "Medium"),
            new("SPC-0007", "market.hydroponic", "Hydroponic", "Medium"),
            new("SPC-0008", "market.scientific-military", "Scientific/Military", "Medium"),
        ],
        [new("seeded",
        [
            new("SPC-0002", "SPC-0005", "risk.safe"),
            new("SPC-0005", "SPC-0006", "risk.normal"),
            new("SPC-0006", "SPC-0007", "risk.normal"),
            new("SPC-0006", "SPC-0008", "risk.elevated"),
            new("SPC-0007", "SPC-0008", "risk.normal"),
            new("SPC-0008", "SPC-0002", "risk.elevated"),
            new("SPC-0005", "SPC-0008", "risk.normal"),
        ],
        [
            new("SPC-0002", 0, 0),
            new("SPC-0005", 100, 0),
            new("SPC-0006", 0, 1000),
            new("SPC-0007", -1000, 0),
            new("SPC-0008", 0, 1500),
        ])],
        [new("risk.elevated", 1250), new("risk.normal", 1100), new("risk.safe", 1000)]);

    private static GameDataRegistry Registry() => GameDataRegistry.Create(
        [], [], Items(), [], stationMarketProfiles: Profiles());

    private static GameDataRegistry RegistryWithApproach() => GameDataRegistry.Create(
        [new ModuleCategoryDefinition(
            "module.engine.basic", "Engine", 1,
            [NavigationComputerCommandTypes.Approach])],
        [new ModuleTypeDefinition(
            "module.engine.basic", "Engine", 1, 5000, 100, 0,
            [NavigationComputerCommandTypes.Approach], BaseCycleTimeMs: 1000,
            MaxSpeedMps: 4000, TurnStepDegrees: 4, LinearInertiaMps2: 40000,
            AngularInertiaDegPerSec: 4)],
        Items(),
        [new CommandDefinition(
            NavigationComputerCommandTypes.Approach, "Approach", TimeFactor: 1000,
            Target: "object", Type: "module.engine.basic", TrailDistanceKm: 200)],
        stationMarketProfiles: Profiles());

    private static IReadOnlyList<ShipModuleData> NavigationModules() =>
    [new(
        "MOD-ENGINE-01", "module.engine.basic", [new HullCellCoordinate(1, 1)],
        100, "On", "Ready", null, [new CargoStackData("item.ore", 3)])];

    private static ItemTypeDefinition[] Items() =>
    [
        new("item.ore", "Ore", 1, 10),
        new("item.steel", "Steel", 1, 10),
        new("item.food", "Food", 1, 10),
        new("item.science", "Science", 1, 10),
        new("item.fuel", "Fuel", 0, 10, TradeUnit: TradeUnit.Kilogram, StorageKind: ItemStorageKind.FuelTank),
    ];

    private static StationMarketProfileDefinition[] Profiles()
    {
        var factors = new Dictionary<StationSize, int>
        {
            [StationSize.Outpost] = 500,
            [StationSize.Medium] = 1000,
            [StationSize.Large] = 1500,
            [StationSize.Huge] = 2000,
        }.ToImmutableDictionary();

        return
        [
            Profile("market.transit", [], ["item.ore"], factors),
            Profile("market.mining", ["item.ore"], ["item.steel"], factors),
            Profile("market.industrial", ["item.steel"], ["item.ore", "item.food", "item.science"], factors),
            Profile("market.hydroponic", ["item.food"], ["item.steel"], factors),
            Profile("market.scientific-military", ["item.science"], ["item.food"], factors),
        ];
    }

    private static StationMarketProfileDefinition Profile(
        string id,
        IReadOnlyList<string> supply,
        IReadOnlyList<string> demand,
        ImmutableDictionary<StationSize, int> factors) =>
        new(id, id, supply.ToImmutableArray(), demand.ToImmutableArray(),
            supply.Concat(demand).Distinct(StringComparer.Ordinal)
                .Select(item => new StationMarketStockDefinition(item, 1)).ToImmutableArray(),
            1000, 100, factors);
}
