using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingMapGeometryTests
{
    [Fact]
    public void Geometry_is_deterministic_for_same_seed_and_reordered_inputs()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 77, Registry());
        var existing = ExistingObjects();

        var first = TradingMapGeometryGenerator.Generate(graph, existing, 77);
        var second = TradingMapGeometryGenerator.Generate(graph, existing.AsEnumerable().Reverse().ToArray(), 77);

        Assert.Equal(first.State.SchemaVersion, second.State.SchemaVersion);
        Assert.Equal(first.State.TemplateId, second.State.TemplateId);
        Assert.Equal(first.State.QuarterTurns, second.State.QuarterTurns);
        Assert.Equal(first.State.Edges, second.State.Edges);
        Assert.Equal(first.State.CargoFlows, second.State.CargoFlows);
        Assert.Equal(first.State.RngStreams, second.State.RngStreams);
        Assert.Equal(first.Stations, second.Stations);
        Assert.Equal(first.GeometryStream, second.GeometryStream);
        Assert.Equal(existing.Single(obj => obj.ObjectId == "SPC-0002"),
            first.Stations.Single(station => station.ObjectId == "SPC-0002"));
    }

    [Fact]
    public void Stations_respect_minimum_separation_and_asteroid_clearance()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 77, Registry());
        var result = TradingMapGeometryGenerator.Generate(graph, ExistingObjects(), 77);

        foreach (var left in result.Stations)
            foreach (var right in result.Stations.Where(station =>
                         string.CompareOrdinal(left.ObjectId, station.ObjectId) < 0))
            {
                Assert.True(DistanceKm(left, right) >= Rules().MinStationDistanceKm);
            }

        foreach (var station in result.Stations)
            foreach (var asteroid in ExistingObjects().Where(obj => obj.ObjectType == "Asteroid"))
                Assert.True(DistanceKm(station, asteroid) >= Rules().ClearanceKm);
    }

    [Fact]
    public void Edges_have_distance_time_class_fuel_and_risk_metadata()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 77, Registry());
        var result = TradingMapGeometryGenerator.Generate(graph, ExistingObjects(), 77);
        var positions = result.Stations.ToDictionary(station => station.ObjectId);

        Assert.Equal(graph.Template.Links.Count, result.State.Edges.Count);
        Assert.All(result.State.Edges, edge =>
        {
            var distance = DistanceKm(positions[edge.FromStationObjectId], positions[edge.ToStationObjectId]);
            var expectedTime = (long)Math.Ceiling(distance / 0.7 * 1000 * 300);
            Assert.Equal(distance, edge.DistanceKm, precision: 12);
            Assert.Equal(expectedTime, edge.TravelEstimateGameTimeMs);
            Assert.Equal(Rules().RiskProfiles.Single(risk => risk.RiskProfileId == edge.RiskProfileId).FuelMultiplierPermille,
                edge.FuelMultiplierPermille);
        });
    }

    [Fact]
    public void Classes_cover_short_medium_long_and_science_is_long()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 77, Registry());
        var result = TradingMapGeometryGenerator.Generate(graph, ExistingObjects(), 77);

        Assert.Contains(result.State.Edges, edge => edge.DistanceClass == "Short");
        Assert.Contains(result.State.Edges, edge => edge.DistanceClass == "Medium");
        Assert.Contains(result.State.Edges, edge => edge.DistanceClass == "Long");
        Assert.Contains(result.State.Edges, edge => edge.DistanceClass == "Long" &&
            (edge.FromStationObjectId == "SPC-0008" || edge.ToStationObjectId == "SPC-0008"));
        Assert.Contains(result.State.Edges, edge => edge.DistanceClass == "Short" &&
            edge.RiskProfileId == "risk.safe" &&
            (edge.FromStationObjectId == "SPC-0002" || edge.ToStationObjectId == "SPC-0002"));
    }

    [Fact]
    public void Invalid_or_impossible_geometry_is_rejected_without_partial_result()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 77, Registry());
        var impossible = Rules() with { MinStationDistanceKm = 250 };
        var impossibleGraph = graph with { Rules = impossible };

        var error = Assert.Throws<ScenarioException>(() =>
            TradingMapGeometryGenerator.Generate(impossibleGraph, ExistingObjects(), 77));

        Assert.Contains("station", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("distance", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, 0, "Stationary")]
    [InlineData(true, 1, "Linear")]
    [InlineData(true, 0, "Linear")]
    public void Start_station_must_be_known_and_stationary(bool isKnown, double speedMps, string movementType)
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 77, Registry());
        var existing = ExistingObjects().Select(obj => obj.ObjectId == "SPC-0002"
            ? obj with { IsKnown = isKnown, SpeedMps = speedMps, MovementType = movementType }
            : obj).ToArray();

        var error = Assert.Throws<ScenarioException>(() =>
            TradingMapGeometryGenerator.Generate(graph, existing, 77));

        Assert.Contains("start station", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stationary", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Geometry_stream_is_independent_from_topology_and_unrelated_rng()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 123, Registry());
        var expectedSeed = RngStreamSeedDerivation.DeriveStreamSeed(123, "TradingMap.Geometry");
        var random = RngStreamNames.CreateDeterministicRandom(expectedSeed);
        for (var i = 0; i < 1000; i++)
            _ = random.NextDouble();
        var expectedQuarterTurns = (int)Math.Floor(random.NextDouble() * 4);

        var unrelated = RngStreamNames.CreateDeterministicRandom(
            RngStreamSeedDerivation.DeriveStreamSeed(123, "StationInventory:unrelated:item.ore"));
        for (var i = 0; i < 41; i++)
            _ = unrelated.NextDouble();

        var result = TradingMapGeometryGenerator.Generate(graph, ExistingObjects(), 123);

        Assert.Equal(expectedQuarterTurns, result.State.QuarterTurns);
        Assert.Equal(new TradingMapRngData("TradingMap.Geometry", expectedSeed, 10010), result.GeometryStream);
        Assert.Equal(graph.TopologyStream, result.State.RngStreams.Single(stream => stream.Name == "TradingMap.Topology"));
    }

    [Fact]
    public void Geometry_rng_replay_has_golden_seed_quarter_turns_and_counter()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 123, Registry());
        var result = TradingMapGeometryGenerator.Generate(graph, ExistingObjects(), 123);

        Assert.Equal(17895960153916692429UL, result.GeometryStream.Seed);
        Assert.Equal("TradingMap.Geometry", result.GeometryStream.Name);
        Assert.Equal(1, result.State.QuarterTurns);
        Assert.Equal(10010UL, result.GeometryStream.Counter);
        Assert.Equal(result.GeometryStream,
            result.State.RngStreams.Single(stream => stream.Name == "TradingMap.Geometry"));
    }

    [Fact]
    public void Offsets_are_rotated_from_start_offset_in_world_units()
    {
        var graph = TradingGraphGenerator.Generate(Rules(), 0, Registry());
        var expectedByQuarterTurn = new Dictionary<int, (double X, double Y)>
        {
            [0] = (11100, 11000),
            [1] = (11000, 11100),
            [2] = (10900, 11000),
            [3] = (11000, 10900),
        };

        foreach (var quarterTurns in expectedByQuarterTurn)
        {
            var seed = Enumerable.Range(0, 256)
                .Select(value => (ulong)value)
                .First(value => TradingMapRandom.DrawIndex(value, "TradingMap.Geometry", 4).Value == quarterTurns.Key);
            var result = TradingMapGeometryGenerator.Generate(graph, ExistingObjects(), seed);
            var station = result.Stations.Single(value => value.ObjectId == "SPC-0005");
            Assert.Equal(quarterTurns.Key, result.State.QuarterTurns);
            Assert.Equal(quarterTurns.Value.X, station.PositionX);
            Assert.Equal(quarterTurns.Value.Y, station.PositionY);
            Assert.Equal(10, DistanceKm(result.Stations.Single(value => value.ObjectId == "SPC-0002"), station));
        }
    }

    [Fact]
    public void Distance_classes_use_inclusive_boundaries_and_configured_reference_speed()
    {
        const double referenceSpeedMps = 350;
        var shortBoundary = (long)Math.Ceiling(10 / (referenceSpeedMps / 1000) * 1000 * 300);
        var mediumDistanceKm = Math.Sqrt(100 * 100 + 1000 * 1000) * 0.1;
        var mediumBoundary = (long)Math.Ceiling(mediumDistanceKm / (referenceSpeedMps / 1000) * 1000 * 300);
        var rules = Rules() with
        {
            ReferenceSpeedMps = referenceSpeedMps,
            ShortMaxGameTimeMs = shortBoundary,
            MediumMaxGameTimeMs = mediumBoundary,
            MaxTravelGameTimeMs = 200_000_000,
        };

        var graph = TradingGraphGenerator.Generate(rules, 77, Registry());
        var result = TradingMapGeometryGenerator.Generate(graph, ExistingObjects(), 77);
        var shortEdge = result.State.Edges.Single(edge =>
            edge.FromStationObjectId == "SPC-0002" && edge.ToStationObjectId == "SPC-0005");
        var mediumEdge = result.State.Edges.Single(edge =>
            edge.FromStationObjectId == "SPC-0005" && edge.ToStationObjectId == "SPC-0006");

        Assert.Equal(shortBoundary, shortEdge.TravelEstimateGameTimeMs);
        Assert.Equal("Short", shortEdge.DistanceClass);
        Assert.Equal(mediumBoundary, mediumEdge.TravelEstimateGameTimeMs);
        Assert.Equal("Medium", mediumEdge.DistanceClass);
        Assert.All(result.State.Edges, edge => Assert.True(edge.TravelEstimateGameTimeMs <= rules.MaxTravelGameTimeMs));
    }

    [Fact]
    public void Reordered_stations_templates_links_offsets_and_objects_are_canonical()
    {
        var rules = Rules();
        var shuffledRules = rules with
        {
            Stations = rules.Stations.Reverse().ToArray(),
            Templates = rules.Templates.Reverse().Select(template => template with
            {
                Links = template.Links.Reverse().Select(link => link with
                {
                    FromStationObjectId = link.ToStationObjectId,
                    ToStationObjectId = link.FromStationObjectId,
                }).ToArray(),
                Offsets = template.Offsets.Reverse().ToArray(),
            }).ToArray(),
            RiskProfiles = rules.RiskProfiles.AsEnumerable().Reverse().ToArray(),
        };

        var first = TradingMapGeometryGenerator.Generate(
            TradingGraphGenerator.Generate(rules, 77, Registry()), ExistingObjects(), 77);
        var second = TradingMapGeometryGenerator.Generate(
            TradingGraphGenerator.Generate(shuffledRules, 77, Registry(Profiles().AsEnumerable().Reverse().ToArray())),
            ExistingObjects().AsEnumerable().Reverse().ToArray(), 77);

        Assert.Equal(first.Stations.Select(StationShape), second.Stations.Select(StationShape));
        Assert.Equal(first.State.Edges, second.State.Edges);
        Assert.Equal(first.State.CargoFlows.Select(FlowShape), second.State.CargoFlows.Select(FlowShape));
        Assert.All(first.State.Edges, edge =>
            Assert.True(string.CompareOrdinal(edge.FromStationObjectId, edge.ToStationObjectId) < 0));
    }

    private static TradingMapGenerationData Rules() => new(
        1, "SPC-0002", 700, 21_600_000, 64_800_000, 129_600_000, 5, 2,
        [
            new("SPC-0002", "market.transit", "Transit", "Large"),
            new("SPC-0005", "market.mining", "Mining", "Medium"),
            new("SPC-0006", "market.industrial", "Industrial", "Medium"),
            new("SPC-0007", "market.hydroponic", "Hydroponic", "Medium"),
            new("SPC-0008", "market.scientific-military", "Scientific/Military", "Medium"),
        ],
        [new("seeded", [
            Link("SPC-0002", "SPC-0005", "risk.safe"),
            Link("SPC-0005", "SPC-0006", "risk.normal"),
            Link("SPC-0006", "SPC-0007", "risk.normal"),
            Link("SPC-0006", "SPC-0008", "risk.elevated"),
            Link("SPC-0007", "SPC-0008", "risk.normal"),
            Link("SPC-0008", "SPC-0002", "risk.elevated"),
            Link("SPC-0005", "SPC-0008", "risk.normal"),
        ], [
            new("SPC-0002", 0, 0),
            new("SPC-0005", 100, 0),
            new("SPC-0006", 0, 1000),
            new("SPC-0007", -1000, 0),
            new("SPC-0008", 0, 1500),
        ])],
        [new("risk.elevated", 1250), new("risk.normal", 1100), new("risk.safe", 1000)]);

    private static TradingMapLinkData Link(string from, string to, string risk) => new(from, to, risk);

    private static SpaceObjectData[] ExistingObjects() =>
    [
        new("SPC-0001", "PlayerShip", "Permanent", "Player Ship", 10000, 10000, 700, 0, "Linear", null, null, null),
        new("SPC-0002", "Station", "Permanent", "Start Station", 11000, 11000, 0, 0, "Stationary", null, null, null,
            IsKnown: true, Inventory: [new StationInventoryItemData("item.ore", 2)],
            MarketProfileId: "market.transit", StationSize: "Large"),
        new("AST-0001", "Asteroid", "Temporary", null, 10400, 10000, 600, 256, "Linear", 1_000_000, "Silicate", null),
        new("AST-0002", "Asteroid", "Temporary", null, 10000, 10450, 1200, 22, "Linear", 1_000_000, "Ice", null),
    ];

    private static GameDataRegistry Registry(params StationMarketProfileDefinition[] customProfiles) =>
        GameDataRegistry.Create([], [], Items(), [],
            stationMarketProfiles: customProfiles.Length == 0 ? Profiles() : customProfiles);

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

    private static double DistanceKm(SpaceObjectData left, SpaceObjectData right) =>
        Math.Sqrt(Math.Pow(right.PositionX - left.PositionX, 2) + Math.Pow(right.PositionY - left.PositionY, 2)) * 0.1;

    private static string StationShape(SpaceObjectData station) =>
        $"{station.ObjectId}|{station.Name}|{station.PositionX:R}|{station.PositionY:R}|" +
        $"{station.MarketProfileId}|{station.StationSize}|{station.IsKnown}";

    private static string FlowShape(TradingMapCargoFlowData flow) =>
        $"{flow.FromStationObjectId}>{flow.ToStationObjectId}:{string.Join(',', flow.ItemTypeIds)}";
}
