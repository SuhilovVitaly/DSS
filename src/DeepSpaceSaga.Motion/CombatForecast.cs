using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

public enum CombatForecastStatus { Recalculating, Contact, NoContact, CalculationLimited, InsufficientData, TargetLost, Expired, MissedCoast, TargetTerminated }

public sealed record CombatForecastOptions(long HorizonMs = 200_000, int MaxSteps = 2000, int MaxPoints = 8001)
{
    public static CombatForecastOptions Default { get; } = new();
}

public readonly record struct CombatForecastPoint(double MotionTimeMs, double X, double Y);
public sealed record CombatForecastLeg(double From, double To, TorpedoRoute Route);

/// <summary>Immutable, physical-time result. The contact retains both centres at the same instant.</summary>
public sealed record CombatForecastResult(string ProjectileId, string TargetId, double StartMotionTimeMs,
    double EndMotionTimeMs, CombatForecastStatus Status, ImmutableArray<CombatForecastPoint> Projectile,
    ImmutableArray<CombatForecastPoint> Target, ImmutableArray<CombatForecastLeg> Legs)
{
    public const int ModelVersion = 1;
    public double? ContactMotionTimeMs => Status == CombatForecastStatus.Contact ? EndMotionTimeMs : null;

    public (double X, double Y, double Direction) ProjectilePose(double time)
    {
        int low = 0, high = Legs.Length - 1;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (Legs[mid].To < time) low = mid + 1; else high = mid;
        }
        var route = Legs[low].Route;
        return TorpedoGuidanceMath.PredictPose(route, time - route.StartMotionTimeMs);
    }
}

/// <summary>Pure conditional simulation of existing flights. No world, events, RNG, or new AI commands.</summary>
public static class CombatForecast
{
    public static CombatForecastResult Torpedo(ObjectMotionSnapshot projectile, ObjectMotionSnapshot? target,
        double now, CombatForecastOptions? options = null, CancellationToken cancellationToken = default)
    {
        var flight = projectile.Torpedo ?? throw new ArgumentException("Expected a torpedo.", nameof(projectile));
        bool known = target is { IsDestroyed: false } && target.ObjectId == flight.TargetObjectId &&
            target.RenderObjectType != SpaceObjectType.UnknownSpaceObject;
        bool sufficient = known && flight.ExpiresAtMotionTimeMs is not null && flight.SpeedKmS > 0 &&
            flight.TurnRateDegPerSec > 0 && CurrentManeuverForecast.HasRequiredData(target!);
        double speed = Math.Max(target?.SpeedKmS ?? 0, target?.MaxSpeedKmS ?? 0);
        if (target?.Orbit is { } orbit)
            speed = Math.Max(speed, orbit.SemiMajorAxis * Math.Tau * 1000 *
                SimulationSpeedExtensions.BaseGameSecondsPerRealSecond / orbit.OrbitalPeriodMs / 10);
        var sampler = sufficient ? CurrentManeuverForecast.CreateSampler(target!) : null;
        ObjectMotionSnapshot Sample(double t)
        {
            var p = sampler!(t - now);
            return target! with { X = p.X, Y = p.Y, SpeedKmS = p.SpeedKmS, Direction = p.Direction };
        }
        var baseline = target;
        long baselineTime = (long)Math.Floor(now);
        double? deadline = flight.ExpiresAtMotionTimeMs;
        if (known && !sufficient)
            deadline = Math.Min(deadline ?? double.PositiveInfinity, CombatGuidanceRules.NextBoundary((long)Math.Floor(now)));
        if (projectile.IsDestroyed) deadline = now;
        return Calculate(projectile.ObjectId, flight.TargetObjectId, flight.Route, now,
            deadline, flight.SpeedKmS, flight.TurnRateDegPerSec,
            sufficient ? t => { var p = sampler!(t - now); return (p.X, p.Y); }
        : null,
            speed / 100, sufficient ? CombatForecastStatus.NoContact : known
                ? CombatForecastStatus.InsufficientData : CombatForecastStatus.TargetLost,
            (pose, route, time) =>
            {
                var next = Sample(time);
                bool changed = CombatGuidanceRules.TargetChanged(baseline!, time - baselineTime, next);
                // A newly displayed command can precede the next guidance boundary.
                // The route still contains the previous aim: recover that assumption
                // from its timed endpoint rather than treating the new velocity as its baseline.
                if (!changed && route.HasIntercept)
                {
                    double duration = route.Segments.Sum(s => s.DurationMs);
                    var aim = TorpedoGuidanceMath.PredictPose(route, duration);
                    var expectedTarget = TorpedoGuidanceMath.PredictSegment(new(next.X, next.Y,
                        next.Direction, next.SpeedKmS, 0, 0), route.StartMotionTimeMs + duration - time);
                    changed = Math.Abs(aim.X - expectedTarget.X) > 1e-6 || Math.Abs(aim.Y - expectedTarget.Y) > 1e-6;
                }
                if (!changed && route.HasIntercept && time - route.StartMotionTimeMs < route.Segments.Sum(s => s.DurationMs)) return route;
                baseline = next;
                baselineTime = time;
                return TorpedoGuidanceMath.Plan(pose, next, flight.SpeedKmS, flight.TurnRateDegPerSec, time);
            }, options, cancellationToken);
    }

