using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeSaveSchemaTests
{
    internal static SimulationEngine ManualZero()
    {
        var engine = new SimulationEngine(AutomaticDefenseTests.WeaponRegistry(0, 100));
        engine.LoadScenario(HostileTorpedoFireTests.HostileScenario());
        engine.ReceiveCommand(Manual("manual", LaunchEnemy(engine)));
        At(engine, 1);
        return engine;
    }

    [Fact]
    public void Zero_chance_manual_flight_is_valid_and_skill_metadata_does_not_change_chance()
    {
        using var engine = ManualZero();
        var save = Save(engine, 1);
        Assert.Equal(2, save.GameState.DefenseState!.Version);
        var flight = Assert.Single(save.GameState.DefenseState.Projectiles).Flight;
        Assert.Equal(0, flight.FrozenChanceTenths);
        Assert.Equal(LaunchMode.Manual, flight.Mode);
        var changed = ChangeFlight(save, f => f with
        {
            RatingBreakdown = f.RatingBreakdown with
            { DefenseOperator = f.RatingBreakdown.DefenseOperator with { Skill = 99 } }
        });
        var loaded = Parse(changed);
        engine.LoadScenario(loaded, true);
        Assert.Equal(0, Assert.Single(Save(engine, 1).GameState.DefenseState!.Projectiles).Flight.FrozenChanceTenths);
    }

    [Theory]
    [InlineData("accuracy-null")]
    [InlineData("accuracy-mismatch")]
    [InlineData("maneuver-null")]
    [InlineData("lifetime-null")]
    [InlineData("expiry-null")]
    [InlineData("expired")]
    [InlineData("overflow")]
    [InlineData("speed-null")]
    [InlineData("turn-negative")]
    [InlineData("mode")]
    [InlineData("chance")]
    [InlineData("route-speed")]
    [InlineData("route-phase")]
    [InlineData("sequence")]
    [InlineData("attempt")]
    [InlineData("cursor")]
    [InlineData("journal-mode")]
    [InlineData("missing-hostile")]
    [InlineData("defense-version")]
    [InlineData("torpedo-provenance")]
    [InlineData("torpedo-maneuverability")]
    public void Malformed_frozen_fields_rejected_atomically(string defect)
    {
        using var engine = ManualZero();
        var save = Save(engine, 1);
        var bad = ChangeFlight(save, f => defect switch
        {
            "accuracy-null" => f with { CapturedAccuracy = null },
            "accuracy-mismatch" => f with { CapturedAccuracy = 1 },
            "maneuver-null" => f with { CapturedTargetManeuverability = null },
            "lifetime-null" => f with { MaxFlightTimeMs = null },
            "expiry-null" => f with { ExpiresAtMotionTimeMs = null },
            "expired" => f with { ExpiresAtMotionTimeMs = 1 },
            "overflow" => f with { MaxFlightTimeMs = long.MaxValue },
            "speed-null" => f with { SpeedKmS = null },
            "turn-negative" => f with { TurnRateDegPerSec = -1 },
            "mode" => f with { Mode = (LaunchMode)99 },
            "chance" => f with { FrozenChanceTenths = 1 },
            "route-speed" => f with { Route = f.Route with { Segments = f.Route.Segments.SetItem(0, f.Route.Segments[0] with { SpeedKmS = 42 }) } },
            "route-phase" => f with { Route = f.Route with { Phase = (TorpedoRoutePhase)99 } },
            _ => f
        });
        var state = bad.GameState;
        state = defect switch
        {
            "sequence" => state with { DefenseState = state.DefenseState! with { ProjectileSequence = 0 } },
            "attempt" => state with { DefenseState = state.DefenseState! with { AttemptedTorpedoIds = [] } },
            "cursor" => state with { CombatState = state.CombatState! with { LastProcessedMotionTimeMs = 2 } },
            "journal-mode" => state with
            {
                DefenseState = state.DefenseState! with
                { Journal = state.DefenseState!.Journal.Select(e => e.LaunchMode is null ? e : e with { LaunchMode = (LaunchMode)99 }).ToArray() }
            },
            "missing-hostile" => state with { HostileCombatState = null },
            "defense-version" => state with { DefenseState = state.DefenseState! with { Version = 1 } },
            "torpedo-provenance" => state with
            {
                CombatState = state.CombatState! with
                { Projectiles = state.CombatState!.Projectiles.Select(p => p with { Flight = p.Flight with { OwnerPlayerRelationAtLaunch = null } }).ToArray() }
            },
            "torpedo-maneuverability" => state with
            {
                CombatState = state.CombatState! with
                { Projectiles = state.CombatState!.Projectiles.Select(p => p with { Flight = p.Flight with { CapturedManeuverability = null } }).ToArray() }
            },
            _ => state
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad with { GameState = state }, true));
        Assert.Equal(ScenarioLoader.Serialize(save), ScenarioLoader.Serialize(Save(engine, 1)));
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

    [Fact]
    public void Case_variant_ids_normalize_consistently()
    {
        using var engine = ManualZero();
        var save = Save(engine, 1);
        var defense = save.GameState.DefenseState!;
        var combat = save.GameState.CombatState!;
        var mixed = save with
        {
            GameState = save.GameState with
            {
                CombatState = combat with
                {
                    Launchers = combat.Launchers.Select(l => l with
                    {
                        OwnerObjectId = l.OwnerObjectId.ToLowerInvariant(),
                        ModuleId = l.ModuleId.ToLowerInvariant(),
                        State = l.State with { ActiveTorpedoObjectId = l.State.ActiveTorpedoObjectId?.ToUpperInvariant() }
                    }).ToArray(),
                    Projectiles = combat.Projectiles.Select(p => p with
                    {
                        ObjectId = p.ObjectId.ToUpperInvariant(),
                        Flight = p.Flight with
                        { OwnerObjectId = p.Flight.OwnerObjectId.ToLowerInvariant(), LauncherModuleId = p.Flight.LauncherModuleId.ToLowerInvariant(), TargetObjectId = p.Flight.TargetObjectId.ToLowerInvariant() },
                        TargetBaseline = p.TargetBaseline! with { ObjectId = p.TargetBaseline!.ObjectId.ToLowerInvariant() }
                    }).ToArray()
                },
                DefenseState = defense with
                {
                    Launchers = defense.Launchers.Select(l => l with
                    {
                        OwnerObjectId = l.OwnerObjectId.ToLowerInvariant(),
                        ModuleId = l.ModuleId.ToLowerInvariant(),
                        State = l.State with { ActiveProjectileId = l.State.ActiveProjectileId?.ToUpperInvariant() }
                    }).ToArray(),
                    AttemptedTorpedoIds = defense.AttemptedTorpedoIds.Select(id => id.ToUpperInvariant()).ToArray(),
                    Projectiles = defense.Projectiles.Select(p => p with
                    {
                        ObjectId = p.ObjectId.ToUpperInvariant(),
                        Flight = p.Flight with
                        { OwnerObjectId = p.Flight.OwnerObjectId.ToLowerInvariant(), LauncherModuleId = p.Flight.LauncherModuleId.ToLowerInvariant(), TargetTorpedoId = p.Flight.TargetTorpedoId.ToUpperInvariant() }
                    }).ToArray()
                }
            }
        };
        var parsed = Parse(mixed);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(defense.Projectiles[0].Flight),
            System.Text.Json.JsonSerializer.Serialize(parsed.GameState.DefenseState!.Projectiles[0].Flight));
        engine.LoadScenario(parsed, true);
        Assert.Equal(Player, Save(engine, 1).GameState.DefenseState!.Projectiles[0].Flight.OwnerObjectId);
        using var ai = Create(HostileOrbitRuntimeTests.Scene());
        var aiSave = Save(ai, 155);
        var payload = aiSave.GameState.HostileCombatState!;
        var actor = Assert.Single(payload.Actors);
        var normalized = Parse(aiSave with
        {
            GameState = aiSave.GameState with
            {
                HostileCombatState = payload with
                {
                    Actors = [actor with { ObjectId = actor.ObjectId.ToLowerInvariant(), State = actor.State with
        { TargetObjectId = actor.State.TargetObjectId.ToLowerInvariant(), EngineModuleId = actor.State.EngineModuleId.ToLowerInvariant(), WeaponModuleId = actor.State.WeaponModuleId.ToLowerInvariant() } }]
                }
            }
        });
        Assert.Equal(actor, Assert.Single(normalized.GameState.HostileCombatState!.Actors));
    }

    [Fact]
    public void Orphaned_enemy_torpedo_keeps_captured_provenance_after_owner_destruction_and_load()
    {
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            { SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Target ? FragileForOneHit(o) : o).ToArray() }
        };
        using var engine = Create(scenario);
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Fire("kill-owner"));
        var own = At(engine, 1).Objects.Single(o => o.Torpedo?.OwnerObjectId == Player).Torpedo!;
        long time = own.PredictedImpactMotionTimeMs!.Value + 1;
        var save = Save(engine, time);
        Assert.DoesNotContain(save.GameState.SpaceObjects, o => o.ObjectId == Target);
        using var restored = Create(Parse(save));
        Assert.Equal(PlayerRelation.Enemy, At(restored, time).Objects.Single(o => o.ObjectId == target).Torpedo!.OwnerPlayerRelationAtLaunch);
        restored.ReceiveCommand(Manual("orphan-after-load", target));
        Assert.Single(At(restored, time).Objects, o => o.Countermeasure is not null);
    }

    private static ScenarioFile Save(SimulationEngine engine, long time) => engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, time);
    private static ScenarioFile Parse(ScenarioFile save) => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, isSave: true);
    private static ScenarioFile ChangeFlight(ScenarioFile save, Func<CountermeasureSnapshot, CountermeasureSnapshot> change) => save with
    {
        GameState = save.GameState with
        {
            DefenseState = save.GameState.DefenseState! with
            { Projectiles = save.GameState.DefenseState!.Projectiles.Select(p => p with { Flight = change(p.Flight) }).ToArray() }
        }
    };
}
