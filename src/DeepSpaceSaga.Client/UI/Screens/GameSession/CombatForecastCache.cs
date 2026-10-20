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
    private (Guid? Session, CombatForecastOptions Options)? _identity;
    private sealed record ReadyFlight(Scene Scene, IReadOnlyDictionary<string, CombatForecastResult> Chain);
    private readonly Dictionary<string, ReadyFlight> _ready = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _recalculatingSince = new(StringComparer.Ordinal);
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
            _ready.Clear();
            _recalculatingSince.Clear();
            _identity = null;
            Source = null;
            _cancellation?.Cancel();
            return;
        }
        if (_identity != (session, options))
        {
            _ready.Clear();
            _recalculatingSince.Clear();
            _published = null;
            _limitedUntil = 0;
            _identity = (session, options);
        }
        var scene = new Scene(session, snapshot, options);
        if (_worker is { IsCompleted: true })
        {
            bool canceled = _cancellation?.IsCancellationRequested == true;
            if (canceled && System.Diagnostics.Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds >= 200)
                _limitedUntil = System.Diagnostics.Stopwatch.GetTimestamp() + System.Diagnostics.Stopwatch.Frequency / 2;
            // Observe genuine faults even when cancellation was requested. A
            // canceled calculation may have partial geometry and must not publish.
            var completed = _worker.GetAwaiter().GetResult();
            if (!canceled && _pending is { } pending)
            {
                Remember(pending, scene, completed);
                if (Matches(pending, scene, completed))
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
        if (_published is { } published && Matches(published, scene, Results))
        {
            _recalculatingSince.Clear();
            Hits++;
            return;
        }
        var flights = snapshot.Objects.Where(o => o.Torpedo is not null || o.Countermeasure is not null).ToArray();
        if (UseWorker && flights.Length is > 0 and <= MaxFullFlights &&
            flights.Where(o => o.Torpedo is not null).All(o => snapshot.Objects.Any(t =>
                t.ObjectId == o.Torpedo!.TargetObjectId && CanPrepareImmediately(o, t))) &&
            flights.Where(o => o.Countermeasure is not null).All(o => flights.Any(t =>
                t.ObjectId == o.Countermeasure!.TargetTorpedoId && t.Torpedo is not null)))
        {
            // The whole current linear scene can be prepared directly. Avoid
            // allocating provisional paths or per-flight cache entries that would
            // immediately be replaced by this complete, coherent result.
            Results = CalculateMeasured(scene, preciseAllocation: false);
            _published = scene;
            _ready.Clear();
            _recalculatingSince.Clear();
            Source = new(session, snapshot.SnapshotSequence, snapshot.MotionTimeMs, CombatForecastResult.ModelVersion);
            return;
        }
        if (_published is { } previous) Remember(previous, scene, Results);
        // Never retain an invalid contact, including while a previous scene finishes.
        bool limited = System.Diagnostics.Stopwatch.GetTimestamp() < _limitedUntil ||
            _worker is not null && System.Diagnostics.Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds >= 225;
        var results = new Dictionary<string, CombatForecastResult>(Calculate(scene, true, limited: limited), StringComparer.Ordinal);
        foreach (var id in _ready.Keys.ToArray())
        {
            var ready = _ready[id];
            if (!results.ContainsKey(id) || !Matches(ready.Scene, scene, ready.Chain, requireAllFlights: false))
                _ready.Remove(id);
            else results[id] = ready.Chain[id];
        }
        // Observed NPC motion is deliberately a limited model. A new observation
        // can differ on every frame, so an asynchronous answer for that old pose
        // would never be current. Linear target forecasts are cheap and can be
        // prepared directly, independently of expensive torpedo/defense chains.
        if (UseWorker)
        {
            var targets = snapshot.Objects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
            var immediate = snapshot.Objects.Where(o => o.Torpedo is not null)
                .OrderBy(o => o.ObjectId, StringComparer.Ordinal).Take(MaxFullFlights)
                .Where(o => !_ready.ContainsKey(o.ObjectId) &&
                    targets.TryGetValue(o.Torpedo!.TargetObjectId, out var target) && CanPrepareImmediately(o, target))
                .ToArray();
            if (immediate.Length > 0)
            {
                var flightIds = immediate.Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
                var ids = flightIds.Concat(immediate.Select(o => o.Torpedo!.TargetObjectId)).ToHashSet(StringComparer.Ordinal);
                int defenseSlots = Math.Max(0, MaxFullFlights - snapshot.Objects.Count(o => o.Torpedo is not null));
                foreach (var defense in snapshot.Objects.Where(o => o.Countermeasure is not null)
                    .OrderBy(o => o.ObjectId, StringComparer.Ordinal).Take(defenseSlots))
                    if (flightIds.Contains(defense.Countermeasure!.TargetTorpedoId)) ids.Add(defense.ObjectId);
                // Publish a current torpedo and its dependent defenses together.
                var linear = scene with { Snapshot = snapshot with { Objects = snapshot.Objects.Where(o => ids.Contains(o.ObjectId)).ToImmutableArray() } };
                var completed = Calculate(linear, false);
                Remember(linear, scene, completed);
                foreach (var (id, result) in completed) results[id] = result;
            }
        }
        long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var id in _recalculatingSince.Keys.Where(id => !results.ContainsKey(id)).ToArray()) _recalculatingSince.Remove(id);
        foreach (var (id, result) in results.ToArray())
        {
            if (result.Status != CombatForecastStatus.Recalculating) { _recalculatingSince.Remove(id); continue; }
            if (!_recalculatingSince.TryGetValue(id, out long since)) _recalculatingSince[id] = since = timestamp;
            if (System.Diagnostics.Stopwatch.GetElapsedTime(since, timestamp).TotalMilliseconds >= 250)
                results[id] = result with { Status = CombatForecastStatus.CalculationLimited };
        }
        Results = results;
        Source = new(session, snapshot.SnapshotSequence, snapshot.MotionTimeMs, CombatForecastResult.ModelVersion);
        _published = null;
        if (_worker is not null)
        {
            if (limited) { _cancellation?.Cancel(); _limitedUntil = System.Diagnostics.Stopwatch.GetTimestamp() + System.Diagnostics.Stopwatch.Frequency / 2; }
            if (_pending is not { } pending || pending.Session != session || pending.Options != options) _cancellation?.Cancel();
            return;
        }
        if (_ready.Count == results.Count && results.Count > 0)
        {
            _published = scene;
            Hits++;
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
            _worker = Task.Run(() => CalculateMeasured(scene, token));
        }
        else
        {
            Results = CalculateMeasured(scene);
            _published = scene;
            Remember(scene, scene, Results);
        }
    }

    private IReadOnlyDictionary<string, CombatForecastResult> CalculateMeasured(Scene scene, CancellationToken cancellationToken = default, bool preciseAllocation = true)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        long allocation = GC.GetTotalAllocatedBytes(precise: preciseAllocation);
        var result = Calculate(scene, false, cancellationToken);
        AllocatedBytes = GC.GetTotalAllocatedBytes(precise: preciseAllocation) - allocation;
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
        // Poll inside each item: ParallelOptions cancellation throws on normal
        // cancellation and would still interrupt a debugger configured for throws.
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount) };
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
            if (!isLimited && !route.Segments.IsDefaultOrEmpty &&
                (obj.Torpedo?.ExpiresAtMotionTimeMs ?? obj.Countermeasure?.ExpiresAtMotionTimeMs) is not null &&
                target is { IsDestroyed: false } && target.RenderObjectType != SpaceObjectType.UnknownSpaceObject &&
                CurrentManeuverForecast.HasRequiredData(target))
            {
                // Keep the currently planned flight visible while the worker
                // computes future guidance. A one-tick stub made the whole path
                // collapse on every target maneuver. This provisional route is
                // explicitly Recalculating: it supplies neither contact nor ETA.
                double plannedEnd = route.StartMotionTimeMs + route.Segments.Sum(s => s.DurationMs);
                end = Math.Max(now, Math.Min(now + scene.Options.HorizonMs, Math.Min(plannedEnd,
                    obj.Torpedo?.ExpiresAtMotionTimeMs ?? obj.Countermeasure?.ExpiresAtMotionTimeMs ?? double.PositiveInfinity)));
                if (obj.Countermeasure is { } defense)
                    end = Math.Max(now, Math.Min(end, target?.Torpedo?.ExpiresAtMotionTimeMs ??
                        defense.MissExpiresAtMotionTimeMs ?? double.PositiveInfinity));
                int steps = Math.Clamp((int)Math.Ceiling((end - now) / 100), 1, Math.Min(128, scene.Options.MaxPoints - 1));
                var path = ImmutableArray.CreateBuilder<CombatForecastPoint>(steps + 1);
                var targets = ImmutableArray.CreateBuilder<CombatForecastPoint>(steps + 1);
                for (int i = 0; i <= steps; i++)
                {
                    double time = now + (end - now) * i / steps;
                    var p = TorpedoGuidanceMath.PredictPose(route, route.ElapsedMs + time - now);
                    path.Add(new(time, i == 0 ? obj.X : p.X, i == 0 ? obj.Y : p.Y));
                    var t = CurrentManeuverForecast.Sample(target!, time - now);
                    targets.Add(new(time, t.X, t.Y));
                }
                return new(obj.ObjectId, targetId, now, end, CombatForecastStatus.Recalculating,
                    path.MoveToImmutable(), targets.MoveToImmutable(), []);
            }
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
            if (cancellationToken.IsCancellationRequested) return;
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
            if (cancellationToken.IsCancellationRequested) return;
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

    private void Remember(Scene source, Scene current, IReadOnlyDictionary<string, CombatForecastResult> results)
    {
        foreach (var (id, result) in results)
        {
            var chain = new Dictionary<string, CombatForecastResult>(StringComparer.Ordinal) { [id] = result };
            // A defense is current only when its entire target torpedo chain is
            // current. A maneuver of another torpedo cannot reject this chain.
            if (results.TryGetValue(result.TargetId, out var target)) chain[target.ProjectileId] = target;
            if (!Matches(source, current, chain, requireAllFlights: false)) continue;
            var ids = chain.Keys.Concat(chain.Values.Select(r => r.TargetId)).ToHashSet(StringComparer.Ordinal);
            var compact = source with { Snapshot = source.Snapshot with
            {
                Objects = source.Snapshot.Objects.Where(o => ids.Contains(o.ObjectId)).ToImmutableArray()
            } };
            _ready[id] = new(compact, chain);
        }
    }

    private static bool Matches(Scene baseline, Scene current, IReadOnlyDictionary<string, CombatForecastResult> results, bool requireAllFlights = true)
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
        if (requireAllFlights && current.Snapshot.Objects.Count(o => o.Torpedo is not null || o.Countermeasure is not null) != results.Count) return false;
        // Reject a changed linear assumption before checking every dependent
        // defense route. Observed target headings can change on every frame.
        foreach (var (id, oldTarget) in previous)
            if (oldTarget.Torpedo is null && oldTarget.Countermeasure is null && LinearMotionPredictor.IsLinear(oldTarget) &&
                currentObjects.TryGetValue(id, out var target) &&
                (!Angle(oldTarget.Direction, target.Direction) || !Close(oldTarget.SpeedKmS, target.SpeedKmS))) return false;
        foreach (var (id, result) in results)
        {
            if ((now >= result.EndMotionTimeMs || result.Legs.IsEmpty) && delta != 0 ||
                !currentObjects.TryGetValue(id, out var obj) || !previous.TryGetValue(id, out var old)) return false;
            if (obj.IsDestroyed != old.IsDestroyed) return false;
            var expectedRoute = result.Legs.IsEmpty ? old.Torpedo?.Route ?? old.Countermeasure?.Route
                : result.Legs.First(l => l.To > now || l == result.Legs[^1]).Route;
            var pose = result.Legs.IsEmpty ? (old.X, old.Y, old.Direction) : result.ProjectilePose(now);
            if (!Close(pose.X, obj.X) || !Close(pose.Y, obj.Y) || !Angle(pose.Direction, obj.Direction)) return false;
            var route = obj.Torpedo?.Route ?? obj.Countermeasure?.Route;
            if (route is null || expectedRoute is null || !SameRoute(route, expectedRoute)) return false;
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
                    // The old forecast assumed a future update time. Once an
                    // actual positive update resolves a zero-duration command,
                    // matching its current pose does not validate that old future.
                    if (delta > 0 && oldTarget.EngineManeuver?.CompletesOnMotionUpdate == true && !IsClampedZeroUpdate(oldTarget)) return false;
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

    private static bool CanPrepareImmediately(ObjectMotionSnapshot projectile, ObjectMotionSnapshot target) =>
        LinearMotionPredictor.IsLinear(target) && !target.IsDestroyed && target.RenderObjectType != SpaceObjectType.UnknownSpaceObject &&
        (projectile.Torpedo?.ExpiresAtMotionTimeMs is null ||
         projectile.Torpedo is { Route.HasIntercept: true } flight && flight.SpeedKmS > target.SpeedKmS);

    private static bool IsClampedZeroUpdate(ObjectMotionSnapshot state)
    {
        if (state is not { HostileCombat: null, Orbit: null, MaxSpeedKmS: > 0, EngineManeuver: { } m }) return false;
        return m.CommandType switch
        {
            ShipEngineCommandTypes.Accelerate => state.SpeedKmS == state.MaxSpeedKmS && m.AccelerationKmS2 >= 0 && m.FirstSpeedDeltaKmS >= 0,
            ShipEngineCommandTypes.Brake => state.SpeedKmS == 0 && m.AccelerationKmS2 <= 0 && m.FirstSpeedDeltaKmS <= 0,
            _ => false
        };
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
        (a.Segments.IsDefault || b.Segments.IsDefault ? a.Segments.IsDefault == b.Segments.IsDefault :
        a.Segments.Length == b.Segments.Length && a.Segments.Zip(b.Segments).All(p =>
            Close(p.First.X, p.Second.X) && Close(p.First.Y, p.Second.Y) && Angle(p.First.Direction, p.Second.Direction) &&
            p.First.SpeedKmS == p.Second.SpeedKmS && p.First.AngularVelocityDegPerSec == p.Second.AngularVelocityDegPerSec &&
            Close(p.First.DurationMs, p.Second.DurationMs)));
}
