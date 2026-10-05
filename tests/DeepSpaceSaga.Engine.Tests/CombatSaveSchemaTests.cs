using DeepSpaceSaga.Contracts;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatSaveSchemaTests
{
    internal static ScenarioFile ValidSave()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("schema-launch"));
        var snapshot = At(engine, 0);
        var missile = Assert.Single(snapshot.Objects.Where(o => o.Torpedo is not null));
        var save = engine.CaptureSaveState();
        return save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ShipClassId is null ? o : o with { HullHitPointsMax = 450 }).ToArray(),
                CombatState = new(1, 0, 1, 0, 0, 100,
                    snapshot.InstalledModules.Where(m => m.LauncherCombat is not null)
                        .Select(m => new LauncherSaveData(Player, m.ModuleId, m.LauncherCombat!)).ToArray(),
                    [new(missile.ObjectId, missile.Torpedo!, false, snapshot.Objects.Single(o => o.ObjectId == Target), 0)],
                    ["schema-launch"])
            }
        };
    }

    [Fact]
    public void Combat_schema_rejects_orphan_duplicate_and_nonfinite_state()
    {
        var save = ValidSave();
        var state = save.GameState.CombatState!;
        var projectile = state.Projectiles[0];
        var launcher = state.Launchers[0];
        var invalid = new[]
        {
            state with { Projectiles = [projectile, projectile] },
            state with { Launchers = [launcher, launcher] },
            state with { Launchers = [launcher with { OwnerObjectId = "missing" }] },
            state with { Launchers = [launcher with { State = launcher.State with { ActiveTorpedoObjectId = null } }] },
            state with { Projectiles = [projectile with { Flight = projectile.Flight with { TargetObjectId = "missing" } }] },
            state with { Projectiles = [projectile with { TargetLost = true }] },
            state with { Projectiles = [projectile with { Flight = projectile.Flight with { SpeedKmS = double.NaN } }] },
            state with { Projectiles = [projectile with { Flight = projectile.Flight with { DistanceTravelledWorldUnits = double.PositiveInfinity } }] },
            state with { LastProcessedMotionTimeMs = 1 },
            state with { ProcessedLaunchCommandIds = ["same", "same"] },
            state with { NextGuidanceMotionTimeMs = 0 }
        };
        using var existing = Create();
        string before = ScenarioLoader.Serialize(existing.CaptureSaveState());
        foreach (var combat in invalid)
        {
            Assert.Throws<ScenarioException>(() => existing.LoadScenario(save with { GameState = save.GameState with { CombatState = combat } }, true));
            Assert.Equal(before, ScenarioLoader.Serialize(existing.CaptureSaveState()));
        }
        Assert.NotNull(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true).GameState.CombatState);
    }

    [Fact]
    public void Current_save_requires_complete_hp_and_flight_state()
    {
        var save = ValidSave();
        foreach (var objects in new[]
        {
            save.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o with { HullHitPoints = null } : o).ToArray(),
            save.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o with { HullHitPointsMax = null } : o).ToArray(),
            save.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o with { HullHitPoints = 451 } : o).ToArray()
        })
            Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save with
            { GameState = save.GameState with { SpaceObjects = objects } }), true));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save with
        { GameState = save.GameState with { CombatState = null } }), true));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save with
        { GameState = save.GameState with { CombatState = save.GameState.CombatState! with { Projectiles = [] } } }), true));
    }

    [Fact]
    public void Legacy_v9_without_combat_remains_readable()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o with { ShipClassId = null, Modules = null }));
        var save = engine.CaptureSaveState();
        save = TradingEconomySaveSchemaTests.WithoutNewContinuation(save, 9);
        save = save with { GameState = save.GameState with { CombatState = null } };
        var loaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true);
        engine.LoadScenario(loaded, true);
        Assert.All(At(engine, 0).Objects, o => Assert.Null(o.HullCombat));
        Assert.Empty(At(engine, 0).InstalledModules);
    }

    [Fact]
    public void Future_combat_route_version_is_rejected()
    {
        var save = ValidSave();
        var combat = save.GameState.CombatState!;
        var projectile = combat.Projectiles[0];
        combat = combat with
        {
            Projectiles = [projectile with { Flight = projectile.Flight with
        { Route = projectile.Flight.Route with { PlannerVersion = int.MaxValue } } }]
        };
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save with
        { GameState = save.GameState with { CombatState = combat } }), true));
    }

    [Theory]
    [InlineData("impactSequence")]
    [InlineData("wreckSequence")]
    [InlineData("projectileSequence")]
    [InlineData("processedLaunchCommandIds")]
    [InlineData("lastProcessedMotionTimeMs")]
    public void Current_combat_requires_explicit_identity_and_clock_fields(string field)
    {
        var json = JsonNode.Parse(ScenarioLoader.Serialize(ValidSave()))!;
        json["gameState"]!["combatState"]!.AsObject().Remove(field);
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json.ToJsonString(), true));
    }
}
