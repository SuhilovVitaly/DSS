using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class DefenseCommandPanelTests
{
    private static readonly WeaponOperatorSnapshot Operator = new("crew", "Alex", WeaponSkillType.TorpedoAttack, 0, 30, 0);
    private static readonly InstalledModuleSnapshot Launcher = new("launcher", "module.torpedo.launcher.basic", "Launcher", 1,
        [CombatCommandTypes.Fire], "On", "Ready", 60, Commands: [new(CombatCommandTypes.Fire, "Fire", "object")],
        LauncherCombat: new(null, 3, 90, 150, Operator));
    private static readonly InstalledModuleSnapshot Defense = new("defense", "module.countermeasure.launcher.basic", "Defense", 2,
        [DefenseCommandTypes.Enable, DefenseCommandTypes.Disable], "On", "Ready", 60,
        Commands: [new(DefenseCommandTypes.Enable, "Включить", "none"), new(DefenseCommandTypes.Disable, "Выключить", "none")],
        Defense: new(true, Operator with { SkillType = WeaponSkillType.CountermeasureDefense }, DefenseState.Guiding, "pr"));
    [Fact]
    public async Task Auto_toggle_sends_only_disable_and_preserves_authoritative_flight()
    {
        await using var f = new Fixture();
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        f.Screen.Render(canvas, 1920, 1080);
        var row = f.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Countermeasure Launcher");
        if (!row.Opened)
        {
            f.Screen.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
            f.Screen.Render(canvas, 1920, 1080);
        }
        var button = Assert.Single(f.Screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == DefenseCommandTypes.Disable);
        Assert.True(button.Enabled);
        f.Screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        var command = Assert.Single(f.Connection.Commands);
        Assert.Equal(DefenseCommandTypes.Disable, command.CommandType);
        Assert.Null(command.TargetObjectId);
        Assert.Equal("pr", f.Handle.Buffer.Latest!.Snapshot.InstalledModules[1].Defense!.ActiveProjectileId);
    }
    [Fact]
    public async Task Countermeasure_selectable_but_fire_disabled()
    {
        await using var f = new Fixture();
        f.Update(s => s with { Objects = s.Objects.SetItem(1, s.Objects[1] with { RenderObjectType = SpaceObjectType.Countermeasure }) });
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        f.Screen.Render(canvas, 1280, 720);
        f.Screen.OnMouseDown(640, 420);
        f.Screen.Render(canvas, 1280, 720);
        Assert.Equal("target", f.Screen.SelectedObjectId);
        Assert.False(Assert.Single(f.Screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.Fire).Enabled);
    }
    [Fact]
    public void Operator_zero_skill_is_a_person_and_breakdown_is_frozen()
    {
        Assert.Contains("Alex", GameSessionScreen.WeaponOperatorText(Operator));
        Assert.Contains("Навык 0", GameSessionScreen.WeaponOperatorText(Operator));
        Assert.Equal("Нет оператора", GameSessionScreen.WeaponOperatorText(null));
        var flight = new CountermeasureSnapshot("ship", "launcher", "torpedo", CountermeasurePhase.Guiding,
            new(0, 1, TorpedoRoutePhase.Straight, true), 0, 320, new(Operator, 48));
        Assert.Contains(ObjectInfoPanel.CountermeasureLines(flight), l => l.Value == "32.0%");
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
                "player", InstalledModules: [Launcher, Defense]));
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
