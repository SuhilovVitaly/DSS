using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0001-US-0002-TK-0004: the five shipped market profiles carry the epic's baseline hourly
/// flows and stock targets, the demo stations change on the whole-hour boundary through the
/// public station-travel command, and both dictionaries are ready for the market-state UI.
/// Expected numbers are written out from the ticket tables, never derived from the Engine.
/// </summary>
public sealed class MarketFlowContentTests
{
    private const string PlayerId = "SPC-MARKET-PLAYER";
    private const string EngineModuleId = "MOD-MARKET-ENGINE";

    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string ClientRoot = Path.Combine(RepoRoot, "src", "DeepSpaceSaga.Client");
    private static readonly string SettingsPath = Path.Combine(ClientRoot, "Settings.json");
    private static readonly string DemoScenarioPath = Path.Combine(
        ClientRoot, "Scenarios", "MarketProfiles", "scenario.json");

    private static readonly ExpectedEconomy[] ExpectedEconomies =
    [
        new("market.mining",
            Outputs: [("item.ice", 18), ("item.iron-ore", 24), ("item.magnesium-ore", 10), ("item.carbon-ore", 8)],
            Inputs: [("item.water", 4), ("item.food-rations", 6), ("item.energy-cells", 8), ("item.steel", 2)],
            Consumption: [],
            Targets:
            [
                ("item.ice", 108), ("item.iron-ore", 108), ("item.magnesium-ore", 108), ("item.carbon-ore", 108),
                ("item.water", 72), ("item.food-rations", 72), ("item.energy-cells", 72), ("item.steel", 72),
            ]),
        new("market.industrial",
            Outputs: [("item.steel", 16), ("item.energy-cells", 12), ("item.electronics", 4)],
            Inputs:
            [
                ("item.iron-ore", 20), ("item.magnesium-ore", 8), ("item.carbon-ore", 6),
                ("item.silicon", 8), ("item.water", 2), ("item.food-rations", 3),
            ],
            Consumption: [],
            Targets:
            [
                ("item.iron-ore", 132), ("item.magnesium-ore", 132), ("item.carbon-ore", 132), ("item.silicon", 132),
                ("item.steel", 88), ("item.energy-cells", 88), ("item.electronics", 88),
                ("item.water", 88), ("item.food-rations", 88),
            ]),
        new("market.hydroponic",
            Outputs: [("item.water", 24), ("item.protein-mass", 10), ("item.food-rations", 8)],
            Inputs: [("item.ice", 20), ("item.energy-cells", 8), ("item.steel", 2), ("item.electronics", 1)],
            Consumption: [],
            Targets:
            [
                ("item.ice", 120), ("item.water", 80), ("item.protein-mass", 80), ("item.food-rations", 80),
                ("item.energy-cells", 80), ("item.steel", 80), ("item.electronics", 80),
            ]),
        new("market.transit",
            Outputs: [],
            Inputs: [],
            Consumption:
            [
                ("item.water", 8), ("item.food-rations", 12), ("item.energy-cells", 6),
                ("item.steel", 2), ("item.electronics", 2),
            ],
            Targets:
            [
                ("item.water", 96), ("item.food-rations", 96), ("item.energy-cells", 96),
                ("item.steel", 96), ("item.electronics", 96),
            ]),
        new("market.scientific-military",
            Outputs: [("item.electronics", 5)],
            Inputs:
            [
                ("item.silicon", 7), ("item.energy-cells", 6), ("item.steel", 3),
                ("item.water", 2), ("item.food-rations", 4),
            ],
            Consumption: [],
            Targets:
            [
                ("item.silicon", 96), ("item.electronics", 64), ("item.energy-cells", 64),
                ("item.steel", 64), ("item.water", 64), ("item.food-rations", 64),
            ]),
    ];

