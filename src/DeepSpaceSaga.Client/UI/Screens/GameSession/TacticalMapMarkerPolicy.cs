using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Single source of truth for tactical-map marker sizes and scale
/// visibility (ТЗ-10, §39/§39.1). Marker sizes are screen-space pixels,
/// zoom-independent — no zoom parameter exists by design.
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

    /// <summary>Scale threshold: at PPU ≥ 1.0 (M0.5/M1) every type is rendered.</summary>
    public const double FullVisibilityPpuThreshold = 1.0;

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
    /// Whether an object with the given client-visible render type should
    /// appear in the render list at the given zoom (pixels per world unit).
    /// At PPU ≥ 1.0 (Small/Combat scale) all 7 types are visible; at lower
    /// PPU (Medium/Large/System) only Sun/Planet/Station/PlayerShip remain.
    /// null resolves like UnknownSpaceObject (hidden at PPU &lt; 1.0).
    /// Client-side filter only — hidden objects keep existing in the engine.
    /// </summary>
    public static bool ShouldRenderAtScale(string? renderObjectType, double pixelsPerWorldUnit)
    {
        if (pixelsPerWorldUnit >= FullVisibilityPpuThreshold)
            return true;

        return renderObjectType is SpaceObjectType.Sun
            or SpaceObjectType.Planet
            or SpaceObjectType.Station
            or SpaceObjectType.PlayerShip;
    }
}
