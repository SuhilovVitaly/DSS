using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class CombatForecastContractTests
{
    [Fact]
    public void Forecast_engine_maneuver_is_optional_and_roundtrips()
    {
        var old = new ObjectMotionSnapshot("ship", 0, 0, 1, 90);
        string json = JsonSerializer.Serialize(old);
        Assert.DoesNotContain("engineManeuver", json);
        Assert.Null(JsonSerializer.Deserialize<ObjectMotionSnapshot>(json)!.EngineManeuver);
        var current = old with { EngineManeuver = new("engine.accelerate", 37, 1000, true, .1, .1, 0) };
        Assert.Equal(current.EngineManeuver, JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(current))!.EngineManeuver);
        var pending = current with
        {
            EngineManeuver = current.EngineManeuver with
            { CycleRemainingMs = 0, CycleIntervalMs = 0, CompletesOnMotionUpdate = true, MotionSampleTimeMs = 37 }
        };
        Assert.Equal(pending.EngineManeuver, JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(pending))!.EngineManeuver);
    }

    [Fact]
    public void Retired_launcher_lifetime_is_absent_from_live_snapshot_schema()
    {
        var old = new InstalledModuleSnapshot("launcher", "torpedo", "Launcher", 0, []);
        string json = JsonSerializer.Serialize(old);
        Assert.DoesNotContain("torpedoMaxFlightTimeMs", json);
        Assert.Null(typeof(InstalledModuleSnapshot).GetProperty("TorpedoMaxFlightTimeMs"));
        string legacy = json[..^1] + ",\"torpedoMaxFlightTimeMs\":1234}";
        var restored = JsonSerializer.Deserialize<InstalledModuleSnapshot>(legacy)!;
        Assert.Equal(old.ModuleId, restored.ModuleId);
        Assert.DoesNotContain("torpedoMaxFlightTimeMs", JsonSerializer.Serialize(restored));
    }
}