    /// <summary>Station id, its profile, and the full first-hour stock table (before → after) plus budget.</summary>
    public static TheoryData<string, string> DemoStations => new()
    {
        { "SPC-MARKET-MINING", "market.mining" },
        { "SPC-MARKET-INDUSTRIAL", "market.industrial" },
        { "SPC-MARKET-HYDROPONIC", "market.hydroponic" },
        { "SPC-MARKET-TRANSIT", "market.transit" },
        { "SPC-MARKET-SCIENTIFIC", "market.scientific-military" },
    };

    private static readonly Dictionary<string, FirstHour> ExpectedFirstHour = new(StringComparer.Ordinal)
    {
        ["SPC-MARKET-MINING"] = new(
            [
                ("item.ice", 162, 180), ("item.iron-ore", 162, 186), ("item.magnesium-ore", 162, 172),
                ("item.carbon-ore", 162, 170), ("item.water", 36, 32), ("item.food-rations", 36, 30),
                ("item.energy-cells", 36, 28), ("item.steel", 36, 34),
            ], BudgetBefore: 9600, BudgetAfter: 9633),
        ["SPC-MARKET-INDUSTRIAL"] = new(
            [
                ("item.steel", 132, 148), ("item.energy-cells", 132, 144), ("item.electronics", 132, 136),
                ("item.iron-ore", 66, 46), ("item.magnesium-ore", 66, 58), ("item.carbon-ore", 66, 60),
                ("item.silicon", 66, 58), ("item.water", 44, 42), ("item.food-rations", 44, 41),
            ], BudgetBefore: 14400, BudgetAfter: 14450),
        ["SPC-MARKET-HYDROPONIC"] = new(
            [
                ("item.water", 120, 144), ("item.protein-mass", 120, 130), ("item.food-rations", 120, 128),
                ("item.ice", 60, 40), ("item.energy-cells", 40, 32), ("item.steel", 40, 38),
                ("item.electronics", 40, 39),
            ], BudgetBefore: 12000, BudgetAfter: 12041),
        // Large demo Transit: 1.5x Medium stock and credits, no production, independent consumption.
        ["SPC-MARKET-TRANSIT"] = new(
            [
                ("item.water", 144, 136), ("item.food-rations", 144, 132), ("item.energy-cells", 144, 138),
                ("item.steel", 144, 142), ("item.electronics", 144, 142),
            ], BudgetBefore: 28800, BudgetAfter: 28900),
        ["SPC-MARKET-SCIENTIFIC"] = new(
            [
                ("item.electronics", 96, 101), ("item.silicon", 48, 41), ("item.energy-cells", 32, 26),
                ("item.steel", 32, 29), ("item.water", 32, 30), ("item.food-rations", 32, 28),
            ], BudgetBefore: 16800, BudgetAfter: 16858),
    };

    private static readonly (string Key, string English, string Russian)[] MarketKeys =
    [
        ("TradeUX.StockShortage", "Shortage", "Дефицит"),
        ("TradeUX.StockNormal", "Normal", "Норма"),
        ("TradeUX.StockSurplus", "Surplus", "Избыток"),
        ("TradeUX.MarketStockSummary", "{0} · target {1} · max {2}", "{0} · цель {1} · максимум {2}"),
        ("TradeUX.StationStorageLimit", "Limited by station storage capacity", "Ограничено свободным складом станции"),
        ("TradeUX.PartialWithRemaining",
            "{0}: {1} of {3} completed · {2} tokens · remaining {4}",
            "{0}: выполнено {1} из {3} · {2} токенов · остаток {4}"),
    ];

