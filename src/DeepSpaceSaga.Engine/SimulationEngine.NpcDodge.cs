using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal sealed record NpcDodgeThreat(string OwnerId, string EngineModuleId, string ProjectileId,
        long DetectedAt, long DueAt, bool Evaluated = false, bool? RollSucceeded = null, bool Evaded = false);
    private readonly Dictionary<(string Owner, string Projectile), NpcDodgeThreat> _npcDodgeThreats = [];
    // Reuse the bounded SplitMix integer primitive with an independent named seed/state.
    private ModuleHitLocationRng _npcDodgeRng = new(0, 0);
    private bool HasPendingNpcDodge => _npcDodgeThreats.Values.Any(t => !t.Evaluated);
    private long NextNpcDodgeTime => _npcDodgeThreats.Values.Where(t => !t.Evaluated).Select(t => t.DueAt).DefaultIfEmpty(long.MaxValue).Min();

    internal (ulong State, ulong Counter) CaptureNpcDodgeRngState() { lock (_worldStateLock) return (_npcDodgeRng.State, _npcDodgeRng.Counter); }
    internal NpcDodgeThreat[] CaptureNpcDodgeThreats()
    {
        lock (_worldStateLock) return _npcDodgeThreats.Values.OrderBy(t => t.DueAt)
            .ThenBy(t => t.OwnerId, StringComparer.Ordinal).ThenBy(t => t.ProjectileId, StringComparer.Ordinal).ToArray();
    }

    private void ObserveNpcDodgeThreats(long time)
    {
        if (_registry.CombatRules?.Npc is not { } rules) return;
        foreach (var owner in _objects.Where(o => IsHostileActor(o) && o.AttackAi is not null).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal))
        {
            var engine = owner.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal).FirstOrDefault(m =>
                IsEngineCategory(_registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex)));
            if (engine is null) continue;
            int skill = owner.Crew.FirstOrDefault(c => c.Id == engine.OperatorCrewId)?.ManeuverabilitySkill ?? 0;
            long delay = (long)Math.Max(rules.ReactionMinMs, rules.ReactionBaseMs - (decimal)rules.ReactionPerSkillMs * skill);
            if (time > long.MaxValue - delay) continue;
            foreach (var projectile in _objects.Where(o => o.InitialMotion.OwnProjectile is { Kind: ProjectileKind.Torpedo } f &&
                !o.IsDestroyed && f.OwnerObjectId != owner.InitialMotion.ObjectId).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal))
            {
                var key = (owner.InitialMotion.ObjectId, projectile.InitialMotion.ObjectId);
                if (_npcDodgeThreats.ContainsKey(key) || !NpcRayThreat(owner, projectile, time)) continue;
                _npcDodgeThreats.Add(key, new(key.Item1, engine.ModuleId, key.Item2, time, time + delay));
            }
        }
    }

    private static bool NpcRayThreat(SpaceObjectRuntime owner, SpaceObjectRuntime projectile, long time)
    {
        if (projectile.InitialMotion.OwnProjectile is not { Kind: ProjectileKind.Torpedo } flight) return false;
        var ship = RuntimeMotion.At(owner, time); var shot = RuntimeMotion.At(projectile, time);
        double s = ship.Direction * Math.PI / 180, p = shot.Direction * Math.PI / 180;
        return BlastThreatMath.Evaluate(ship.X, ship.Y, ship.SpeedKmS * Math.Sin(s), -ship.SpeedKmS * Math.Cos(s),
            shot.X, shot.Y, Math.Sin(p), -Math.Cos(p), shot.SpeedKmS, flight.BlastRadiusKm).IsThreat;
    }

    private NpcDodgeThreat? NextNpcDodge(double cursor, long to) => _npcDodgeThreats.Values
        .Where(t => !t.Evaluated && t.DueAt >= cursor && t.DueAt <= to).OrderBy(t => t.DueAt)
        .ThenBy(t => t.OwnerId, StringComparer.Ordinal).ThenBy(t => t.ProjectileId, StringComparer.Ordinal).FirstOrDefault();

    private void ExecuteNpcDodge(NpcDodgeThreat episode)
    {
        var key = (episode.OwnerId, episode.ProjectileId);
        _npcDodgeThreats[key] = episode with { Evaluated = true };
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == episode.OwnerId && IsHostileActor(o));
        var projectile = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == episode.ProjectileId && !o.IsDestroyed);
        var engine = owner?.Modules.FirstOrDefault(m => m.ModuleId == episode.EngineModuleId);
        if (owner is null || projectile is null || engine is null || owner.IsDocked ||
            !CanExecuteEngineCommand(engine, _registry.ModuleTypes.GetDefinition(engine.ModuleTypeIndex), episode.DueAt) ||
            !NpcRayThreat(owner, projectile, episode.DueAt)) return;
        ulong numerator = (ulong)Math.Round(_registry.CombatRules!.Npc!.DodgeChance * 1_000_000), denominator = 1_000_000;
        ulong a = numerator, b = denominator;
        while (b != 0) { ulong remainder = a % b; a = b; b = remainder; }
        // Reduce the fraction: the default 0.1 preserves the existing seeded 1-in-10 stream.
        bool success = _npcDodgeRng.NextBelow(denominator / a) < numerator / a;
        _npcDodgeThreats[key] = episode with { Evaluated = true, RollSucceeded = success };
        if (!success) return;
        var ship = RuntimeMotion.At(owner, episode.DueAt); var shot = RuntimeMotion.At(projectile, episode.DueAt);
        double angle = shot.Direction * Math.PI / 180;
        double cross = Math.Sin(angle) * (ship.Y - shot.Y) + Math.Cos(angle) * (ship.X - shot.X);
        var side = cross > 1e-9 ? EvadeSide.Right : EvadeSide.Left;
        var result = TryEvade(owner, engine.ModuleId, episode.ProjectileId, side, episode.DueAt);
        _npcDodgeThreats[key] = episode with { Evaluated = true, RollSucceeded = true, Evaded = result.Disposition == CommandStartDisposition.Started };
    }
}
