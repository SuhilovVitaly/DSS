namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>
/// Single source of truth for the carrier's visual identity across the world ship and the tactical
/// battle. The world <c>AppearanceId</c> maps to one stable resource key; a missing appearance asset
/// falls back to the default carrier model without losing the ship's name.
/// </summary>
public static class CarrierAppearance
{
    public const string DefaultResourceKey = "carrier";

    /// <summary>Stable resource key shared by the world ship and the tactical carrier.</summary>
    public static string ResourceKey(string? appearanceId) =>
        string.IsNullOrWhiteSpace(appearanceId) ? DefaultResourceKey : appearanceId;

    /// <summary>Fallback resource key when the appearance asset is missing; the ship name is never derived from it.</summary>
    public static string FallbackKey(string? appearanceId) => DefaultResourceKey;

    /// <summary>Rotation frame index (the 32-frame wheel shared with the tactical ship sheet).</summary>
    public static int Frame(double course)
    {
        double angle = (course % 360 + 360) % 360;
        return (int)Math.Round((angle + 90) / 11.25) % 32;
    }
}
