using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureSaveSchemaTests
{
    [Theory]
    [InlineData("missing-rating")]
    [InlineData("mismatched-rating")]
    [InlineData("missing-operator")]
    [InlineData("invalid-skill")]
    [InlineData("wrong-skill-type")]
    [InlineData("false-migration")]
    public void Invalid_captured_torpedo_rating_is_rejected_atomically(string mutation)
    {
        using var engine = Create(AutomaticDefenseTests.DefenseScenario());
        engine.ReceiveCommand(Fire("launch")); At(engine, 0);
        var save = engine.CaptureSaveState();
        string before = ScenarioLoader.Serialize(save);
        var combat = save.GameState.CombatState!;
        var row = Assert.Single(combat.Projectiles);
        var flight = row.Flight;
        flight = mutation switch
        {
            "missing-rating" => flight with { TorpedoRating = null },
            "mismatched-rating" => flight with { TorpedoRating = 0 },
            "missing-operator" => flight with { RatingBreakdown = null },
            "invalid-skill" => flight with { RatingBreakdown = flight.RatingBreakdown! with { Skill = 101 } },
            "wrong-skill-type" => flight with { RatingBreakdown = flight.RatingBreakdown! with { SkillType = DeepSpaceSaga.Contracts.WeaponSkillType.CountermeasureDefense } },
            _ => flight with { RatingMigratedFromLegacySave = true }
        };
        var bad = save with { GameState = save.GameState with { CombatState = combat with { Projectiles = [row with { Flight = flight }] } } };
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(bad), true));
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Theory]
    [InlineData("chance")]
    [InlineData("attempt")]
    [InlineData("busy")]
    [InlineData("phase")]
    [InlineData("route")]
    [InlineData("selection")]
    [InlineData("rng-counter")]
    public void Malformed_active_defense_is_rejected(string mutation)
    {
        using var engine = Create(AutomaticDefenseTests.DefenseScenario());
        engine.ReceiveCommand(Fire("launch")); At(engine, 0); At(engine, 1000);
        var save = engine.CaptureSaveStateForTests(1000, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0);
        var state = save.GameState.DefenseState!;
        var projectile = Assert.Single(state.Projectiles);
        state = mutation switch
        {
            "chance" => state with { Projectiles = [projectile with { Flight = projectile.Flight with { FrozenChanceTenths = 501 } }] },
            "attempt" => state with { AttemptedTorpedoIds = [] },
            "busy" => state with { Launchers = state.Launchers.Select(l => l with { State = l.State with { ActiveProjectileId = "missing" } }).ToArray() },
            "phase" => state with { Projectiles = [projectile with { Flight = projectile.Flight with { Phase = (DeepSpaceSaga.Contracts.CountermeasurePhase)99 } }] },
            "route" => state with { Projectiles = [projectile with { Flight = projectile.Flight with { Route = projectile.Flight.Route with { ElapsedMs = -1 } } }] },
            "selection" => state with { SelectedProjectileId = "missing" },
            _ => state with { RngCounter = 1 }
        };
        save = save with { GameState = save.GameState with { DefenseState = state } };
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
    }
    [Fact]
    public void Unknown_rng_version_rejected()
    {
        var saved = TorpedoImpactTests.Scenario();
        saved = saved with { GameState = saved.GameState with { DefenseState = new(1, 0, 999, 0, 0, 0, [], [], [], []) } };
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(System.Text.Json.JsonSerializer.Serialize(saved)));
    }
}
