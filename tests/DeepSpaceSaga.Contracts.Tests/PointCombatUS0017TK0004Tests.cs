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
    public void Turn_hold_roundtrip_with_explicit_direction(TurnDirection direction, bool active)
    {
        var command = new PlayerCommand("hold-1", 7, "player", "engine-1", ShipEngineCommandTypes.TurnHold,
            TurnDirection: direction, Active: active);
        Assert.Equal(command, JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command)));
        Assert.Equal("engine.turnHold", command.CommandType);
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
}
