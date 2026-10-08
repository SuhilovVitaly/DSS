using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class ResourceOrbitBindingTests
{
    [Fact]
    public void ResourceBindingRoundTrip()
    {
        var map = new StationClusterMapSnapshot(1, "c", [], [], [], [new("field", "c", "station", 0.000001, -17.5)]);
        var restored = JsonSerializer.Deserialize<StationClusterMapSnapshot>(JsonSerializer.Serialize(map))!;
        Assert.Equal(map.ResourceBindings.ToArray(), restored.ResourceBindings.ToArray());
        var legacy = JsonSerializer.Deserialize<StationClusterMapSnapshot>("""{"rulesVersion":1,"startClusterId":"c","clusters":[],"stations":[],"links":[]} """)!;
        Assert.True(legacy.ResourceBindings.IsDefaultOrEmpty);
        Assert.True(JsonSerializer.Deserialize<StationClusterMapSnapshot>(JsonSerializer.Serialize(legacy))!.ResourceBindings.IsEmpty);
    }

    [Fact]
    public void BindingDoesNotDuplicateComposition()
    {
        Assert.Equal(new[] { "AnchorStationId", "ClusterId", "FieldId", "OffsetX", "OffsetY" }, typeof(ClusterResourceBinding).GetProperties().Select(p => p.Name).Order(StringComparer.Ordinal));
    }
}
