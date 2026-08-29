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
    /// World-coordinate target of the active navigation cycle. Dual meaning depending on
    /// which command is active: for <see cref="ShipEngineCommandTypes.Orbit"/> this is a
    /// fixed, permanently-locked world point captured once. For
    /// <see cref="NavigationComputerCommandTypes.Approach"/> this holds the target pose
    /// position captured when the command starts. The client follows the same fixed
    /// fly-through plan and does not extrapolate future target motion.
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
    /// Locked straight-line course for the active navigation cycle (degrees). Dual
    /// meaning depending on which command is active, same convention as
    /// <see cref="NavigationTargetX"/>: for <see cref="ShipEngineCommandTypes.Orbit"/>
    /// this is a permanent lock — once non-null the ship should not re-derive the
    /// bearing, client-side prediction must use NavigationWaypointMath instead of
    /// generic turn steps. For <see cref="NavigationComputerCommandTypes.Approach"/>
    /// this stores the remaining length of the third fly-through segment while
    /// <see cref="NavigationPhase"/> starts with <c>FlyThrough:</c>. Legacy Approach
    /// snapshots may still use the former cycle-scoped course-lock meaning.
    /// </summary>
    double? NavigationLockedCourseDegrees = null,
    string? ObjectType = null,
    string? RelationToPlayer = null,
    string? DisplayName = null,
    string? RenderObjectType = null,
    double? MaxSpeedKmS = null,
    /// <summary>
    /// Current staged navigation phase. Approach uses <c>FlyThroughPending</c> and
    /// <c>FlyThrough:&lt;path type&gt;</c> for its captured pose path.
    /// </summary>
    string? NavigationPhase = null,
    /// <summary>Orbit escape course, or remaining length of Approach fly-through segment 1.</summary>
    double? NavigationEscapeCourseDegrees = null,
    /// <summary>Orbit departure distance, or remaining length of Approach fly-through segment 2.</summary>
    double? NavigationRequiredDepartureDistance = null,
    /// <summary>
    /// True when this object (the player ship) is authoritatively docked to a station
    /// (<see cref="NavigationComputerCommandTypes.Dock"/>). Always false for every other
    /// object. Source of truth is <c>SpaceObjectRuntime.IsDocked</c> — projected onto the
    /// outgoing snapshot row the same way <see cref="ObjectType"/>/<see cref="DisplayName"/> are.
    /// </summary>
    bool IsDocked = false,
    /// <summary>ObjectId of the station this object is docked to. Null unless <see cref="IsDocked"/>.</summary>
    string? DockedStationObjectId = null,
    /// <summary>
    /// Target speed captured when Approach starts. It is metadata only; the target's
    /// future position is not extrapolated.
    /// </summary>
    double? NavigationTargetSpeedKmS = null,
    /// <summary>
    /// Target heading captured when the active Approach command starts; see
    /// <see cref="NavigationTargetSpeedKmS"/>. Null when no Approach cycle is active.
    /// </summary>
    double? NavigationTargetDirectionDegrees = null,
    /// <summary>
    /// Effective distance of the behind-target staging point for an active
    /// <see cref="NavigationComputerCommandTypes.Approach"/>, in world units.
    /// </summary>
    double? NavigationApproachTrailDistanceWorldUnits = null);
