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
    /// <see cref="NavigationComputerCommandTypes.Approach"/> this instead holds the
    /// trailing aim point as freshly recomputed on the most recently completed ~1s
    /// server cycle (live, re-baked every cycle from the target's current position —
    /// never a fixed lock); see <see cref="NavigationTargetSpeedKmS"/> for the
    /// accompanying baked target velocity used to extrapolate this point forward
    /// client-side between bakes. Null when no navigation cycle is active.
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
    /// (story-20260827-083137.md, Post-implementation bug fix #2) this is instead
    /// cycle-scoped and NOT permanent: <see cref="DeepSpaceSaga.Motion.ApproachPursuitMath.Step"/>
    /// itself drops and re-derives it whenever the live aim point drifts meaningfully,
    /// since (unlike Orbit's fixed point) the aim point genuinely keeps moving as the
    /// target moves. Null when no navigation cycle is active, or not yet locked.
    /// </summary>
    double? NavigationLockedCourseDegrees = null,
    string? ObjectType = null,
    string? RelationToPlayer = null,
    string? DisplayName = null,
    string? RenderObjectType = null,
    double? MaxSpeedKmS = null,
    /// <summary>
    /// Current staged navigation phase for <see cref="ShipEngineCommandTypes.Orbit"/>.
    /// Null means standard approach for saves/snapshots created before staged navigation.
    /// </summary>
    string? NavigationPhase = null,
    /// <summary>Escape course used by the close-target escape phases, degrees.</summary>
    double? NavigationEscapeCourseDegrees = null,
    /// <summary>Required distance from the target before leaving the escape-depart phase.</summary>
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
    /// Target's live speed (km/s), baked in on the most recently completed cycle of the
    /// active <see cref="NavigationComputerCommandTypes.Approach"/> command. Unlike
    /// <see cref="NavigationTargetX"/>/<see cref="NavigationTargetY"/> (Orbit-specific, a
    /// fixed locked point), this field is Approach-specific and is overwritten every cycle
    /// with the target's freshly re-read value so the client can extrapolate the target's
    /// motion without a cross-object lookup. Null when no Approach cycle is active.
    /// </summary>
    double? NavigationTargetSpeedKmS = null,
    /// <summary>
    /// Target's live heading (degrees), baked in on the most recently completed cycle of
    /// the active <see cref="NavigationComputerCommandTypes.Approach"/> command; see
    /// <see cref="NavigationTargetSpeedKmS"/>. Null when no Approach cycle is active.
    /// </summary>
    double? NavigationTargetDirectionDegrees = null,
    /// <summary>
    /// Configured distance of the behind-target staging point for an active
    /// <see cref="NavigationComputerCommandTypes.Approach"/>, in world units.
    /// </summary>
    double? NavigationApproachTrailDistanceWorldUnits = null);
