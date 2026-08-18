namespace DeepSpaceSaga.Contracts;

/// <summary>Stable command type IDs for the first player ship bridge navigation computer module.</summary>
public static class NavigationComputerCommandTypes
{
    /// <remarks>
    /// Preconditions (documentation only, not enforced at runtime yet):
    /// Station selected; Station in range &lt; 200; Station speed and direction synchronized.
    /// </remarks>
    public const string Dock = "navigation.dock";

    public const string StationsList = "navigation.stationsList";
}
