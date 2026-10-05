using System.Text.Json;
using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class VoyageFuelLifecycleTests
{
    private const string Ship = QuotedTradeExecutionTests.ShipId;
    private const string Tank = QuotedTradeExecutionTests.EngineModuleId;
    private const string SecondTank = "second-engine-tank";
    private static ScenarioFile Save(SimulationEngine e, long time = 0) => e.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, time);
    private static AuthoritativeSnapshot Snapshot(SimulationEngine e, long time = 0) => e.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, time);
    private static ShipModuleData[] Tanks(ScenarioFile save) => save.GameState.SpaceObjects.Single(o => o.ObjectId == Ship).Modules!
        .Where(m => m.ModuleId == Tank || m.ModuleId == SecondTank).ToArray();
    private static void AssertTanks(ShipModuleData[] expected, ShipModuleData[] actual) =>
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    private static string Destination(SimulationEngine e) => Snapshot(e).Voyage!.RouteOptions.First(o => o.IsAvailable).DestinationStationObjectId;
    private static SimulationEngine Load(ScenarioFile save, GameDataRegistry? registry = null)
    {
        var engine = new SimulationEngine(registry ?? QuotedTradeExecutionTests.RealRegistry());
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: save.SaveFormatVersion > 0);
        return engine;
    }
    private static ScenarioFile Initial(bool second = false, long firstAmount = 1000)
    {
        using var engine = VoyageLifecycleTests.CreateEngine();
        var save = Save(engine);
        return save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != Ship ? o : o with
                {
                    Modules = o.Modules!.Select(m => m.ModuleId == Tank ? m with { FuelAmountKg = firstAmount, FuelCostBasisCredits = firstAmount * 11 } : m)
                        .Concat(second ? [o.Modules!.Single(m => m.ModuleId == Tank) with
                            { ModuleId = SecondTank, FuelAmountKg = 1000, FuelCostBasisCredits = 100003,
                                OccupiedCells = o.HullLayout!.Cells.Where(c => !o.Modules!.SelectMany(m => m.OccupiedCells).Contains(c))
                                    .Take(o.Modules!.Single(m => m.ModuleId == Tank).OccupiedCells.Count).ToArray() }] : []).ToArray()
                }).ToArray()
            }
        };
    }

    [Theory]
    [InlineData("10", 1000, 10L, 1L)]
    [InlineData("10.000000000000001", 1000, 10L, 2L)]
    [InlineData("7.5", 1300, 10L, 1L)]
    [InlineData("7.7", 1300, 10L, 2L)]
    [InlineData("10001", 1250, 10L, 1251L)]
    [InlineData("1", 1000, long.MaxValue, 1L)]
    public void Fractional_distance_formula_uses_exact_integer_ceiling(string distance, int multiplier, long efficiency, long expected)
        => Assert.Equal(expected, SimulationEngine.CalculateVoyageFuel(decimal.Parse(distance, CultureInfo.InvariantCulture), multiplier, efficiency));

    [Fact]
    public void Formula_overflow_is_explicit_and_round_trip_distance_keeps_fraction()
    {
        Assert.Throws<OverflowException>(() => SimulationEngine.CalculateVoyageFuel(decimal.MaxValue, int.MaxValue, 1));
        Assert.Equal(10.000000000000002m, SimulationEngine.CaptureFuelDistance(10.000000000000002));
    }

    [Fact]
    public void Undock_reserves_selected_route_and_basis_once_without_motion_change()
    {
        using var engine = Load(Initial());
        var before = Snapshot(engine);
        string destination = Destination(engine);
        var route = before.TradingRoutes.Single(r => r.DestinationStationObjectId == destination);
        var edge = Save(engine).GameState.TradingMap!.Edges.Single(e =>
            e.FromStationObjectId == route.OriginStationObjectId && e.ToStationObjectId == destination ||
            e.ToStationObjectId == route.OriginStationObjectId && e.FromStationObjectId == destination);
        long expected = SimulationEngine.CalculateVoyageFuel(SimulationEngine.CaptureFuelDistance(edge.DistanceKm), route.EffectiveFuelMultiplierPermille, 10);
        var command = VoyageLifecycleTests.Undock("fuel-reserve", destination);
        var tank = Tanks(Save(engine))[0];
        var ship = before.Objects.Single(o => o.ObjectId == Ship);
        engine.ReceiveCommand(command);
        var after = Snapshot(engine);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(after.CommandResults).Status);
        Assert.Equal(expected, after.ActiveVoyage!.ReservedFuelKg);
        Assert.Equal(0, after.ActiveVoyage.ProjectedConsumedFuelKg);
        Assert.Equal(0, after.ActiveVoyage.ProjectedRouteFuelCostCredits);
        var reservedTank = Tanks(Save(engine))[0];
        Assert.Equal(tank.FuelAmountKg - expected, reservedTank.FuelAmountKg);
        Assert.Equal(tank.FuelCostBasisCredits - expected * 11, reservedTank.FuelCostBasisCredits);
        var departedShip = after.Objects.Single(o => o.ObjectId == Ship);
        Assert.Equal((ship.X, ship.Y, ship.SpeedKmS, ship.Direction), (departedShip.X, departedShip.Y, departedShip.SpeedKmS, departedShip.Direction));
        engine.ReceiveCommand(command);
        Assert.Single(Snapshot(engine).CommandResults);
        AssertTanks([reservedTank], Tanks(Save(engine)));
    }

    [Fact]
    public void Insufficient_fuel_rejects_before_docking_voyage_tank_basis_or_money_changes()
    {
        using var engine = Load(Initial(firstAmount: 0));
        var before = Save(engine);
        string destination = Snapshot(engine).TradingRoutes.First(r => r.Availability != TradingRouteAvailability.Unavailable).DestinationStationObjectId;
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("fuel-empty", destination));
        var after = Snapshot(engine);
        Assert.Equal(CommandReasonCodes.InsufficientVoyageFuel, Assert.Single(after.CommandResults).ReasonCode);
        Assert.True(after.Objects.Single(o => o.ObjectId == Ship).IsDocked);
        Assert.Equal(before.GameState.VoyageState, Save(engine).GameState.VoyageState);
        AssertTanks(Tanks(before), Tanks(Save(engine)));
        Assert.Equal(before.GameState.PlayerTokens, after.PlayerCredits);
        Assert.Null(after.LastVoyageFuelSettlement);
        Assert.All(after.Voyage!.RouteOptions, o => Assert.False(o.IsAvailable));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_or_conflicting_engine_efficiency_rejects_without_partial_reservation(bool conflict)
    {
        var source = QuotedTradeExecutionTests.RealRegistry();
        var modules = Enumerable.Range(0, source.ModuleTypes.Count).Select(source.ModuleTypes.GetDefinition)
            .Select(m => m.TypeId == "module.engine.basic" && !conflict ? m with { FuelEfficiencyKmPerKg = null } : m).ToList();
        if (conflict) modules.Add(modules.Single(m => m.TypeId == "module.engine.basic") with { TypeId = "module.engine.other", FuelEfficiencyKmPerKg = 11 });
        var registry = GameDataRegistry.Create(
            Enumerable.Range(0, source.ModuleCategories.Count).Select(source.ModuleCategories.GetDefinition), modules,
            Enumerable.Range(0, source.ItemTypes.Count).Select(source.ItemTypes.GetDefinition),
            Enumerable.Range(0, source.CommandDefinitions.Count).Select(source.CommandDefinitions.GetDefinition),
            Enumerable.Range(0, source.FactoryTypes.Count).Select(source.FactoryTypes.GetDefinition),
            Enumerable.Range(0, source.Recipes.Count).Select(source.Recipes.GetDefinition),
            dialogues: Enumerable.Range(0, source.Dialogues.Count).Select(source.Dialogues.GetDefinition),
            stationMarketProfiles: Enumerable.Range(0, source.StationMarketProfiles.Count).Select(source.StationMarketProfiles.GetDefinition),
            shipClasses: Enumerable.Range(0, source.ShipClasses.Count).Select(source.ShipClasses.GetDefinition),
            stationMarketEvents: Enumerable.Range(0, source.StationMarketEvents.Count).Select(source.StationMarketEvents.GetDefinition));
        var save = Initial(second: conflict);
        if (conflict) save = QuotedTradeExecutionTests.WithShipModules(save, m => m.ModuleId == SecondTank ? m with { ModuleTypeId = "module.engine.other" } : m);
        using var engine = Load(save, registry);
        var before = Tanks(Save(engine));
        string destination = Snapshot(engine).TradingRoutes.First(r => r.Availability != TradingRouteAvailability.Unavailable).DestinationStationObjectId;
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("efficiency", destination));
        Assert.Equal(CommandReasonCodes.FuelEfficiencyUnavailable, Assert.Single(Snapshot(engine).CommandResults).ReasonCode);
        AssertTanks(before, Tanks(Save(engine)));
        Assert.Null(Snapshot(engine).ActiveVoyage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Interruption_refunds_original_tanks_and_basis_at_saved_progress_and_survives_replay(int progress)
    {
        using var departed = Load(Initial(second: true, firstAmount: 1));
        string destination = Destination(departed);
        var before = Tanks(Save(departed));
        departed.ReceiveCommand(VoyageLifecycleTests.Undock("multi-tank", destination));
        Snapshot(departed);
        var save = Save(departed);
        var voyage = save.GameState.VoyageState! with { ProgressPermille = progress };
        Assert.Equal(2, voyage.FuelReservationParts!.Count);
        // External world inputs enter through validated save/load, never runtime mutation.
        save = save with
        {
            GameState = save.GameState with
            {
                VoyageState = voyage,
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == destination ? o with { IsDestroyed = true } : o).ToArray()
            }
        };
        using var engine = Load(save);
        var projected = Snapshot(engine).ActiveVoyage!;
        long reserved = voyage.FuelReservationParts.Sum(p => p.ReservedFuelKg);
        long consumed = (reserved * progress + 999) / 1000;
        Assert.Equal(consumed, projected.ProjectedConsumedFuelKg);
        var interrupted = Snapshot(engine, 1);
        Assert.Null(interrupted.ActiveVoyage);
        var receipt = Assert.IsType<VoyageFuelSettlementSnapshot>(interrupted.LastVoyageFuelSettlement);
        Assert.Equal(reserved, receipt.ReservedFuelKg);
        Assert.Equal(consumed, receipt.ConsumedFuelKg);
        Assert.Equal(reserved - consumed, receipt.ReturnedFuelKg);
        Assert.Equal(projected.ProjectedRouteFuelCostCredits, receipt.RouteFuelCostCredits);
        var tanks = Tanks(Save(engine, 1));
        Assert.Equal(before.Sum(m => m.FuelAmountKg!.Value) - consumed, tanks.Sum(m => m.FuelAmountKg!.Value));
        Assert.Equal(before.Sum(m => m.FuelCostBasisCredits!.Value), tanks.Sum(m => m.FuelCostBasisCredits!.Value) + receipt.RouteFuelCostCredits);
        Assert.All(tanks, t => Assert.InRange(t.FuelAmountKg!.Value, 0, 1000));
        Assert.Equal(progress == 0 ? 1 : 0, tanks[0].FuelAmountKg);
        Assert.Equal(receipt, Snapshot(engine, 2).LastVoyageFuelSettlement);
        using var reloaded = Load(Save(engine, 2));
        Assert.Equal(receipt, Snapshot(reloaded, 3).LastVoyageFuelSettlement);
        AssertTanks(tanks, Tanks(Save(reloaded, 3)));
    }

    [Fact]
    public void Real_arrival_consumes_full_reservation_once_and_post_settlement_save_is_exact()
    {
        using var voyage = TradingVoyageFixture.Create(calendarRatio: 1);
        var before = Tanks(voyage.Save())[0];
        var (_, result) = voyage.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: voyage.Destination);
        Assert.Equal(CommandResultStatus.Executed, result!.Status);
        var active = voyage.Save().GameState.VoyageState!;
        long reserved = active.FuelReservationParts!.Sum(p => p.ReservedFuelKg);
        voyage.FinishFlightTo(voyage.Destination, splitSnapshots: true);
        var receipt = Assert.IsType<VoyageFuelSettlementSnapshot>(voyage.Snapshot.LastVoyageFuelSettlement);
        Assert.Equal(active.VoyageId, receipt.VoyageId);
        Assert.Equal(reserved, receipt.ConsumedFuelKg);
        Assert.Equal(0, receipt.ReturnedFuelKg);
        Assert.Equal(before.FuelAmountKg - reserved, Tanks(voyage.Save())[0].FuelAmountKg);
        Assert.Equal(before.FuelCostBasisCredits, Tanks(voyage.Save())[0].FuelCostBasisCredits + receipt.RouteFuelCostCredits);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(voyage.Save()), true);
        using var loaded = Load(save);
        Assert.Equal(receipt, Snapshot(loaded, voyage.MotionTime).LastVoyageFuelSettlement);
        AssertTanks(Tanks(save), Tanks(Save(loaded, voyage.MotionTime)));
        voyage.Capture();
        Assert.Equal(receipt, voyage.Snapshot.LastVoyageFuelSettlement);
    }

    [Fact]
    public void Mid_voyage_load_preserves_reservation_and_command_receipt_without_second_debit()
    {
        using var engine = Load(Initial());
        var command = VoyageLifecycleTests.Undock("persisted-reserve", Destination(engine));
        engine.ReceiveCommand(command);
        Snapshot(engine);
        var save = Save(engine);
        using var restored = Load(save);
        var before = Snapshot(restored);
        Assert.Equal(Snapshot(engine).ActiveVoyage, before.ActiveVoyage);
        AssertTanks(Tanks(save), Tanks(Save(restored)));
        restored.ReceiveCommand(command);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(Snapshot(restored).CommandResults).Status);
        AssertTanks(Tanks(save), Tanks(Save(restored)));
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("capacity")]
    [InlineData("basis")]
    [InlineData("formula")]
    public void Malformed_reservation_rejects_atomically(string corruption)
    {
        using var engine = Load(Initial());
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("bad-reserve", Destination(engine)));
        Snapshot(engine);
        var save = Save(engine);
        var part = save.GameState.VoyageState!.FuelReservationParts![0];
        var parts = corruption == "duplicate" ? new[] { part, part } : new[] { part with
        {
            ReservedFuelKg = corruption == "capacity" ? 1001 : part.ReservedFuelKg,
            ReservedFuelCostBasisCredits = corruption == "basis" ? -1 : part.ReservedFuelCostBasisCredits
        } };
        var bad = save with
        {
            GameState = save.GameState with
            {
                VoyageState = save.GameState.VoyageState with
                { FuelReservationParts = parts, FuelDistanceKm = corruption == "formula" ? 100000 : save.GameState.VoyageState.FuelDistanceKm }
            }
        };
        string before = ScenarioLoader.Serialize(save);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, true));
        Assert.Equal(before, ScenarioLoader.Serialize(Save(engine)));
    }
}
