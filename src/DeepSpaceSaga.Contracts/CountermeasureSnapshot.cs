using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

public enum CountermeasurePhase { Guiding, MissedCoast }
public enum DefenseState { Ready, Guiding, Reloading, NoOperator }

public static class DefenseCommandTypes
{
    public const string Enable = "defense.enable";
    public const string Disable = "defense.disable";
}

/// <summary>Captured opposing ratings; the Engine supplies ChanceTenths in 0..1000.</summary>
public sealed record InterceptionRatingBreakdown(
    WeaponOperatorSnapshot DefenseOperator,
    decimal TorpedoRating,
    WeaponOperatorSnapshot? TorpedoOperator = null);

/// <summary>Immutable defense status; ReloadDueMotionTimeMs is an absolute physical deadline.</summary>
public sealed record DefenseSnapshot(
    bool AutoEnabled,
    WeaponOperatorSnapshot? Operator,
    DefenseState State,
    string? ActiveProjectileId = null,
    double? ReloadDueMotionTimeMs = null,
    double RangeKm = 100);

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
    double? ResolvedAtMotionTimeMs = null);
