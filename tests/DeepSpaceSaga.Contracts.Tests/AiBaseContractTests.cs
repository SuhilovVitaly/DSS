using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class AiBaseContractTests
{
    [Fact]
    public void AiBaseJsonRoundTrip()
    {
        var orbit = new OrbitalElements(1234.5, 1234.5, 987654, 359, 0.25, 123, "clockwise");
        var map = new AiMapEnvironmentSnapshot(1,
            [new("SYS-AI-1", "Planetary", "Ai", "SYS-PLANET-1", null, 0, 0),
             new("SYS-AI-2", "Orbital", "Ai", null, orbit, 0, 0)]);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], AiMap: map);
        var json = JsonSerializer.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);
        var bases = document.RootElement.GetProperty("aiMap").GetProperty("bases");
        Assert.Equal("Ai", bases[0].GetProperty("owner").GetString());
        Assert.Equal("SYS-PLANET-1", bases[0].GetProperty("parentObjectId").GetString());
        Assert.Equal(JsonValueKind.Null, bases[0].GetProperty("orbit").ValueKind);
        Assert.Equal(JsonValueKind.Null, bases[1].GetProperty("parentObjectId").ValueKind);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        Assert.Equal(map.Bases.ToArray(), restored.AiMap!.Bases.ToArray());
        var defaults = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>(
            JsonSerializer.Serialize(new AiMapEnvironmentSnapshot(1, default)))!;
        Assert.True(defaults.Bases.IsEmpty);
        const string legacy = """{"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,"Objects":[]}""";
        Assert.Null(JsonSerializer.Deserialize<AuthoritativeSnapshot>(legacy)!.AiMap);
    }

    [Fact]
    public void MarketProfileDoesNotDefineOwner()
    {
        var humanMap = new StationClusterMapSnapshot(1, "home",
            [new("home", "Home", "belt", ["human-science"], "scientific-military")],
            [new("human-science", "home", "scientific-military")], []);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], ClusterMap: humanMap,
            AiMap: new(1, [new("ai-base", "Planetary", "Ai", "planet", null, 0, 0)]));
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal("scientific-military", restored.ClusterMap!.Stations[0].MarketProfileId);
        Assert.DoesNotContain(restored.AiMap!.Bases, b => b.ObjectId == "human-science");
        Assert.Equal("Ai", Assert.Single(restored.AiMap.Bases).Owner);
    }
}
