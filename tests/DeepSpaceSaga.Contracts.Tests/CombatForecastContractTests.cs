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
    public void Forecast_launcher_lifetime_is_optional_and_roundtrips_without_changing_legacy_payloads()
    {
        var old = new InstalledModuleSnapshot("launcher", "torpedo", "Launcher", 0, []);
        string json = JsonSerializer.Serialize(old);
        Assert.DoesNotContain("torpedoMaxFlightTimeMs", json);
        Assert.Null(JsonSerializer.Deserialize<InstalledModuleSnapshot>(json)!.TorpedoMaxFlightTimeMs);
        var current = old with { TorpedoMaxFlightTimeMs = 1234 };
        string currentJson = JsonSerializer.Serialize(current);
        Assert.Contains("\"torpedoMaxFlightTimeMs\":1234", currentJson);
        Assert.Equal(1234, JsonSerializer.Deserialize<InstalledModuleSnapshot>(currentJson)!.TorpedoMaxFlightTimeMs);
    }
}
