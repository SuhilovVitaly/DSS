using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TorpedoCommandPanelTests
{
    private static readonly InstalledModuleSnapshot Launcher = new("actual-launcher", "module.torpedo.launcher.basic", "Launcher", 1,
        [CombatCommandTypes.Fire], "On", "Ready", 60, Commands: [new(CombatCommandTypes.Fire, "Fire", "object")],
        LauncherCombat: new(null, 3, 90, 150, new("crew", "Operator", WeaponSkillType.TorpedoAttack, 50, 30, 30)));

    [Fact]
    public async Task Busy_and_pending_launcher_disable_fire()
    {
        await using var f = new Fixture();
        SelectTarget(f);
        var button = Button(f.Screen);
        // Also attempt a reentrant click during transport submission, before it returns.
        var transport = new TaskCompletionSource();
        f.Connection.OnSend = _ =>
        {
            f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
            return new(transport.Task);
        };
        f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        Assert.Single(f.Connection.Commands);
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        transport.SetResult();
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled); // transport completion is not execution confirmation
        var command = Assert.Single(f.Connection.Commands);
        f.Update(s => s with { CommandResults = [Result(command with { CommandId = "unrelated" }, CommandResultStatus.Rejected)] });
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        f.Update(s => s with { CommandResults = [Result(command, CommandResultStatus.Deferred)] });
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        f.Update(s => s with
        {
            CommandResults = [Result(command, CommandResultStatus.Executed)],
            InstalledModules = [Launcher with { LauncherCombat = Launcher.LauncherCombat! with { ActiveTorpedoObjectId = "torpedo" } }]
        });
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        f.Update(s => s with { CommandResults = [], InstalledModules = [Launcher] });
        Render(f.Screen);
        Assert.True(Button(f.Screen).Enabled);
    }

    [Fact]
    public async Task Paused_fire_uses_selected_target_not_hover()
    {
        await using var f = new Fixture();
        SelectTarget(f);
        f.Screen.OnMouseMove(740, 420);
        Render(f.Screen);
        ClickFire(f);
        var command = Assert.Single(f.Connection.Commands);
        Assert.Equal("target", command.TargetObjectId);
        Assert.Equal(Launcher.ModuleId, command.ModuleId);
        Assert.Equal(SimulationSpeed.Speed0, f.Handle.Buffer.CurrentSpeed);
        f.Screen.OnMouseDown(740, 420);
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        Assert.Equal("target", command.TargetObjectId);
    }

    [Theory]
    [InlineData(CommandResultStatus.Rejected)]
    [InlineData(CommandResultStatus.Failed)]
    [InlineData(CommandResultStatus.Cancelled)]
    [InlineData(CommandResultStatus.Executed)]
    public async Task Rejected_submission_returns_panel_to_authoritative_state(CommandResultStatus status)
    {
        await using var f = new Fixture();
        SelectTarget(f);
        ClickFire(f);
        var command = Assert.Single(f.Connection.Commands);
        f.Update(s => s with { CommandResults = [Result(command, status)] });
        // Coalesce away the snapshot carrying the receipt before rendering again.
        f.Update(s => s with { CommandResults = [] });
        Render(f.Screen);
        Assert.True(Button(f.Screen).Enabled);
        ClickFire(f);
        Assert.Equal(2, f.Connection.Commands.Count);
        Assert.NotEqual(command.CommandId, f.Connection.Commands[1].CommandId);
        Assert.True(f.Connection.Commands[1].ClientSequence > command.ClientSequence);
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("async")]
    [InlineData("cancel")]
    public async Task Transport_failure_releases_pending_submission(string failure)
    {
        await using var f = new Fixture();
        SelectTarget(f);
        var transport = new TaskCompletionSource();
        f.Connection.OnSend = _ => failure == "sync" ? throw new IOException("offline") : new(transport.Task);
        ClickFire(f);
        if (failure == "async") transport.SetException(new IOException("offline"));
        if (failure == "cancel") transport.SetCanceled();
        Render(f.Screen);
        Assert.True(Button(f.Screen).Enabled);
        Assert.Equal("Send failed", f.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").StatusBarText);
    }

    [Fact]
    public async Task Information_overlay_consumes_reported_click_and_never_covers_launcher()
    {
        await using var f = new Fixture();
        SelectTarget(f);
        Assert.True(f.Screen.LastPanelRect.Contains(56, 604));
        var row = f.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher");
        Assert.False(row.BodyRect.IntersectsWith(f.Screen.LastPanelRect));
        // Force the old geometry to prove hit-test ordering independently of the layout fix.
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        f.Screen.CommandsPanel.Render(canvas, ImmutableArray<InstalledModuleSnapshot>.Empty);
        var oldFire = Button(f.Screen);
        Assert.True(oldFire.Enabled);
        // Put a visible overlay over the cached button using the existing diagnostic field.
        typeof(GameSessionScreen).GetField("_lastPanelRect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(f.Screen, oldFire.Rect);
        f.Screen.OnMouseDown(oldFire.Rect.MidX, oldFire.Rect.MidY);
        Assert.Empty(f.Connection.Commands);
        Render(f.Screen);
        f.Screen.OnMouseDown(56, 604);
        Assert.Empty(f.Connection.Commands);
    }

    private static CommandResult Result(PlayerCommand command, CommandResultStatus status) =>
        new(command.CommandId, command.ObjectId, command.ModuleId, command.CommandType, status, 0);

    private static void ClickFire(Fixture f)
    {
        var button = Button(f.Screen);
        f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
    }

    [Fact]
    public async Task Launcher_panel_requires_selected_nonself_target()
    {
        await using var f = new Fixture();
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        f.Screen.OnMouseDown(640, 360);
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        f.Screen.OnMouseDown(640, 420);
        Render(f.Screen);
        Assert.True(Button(f.Screen).Enabled);
        // Unknown selectable targets are legal; no knowledge-derived properties are needed.
        f.Update(s => s with { Objects = [s.Objects[0]] });
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("legacy")]
    [InlineData("busy")]
    [InlineData("off")]
    [InlineData("broken")]
    [InlineData("disabled")]
    public async Task Launcher_requires_actual_ready_capability(string state)
    {
        await using var f = new Fixture();
        SelectTarget(f);
        var module = state switch
        {
            "legacy" => Launcher with { LauncherCombat = null },
            "busy" => Launcher with { LauncherCombat = Launcher.LauncherCombat! with { ActiveTorpedoObjectId = "projectile" } },
            "off" => Launcher with { PowerState = "Off" },
            "broken" => Launcher with { StructurePoints = 0 },
            "disabled" => Launcher with { OperationalState = "Disabled" },
            _ => Launcher
        };
        f.Update(s => s with { InstalledModules = state == "missing" ? [] : [module] });
        // Check the sender against stale rendered enablement too.
        var oldButton = Button(f.Screen);
        f.Screen.OnMouseDown(oldButton.Rect.MidX, oldButton.Rect.MidY);
        Render(f.Screen);
        Assert.False(Button(f.Screen).Enabled);
        Assert.Empty(f.Connection.Commands);
    }

    [Theory]
    [InlineData(720, 1f)]
    [InlineData(720, 1.5f)]
    [InlineData(600, 1.5f)]
    public async Task Launcher_and_all_group_captions_fit_small_viewport(int height, float scale)
    {
        await using var f = new Fixture(scale);
        Render(f.Screen, height);
        var panel = f.Screen.CommandsPanel;
        Assert.Equal(6, panel.CommandPanelRows.Count);
        Assert.True(panel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher").Opened);
        Assert.All(panel.CommandPanelRows, r => Assert.True(r.CaptionRect.Bottom * scale <= height));
        Assert.True(panel.BodyRect.Bottom * scale <= height);
        Assert.False(panel.BodyRect.IntersectsWith(f.Screen.LastPanelRect));
        var row = panel.CommandPanelRows.First(r => !r.Opened);
        f.Screen.OnMouseDown(row.CaptionRect.MidX * scale, row.CaptionRect.MidY * scale);
        Render(f.Screen, height);
        Assert.True(panel.CommandPanelRows.Single(r => r.Name == row.Name).Opened);
        Assert.True(panel.BodyRect.Bottom * scale <= height);
    }

    private static void SelectTarget(Fixture f)
    {
        Render(f.Screen);
        f.Screen.OnMouseDown(640, 420);
        Render(f.Screen);
        Assert.True(Button(f.Screen).Enabled);
    }

    private static CommandButtonGeometry Button(GameSessionScreen screen) =>
        Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.Fire);

    private static void Render(GameSessionScreen screen, int height = 720)
    {
        using var bitmap = new SKBitmap(1280, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, height);
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
