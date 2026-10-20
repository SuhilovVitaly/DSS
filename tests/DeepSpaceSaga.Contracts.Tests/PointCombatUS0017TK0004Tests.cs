using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0017TK0004Tests
{
    [Theory]
    [InlineData(TurnDirection.Left, true)]
    [InlineData(TurnDirection.Right, true)]
    [InlineData(TurnDirection.Left, false)]
    [InlineData(TurnDirection.Right, false)]
    public void Turn_hold_and_summary_ack_roundtrip_with_explicit_direction(TurnDirection direction, bool active)
    {
        var command = new PlayerCommand("hold-1", 7, "player", "engine-1", ShipEngineCommandTypes.TurnHold,
            TurnDirection: direction, Active: active);
        Assert.Equal(command, JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command)));
        var acknowledgement = new PlayerCommand("ack-2", 8, "player", "engine-1", CombatEncounterCommandTypes.AcknowledgeResults);
        var restored = JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(acknowledgement))!;
        Assert.Equal(acknowledgement, restored);
        Assert.Null(restored.TurnDirection);
        Assert.Null(restored.Active);
        Assert.Equal("engine.turnHold", command.CommandType);
        Assert.Equal("combat.acknowledgeResults", restored.CommandType);
    }

    [Fact]
    public void Missing_hold_parameters_remain_missing_instead_of_starting_or_releasing()
    {
        var incomplete = new PlayerCommand("invalid", 9, "player", "engine-1", ShipEngineCommandTypes.TurnHold);
        var restored = JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(incomplete))!;
        Assert.Null(restored.TurnDirection);
        Assert.Null(restored.Active);
        var unknown = incomplete with { TurnDirection = (TurnDirection)99, Active = true };
        Assert.Equal(99, (int)JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(unknown))!.TurnDirection!.Value);
        var rejection = new CommandResult("invalid", "player", "engine-1", ShipEngineCommandTypes.TurnHold,
            CommandResultStatus.Rejected, 1234, CommandReasonCodes.InvalidTurnPayload);
        Assert.Equal("invalid_turn_payload", JsonSerializer.Deserialize<CommandResult>(JsonSerializer.Serialize(rejection))!.ReasonCode);
    }

    [Fact]
    public void Rejected_control_carries_reason_and_actual_speed_without_changing_legacy_shape()
    {
        var rejected = new SimulationControlAcknowledgement(Guid.NewGuid(), 1234, SimulationSpeed.Speed1, false,
            CommandReasonCodes.CombatSpeedLocked);
        var restored = JsonSerializer.Deserialize<SimulationControlAcknowledgement>(JsonSerializer.Serialize(rejected))!;
        Assert.Equal(rejected, restored);
        Assert.Equal("combat_speed_locked", restored.ReasonCode);
        Assert.Equal(SimulationSpeed.Speed1, restored.CurrentSpeed);
        var old = rejected with { ReasonCode = null };
        string json = JsonSerializer.Serialize(old);
        Assert.DoesNotContain("\"ReasonCode\"", json);
        Assert.Null(JsonSerializer.Deserialize<SimulationControlAcknowledgement>(json)!.ReasonCode);
    }
}
