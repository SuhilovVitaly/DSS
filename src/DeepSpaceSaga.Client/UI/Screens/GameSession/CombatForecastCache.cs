using System.Collections.Concurrent;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>One worker, one pending scene, atomic publication on the UI thread.</summary>
internal sealed class CombatForecastCache
{
    internal const int MaxFullFlights = 16;
    internal const int MaxVisibleFlights = 4096;
    private sealed class RouteEquality : IEqualityComparer<TorpedoRoute>
    {
        public bool Equals(TorpedoRoute? a, TorpedoRoute? b) => ReferenceEquals(a, b) || a is not null && b is not null &&
            a with { Segments = b.Segments } == b && (a.Segments.IsDefault || b.Segments.IsDefault
                ? a.Segments.IsDefault == b.Segments.IsDefault : a.Segments.SequenceEqual(b.Segments));
        public int GetHashCode(TorpedoRoute route)
        {
            var hash = new HashCode();
            hash.Add(route with { Segments = default });
            if (!route.Segments.IsDefault) foreach (var segment in route.Segments) hash.Add(segment);
            return hash.ToHashCode();
        }
    }
    private long _startedAt;
    private long _limitedUntil;
    internal sealed record SourceScene(Guid? Session, ulong Revision, double MotionTimeMs, int ModelVersion);
    internal SourceScene? Source { get; private set; }
    private sealed record Scene(Guid? Session, AuthoritativeSnapshot Snapshot, CombatForecastOptions Options);
    private Scene? _published;
    private Scene? _pending;
    private CancellationTokenSource? _cancellation;
    private Task<IReadOnlyDictionary<string, CombatForecastResult>>? _worker;
    internal IReadOnlyDictionary<string, CombatForecastResult> Results { get; private set; } = new Dictionary<string, CombatForecastResult>();
    internal long Hits { get; private set; }
    internal long Misses { get; private set; }
    internal double PreparationMs { get; private set; }
    internal long AllocatedBytes { get; private set; }
    internal Task? PendingWork => _worker;
    internal bool UseWorker { get; set; } = true;
    internal void CancelPending() => _cancellation?.Cancel();

    internal void Prepare(Guid? session, AuthoritativeSnapshot? snapshot, CombatForecastOptions options)
    {
        if (snapshot is null)
        {
            Results = new Dictionary<string, CombatForecastResult>();
            _published = null;
            Source = null;
            _cancellation?.Cancel();
            return;
        }
        var scene = new Scene(session, snapshot, options);
        if (_worker is { IsCompleted: true })
        {
            if (_worker.IsCanceled && System.Diagnostics.Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds >= 200)
                _limitedUntil = System.Diagnostics.Stopwatch.GetTimestamp() + System.Diagnostics.Stopwatch.Frequency / 2;
            if (!_worker.IsCanceled)
            {
                var completed = _worker.GetAwaiter().GetResult();
                if (_pending is { } pending && Matches(pending, scene, completed))
                {
                    Results = completed;
                    _published = pending;
                    Source = new(pending.Session, pending.Snapshot.SnapshotSequence, pending.Snapshot.MotionTimeMs, CombatForecastResult.ModelVersion);
                }
            }
            _cancellation?.Dispose();
            _cancellation = null;
            _worker = null;
            _pending = null;
        }
        if (_published is { } published && Matches(published, scene, Results)) { Hits++; return; }
        // Never retain an invalid contact, including while a previous scene finishes.
        bool limited = System.Diagnostics.Stopwatch.GetTimestamp() < _limitedUntil ||
            _worker is not null && System.Diagnostics.Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds >= 225;
        Results = Calculate(scene, true, limited: limited);
        Source = new(session, snapshot.SnapshotSequence, snapshot.MotionTimeMs, CombatForecastResult.ModelVersion);
        _published = null;
        if (_worker is not null)
        {
            if (limited) { _cancellation?.Cancel(); _limitedUntil = System.Diagnostics.Stopwatch.GetTimestamp() + System.Diagnostics.Stopwatch.Frequency / 2; }
            if (_pending is not { } pending || pending.Session != session || pending.Options != options ||
                !SameControls(pending.Snapshot, snapshot)) _cancellation?.Cancel();
            return;
        }
        if (limited) return;
        Misses++;
        bool expensive = snapshot.Objects.Any(o => o.Countermeasure is not null ||
            o.Torpedo is { } f && (!f.Route.HasIntercept || snapshot.Objects.Any(t =>
                t.ObjectId == f.TargetObjectId && (!LinearMotionPredictor.IsLinear(t) || !MatchesAim(f, t, snapshot.MotionTimeMs)))));
        if (UseWorker && expensive)
        {
            _pending = scene;
            _cancellation = new CancellationTokenSource();
            _startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            _cancellation.CancelAfter(225);
            var token = _cancellation.Token;
            _worker = Task.Run(() => CalculateMeasured(scene, token), token);
        }
        else { Results = CalculateMeasured(scene); _published = scene; }
    }

