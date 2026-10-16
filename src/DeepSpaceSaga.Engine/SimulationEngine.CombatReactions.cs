using System.Globalization;
using System.Text.Json;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private readonly Dictionary<(string Kind, string Owner, string Module, string Target), CombatReactionSaveData> _combatReactions = [];

    private long? ReactionTime(string kind, string owner, string module, string target, long eligibleAt, double readyAt)
    {
        var key = (kind, owner, module, target);
        if (!_combatReactions.TryGetValue(key, out var pending) || pending.ReadyAt != readyAt)
        {
            // Event-addressed independent stream: evaluation order and repeated scheduler queries consume no combat RNG.
            string address = JsonSerializer.Serialize(new[] { kind, owner, module, target,
                eligibleAt.ToString(CultureInfo.InvariantCulture), readyAt.ToString("R", CultureInfo.InvariantCulture) });
            var rng = new ModuleHitLocationRng(RngStreamSeedDerivation.DeriveStreamSeed(MasterSeed,
                "combat.automatic-reaction.v1:" + address), 0);
            long delay = 500 + (long)rng.NextBelow(751);
            if (eligibleAt > long.MaxValue - delay) return null;
            pending = new(kind, owner, module, target, eligibleAt, readyAt, eligibleAt + delay);
            _combatReactions[key] = pending;
        }
        return Math.Max(eligibleAt, pending.DueAt);
    }

    private void PruneCombatReactions()
    {
        foreach (var key in _combatReactions.Keys.Where(k =>
            !_objects.Any(o => o.InitialMotion.ObjectId == k.Owner && !o.IsDestroyed) ||
            !_objects.Any(o => o.InitialMotion.ObjectId == k.Target && !o.IsDestroyed)).ToArray())
            _combatReactions.Remove(key);
    }

    private void ClearCombatReactions(string owner, string module, string? kind = null)
    {
        foreach (var key in _combatReactions.Keys.Where(k => k.Owner == owner && k.Module == module && (kind is null || k.Kind == kind)).ToArray())
            _combatReactions.Remove(key);
    }

    private CombatReactionSaveData[] CaptureCombatReactions() => _combatReactions.Values
        .Where(r => _objects.Any(o => o.InitialMotion.ObjectId == r.OwnerId && !o.IsDestroyed) &&
            _objects.Any(o => o.InitialMotion.ObjectId == r.TargetId && !o.IsDestroyed))
        .OrderBy(r => r.Kind, StringComparer.Ordinal).ThenBy(r => r.OwnerId, StringComparer.Ordinal)
        .ThenBy(r => r.ModuleId, StringComparer.Ordinal).ThenBy(r => r.TargetId, StringComparer.Ordinal).ToArray();

    private static void ValidateCombatReactions(GameStateData state)
    {
        var seen = new HashSet<(string, string, string, string)>();
        foreach (var r in state.CombatReactionDelays ?? [])
        {
            if (r is null || r.Kind is not ("hostile" or "defense") || string.IsNullOrWhiteSpace(r.OwnerId) ||
                string.IsNullOrWhiteSpace(r.ModuleId) || string.IsNullOrWhiteSpace(r.TargetId) ||
                r.EligibleAt < 0 || r.EligibleAt > state.MotionTimeMs || !double.IsFinite(r.ReadyAt) || r.ReadyAt < 0 || r.ReadyAt > r.EligibleAt ||
                r.DueAt < r.EligibleAt || r.DueAt - r.EligibleAt is < 500 or > 1250 ||
                r.Kind == "defense" && r.OwnerId != state.PlayerShipObjectId ||
                !seen.Add((r.Kind, r.OwnerId, r.ModuleId, r.TargetId)) ||
                !state.SpaceObjects.Any(o => o.ObjectId == r.OwnerId && !o.IsDestroyed &&
                    o.Modules?.Any(m => m.ModuleId == r.ModuleId) == true) ||
                !state.SpaceObjects.Any(o => o.ObjectId == r.TargetId && !o.IsDestroyed))
                throw new ScenarioException("Invalid automatic combat reaction delay.");
        }
    }

    private void RestoreCombatReactions(GameStateData state)
    {
        _combatReactions.Clear();
        foreach (var r in state.CombatReactionDelays ?? [])
            _combatReactions.Add((r.Kind, r.OwnerId, r.ModuleId, r.TargetId), r);
    }
}
