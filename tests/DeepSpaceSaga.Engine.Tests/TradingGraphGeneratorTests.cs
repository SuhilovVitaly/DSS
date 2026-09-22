using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingGraphGeneratorTests
{
    [Fact]
    public void Every_template_has_five_roles_degree_two_two_cycles_and_no_bridges()
    {
        var rules = Rules();
        var registry = Registry();

        foreach (var template in rules.Templates)
        {
            var flows = TradingGraphGenerator.ValidateTemplate(rules, template, registry);
            Assert.NotNull(flows);
            Assert.Equal(5, rules.Stations.Count);
            Assert.True(template.Links.Count - rules.Stations.Count + 1 >= 2);
            Assert.All(rules.Stations, station =>
                Assert.True(template.Links.Count(link =>
                    link.FromStationObjectId == station.ObjectId || link.ToStationObjectId == station.ObjectId) >= 2));

            foreach (var removed in template.Links)
            {
                var adjacency = rules.Stations.ToDictionary(station => station.ObjectId,
                    _ => new HashSet<string>(StringComparer.Ordinal));
                foreach (var link in template.Links.Where(link => link != removed))
                {
                    adjacency[link.FromStationObjectId].Add(link.ToStationObjectId);
                    adjacency[link.ToStationObjectId].Add(link.FromStationObjectId);
                }
                Assert.Equal(rules.Stations.Count, Reachable(adjacency, rules.Stations[0].ObjectId));
            }
        }
    }

    [Fact]
    public void Every_producer_has_consumer_and_a_return_cargo_pair()
    {
        var rules = Rules();
        var registry = Registry();
        var plan = TradingGraphGenerator.Generate(rules, 42, registry);
        var profiles = Profiles().ToDictionary(profile => profile.TypeId, StringComparer.Ordinal);
        var stations = rules.Stations.ToDictionary(station => station.ObjectId, StringComparer.Ordinal);

        foreach (var station in stations.Values)
        {
            var profile = profiles[station.MarketProfileId];
            var outgoing = plan.CargoFlows.Where(flow => flow.FromStationObjectId == station.ObjectId).ToArray();
            Assert.True(profile.SupplyItemTypeIds.Length == 0 || outgoing.Length > 0, station.ObjectId);
            foreach (var flow in outgoing)
            {
                var expected = profile.SupplyItemTypeIds
                    .Intersect(profiles[stations[flow.ToStationObjectId].MarketProfileId].DemandItemTypeIds,
                        StringComparer.Ordinal)
                    .Where(item => item != "item.fuel")
                    .OrderBy(item => item, StringComparer.Ordinal);
                Assert.Equal(expected, flow.ItemTypeIds);
            }
        }

        Assert.Contains(plan.CargoFlows, flow => plan.CargoFlows.Any(reverse =>
            reverse.FromStationObjectId == flow.ToStationObjectId &&
            reverse.ToStationObjectId == flow.FromStationObjectId));
        Assert.Contains(plan.CargoFlows, flow => flow.ToStationObjectId != rules.StartStationObjectId);
    }

    [Fact]
    public void Simple_ring_without_chord_is_rejected()
    {
        var rules = Rules() with
        {
            Templates = [Template("ring", [
                Link("ST-IND", "ST-MINE"), Link("ST-MINE", "ST-TRANSIT"),
                Link("ST-TRANSIT", "ST-SCI"), Link("ST-SCI", "ST-HYDROPONIC"),
                Link("ST-HYDROPONIC", "ST-IND")])]
        };

        var error = Assert.Throws<ScenarioException>(() =>
            TradingGraphGenerator.Generate(rules, 1, Registry()));
        Assert.Contains("cycle", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Disconnected_duplicate_self_loop_and_missing_required_role_links_are_rejected()
    {
        var registry = Registry();
        Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(
            Rules() with { Templates = [Template("disconnected", [Link("ST-IND", "ST-MINE"), Link("ST-IND", "ST-HYDROPONIC"), Link("ST-IND", "ST-SCI")])] },
            1, registry));
        Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(
            Rules() with { Templates = [Template("self", Rules().Templates[0].Links.Append(Link("ST-MINE", "ST-MINE")).ToArray())] },
            1, registry));
        Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(
            Rules() with { Templates = [Template("duplicate", Rules().Templates[0].Links.Append(Link("ST-MINE", "ST-IND")).ToArray())] },
            1, registry));
        Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(
            Rules() with
            {
                Templates = [Template("missing-role", Rules().Templates[0].Links.Where(link =>
                !(link.FromStationObjectId == "ST-IND" && link.ToStationObjectId == "ST-SCI")).ToArray())]
            },
            1, registry));
    }

    [Fact]
    public void Missing_consumer_and_missing_return_cargo_are_rejected()
    {
        var noFlowsRegistry = Registry(Profiles().Select(profile => profile with
        {
            SupplyItemTypeIds = [],
            DemandItemTypeIds = ["item.ore"],
            InitialInventory = [new StationMarketStockDefinition("item.ore", 1)],
        }).ToArray());
        Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(Rules(), 1, noFlowsRegistry));

        var oneWayRegistry = Registry(Profiles().Select(profile => profile with
        {
            SupplyItemTypeIds = profile.TypeId == "market.mining" ? ["item.ore"] : [],
            DemandItemTypeIds = profile.TypeId == "market.industrial" ? ["item.ore"] : ["item.steel"],
            InitialInventory = (profile.TypeId == "market.mining"
                    ? new[] { "item.ore", "item.steel" }
                    : new[] { profile.TypeId == "market.industrial" ? "item.ore" : "item.steel" })
                .Select(item => new StationMarketStockDefinition(item, 1)).ToImmutableArray(),
        }).ToArray());
        Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(Rules(), 1, oneWayRegistry));
    }

    [Fact]
    public void Generate_rejects_unselected_invalid_template_before_topology_selection()
    {
        var valid = Rules().Templates[0];
        var invalid = valid with
        {
            TemplateId = "z-invalid",
            Links = valid.Links.Where(link =>
                !(link.FromStationObjectId == "ST-IND" && link.ToStationObjectId == "ST-SCI")).ToArray(),
        };
        var rules = Rules() with { Templates = [valid, invalid] };
        var seed = Enumerable.Range(0, 256)
            .Select(value => (Seed: (ulong)value, Selection: TradingMapRandom.DrawIndex((ulong)value, "TradingMap.Topology", 2).Value))
            .First(value => value.Selection == 0)
            .Seed;

        // The sorted valid template is index 0 for this seed, but Generate must still
        // validate the second candidate before it consumes/selects the topology stream.
        Assert.Equal(0, TradingMapRandom.DrawIndex(seed, "TradingMap.Topology", 2).Value);
        var error = Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(rules, seed, Registry()));
        Assert.Contains("z-invalid", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_profile_or_item_is_contextual_error()
    {
        var profileError = Assert.Throws<ScenarioException>(() => TradingGraphGenerator.Generate(
            Rules() with
            {
                Stations = Rules().Stations.Select(station => station.ObjectId == "ST-MINE"
                    ? station with { MarketProfileId = "market.unknown" }
                    : station).ToArray()
            },
            1, Registry()));
        Assert.Contains("market.unknown", profileError.Message, StringComparison.Ordinal);
        Assert.Contains("ST-MINE", profileError.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Same_seed_reordered_input_and_unrelated_rng_have_identical_plan()
    {
        var rules = Rules();
        var shuffled = rules with
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
            RiskProfiles = rules.RiskProfiles.Reverse().ToArray(),
        };
        var shuffledProfiles = Enumerable.Reverse(Profiles()).Select(ShuffleProfile).ToArray();
        ConsumeUnrelatedNamedStream(77);

        var first = TradingGraphGenerator.Generate(rules, 77, Registry());
        var second = TradingGraphGenerator.Generate(shuffled, 77, Registry(shuffledProfiles));
        Assert.Equal(first.Rules.Stations, second.Rules.Stations);
        Assert.Equal(first.Rules.Templates.Select(TemplateShape), second.Rules.Templates.Select(TemplateShape));
        Assert.Equal(first.Rules.RiskProfiles, second.Rules.RiskProfiles);
        Assert.Equal(first.Template.TemplateId, second.Template.TemplateId);
        Assert.Equal(first.Template.Links, second.Template.Links);
        Assert.Equal(first.Template.Offsets, second.Template.Offsets);
        Assert.Equal(first.CargoFlows.Select(FlowShape), second.CargoFlows.Select(FlowShape));
        Assert.Equal(first.TopologyStream, second.TopologyStream);
    }

    [Fact]
    public void Seed_corpus_selects_multiple_templates()
    {
        var rules = Rules() with
        {
            Templates =
            [
                Rules().Templates[0],
                Rules().Templates[0] with { TemplateId = "ring-b", Offsets = Rules().Templates[0].Offsets.Select(offset => offset with { X = offset.X + 1 }).ToArray() },
            ]
        };
        var selected = Enumerable.Range(0, 256)
            .Select(seed => TradingGraphGenerator.Generate(rules, (ulong)seed, Registry()).Template.TemplateId)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(2, selected.Count);
    }

    [Fact]
    public void Topology_stream_seed_and_counter_are_replayable()
    {
        var result = TradingMapRandom.DrawIndex(123, "TradingMap.Topology", 7);
        var seed = RngStreamSeedDerivation.DeriveStreamSeed(123, "TradingMap.Topology");
        var random = RngStreamNames.CreateDeterministicRandom(seed);
        for (var i = 0; i < 1000; i++)
            _ = random.NextDouble();
        var expected = (int)Math.Floor(random.NextDouble() * 7);

        Assert.Equal(expected, result.Value);
        Assert.Equal("TradingMap.Topology", result.State.Name);
        Assert.Equal(seed, result.State.Seed);
        Assert.Equal(10010UL, result.State.Counter);
        Assert.Throws<ArgumentOutOfRangeException>(() => TradingMapRandom.DrawIndex(1, "x", 0));
    }

    [Fact]
    public void Topology_stream_is_independent_of_unrelated_named_stream_consumption()
    {
        const ulong masterSeed = 123;
        const int exclusiveMax = 7;
        var expected = ReplayTopologyIndex(masterSeed, exclusiveMax);

        ConsumeUnrelatedNamedStream(masterSeed);
        var first = TradingMapRandom.DrawIndex(masterSeed, "TradingMap.Topology", exclusiveMax);
        ConsumeUnrelatedNamedStream(masterSeed);
        var replay = TradingMapRandom.DrawIndex(masterSeed, "TradingMap.Topology", exclusiveMax);

        Assert.Equal(expected, first.Value);
        Assert.Equal(first.Value, replay.Value);
        Assert.Equal(first.State, replay.State);
    }

    private static TradingMapGenerationData Rules() => new(
        1, "ST-TRANSIT", 10, 100, 200, 300, 20, 5,
        [
            new("ST-TRANSIT", "market.transit", "Transit", "Large"),
            new("ST-MINE", "market.mining", "Mining", "Medium"),
            new("ST-IND", "market.industrial", "Industrial", "Medium"),
            new("ST-HYDROPONIC", "market.hydroponic", "Hydroponic", "Medium"),
            new("ST-SCI", "market.scientific-military", "Scientific", "Medium"),
        ],
        [Template("ring-a", [
            Link("ST-TRANSIT", "ST-MINE"), Link("ST-MINE", "ST-IND"), Link("ST-IND", "ST-HYDROPONIC"),
            Link("ST-HYDROPONIC", "ST-SCI"), Link("ST-SCI", "ST-TRANSIT"), Link("ST-IND", "ST-SCI")])],
        [new("risk.normal", 1000)]);

    private static TradingMapTemplateData Template(string id, IReadOnlyList<TradingMapLinkData> links) =>
        new(id, links, StationIds.Select((stationId, index) =>
            new TradingMapOffsetData(stationId, index == 0 ? 0 : index * 10, index == 0 ? 0 : -index * 10)).ToArray());

    private static readonly string[] StationIds =
        ["ST-TRANSIT", "ST-MINE", "ST-IND", "ST-HYDROPONIC", "ST-SCI"];

    private static TradingMapLinkData Link(string from, string to) => new(from, to, "risk.normal");

    private static GameDataRegistry Registry(params StationMarketProfileDefinition[] customProfiles) =>
        GameDataRegistry.Create([], [], Items(), [], stationMarketProfiles: customProfiles.Length == 0 ? Profiles() : customProfiles);

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

    private static StationMarketProfileDefinition ShuffleProfile(StationMarketProfileDefinition profile) => profile with
    {
        SupplyItemTypeIds = profile.SupplyItemTypeIds.Reverse().ToImmutableArray(),
        DemandItemTypeIds = profile.DemandItemTypeIds.Reverse().ToImmutableArray(),
    };

    private static StationMarketProfileDefinition Profile(
        string id,
        IReadOnlyList<string> supply,
        IReadOnlyList<string> demand,
        ImmutableDictionary<StationSize, int> factors) =>
        new(id, id, supply.ToImmutableArray(), demand.ToImmutableArray(),
            supply.Concat(demand).Distinct(StringComparer.Ordinal)
                .Select(item => new StationMarketStockDefinition(item, 1)).ToImmutableArray(),
            1000, 100, factors);

    private static int Reachable(IReadOnlyDictionary<string, HashSet<string>> adjacency, string start)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { start };
        var queue = new Queue<string>([start]);
        while (queue.Count > 0)
        {
            foreach (var next in adjacency[queue.Dequeue()])
                if (visited.Add(next)) queue.Enqueue(next);
        }
        return visited.Count;
    }

    private static string TemplateShape(TradingMapTemplateData template) =>
        $"{template.TemplateId}|{string.Join(';', template.Links.Select(link => $"{link.FromStationObjectId}>{link.ToStationObjectId}:{link.RiskProfileId}"))}|" +
        string.Join(';', template.Offsets.Select(offset => $"{offset.StationObjectId}:{offset.X}:{offset.Y}"));

    private static string FlowShape(TradingMapCargoFlowData flow) =>
        $"{flow.FromStationObjectId}>{flow.ToStationObjectId}:{string.Join(',', flow.ItemTypeIds)}";

    private static void ConsumeUnrelatedNamedStream(ulong masterSeed)
    {
        var random = RngStreamNames.CreateDeterministicRandom(
            RngStreamSeedDerivation.DeriveStreamSeed(
                masterSeed,
                RngStreamNames.StationInventory("ST-MINE", "item.ore")));
        for (var i = 0; i < 37; i++)
            _ = random.NextDouble();
    }

    private static int ReplayTopologyIndex(ulong masterSeed, int exclusiveMax)
    {
        var seed = RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, "TradingMap.Topology");
        var random = RngStreamNames.CreateDeterministicRandom(seed);
        for (var i = 0; i < 1000; i++)
            _ = random.NextDouble();
        return (int)Math.Floor(random.NextDouble() * exclusiveMax);
    }
}
