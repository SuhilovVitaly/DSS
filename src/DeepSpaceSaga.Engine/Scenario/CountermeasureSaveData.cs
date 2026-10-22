using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Engine.Scenario;

public sealed record CountermeasureStateData(
    [property: JsonRequired] int Version,
    [property: JsonRequired] long ProjectileSequence,
    [property: JsonRequired] int RngVersion,
    [property: JsonRequired] ulong RngState,
    [property: JsonRequired] ulong RngCounter,
    [property: JsonRequired] long JournalSequence,
    [property: JsonRequired] IReadOnlyList<DefenseLauncherSaveData> Launchers,
    [property: JsonRequired] IReadOnlyList<CountermeasureProjectileSaveData> Projectiles,
    [property: JsonRequired] IReadOnlyList<CombatJournalEntry> Journal,
    string? SelectedProjectileId = null);
public sealed record DefenseLauncherSaveData(string OwnerObjectId, string ModuleId, DefenseCombatSaveState State);
/// <summary>Save-only envelope preserves retired auto/burst validation without live snapshot members.</summary>
public sealed record DefenseCombatSaveState(
    [property: JsonIgnore] bool AutoEnabled,
    WeaponOperatorSnapshot? Operator,
    DefenseState State,
    string? ActiveProjectileId = null,
    double? ReloadDueMotionTimeMs = null,
    double RangeKm = 100,
    /// <summary>Independent inclusive manual launch range in km.</summary>
    double? ManualRangeKm = null,
    decimal? Accuracy = null,
    double? SpeedKmS = null,
    double? TurnRateDegPerSec = null,
    long? MaxFlightTimeMs = null,
    int Barrels = 1,
    /// <summary>Null in pre-migration snapshots; an empty array means no queued barrel shots.</summary>
    ImmutableArray<PendingBurstShot>? PendingBurstShots = null,
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
    double? EffectiveScatterPercent = null)
{
    public static implicit operator DefenseCombatSaveState(DefenseSnapshot state) => new(
        false, state.Operator, state.State, ActiveProjectileId: state.ActiveProjectileId,
        ReloadDueMotionTimeMs: state.ReloadDueMotionTimeMs, RangeKm: state.RangeKm,
        PendingBurstShots: [], ReadyAtMotionTimeMs: state.ReadyAtMotionTimeMs,
        ActiveProjectileIds: state.ActiveProjectileIds, ReloadDurationMs: state.ReloadDurationMs,
        Profiles: state.Profiles, ScatterPercent: state.ScatterPercent, EffectiveScatterPercent: state.EffectiveScatterPercent);
}
public sealed record CountermeasureProjectileSaveData(string ObjectId, CountermeasureSnapshot Flight, long TargetPlanStartMotionTimeMs);

