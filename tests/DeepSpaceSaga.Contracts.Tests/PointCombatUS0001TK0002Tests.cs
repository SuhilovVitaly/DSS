using System.Reflection;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0001TK0002Tests
{
    [Theory]
    [InlineData(CombatCommandTypes.Launch, EvadeSide.Left)]
    [InlineData(DefenseCommandTypes.Launch, EvadeSide.Right)]
    [InlineData(CombatCommandTypes.Detonate, EvadeSide.Right)]
    [InlineData(ManeuverCommandTypes.Evade, EvadeSide.Left)]
    public void Commands_roundtrip_preserves_explicit_point_profile_and_threat(string type, EvadeSide side)
    {
        var command = new PlayerCommand("command-71", 72, "player", "launcher-2", type,
            TargetObjectId: "own-projectile-1", TargetWorldX: -132.125, TargetWorldY: 880.5,
            ProfileId: "medium", ThreatProjectileId: "enemy-projectile-3", EvadeSide: side);

        var restored = JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command));

        Assert.Equal(command, restored);
        Assert.Equal(-132.125, restored!.TargetWorldX);
        Assert.Equal(880.5, restored.TargetWorldY);
        Assert.Equal("medium", restored.ProfileId);
        Assert.Equal("enemy-projectile-3", restored.ThreatProjectileId);
        Assert.Equal(side, restored.EvadeSide);
        Assert.Equal("own-projectile-1", restored.TargetObjectId);
    }

    [Fact]
    public void Reason_codes_are_unique_and_stable()
    {
        string[] expected = ["missing_profile", "unknown_profile", "launcher_reloading", "no_weapon_operator",
            "target_out_of_range", "invalid_time", "projectile_id_exhausted", "not_own_projectile",
            "stale_projectile", "unknown_threat", "invalid_evade_side", "engine_disabled"];
        string[] actual = [CommandReasonCodes.MissingProfile, CommandReasonCodes.UnknownProfile,
            CommandReasonCodes.LauncherReloading, CommandReasonCodes.NoWeaponOperator,
            CommandReasonCodes.LaunchOutOfRange, CommandReasonCodes.InvalidTime,
            CommandReasonCodes.ProjectileIdExhausted, CommandReasonCodes.NotOwnProjectile,
            CommandReasonCodes.StaleProjectile, CommandReasonCodes.UnknownThreat,
            CommandReasonCodes.InvalidEvadeSide, CommandReasonCodes.EngineDisabled];
        Assert.Equal(expected, actual);
        var all = typeof(CommandReasonCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!).ToArray();
        Assert.Equal(all.Length, all.Distinct(StringComparer.Ordinal).Count());

        foreach (string reason in actual)
        {
            var rejection = new CommandResult("failed-launch", "player", "launcher-2", CombatCommandTypes.Launch,
                CommandResultStatus.Rejected, 1250, reason);
            var restored = JsonSerializer.Deserialize<CommandResult>(JsonSerializer.Serialize(rejection))!;
            Assert.Equal(CommandResultStatus.Rejected, restored.Status);
            Assert.Equal(reason, restored.ReasonCode);
            Assert.Null(restored.TradeReceipt);
        }
    }

    [Fact]
    public void New_command_ids_are_stable_and_omitted_parameters_remain_explicitly_missing()
    {
        Assert.Equal("torpedo.launch", CombatCommandTypes.Launch);
        Assert.Equal("torpedo.detonate", CombatCommandTypes.Detonate);
        Assert.Equal("defense.launch", DefenseCommandTypes.Launch);
        Assert.Equal("maneuver.evade", ManeuverCommandTypes.Evade);
        var command = new PlayerCommand("missing", 1, "player", "engine", ManeuverCommandTypes.Evade);
        var restored = JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command))!;
        Assert.Null(restored.TargetWorldX);
        Assert.Null(restored.TargetWorldY);
        Assert.Null(restored.ProfileId);
        Assert.Null(restored.ThreatProjectileId);
        Assert.Null(restored.EvadeSide);
    }
}
