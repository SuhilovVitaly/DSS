using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI;

/// <summary>Checks the live station visit before opening or retaining station overlays.</summary>
internal sealed class StationTradeNavigation(Func<GameSessionHandle?> currentHandle)
{
    private readonly object _gate = new();
    private Task? _inFlight;

    internal static bool CanOpen(AuthoritativeSnapshot? snapshot) =>
        TradeModel.ResolveLocalStationId(snapshot) is not null;

    internal Task RemoveInvalidOverlaysAsync(ScreenStack stack,
        Func<AuthoritativeSnapshot?> latest, Func<Task> popModalAsync)
    {
        lock (_gate)
        {
            if (_inFlight is not null) return _inFlight;
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _inFlight = completion.Task;
            _ = RemoveCoreAsync(stack, latest, popModalAsync, completion);
            return completion.Task;
        }
    }

    private async Task RemoveCoreAsync(ScreenStack stack, Func<AuthoritativeSnapshot?> latest,
        Func<Task> popModalAsync, TaskCompletionSource completion)
    {
        try
        {
            while (stack.Count > 1)
            {
                var handle = currentHandle();
                var snapshot = latest();
                bool invalid = stack.Current switch
                {
                    TradeScreen trade => !CanOpen(snapshot) ||
                        !ReferenceEquals(trade.OpenedForHandle, handle) ||
                        !trade.HasValidVisit,
                    StationScreen station => !CanOpen(snapshot) ||
                        !ReferenceEquals(station.OpenedForHandle, handle) ||
                        !station.HasValidVisit,
                    _ => false
                };
                if (!invalid) break;
                await popModalAsync();
            }
            completion.SetResult();
        }
        catch (Exception error)
        {
            completion.SetException(error);
        }
        finally
        {
            lock (_gate) _inFlight = null;
        }
    }
}
