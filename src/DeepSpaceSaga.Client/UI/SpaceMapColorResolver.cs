using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Maps object type and player relation to a marker color.
/// Pure client-side — no Engine dependency.
/// Source of truth: Docs/TacticalMapColors.md.
/// </summary>
internal static class SpaceMapColorResolver
{
    internal static readonly SKColor FallbackColor = new(30, 45, 65); // #1E2D41

    public static SKColor GetColor(string? objectType, string? relationToPlayer)
    {
        return objectType switch
        {
            SpaceObjectType.UnknownSpaceObject => FallbackColor,
            SpaceObjectType.PlayerShip => PlayerShipColor,
            SpaceObjectType.NpcShip => relationToPlayer switch
            {
                PlayerRelation.Enemy => NpcEnemyColor,
                PlayerRelation.Friend => NpcFriendColor,
                _ => NpcNeutralColor
            },
            SpaceObjectType.Asteroid => AsteroidColor,
            SpaceObjectType.Container => ContainerColor,
            SpaceObjectType.Station => StationColor,
            SpaceObjectType.Planet => PlanetColor,
            SpaceObjectType.Sun => SunColor,
            _ => FallbackColor
        };
    }

    // ── Palette (from Docs/TacticalMapColors.md) ────────────────

    internal static readonly SKColor PlayerShipColor = new(85, 107, 47);   // #556B2F DarkOliveGreen
    internal static readonly SKColor NpcNeutralColor = new(169, 169, 169); // #A9A9A9 DarkGray
    internal static readonly SKColor NpcEnemyColor = new(139, 0, 0);       // #8B0000 DarkRed
    internal static readonly SKColor NpcFriendColor = new(46, 139, 87);    // #2E8B57 SeaGreen
    internal static readonly SKColor AsteroidColor = new(245, 245, 245);   // #F5F5F5 WhiteSmoke
    internal static readonly SKColor ContainerColor = new(128, 128, 128);  // #808080 Gray
    internal static readonly SKColor StationColor = new(255, 165, 0);      // #FFA500 Orange
    internal static readonly SKColor PlanetColor = new(245, 245, 245);     // #F5F5F5 WhiteSmoke
    internal static readonly SKColor SunColor = new(255, 165, 0);          // #FFA500 Orange
}
