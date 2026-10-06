using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class VoyageFuelAccountingTests
{
    private const string Tank = QuotedTradeExecutionTests.EngineModuleId;
    private static ShipModuleData SavedTank(SimulationEngine engine) => engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0)
        .GameState.SpaceObjects.Single(o => o.ObjectId == QuotedTradeExecutionTests.ShipId).Modules!.Single(m => m.ModuleId == Tank);
    private static SimulationEngine Create(long amount = 750, long? basis = null, bool profile = true) =>
        QuotedTradeExecutionTests.CreateMarketEngine(adjust: save =>
        {
            save = QuotedTradeExecutionTests.WithShipModules(save,
                m => m.ModuleId == Tank ? m with { FuelAmountKg = amount, FuelCostBasisCredits = basis } : m);
            return profile ? save : save with
            {
                GameState = save.GameState with
                {
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != QuotedTradeExecutionTests.StationId ? o : o with
                    {
                        MarketProfileId = null,
                        MarketProfileFingerprint = null,
                        MarketBudgetCredits = null,
                        MarketRevision = null
                    }).ToArray()
                }
            };
        });

    [Theory]
    [InlineData(null, "module.engine", true)]
    [InlineData(10L, "module.engine", true)]
    [InlineData(0L, "module.engine", false)]
    [InlineData(-1L, "module.engine", false)]
    [InlineData(10L, "module.storage", false)]
    public void Engine_efficiency_accepts_positive_optional_value_and_rejects_invalid_owner_or_value(long? efficiency, string owner, bool valid)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        var content = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Data", "Modules", "Engine", "modules-engine.json")))!;
        var module = content["moduleImplementations"]![0]!;
        module["type"] = owner;
        module["fuelEfficiencyKmPerKg"] = efficiency;
        string file = Path.Combine(Path.GetTempPath(), "dss-fuel-efficiency-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, content.ToJsonString());
        try
        {
            ModuleCategoryDefinition[] categories = [new("module.engine", "Engine", 1, []), new("module.storage", "Storage", 1, [])];
            if (!valid) Assert.Throws<ContentException>(() => EngineContentLoader.LoadModuleImplementations(file, categories));
            else Assert.Equal(efficiency, Assert.Single(EngineContentLoader.LoadModuleImplementations(file, categories)).FuelEfficiencyKmPerKg);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void Legacy_fuel_bootstraps_basis_from_item_fuel_base_price()
    {
        using var engine = Create();
        long price = QuotedTradeExecutionTests.Registry.ItemTypes.GetDefinition(QuotedTradeExecutionTests.Registry.ItemTypes.GetIndex("item.fuel")).BasePriceCredits!.Value;
        Assert.Equal(750 * price, SavedTank(engine).FuelCostBasisCredits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    public void Legacy_positive_fuel_requires_catalog_price_and_checked_bootstrap(long? price)
    {
        var source = QuotedTradeExecutionTests.Registry;
        var registry = GameDataRegistry.Create(
            Enumerable.Range(0, source.ModuleCategories.Count).Select(source.ModuleCategories.GetDefinition),
            Enumerable.Range(0, source.ModuleTypes.Count).Select(source.ModuleTypes.GetDefinition),
            price is null ? [] : [new ItemTypeDefinition("item.fuel", "Fuel", 1, price, TradeUnit: TradeUnit.Kilogram, StorageKind: ItemStorageKind.FuelTank)],
            Enumerable.Range(0, source.CommandDefinitions.Count).Select(source.CommandDefinitions.GetDefinition),
            shipClasses: Enumerable.Range(0, source.ShipClasses.Count).Select(source.ShipClasses.GetDefinition));
        var template = QuotedTradeExecutionTests.MarketTemplate();
        var ship = template.GameState.SpaceObjects.Single(o => o.ObjectId == QuotedTradeExecutionTests.ShipId);
        template = template with
        {
            GameState = template.GameState with
            {
                DefenseState = null,
                CombatState = null,
                CatalogCompatibility = null,
                SolarSystem = null,
                SpaceObjects = [ship with { DockedStationObjectId = null, IsDocked = false, Modules = ship.Modules!.Where(m => m.ModuleId == Tank)
                .Select(m => m with { FuelAmountKg = 2, FuelCostBasisCredits = null }).ToArray() }]
            }
        };
        using var engine = new SimulationEngine(registry);
        if (price == 1)
        {
            engine.LoadScenario(template);
            Assert.Equal(2, SavedTank(engine).FuelCostBasisCredits);
        }
        else
        {
            var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(template));
            Assert.Contains(price is null ? "requires item.fuel" : "overflow", error.Message);
        }
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1234L)]
    [InlineData(long.MaxValue)]
    public void Explicit_fuel_basis_round_trips_without_revaluation(long basis)
    {
        using var engine = Create(basis: basis);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        using var loaded = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        Assert.Equal(750, SavedTank(loaded).FuelAmountKg);
        Assert.Equal(basis, SavedTank(loaded).FuelCostBasisCredits);
    }

    [Theory]
    [InlineData(-1L, 750L, Tank)]
    [InlineData(1L, 0L, Tank)]
    [InlineData(1L, 750L, QuotedTradeExecutionTests.CargoModuleId)]
    public void Inconsistent_basis_rejects_atomically(long basis, long amount, string moduleId)
    {
        using var engine = Create(basis: 1234);
        string before = ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0));
        var bad = QuotedTradeExecutionTests.WithShipModules(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0),
            m => m.ModuleId == moduleId ? m with { FuelAmountKg = amount, FuelCostBasisCredits = basis } : m);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Refuel_adds_exact_authoritative_cost_to_tank_basis_atomically_and_replay_is_free(bool quoted)
    {
        using var engine = Create(basis: 1234, profile: quoted);
        var quote = QuotedTradeExecutionTests.Quote(engine, TradeCommandTypes.Refuel, "item.fuel", 10);
        Assert.Null(quote.DisabledReason);
        var command = quoted ? QuotedTradeExecutionTests.Bind("basis-refuel", quote) : new PlayerCommand("basis-refuel", 1,
            QuotedTradeExecutionTests.ShipId, Tank, TradeCommandTypes.Refuel, ItemTypeId: "item.fuel", Quantity: 10);
        long credits = engine.PlayerCredits;
        var result = QuotedTradeExecutionTests.Apply(engine, command);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        long cost = credits - engine.PlayerCredits;
        Assert.True(cost > 0);
        if (quoted) Assert.Equal(quote.TotalCredits, cost);
        Assert.Equal(760, SavedTank(engine).FuelAmountKg);
        Assert.Equal(1234 + cost, SavedTank(engine).FuelCostBasisCredits);
        string after = QuotedTradeExecutionTests.WorldProjection(engine);
        Assert.Equal(result, QuotedTradeExecutionTests.Apply(engine, command));
        Assert.Equal(after, QuotedTradeExecutionTests.WorldProjection(engine));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Overflowing_refuel_basis_rejects_without_money_stock_tank_or_basis_mutation(bool quoted)
    {
        using var engine = Create(basis: long.MaxValue, profile: quoted);
        var quote = QuotedTradeExecutionTests.Quote(engine, TradeCommandTypes.Refuel, "item.fuel", 1);
        var command = quoted ? QuotedTradeExecutionTests.Bind("basis-overflow", quote) : new PlayerCommand("basis-overflow", 1,
            QuotedTradeExecutionTests.ShipId, Tank, TradeCommandTypes.Refuel, ItemTypeId: "item.fuel", Quantity: 1);
        string before = QuotedTradeExecutionTests.WorldProjection(engine);
        var result = QuotedTradeExecutionTests.Apply(engine, command);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal("value_overflow", result.ReasonCode);
        Assert.Equal(before, QuotedTradeExecutionTests.WorldProjection(engine));
    }

    [Theory]
    [InlineData(0L, 0L, 0L, 0L)]
    [InlineData(2L, 1L, 1L, 1L)]
    [InlineData(3L, 10L, 1L, 3L)]
    [InlineData(3L, 10L, 2L, 7L)]
    [InlineData(3L, 10L, 3L, 10L)]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue / 2, long.MaxValue / 2)]
    public void Proportional_basis_allocation_conserves_total_with_away_from_zero_rounding(long totalKg, long basis, long takenKg, long expected)
    {
        long allocated = SimulationEngine.AllocateFuelCostBasis(totalKg, basis, takenKg);
        Assert.Equal(expected, allocated);
        Assert.InRange(allocated, 0, basis);
        Assert.Equal(basis, allocated + (basis - allocated));
    }

    [Fact]
    public void Individual_engine_commands_do_not_change_fuel_or_basis()
    {
        using var voyage = TradingVoyageFixture.Create(calendarRatio: 1);
        voyage.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: voyage.Destination);
        var before = voyage.Save().GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules!.Single(m => m.ModuleId == Tank);
        voyage.FinishFlightTo(voyage.Destination);
        var after = voyage.Save().GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules!.Single(m => m.ModuleId == Tank);
        Assert.True(voyage.ApproachSpeeds.Count > 0);
        Assert.Equal(before.FuelAmountKg, after.FuelAmountKg);
        Assert.Equal(before.FuelCostBasisCredits, after.FuelCostBasisCredits);
    }
}
