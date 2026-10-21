using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Session-local presentation state; never serialized or advanced by game time.</summary>
internal sealed class CombatEffectStore
{
    internal const double LifetimeMs = 2000;
    private readonly Func<long> _timestampProvider;
    internal bool StartOnPresentation { get; set; }
    private long _journalFloor;
    private bool _journalInitialized = true;
    private readonly HashSet<long> _seenJournal = new();
    private readonly List<(CombatJournalEntry Entry, long Started)> _pointEffects = new();
    internal IReadOnlyList<(CombatJournalEntry Entry, long Started)> PointEffects => _pointEffects;
    internal PointCombatVisualSettings PointStyles { get; set; } = PointCombatVisualSettings.Default;
    private readonly List<(CombatJournalEntry Entry, long Started)> _hitResults = [];
    internal IReadOnlyList<(CombatJournalEntry Entry, long Started)> HitResults => _hitResults;
    private bool _hitResultsInitialized;
    internal void ResetJournal(ImmutableArray<CombatJournalEntry> baseline = default)
    {
        _seenJournal.Clear(); _journalFloor = 0;
        _pointEffects.Clear(); _hitResults.Clear(); _hitResultsInitialized = !baseline.IsDefault;
        _journalInitialized = !baseline.IsDefault;
        if (!baseline.IsDefaultOrEmpty) foreach (var entry in baseline) _seenJournal.Add(entry.EventId);
        PruneSeen(_seenJournal, ref _journalFloor);
    }
    internal void ReceiveJournal(ImmutableArray<CombatJournalEntry> entries, long receivedAt)
    {
        if (!_journalInitialized && !entries.IsDefault) { ResetJournal(entries); return; }
        bool baselineHits = !_hitResultsInitialized && !entries.IsDefault;
        if (!entries.IsDefault) _hitResultsInitialized = true;
        if (!entries.IsDefaultOrEmpty)
            foreach (var entry in entries)
                if (entry.EventId > _journalFloor && _seenJournal.Add(entry.EventId))
                {
                    long started = StartOnPresentation ? _timestampProvider() : receivedAt;
                    if (!baselineHits && entry.HitResult is not null) _hitResults.Add((entry, started));
                    if (entry.Type is CombatEventType.TorpedoDetonated or CombatEventType.CountermissileDetonated or
                        CombatEventType.ManuallyDetonated or CombatEventType.ShotDown) _pointEffects.Add((entry, started));
                }
        PruneSeen(_seenJournal, ref _journalFloor);
        long now = _timestampProvider();
        _hitResults.RemoveAll(e => (double)(now - e.Started) * 1000 / Stopwatch.Frequency >= LifetimeMs);
        _pointEffects.RemoveAll(e => PointProgress(e.Entry, e.Started, now) >= 1);
    }

    internal double PointProgress(CombatJournalEntry entry, long started, long now) => Math.Clamp(
        (now - (double)started) * 1000 / Stopwatch.Frequency /
        (entry.Type == CombatEventType.ShotDown ? PointStyles.ShotDownDurationRealMs : PointStyles.BlastDurationRealMs), 0, 1);

    internal CombatEffectStore(Func<long> timestampProvider) => _timestampProvider = timestampProvider;

    private static void PruneSeen(HashSet<long> seen, ref long floor)
    {
        if (seen.Count <= 4096) return;
        foreach (long id in seen.Order().Take(seen.Count - 4096).ToArray())
        {
            seen.Remove(id);
            floor = Math.Max(floor, id);
        }
    }

}