    public static CombatForecastResult Countermeasure(ObjectMotionSnapshot projectile, ObjectMotionSnapshot? target,
        CombatForecastResult? targetForecast, double now, CombatForecastOptions? options = null, CancellationToken cancellationToken = default)
    {
        var flight = projectile.Countermeasure ?? throw new ArgumentException("Expected a countermeasure.", nameof(projectile));
        double? expires = flight.ExpiresAtMotionTimeMs;
        if (projectile.IsDestroyed) expires = now;
        if (flight.Phase == CountermeasurePhase.MissedCoast)
            expires = Math.Min(expires ?? double.PositiveInfinity, flight.MissExpiresAtMotionTimeMs ?? now);
        bool guiding = flight.Phase == CountermeasurePhase.Guiding;
        bool sufficient = guiding && flight.SpeedKmS is > 0 && flight.TurnRateDegPerSec is > 0 && expires is not null &&
            target?.Torpedo is not null && target.ObjectId == flight.TargetTorpedoId &&
            targetForecast is { Legs.IsEmpty: false } && targetForecast.ProjectileId == flight.TargetTorpedoId &&
            targetForecast.StartMotionTimeMs == now &&
            targetForecast.Status is not (CombatForecastStatus.InsufficientData or CombatForecastStatus.TargetLost);
        var status = !guiding ? CombatForecastStatus.MissedCoast : target?.Torpedo is null
            ? CombatForecastStatus.TargetLost : sufficient ? CombatForecastStatus.NoContact : CombatForecastStatus.InsufficientData;
        double end = Math.Min(expires ?? now, guiding ? targetForecast?.EndMotionTimeMs ?? double.PositiveInfinity : double.PositiveInfinity);
        if (guiding && !sufficient && status == CombatForecastStatus.InsufficientData)
            end = Math.Min(end, CombatGuidanceRules.NextBoundary((long)Math.Floor(now)));
        long targetPlan = target?.Torpedo?.Route.StartMotionTimeMs ?? -1;
        var result = Calculate(projectile.ObjectId, flight.TargetTorpedoId, flight.Route, now, end,
            flight.SpeedKmS ?? projectile.SpeedKmS, flight.TurnRateDegPerSec ?? 0,
            sufficient ? t => { var p = targetForecast!.ProjectilePose(t); return (p.X, p.Y); }
        : null,
            (target?.Torpedo?.SpeedKmS ?? 0) / 100, status,
            (pose, route, time) =>
            {
                var targetRoute = targetForecast!.Legs.First(l => l.To > time || l == targetForecast.Legs[^1]).Route;
                if (targetPlan == targetRoute.StartMotionTimeMs && route.HasIntercept) return route;
                targetPlan = targetRoute.StartMotionTimeMs;
                double deadline = flight.Mode == LaunchMode.Manual ? end :
                    targetRoute.StartMotionTimeMs + targetRoute.Segments.Sum(s => s.DurationMs);
                return CountermeasureGuidanceMath.PlanManual(pose, targetRoute, time,
                    Math.Max(1, (long)Math.Ceiling(expires!.Value - time)), flight.SpeedKmS!.Value,
                    flight.TurnRateDegPerSec!.Value, deadline).Route;
            }, options, cancellationToken);
        // Engine resolves a hull contact before a simultaneous defense contact.
        if (guiding && targetForecast?.Status == CombatForecastStatus.Contact && result.EndMotionTimeMs >= targetForecast.EndMotionTimeMs)
            result = result with { Status = CombatForecastStatus.TargetTerminated };
        else if (targetForecast?.Status == CombatForecastStatus.Expired && result.Status == CombatForecastStatus.Expired &&
            result.EndMotionTimeMs == targetForecast.EndMotionTimeMs)
            result = result with { Status = CombatForecastStatus.TargetTerminated };
        else if (targetForecast?.Status == CombatForecastStatus.CalculationLimited && result.Status == CombatForecastStatus.Expired &&
            result.EndMotionTimeMs == targetForecast.EndMotionTimeMs)
            result = result with { Status = CombatForecastStatus.CalculationLimited };
        return result;
    }