    private IReadOnlyDictionary<string, CombatForecastResult> CalculateMeasured(Scene scene, CancellationToken cancellationToken = default)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        long allocation = GC.GetTotalAllocatedBytes(precise: true);
        var result = Calculate(scene, false, cancellationToken);
        AllocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocation;
        PreparationMs = watch.Elapsed.TotalMilliseconds;
        return result;
    }

    private static IReadOnlyDictionary<string, CombatForecastResult> Calculate(Scene scene, bool placeholder, CancellationToken cancellationToken = default, bool limited = false)
    {
        var snapshot = scene.Snapshot;
        var results = new ConcurrentDictionary<string, CombatForecastResult>(StringComparer.Ordinal);
        var torpedoes = snapshot.Objects.Where(o => o.Torpedo is not null).OrderBy(o => o.ObjectId, StringComparer.Ordinal).Take(MaxVisibleFlights).ToArray();
        var defenses = snapshot.Objects.Where(o => o.Countermeasure is not null).OrderBy(o => o.ObjectId, StringComparer.Ordinal).Take(MaxVisibleFlights - torpedoes.Length).ToArray();
        var targetIds = torpedoes.Select(o => o.Torpedo!.TargetObjectId).Concat(defenses.Select(o => o.Countermeasure!.TargetTorpedoId)).ToHashSet(StringComparer.Ordinal);
        var objects = snapshot.Objects.Where(o => targetIds.Contains(o.ObjectId)).ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        // Identical physical flights may share immutable geometry, independently of
        // object identity. Dependencies are still published with their own IDs.
        var torpedoGeometry = new ConcurrentDictionary<(ObjectMotionSnapshot, CombatForecastOptions), Lazy<CombatForecastResult>>();
        var defenseGeometry = new ConcurrentDictionary<(ObjectMotionSnapshot, ImmutableArray<CombatForecastLeg>, CombatForecastOptions), Lazy<CombatForecastResult>>();
        var routeValues = new ConcurrentDictionary<TorpedoRoute, TorpedoRoute>(new RouteEquality());
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount), CancellationToken = cancellationToken };
        if (!placeholder && torpedoes.Length + defenses.Length > 2)
        {
            Parallel.For(0, torpedoes.Length, parallel, Torpedo);
            // This barrier preserves the dependency: every defense reads a complete
            // torpedo result from the same immutable source scene.
            Parallel.For(0, defenses.Length, parallel, Defense);
        }
        else
        {
            for (int i = 0; i < torpedoes.Length; i++) Torpedo(i);
            for (int i = 0; i < defenses.Length; i++) Defense(i);
        }
        return results.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

        CombatForecastOptions Settings(int index) => index >= MaxFullFlights ? scene.Options with { MaxSteps = 1 } : scene.Options;
        CombatForecastResult Short(ObjectMotionSnapshot obj, ObjectMotionSnapshot? target, string targetId, bool isLimited)
        {
            double now = snapshot.MotionTimeMs;
            var route = obj.Torpedo?.Route ?? obj.Countermeasure!.Route;
            double end = Math.Max(now, Math.Min(CombatGuidanceRules.NextBoundary((long)now),
                obj.Torpedo?.ExpiresAtMotionTimeMs ?? obj.Countermeasure?.ExpiresAtMotionTimeMs ?? double.PositiveInfinity));
            if (obj.Countermeasure is { } cm)
                end = Math.Max(now, Math.Min(end, target?.Torpedo?.ExpiresAtMotionTimeMs ??
                    cm.MissExpiresAtMotionTimeMs ?? double.PositiveInfinity));
            var pose = route.Segments.IsDefaultOrEmpty ? (obj.X, obj.Y, obj.Direction) :
                TorpedoGuidanceMath.PredictPose(route, route.ElapsedMs + end - now);
            var destination = target is null || !CurrentManeuverForecast.HasRequiredData(target) ? target :
                CurrentManeuverForecast.Sample(target, end - now);
            return new(obj.ObjectId, targetId, now, end,
                isLimited ? CombatForecastStatus.CalculationLimited : CombatForecastStatus.Recalculating,
                [new(now, obj.X, obj.Y), new(end, pose.Item1, pose.Item2)], target is null ? [] :
                [new(now, target.X, target.Y), new(end, destination!.X, destination.Y)], []);
        }
        void Torpedo(int index)
        {
            var obj = torpedoes[index];
            objects.TryGetValue(obj.Torpedo!.TargetObjectId, out var target);
            if (placeholder || index >= MaxFullFlights)
            { results[obj.ObjectId] = Short(obj, target, obj.Torpedo.TargetObjectId, limited || index >= MaxFullFlights); return; }
            var physical = new ObjectMotionSnapshot("", obj.X, obj.Y, obj.SpeedKmS, obj.Direction, IsDestroyed: obj.IsDestroyed,
                Torpedo: obj.Torpedo with
                {
                    OwnerObjectId = "",
                    LauncherModuleId = "",
                    Trail = default,
                    Route = routeValues.GetOrAdd(obj.Torpedo.Route, obj.Torpedo.Route)
                });
            var result = torpedoGeometry.GetOrAdd((physical, Settings(index)), _ => new(() =>
                CombatForecast.Torpedo(obj, target, snapshot.MotionTimeMs, Settings(index), cancellationToken))).Value;
            results[obj.ObjectId] = result with { ProjectileId = obj.ObjectId };
        }
        void Defense(int index)
        {
            var obj = defenses[index];
            objects.TryGetValue(obj.Countermeasure!.TargetTorpedoId, out var target);
            results.TryGetValue(obj.Countermeasure.TargetTorpedoId, out var targetForecast);
            int slot = index + torpedoes.Length;
            if (placeholder || slot >= MaxFullFlights)
            { results[obj.ObjectId] = Short(obj, target, obj.Countermeasure.TargetTorpedoId, limited || slot >= MaxFullFlights); return; }
            var physical = new ObjectMotionSnapshot("", obj.X, obj.Y, obj.SpeedKmS, obj.Direction, IsDestroyed: obj.IsDestroyed,
                Countermeasure: obj.Countermeasure with
                {
                    OwnerObjectId = "",
                    LauncherModuleId = "",
                    TargetTorpedoId = "",
                    Trail = default,
                    Route = routeValues.GetOrAdd(obj.Countermeasure.Route, obj.Countermeasure.Route)
                });
            var key = (physical,
                targetForecast?.Legs ?? [], Settings(slot));
            var result = defenseGeometry.GetOrAdd(key, _ => new(() => CombatForecast.Countermeasure(obj, target, targetForecast, snapshot.MotionTimeMs,
                Settings(slot), cancellationToken))).Value;
            results[obj.ObjectId] = result with { ProjectileId = obj.ObjectId, TargetId = obj.Countermeasure.TargetTorpedoId };
        }
    }

    private static bool SameControls(AuthoritativeSnapshot a, AuthoritativeSnapshot b)
    {
        var objects = b.Objects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        foreach (var old in a.Objects)
        {
            if (!objects.TryGetValue(old.ObjectId, out var current)) return false;
            if (old.ActiveEngineCommandType != current.ActiveEngineCommandType || old.MaxSpeedKmS != current.MaxSpeedKmS ||
                old.EngineManeuver?.AccelerationKmS2 != current.EngineManeuver?.AccelerationKmS2 ||
                old.EngineManeuver?.Repeats != current.EngineManeuver?.Repeats ||
                old.HostileCombat?.Segment.AccelerationKmS2 != current.HostileCombat?.Segment.AccelerationKmS2 ||
                old.HostileCombat?.Segment.TurnRateDegPerSec != current.HostileCombat?.Segment.TurnRateDegPerSec ||
                old.Torpedo?.TargetObjectId != current.Torpedo?.TargetObjectId ||
                old.Countermeasure?.Phase != current.Countermeasure?.Phase ||
                old.Countermeasure?.TargetTorpedoId != current.Countermeasure?.TargetTorpedoId || current.IsDestroyed) return false;
        }
        return true;
    }

    private static bool Matches(Scene baseline, Scene current, IReadOnlyDictionary<string, CombatForecastResult> results)
    {
        if (baseline.Session != current.Session || baseline.Options != current.Options) return false;
        if (baseline.Snapshot.Objects == current.Snapshot.Objects && baseline.Snapshot.MotionTimeMs == current.Snapshot.MotionTimeMs) return true;
        if (results.Count == 0) return !current.Snapshot.Objects.Any(o => o.Torpedo is not null || o.Countermeasure is not null);
        double now = current.Snapshot.MotionTimeMs;
        double delta = now - baseline.Snapshot.MotionTimeMs;
        if (delta < 0) return false;
        var needed = results.Keys.Concat(results.Values.Select(r => r.TargetId)).ToHashSet(StringComparer.Ordinal);
        var currentObjects = current.Snapshot.Objects.Where(o => needed.Contains(o.ObjectId)).ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        var previous = baseline.Snapshot.Objects.Where(o => needed.Contains(o.ObjectId)).ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        if (current.Snapshot.Objects.Count(o => o.Torpedo is not null || o.Countermeasure is not null) != results.Count) return false;
        foreach (var (id, result) in results)
        {
            if (now >= result.EndMotionTimeMs || result.Legs.IsEmpty || !currentObjects.TryGetValue(id, out var obj) ||
                !previous.TryGetValue(id, out var old)) return false;
            if (obj.IsDestroyed != old.IsDestroyed) return false;
            var leg = result.Legs.First(l => l.To > now || l == result.Legs[^1]);
            var pose = result.ProjectilePose(now);
            if (!Close(pose.X, obj.X) || !Close(pose.Y, obj.Y) || !Angle(pose.Direction, obj.Direction)) return false;
            var route = obj.Torpedo?.Route ?? obj.Countermeasure?.Route;
            if (route is null || !SameRoute(route, leg.Route)) return false;
            if (obj.Torpedo is { } torpedo && old.Torpedo is { } oldTorpedo)
            {
                if (torpedo with
                {
                    Route = oldTorpedo.Route,
                    Trail = oldTorpedo.Trail,
                    DistanceTravelledWorldUnits = oldTorpedo.DistanceTravelledWorldUnits,
                    PredictedImpactMotionTimeMs = oldTorpedo.PredictedImpactMotionTimeMs
                } != oldTorpedo) return false;
                if (currentObjects.TryGetValue(result.TargetId, out var target) && previous.TryGetValue(result.TargetId, out var oldTarget))
                {
                    var expected = CurrentManeuverForecast.HasRequiredData(oldTarget)
                        ? CurrentManeuverForecast.Sample(oldTarget, delta)
                        : new LinearMotionPredictor().Predict(oldTarget, (long)delta);
                    bool sameHealth = expected.ModuleHealth.IsDefaultOrEmpty && target.ModuleHealth.IsDefaultOrEmpty ||
                        !expected.ModuleHealth.IsDefault && !target.ModuleHealth.IsDefault &&
                        expected.ModuleHealth.SequenceEqual(target.ModuleHealth);
                    if (!Close(expected.X, target.X) || !Close(expected.Y, target.Y) || !Angle(expected.Direction, target.Direction) ||
                        !Close(expected.SpeedKmS, target.SpeedKmS) ||
                        expected with
                        {
                            X = target.X,
                            Y = target.Y,
                            Direction = target.Direction,
                            SpeedKmS = target.SpeedKmS,
                            HostileCombat = target.HostileCombat,
                            EngineManeuver = SameEngineManeuver(expected.EngineManeuver, target.EngineManeuver)
                                ? target.EngineManeuver : expected.EngineManeuver,
                            // Fresh immutable UI projections are not a maneuver change.
                            ModuleHealth = sameHealth ? target.ModuleHealth : expected.ModuleHealth,
                            Defense = target.Defense,
                            HullCombat = target.HullCombat,
                            Survey = target.Survey,
                            ObjectType = target.ObjectType,
                            RelationToPlayer = target.RelationToPlayer,
                            DisplayName = target.DisplayName,
                            Image = target.Image,
                            CaptainDisplayName = target.CaptainDisplayName,
                            CaptainPortraitImage = target.CaptainPortraitImage,
                            DockOperatorDisplayName = target.DockOperatorDisplayName,
                            DockOperatorPortraitImage = target.DockOperatorPortraitImage
                        } != target ||
                        oldTarget.HostileCombat?.Segment.AccelerationKmS2 != target.HostileCombat?.Segment.AccelerationKmS2 ||
                        oldTarget.HostileCombat?.Segment.TurnRateDegPerSec != target.HostileCombat?.Segment.TurnRateDegPerSec ||
                        (oldTarget.HostileCombat is { } oldHostile && target.HostileCombat is { } hostile &&
                            oldHostile with { Segment = hostile.Segment } != hostile)) return false;
                }
                else if (currentObjects.ContainsKey(result.TargetId) != previous.ContainsKey(result.TargetId)) return false;
            }
            else if (obj.Countermeasure is { } cm && old.Countermeasure is { } oldCm)
            {
                if (cm with { Route = oldCm.Route, Trail = oldCm.Trail, PredictedEncounterMotionTimeMs = oldCm.PredictedEncounterMotionTimeMs } != oldCm) return false;
            }
            else return false;
        }
        return true;
    }

    private static bool Close(double a, double b) => Math.Abs(a - b) <= 1e-6;
    private static bool SameEngineManeuver(EngineManeuverSnapshot? a, EngineManeuverSnapshot? b) =>
        a is null || b is null ? a == b :
        Close(a.CycleRemainingMs, b.CycleRemainingMs) && Close(a.FirstSpeedDeltaKmS, b.FirstSpeedDeltaKmS) &&
        a with { CycleRemainingMs = b.CycleRemainingMs, FirstSpeedDeltaKmS = b.FirstSpeedDeltaKmS } == b;
    private static bool MatchesAim(TorpedoSnapshot flight, ObjectMotionSnapshot target, double now)
    {
        if (flight.Route.Segments.IsDefaultOrEmpty) return false;
        double duration = flight.Route.Segments.Sum(s => s.DurationMs);
        var end = TorpedoGuidanceMath.PredictPose(flight.Route, duration);
        var aim = TorpedoGuidanceMath.PredictSegment(new(target.X, target.Y, target.Direction, target.SpeedKmS, 0, 0),
            flight.Route.StartMotionTimeMs + duration - now);
        return Close(end.X, aim.X) && Close(end.Y, aim.Y);
    }
    private static bool Angle(double a, double b) => Math.Abs((a - b + 540) % 360 - 180) < 1e-8;
    private static bool SameRoute(TorpedoRoute a, TorpedoRoute b) =>
        a.StartMotionTimeMs == b.StartMotionTimeMs && a.PlannerVersion == b.PlannerVersion && a.HasIntercept == b.HasIntercept &&
        a.Segments.Length == b.Segments.Length && a.Segments.Zip(b.Segments).All(p =>
            Close(p.First.X, p.Second.X) && Close(p.First.Y, p.Second.Y) && Angle(p.First.Direction, p.Second.Direction) &&
            p.First.SpeedKmS == p.Second.SpeedKmS && p.First.AngularVelocityDegPerSec == p.Second.AngularVelocityDegPerSec &&
            Close(p.First.DurationMs, p.Second.DurationMs));
}
