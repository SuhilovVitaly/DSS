using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.TacticalBattle;

/// <summary>Only structural definitions from V4 Spec, never tunable content values.</summary>
internal static class TacticalBattleConstants
{
    // Combat §1, §6.2–6.3 and Generation §3.2, §3.4, §2.
    public const double RearHeadingOffset = 180.0;
    public const double ScreenFlankOffset = 90.0;
    public const double AimErrorMultiplier = 2.0;
    public const double Percent = 100.0;
    public const int PursuitDraws = 2;
    public const int DefaultShieldSlots = 1;
    public const int DefaultShieldCount = 1;
    public const double IntactEngineFactor = 1.0;
    public const string CarrierFrame = "carrier";
    public const string InitialRole = "fighter";
    public const string InitialStrikeMode = "in";
    public static readonly ImmutableArray<string> RoleOrder = ["striker", "interceptor", "fighter"];
    public static readonly ImmutableArray<string> SideOrder = ["A", "B"];

    // Generation §1: mulberry32 structural arithmetic.
    public const uint RandomIncrement = 0x6D2B79F5;
    public const double RandomDivisor = 4294967296.0;
}
