using System.Text.Json.Serialization;
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
    [property: JsonRequired] IReadOnlyList<string> AttemptedTorpedoIds,
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
    private static bool Near(double a, double b) => double.IsFinite(a) && double.IsFinite(b) && Math.Abs(a - b) <= 1e-5;
    internal static GameStateData ValidateAndNormalize(GameStateData state, int format)
    {
        var saved = state.DefenseState;
        if (saved is null)
        {
            Require(format < 11 && !state.SpaceObjects.Any(o => o.ObjectType == SpaceObjectType.Countermeasure), "missing defense state");
            return state;
        }
        Require(saved.Version == 1 && saved.RngVersion == CountermeasureRng.AlgorithmVersion, "unsupported version");
        Require(saved.ProjectileSequence is >= 0 and < long.MaxValue && saved.JournalSequence is >= 0 and < long.MaxValue,
            "invalid counters");
        Require(saved.Launchers is not null && saved.Projectiles is not null && saved.AttemptedTorpedoIds is not null && saved.Journal is not null,
            "missing collections");
        Require(saved.Launchers!.All(l => l is not null && l.State is not null) &&
            saved.Projectiles!.All(p => p is not null && p.Flight is not null) && saved.Journal!.All(e => e is not null), "null row");
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
                State = l.State with { ActiveProjectileId = l.State.ActiveProjectileId is { } id ? Canon(id) : null }
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
            }).ToArray(),
            AttemptedTorpedoIds = saved.AttemptedTorpedoIds!.Select(Canon).ToArray()
        };
        var attempted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in saved.AttemptedTorpedoIds)
            Require(attempted.Add(id) && objects.TryGetValue(id, out var torpedo) && torpedo.ObjectType == SpaceObjectType.Missile,
                "invalid attempted torpedo");
        var launchers = new Dictionary<(string, string), DefenseSnapshot>();
        foreach (var row in saved.Launchers)
        {
            Require(objects.TryGetValue(row.OwnerObjectId, out var owner) && owner.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip,
                "missing defense owner");
            var module = owner!.Modules?.FirstOrDefault(m => m.ModuleId == row.ModuleId);
            Require(module is not null && launchers.TryAdd((row.OwnerObjectId, row.ModuleId), row.State), "invalid or duplicate launcher");
            var d = row.State;
            Require(Enum.IsDefined(d.State) && double.IsFinite(d.RangeKm) && d.RangeKm > 0, "invalid defense state");
            if (d.Operator is { } op)
            {
                ValidateOperator(op);
                Require(op.SkillType == WeaponSkillType.CountermeasureDefense && owner.Crew?.Any(c => c.CrewId == op.CrewId) == true &&
                    module!.OperatorCrewId == op.CrewId, "operator membership mismatch");
            }
            Require((d.State == DefenseState.NoOperator) == (d.Operator is null), "operator state mismatch");
            Require((d.State == DefenseState.Guiding) == (d.ActiveProjectileId is not null), "active projectile state mismatch");
            Require((d.State == DefenseState.Reloading) == (d.ReloadDueMotionTimeMs is not null), "reload state mismatch");
            if (d.ReloadDueMotionTimeMs is { } due) Require(double.IsFinite(due) && due > state.MotionTimeMs, "invalid reload deadline");
        }
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in saved.Projectiles)
        {
            var f = row.Flight;
            Require(ids.Add(row.ObjectId) && objects.TryGetValue(row.ObjectId, out var obj) && obj.ObjectType == SpaceObjectType.Countermeasure,
                "invalid or duplicate projectile");
            obj = objects[row.ObjectId];
            Require(launchers.TryGetValue((f.OwnerObjectId, f.LauncherModuleId), out var launcher) && launcher.ActiveProjectileId == row.ObjectId,
                "orphan projectile");
            Require(f.LaunchMotionTimeMs >= 0 && f.LaunchMotionTimeMs <= state.MotionTimeMs && f.FrozenChanceTenths is > 0 and <= 1000 &&
                Enum.IsDefined(f.Phase) && f.RatingBreakdown is not null, "invalid frozen flight");
            ValidateOperator(f.RatingBreakdown!.DefenseOperator);
            if (f.RatingBreakdown.TorpedoOperator is { } attack) ValidateOperator(attack);
            Require(f.RatingBreakdown.TorpedoRating >= 0 && f.FrozenChanceTenths == InterceptionMath.ChanceTenths(
                f.RatingBreakdown.DefenseOperator.EffectiveRating, f.RatingBreakdown.TorpedoRating), "chance mismatch");
            if (f.Phase == CountermeasurePhase.Guiding)
            {
                Require(objects.TryGetValue(f.TargetTorpedoId, out var target) && target.ObjectType == SpaceObjectType.Missile && attempted.Contains(f.TargetTorpedoId),
                    "missing target or attempt");
                Require(f.ResolutionRoll is null && f.ResolvedAtMotionTimeMs is null && f.MissExpiresAtMotionTimeMs is null, "resolved guiding flight");
                Require(row.TargetPlanStartMotionTimeMs >= 0 && row.TargetPlanStartMotionTimeMs <= state.MotionTimeMs, "invalid guidance baseline");
            }
            else Require(f.ResolutionRoll is >= 1 and <= 1000 && f.ResolutionRoll > f.FrozenChanceTenths &&
                f.ResolvedAtMotionTimeMs is { } resolved && double.IsFinite(resolved) && resolved >= f.LaunchMotionTimeMs && resolved <= state.MotionTimeMs &&
                f.MissExpiresAtMotionTimeMs == resolved + 2000 && f.MissExpiresAtMotionTimeMs > state.MotionTimeMs, "invalid missed coast");
            var route = f.Route;
            Require(route is not null && route.PlannerVersion == 1 && !route.Segments.IsDefaultOrEmpty &&
                route.StartMotionTimeMs >= f.LaunchMotionTimeMs && route.StartMotionTimeMs <= state.MotionTimeMs &&
                Near(route.ElapsedMs, state.MotionTimeMs - route.StartMotionTimeMs), "invalid route");
            foreach (var s in route!.Segments) ValidateSegment(s);
            for (int i = 1; i < route.Segments.Length; i++) ValidateJoin(route.Segments[i - 1], route.Segments[i]);
            Require(double.IsFinite(route.Segments.Sum(s => s.DurationMs)), "route duration overflow");
            if (f.PredictedEncounterMotionTimeMs is { } encounter) Require(double.IsFinite(encounter) && encounter >= f.LaunchMotionTimeMs, "invalid encounter");
            if (f.Phase == CountermeasurePhase.MissedCoast) Require(!route.HasIntercept && route.Segments.All(s => s.AngularVelocityDegPerSec == 0), "steering after miss");
            var pose = TorpedoGuidanceMath.PredictPose(route, route.ElapsedMs);
            Require(Near(pose.X, obj.PositionX) && Near(pose.Y, obj.PositionY) && Near(pose.Direction, obj.DirectionDegrees) &&
                Near(obj.SpeedMps / 1000, route.Segments[^1].SpeedKmS), "pose mismatch");
            Require(!f.Trail.IsDefault, "missing trail");
            double time = f.LaunchMotionTimeMs;
            TorpedoRouteSegment? previous = null;
            foreach (var trail in f.Trail)
            {
                Require(trail is not null && trail.PlannerVersion == 1 && Near(trail.StartMotionTimeMs, time), "trail time gap");
                ValidateSegment(trail!.Segment);
                if (previous is not null) ValidateJoin(previous, trail.Segment);
                previous = trail.Segment; time += trail.Segment.DurationMs;
            }
            Require(Near(time, state.MotionTimeMs), "incomplete trail");
            if (previous is not null)
            {
                var end = TorpedoGuidanceMath.PredictSegment(previous, previous.DurationMs);
                Require(Near(end.X, obj.PositionX) && Near(end.Y, obj.PositionY), "trail endpoint mismatch");
            }
        }
        Require(objects.Values.Where(o => o.ObjectType == SpaceObjectType.Countermeasure).All(o => ids.Contains(o.ObjectId)), "projectile without flight");
        Require(saved.Launchers.All(l => l.State.ActiveProjectileId is null || ids.Contains(l.State.ActiveProjectileId)), "busy launcher without flight");
        long lastId = 0; double lastTime = 0; ulong rolls = 0;
        foreach (var entry in saved.Journal!)
        {
            Require(entry.EventId > lastId && entry.EventId <= saved.JournalSequence && Enum.IsDefined(entry.Type) &&
                double.IsFinite(entry.MotionTimeMs) && entry.MotionTimeMs >= lastTime && entry.MotionTimeMs <= state.MotionTimeMs &&
                double.IsFinite(entry.X) && double.IsFinite(entry.Y) && !string.IsNullOrWhiteSpace(entry.ProjectileObjectId) &&
                !string.IsNullOrWhiteSpace(entry.ActorObjectId) && !string.IsNullOrWhiteSpace(entry.TargetObjectId), "invalid journal entry");
            Require(entry.ChanceTenths is null or (>= 0 and <= 1000) && entry.Roll is null or (>= 1 and <= 1000) && entry.Damage is null or >= 0,
                "invalid journal payload");
            if (entry.RatingBreakdown is { } rating)
            {
                ValidateOperator(rating.DefenseOperator);
                if (rating.TorpedoOperator is { } attack) ValidateOperator(attack);
                Require(rating.TorpedoRating >= 0 && entry.ChanceTenths == InterceptionMath.ChanceTenths(rating.DefenseOperator.EffectiveRating, rating.TorpedoRating), "journal chance mismatch");
            }
            if (entry.TorpedoOperator is { } op) ValidateOperator(op);
            bool resolution = entry.Type is CombatEventType.Intercept or CombatEventType.Miss;
            Require(resolution == (entry.Roll is not null), "roll on non-resolution");
            if (resolution)
            {
                Require(entry.RatingBreakdown is not null && (entry.Type == CombatEventType.Intercept) == (entry.Roll <= entry.ChanceTenths), "invalid resolution");
                rolls++;
            }
            lastId = entry.EventId; lastTime = entry.MotionTimeMs;
        }
        Require(lastId == saved.JournalSequence && rolls == saved.RngCounter, "journal/RNG counter mismatch");
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
        if (format < 11 && flight.TorpedoRating is null && flight.RatingBreakdown is null &&
            !flight.RatingMigratedFromLegacySave) return;
        Require(flight.TorpedoRating is >= 0, "missing or negative captured torpedo rating");
        if (flight.RatingMigratedFromLegacySave)
        {
            Require(flight.TorpedoRating == 30m && flight.RatingBreakdown is null, "invalid legacy rating provenance");
            return;
        }
        ValidateOperator(flight.RatingBreakdown);
        Require(flight.RatingBreakdown!.SkillType == WeaponSkillType.TorpedoAttack &&
            flight.RatingBreakdown.EffectiveRating == flight.TorpedoRating, "captured torpedo rating mismatch");
    }

    private static void ValidateOperator(WeaponOperatorSnapshot? op)
    {
        Require(op is not null && !string.IsNullOrWhiteSpace(op.CrewId) && !string.IsNullOrWhiteSpace(op.DisplayName) &&
            Enum.IsDefined(op.SkillType) && op.Skill is >= 0 and <= 100 && op.BaseRating >= 0 && op.EffectiveRating >= 0, "invalid operator");
        try { Require(op!.EffectiveRating == checked(op.BaseRating * (op.Skill / 50m)), "rating mismatch"); }
        catch (OverflowException) { throw new ScenarioException("Invalid defense rating overflow."); }
    }
    private static void ValidateSegment(TorpedoRouteSegment? s) => Require(s is not null &&
        new[] { s.X, s.Y, s.Direction, s.SpeedKmS, s.AngularVelocityDegPerSec, s.DurationMs }.All(double.IsFinite) &&
        s.SpeedKmS > 0 && s.DurationMs >= 0, "invalid segment");
    private static void ValidateJoin(TorpedoRouteSegment previous, TorpedoRouteSegment next)
    {
        var end = TorpedoGuidanceMath.PredictSegment(previous, previous.DurationMs);
        Require(Near(end.X, next.X) && Near(end.Y, next.Y) && Near((end.Direction - next.Direction + 540) % 360, 180), "geometry gap");
    }
}
