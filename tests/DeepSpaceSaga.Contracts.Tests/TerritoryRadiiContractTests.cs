using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class TerritoryRadiiContractTests
{
    [Fact]
    public void OverlappingTerritoriesRetainOwners()
    {
        var map = new AiMapEnvironmentSnapshot(1, [], [new("area-1", "base-1", 200.5, 1000.25), new("area-2", "base-2", 2, 2)]);
        var json = JsonSerializer.Serialize(map);
        var restored = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(200.5, doc.RootElement.GetProperty("territories")[0].GetProperty("defenceRadiusKm").GetDouble());
        Assert.Equal("base-1", restored.Territories[0].BaseObjectId);
        Assert.Equal(2, restored.Territories.Length);
        Assert.DoesNotContain(typeof(TerritoryMapData).GetProperties(), p => p.Name is "X" or "Y" or "Damage");
    }

    [Fact]
    public void LegacyAiSnapshotDefaultsTerritories()
    {
        var legacy = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>("""{"rulesVersion":1,"bases":[]}""")!;
        Assert.True(legacy.Territories.IsDefaultOrEmpty);
        var restored = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>(JsonSerializer.Serialize(new AiMapEnvironmentSnapshot(1, default)))!;
        Assert.True(restored.Territories.IsEmpty);
    }
}
