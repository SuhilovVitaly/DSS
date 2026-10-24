using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Maps object type and player relation to a marker color.
/// Pure client-side — no Engine dependency.
/// Source of truth: DB/Documentation/03-Design/TacticalMapSpecification.md.
/// </summary>
internal static class SpaceMapColorResolver
{
    internal static readonly SKColor FallbackColor = new(165, 175, 196, 230);

    public static SKColor GetColor(string? objectType, string? relationToPlayer)
    {
        return objectType switch
        {
            SpaceObjectType.UnknownSpaceObject => FallbackColor,
            SpaceObjectType.Missile => TacticalMapPalette.Torpedo,
            SpaceObjectType.Countermeasure => TacticalMapPalette.Countermeasure,
            SpaceObjectType.Explosion => TacticalMapPalette.Explosion,
            SpaceObjectType.Wreck => TacticalMapPalette.Wreck,
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

    // ── Palette (from DB/Documentation/03-Design/TacticalMapSpecification.md) ─────────

    internal static readonly SKColor PlayerShipColor = new(140, 185, 224);
    internal static readonly SKColor NpcNeutralColor = new(186, 196, 206);
    internal static readonly SKColor NpcEnemyColor = new(240, 128, 136);
    internal static readonly SKColor NpcFriendColor = new(101, 201, 165);
    internal static readonly SKColor AsteroidColor = new(165, 180, 194, 217);
    internal static readonly SKColor ContainerColor = new(180, 154, 224, 230);
    internal static readonly SKColor StationColor = new(201, 173, 125);
    internal static readonly SKColor PlanetColor = new(201, 173, 125, 230);
    internal static readonly SKColor SunColor = new(255, 209, 138);
}

/// <summary>Client-only semantic roles; alpha rounds once from percentage, never HSL.</summary>
internal static class TacticalMapPalette
{
    internal static readonly SKColor Torpedo = new(237, 184, 106);
    internal static readonly SKColor Countermeasure = new(118, 213, 220);
    internal static readonly SKColor Preview = new(183, 171, 223, 140);
    internal static readonly SKColor Label = new(220, 230, 238, 242);
    internal static readonly SKColor Annotation = new(220, 230, 238, 230);
    internal static readonly SKColor LabelBackground = new(21, 32, 43, 235);
    internal static readonly SKColor HpEmpty = new(35, 44, 52);
    internal static readonly SKColor Explosion = new(238, 167, 122);
    internal static readonly SKColor Wreck = new(124, 137, 152, 166);
    internal static readonly SKColor OtherTrail = new(151, 168, 184);
    internal static readonly SKColor Target = new(255, 209, 138, 230);

    internal static byte Alpha(double percent) => (byte)Math.Floor(255 * Math.Clamp(percent, 0, 100) / 100 + .5);
    internal static SKColor Trail(SKColor color, double fraction, double near = 35, double far = 5) =>
        color.WithAlpha(Alpha(far + (near - far) * Math.Clamp(fraction, 0, 1)));
}
