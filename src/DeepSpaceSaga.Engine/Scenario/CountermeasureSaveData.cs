using System.Text.Json.Serialization;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Motion;
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
public sealed record DefenseLauncherSaveData(string OwnerObjectId, string ModuleId, DefenseSnapshot State);
public sealed record CountermeasureProjectileSaveData(string ObjectId, CountermeasureSnapshot Flight, long TargetPlanStartMotionTimeMs);

internal static class CountermeasureSaveValidation
{
    private static void Require(bool condition, string reason)
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
            saved.RngVersion == CountermeasureRng.AlgorithmVersion, "unsupported version");
        Require(saved.ProjectileSequence >= 0 && saved.JournalSequence is >= 0 and < long.MaxValue,
            "invalid counters");
        Require(saved.Launchers is not null && saved.Projectiles is not null && saved.Journal is not null,
            "missing collections");
        Require(saved.Launchers!.All(l => l is not null && l.State is not null) &&
            saved.Projectiles!.All(p => p is not null && p.Flight is not null) && saved.Journal!.All(e => e is not null), "null row");
        Require(saved.Projectiles!.Count == 0, "retired guided countermeasure continuation is unsupported");
        Require(saved.Launchers!.All(l => !l.State.AutoEnabled && l.State.State != DefenseState.Guiding &&
            l.State.PendingBurstShots is not { IsDefaultOrEmpty: false }), "retired automatic or burst defense continuation is unsupported");
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
                    ActiveProjectileIds = l.State.ActiveProjectileIds is { } ids ? ids.Select(Canon).ToImmutableArray() : null,
                    PendingBurstShots = l.State.PendingBurstShots is { } pending
                        ? pending.Select(shot => shot with { TargetTorpedoId = Canon(shot.TargetTorpedoId) }).ToImmutableArray()
                        : null
                }
            }).ToArray(),
            Projectiles = saved.Projectiles!.Select(p => p with
            {
                ObjectId = Canon(p.ObjectId),
                Flight = p.Flight with
                {
                    OwnerObjectId = Canon(p.Flight.OwnerObjectId),
                    LauncherModuleId = Module(p.Flight.OwnerObjectId, p.Flight.LauncherModuleId),
                    TargetTorpedoId = Canon(p.Flight.TargetTorpedoId)
                }
            }).ToArray()
        };
        var launchers = new Dictionary<(string, string), DefenseSnapshot>();
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
            if (format >= 16)
                Require(d.ManualRangeKm is { } manual && double.IsFinite(manual) && manual > 0 &&
                    d.Accuracy is >= 0 and <= 100 && d.SpeedKmS is { } speed && double.IsFinite(speed) && speed > 0 &&
                    d.TurnRateDegPerSec is { } turn && double.IsFinite(turn) && turn > 0 && d.MaxFlightTimeMs > 0,
                    "missing or invalid launcher parameters");
            if (d.Operator is { } op)
            {
                ValidateOperator(op);
                Require(op.SkillType == WeaponSkillType.CountermeasureDefense && owner.Crew?.Any(c => c.CrewId == op.CrewId) == true &&
                    module!.OperatorCrewId == op.CrewId, "operator membership mismatch");
            }
            Require((d.State == DefenseState.NoOperator) == (d.Operator is null), "operator state mismatch");
            Require((d.State == DefenseState.Guiding) == (d.ActiveProjectileId is not null), "active projectile state mismatch");
            Require((d.State == DefenseState.Reloading) == (d.ReloadDueMotionTimeMs is not null), "reload state mismatch");
            if (d.ActiveProjectileIds is { } activeIds)
                Require(activeIds.Length > 1 && activeIds[0] == d.ActiveProjectileId &&
                    activeIds.All(id => !string.IsNullOrWhiteSpace(id)) &&
                    activeIds.Distinct(StringComparer.Ordinal).Count() == activeIds.Length,
                    "invalid active projectile list");
            if (d.ReloadDueMotionTimeMs is { } due) Require(double.IsFinite(due) && due > state.MotionTimeMs, "invalid reload deadline");
        }
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pointIds = state.PointCombatState?.Projectiles.Where(p => p.Flight.Kind == ProjectileKind.Countermissile)
            .Select(p => p.ObjectId).ToHashSet(StringComparer.Ordinal) ?? [];
        Require(objects.Values.Where(o => o.ObjectType == SpaceObjectType.Countermeasure).All(o => ids.Contains(o.ObjectId) || pointIds.Contains(o.ObjectId)), "projectile without flight");
        Require(saved.Launchers.Where(l => state.PointCombatState is null || l.State.Profiles is not { IsDefaultOrEmpty: false }).All(l => l.State.ActiveProjectileIds is { } active
            ? active.All(ids.Contains)
            : l.State.ActiveProjectileId is null || ids.Contains(l.State.ActiveProjectileId)), "busy launcher without flight");
        long lastId = 0; double lastTime = 0; ulong rolls = 0, torpedoRolls = 0, breakdownEvents = 0;
        foreach (var entry in saved.Journal!)
        {
            if (state.PointCombatState is not null)
                Require(entry.ChanceTenths is null && entry.Roll is null && entry.RatingBreakdown is null &&
                    entry.TorpedoOperator is null && entry.LaunchMode is null && entry.Accuracy is null &&
                    entry.TargetManeuverability is null && entry.Attack is null && entry.Defense is null &&
                    entry.TerminationKind is null, "retired probability or guidance facts in point journal");
            Require(entry.EventId > lastId && entry.EventId <= saved.JournalSequence && Enum.IsDefined(entry.Type) &&
                double.IsFinite(entry.MotionTimeMs) && entry.MotionTimeMs >= lastTime && entry.MotionTimeMs <= state.MotionTimeMs &&
                double.IsFinite(entry.X) && double.IsFinite(entry.Y) && !string.IsNullOrWhiteSpace(entry.ProjectileObjectId) &&
                !string.IsNullOrWhiteSpace(entry.ActorObjectId) && (entry.ProjectileKind is not null && state.PointCombatState is not null ||
                !string.IsNullOrWhiteSpace(entry.TargetObjectId)), "invalid journal entry");
            Require(entry.ChanceTenths is null or (>= 0 and <= 1000) && entry.Roll is null or (>= 1 and <= 1000) && entry.Damage is null or >= 0,
                "invalid journal payload");
            if (format >= 16 && (entry.RatingBreakdown is not null || entry.LaunchMode is not null ||
                entry.Type is CombatEventType.Intercept or CombatEventType.Expired ||
                entry.Type == CombatEventType.Miss && entry.LaunchMode is not null || entry.Result == "countermeasure"))
            {
                Require(!string.IsNullOrWhiteSpace(entry.LauncherModuleId) && entry.LaunchMode is { } mode && Enum.IsDefined(mode) &&
                    entry.Accuracy is >= 0 and <= 100 && entry.TargetManeuverability is >= 0 and <= 200 && entry.Damage == 0 &&
                    entry.RatingBreakdown is { DefenseOperator: not null } captured && entry.Accuracy == captured.DefenseOperator.EffectiveRating &&
                    entry.TargetManeuverability == captured.TorpedoRating &&
                    SequenceWithin(entry.ProjectileObjectId, "countermeasure-", saved.ProjectileSequence), "invalid captured journal facts");
                Require(entry.TerminationKind is null || Enum.IsDefined(entry.TerminationKind.Value), "invalid termination kind");
                if (entry.Type == CombatEventType.Expired)
                    Require(entry.TerminationKind is CountermeasureTerminationKind.LifetimeExpired or CountermeasureTerminationKind.MissedCoastExpired,
                        "invalid expiry reason");
            }
            if (entry.RatingBreakdown is { } rating)
            {
                ValidateOperator(rating.DefenseOperator);
                if (rating.TorpedoOperator is { } attack) ValidateOperator(attack);
                Require(rating.TorpedoRating >= 0 && entry.ChanceTenths is { } chance && CapturedChanceMatches(
                    rating.DefenseOperator!.EffectiveRating, rating.TorpedoRating, chance), "journal chance mismatch");
            }
            if (entry.TorpedoOperator is { } op) ValidateOperator(op);
            bool resolution = entry.Type == CombatEventType.Intercept ||
                entry.Type == CombatEventType.Miss && entry.RatingBreakdown is not null;
            bool torpedoRoll = entry.Type == CombatEventType.TorpedoHitRoll;
            bool breakdown = entry.Type == CombatEventType.LauncherBreakdown;
            Require((resolution || torpedoRoll || breakdown) == (entry.Roll is not null), "roll on non-resolution");
            if (breakdown)
            {
                Require(entry.ChanceTenths is > 0 and <= 1000 && entry.Roll <= entry.ChanceTenths &&
                    !string.IsNullOrWhiteSpace(entry.LauncherModuleId) && !string.IsNullOrWhiteSpace(entry.Result),
                    "invalid launcher breakdown");
                breakdownEvents++;
            }
            if (torpedoRoll)
            {
                Require(entry.Attack is >= 0 && entry.Defense is >= 0 && entry.ChanceTenths is not null &&
                    entry.Result is "hit" or "miss" &&
                    (entry.Result == "hit") == (entry.Roll <= entry.ChanceTenths), "invalid torpedo hit roll");
                torpedoRolls++;
            }
            if (resolution)
            {
                Require(entry.RatingBreakdown is not null && (entry.Type == CombatEventType.Intercept) == (entry.Roll <= entry.ChanceTenths), "invalid resolution");
                rolls++;
            }
            lastId = entry.EventId; lastTime = entry.MotionTimeMs;
        }
        Require(lastId == saved.JournalSequence && rolls == saved.RngCounter, "journal/RNG counter mismatch");
        Require(torpedoRolls == (state.CombatState?.TorpedoHitRngCounter ?? 0), "torpedo-hit journal/RNG counter mismatch");
        Require(breakdownEvents <= (state.CombatState?.LauncherBreakdownRngCounter ?? 0),
            "launcher-breakdown journal/RNG counter mismatch");
        if (saved.SelectedProjectileId is { } selected)
        {
            selected = Canon(selected);
            Require(ids.Contains(selected), "selected countermeasure is absent");
            saved = saved with { SelectedProjectileId = selected };
        }
        return state with { DefenseState = saved };
    }
    internal static void ValidateCapturedTorpedoRating(TorpedoSnapshot flight, int format)
    {
        if (format >= 16)
            Require(!flight.RatingMigratedFromLegacySave && flight.CapturedManeuverability is >= 0 and <= 200 &&
                flight.OwnerPlayerRelationAtLaunch is PlayerRelation.Self or PlayerRelation.Enemy or PlayerRelation.Neutral or PlayerRelation.Friend,
                "missing phase-three torpedo provenance");
        if (format < 11 && flight.TorpedoRating is null && flight.RatingBreakdown is null &&
            !flight.RatingMigratedFromLegacySave) return;
        Require(flight.TorpedoRating is >= 0, "missing or negative captured torpedo rating");
        Require(flight.CapturedManeuverability is null || flight.CapturedManeuverability == flight.TorpedoRating,
            "captured maneuverability mismatch");
        if (flight.RatingMigratedFromLegacySave)
        {
            Require(flight.TorpedoRating == 30m && flight.RatingBreakdown is null, "invalid legacy rating provenance");
            return;
        }
        ValidateOperator(flight.RatingBreakdown);
        Require(flight.RatingBreakdown!.SkillType == WeaponSkillType.TorpedoAttack &&
            flight.ModeManeuverabilityBonus is >= 0 and <= 100 &&
            flight.RatingBreakdown.EffectiveRating + flight.ModeManeuverabilityBonus == flight.TorpedoRating,
            "captured torpedo rating mismatch");
    }

    private static void ValidateOperator(WeaponOperatorSnapshot? op)
    {
        Require(op is not null && !string.IsNullOrWhiteSpace(op.CrewId) && !string.IsNullOrWhiteSpace(op.DisplayName) &&
            Enum.IsDefined(op.SkillType) && op.Skill is >= 0 and <= 100 && op.BaseRating >= 0 && op.EffectiveRating >= 0, "invalid operator");
        Require(op!.BaseRating <= 100 &&
            (op.EffectiveRating == op.BaseRating || op.EffectiveRating == op.BaseRating + op.Skill), "rating mismatch");
    }

    // v16 has no captured chance-rules revision. Preserve EP-0008 base-zero saves and the
    // shipped EP-0009 base-fifty rules until v17 records the exact rule identity.
    private static bool CapturedChanceMatches(decimal attack, decimal defense, int chance) =>
        chance == InterceptionMath.ChanceTenths(attack, defense) ||
        chance == CombatChanceMath.ChanceTenths(attack, defense, CombatChanceMath.DefaultRules);
    internal static bool SequenceWithin(string id, string prefix, long sequence) =>
        id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
        long.TryParse(id.AsSpan(prefix.Length), System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out long number) && number > 0 && number <= sequence;
}
