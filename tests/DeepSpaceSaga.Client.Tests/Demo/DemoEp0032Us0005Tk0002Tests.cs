using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0032Us0005Tk0002Tests
{
    [Fact]
    public async Task AutosaveWaitsForAdoptionAndDurableAcknowledgement()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        for (int i = 0; i < 100 && handle.DemoCampaign is null; i++) await Task.Delay(10);
        Assert.NotNull(handle.DemoCampaign);
        Assert.Equal(0, connection.Calls);
        handle.ActivateAutosave();
        await connection.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Saving", handle.AutosaveStatus);
        connection.Written.SetResult();
        await handle.SuspendAutosaveAsync();
        Assert.Equal("Saved", handle.AutosaveStatus);
        Assert.Equal(1, connection.Calls);
    }

    [Fact]
    public async Task FailedWriteIsVisibleAndRetryDoesNotRepeatGameplay()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        for (int i = 0; i < 100 && handle.DemoCampaign is null; i++) await Task.Delay(10);
        handle.ActivateAutosave();
        await connection.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        connection.Written.SetException(new IOException("disk full"));
        for (int i = 0; i < 100 && handle.AutosaveStatus == "Saving"; i++) await Task.Delay(10);
        Assert.Equal("Failed", handle.AutosaveStatus);
        connection.Written = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var retry = handle.RetryAutosaveAsync();
        connection.Written.SetResult();
        await retry;
        Assert.Equal("Saved", handle.AutosaveStatus);
        Assert.Equal(2, connection.Calls);
        await handle.SuspendAutosaveAsync();
        await handle.RetryAutosaveAsync();
        Assert.Equal(2, connection.Calls);
    }

    [Fact]
    public void ContinueButtonUsesActualMenuHitPath()
    {
        var screen = new MainMenuScreen(true);
        using var bitmap = new SKBitmap(1000, 800);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1000, 800);
        Assert.Equal(ScreenEvent.ContinueCampaign, screen.OnMouseDown(500,
            MenuLayout.PanelTop(800) + MenuLayout.CreditsY + 20, MouseButton.Left));
    }

    private sealed class Connection : IGameSessionConnection
    {
        public readonly TaskCompletionSource Called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Written = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls;
        public ValueTask RequestAutosaveAsync(long sequence, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Calls); Called.TrySetResult(); return new(Written.Task); }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new(1, 0, SimulationSpeed.Speed0, [], DemoCampaign: new(1, 1,
                Start: new(1, "demo", 32, "home", [new("home", "service")], "shared")));
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
