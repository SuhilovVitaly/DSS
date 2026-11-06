using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0011TK0004Tests
{
    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Every_point_launch_reason_is_localized_in_source_and_packaged_locale(string language)
    {
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", "Data", "Locale", language + ".json"));
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(source))!;
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Locale", language + ".json")));
        string[] reasons = [CommandReasonCodes.UnknownObject, CommandReasonCodes.UnknownModule, CommandReasonCodes.UnknownCommandType,
            CommandReasonCodes.ModuleUnavailable, CommandReasonCodes.ModuleDisabled, CommandReasonCodes.ModuleDestroyed, CommandReasonCodes.Busy,
            CommandReasonCodes.LauncherReloading, CommandReasonCodes.MissingProfile, CommandReasonCodes.UnknownProfile, CommandReasonCodes.InvalidTargetCoordinates,
            CommandReasonCodes.NoWeaponOperator, CommandReasonCodes.LaunchOutOfRange, CommandReasonCodes.InvalidTime, CommandReasonCodes.ProjectileIdExhausted,
            CommandReasonCodes.SessionDefeated, "dialogue_active", "player_destroyed", "connection_unavailable", "send_failed", "command_rejected", "friendly_fire_confirmation"];
        foreach (string reason in reasons)
        { Assert.True(strings.TryGetValue("Combat.Reason." + reason, out var text), reason); Assert.False(string.IsNullOrWhiteSpace(text)); Assert.NotEqual(reason, text); }
        Assert.True(strings.ContainsKey("Combat.CommandRejected"));
    }
}
