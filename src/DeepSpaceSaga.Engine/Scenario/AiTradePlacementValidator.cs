using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

internal sealed record PlacementCheck(long EpochGameTimeMs, string BaseId, string LinkId, double Clearance);
internal sealed record PlacementViolation(long EpochGameTimeMs, string BaseId, string LinkId, string Reason);
internal sealed record PlacementConnectivity(long EpochGameTimeMs, int Components);
internal sealed record PlacementValidationResult(bool IsValid, long HorizonGameTimeMs, double SunExclusionRadius,
    ImmutableArray<PlacementCheck> Checks, ImmutableArray<PlacementViolation> Violations,
    ImmutableArray<long> CriticalEpochs, ImmutableArray<PlacementConnectivity> Connectivity, int Attempts = 1);

/// <summary>Finite-horizon reservation proof. Does not route ships or impose gameplay restrictions.</summary>
internal static class AiTradePlacementValidator
{
    internal const long Day = 86400000;
    internal const long DefaultHorizon = 365 * Day;
    internal const double Epsilon = 1e-6;
    private const int CalendarScale = 300;
    private const int MaxEpochs = 8192;
    private const int MaxSubdivisions = 100000;
    internal readonly record struct Point(double X, double Y);
    internal readonly record struct Disc(Point Center, double Radius);

    internal static PlacementValidationResult Validate(ScenarioFile world, StationClusterMapSnapshot clusters,
        AiMapEnvironmentSnapshot ai, long horizonGameTimeMs)
    {
        var state = world.GameState;
        double systemRadius = state.SolarSystem?.SystemRadius ?? 0;
        double sunRadius = Math.Max(1, systemRadius * 0.01);
        var checks = ImmutableArray.CreateBuilder<PlacementCheck>();
        var violations = ImmutableArray.CreateBuilder<PlacementViolation>();
        var criticalEpochs = new SortedSet<long>();
        var connectivity = ImmutableArray.CreateBuilder<PlacementConnectivity>();
        PlacementValidationResult Result() => new(violations.Count == 0, horizonGameTimeMs, sunRadius,
            checks.ToImmutable(), violations.ToImmutable(), criticalEpochs.ToImmutableArray(), connectivity.ToImmutable());
        void Fail(long epoch, string baseId, string link, string reason) =>
            violations.Add(new(checked(state.GameTimeMs + epoch), baseId, link, reason));
        if (horizonGameTimeMs < 0 || horizonGameTimeMs > DefaultHorizon || systemRadius <= sunRadius)
        { Fail(0, "", "", "invalid_horizon_or_bounds"); return Result(); }
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var groups = clusters.Clusters.OrderBy(c => c.Id, StringComparer.Ordinal).ToArray();
        var territories = ai.Territories.IsDefault ? [] : ai.Territories.OrderBy(t => t.Id, StringComparer.Ordinal).ToArray();
        Point At(string id, long elapsed)
        {
            var p = AiBaseGenerator.Pose(objects[id], checked(state.MotionTimeMs + elapsed / CalendarScale));
            return new(p.X, p.Y);
        }
        double Speed(string id) => objects[id].Orbit is { } orbit ? Math.Tau * orbit.SemiMajorAxis / orbit.OrbitalPeriodMs : 0;
        var local = groups.SelectMany(c =>
        {
            var ids = c.StationIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return clusters.Links.Where(l => ids.Contains(l.FromStationId) && ids.Contains(l.ToStationId));
        }).OrderBy(l => l.Id, StringComparer.Ordinal).ToArray();
        // Degenerate segments cover isolated human stations too.
        var routes = local.Select(l => (l.Id, A: l.FromStationId, B: l.ToStationId))
            .Concat(groups.SelectMany(c => c.StationIds).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.Ordinal).Select(id => (Id: "node:" + id, A: id, B: id))).ToArray();
        var epochs = new SortedSet<long> { 0, horizonGameTimeMs };
        foreach (long day in new long[] { 1, 7, 30, 100, 365 })
            if (day * Day <= horizonGameTimeMs) epochs.Add(day * Day);
        foreach (var territory in territories)
            foreach (var station in groups.SelectMany(g => g.StationIds))
            {
                var a = objects[territory.BaseObjectId].Orbit;
                var b = objects[station].Orbit;
                if (a is null || b is null) continue;
                double omega = AngularSpeed(a) - AngularSpeed(b);
                if (Math.Abs(omega) < 1e-30) continue;
                double phase = Phase(a, state.MotionTimeMs) - Phase(b, state.MotionTimeMs);
                double last = phase + omega * horizonGameTimeMs;
                double low = Math.Ceiling(Math.Min(phase, last) / Math.PI);
                double high = Math.Floor(Math.Max(phase, last) / Math.PI);
                if (!double.IsFinite(low) || !double.IsFinite(high) || high - low > MaxEpochs)
                { Fail(0, territory.BaseObjectId, station, "critical_epoch_budget"); return Result(); }
                for (double k = low; k <= high; k++)
                {
                    double time = (k * Math.PI - phase) / omega;
                    if (time < 0 || time > horizonGameTimeMs) continue;
                    foreach (long rounded in new[] { (long)Math.Floor(time / CalendarScale) * CalendarScale, (long)Math.Ceiling(time / CalendarScale) * CalendarScale })
                    {
                        long epoch = Math.Clamp(rounded, 0, horizonGameTimeMs);
                        epochs.Add(epoch); criticalEpochs.Add(checked(state.GameTimeMs + epoch));
                    }
                    if (epochs.Count > MaxEpochs)
                    { Fail(0, territory.BaseObjectId, station, "critical_epoch_budget"); return Result(); }
                }
            }

