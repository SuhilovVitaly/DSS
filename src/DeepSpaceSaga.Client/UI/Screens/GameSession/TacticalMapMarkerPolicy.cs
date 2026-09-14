using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Full-detail tactical marker sizes and styles. The map view applies
/// compact markers and clustering separately; contacts never disappear by type.
/// Pure client-side policy, no Skia dependencies.
/// </summary>
internal static class TacticalMapMarkerPolicy
{
    /// <summary>Marker size for regular objects (screen px).</summary>
    public const float RegularMarkerSizePx = 10f;

    /// <summary>Marker size for planets (screen px).</summary>
    public const float PlanetMarkerSizePx = 25f;

    /// <summary>Marker size for the sun (screen px).</summary>
    public const float SunMarkerSizePx = 50f;


    /// <summary>
    /// Marker size in screen px for a given client-visible render type.
    /// UnknownSpaceObject/null resolve to the regular 10 px marker.
    /// </summary>
    public static float GetMarkerSizePx(string? renderObjectType)
    {
        return renderObjectType switch
        {
            SpaceObjectType.Sun => SunMarkerSizePx,
            SpaceObjectType.Planet => PlanetMarkerSizePx,
            _ => RegularMarkerSizePx
        };
    }

    /// <summary>
    /// Marker radius in screen px (= size / 2). Zoom-independent —
    /// the zoom level is deliberately not a parameter.
    /// </summary>
    public static float GetMarkerRadiusPx(string? renderObjectType)
    {
        return GetMarkerSizePx(renderObjectType) / 2f;
    }

    /// <summary>
    /// Whether the given client-visible render type should draw as a soft
    /// glinting point of light (bright core + fading halo, no directional
    /// shading) instead of the default shaded-sphere marker. True for
    /// unresolved sensor contacts (UnknownSpaceObject) and asteroids;
    /// false — including for null — for every other type.
    /// </summary>
    public static bool UsesGlintMarker(string? renderObjectType)
    {
        return renderObjectType is SpaceObjectType.UnknownSpaceObject or SpaceObjectType.Asteroid;
    }
}
