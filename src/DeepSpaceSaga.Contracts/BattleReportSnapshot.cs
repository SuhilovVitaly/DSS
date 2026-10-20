using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Confirmed player-visible facts, E01 through E07 in specification order.</summary>
public enum BattleReportKind
{
    EnemyInRange,
    EnemyTorpedoLaunch,
    PlayerTorpedoLaunch,
    PlayerCountermeasureLaunch,
    EnemyTorpedoIntercepted,
    EnemyShipHit,
    PlayerShipDamaged
}

public enum BattleReportVariant { None, Hit, Destroyed }
public enum BattleReportReporterRole { ScannerOperator, TorpedoOperator, CountermeasureOperator, Captain }

/// <summary>Captured identity. Null crew/name/portrait denotes the reporting service; it never invents crew.</summary>
public sealed record BattleReportReporterSnapshot(BattleReportReporterRole Role,
    string? CrewId = null, string? DisplayName = null, string? PortraitImage = null);

/// <summary>Permitted module facts captured after a complete contact. No hull-health model is implied.</summary>
public sealed record BattleReportDamageSnapshot(string ModuleId, string? ModuleDisplayName,
    int ArmorBefore, int ArmorAfter, int StructureBefore, int StructureAfter);

/// <summary>
/// Immutable report independent of live objects. Sequence is positive and session-owned;
/// MotionTimeMs is finite, nonnegative physical time, including fractional contact times.
/// EpisodeId correlates one projectile/contact. Optional participant fields contain only
/// information the player may know, captured at the event; null means undisclosed/unavailable.
/// Text, localization, graphic resources and presentation state belong to Client.
/// </summary>
public sealed record BattleReportSnapshot(long Sequence, double MotionTimeMs, BattleReportKind Kind,
    string EpisodeId, BattleReportReporterSnapshot Reporter,
    string? SourceObjectId = null, string? SourceDisplayName = null,
    string? TargetObjectId = null, string? TargetDisplayName = null,
    string? ProjectileObjectId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? LauncherModuleId = null,
    BattleReportVariant Variant = BattleReportVariant.None,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BattleReportDamageSnapshot? Damage = null,
    /// <summary>Position of an already resolved event, never a future aim point.</summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? X = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Y = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProjectileKind? ProjectileKind = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProfileId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? BlastRadiusKm = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProjectileHitResult? HitResult = null);

/// <summary>
/// Session report delta. BaselineSequence is fixed before the first advance (also after Load);
/// only reports above it are new. LatestSequence is the highest issued sequence, independent
/// of snapshot/journal sequence. Reports are ordered by sequence, not rounded timestamps.
/// Pending delivery is bounded by PendingLimit at every delivery stage; overflow must fault
/// the session explicitly rather than discard undelivered reports. UI history is separately bounded.
/// </summary>
public sealed record CombatReportBatchSnapshot(long BaselineSequence, long LatestSequence,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<BattleReportSnapshot>))]
    ImmutableArray<BattleReportSnapshot> Reports = default)
{
    public const int PendingLimit = 4096;
}
