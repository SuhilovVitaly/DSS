using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Bounded current linear point paths, published coherently on the UI thread.</summary>
internal sealed class CombatForecastCache
{
    internal const int MaxFullFlights = 16;
    internal const int MaxVisibleFlights = 4096;
    internal sealed record SourceScene(Guid? Session, ulong Revision, double MotionTimeMs, int ModelVersion);
    internal SourceScene? Source { get; private set; }
    private sealed record Scene(Guid? Session, double Time, CombatForecastOptions Options,
        IReadOnlyDictionary<string, ObjectMotionSnapshot> Flights);
    private Scene? _published;
    internal IReadOnlyDictionary<string, CombatForecastResult> Results { get; private set; } = new Dictionary<string, CombatForecastResult>();
    internal long Hits { get; private set; }
    internal long Misses { get; private set; }
    internal double PreparationMs { get; private set; }
    internal long AllocatedBytes { get; private set; }
    private static bool IsPoint(ObjectMotionSnapshot o) => o.OwnProjectile is not null || o.ObservedProjectile is not null;

    internal void Clear()
    {
        Results = new Dictionary<string, CombatForecastResult>();
        _published = null; Source = null;
    }

    internal void Prepare(Guid? session, AuthoritativeSnapshot? snapshot, CombatForecastOptions options)
    {
        if (snapshot is null) { Clear(); return; }
        var points = snapshot.Objects.Where(IsPoint).OrderBy(o => o.ObjectId, StringComparer.Ordinal).Take(MaxVisibleFlights).ToArray();
        if (_published is { } previous && Matches(previous, session, snapshot.MotionTimeMs, options, points))
        { Hits++; return; }
        var watch = System.Diagnostics.Stopwatch.StartNew(); long allocation = GC.GetTotalAllocatedBytes(precise: false);
        var results = new Dictionary<string, CombatForecastResult>(StringComparer.Ordinal);
        int slot = 0;
        foreach (var point in points)
            results[point.ObjectId] = CombatForecast.PointProjectile(point, null, snapshot.MotionTimeMs,
                slot++ >= MaxFullFlights ? options with { MaxSteps = 1 } : options);
        Results = results;
        _published = new(session, snapshot.MotionTimeMs, options, points.ToDictionary(o => o.ObjectId, StringComparer.Ordinal));
        Source = new(session, snapshot.SnapshotSequence, snapshot.MotionTimeMs, CombatForecastResult.ModelVersion);
        Misses++; PreparationMs = watch.Elapsed.TotalMilliseconds;
        AllocatedBytes = GC.GetTotalAllocatedBytes(precise: false) - allocation;
    }

    private bool Matches(Scene previous, Guid? session, double now, CombatForecastOptions options, ObjectMotionSnapshot[] points)
    {
        if (previous.Session != session || previous.Options != options || now < previous.Time || points.Length != Results.Count) return false;
        foreach (var current in points)
        {
            if (!previous.Flights.TryGetValue(current.ObjectId, out var old) || !Results.TryGetValue(current.ObjectId, out var result) ||
                current.IsDestroyed != old.IsDestroyed || now >= result.EndMotionTimeMs && now != previous.Time || !SamePointCapture(current, old)) return false;
            var pose = result.ProjectilePose(now);
            if (Math.Abs(pose.X - current.X) > 1e-6 || Math.Abs(pose.Y - current.Y) > 1e-6) return false;
        }
        return true;
    }

    private static bool SamePointCapture(ObjectMotionSnapshot current, ObjectMotionSnapshot previous)
    {
        if (current.OwnProjectile is { } own && previous.OwnProjectile is { } old)
            return own with
            {
                X = old.X,
                Y = old.Y,
                Reporter = old.Reporter,
                OwnerPlayerRelationAtLaunch = old.OwnerPlayerRelationAtLaunch
            } == old &&
                current.ObservedProjectile == previous.ObservedProjectile;
        return current.OwnProjectile is null && previous.OwnProjectile is null &&
            current.ObservedProjectile is { } observed && previous.ObservedProjectile is { } prior &&
            observed with { X = prior.X, Y = prior.Y } == prior;
    }

}
