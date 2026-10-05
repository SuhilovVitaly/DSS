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
    private readonly HashSet<long> _seen = new();
    private readonly List<CombatEffect> _active = new();
    internal IReadOnlyList<CombatEffect> Active => _active;
    private bool _journalInitialized = true;
    private readonly HashSet<long> _seenJournal = new();
    private readonly List<(CombatJournalEntry Entry, long Started)> _results = new();
    internal IReadOnlyList<(CombatJournalEntry Entry, long Started)> Results => _results;
    internal void ResetJournal(ImmutableArray<CombatJournalEntry> baseline = default)
    {
        _seenJournal.Clear(); _results.Clear();
        _journalInitialized = !baseline.IsDefault;
        if (!baseline.IsDefaultOrEmpty) foreach (var entry in baseline) _seenJournal.Add(entry.EventId);
    }
    internal void ReceiveJournal(ImmutableArray<CombatJournalEntry> entries, long receivedAt)
    {
        if (!_journalInitialized && !entries.IsDefault) { ResetJournal(entries); return; }
        if (!entries.IsDefaultOrEmpty)
            foreach (var entry in entries)
                if (_seenJournal.Add(entry.EventId) && entry.Type is CombatEventType.Intercept or CombatEventType.Miss)
                    _results.Add((entry, receivedAt));
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
        _active.Clear();
        if (!baseline.IsDefaultOrEmpty)
            foreach (var impact in baseline) _seen.Add(impact.EventId);
    }

    internal void Receive(ImmutableArray<CombatImpactSnapshot> impacts, long? receivedAtTimestamp = null)
    {
        long now = _timestampProvider();
        if (!impacts.IsDefaultOrEmpty)
            foreach (var impact in impacts)
                if (_seen.Add(impact.EventId)) _active.Add(new(impact, _receiptTimestampProvider?.Invoke(impact.EventId) ?? receivedAtTimestamp ?? now));
        _active.RemoveAll(effect => effect.Progress(now) >= 1);
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
