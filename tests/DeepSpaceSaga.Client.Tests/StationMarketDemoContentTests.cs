using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class StationMarketDemoContentTests
{
    private const string PlayerId = "SPC-MARKET-PLAYER";
    private const string NavigationModuleId = "MOD-MARKET-NAV";
    private const string CargoModuleId = "MOD-MARKET-CARGO";
    private const string EngineModuleId = "MOD-MARKET-ENGINE";

    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string ClientRoot = Path.Combine(RepoRoot, "src", "DeepSpaceSaga.Client");
    private static readonly string SettingsPath = Path.Combine(ClientRoot, "Settings.json");
    private static readonly string ProfilesPath = Path.Combine(
        ClientRoot, "Data", "Markets", "station-market-profiles.json");
    private static readonly string DemoScenarioPath = Path.Combine(
        ClientRoot, "Scenarios", "MarketProfiles", "scenario.json");

    private static readonly ExpectedProfile[] ExpectedProfiles =
    [
        new("market.mining", "Mining",
            ["item.ice", "item.iron-ore", "item.magnesium-ore", "item.carbon-ore"],
            ["item.water", "item.food-rations", "item.energy-cells", "item.steel"],
            [
                ("item.ice", 162), ("item.iron-ore", 162), ("item.magnesium-ore", 162),
                ("item.carbon-ore", 162), ("item.water", 36), ("item.food-rations", 36),
                ("item.energy-cells", 36), ("item.steel", 36),
            ], 9600, 200),
        new("market.industrial", "Industrial",
            ["item.steel", "item.energy-cells", "item.electronics"],
            [
                "item.iron-ore", "item.magnesium-ore", "item.carbon-ore", "item.silicon",
                "item.water", "item.food-rations",
            ],
            [
                ("item.steel", 132), ("item.energy-cells", 132), ("item.electronics", 132),
                ("item.iron-ore", 66), ("item.magnesium-ore", 66), ("item.carbon-ore", 66),
                ("item.silicon", 66), ("item.water", 44), ("item.food-rations", 44),
            ], 14400, 200),
        new("market.hydroponic", "Hydroponic",
            ["item.water", "item.protein-mass", "item.food-rations"],
            ["item.ice", "item.energy-cells", "item.steel", "item.electronics"],
            [
                ("item.water", 120), ("item.protein-mass", 120), ("item.food-rations", 120),
                ("item.ice", 60), ("item.energy-cells", 40), ("item.steel", 40),
                ("item.electronics", 40),
            ], 12000, 200),
        new("market.transit", "Transit", [],
            ["item.water", "item.food-rations", "item.energy-cells", "item.steel", "item.electronics"],
            [
                ("item.water", 96), ("item.food-rations", 96), ("item.energy-cells", 96),
                ("item.steel", 96), ("item.electronics", 96),
            ], 19200, 400),
        new("market.scientific-military", "Scientific/Military", ["item.electronics"],
            ["item.silicon", "item.energy-cells", "item.steel", "item.water", "item.food-rations"],
            [
                ("item.electronics", 96), ("item.silicon", 48), ("item.energy-cells", 32),
                ("item.steel", 32), ("item.water", 32), ("item.food-rations", 32),
            ], 16800, 200),
    ];

    public static TheoryData<string, string> DemoStations => new()
    {
        { "SPC-MARKET-MINING", "item.ice" },
        { "SPC-MARKET-INDUSTRIAL", "item.steel" },
        { "SPC-MARKET-HYDROPONIC", "item.water" },
        { "SPC-MARKET-TRANSIT", "item.water" },
        { "SPC-MARKET-SCIENTIFIC", "item.electronics" },
    };

    [Fact]
    public void Shipping_profiles_match_five_role_baselines()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);

        Assert.Equal(5, registry.StationMarketProfiles.Count);
        Assert.Equal(ExpectedProfiles.Select(profile => profile.Id), Enumerable.Range(0, 5)
            .Select(registry.StationMarketProfiles.GetDefinition).Select(profile => profile.TypeId));

        foreach (var expected in ExpectedProfiles)
        {
            var actual = registry.StationMarketProfiles.GetDefinition(
                registry.StationMarketProfiles.GetIndex(expected.Id));

            Assert.Equal(expected.DisplayName, actual.DisplayName);
            Assert.Equal(expected.Supply, actual.SupplyItemTypeIds);
            Assert.Equal(expected.Demand, actual.DemandItemTypeIds);
            Assert.Equal(expected.Inventory, actual.InitialInventory
                .Select(stock => (stock.ItemTypeId, stock.Quantity)));
            Assert.Equal(expected.InitialCredits, actual.InitialCredits);
            Assert.Equal(expected.RefuelStockKg, actual.RefuelStockKg);
            Assert.Equal(500, actual.SizeFactors[StationSize.Outpost]);
            Assert.Equal(1000, actual.SizeFactors[StationSize.Medium]);
            Assert.Equal(1500, actual.SizeFactors[StationSize.Large]);
            Assert.Equal(2000, actual.SizeFactors[StationSize.Huge]);
            Assert.DoesNotContain("item.fuel", actual.InitialInventory.Select(stock => stock.ItemTypeId));
            Assert.DoesNotContain("item.uranium-ore", actual.InitialInventory.Select(stock => stock.ItemTypeId));
        }
    }

    [Fact]
    public void Demo_assigns_five_profiles_and_is_deterministic()
    {
        var source = ScenarioLoader.LoadFromFile(DemoScenarioPath);
        Assert.Equal("market-profiles", source.Metadata.ScenarioId);
        Assert.Equal("Five Station Markets", source.Metadata.Name);
        Assert.Equal(20260921UL, source.GameState.MasterSeed);
        Assert.Equal(100000, source.GameState.PlayerTokens);

        var player = Assert.Single(source.GameState.SpaceObjects, obj => obj.ObjectId == PlayerId);
        Assert.Equal("Market Surveyor", player.Name);
        Assert.Equal((10000d, 10000d), (player.PositionX, player.PositionY));
        Assert.False(player.IsDocked);
        Assert.Null(player.DockedStationObjectId);
        Assert.Equal([(4, 0), (4, 1), (4, 2)],
            player.HullLayout!.Cells.Select(cell => (cell.X, cell.Y)));
        Assert.Equal([NavigationModuleId, CargoModuleId, EngineModuleId],
            player.Modules!.Select(module => module.ModuleId));

        var expectedAssignments = new Dictionary<string, (string ProfileId, string Size)>
        {
            ["SPC-MARKET-MINING"] = ("market.mining", "Medium"),
            ["SPC-MARKET-INDUSTRIAL"] = ("market.industrial", "Medium"),
            ["SPC-MARKET-HYDROPONIC"] = ("market.hydroponic", "Medium"),
            ["SPC-MARKET-TRANSIT"] = ("market.transit", "Large"),
            ["SPC-MARKET-SCIENTIFIC"] = ("market.scientific-military", "Medium"),
        };

        foreach (var assignment in expectedAssignments)
        {
            var station = Assert.Single(source.GameState.SpaceObjects,
                obj => obj.ObjectId == assignment.Key);
            Assert.Equal(assignment.Value.ProfileId, station.MarketProfileId);
            Assert.Equal(assignment.Value.Size, station.StationSize);
            Assert.Null(station.Credits);
            Assert.Null(station.Inventory);
            Assert.Equal(100, station.PortFeeCreditsPerDay);
        }

        var first = CaptureDemoState();
        var second = CaptureDemoState();
        Assert.Equal(ScenarioLoader.Serialize(first), ScenarioLoader.Serialize(second));

        foreach (var assignment in expectedAssignments)
        {
            var expected = Assert.Single(ExpectedProfiles,
                profile => profile.Id == assignment.Value.ProfileId);
            var station = Assert.Single(first.GameState.SpaceObjects,
                obj => obj.ObjectId == assignment.Key);
            Assert.NotNull(station.Inventory);
            var stationInventory = station.Inventory!;
            long factor = assignment.Value.Size == "Large" ? 1500 : 1000;
            var expectedInventory = expected.Inventory.ToDictionary(
                item => item.ItemId, item => Scale(item.Quantity, factor), StringComparer.Ordinal);
            expectedInventory.Add("item.fuel", Scale(expected.RefuelStockKg, factor));

            Assert.Equal(Scale(expected.InitialCredits, factor), station.Credits);
            Assert.Equal(expectedInventory.OrderBy(pair => pair.Key, StringComparer.Ordinal),
                stationInventory.ToDictionary(item => item.ItemTypeId, item => item.Quantity,
                    StringComparer.Ordinal).OrderBy(pair => pair.Key, StringComparer.Ordinal));
            Assert.DoesNotContain(stationInventory, item => item.ItemTypeId == "item.uranium-ore");
        }

        var transit = Assert.Single(first.GameState.SpaceObjects,
            obj => obj.ObjectId == "SPC-MARKET-TRANSIT");
        Assert.NotNull(transit.Inventory);
        var transitInventory = transit.Inventory!;
        Assert.Equal(28800, transit.Credits);
        Assert.All(transitInventory.Where(item => item.ItemTypeId != "item.fuel"),
            item => Assert.Equal(144, item.Quantity));
        Assert.Equal(600, Assert.Single(transitInventory,
            item => item.ItemTypeId == "item.fuel").Quantity);
    }

    [Fact]
    public void Default_and_docked_explicit_inventories_are_unchanged()
    {
        var requiredSeven = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["item.food-rations"] = 500,
            ["item.energy-cells"] = 350,
            ["item.fuel"] = 700,
            ["item.ice"] = 320,
            ["item.iron-ore"] = 410,
            ["item.silicon"] = 70,
            ["item.magnesium-ore"] = 120,
        };
        var dockedExpected = new Dictionary<string, long>(requiredSeven, StringComparer.Ordinal)
        {
            ["item.uranium-ore"] = 20,
            ["item.carbon-ore"] = 510,
        };

        AssertLegacyStation("Default", requiredSeven);
        AssertLegacyStation("Docked", dockedExpected);
    }

    [Theory]
    [MemberData(nameof(DemoStations))]
    public async Task Every_demo_station_docks_and_trades_via_session(
        string stationId, string representativeSupplyItemId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using IGameSessionConnection connection =
            LocalGameSessionConnection.CreateFromScenarioFile(SettingsPath, DemoScenarioPath, saveDirectory: null);
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        var initial = await WaitForSnapshotAsync(snapshots,
            snapshot => snapshot.PlayerShipObjectId == PlayerId, timeout.Token);
        Assert.Equal(100000, initial.PlayerCredits);

        const ulong dockSequence = 1;
        string dockCommandId = $"{stationId}-dock";
        await connection.SendCommandAsync(new PlayerCommand(
            dockCommandId, dockSequence, PlayerId, NavigationModuleId,
            NavigationComputerCommandTypes.Dock, TargetObjectId: stationId), timeout.Token);

        var identityRequest = await WaitForSnapshotAsync(snapshots,
            snapshot => snapshot.ActiveDialogue?.CurrentNodeId == "request_ship_id", timeout.Token);
        AssertCommandExecuted(identityRequest, dockCommandId, requestedQuantity: null);

        await connection.SendDialogueCommandAsync(new DialogueCommand(
            $"{stationId}-truthful", DialogueAction.Choose,
            identityRequest.ActiveDialogue!.InstanceId, identityRequest.ActiveDialogue.Revision,
            ChoiceId: "truthful_id"), timeout.Token);
        var feeOffer = await WaitForSnapshotAsync(snapshots,
            snapshot => snapshot.ActiveDialogue?.CurrentNodeId == "offer_port_fee", timeout.Token);

        await connection.SendDialogueCommandAsync(new DialogueCommand(
            $"{stationId}-accept-fee", DialogueAction.Choose,
            feeOffer.ActiveDialogue!.InstanceId, feeOffer.ActiveDialogue.Revision,
            ChoiceId: "accept_fee"), timeout.Token);
        var docked = await WaitForSnapshotAsync(snapshots, snapshot =>
            snapshot.Objects.Any(obj => obj.ObjectId == PlayerId && obj.IsDocked &&
                obj.DockedStationObjectId == stationId) &&
            snapshot.DockedStationTrade?.StationObjectId == stationId, timeout.Token);

        Assert.Equal(99900, docked.PlayerCredits);
        using (var tradeJson = JsonDocument.Parse(JsonSerializer.Serialize(docked.DockedStationTrade)))
        {
            Assert.False(tradeJson.RootElement.TryGetProperty("Credits", out _));
            Assert.False(tradeJson.RootElement.TryGetProperty("InitialCredits", out _));
        }

        Assert.Equal("welcome", docked.ActiveDialogue?.CurrentNodeId);
        await connection.SendDialogueCommandAsync(new DialogueCommand(
            $"{stationId}-continue", DialogueAction.Choose,
            docked.ActiveDialogue!.InstanceId, docked.ActiveDialogue.Revision,
            ChoiceId: "continue"), timeout.Token);
        var readyToTrade = await WaitForSnapshotAsync(snapshots,
            snapshot => snapshot.ActiveDialogue is null &&
                snapshot.DockedStationTrade?.StationObjectId == stationId, timeout.Token);

        var supplyBefore = TradeItem(readyToTrade, representativeSupplyItemId);
        var cargoBeforeBuy = CargoQuantity(readyToTrade, representativeSupplyItemId);
        string buyCommandId = $"{stationId}-buy";
        await connection.SendCommandAsync(new PlayerCommand(
            buyCommandId, 2, PlayerId, CargoModuleId, TradeCommandTypes.Buy,
            ItemTypeId: representativeSupplyItemId, Quantity: 1), timeout.Token);
        var bought = await WaitForSnapshotAsync(snapshots,
            snapshot => HasCommandResult(snapshot, buyCommandId), timeout.Token);
        AssertCommandExecuted(bought, buyCommandId, 1);
        Assert.Equal(supplyBefore.StockQuantity - 1,
            TradeItem(bought, representativeSupplyItemId).StockQuantity);
        Assert.Equal(cargoBeforeBuy + 1, CargoQuantity(bought, representativeSupplyItemId));
        Assert.Equal(readyToTrade.PlayerCredits - supplyBefore.UnitPriceCredits, bought.PlayerCredits);

        var waterBefore = TradeItem(bought, "item.water");
        long cargoWaterBefore = CargoQuantity(bought, "item.water");
        string sellCommandId = $"{stationId}-sell";
        await connection.SendCommandAsync(new PlayerCommand(
            sellCommandId, 3, PlayerId, CargoModuleId, TradeCommandTypes.Sell,
            ItemTypeId: "item.water", Quantity: 1), timeout.Token);
        var sold = await WaitForSnapshotAsync(snapshots,
            snapshot => HasCommandResult(snapshot, sellCommandId), timeout.Token);
        AssertCommandExecuted(sold, sellCommandId, 1);
        Assert.Equal(waterBefore.StockQuantity + 1, TradeItem(sold, "item.water").StockQuantity);
        Assert.Equal(cargoWaterBefore - 1, CargoQuantity(sold, "item.water"));
        Assert.Equal(bought.PlayerCredits + waterBefore.UnitPriceCredits, sold.PlayerCredits);

        var fuelBefore = TradeItem(sold, "item.fuel");
        long tankBefore = FuelQuantity(sold);
        string refuelCommandId = $"{stationId}-refuel";
        await connection.SendCommandAsync(new PlayerCommand(
            refuelCommandId, 4, PlayerId, EngineModuleId, TradeCommandTypes.Refuel,
            ItemTypeId: "item.fuel", Quantity: 1), timeout.Token);
        var refueled = await WaitForSnapshotAsync(snapshots,
            snapshot => HasCommandResult(snapshot, refuelCommandId), timeout.Token);
        AssertCommandExecuted(refueled, refuelCommandId, 1);
        Assert.Equal(fuelBefore.StockQuantity - 1, TradeItem(refueled, "item.fuel").StockQuantity);
        Assert.Equal(tankBefore + 1, FuelQuantity(refueled));
        Assert.Equal(sold.PlayerCredits - fuelBefore.UnitPriceCredits, refueled.PlayerCredits);
    }

    [Fact]
    public void Unknown_shipping_profile_item_blocks_session_creation()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"dss-market-profile-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        string invalidProfilesPath = Path.Combine(tempRoot, "station-market-profiles.json");
        string tempSettingsPath = Path.Combine(tempRoot, "Settings.json");

        try
        {
            var profiles = JsonNode.Parse(File.ReadAllText(ProfilesPath))!.AsObject();
            profiles["profiles"]![0]!["supplyItemTypeIds"]![0] = "item.missing";
            profiles["profiles"]![0]!["initialInventory"]![0]!["itemTypeId"] = "item.missing";
            File.WriteAllText(invalidProfilesPath, profiles.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
            }));

            var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!.AsObject();
            var typeData = settings["typeData"]!.AsObject();
            foreach (string propertyName in typeData.Select(property => property.Key).ToArray())
            {
                string declaredPath = typeData[propertyName]!.GetValue<string>();
                typeData[propertyName] = Path.GetFullPath(Path.Combine(ClientRoot, declaredPath));
            }
            typeData["stationMarketProfiles"] = invalidProfilesPath;
            settings["defaultScenario"] = Path.Combine(ClientRoot, "Scenarios", "Default", "scenario.json");
            File.WriteAllText(tempSettingsPath, settings.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
            }));

            var error = Assert.Throws<ContentException>(() =>
                LocalGameSessionConnection.CreateFromScenarioFile(
                    tempSettingsPath, DemoScenarioPath, saveDirectory: null));
            Assert.Contains(invalidProfilesPath, error.Message, StringComparison.Ordinal);
            Assert.Contains("market.mining", error.Message, StringComparison.Ordinal);
            Assert.Contains("item.missing", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Demo_and_profiles_are_copied_to_output()
    {
        string[] relativePaths =
        [
            Path.Combine("Data", "Markets", "station-market-profiles.json"),
            Path.Combine("Scenarios", "MarketProfiles", "scenario.json"),
        ];

        foreach (string relativePath in relativePaths)
        {
            string sourcePath = Path.Combine(ClientRoot, relativePath);
            string outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            Assert.True(File.Exists(outputPath), $"Missing shipped content: {outputPath}");
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(sourcePath)),
                JsonNode.Parse(File.ReadAllText(outputPath))), $"Shipped content differs: {relativePath}");
        }
    }

    private static ScenarioFile CaptureDemoState()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        return engine.CaptureSaveState();
    }

    private static void AssertLegacyStation(string scenarioDirectory, IReadOnlyDictionary<string, long> expected)
    {
        string path = Path.Combine(ClientRoot, "Scenarios", scenarioDirectory, "scenario.json");
        var scenario = ScenarioLoader.LoadFromFile(path);
        var station = Assert.Single(scenario.GameState.SpaceObjects,
            obj => obj.ObjectType.Equals("Station", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Large", station.StationSize);
        Assert.Null(station.MarketProfileId);
        Assert.Equal(expected.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            station.Inventory!.ToDictionary(item => item.ItemTypeId, item => item.Quantity,
                StringComparer.Ordinal).OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    private static long Scale(long value, long factor) => value * factor / 1000;

    private static async Task<AuthoritativeSnapshot> WaitForSnapshotAsync(
        IAsyncEnumerator<AuthoritativeSnapshot> snapshots,
        Func<AuthoritativeSnapshot, bool> predicate,
        CancellationToken cancellationToken)
    {
        while (await snapshots.MoveNextAsync().AsTask().WaitAsync(cancellationToken))
        {
            if (predicate(snapshots.Current)) return snapshots.Current;
        }

        throw new InvalidOperationException("Snapshot stream completed before the expected state was published.");
    }

    private static bool HasCommandResult(AuthoritativeSnapshot snapshot, string commandId) =>
        snapshot.CommandResults.Any(result => result.CommandId == commandId);

    private static void AssertCommandExecuted(
        AuthoritativeSnapshot snapshot, string commandId, long? requestedQuantity)
    {
        var result = Assert.Single(snapshot.CommandResults, item => item.CommandId == commandId);
        Assert.True(result.Status == CommandResultStatus.Executed,
            $"Command '{commandId}' was {result.Status}: {result.ReasonCode}");
        Assert.Null(result.ReasonCode);
        if (requestedQuantity is { } quantity)
        {
            Assert.Equal(1, quantity);
            // These one-unit operations must be full fills; the contract encodes a full fill as null.
            Assert.Null(result.ExecutedQuantity);
        }
        else
        {
            Assert.Null(result.ExecutedQuantity);
        }
    }

    private static StationInventoryItemSnapshot TradeItem(
        AuthoritativeSnapshot snapshot, string itemTypeId) =>
        Assert.Single(snapshot.DockedStationTrade!.Items, item => item.ItemTypeId == itemTypeId);

    private static long CargoQuantity(AuthoritativeSnapshot snapshot, string itemTypeId)
    {
        var cargo = Assert.Single(snapshot.InstalledModules, module => module.ModuleId == CargoModuleId);
        return cargo.Cargo.FirstOrDefault(stack => stack.ItemTypeId == itemTypeId)?.Quantity ?? 0;
    }

    private static long FuelQuantity(AuthoritativeSnapshot snapshot) =>
        Assert.Single(snapshot.InstalledModules, module => module.ModuleId == EngineModuleId).FuelAmountKg!.Value;

    private sealed record ExpectedProfile(
        string Id,
        string DisplayName,
        string[] Supply,
        string[] Demand,
        (string ItemId, long Quantity)[] Inventory,
        long InitialCredits,
        long RefuelStockKg);
}