    [Fact]
    public void Shipping_market_rates_and_targets_match_epic_baseline()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);

        Assert.Equal(ExpectedEconomies.Length, registry.StationMarketProfiles.Count);
        foreach (var expected in ExpectedEconomies)
        {
            var profile = registry.StationMarketProfiles.GetDefinition(
                registry.StationMarketProfiles.GetIndex(expected.ProfileId));
            var economy = Assert.IsType<StationMarketEconomyDefinition>(profile.Economy);

            Assert.Equal(StationMarketProductionSource.Profile, economy.ProductionSource);
            AssertRates(expected.Outputs, economy.HourlyOutputs);
            AssertRates(expected.Inputs, economy.HourlyInputs);
            AssertRates(expected.Consumption, economy.HourlyConsumption);
            Assert.Equal(Sorted(expected.Targets), Sorted(economy.StockTargets
                .Select(target => (target.ItemTypeId, target.TargetStock))));
            Assert.Equal(500, economy.ShortageThresholdPermille);
            Assert.Equal(1500, economy.SurplusThresholdPermille);
            Assert.Equal(24, economy.BudgetRegenerationDivisorPerDay);

            var flowItems = economy.HourlyOutputs.Concat(economy.HourlyInputs).Concat(economy.HourlyConsumption)
                .Select(rate => rate.ItemTypeId)
                .Concat(economy.StockTargets.Select(target => target.ItemTypeId))
                .ToArray();
            Assert.DoesNotContain("item.fuel", flowItems);
            Assert.DoesNotContain("item.uranium-ore", flowItems);
        }
    }

    [Fact]
    public void Every_size_has_valid_stock_bounds_and_initial_budget()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        var expectedFactors = new Dictionary<StationSize, long>
        {
            [StationSize.Outpost] = 500,
            [StationSize.Medium] = 1000,
            [StationSize.Large] = 1500,
            [StationSize.Huge] = 2000,
        };

        foreach (var expected in ExpectedEconomies)
        {
            var profile = registry.StationMarketProfiles.GetDefinition(
                registry.StationMarketProfiles.GetIndex(expected.ProfileId));
            var rates = expected.Outputs.Concat(expected.Inputs).Concat(expected.Consumption).ToArray();

            foreach (var (size, factor) in expectedFactors)
            {
                Assert.Equal(factor, profile.SizeFactors[size]);
                foreach (var (itemId, target) in expected.Targets)
                {
                    // Every shipped target and initial stock divides evenly by 1000 at every factor,
                    // so the plain product is exact here (no rounding case is involved).
                    long scaledTarget = target * factor / 1000;
                    long maxStock = 2 * scaledTarget;
                    long initial = profile.InitialInventory.Single(stock => stock.ItemTypeId == itemId).Quantity;
                    Assert.True(scaledTarget > 0, $"{expected.ProfileId}/{size}/{itemId}: target is not positive");
                    Assert.True(initial * factor / 1000 <= maxStock,
                        $"{expected.ProfileId}/{size}/{itemId}: initial stock exceeds max {maxStock}");
                    foreach (var rate in rates.Where(rate => rate.ItemId == itemId))
                        Assert.True(rate.Quantity <= maxStock,
                            $"{expected.ProfileId}/{size}/{itemId}: hourly rate exceeds max {maxStock}");
                }

                long maxBudget = 2 * (profile.InitialCredits * factor / 1000);
                Assert.True(maxBudget / 24 > 0, $"{expected.ProfileId}/{size}: daily grant is zero");
            }
        }

        // Authoritative side of the same table: the Engine publishes exactly these bounds for each
        // profile at each size, and a New Game starts the budget at the size-scaled initial credits.
        foreach (var (stationId, profileId) in StationProfiles())
        {
            var expected = ExpectedEconomies.Single(economy => economy.ProfileId == profileId);
            foreach (var (size, factor) in expectedFactors)
            {
                using var engine = CreateDockedEngine(stationId, size.ToString());
                var trade = engine.CaptureSnapshot().DockedStationTrade;
                Assert.NotNull(trade);
                Assert.Equal(stationId, trade!.StationObjectId);
                foreach (var (itemId, target) in expected.Targets)
                {
                    var row = Assert.Single(trade.Items, item => item.ItemTypeId == itemId);
                    Assert.Equal(target * factor / 1000, row.TargetStock);
                    Assert.Equal(2 * target * factor / 1000, row.MaxStock);
                    Assert.InRange(row.StockQuantity, 0, row.MaxStock!.Value);
                    Assert.Equal(row.MaxStock - row.StockQuantity, row.FreeStockCapacity);
                    Assert.NotNull(row.StockState);
                }

                var fuel = Assert.Single(trade.Items, item => item.ItemTypeId == "item.fuel");
                Assert.Null(fuel.TargetStock);
                Assert.Null(fuel.StockState);

                var profile = registry.StationMarketProfiles.GetDefinition(
                    registry.StationMarketProfiles.GetIndex(profileId));
                var station = StationData(engine, stationId);
                Assert.Equal(profile.InitialCredits * factor / 1000, station.MarketBudgetCredits);
            }
        }
    }

    [Theory]
    [MemberData(nameof(DemoStations))]
    public async Task Public_station_travel_applies_one_hour_of_shipping_market_flow(
        string stationId, string profileId)
    {
        var expected = ExpectedFirstHour[stationId];
        var engine = CreateDockedEngine(stationId, stationSize: null);
        await using var connection = new LocalGameSessionConnection(engine);

        var before = engine.CaptureSnapshot();
        Assert.Equal(0, before.GameTimeMs);
        Assert.Equal(expected.BudgetBefore, StationData(engine, stationId).MarketBudgetCredits);
        foreach (var (itemId, stockBefore, _) in expected.Stocks)
            Assert.Equal(stockBefore, TradeRow(before, itemId).StockQuantity);
        long stationFuelBefore = TradeRow(before, "item.fuel").StockQuantity;
        long shipFuelBefore = ShipFuel(before);

        var command = new StationTravelCommand($"{stationId}-to-market", StationDistrict.Market);
        var travelled = await connection.TravelStationAsync(command);

        Assert.True(travelled.Accepted, travelled.Error);
        var after = travelled.Snapshot;
        Assert.Equal(GameCalendar.HourMs, after.GameTimeMs);
        Assert.Equal(stationId, after.DockedStationTrade?.StationObjectId);

        var targets = ExpectedEconomies.Single(economy => economy.ProfileId == profileId).Targets
            .ToDictionary(target => target.ItemId, target => target.Target, StringComparer.Ordinal);
        long factor = stationId == "SPC-MARKET-TRANSIT" ? 1500 : 1000;
        Assert.Equal(targets.Keys.Order(StringComparer.Ordinal),
            after.DockedStationTrade!.Items.Where(item => item.ItemTypeId != "item.fuel")
                .Select(item => item.ItemTypeId).Order(StringComparer.Ordinal));
        foreach (var (itemId, _, stockAfter) in expected.Stocks)
        {
            var row = TradeRow(after, itemId);
            Assert.Equal(stockAfter, row.StockQuantity);
            Assert.Equal(targets[itemId] * factor / 1000, row.TargetStock);
            Assert.Equal(2 * targets[itemId] * factor / 1000, row.MaxStock);
            Assert.InRange(row.StockQuantity, 0, row.MaxStock!.Value);
        }

        Assert.Equal(stationFuelBefore, TradeRow(after, "item.fuel").StockQuantity);
        Assert.Equal(shipFuelBefore, ShipFuel(after));
        Assert.Equal(expected.BudgetAfter, StationData(engine, stationId).MarketBudgetCredits);

        var repeated = await connection.TravelStationAsync(command);
        Assert.True(repeated.Accepted, repeated.Error);
        Assert.Equal(GameCalendar.HourMs, repeated.Snapshot.GameTimeMs);
        foreach (var (itemId, _, stockAfter) in expected.Stocks)
            Assert.Equal(stockAfter, TradeRow(repeated.Snapshot, itemId).StockQuantity);
        Assert.Equal(expected.BudgetAfter, StationData(engine, stationId).MarketBudgetCredits);
    }

    [Fact]
    public void Shipping_fuel_and_legacy_scenarios_have_no_hourly_cargo_flow()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        foreach (var expected in ExpectedEconomies)
        {
            var profile = registry.StationMarketProfiles.GetDefinition(
                registry.StationMarketProfiles.GetIndex(expected.ProfileId));
            Assert.DoesNotContain(profile.Economy!.StockTargets, target => target.ItemTypeId == "item.fuel");
        }

        foreach (string scenario in new[] { "Default", "Docked" })
        {
            var source = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", scenario, "scenario.json"));
            Assert.All(source.GameState.SpaceObjects, obj => Assert.Null(obj.MarketProfileId));
        }

        // The legacy docked New Game keeps its pre-economy trade rows across a public hour boundary.
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(
            SettingsPath, Path.Combine(ClientRoot, "Scenarios", "Docked", "scenario.json"));
        var travelled = engine.TravelStation(new StationTravelCommand("legacy-to-market", StationDistrict.Market));
        Assert.True(travelled.Accepted, travelled.Error);
        Assert.Equal(GameCalendar.HourMs, travelled.Snapshot.GameTimeMs);
        Assert.NotNull(travelled.Snapshot.DockedStationTrade);
        Assert.All(travelled.Snapshot.DockedStationTrade!.Items, row =>
        {
            Assert.Null(row.TargetStock);
            Assert.Null(row.MaxStock);
            Assert.Null(row.FreeStockCapacity);
            Assert.Null(row.StockState);
        });
        Assert.All(engine.CaptureSaveState().GameState.SpaceObjects,
            obj => Assert.Null(obj.MarketBudgetCredits));
    }

    [Fact]
    public void English_and_russian_market_keys_have_matching_arguments()
    {
        var english = Localization.LoadLocaleFile("English");
        var russian = Localization.LoadLocaleFile("Russian");
        Assert.NotNull(english);
        Assert.NotNull(russian);

        foreach (var (key, englishText, russianText) in MarketKeys)
        {
            Assert.Equal(englishText, english![key]);
            Assert.Equal(russianText, russian![key]);
        }

        Assert.Equal(["{0}", "{1}", "{2}"], Placeholders(english!["TradeUX.MarketStockSummary"]));
        Assert.Equal(["{0}", "{1}", "{2}", "{3}", "{4}"], Placeholders(english["TradeUX.PartialWithRemaining"]));
        // PartialWithRemaining extends PartialResult: its first four arguments keep their meaning.
        Assert.Equal(["{0}", "{1}", "{2}", "{3}"], Placeholders(english["TradeUX.PartialResult"]));

        foreach (string key in english.Keys.Where(key => key.StartsWith("TradeUX.", StringComparison.Ordinal)))
        {
            Assert.True(russian!.ContainsKey(key), $"Russian.json is missing '{key}'");
            Assert.Equal(Placeholders(english[key]), Placeholders(russian[key]));
        }
        Assert.Equal(english.Keys.Where(key => key.StartsWith("TradeUX.", StringComparison.Ordinal)).Order(),
            russian!.Keys.Where(key => key.StartsWith("TradeUX.", StringComparison.Ordinal)).Order());

        // Each authoritative band maps to "TradeUX.Stock<State>" in both dictionaries.
        foreach (var state in Enum.GetValues<StationMarketStockState>())
        {
            string key = $"TradeUX.Stock{state}";
            Assert.True(english.ContainsKey(key), $"English.json is missing '{key}'");
            Assert.True(russian.ContainsKey(key), $"Russian.json is missing '{key}'");
            Assert.Empty(Placeholders(english[key]));
            Assert.Empty(Placeholders(russian[key]));
        }
    }

    [Fact]
    public void Market_content_is_loaded_from_shipped_data()
    {
        string[] relativePaths =
        [
            Path.Combine("Data", "Markets", "station-market-profiles.json"),
            Path.Combine("Data", "Locale", "English.json"),
            Path.Combine("Data", "Locale", "Russian.json"),
        ];

        foreach (string relativePath in relativePaths)
        {
            string sourcePath = Path.Combine(ClientRoot, relativePath);
            string outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            Assert.True(File.Exists(outputPath), $"Missing shipped content: {outputPath}");
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(sourcePath)),
                JsonNode.Parse(File.ReadAllText(outputPath))), $"Shipped content differs: {relativePath}");
        }

        var shipped = EngineContentLoader.LoadRegistryFromSettingsFile(
            Path.Combine(AppContext.BaseDirectory, "Settings.json"), out _, out _);
        Assert.Equal(ExpectedEconomies.Select(economy => economy.ProfileId),
            Enumerable.Range(0, shipped.StationMarketProfiles.Count)
                .Select(shipped.StationMarketProfiles.GetDefinition)
                .Where(profile => profile.Economy is not null)
                .Select(profile => profile.TypeId));
    }

    private static IEnumerable<(string StationId, string ProfileId)> StationProfiles() =>
        DemoStations.Select(row => ((string)row[0], (string)row[1]));

    /// <summary>
    /// In-memory variation of the shipped demo: the player is already docked to the chosen station
    /// at GameTime 0 and Speed0, optionally with that station resized. The shipped scenario file is
    /// never written; the clock never advances on its own, so only public commands move time.
    /// </summary>
    private static SimulationEngine CreateDockedEngine(string stationId, string? stationSize)
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        var source = ScenarioLoader.LoadFromFile(DemoScenarioPath);
        var station = Assert.Single(source.GameState.SpaceObjects, obj => obj.ObjectId == stationId);
        var objects = source.GameState.SpaceObjects.Select(obj =>
        {
            if (obj.ObjectId == PlayerId)
                return obj with
                {
                    IsDocked = true,
                    DockedStationObjectId = stationId,
                    PositionX = station.PositionX,
                    PositionY = station.PositionY,
                    SpeedMps = 0,
                };
            if (obj.ObjectId == stationId && stationSize is not null)
                return obj with { StationSize = stationSize };
            return obj;
        }).ToArray();
        var docked = source with { GameState = source.GameState with { SpaceObjects = objects } };

        var engine = new SimulationEngine(registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(docked);
        return engine;
    }

    private static SpaceObjectData StationData(SimulationEngine engine, string stationId) =>
        Assert.Single(engine.CaptureSaveState().GameState.SpaceObjects, obj => obj.ObjectId == stationId);

    private static StationInventoryItemSnapshot TradeRow(AuthoritativeSnapshot snapshot, string itemTypeId) =>
        Assert.Single(snapshot.DockedStationTrade!.Items, item => item.ItemTypeId == itemTypeId);

    private static long ShipFuel(AuthoritativeSnapshot snapshot) =>
        Assert.Single(snapshot.InstalledModules, module => module.ModuleId == EngineModuleId).FuelAmountKg!.Value;

    private static void AssertRates(
        (string ItemId, long Quantity)[] expected, IEnumerable<StationMarketStockDefinition> actual) =>
        Assert.Equal(Sorted(expected), Sorted(actual.Select(rate => (rate.ItemTypeId, rate.Quantity))));

    private static (string, long)[] Sorted(IEnumerable<(string ItemId, long Value)> entries) =>
        entries.OrderBy(entry => entry.ItemId, StringComparer.Ordinal).ToArray();

    private static string[] Placeholders(string text) =>
        Regex.Matches(text, @"\{\d+\}").Select(match => match.Value).Distinct().Order(StringComparer.Ordinal).ToArray();

    private sealed record ExpectedEconomy(
        string ProfileId,
        (string ItemId, long Quantity)[] Outputs,
        (string ItemId, long Quantity)[] Inputs,
        (string ItemId, long Quantity)[] Consumption,
        (string ItemId, long Target)[] Targets);

    private sealed record FirstHour(
        (string ItemId, long Before, long After)[] Stocks,
        long BudgetBefore,
        long BudgetAfter);
}
