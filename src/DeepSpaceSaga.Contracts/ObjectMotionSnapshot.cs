namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Minimal motion DTO for the render/prediction pipeline.
/// Speed in km/s. Direction in degrees: 0° = up, 90° = right, clockwise.
/// Active engine cycle metadata allows the client to predict discrete turns
/// with the same step timing as the authoritative simulation.
/// World coordinates: 1 unit = 100 m, Sun at (0, 0).
/// </summary>
/// <param name="MaxSpeedKmS">km/s, null if the object has no active engine module</param>
public sealed record ObjectMotionSnapshot(
    string ObjectId,
    double X,          // world units
    double Y,          // world units
    double SpeedKmS,   // km/s
    double Direction,  // degrees, 0° = up, clockwise
    string? ActiveEngineCommandType = null,
    int TurnStepDegrees = 0,
    long TurnStepRemainingMs = 0,
    long TurnStepIntervalMs = 0,
    /// <summary>
    /// World-coordinate target of the active navigation cycle
    /// (<see cref="ShipEngineCommandTypes.NavigateToPoint"/>), world units.
    /// Null when no navigation cycle is active.
    /// </summary>
    double? NavigationTargetX = null,
    /// <summary>World-coordinate target of the active navigation cycle; see <see cref="NavigationTargetX"/>.</summary>
    double? NavigationTargetY = null,
    /// <summary>
    /// Angular inertia of the engine module running the navigation cycle (degrees per
    /// second) — the client projects the same deterministic step math with it.
    /// 0 when no navigation cycle is active.
    /// </summary>
    int NavigationAngularInertiaDegPerSec = 0,
    /// <summary>
    /// Locked straight-line course for the active navigation cycle (degrees).
    /// When non-null the ship has locked onto a course and should not turn further —
    /// client-side prediction must use NavigationWaypointMath instead of generic turn steps.
    /// </summary>
    double? NavigationLockedCourseDegrees = null,
    string? ObjectType = null,
    string? RelationToPlayer = null,
    string? DisplayName = null,
    string? RenderObjectType = null,
    double? MaxSpeedKmS = null,
    /// <summary>
    /// Current staged navigation phase for <see cref="ShipEngineCommandTypes.NavigateToPoint"/>.
    /// Null means standard approach for saves/snapshots created before staged navigation.
    /// </summary>
    string? NavigationPhase = null,
    /// <summary>Escape course used by the close-target escape phases, degrees.</summary>
    double? NavigationEscapeCourseDegrees = null,
    /// <summary>Required distance from the target before leaving the escape-depart phase.</summary>
    double? NavigationRequiredDepartureDistance = null);
