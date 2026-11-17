using System.Runtime.CompilerServices;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us3Tk3Tests
{
    [Fact]
    public async Task CapturedViewCannotEscapeOrSubmitFromRenderingAndReportsRefusal()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        var flow = new TacticalBattleFlow(handle) { CampaignEncounter = new(new("battle:g", "encounter:g", "g"), "captured", true) };
        var screen = new CapturedEncounterScreen(flow);
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 800, 600);
        screen.OnActivated(); screen.OnDeactivated(); screen.OnActivated();
        Assert.Equal(0, connection.Calls);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.F8));
        Assert.Equal(ScreenEvent.StartTacticalBattle, screen.OnKeyDown(Key.Enter));
        Assert.False(await flow.StartEncounterAsync());
        Assert.Equal(1, connection.Calls);
        Assert.Contains(Localization.Get("Demo.Encounter.sensor_report_pending"), flow.Error);
        Assert.Null(flow.State);
    }

    private sealed class Connection : IGameSessionConnection
    {
        public int Calls;
        public ValueTask<DemoCampaignResponse> ExecuteDemoCampaignAsync(DemoCampaignCommand command, CancellationToken cancellationToken = default)
        { Calls++; return ValueTask.FromResult(new DemoCampaignResponse(command.CommandId, false, "sensor_report_pending", 0)); }
        public ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<TacticalBattleSnapshot?>(null);
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.Infinite, cancellationToken); yield break; }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
