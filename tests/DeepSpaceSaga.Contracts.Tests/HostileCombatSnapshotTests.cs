using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class HostileCombatSnapshotTests
{
    [Theory]
    [InlineData(true, HostileCombatPhase.Pursuit)]
    [InlineData(false, HostileCombatPhase.OrbitEntry)]
    [InlineData(true, HostileCombatPhase.OrbitHold)]
    [InlineData(true, HostileCombatPhase.FlankApproach)]
    [InlineData(false, HostileCombatPhase.Retreat)]
    public void Hostile_state_and_motion_segment_roundtrip(bool clockwise, HostileCombatPhase phase)
    {
        var segment = new HostileMotionSegment(12_300, 100, 37, 10.25, -2.5, 0.4, 359.5, 0.1, -12);
        var combat = new HostileCombatSnapshot("player", "engine", "weapon", 100, clockwise, phase, segment);
        var motion = new ObjectMotionSnapshot("enemy", 10.5, -2.4, 0.4037, 359.056, HostileCombat: combat);
        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(motion));
        Assert.Equal(motion with { ModuleHealth = restored!.ModuleHealth }, restored);
        Assert.Empty(restored.ModuleHealth);
        Assert.Equal(clockwise, restored!.HostileCombat!.Clockwise);
        Assert.Equal(1, restored.HostileCombat.Version);
        Assert.Equal(1, restored.HostileCombat.Segment.Version);
        Assert.Equal(12_300, restored.HostileCombat.Segment.EpochMs);
        Assert.Equal(37, restored.HostileCombat.Segment.ElapsedMs);
    }

    [Fact]
    public void Ordinary_motion_has_no_implicit_hostile_controller()
    {
        var motion = JsonSerializer.Deserialize<ObjectMotionSnapshot>(
            """{"ObjectId":"friend","X":0,"Y":0,"SpeedKmS":1,"Direction":90}""");
        Assert.Null(motion!.HostileCombat);
    }
}