        foreach (long epoch in epochs)
        {
            var discs = territories.Select(t => new Disc(At(t.BaseObjectId, epoch), t.PatrolRadiusKm * 10)).ToArray();
            foreach (var t in territories)
            {
                foreach (var route in routes)
                {
                    double clearance = Distance(At(t.BaseObjectId, epoch), At(route.A, epoch), At(route.B, epoch)) - t.PatrolRadiusKm * 10;
                    checks.Add(new(checked(state.GameTimeMs + epoch), t.BaseObjectId, route.Id, clearance));
                    if (checks.Count > 200000)
                    { Fail(epoch, t.BaseObjectId, route.Id, "check_budget_uncertain"); return Result(); }
                    if (clearance <= Epsilon)
                    { Fail(epoch, t.BaseObjectId, route.Id, "local_route_overlap"); return Result(); }
                }
                if (epoch == 0 && Distance(At(t.BaseObjectId, 0), At(state.PlayerShipObjectId, 0), At(state.PlayerShipObjectId, 0)) <= t.PatrolRadiusKm * 10 + Epsilon)
                { Fail(0, t.BaseObjectId, state.PlayerShipObjectId, "ai_start_network_overlap"); return Result(); }
            }
            var access = groups.Select(g => At(g.StationIds.OrderBy(x => x, StringComparer.Ordinal).First(), epoch)).ToArray();
            int components = DetourComponents(access, discs, systemRadius, sunRadius);
            connectivity.Add(new(checked(state.GameTimeMs + epoch), components));
            if (components != 1)
            { Fail(epoch, string.Join(",", territories.Select(t => t.BaseObjectId)), "cluster-access", components < 0 ? "detour_budget_uncertain" : "detour_disconnected"); return Result(); }
        }

