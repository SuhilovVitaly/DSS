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

    /// <remarks>
    /// Physically an Engine command (registered under module.engine's commandTypeIds,
    /// not the Navigation Computer's), despite the name/namespace — same pattern as
    /// <see cref="ShipEngineCommandTypes.Orbit"/>. Steers the ship to a point trailing
    /// behind the selected object along its current heading, re-aiming every ~1s from
    /// the object's live speed/direction, and completes by snapping the ship's speed
    /// and direction to the target's exactly (priming it for <see cref="Dock"/>). See
    /// DeepSpaceSaga.Motion.ApproachPursuitMath for the shared steering math and
    /// SimulationEngine (DeepSpaceSaga.Engine) for the command lifecycle.
    /// </remarks>
    public const string Approach = "navigation.approach";
}
