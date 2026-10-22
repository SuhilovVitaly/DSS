using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeSaveSchemaTests
{
    [Fact]
    public void Captured_point_flight_roundtrips_without_probability_or_guidance_state()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot());
        var save = Point.Save(engine, 100); var flight = Assert.Single(save.GameState.PointCombatState!.Projectiles);
        Assert.Empty(save.GameState.DefenseState!.Projectiles);
        Assert.Null(Assert.Single(save.GameState.DefenseState.Journal).ChanceTenths);
        engine.LoadScenario(Parse(save), true);
        Assert.Equal(flight, Assert.Single(Point.Save(engine, 100).GameState.PointCombatState!.Projectiles));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("module")]
    [InlineData("kind")]
    [InlineData("profile")]
    [InlineData("speed")]
    [InlineData("radius")]
    [InlineData("damage")]
    [InlineData("deadline")]
    [InlineData("position")]
    [InlineData("launch-time")]
    [InlineData("sequence")]
    [InlineData("cursor")]
    [InlineData("defense-version")]
    [InlineData("journal-chance")]
    [InlineData("journal-roll")]
    [InlineData("defense-auto")]
    [InlineData("defense-guiding")]
    [InlineData("defense-burst")]
    public void Malformed_point_fields_are_rejected_atomically(string defect)
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot());
        var save = Point.Save(engine, 100); var state = save.GameState; var point = state.PointCombatState!;
        var row = Assert.Single(point.Projectiles); var f = row.Flight;
        f = defect switch
        {
            "owner" => f with { OwnerObjectId = "missing" },
            "module" => f with { LauncherModuleId = "missing" },
            "kind" => f with { Kind = (ProjectileKind)99 },
            "profile" => f with { ProfileId = "" },
            "speed" => f with { SpeedKmS = 0 },
            "radius" => f with { BlastRadiusKm = 0 },
            "damage" => f with { Damage = -1 },
            "deadline" => f with { DetonationMotionTimeMs = 99 },
            "position" => f with { X = f.X + 100 },
            "launch-time" => f with { LaunchMotionTimeMs = 101 },
            _ => f
        };
        state = state with { PointCombatState = point with { Projectiles = [row with { Flight = f }] } };
        state = defect switch
        {
            "sequence" => state with { DefenseState = state.DefenseState! with { ProjectileSequence = 0 } },
            "cursor" => state with { CombatState = state.CombatState! with { LastProcessedMotionTimeMs = 101 } },
            "defense-version" => state with { DefenseState = state.DefenseState! with { Version = 1 } },
            "journal-chance" => state with { DefenseState = state.DefenseState! with { Journal = state.DefenseState.Journal.Select(e => e with { ChanceTenths = 400 }).ToArray() } },
            "journal-roll" => state with { DefenseState = state.DefenseState! with { Journal = state.DefenseState.Journal.Select(e => e with { Roll = 42 }).ToArray() } },
            _ => state
        };
        if (defect.StartsWith("defense-", StringComparison.Ordinal) && defect != "defense-version")
            state = state with
            {
                DefenseState = state.DefenseState! with
                {
                    Launchers = state.DefenseState.Launchers.Select(l =>
                l.OwnerObjectId != Point.Player || l.ModuleId != Point.Counter ? l : l with
                {
                    State = defect switch
                    {
                        "defense-auto" => l.State with { AutoEnabled = true },
                        "defense-guiding" => l.State with { State = DefenseState.Guiding },
                        "defense-burst" => l.State with { PendingBurstShots = [new PendingBurstShot(200, "torpedo-1", 2)] },
                        _ => l.State
                    }
                }).ToArray()
                }
            };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = state }, true));
        Assert.Equal(ScenarioLoader.Serialize(save), ScenarioLoader.Serialize(Point.Save(engine, 100)));
    }

    [Fact]
    public void Case_variant_point_ids_normalize_consistently()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot());
        var save = Point.Save(engine, 100); var state = save.GameState;
        var mixed = save with
        {
            GameState = state with
            {
                PointCombatState = state.PointCombatState! with
                {
                    Projectiles = state.PointCombatState!.Projectiles.Select(p => p with
                    { ObjectId = p.ObjectId.ToUpperInvariant(), Flight = p.Flight with { OwnerObjectId = p.Flight.OwnerObjectId.ToLowerInvariant(), LauncherModuleId = p.Flight.LauncherModuleId.ToLowerInvariant() } }).ToArray()
                },
                DefenseState = state.DefenseState! with
                {
                    Launchers = state.DefenseState.Launchers.Select(l => l with
                    {
                        OwnerObjectId = l.OwnerObjectId.ToLowerInvariant(),
                        ModuleId = l.ModuleId.ToLowerInvariant(),
                        State = l.State with
                        { ActiveProjectileId = l.State.ActiveProjectileId?.ToUpperInvariant(), ActiveProjectileIds = l.State.ActiveProjectileIds?.Select(id => id.ToUpperInvariant()).ToImmutableArray() }
                    }).ToArray()
                }
            }
        };
        engine.LoadScenario(Parse(mixed), true);
        Assert.Equal(Assert.Single(state.PointCombatState!.Projectiles), Assert.Single(Point.Save(engine, 100).GameState.PointCombatState!.Projectiles));
    }

    [Theory]
    [InlineData("phase")]
    [InlineData("cursor")]
    [InlineData("duplicate")]
    [InlineData("epoch")]
    [InlineData("elapsed")]
    [InlineData("position")]
    [InlineData("module")]
    [InlineData("radius")]
    [InlineData("target")]
    [InlineData("version")]
    public void Invalid_deadline_and_ai_phase_rejected(string defect)
    {
        using var engine = Create(HostileOrbitRuntimeTests.Scene());
        var save = Save(engine, 155);
        var payload = save.GameState.HostileCombatState!;
        var row = Assert.Single(payload.Actors);
        Assert.True(row.ActiveMotion);
        Parse(save);
        var h = row.State;
        h = defect switch
        {
            "phase" => h with { Phase = (HostileCombatPhase)99 },
            "epoch" => h with { Segment = h.Segment with { EpochMs = 156 } },
            "elapsed" => h with { Segment = h.Segment with { ElapsedMs = 101 } },
            "position" => h with { Segment = h.Segment with { StartX = h.Segment.StartX + 100 } },
            "module" => h with { EngineModuleId = "missing" },
            "radius" => h with { DesiredRadiusKm = 101 },
            "target" => h with { TargetObjectId = row.ObjectId },
            "version" => h with { Version = 99 },
            _ => h
        };
        payload = payload with { Actors = [row with { State = h }] };
        if (defect == "cursor") payload = payload with { NextDecisionMotionTimeMs = 199 };
        if (defect == "duplicate") payload = payload with { Actors = [row, row with { ObjectId = row.ObjectId.ToLowerInvariant() }] };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = save.GameState with { HostileCombatState = payload } }, true));
        Assert.Equal(ScenarioLoader.Serialize(save), ScenarioLoader.Serialize(Save(engine, 155)));
    }


    private static ScenarioFile Save(SimulationEngine engine, long time) => engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, time);
    private static ScenarioFile Parse(ScenarioFile save) => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, isSave: true);
}
