using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Combat commands addressed by owner object, launcher module and explicit target.</summary>
public static class CombatCommandTypes
{
    public const string Fire = "torpedo.fire";
}

/// <summary>
/// Player-visible authoritative hull health in integer hit points. ShipClassId is an
/// explicit content identity, independent of sprite/name. The Engine owns damage and masking.
/// </summary>
public sealed record HullCombatSnapshot(string ShipClassId, int CurrentHp, int MaxHp);

/// <summary>
/// Captured launcher parameters: speed in km/s, positive maximum turn rate in degrees/s,
/// damage in integer hull hit points. ActiveTorpedoObjectId is null when no projectile is
/// active; module power/availability is still checked separately by the Engine.
/// </summary>
public sealed record LauncherCombatSnapshot(
    string? ActiveTorpedoObjectId,
    double SpeedKmS,
    double TurnRateDegPerSec,
    int Damage);

/// <summary>
/// Immutable confirmed torpedo flight. The Engine produces this state; the DTO performs
/// no guidance, damage, RNG draws or UI animation. Coordinates/distances use world units
/// (1 unit = 100 m). Speed is the projectile's own km/s, without carrier velocity.
/// </summary>
/// <param name="OwnerObjectId">Launching ship identity, excluded from this projectile's contacts.</param>
/// <param name="LauncherModuleId">Launching module identity on the owner.</param>
/// <param name="TargetObjectId">Original commanded target, retained even if selection changes or the target is lost.</param>
/// <param name="LaunchMotionTimeMs">Absolute physical MotionTimeMs at launch, not calendar or UI time.</param>
/// <param name="SpeedKmS">Captured constant projectile speed in km/s.</param>
/// <param name="TurnRateDegPerSec">Captured positive maximum angular speed in degrees/s.</param>
/// <param name="Damage">Captured damage in integer hull hit points.</param>
/// <param name="DistanceTravelledWorldUnits">Authoritative cumulative distance since launch, in world units.</param>
/// <param name="Route">Current confirmed plan, including its execution phase and intercept status.</param>
/// <param name="Trail">Complete executed analytic segments in chronological order, not a render polyline.</param>
/// <param name="PredictedImpactMotionTimeMs">
/// Absolute physical MotionTimeMs of the predicted encounter; null when no intercept is
/// known (including a lost target). Null must not be replaced with zero or calendar time.
/// </param>
/// <param name="HitChancePercent">100 for the MVP: chance on contact, not a promise to ignore obstacles.</param>
public sealed record TorpedoSnapshot(
    string OwnerObjectId,
    string LauncherModuleId,
    string TargetObjectId,
    long LaunchMotionTimeMs,
    double SpeedKmS,
    double TurnRateDegPerSec,
    int Damage,
    double DistanceTravelledWorldUnits,
    TorpedoRoute Route,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TrailSegment>))]
    ImmutableArray<TrailSegment> Trail = default,
    long? PredictedImpactMotionTimeMs = null,
    int HitChancePercent = 100);

/// <summary>Current execution phase, independent of whether the route predicts an intercept.</summary>
public enum TorpedoRoutePhase
{
    Turning,
    Straight,
    Complete
}

/// <summary>
/// Confirmed world-space route. Segments are ordered from the plan's origin and retain
/// their full geometry as ElapsedMs advances. No-intercept pursuit may still have segments;
/// HasIntercept=false does not mean the projectile has stopped or the launcher is free.
/// Target loss can likewise leave a straight continuation without a predicted encounter.
/// </summary>
/// <param name="StartMotionTimeMs">Absolute MotionTimeMs of this plan's origin (may be later than launch after replanning).</param>
/// <param name="PlannerVersion">Version of the shared Motion planner that produced the geometry.</param>
/// <param name="Phase">Authoritative current execution phase, separate from geometric segments.</param>
/// <param name="HasIntercept">Whether this plan reaches the target; false requires a null predicted impact time.</param>
/// <param name="Segments">Analytic segments in traversal order; default serializes as an empty array.</param>
/// <param name="ElapsedMs">Physical milliseconds since the plan's origin; fractional values preserve sub-step geometry.</param>
public sealed record TorpedoRoute(
    long StartMotionTimeMs,
    int PlannerVersion,
    TorpedoRoutePhase Phase,
    bool HasIntercept,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TorpedoRouteSegment>))]
    ImmutableArray<TorpedoRouteSegment> Segments = default,
    double ElapsedMs = 0);

/// <summary>
/// One analytic constant-speed straight or circular-arc segment. X/Y are the starting
/// world coordinates (1 unit = 100 m). Direction is the starting heading in degrees:
/// 0 = up, 90 = right, clockwise. AngularVelocityDegPerSec is signed: positive turns
/// clockwise, negative counterclockwise, zero is straight. DurationMs is a finite,
/// nonnegative physical duration, with fractional milliseconds allowed. Geometry is
/// evaluated by shared Motion, never by this DTO; no pixel/render sampling is stored.
/// </summary>
public sealed record TorpedoRouteSegment(
    double X,
    double Y,
    double Direction,
    double SpeedKmS,
    double AngularVelocityDegPerSec,
    double DurationMs);

/// <summary>
/// An executed portion of flight, in chronological order from launch. StartMotionTimeMs
/// is absolute physical time, allowing fractional milliseconds at analytic boundaries.
/// Segment.DurationMs ends at the actually reached pose (including a contact), not at
/// a future planned endpoint. PlannerVersion identifies the geometry's producing planner;
/// later replans do not replace earlier history. UI expiry time is not part of this DTO.
/// </summary>
public sealed record TrailSegment(
    double StartMotionTimeMs,
    TorpedoRouteSegment Segment,
    int PlannerVersion);
