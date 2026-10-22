using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class SpeedHoldContractTests
{
    [Theory]
    [InlineData(SpeedDirection.Accelerate, true, true)]
    [InlineData(SpeedDirection.Brake, true, false)]
    [InlineData(SpeedDirection.Brake, false, false)]
    public void Command_roundtrip_preserves_independent_speed_payload(SpeedDirection direction, bool active, bool latched)
    {
        var command = new PlayerCommand("hold", 1, "player", "engine", ShipEngineCommandTypes.SpeedHold,
            Active: active, SpeedDirection: direction, Latched: latched);
        Assert.Equal(command, JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command)));
        Assert.Equal("engine.speedHold", command.CommandType);
    }

    [Fact]
    public void Ordinary_payloads_omit_speed_fields_and_missing_fields_keep_legacy_defaults()
    {
        var command = new PlayerCommand("turn", 2, "player", "engine", ShipEngineCommandTypes.TurnHold, TurnDirection: TurnDirection.Left, Active: true);
        var json = JsonSerializer.SerializeToElement(command);
        Assert.False(json.TryGetProperty("SpeedDirection", out _)); Assert.False(json.TryGetProperty("Latched", out _));
        var motion = new ObjectMotionSnapshot("player", 0, 0, 3, 0);
        var motionJson = JsonSerializer.SerializeToElement(motion);
        Assert.False(motionJson.TryGetProperty("SpeedHoldAccelerationKmS2", out _)); Assert.False(motionJson.TryGetProperty("SpeedHoldLatched", out _));
        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(motionJson)!;
        Assert.Null(restored.SpeedHoldAccelerationKmS2); Assert.False(restored.SpeedHoldLatched);
    }

    [Fact]
    public void Combined_motion_roundtrip_retains_both_axes_and_latch()
    {
        var motion = new ObjectMotionSnapshot("player", 1, 2, 3, 4, TurnHoldRateDegPerSec: 20, SpeedHoldAccelerationKmS2: .6, SpeedHoldLatched: true);
        var restored = JsonSerializer.Deserialize<ObjectMotionSnapshot>(JsonSerializer.Serialize(motion))!;
        Assert.Equal(motion.ObjectId, restored.ObjectId);
        Assert.Equal(motion.X, restored.X); Assert.Equal(motion.Y, restored.Y);
        Assert.Equal(motion.SpeedKmS, restored.SpeedKmS); Assert.Equal(motion.Direction, restored.Direction);
        Assert.Equal(20, restored.TurnHoldRateDegPerSec); Assert.Equal(.6, restored.SpeedHoldAccelerationKmS2); Assert.True(restored.SpeedHoldLatched);
    }
}
