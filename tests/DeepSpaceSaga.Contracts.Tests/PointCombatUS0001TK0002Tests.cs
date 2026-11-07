using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0001TK0002Tests
{
    [Fact]
    public void Orbit_coordinates_and_current_rejection_roundtrip_without_retired_payload()
    {
        var command = new PlayerCommand("orbit", 72, "player", "engine", ShipEngineCommandTypes.Orbit,
            TargetWorldX: -132.125, TargetWorldY: 880.5);
        string json = JsonSerializer.Serialize(command);
        foreach (string name in new[] { "ModeId", "ProfileId", "ThreatProjectileId", "EvadeSide" })
            Assert.DoesNotContain($"\"{name}\"", json);
        Assert.Equal(command, JsonSerializer.Deserialize<PlayerCommand>(json));
        var rejection = new CommandResult("orbit", "player", "engine", ShipEngineCommandTypes.Orbit,
            CommandResultStatus.Rejected, 1250, CommandReasonCodes.InvalidTargetCoordinates);
        Assert.Equal(rejection, JsonSerializer.Deserialize<CommandResult>(JsonSerializer.Serialize(rejection)));
        var all = typeof(CommandReasonCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!).ToArray();
        Assert.Equal(all.Length, all.Distinct(StringComparer.Ordinal).Count());
    }
}
