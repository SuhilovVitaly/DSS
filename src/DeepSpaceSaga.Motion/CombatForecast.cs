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
    public const int ModelVersion = 3;
    public double? ContactMotionTimeMs => Status == CombatForecastStatus.Contact ? EndMotionTimeMs : null;

    public (double X, double Y, double Direction) ProjectilePose(double time)
    {
        if (!Legs.IsDefaultOrEmpty) throw new InvalidOperationException("Guidance forecast legs are unsupported.");
        {
            if (Projectile.IsDefaultOrEmpty) throw new InvalidOperationException("Forecast has no projectile samples.");
            int lowPoint = 0, highPoint = Projectile.Length - 1;
            while (lowPoint < highPoint)
            {
                int mid = (lowPoint + highPoint) / 2;
                if (Projectile[mid].MotionTimeMs < time) lowPoint = mid + 1; else highPoint = mid;
            }
            int index = Math.Max(0, lowPoint - 1);
            var a = Projectile[index];
            var b = Projectile[Math.Min(index + 1, Projectile.Length - 1)];
            double progress = b.MotionTimeMs == a.MotionTimeMs ? 0 : (time - a.MotionTimeMs) / (b.MotionTimeMs - a.MotionTimeMs);
            var p = BlastMath.PositionAt(a.X, a.Y, b.X, b.Y, progress);
            double direction = Math.Atan2(b.X - a.X, a.Y - b.Y) * 180 / Math.PI;
            return (p.X, p.Y, (direction + 360) % 360);
        }
    }
}

/// <summary>Pure point-flight forecast using only captured own geometry or public observations.</summary>
public static class CombatForecast
{
    public static CombatForecastResult Torpedo(ObjectMotionSnapshot projectile, ObjectMotionSnapshot? target,
        double now, CombatForecastOptions? options = null, CancellationToken cancellationToken = default) =>
        PointProjectile(projectile, target, now, options, cancellationToken);

    public static CombatForecastResult Countermeasure(ObjectMotionSnapshot projectile, ObjectMotionSnapshot? target,
        double now, CombatForecastOptions? options = null, CancellationToken cancellationToken = default) =>
        PointProjectile(projectile, target, now, options, cancellationToken);

