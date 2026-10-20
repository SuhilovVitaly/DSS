using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Point-aim projectile kind, independent of ownership and UI selection.</summary>
public enum ProjectileKind { Torpedo, Countermissile }

/// <summary>Engine-validated point-launch choice; speed km/s and physical blast radius km.</summary>
public sealed record LaunchProfileSnapshot(string ProfileId, double SpeedKmS, double BlastRadiusKm);

/// <summary>
/// Captured flight information visible to the projectile's owner. Coordinates use world
/// units (100 m), speed is km/s and radius is km. Times are absolute physical milliseconds;
/// the detonation deadline may fall between whole milliseconds. The Engine supplies all
/// values; this transport record performs no simulation or random draws.
/// </summary>
public sealed record OwnProjectileSnapshot(
    ProjectileKind Kind,
    string OwnerObjectId,
    string LauncherModuleId,
    double X,
    double Y,
    double StartX,
    double StartY,
    double DetonationX,
    double DetonationY,
    long LaunchMotionTimeMs,
    double DetonationMotionTimeMs,
    double SpeedKmS,
    string ProfileId,
    double BlastRadiusKm,
    int Damage,
    decimal ArmorPiercingPercent,
    decimal ArmorDamagePercent = 100,
    double CoreFraction = 0.3);

/// <summary>
/// Foreign projectile observation. The unit direction vector is measured in world axes;
/// coordinates use world units, speed km/s and radius km. No endpoint, deadline, launch
/// position, target, route, module or combat rating can be represented by this schema.
/// Unknown JSON members are rejected even when the surrounding serializer skips them.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ObservedProjectileSnapshot(
    ProjectileKind Kind,
    double X,
    double Y,
    double DirectionX,
    double DirectionY,
    double SpeedKmS,
    string ProfileId,
    double BlastRadiusKm);

/// <summary>Side relative to the threat's direction, not the evading ship's heading.</summary>
public enum EvadeSide { Left, Right }

public static class ManeuverCommandTypes
{
    public const string Evade = "maneuver.evade";
}

/// <summary>Combat commands addressed by owner object and launcher module.</summary>
public static class CombatCommandTypes
{
    public const string Launch = "torpedo.launch";
    public const string Detonate = "torpedo.detonate";
    // Transitional consumers are migrated before these legacy identifiers are removed.
    public const string Fire = "torpedo.fire";
    public const string SelfDestruct = "torpedo.selfDestruct";
}

/// <summary>Torpedo launcher command identifiers; mode selection is addressed to one launcher.</summary>
public static class TorpedoLauncherCommandTypes
{
    public const string SetMode = "torpedo.setMode";
}

/// <summary>Origin of a launch; independent of the launcher's current automatic-defense toggle.</summary>
public enum LaunchMode { Auto, Manual }

/// <summary>Why a countermeasure flight ended. Only a resolved contact consumes an interception roll.</summary>
public enum CountermeasureTerminationKind { Intercept, MissedCoastExpired, LifetimeExpired, TargetLost, OwnerLost }

/// <summary>
/// Player-visible authoritative hull health in integer hit points. ShipClassId is an
/// explicit content identity, independent of sprite/name. The Engine owns damage and masking.
/// </summary>
public sealed record HullCombatSnapshot(string ShipClassId, int CurrentHp, int MaxHp);

/// <summary>
/// Captured launcher parameters: speed in km/s, positive maximum turn rate in degrees/s,
/// damage in integer hull hit points. ActiveTorpedoIds lists every projectile still
/// in flight; launch readiness is governed by ReadyAtMotionTimeMs.
/// </summary>
public sealed record LauncherCombatSnapshot(
    double SpeedKmS,
    double TurnRateDegPerSec,
    int Damage,
    WeaponOperatorSnapshot? Operator = null,
    /// <summary>Inclusive launch range in km, supplied by the module definition. Null means not supplied.</summary>
    double? RangeKm = null,
    /// <summary>Module-owned interception resistance in 0..100, distinct from physical turn rate.</summary>
    decimal? Maneuverability = null,
    string ModeId = "standard",
    double? ReadyAtMotionTimeMs = null,
    bool IsBrokenUntilDock = false,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> ActiveTorpedoIds = default,
    double? ReloadDurationMs = null,
    /// <summary>Point-launch capabilities; null for legacy producers. No implicit profiles.</summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ImmutableArray<LaunchProfileSnapshot>? Profiles = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    double? ScatterPercent = null);

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
/// <param name="HitChancePercent">Legacy captured percentage in 0..100; the module-combat hit roll uses captured attack, defense and ChanceTenths.</param>
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
    int HitChancePercent = 100,
    /// <summary>Rating captured at launch. Null only in legacy payloads before Engine migration.</summary>
    decimal? TorpedoRating = null,
    WeaponOperatorSnapshot? RatingBreakdown = null,
    /// <summary>True when the Engine supplied legacy migration rating 30 without inventing an operator.</summary>
    bool RatingMigratedFromLegacySave = false,
    /// <summary>Module maneuverability captured at launch; replan and operator changes never replace it.</summary>
    decimal? CapturedManeuverability = null,
    /// <summary>Launching owner's relation to the player, retained as provenance after the owner disappears.</summary>
    string? OwnerPlayerRelationAtLaunch = null,
    /// <summary>Captured attack contribution for the contact-time hit roll.</summary>
    decimal? CapturedAttack = null,
    /// <summary>Authoritative chance in tenths of one percent; populated by EP-0009 runtime.</summary>
    int? HitChanceTenths = null,
    /// <summary>Armor piercing captured at launch, independent of later catalog edits.</summary>
    decimal ArmorPiercingPercent = 0,
    /// <summary>Armor damage multiplier captured at launch, independent of later catalog edits.</summary>
    decimal ArmorDamagePercent = 100,
    /// <summary>Mode frozen at launch; changing the launcher later cannot rewrite the flight.</summary>
    string ModeId = "standard",
    decimal ModeManeuverabilityBonus = 0,
    /// <summary>Absolute physical expiry captured at launch; null only for pre-EP-0009 saves.</summary>
    long? ExpiresAtMotionTimeMs = null);

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

/// <summary>
/// Authoritative contact fact, retained in the session journal across snapshots.
/// Consumers deduplicate by EventId within one session and clear that set when the
/// session is replaced. IDs are monotonic within a session, not globally unique.
/// MotionTimeMs is fractional physical time; X/Y and FinalTrail use world units.
/// The final trail ends at contact and remains available after projectile removal.
/// No UI clock, animation age or pixel geometry is transmitted or saved here.
/// </summary>
public sealed record CombatImpactSnapshot(
    long EventId,
    string TorpedoObjectId,
    string OwnerObjectId,
    string LauncherModuleId,
    string TargetObjectId,
    string HitObjectId,
    double MotionTimeMs,
    double X,
    double Y,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TrailSegment>))]
    ImmutableArray<TrailSegment> FinalTrail = default,
    int DamageApplied = 0,
    string? DestroyedObjectId = null,
    string? WreckObjectId = null,
    TorpedoTerminationKind TerminationKind = TorpedoTerminationKind.Impact);

public enum TorpedoTerminationKind { Impact, SelfDestruct, Intercept, Miss, Expired }
