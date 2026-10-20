using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0009TK0003Tests
{
    internal static PointCombatStateData Payload()
    {
        OwnProjectileSnapshot Flight(string owner, ProjectileKind kind) => new(kind, owner, "launcher", 1, 2, 0, 0,
            10, 20, 0, 1000.25, kind == ProjectileKind.Torpedo ? 6 : 15, "captured-custom-profile", 9,
            kind == ProjectileKind.Torpedo ? 200 : 0, 20, Reporter: owner == "player" ? new(BattleReportReporterRole.TorpedoOperator, "crew", "Captured Name") : null,
            OwnerPlayerRelationAtLaunch: owner == "player" ? PlayerRelation.Self : PlayerRelation.Enemy);
        return new(1, [new("torpedo-1", Flight("player", ProjectileKind.Torpedo)),
            new("torpedo-2", Flight("enemy", ProjectileKind.Torpedo)), new("countermeasure-1", Flight("player", ProjectileKind.Countermissile))],
            [new("player", "engine", 20, new(20, 1500.5, "torpedo-2", EvadeSide.Right,
                new("player", 1, 2, 3, 45)), null, 10)],
            new(1, 123, 6), new(1, 456, 2), [new("enemy", "npc-engine", "torpedo-1", 50, 800, false, null, false)],
            new(CombatEncounterState.Active, 20, null, 20, ["enemy"], ["torpedo-1", "torpedo-2", "countermeasure-1"],
                new(TorpedoesLaunched: 3, CountermissilesLaunched: 1, PlayerEvasions: 1, ArmorDamageTaken: 7, StructureDamageTaken: 9), SimulationSpeed.Speed2), 1);
    }

    [Fact]
    public void Save18_schema_preserves_captured_projectiles_and_rng()
    {
        var value = Payload(); PointCombatSaveValidation.Validate(value, 100);
        string json = JsonSerializer.Serialize(value);
        var restored = JsonSerializer.Deserialize<PointCombatStateData>(json)!;
        PointCombatSaveValidation.Validate(restored, 100);
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        Assert.Equal(3, restored.Projectiles.Count);
        Assert.Equal(1000.25, restored.Projectiles[1].Flight.DetonationMotionTimeMs);
        Assert.Equal("Captured Name", restored.Projectiles[0].Flight.Reporter!.DisplayName);
        Assert.Equal(45, restored.Evasions[0].Motion.BaseMotion.Direction);
        Assert.Equal(1500.5, restored.Evasions[0].Motion.EndsAtMotionTimeMs);
        Assert.Equal((123UL, 6UL, 456UL, 2UL), (restored.LaunchScatter.State, restored.LaunchScatter.Counter, restored.NpcDodge.State, restored.NpcDodge.Counter));
        Assert.Equal(800, restored.DodgeThreats[0].DueAt);
        Assert.Equal(SimulationSpeed.Speed2, restored.Encounter.ResumeSpeed);
        Assert.Equal((7L, 9L, 1L), (restored.Encounter.Stats.ArmorDamageTaken, restored.Encounter.Stats.StructureDamageTaken, restored.PlayerEvasionEpisodes));
    }

    [Theory]
    [InlineData("deadline")]
    [InlineData("profile")]
    [InlineData("radius")]
    [InlineData("rng-version")]
    [InlineData("rng-counter")]
    [InlineData("duplicate")]
    [InlineData("base-course")]
    [InlineData("dodge")]
    [InlineData("statistics")]
    [InlineData("encounter")]
    public void Save18_schema_rejects_invalid_deadline_profile_or_rng(string defect)
    {
        var p = Payload(); var shot = p.Projectiles[0];
        p = defect switch
        {
            "deadline" => p with { Projectiles = [shot with { Flight = shot.Flight with { DetonationMotionTimeMs = 99 } }] },
            "profile" => p with { Projectiles = [shot with { Flight = shot.Flight with { ProfileId = "" } }] },
            "radius" => p with { Projectiles = [shot with { Flight = shot.Flight with { BlastRadiusKm = 0 } }] },
            "rng-version" => p with { LaunchScatter = p.LaunchScatter with { AlgorithmVersion = 2 } },
            "rng-counter" => p with { LaunchScatter = p.LaunchScatter with { Counter = 3 } },
            "duplicate" => p with { Projectiles = [shot, shot with { ObjectId = shot.ObjectId.ToUpperInvariant() }] },
            "base-course" => p with { Evasions = [p.Evasions[0] with { Motion = p.Evasions[0].Motion with { BaseMotion = new("other", 0, 0, 0, 0) } }] },
            "dodge" => p with { DodgeThreats = [p.DodgeThreats[0] with { Evaded = true }] },
            "statistics" => p with { Encounter = p.Encounter with { Stats = p.Encounter.Stats with { ArmorDamageTaken = -1 } } },
            _ => p with { Encounter = p.Encounter with { State = CombatEncounterState.Won } }
        };
        using var engine = TorpedoImpactTests.Create(); var before = engine.CaptureSaveStateForTests(100, SimulationSpeed.Speed0);
        var corrupt = before with { GameState = before.GameState with { PointCombatState = p } };
        Assert.Contains("Invalid point combat save", Assert.Throws<ScenarioException>(() => engine.LoadScenario(corrupt, true)).Message);
        Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(100, SimulationSpeed.Speed0)));
    }

    [Theory]
    [InlineData("LaunchScatter")]
    [InlineData("NpcDodge")]
    [InlineData("PlayerEvasionEpisodes")]
    public void Required_schema_members_cannot_silently_default(string member)
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(Payload()))!.AsObject(); json.Remove(member);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PointCombatStateData>(json.ToJsonString()));
    }

    [Theory]
    [InlineData("ArmorPiercingPercent")]
    [InlineData("CoreFraction")]
    [InlineData("LaunchMotionTimeMs")]
    public void Captured_zero_capable_parameters_cannot_be_omitted(string member)
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(Payload()))!;
        json["Projectiles"]![0]!["Flight"]!.AsObject().Remove(member);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PointCombatStateData>(json.ToJsonString()));
    }

    [Fact]
    public void Acknowledged_idle_encounter_keeps_completed_statistics_and_end_time()
    {
        var value = Payload(); value = value with { Encounter = value.Encounter with { State = CombatEncounterState.Idle, EndedAt = 90 } };
        PointCombatSaveValidation.Validate(value, 100);
        Assert.Equal(90, JsonSerializer.Deserialize<PointCombatStateData>(JsonSerializer.Serialize(value))!.Encounter.EndedAt);
    }

    [Fact]
    public void Save17_is_rejected_before_deserialization_without_changing_world_or_file()
    {
        using var engine = TorpedoImpactTests.Create(); var before = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        Assert.Equal(18, before.SaveFormatVersion);
        Assert.Equal(3, before.GameState.HostileCombatState!.Version);
        string path = Path.Combine(Path.GetTempPath(), "dss-save17-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, "{\"saveFormatVersion\":17,\"gameState\":\"invalid-old-body\"}"); var bytes = File.ReadAllBytes(path);
            Assert.Contains("incompatible_save_version", Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromFile(path, true, true)).Message);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            Assert.Contains("incompatible_save_version", Assert.Throws<ScenarioException>(() => engine.LoadScenario(before with { SaveFormatVersion = 17 }, true)).Message);
            Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)));
        }
        finally { File.Delete(path); }
    }
}
