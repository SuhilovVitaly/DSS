using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class BattleResultsScreen
{
    private CancellationTokenSource? _lootTicker;
    private readonly object _progressGate = new();
    private Dictionary<string, long> _estimatedDeadline = new(StringComparer.Ordinal);

    public override void OnActivated()
    {
        base.OnActivated();
        _lootTicker = new();
        _ = UpdateLootAsync(_lootTicker.Token);
    }

    public override void OnDeactivated()
    {
        _lootTicker?.Cancel();
        _lootTicker?.Dispose();
        _lootTicker = null;
        base.OnDeactivated();
    }

    private async Task UpdateLootAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                while (!cancellationToken.IsCancellationRequested && CurrentLoot is { } loot)
                {
                    SynchronizeDeadlines(loot);
                    if (LootInputBlocked || !loot.Operations.Any(op => Progress(op) >= 1)) break;
                    bool applied = await _lootPresentation.SubmitAsync((id, revision, battle, _) =>
                        new AdvanceLootOperationsCommand(id, revision, battle), needsSource: false).ConfigureAwait(false);
                    if (!applied) break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void SynchronizeDeadlines(LootSessionSnapshot loot)
    {
        long now = Environment.TickCount64;
        lock (_progressGate)
        {
            _estimatedDeadline = loot.Operations.ToDictionary(op => op.OperationId,
                op => Math.Min(_estimatedDeadline.GetValueOrDefault(op.OperationId, long.MaxValue), now + op.RemainingMs),
                StringComparer.Ordinal);
        }
    }

    private float Progress(LootOperationSnapshot operation)
    {
        long now = Environment.TickCount64;
        long deadline;
        lock (_progressGate)
            deadline = _estimatedDeadline.GetValueOrDefault(operation.OperationId, now + operation.RemainingMs);
        return 1 - (float)Math.Clamp(deadline - now, 0, operation.DurationMs) / operation.DurationMs;
    }

    internal static SKRect ProgressFill(SKRect track, float progress) =>
        new(track.Left, track.Bottom - track.Height * Math.Clamp(progress, 0, 1), track.Right, track.Bottom);
}
