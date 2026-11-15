using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>Read-only presentation of the crew roster with their portrait asset ids.</summary>
public static class ShipCrewPanel
{
    /// <summary>Portrait asset ids in roster order; identity is the stable asset id, never the display name.</summary>
    public static IReadOnlyList<string?> PortraitAssetIds(IEnumerable<CrewMemberSnapshot> crew) =>
        crew.Select(c => c.PortraitAssetId).ToArray();
}
