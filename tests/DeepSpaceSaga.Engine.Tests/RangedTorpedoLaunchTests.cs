using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class RangedTorpedoLaunchTests
{
    private const string PirateLauncher = "MOD-PIRATE-TORPEDO-01";

    [Theory]
    [InlineData(false, 500, true)]
    [InlineData(false, 500.01, false)]
    [InlineData(true, 500, true)]
    [InlineData(true, 500.01, false)]
    public void Both_sides_use_configured_module_range(bool hostile, double distance, bool allowed)
    {
        var source = Registry.Value;
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
            Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories),
            All(source.ModuleTypes).Select(m => m.TorpedoRangeKm is not null ? m with { TorpedoRangeKm = 50 } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents));
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { PositionY = -distance } : o));
        if (hostile) engine.TryStartHostileTorpedoFire(Target, PirateLauncher, 0);
        else engine.ReceiveCommand(Fire("configured-range"));
        Assert.Equal(allowed ? 1 : 0, At(engine, 0).Objects.Count(o => o.Torpedo is not null));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void Range_uses_physical_launch_pose(bool hostile, bool approaching)
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == (hostile ? Player : Target)
            ? o with
            {
                PositionY = approaching ? -2010 : -1990,
                SpeedMps = 1000,
                DirectionDegrees = approaching ? 180 : 0,
                MovementType = "Linear"
            }
            : o with { PositionY = 0 }));
        At(engine, 2000);
        if (hostile) engine.TryStartHostileTorpedoFire(Target, PirateLauncher, 2000);
        else engine.ReceiveCommand(Fire("moving-range"));
        Assert.Equal(approaching ? 1 : 0, At(engine, 2000).Objects.Count(o => o.Torpedo is not null));
    }

    [Theory]
    [InlineData(false, 1999.99, true)]
    [InlineData(false, 2000, true)]
    [InlineData(false, 2000.01, false)]
    [InlineData(true, 1999.99, true)]
    [InlineData(true, 2000, true)]
    [InlineData(true, 2000.01, false)]
    public void Both_sides_obey_inclusive_range(bool hostile, double distance, bool allowed)
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { PositionY = -distance } : o));
        if (hostile)
        {
            var result = engine.TryStartHostileTorpedoFire(Target, PirateLauncher, 0);
            Assert.Equal(allowed ? CommandStartDisposition.Started : CommandStartDisposition.Rejected, result.Disposition);
            Assert.Equal(allowed ? null : "target_out_of_range", result.ReasonCode);
            Assert.Empty(At(engine, 0).CommandResults);
            Assert.Empty(engine.CaptureSaveState().GameState.CombatState!.ProcessedLaunchCommandIds);
        }
        else
        {
            engine.ReceiveCommand(Fire("range"));
            var result = Assert.Single(At(engine, 0).CommandResults);
            Assert.Equal(allowed ? CommandResultStatus.Executed : CommandResultStatus.Rejected, result.Status);
            Assert.Equal(allowed ? null : "target_out_of_range", result.ReasonCode);
        }
        var flights = At(engine, 0).Objects.Where(o => o.Torpedo is not null).ToArray();
        Assert.Equal(allowed ? 1 : 0, flights.Length);
        if (allowed)
        {
            Assert.Equal(hostile ? Target : Player, flights[0].Torpedo!.OwnerObjectId);
            Assert.Equal(hostile ? Player : Target, flights[0].Torpedo!.TargetObjectId);
            Assert.Equal(hostile ? PlayerRelation.Enemy : PlayerRelation.Self, flights[0].Torpedo!.OwnerPlayerRelationAtLaunch);
        }
    }

    [Fact]
    public void Forged_npc_player_command_rejected()
    {
        using var engine = Create();
        string before = JsonSerializer.Serialize(engine.CaptureSaveState().GameState.CombatState);
        engine.ReceiveCommand(new("forged", 1, Target, PirateLauncher, CombatCommandTypes.Fire, Player));
        var result = Assert.Single(At(engine, 0).CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal("unknown_object", result.ReasonCode);
        Assert.Equal(before, JsonSerializer.Serialize(engine.CaptureSaveState().GameState.CombatState));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Active_torpedo_survives_range_exit(bool hostile)
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == (hostile ? Player : Target)
            ? o with { PositionY = -1990, SpeedMps = 4000, DirectionDegrees = 0, MovementType = "Linear" }
            : o with { PositionY = 0 }));
        if (hostile) Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostileTorpedoFire(Target, PirateLauncher, 0).Disposition);
        else { engine.ReceiveCommand(Fire("flee")); At(engine, 0); }
        var initial = Assert.Single(At(engine, 0).Objects, o => o.Torpedo is not null);
        var after = At(engine, 1000);
        Assert.Equal(initial.ObjectId, Assert.Single(after.Objects, o => o.Torpedo is not null).ObjectId);
        Assert.Empty(after.CombatImpacts);
    }

    [Theory]
    [InlineData("friend")]
    [InlineData("neutral")]
    public void Non_enemy_internal_actor_cannot_launch(string relation)
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with { RelationToPlayer = relation } : o));
        Assert.Equal(CommandStartDisposition.Rejected, engine.TryStartHostileTorpedoFire(Target, PirateLauncher, 0).Disposition);
        Assert.DoesNotContain(At(engine, 0).Objects, o => o.Torpedo is not null);
    }
}
