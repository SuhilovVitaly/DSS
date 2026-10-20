using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class PointCombatUS0006TK0002Tests
{
    [Fact]
    public void Own_evasion_schedule_preserves_base_motion_and_fractional_physical_time()
    {
        var state = new ObjectMotionSnapshot("player", 12, 34, 6, 90,
            Evasion: new(1000, 2345.625, "observed-torpedo", EvadeSide.Left, new("player", 0, 0, 2, 0)));
        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(state))!;
        Assert.Equal(2345.625, restored.Evasion!.EndsAtMotionTimeMs);
        Assert.Equal(1000, restored.Evasion.SampleMotionTimeMs);
        Assert.Equal("observed-torpedo", restored.Evasion.ThreatProjectileId);
        Assert.Equal(2, restored.Evasion.BaseMotion.SpeedKmS);
        Assert.Equal(0, restored.Evasion.BaseMotion.Direction);
        Assert.Null(restored.Evasion.BaseMotion.Evasion);
    }

    [Fact]
    public void Absent_evasion_is_not_an_extra_serialized_control_field()
    {
        var state = new ObjectMotionSnapshot("foreign", 0, 0, 1, 0);
        Assert.DoesNotContain("Evasion", JsonSerializer.Serialize(state));
        Assert.Null(JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(state))!.Evasion);
    }
}
