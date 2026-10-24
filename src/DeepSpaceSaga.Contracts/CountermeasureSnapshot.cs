using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public enum CountermeasurePhase { Guiding, MissedCoast }
public enum DefenseState { Ready, Guiding, Reloading, NoOperator }

/// <summary>Historical burst payload retained for explicit old-state validation/rejection; EP12 schedules no defense burst.</summary>
public sealed record PendingBurstShot(double DueMotionTimeMs, string TargetTorpedoId, int BarrelNumber,
    LaunchMode Mode = LaunchMode.Auto);

public static class DefenseCommandTypes
{
    public const string Launch = "defense.launch";
    // Reserved legacy identifiers retain wire identity for explicit rejection; EP12 does not execute these commands.
    public const string Fire = "defense.fire";
    public const string Enable = "defense.enable";
    public const string Disable = "defense.disable";
}

/// <summary>Historical opposing-rating DTO retained for wire identity; current point Engine does not publish interception chance.</summary>
public sealed record InterceptionRatingBreakdown(
    WeaponOperatorSnapshot DefenseOperator,
    decimal TorpedoRating,
    WeaponOperatorSnapshot? TorpedoOperator = null);

/// <summary>Immutable defense status; ReloadDueMotionTimeMs is an absolute physical deadline.</summary>
public sealed record DefenseSnapshot(
    WeaponOperatorSnapshot? Operator,
    DefenseState State,
    string? ActiveProjectileId = null,
    double? ReloadDueMotionTimeMs = null,
    double RangeKm = 100,
    double? ReadyAtMotionTimeMs = null,
    /// <summary>Populated only for overlapping flights; the singular field remains the first active ID.</summary>
    ImmutableArray<string>? ActiveProjectileIds = null,
    double? ReloadDurationMs = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    ImmutableArray<LaunchProfileSnapshot>? Profiles = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    double? ScatterPercent = null,
    /// <summary>Own current sigma per km under configured rules and assigned operator.</summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    double? EffectiveScatterPercent = null);

/// <summary>
/// Separate projectile type, never a torpedo fire target. All times use physical simulation
/// milliseconds, not calendar or UI time. A missing encounter estimate remains unknown.
/// Route and trail retain analytic world-unit geometry (1 unit = 100 m).
/// </summary>
public sealed record CountermeasureSnapshot(
    string OwnerObjectId,
    string LauncherModuleId,
    string TargetTorpedoId,
    CountermeasurePhase Phase,
    TorpedoRoute Route,
    long LaunchMotionTimeMs,
    int FrozenChanceTenths,
    InterceptionRatingBreakdown RatingBreakdown,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TrailSegment>))]
    ImmutableArray<TrailSegment> Trail = default,
    double? MissExpiresAtMotionTimeMs = null,
    double? PredictedEncounterMotionTimeMs = null,
    int? ResolutionRoll = null,
    double? ResolvedAtMotionTimeMs = null,
    LaunchMode Mode = LaunchMode.Auto,
    decimal? CapturedAccuracy = null,
    decimal? CapturedTargetManeuverability = null,
    /// <summary>Captured maximum physical duration, shared by Guiding and MissedCoast.</summary>
    long? MaxFlightTimeMs = null,
    /// <summary>Absolute physical launch time plus captured lifetime; not extended by replans or toggles.</summary>
    long? ExpiresAtMotionTimeMs = null,
    double? SpeedKmS = null,
    double? TurnRateDegPerSec = null);
