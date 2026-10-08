using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class EnvironmentFieldContractTests
{
    [Fact]
    public void FieldGeometryAndAnchorRoundTrip()
    {
        var orbit = new OrbitalElements(100, 99, 80000, 12, 0.5, 400, "clockwise");
        var map = new AiMapEnvironmentSnapshot(1, [], Fields:
            [new("r", "Radiation", 0.5, "Sun", null, null, 0, 0, 0, 50000, 0, 360, ulong.MaxValue),
             new("d", "Dust", 0.25, "Orbit", null, orbit, 1.25, 2.5, 10, 20, 350, 30, 123),
             new("b", "Debris", 1, "Parent", "planet", null, 3, 4, 0, 2500, 0, 360, 1)]);
        string json = JsonSerializer.Serialize(map);
        var restored = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>(json)!;
        Assert.Equal(json, JsonSerializer.Serialize(restored));
        using var document = JsonDocument.Parse(json);
        var field = document.RootElement.GetProperty("fields")[1];
        Assert.Equal(1.25, field.GetProperty("offsetX").GetDouble());
        Assert.Equal(350, field.GetProperty("startAngleDegrees").GetDouble());
        Assert.Equal(ulong.MaxValue, restored.Fields[0].DecorationSeed);
        Assert.Equal(orbit, restored.Fields[1].Orbit);
    }

    [Fact]
    public void NoGameplayModifiersInFieldContract()
    {
        var legacy = JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>("""{"rulesVersion":1,"bases":[]}""")!;
        Assert.True(legacy.Fields.IsDefaultOrEmpty);
        Assert.True(JsonSerializer.Deserialize<AiMapEnvironmentSnapshot>(JsonSerializer.Serialize(legacy))!.Fields.IsEmpty);
        Assert.Equal(new[] { "Id", "Kind", "Intensity", "AnchorKind", "ParentObjectId", "Orbit", "OffsetX", "OffsetY", "InnerRadius", "OuterRadius", "StartAngleDegrees", "SweepDegrees", "DecorationSeed" }.Order(),
            typeof(EnvironmentFieldData).GetProperties().Select(p => p.Name).Order());
    }
}