        int budget = MaxSubdivisions;
        var times = epochs.ToArray();
        foreach (var t in territories)
            foreach (var route in routes)
            {
                double velocity = Speed(t.BaseObjectId) + Math.Max(Speed(route.A), Speed(route.B));
                bool Certify(long start, long end, int depth)
                {
                    long mid = start + (end - start) / 2;
                    if (--budget < 0) { Fail(mid, t.BaseObjectId, route.Id, "interval_budget_uncertain"); return false; }
                    double clearance = Distance(At(t.BaseObjectId, mid), At(route.A, mid), At(route.B, mid)) - t.PatrolRadiusKm * 10;
                    if (clearance <= Epsilon) { Fail(mid, t.BaseObjectId, route.Id, "interval_overlap"); return false; }
                    // Calendar-to-motion integer sampling adds at most one physical millisecond of displacement.
                    double bound = velocity * ((end - start) / 2.0 + CalendarScale);
                    if (clearance - bound > Epsilon) return true;
                    if (depth >= 32 || end - start <= CalendarScale)
                    { Fail(mid, t.BaseObjectId, route.Id, "interval_clearance_uncertain"); return false; }
                    return Certify(start, mid, depth + 1) && Certify(mid, end, depth + 1);
                }
                for (int i = 1; i < times.Length; i++)
                    if (!Certify(times[i - 1], times[i], 0)) return Result();
            }
        return Result();
    }

    private static double AngularSpeed(OrbitalElements orbit) =>
        (orbit.OrbitDirection == "clockwise" ? 1 : -1) * Math.Tau / orbit.OrbitalPeriodMs;

    private static double Phase(OrbitalElements orbit, long motionTime)
    {
        Int128 elapsed = ((Int128)motionTime - orbit.EpochSimulationTimeMs) * CalendarScale;
        return (orbit.InitialPhase + orbit.PhaseOffsetDegrees) * Math.PI / 180 +
            AngularSpeed(orbit) * (double)(elapsed % orbit.OrbitalPeriodMs);
    }

    internal static double Distance(Point point, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double length = dx * dx + dy * dy;
        double t = length == 0 ? 0 : Math.Clamp(((point.X - a.X) * dx + (point.Y - a.Y) * dy) / length, 0, 1);
        return Math.Sqrt(Math.Pow(point.X - a.X - t * dx, 2) + Math.Pow(point.Y - a.Y - t * dy, 2));
    }

    /// <summary>Circumscribed vertices are conservative; rejected narrow passages are retried, never accepted on uncertainty.</summary>
    internal static bool DetourConnected(IReadOnlyList<Point> access, IReadOnlyList<Disc> territories, double systemRadius, double sunRadius)
        => DetourComponents(access, territories, systemRadius, sunRadius) == 1;

    private static int DetourComponents(IReadOnlyList<Point> access, IReadOnlyList<Disc> territories, double systemRadius, double sunRadius)
    {
        if (access.Count == 0) return 0;
        var discs = territories.Append(new Disc(new(0, 0), sunRadius)).ToArray();
        bool Allowed(Point p) => double.IsFinite(p.X) && double.IsFinite(p.Y) &&
            p.X * p.X + p.Y * p.Y < Math.Pow(systemRadius - Epsilon, 2) &&
            discs.All(d => Distance(d.Center, p, p) > d.Radius + Epsilon);
        bool Edge(Point a, Point b) => discs.All(d => Distance(d.Center, a, b) > d.Radius + Epsilon);
        if (access.Any(p => !Allowed(p))) return 0;
        var nodes = access.ToList();
        int edgeBudget = 2000000;
        int Components()
        {
            var reached = new bool[nodes.Count];
            int components = 0, remaining = access.Count;
            for (int root = 0; root < access.Count; root++)
            {
                if (reached[root]) continue;
                components++; reached[root] = true; remaining--;
                var queue = new Queue<int>(); queue.Enqueue(root);
                while (queue.TryDequeue(out int from))
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        if (remaining == 0) return components;
                        if (reached[i]) continue;
                        if (--edgeBudget < 0) return -1;
                        if (!Edge(nodes[from], nodes[i])) continue;
                        reached[i] = true; queue.Enqueue(i);
                        if (i < access.Count) remaining--;
                    }
            }
            return components;
        }
        int direct = Components();
        if (direct is 1 or -1) return direct;
        if ((long)discs.Length * 64 + nodes.Count > 8192) return -1;
        foreach (var disc in discs)
        {
            double radius = (disc.Radius + 4 * Epsilon) / Math.Cos(Math.PI / 64);
            for (int i = 0; i < 64; i++)
            {
                double angle = Math.Tau * i / 64;
                var point = new Point(disc.Center.X + radius * Math.Cos(angle), disc.Center.Y + radius * Math.Sin(angle));
                if (Allowed(point)) nodes.Add(point);
            }
        }
        return Components();
    }
}
