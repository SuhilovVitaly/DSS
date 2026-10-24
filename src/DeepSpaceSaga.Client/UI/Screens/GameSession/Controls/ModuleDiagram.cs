using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>Own authoritative module health; journal effects use real presentation time.</summary>
internal sealed class ModuleDiagram
{
    internal sealed record Tile(string Id, string Name, int Armor, int ArmorMax, int Structure,
        int StructureMax, bool Critical, bool Destroyed, double DisabledRealSeconds, int? HitDamage);
    internal IReadOnlyList<Tile> Tiles { get; private set; } = [];
    internal int DestroyedCount => Tiles.Count(t => t.Destroyed);
    private Guid? _session;
    private ulong _sequence;
    private long _cursor;
    private bool _initialized;
    private readonly Dictionary<string, (int Damage, double Started)> _hits = new(StringComparer.Ordinal);
    private ImmutableArray<InstalledModuleSnapshot> _lastModules;
    private double _lastMotion, _lastMultiplier;

    internal void Update(Guid? session, ulong sequence, string owner, ImmutableArray<InstalledModuleSnapshot> modules,
        ImmutableArray<CombatJournalEntry> journal, double motionMs, double multiplier, double realMs)
    {
        bool baseline = !_initialized || _session != session || sequence < _sequence;
        if (!baseline && sequence == _sequence && modules == _lastModules && motionMs == _lastMotion &&
            multiplier == _lastMultiplier && _hits.Count == 0) return;
        _lastModules = modules; _lastMotion = motionMs; _lastMultiplier = multiplier;
        if (baseline) { _hits.Clear(); _cursor = journal.IsDefaultOrEmpty ? 0 : journal.Max(e => e.EventId); }
        _initialized = true; _session = session; _sequence = sequence;
        if (!journal.IsDefaultOrEmpty)
            foreach (var entry in journal.Where(e => e.EventId > _cursor).OrderBy(e => e.EventId))
            {
                if (entry.TargetObjectId == owner && entry.HitModuleId is { } id && entry.Damage is > 0)
                    _hits[id] = (entry.Damage.Value, realMs);
                _cursor = entry.EventId;
            }
        foreach (var id in _hits.Where(p => realMs - p.Value.Started >= 800).Select(p => p.Key).ToArray()) _hits.Remove(id);
        Tiles = modules.IsDefaultOrEmpty ? [] : modules.OrderBy(m => m.Position).ThenBy(m => m.ModuleId, StringComparer.Ordinal)
            .Select(m => new Tile(m.ModuleId, Localization.CombatModuleName(m.ModuleTypeId, m.DisplayName), m.ArmorCurrent, m.ArmorMax, m.StructureCurrent,
                m.StructureMax, m.IsCritical, m.IsDestroyed,
                Math.Max(0, (m.DisabledUntilMotionTimeMs ?? motionMs) - motionMs) / 1000 / (multiplier > 0 ? multiplier : .25),
                _hits.TryGetValue(m.ModuleId, out var hit) ? hit.Damage : null)).ToArray();
    }
}