    private static CombatForecastResult Calculate(string id, string targetId, TorpedoRoute initial, double now,
        double? expires, double speed, double turnRate, Func<double, (double X, double Y)>? target,
        double targetSpeedBound, CombatForecastStatus initialStatus,
        Func<ObjectMotionSnapshot, TorpedoRoute, long, TorpedoRoute> replan, CombatForecastOptions? options, CancellationToken cancellationToken)
    {
        options ??= CombatForecastOptions.Default;
        if (options.HorizonMs <= 0 || options.MaxSteps <= 0 || options.MaxPoints < 2 ||
            options.HorizonMs > 200_000 || options.MaxSteps > 2000 || options.MaxPoints > 8001 || !double.IsFinite(now) || now < 0)
            throw new ArgumentOutOfRangeException(nameof(options));
        var points = ImmutableArray.CreateBuilder<CombatForecastPoint>();
        var targets = ImmutableArray.CreateBuilder<CombatForecastPoint>();
        var legs = ImmutableArray.CreateBuilder<CombatForecastLeg>();
        var route = initial;
        if (initial.Segments.IsDefaultOrEmpty)
            return new(id, targetId, now, now, CombatForecastStatus.InsufficientData, [], [], []);
        // Route.ElapsedMs is relative to the displayed sample, and can be fractional.
        double routeOffset = initial.ElapsedMs - (now - initial.StartMotionTimeMs);
        if (Math.Abs(routeOffset) > 1e-6)
            return new(id, targetId, now, now, CombatForecastStatus.InsufficientData, [], [], []);
        double end = Math.Max(now, Math.Min(now + options.HorizonMs, expires ?? double.PositiveInfinity));
        double cursor = now;
        var status = initialStatus;
        Add(now);
        int steps = 0;
        while (cursor < end)
        {
            // Cancellation is a normal forecast budget/lifecycle limit. Return an
            // incomplete result without throwing a first-chance debugger exception.
            if (cancellationToken.IsCancellationRequested)
            { status = CombatForecastStatus.CalculationLimited; break; }
            if (steps++ >= options.MaxSteps || points.Count + 2 > options.MaxPoints)
            { status = CombatForecastStatus.CalculationLimited; break; }
            double next = Math.Min(end, CombatGuidanceRules.NextBoundary((long)Math.Floor(cursor)));
            if (target is not null)
            {
                var activeRoute = route;
                var path = new CollisionPath(t =>
                {
                    var p = TorpedoGuidanceMath.PredictPose(activeRoute, t - activeRoute.StartMotionTimeMs);
                    return (p.X, p.Y);
                }, speed / 100, speed / 100 * turnRate * Math.PI / 180_000);
                var contact = TorpedoCollisionMath.FirstContact(path, new(target, targetSpeedBound), cursor, next);
                if (contact is { } hit) { next = hit.MotionTimeMs; status = CombatForecastStatus.Contact; }
            }
            legs.Add(new(cursor, next, route));
            Add((cursor + next) / 2);
            Add(next);
            cursor = next;
            if (status == CombatForecastStatus.Contact || cursor >= end) break;
            if (steps >= options.MaxSteps || points.Count + 2 > options.MaxPoints)
            { status = CombatForecastStatus.CalculationLimited; break; }
            if (target is not null)
            {
                var pose = TorpedoGuidanceMath.PredictPose(route, cursor - route.StartMotionTimeMs);
                route = replan(new(id, pose.X, pose.Y, speed, pose.Direction), route, (long)cursor);
            }
        }
        if (cursor >= end && status == CombatForecastStatus.NoContact && expires <= end) status = CombatForecastStatus.Expired;
        return new(id, targetId, now, cursor, status, points.ToImmutable(), targets.ToImmutable(), legs.ToImmutable());

        void Add(double time)
        {
            if (points.Count != 0 && points[^1].MotionTimeMs == time) return;
            var p = TorpedoGuidanceMath.PredictPose(route, time - route.StartMotionTimeMs);
            points.Add(new(time, p.X, p.Y));
            if (target is not null) { var q = target(time); targets.Add(new(time, q.X, q.Y)); }
        }
    }
}
