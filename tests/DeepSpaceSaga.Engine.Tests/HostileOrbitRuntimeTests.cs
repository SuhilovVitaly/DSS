using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class HostileOrbitRuntimeTests
{
    internal static ScenarioFile Scene(string relation = PlayerRelation.Enemy, string enginePower = "On", bool known = false) =>
        TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o : o with
        {
            PositionY = -10000,
            DirectionDegrees = 180,
            RelationToPlayer = relation,
            IsKnown = known,
            Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = enginePower } : m).ToArray()
        });

    [Fact]
    public void Hostile_ai_starts_without_detection_gate_and_uses_real_engine()
    {
        using var engine = Create(Scene());
        var paused = At(engine, 0).Objects.Single(o => o.ObjectId == Target);
        var beforeFuel = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0).GameState.SpaceObjects.Single(o => o.ObjectId == Target).Modules!.Select(m => m.FuelAmountKg).ToArray();
        Assert.Null(paused.HostileCombat);
        var snapshot = At(engine, 1000);
        var moved = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.Null(moved.DisplayName);
        Assert.True(moved.Y > paused.Y);
        Assert.Equal(0.4, moved.SpeedKmS, 9);
        Assert.Equal(100, moved.HostileCombat!.DesiredRadiusKm);
        Assert.Equal(HostileCombatPhase.Pursuit, moved.HostileCombat.Phase);
        Assert.Equal(1000, moved.HostileCombat.Segment.EpochMs);
        Assert.Empty(snapshot.CommandResults);
        Assert.Empty(snapshot.ShipEvents);
        var afterFuel = engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0).GameState.SpaceObjects.Single(o => o.ObjectId == Target).Modules!.Select(m => m.FuelAmountKg).ToArray();
        Assert.Equal(beforeFuel, afterFuel);
    }

    [Theory]
    [InlineData(PlayerRelation.Friend, "On")]
    [InlineData(PlayerRelation.Neutral, "On")]
    [InlineData(PlayerRelation.Enemy, "Off")]
    public void Non_hostiles_and_inoperable_engines_do_not_start_ai(string relation, string power)
    {
        using var engine = Create(Scene(relation, power));
        var original = At(engine, 0).Objects.Single(o => o.ObjectId == Target);
        var later = At(engine, 12345).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(original.X, later.X);
        Assert.Equal(original.Y, later.Y);
        Assert.Equal(original.SpeedKmS, later.SpeedKmS);
        Assert.Null(later.HostileCombat);
    }

    [Fact]
    public void Destroyed_engine_does_not_steer()
    {
        var scenario = Scene();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId != Target ? o : o with { Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { StructurePoints = 0 } : m).ToArray() }).ToArray()
            }
        };
        using var engine = Create(scenario);
        Assert.Null(At(engine, 1000).Objects.Single(o => o.ObjectId == Target).HostileCombat);
    }

    [Fact]
    public void Docking_binding_is_not_overridden_by_hostile_controller()
    {
        var scenario = Scene();
        var station = new SpaceObjectData("station", SpaceObjectType.Station, "Permanent", "Station",
            100, 200, 0, 0, "Stationary", null, null, null);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId != Target ? o : o with { IsDocked = true, DockedStationObjectId = "station" }).Append(station).ToArray()
            }
        };
        using var engine = Create(scenario);
        var before = At(engine, 0).Objects.Single(o => o.ObjectId == Target);
        var after = At(engine, 1000).Objects.Single(o => o.ObjectId == Target);
        Assert.True(after.IsDocked);
        Assert.Null(after.HostileCombat);
        Assert.Equal(before.X, after.X);
        Assert.Equal(before.Y, after.Y);
    }

    [Fact]
    public void Pause_and_calendar_only_advance_freeze_controller_and_segment()
    {
        using var engine = Create(Scene());
        var original = At(engine, 155).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(55, original.HostileCombat!.Segment.ElapsedMs);
        var paused = At(engine, 155, 500000).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(original, paused);
    }

    [Fact]
    public void Paused_scenario_with_nonzero_epoch_does_not_initialize_ai()
    {
        var scenario = Scene();
        scenario = scenario with { GameState = scenario.GameState with { GameTimeMs = 1234, SimulationTimeMs = 1234 } };
        using var engine = Create(scenario);
        Assert.Null(At(engine, 1234).Objects.Single(o => o.ObjectId == Target).HostileCombat);
        Assert.Null(At(engine, 1234, 9000).Objects.Single(o => o.ObjectId == Target).HostileCombat);
        Assert.NotNull(At(engine, 1235, 9001).Objects.Single(o => o.ObjectId == Target).HostileCombat);
    }

    [Fact]
    public void Split_steps_match_authoritative_motion_and_shared_prediction()
    {
        using var whole = Create(Scene());
        using var split = Create(Scene());
        foreach (long time in new long[] { 1, 31, 100, 101, 155, 234, 700, 713, 997 }) At(split, time);
        var a = At(whole, 1013).Objects.Single(o => o.ObjectId == Target);
        var b = At(split, 1013).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(a, b);
        var predicted = new LinearMotionPredictor().Predict(a, 47);
        Assert.Equal(predicted, At(whole, 1060).Objects.Single(o => o.ObjectId == Target));
    }

    [Fact]
    public void Direction_is_retained_during_entry_and_hold()
    {
        var scenario = Scene();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId == Target ? o with { PositionY = -1400, DirectionDegrees = 359 } : o).ToArray()
            }
        };
        using var engine = Create(scenario);
        var first = At(engine, 1).Objects.Single(o => o.ObjectId == Target).HostileCombat!;
        Assert.Equal(HostileCombatPhase.OrbitEntry, first.Phase);
        var held = At(engine, 500000).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(HostileCombatPhase.OrbitHold, held.HostileCombat!.Phase);
        Assert.Equal(first.Clockwise, held.HostileCombat.Clockwise);
        Assert.InRange(Math.Sqrt(held.X * held.X + held.Y * held.Y), 950, 1050);
    }

    [Fact]
    public void Every_armed_enemy_is_controlled_and_player_cannot_command_its_engine()
    {
        var source = Scene();
        var pirate = source.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        var other = pirate with
        {
            ObjectId = "another-enemy",
            PositionX = 1000,
            Modules = pirate.Modules!.Select(m => m with
            { ModuleId = "other-" + m.ModuleId, OperatorCrewId = m.OperatorCrewId is null ? null : "other-" + m.OperatorCrewId }).ToArray(),
            Crew = pirate.Crew!.Select(c => c with { CrewId = "other-" + c.CrewId }).ToArray()
        };
        using var engine = Create(source with { GameState = source.GameState with { SpaceObjects = source.GameState.SpaceObjects.Append(other).ToArray() } });
        engine.ReceiveCommand(new("forged", 1, Target, "MOD-PIRATE-ENGINE-01", ShipEngineCommandTypes.Accelerate));
        var snapshot = At(engine, 100);
        Assert.Equal(2, snapshot.Objects.Count(o => o.HostileCombat is not null));
        Assert.All(snapshot.Objects.Where(o => o.HostileCombat is not null), o => Assert.Equal(0.04, o.SpeedKmS, 10));
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(snapshot.CommandResults).Status);
    }
}
