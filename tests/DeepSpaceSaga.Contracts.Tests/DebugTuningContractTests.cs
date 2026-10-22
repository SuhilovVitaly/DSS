using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class DebugTuningContractTests
{
    [Fact]
    public void Patch_roundtrip_retains_multiple_fields_and_explicit_profile()
    {
        var command = new PlayerCommand("debug-1", 1, "player", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([
                new("module.engine.tetrarch", "maxSpeedKmS", 5.5),
                new("module.torpedo.launcher.basic", "blastRadiusKm", 2.5, "fast")]));
        var restored = JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command))!;
        Assert.Equal(command.CommandType, restored.CommandType);
        Assert.Equal(command.DebugTuning!.Values.ToArray(), restored.DebugTuning!.Values.ToArray());
    }

    [Fact]
    public void Optional_debug_payload_and_profile_are_absent_when_null()
    {
        var command = new PlayerCommand("ordinary", 2, "player", "engine", "engine.brake");
        Assert.False(JsonSerializer.SerializeToElement(command).TryGetProperty("DebugTuning", out _));
        Assert.False(JsonSerializer.SerializeToElement(new DebugTuningValue("engine", "reloadMs", 100))
            .TryGetProperty("ProfileId", out _));
    }

    [Fact]
    public void Snapshot_roundtrip_preserves_revision_values_and_original_defaults()
    {
        var snapshot = new DebugTuningSnapshot(7,
            [new("module.engine.tetrarch", "maxSpeedKmS", 2)],
            [new("module.engine.tetrarch", "maxSpeedKmS", 5)]);
        var restored = JsonSerializer.Deserialize<DebugTuningSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(7, restored.Revision);
        Assert.Equal(2, Assert.Single(restored.Values).Value);
        Assert.Equal(5, Assert.Single(restored.Defaults).Value);
    }
}