    /// <summary>Conditional area contact only at a known own detonation time. Foreign traces have no inferred endpoint.</summary>
    public static CombatForecastResult PointProjectile(ObjectMotionSnapshot projectile, ObjectMotionSnapshot? target,
        double now, CombatForecastOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= CombatForecastOptions.Default;
        if (options.HorizonMs is <= 0 or > 200_000 || options.MaxSteps is <= 0 or > 2000 ||
            options.MaxPoints is < 2 or > 8001 || !double.IsFinite(now) || now < 0)
            throw new ArgumentOutOfRangeException(nameof(options));
        var own = projectile.OwnProjectile;
        if (own is null && projectile.ObservedProjectile is null)
            throw new ArgumentException("Expected a point projectile.", nameof(projectile));
        if (own is not null && (!double.IsFinite(own.DetonationMotionTimeMs) ||
                own.LaunchMotionTimeMs < 0 || own.DetonationMotionTimeMs < own.LaunchMotionTimeMs || own.SpeedKmS <= 0 ||
                !double.IsFinite(own.SpeedKmS) || own.BlastRadiusKm <= 0 || !double.IsFinite(own.BlastRadiusKm) ||
                !double.IsFinite(own.CoreFraction) || own.CoreFraction is <= 0 or >= 1))
            throw new ArgumentException("Invalid captured projectile.", nameof(projectile));
        bool knownTarget = own is not null && target is { IsDestroyed: false } &&
            target.ObjectId != projectile.ObjectId && target.RenderObjectType != SpaceObjectType.UnknownSpaceObject &&
            CurrentManeuverForecast.HasRequiredData(target) &&
            (own.Kind == ProjectileKind.Countermissile ? IsProjectile(target) :
                !IsProjectile(target) && (target.ObjectType ?? target.RenderObjectType) is null or SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip);
        bool incompleteTarget = own is not null && target is { IsDestroyed: false } &&
            target.RenderObjectType != SpaceObjectType.UnknownSpaceObject && !CurrentManeuverForecast.HasRequiredData(target) &&
            (own.Kind == ProjectileKind.Countermissile ? IsProjectile(target) : !IsProjectile(target) &&
                (target.ObjectType ?? target.RenderObjectType) is null or SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip);
        var sampler = knownTarget ? CurrentManeuverForecast.CreateSampler(target!) : null;
        var points = ImmutableArray.CreateBuilder<CombatForecastPoint>();
        var targets = ImmutableArray.CreateBuilder<CombatForecastPoint>();
        double end = own is null ? now + options.HorizonMs : Math.Max(now, Math.Min(now + options.HorizonMs, own.DetonationMotionTimeMs));
        double? targetEnds = knownTarget && own!.Kind == ProjectileKind.Countermissile && target!.OwnProjectile is { } targetOwn &&
            (targetOwn.DetonationMotionTimeMs < own.DetonationMotionTimeMs ||
             targetOwn.DetonationMotionTimeMs == own.DetonationMotionTimeMs && targetOwn.Kind == ProjectileKind.Countermissile &&
             string.CompareOrdinal(target.ObjectId, projectile.ObjectId) < 0) ? targetOwn.DetonationMotionTimeMs : null;
        if (targetEnds is { } targetEnd) end = Math.Max(now, Math.Min(end, targetEnd));
        double cursor = now;
        var status = own is null || incompleteTarget ? CombatForecastStatus.InsufficientData : CombatForecastStatus.NoContact;
        if (projectile.IsDestroyed || own is not null && now >= own.DetonationMotionTimeMs)
            end = now;
        Add(now);
        int steps = 0;
        while (cursor < end)
        {
            if (cancellationToken.IsCancellationRequested || steps >= options.MaxSteps || points.Count >= options.MaxPoints)
            { status = CombatForecastStatus.CalculationLimited; break; }
            double next = Math.Min(end, cursor + 100);
            Add(next);
            cursor = next;
            steps++;
        }
        if (cancellationToken.IsCancellationRequested) status = CombatForecastStatus.CalculationLimited;
        else if (projectile.IsDestroyed) status = CombatForecastStatus.Expired;
        else if (targetEnds is { } targetDeadline && cursor >= targetDeadline) status = CombatForecastStatus.TargetTerminated;
        else if (own is not null && cursor >= own.DetonationMotionTimeMs)
        {
            status = incompleteTarget ? CombatForecastStatus.InsufficientData : CombatForecastStatus.Expired;
            if (knownTarget && now <= own.DetonationMotionTimeMs)
            {
                var p = points[^1];
                var q = targets[^1];
                double dx = (p.X - q.X) / 10, dy = (p.Y - q.Y) / 10;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                bool contact = own.Kind == ProjectileKind.Countermissile ? distance <= own.BlastRadiusKm :
                    Math.Round(own.Damage * BlastMath.Falloff(distance, own.BlastRadiusKm, own.CoreFraction), MidpointRounding.AwayFromZero) > 0;
                if (contact) status = CombatForecastStatus.Contact;
            }
        }
        return new(projectile.ObjectId, knownTarget ? target!.ObjectId : string.Empty, now, cursor,
            status, points.ToImmutable(), targets.ToImmutable(), []);

        void Add(double time)
        {
            (double X, double Y) p;
            if (own is null)
            {
                var sample = CurrentManeuverForecast.Sample(projectile, time - now);
                p = (sample.X, sample.Y);
            }
            else
            {
                double duration = own.DetonationMotionTimeMs - own.LaunchMotionTimeMs;
                double progress = duration == 0 ? 1 : (time - own.LaunchMotionTimeMs) / duration;
                p = BlastMath.PositionAt(own.StartX, own.StartY, own.DetonationX, own.DetonationY, progress);
            }
            points.Add(new(time, p.X, p.Y));
            if (sampler is not null) { var q = sampler(time - now); targets.Add(new(time, q.X, q.Y)); }
        }
    }

    private static bool IsProjectile(ObjectMotionSnapshot target) => target.OwnProjectile is not null || target.ObservedProjectile is not null ||
        target.ObjectType is SpaceObjectType.Missile or SpaceObjectType.Countermeasure;

}