internal static class CountermeasureSaveValidation
{
    private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
    {
        if (!condition) throw new ScenarioException("Invalid defense save: " + reason);
    }
    internal static GameStateData ValidateAndNormalize(GameStateData state, int format)
    {
        var saved = state.DefenseState;
        if (saved is null)
        {
            Require(format < 11 && !state.SpaceObjects.Any(o => o.ObjectType == SpaceObjectType.Countermeasure), "missing defense state");
            return state;
        }
        Require((format >= 17 ? saved.Version == 3 : format >= 16 ? saved.Version == 2 : saved.Version is 1 or 2 or 3) &&
            saved.RngVersion == 1 && saved.RngState == 0 && saved.RngCounter == 0, "unsupported version or retired interception RNG");
        Require(saved.ProjectileSequence >= 0 && saved.JournalSequence is >= 0 and < long.MaxValue, "invalid counters");
        Require(saved.Launchers is not null && saved.Projectiles is { Count: 0 } && saved.Journal is not null && saved.SelectedProjectileId is null,
            "missing collections or retired guided continuation");
        Require(saved.Launchers!.All(l => l is not null && l.State is not null) && saved.Journal!.All(e => e is not null), "null row");
        Require(saved.Launchers.All(l => !l.State.AutoEnabled && l.State.State != DefenseState.Guiding &&
            l.State.PendingBurstShots is not { IsDefaultOrEmpty: false }), "retired automatic or burst continuation");
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        string Canon(string id)
        {
            Require(!string.IsNullOrWhiteSpace(id), "empty reference");
            return objects.TryGetValue(id, out var obj) ? obj.ObjectId : id;
        }
        string Module(string ownerId, string moduleId)
        {
            Require(!string.IsNullOrWhiteSpace(moduleId), "empty module reference");
            return objects.TryGetValue(ownerId, out var owner) ? owner.Modules?.FirstOrDefault(m =>
                string.Equals(m.ModuleId, moduleId, StringComparison.OrdinalIgnoreCase))?.ModuleId ?? moduleId : moduleId;
        }
        saved = saved with
        {
            Launchers = saved.Launchers!.Select(l => l with
            {
                OwnerObjectId = Canon(l.OwnerObjectId),
                ModuleId = Module(l.OwnerObjectId, l.ModuleId),
                State = l.State with
                {
                    ActiveProjectileId = l.State.ActiveProjectileId is { } id ? Canon(id) : null,
                    ActiveProjectileIds = l.State.ActiveProjectileIds is { } ids ? ids.Select(Canon).ToImmutableArray() : null
                }
            }).ToArray()
        };
        var launchers = new Dictionary<(string, string), DefenseCombatSaveState>();
        foreach (var row in saved.Launchers)
        {
            Require(objects.TryGetValue(row.OwnerObjectId, out var owner) && owner.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip,
                "missing defense owner");
            var module = owner!.Modules?.FirstOrDefault(m => m.ModuleId == row.ModuleId);
            Require(module is not null && launchers.TryAdd((row.OwnerObjectId, row.ModuleId), row.State), "invalid or duplicate launcher");
            var d = row.State;
            Require(Enum.IsDefined(d.State) && double.IsFinite(d.RangeKm) && d.RangeKm > 0, "invalid defense state");
            if (state.PointCombatState is not null && d.Profiles is { IsDefaultOrEmpty: false })
            {
                Require(PointCombatSaveValidation.ProfilesValid(d.Profiles.Value) && d.ReloadDurationMs is >= 0 && double.IsFinite(d.ReloadDurationMs.Value) &&
                    d.ScatterPercent is >= 0 && double.IsFinite(d.ScatterPercent.Value) && d.ManualRangeKm is null && d.Accuracy is null &&
                    d.SpeedKmS is null && d.TurnRateDegPerSec is null && d.MaxFlightTimeMs is null && d.Barrels == 1,
                    "invalid point defense capabilities or legacy fields");
                Require(!d.AutoEnabled && d.State != DefenseState.Guiding && d.PendingBurstShots is { IsDefaultOrEmpty: true } &&
                    d.ActiveProjectileIds is { IsDefault: false } && d.ActiveProjectileId == d.ActiveProjectileIds.Value.FirstOrDefault(),
                    "invalid point defense state");
                Require(d.ActiveProjectileIds!.Value.Distinct(StringComparer.Ordinal).Count() == d.ActiveProjectileIds.Value.Length &&
                    d.ActiveProjectileIds.Value.All(id => state.PointCombatState.Projectiles.Any(p => p.ObjectId == id &&
                        p.Flight.Kind == ProjectileKind.Countermissile && p.Flight.OwnerObjectId == row.OwnerObjectId && p.Flight.LauncherModuleId == row.ModuleId)),
                    "point defense flight ownership mismatch");
                Require(d.ReadyAtMotionTimeMs is null || double.IsFinite(d.ReadyAtMotionTimeMs.Value) && d.ReadyAtMotionTimeMs >= 0,
                    "invalid point reload deadline");
                bool reloading = d.ReadyAtMotionTimeMs > state.MotionTimeMs;
                Require(d.State == (reloading ? DefenseState.Reloading : d.Operator is null ? DefenseState.NoOperator : DefenseState.Ready) &&
                    d.ReloadDueMotionTimeMs == (reloading ? d.ReadyAtMotionTimeMs : null) &&
                    (d.ActiveProjectileIds.Value.IsEmpty || d.ReadyAtMotionTimeMs is not null), "point reload/state mismatch");
                if (d.Operator is { } pointOperator)
                {
                    ValidateOperator(pointOperator);
                    Require(pointOperator.SkillType == WeaponSkillType.CountermeasureDefense && pointOperator.BaseRating == 0 &&
                        pointOperator.Skill is >= 0 and <= 30 && module!.OperatorCrewId == pointOperator.CrewId &&
                        owner.Crew?.Any(c => c.CrewId == pointOperator.CrewId) == true, "invalid point operator membership");
                }
                continue;
            }
            throw new ScenarioException("Invalid defense save: retired guided capabilities.");
        }
        var pointIds = state.PointCombatState?.Projectiles.Where(p => p.Flight.Kind == ProjectileKind.Countermissile)
            .Select(p => p.ObjectId).ToHashSet(StringComparer.Ordinal) ?? [];
        Require(objects.Values.Where(o => o.ObjectType == SpaceObjectType.Countermeasure).All(o => pointIds.Contains(o.ObjectId)), "projectile without point flight");
        long lastId = 0; double lastTime = 0;
        foreach (var e in saved.Journal!)
        {
            Require(e.ChanceTenths is null && e.Roll is null && e.RatingBreakdown is null && e.TorpedoOperator is null && e.LaunchMode is null &&
                e.Accuracy is null && e.TargetManeuverability is null && e.Attack is null && e.Defense is null && e.TerminationKind is null,
                "retired probability or guidance facts in point journal");
            Require(e.Type is CombatEventType.Launch or CombatEventType.Miss or CombatEventType.Hit or CombatEventType.ModuleHit or
                CombatEventType.ModuleDestroyed or CombatEventType.ShipDestroyed or CombatEventType.TorpedoDetonated or
                CombatEventType.CountermissileDetonated or CombatEventType.ShotDown or CombatEventType.ManuallyDetonated or CombatEventType.TorpedoReloadCompleted,
                "retired or invalid event type");
            Require(e.EventId > lastId && e.EventId <= saved.JournalSequence && double.IsFinite(e.MotionTimeMs) &&
                e.MotionTimeMs >= lastTime && e.MotionTimeMs <= state.MotionTimeMs && double.IsFinite(e.X) && double.IsFinite(e.Y) &&
                !string.IsNullOrWhiteSpace(e.ActorObjectId) &&
                (e.Type == CombatEventType.TorpedoReloadCompleted
                    ? e.ActorObjectId == state.PlayerShipObjectId && !string.IsNullOrWhiteSpace(e.LauncherModuleId) &&
                      string.IsNullOrEmpty(e.ProjectileObjectId) && e.ProjectileKind is null
                    : !string.IsNullOrWhiteSpace(e.ProjectileObjectId) &&
                      (e.ProjectileKind is not null && state.PointCombatState is not null || !string.IsNullOrWhiteSpace(e.TargetObjectId))) &&
                e.Damage is null or >= 0, $"invalid journal entry ({e.Type}/{e.EventId})");
            lastId = e.EventId; lastTime = e.MotionTimeMs;
        }
        Require(lastId == saved.JournalSequence, "journal sequence mismatch");
        return state with { DefenseState = saved };
    }

    private static void ValidateOperator(WeaponOperatorSnapshot? op) => Require(op is not null &&
        !string.IsNullOrWhiteSpace(op.CrewId) && !string.IsNullOrWhiteSpace(op.DisplayName) && Enum.IsDefined(op.SkillType) &&
        op.Skill is >= 0 and <= 30 && op.BaseRating == 0 && op.EffectiveRating == op.Skill, "invalid point operator");

    internal static bool SequenceWithin(string id, string prefix, long sequence) =>
        id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
        long.TryParse(id.AsSpan(prefix.Length), System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out long number) && number > 0 && number <= sequence;
}
