using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class PoiContractTests
{
    [Fact]
    public void PoiDescriptionAndAnchorRoundTrip()
    {
        var orbit = new OrbitalElements(100, 90, 70000, 12, 0.25, 100, "counterclockwise");
        var map = new AiMapEnvironmentSnapshot(1, [], PointsOfInterest:
            [new("relay", "Ретранслятор", "Неактивный узел связи.", "planet", null, 1.25, -2.5),
             new("platform", "Платформа", "Нет доступных действий.", null, orbit, 0, 0)]);
        string json = JsonSerializer.Serialize(map);
        var restored = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(restored)); Assert.Equal(map.PointsOfInterest.ToArray(), restored.PointsOfInterest.ToArray());
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Неактивный узел связи.", document.RootElement.GetProperty("pointsOfInterest")[0].GetProperty("description").GetString());
        Assert.Equal(1.25, restored.PointsOfInterest[0].OffsetX); Assert.Equal(orbit, restored.PointsOfInterest[1].Orbit);
        Assert.Equal(new[] { "ObjectId", "Name", "Description", "ParentObjectId", "Orbit", "OffsetX", "OffsetY" }.Order(),
            typeof(PointOfInterestData).GetProperties().Select(p => p.Name).Order());
    }

    [Fact]
    public void LegacyMapHasEmptyPoi()
    {
        var legacy = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>("""{"rulesVersion":1,"bases":[]}""")!;
        Assert.True(legacy.PointsOfInterest.IsDefaultOrEmpty);
        Assert.True(JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>(JsonSerializer.Serialize(legacy))!.PointsOfInterest.IsEmpty);
    }
}
