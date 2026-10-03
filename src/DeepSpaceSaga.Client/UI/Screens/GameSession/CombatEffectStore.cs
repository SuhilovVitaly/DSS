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
        internal double Progress(long now) => Math.Clamp(
            ((double)now - StartedAtTimestamp) * 1000 / Stopwatch.Frequency / LifetimeMs, 0, 1);
        internal float RadiusPx(long now) => (float)(50 * Progress(now));
        internal byte Alpha(long now, byte paletteAlpha) => (byte)Math.Round(paletteAlpha * (1 - Progress(now)));
    }
}
