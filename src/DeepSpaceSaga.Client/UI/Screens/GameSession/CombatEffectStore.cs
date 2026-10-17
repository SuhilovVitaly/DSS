using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Session-local presentation state; never serialized or advanced by game time.</summary>
internal sealed class CombatEffectStore
{
    internal const double LifetimeMs = 2000;
    private readonly Func<long> _timestampProvider;
    private readonly Func<long, long?>? _receiptTimestampProvider;
    internal bool StartOnPresentation { get; set; }
    private readonly HashSet<long> _seen = new();
    private long _impactFloor;
    private long _journalFloor;
    private readonly List<CombatEffect> _active = new();
    internal IReadOnlyList<CombatEffect> Active => _active;
    private bool _journalInitialized = true;
    private readonly HashSet<long> _seenJournal = new();
    private readonly List<(CombatJournalEntry Entry, long Started)> _results = new();
    internal IReadOnlyList<(CombatJournalEntry Entry, long Started)> Results => _results;
    internal void ResetJournal(ImmutableArray<CombatJournalEntry> baseline = default)
    {
        _seenJournal.Clear(); _results.Clear(); _journalFloor = 0;
        _journalInitialized = !baseline.IsDefault;
        if (!baseline.IsDefaultOrEmpty) foreach (var entry in baseline) _seenJournal.Add(entry.EventId);
        PruneSeen(_seenJournal, ref _journalFloor);
    }
    internal void ReceiveJournal(ImmutableArray<CombatJournalEntry> entries, long receivedAt)
    {
        if (!_journalInitialized && !entries.IsDefault) { ResetJournal(entries); return; }
        if (!entries.IsDefaultOrEmpty)
            foreach (var entry in entries)
                if (entry.EventId > _journalFloor && _seenJournal.Add(entry.EventId) && entry.Type is CombatEventType.Intercept or CombatEventType.Miss)
                    _results.Add((entry, StartOnPresentation ? _timestampProvider() : receivedAt));
        PruneSeen(_seenJournal, ref _journalFloor);
        long now = _timestampProvider();
        _results.RemoveAll(e => (double)(now - e.Started) * 1000 / Stopwatch.Frequency >= LifetimeMs);
    }

    internal CombatEffectStore(Func<long> timestampProvider, Func<long, long?>? receiptTimestampProvider = null)
    {
        _timestampProvider = timestampProvider;
        _receiptTimestampProvider = receiptTimestampProvider;
    }

    internal void Reset(ImmutableArray<CombatImpactSnapshot> baseline = default)
    {
        _seen.Clear();
        _impactFloor = 0;
        _active.Clear();
        if (!baseline.IsDefaultOrEmpty)
            foreach (var impact in baseline) _seen.Add(impact.EventId);
        PruneSeen(_seen, ref _impactFloor);
    }

    internal void Receive(ImmutableArray<CombatImpactSnapshot> impacts, long? receivedAtTimestamp = null)
    {
        long now = _timestampProvider();
        if (!impacts.IsDefaultOrEmpty)
            foreach (var impact in impacts)
                if (impact.EventId > _impactFloor && _seen.Add(impact.EventId)) _active.Add(new(impact, StartOnPresentation ? now : _receiptTimestampProvider?.Invoke(impact.EventId) ?? receivedAtTimestamp ?? now));
        PruneSeen(_seen, ref _impactFloor);
        _active.RemoveAll(effect => effect.Progress(now) >= 1);
    }

    private static void PruneSeen(HashSet<long> seen, ref long floor)
    {
        if (seen.Count <= 4096) return;
        foreach (long id in seen.Order().Take(seen.Count - 4096).ToArray())
        {
            seen.Remove(id);
            floor = Math.Max(floor, id);
        }
    }

    internal readonly record struct CombatEffect(CombatImpactSnapshot Impact, long StartedAtTimestamp)
    {
        internal ImmutableArray<TrailSegment> TerminalTrail => Impact.TerminationKind == TorpedoTerminationKind.SelfDestruct
            ? Impact.FinalTrail : [];
        internal double Progress(long now) => Math.Clamp(
            ((double)now - StartedAtTimestamp) * 1000 / Stopwatch.Frequency / LifetimeMs, 0, 1);
        internal float RadiusPx(long now) => (float)(50 * Progress(now));
        internal byte Alpha(long now, byte paletteAlpha) => (byte)Math.Round(paletteAlpha * (1 - Progress(now)));
    }
}
