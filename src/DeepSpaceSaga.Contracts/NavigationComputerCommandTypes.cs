namespace DeepSpaceSaga.Contracts;

/// <summary>Stable command type IDs for the first player ship bridge navigation computer module.</summary>
public static class NavigationComputerCommandTypes
{
    /// <remarks>
    /// Preconditions, enforced authoritatively by SimulationEngine.TryStartNavigationCommand:
    /// target must be a Station; distance &lt; the command definition's rangeKm (200 by
    /// default); ship speed and direction must already match the station's. Undock is not
    /// implemented yet — see Docs/FirstRelease/Mechanics/Docking.md.
    /// </remarks>
    public const string Dock = "navigation.dock";

    public const string StationsList = "navigation.stationsList";
}
