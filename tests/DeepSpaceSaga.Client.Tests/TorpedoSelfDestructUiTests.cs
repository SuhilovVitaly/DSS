using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class TorpedoSelfDestructUiTests
{
    private static readonly InstalledModuleSnapshot Launcher = new("launcher", "module.torpedo.launcher.basic", "Launcher", 1,
        [CombatCommandTypes.Fire, CombatCommandTypes.SelfDestruct], "On", "Ready", 60,
        Commands: [new(CombatCommandTypes.Fire, "Fire", "object"), new(CombatCommandTypes.SelfDestruct, "Самоуничтожение", "none")],
        LauncherCombat: new("active-flight", 3, 90, 150));
    [Fact]
    public async Task Self_destruct_button_uses_launcher_active_id_on_pause()
    {
        await using var f = new Fixture();
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        f.Screen.Render(canvas, 1280, 720);
        var button = Assert.Single(f.Screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.SelfDestruct);
        Assert.True(button.Enabled);
        f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        var command = Assert.Single(f.Connection.Commands);
        Assert.Equal(CombatCommandTypes.SelfDestruct, command.CommandType);
        Assert.Equal("active-flight", command.TargetObjectId);
        Assert.Equal("launcher", command.ModuleId);
        Assert.Equal(SimulationSpeed.Speed0, f.Handle.Buffer.CurrentSpeed);
    }
    [Theory]
    [InlineData(TorpedoTerminationKind.SelfDestruct, true)]
    [InlineData(TorpedoTerminationKind.Impact, false)]
    public void Self_destruct_terminal_trail_lives_two_real_seconds(TorpedoTerminationKind kind, bool visible)
    {
        long now = 0;
        var store = new CombatEffectStore(() => now);
        store.Receive([new(1, "t", "p", "l", "target", "", 0, 0, 0, [new(0, new(0, 0, 0, 3, 0, 100), 1)], TerminationKind: kind)]);
        Assert.Equal(visible, !Assert.Single(store.Active).TerminalTrail.IsDefaultOrEmpty);
        now = Stopwatch.Frequency * 2;
        store.Receive(default);
        Assert.Empty(store.Active);
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public RecordingConnection Connection { get; } = new();
        public GameSessionHandle Handle { get; }
        public GameSessionScreen Screen { get; }
        public Fixture(float scale = 1)
        {
            Handle = new(Connection);
            Handle.Buffer.Update(new(1, 0, SimulationSpeed.Speed0,
                [new("player", 10000, 10000, .7, 0), new("target", 10000, 10060, 0, 0), new("hover", 10100, 10060, 0, 0)],
                "player", InstalledModules: [Launcher]));
            Screen = new(Handle.Buffer, new LinearMotionPredictor(), Handle, uiScale: scale);
        }
        public void Update(Func<AuthoritativeSnapshot, AuthoritativeSnapshot> change)
        {
            var old = Handle.Buffer.Latest!.Snapshot;
            Handle.Buffer.Update(change(old) with { SnapshotSequence = old.SnapshotSequence + 1 });
        }
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        public List<PlayerCommand> Commands { get; } = [];
        public Func<PlayerCommand, ValueTask>? OnSend { get; set; }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return OnSend?.Invoke(command) ?? ValueTask.CompletedTask;
        }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
