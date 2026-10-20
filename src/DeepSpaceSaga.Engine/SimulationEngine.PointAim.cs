using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal sealed record NpcPointAim(string ProfileId, double X, double Y, double FlightSeconds, double ExpectedMissKm, double Coverage);

    internal NpcPointAim? CaptureNpcPointAim(string ownerId, string moduleId, long time)
    {
        lock (_worldStateLock)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId && IsHostileActor(o));
            var module = owner?.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
            var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
            return owner is null || module is null || player is null ? null : FindNpcPointAim(owner, module, player, time);
        }
    }

    private NpcPointAim? FindNpcPointAim(SpaceObjectRuntime owner, InstalledModuleRuntime module, SpaceObjectRuntime player, long time)
    {
        if (time < 0 || owner.AttackAi is not { } ai || _registry.CombatRules is not { Blast: { } blast, Scatter: { } scatter }) return null;
        var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        var op = ResolveWeaponOperator(owner, module, WeaponSkillType.TorpedoAttack);
        if (!type.IsPointLauncher || op is null) return null;
        var origin = RuntimeMotion.At(owner, time);
        var target = RuntimeMotion.At(player, time);
        double angle = target.Direction * Math.PI / 180;
        double vx = target.SpeedKmS * Math.Sin(angle), vy = -target.SpeedKmS * Math.Cos(angle);
        double x = (target.X - origin.X) / 10, y = (target.Y - origin.Y) / 10;
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(vx) || !double.IsFinite(vy)) return null;
        double acceleration = player.Modules.Select(m => (Module: m, Type: _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex)))
            .Where(p => CanExecuteEngineCommand(p.Module, p.Type, time))
            .Select(p => (p.Type.LinearInertiaMps2 ?? 0) / 1000.0).DefaultIfEmpty(0).Max();
        NpcPointAim? best = null;
        foreach (var profile in type.Profiles.OrderBy(p => p.ProfileId, StringComparer.Ordinal))
        {
            double? flight = PointInterceptSeconds(x, y, vx, vy, profile.SpeedKmS);
            if (flight is not { } tau || tau * 1000 >= long.MaxValue - (double)time) continue;
            double range = profile.SpeedKmS * tau;
            if (!double.IsFinite(range) || range > type.RangeKm!.Value) continue;
            double px = target.X + vx * tau * 10, py = target.Y + vy * tau * 10;
            double sigma = BlastMath.ScatterSigma(range, type.ScatterPercent!.Value, op.Skill,
                scatter.SkillPivot, scatter.SkillSlope, scatter.MinMultiplier);
            double miss = ai.AssumedTargetDodge * .5 * acceleration * Math.Pow(Math.Max(0, tau - 1), 2) + sigma;
            if (!double.IsFinite(miss)) continue;
            double coverage = BlastMath.Falloff(miss, profile.BlastRadiusKm, blast.CoreFraction);
            // One representable step admits the exact mathematical threshold after falloff rounding.
            if (coverage < Math.BitDecrement(ai.FireThreshold) || !double.IsFinite(px) || !double.IsFinite(py)) continue;
            bool friendlyDanger = _objects.Any(friend => !friend.IsDestroyed &&
                friend.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
                (friend.InitialMotion.ObjectId == owner.InitialMotion.ObjectId || GetRelationToPlayer(friend) == GetRelationToPlayer(owner)) &&
                FriendlyWithinBlast(RuntimeMotion.At(friend, time), px, py, tau, profile.BlastRadiusKm + 1));
            if (!friendlyDanger && (best is null || coverage > best.Coverage)) best = new(profile.ProfileId, px, py, tau, miss, coverage);
        }
        return best;
    }

    private static bool FriendlyWithinBlast(ObjectMotionSnapshot friend, double x, double y, double tau, double radius)
    {
        double angle = friend.Direction * Math.PI / 180;
        double dx = (friend.X - x) / 10 + friend.SpeedKmS * Math.Sin(angle) * tau;
        double dy = (friend.Y - y) / 10 - friend.SpeedKmS * Math.Cos(angle) * tau;
        return dx * dx + dy * dy < radius * radius;
    }

    private static double? PointInterceptSeconds(double x, double y, double vx, double vy, double speed)
    {
        double c = x * x + y * y;
        if (c == 0) return 0;
        double a = vx * vx + vy * vy - speed * speed, b = 2 * (x * vx + y * vy);
        if (Math.Abs(a) <= 1e-12 * Math.Max(1, speed * speed + vx * vx + vy * vy)) return b < 0 ? -c / b : null;
        double discriminant = b * b - 4 * a * c;
        if (discriminant < 0 || !double.IsFinite(discriminant)) return null;
        double q = -.5 * (b + Math.CopySign(Math.Sqrt(discriminant), b));
        double first = q / a, second = q == 0 ? double.PositiveInfinity : c / q;
        double result = Math.Min(first >= 0 ? first : double.PositiveInfinity, second >= 0 ? second : double.PositiveInfinity);
        return double.IsFinite(result) ? result : null;
    }
}
