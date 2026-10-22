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
        [CombatCommandTypes.Launch, CombatCommandTypes.Detonate], "On", "Ready", 60,
        Commands: [new(CombatCommandTypes.Launch, "Launch", "point"), new(CombatCommandTypes.Detonate, "Detonate", "object")],
        LauncherCombat: new(6, 0, 200, new("crew", "Operator", WeaponSkillType.TorpedoAttack, 0, 30, 30), RangeKm: 70,
            Profiles: [new("slow", 6, 9), new("medium", 10, 5), new("fast", 16, 2.5)], ScatterPercent: .02));

    [Theory]
    [InlineData(1, "slow")]
    [InlineData(2, "medium")]
    [InlineData(3, "fast")]
    public async Task Paused_point_profile_selection_is_local_and_has_no_mode_reload_rng_or_optimistic_launch(int key, string expected)
    {
        await using var f = await PointCombatUiFixture.Create();
        var before = f.Engine.CaptureLaunchScatterState();
        f.Aim(profile: key);
        Assert.Equal(expected, f.Screen.PointAim!.ProfileId); Assert.Equal(ProjectileKind.Torpedo, f.Screen.PointAim.Kind);
        var launcher = f.Presented.InstalledModules.Single(m => m.ModuleId == PointCombatUiFixture.Torpedo).LauncherCombat!;
        Assert.Empty(launcher.ModeId); Assert.Null(launcher.ReadyAtMotionTimeMs);
        Assert.Equal(before, f.Engine.CaptureLaunchScatterState()); Assert.Null(f.Screen.LastPointCommandId);
        Assert.DoesNotContain(f.Presented.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(0, f.Presented.MotionTimeMs); Assert.Equal(SimulationSpeed.Speed0, f.Presented.CurrentSpeed);
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
        Assert.Equal(5, panel.CommandPanelRows.Count);
        var weapon = panel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher");
        if (!weapon.Opened) { f.Screen.OnMouseDown(weapon.CaptionRect.MidX * scale, weapon.CaptionRect.MidY * scale); Render(f.Screen, height); }
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

    internal static void SelectTarget(Fixture f)
    {
        Render(f.Screen);
        f.Screen.OnMouseDown(640, 420);
        Render(f.Screen);
        Assert.Equal("target", f.Screen.SelectedObjectId);
    }

    internal static CommandButtonGeometry Button(GameSessionScreen screen) =>
        Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.Launch);

    internal static void Render(GameSessionScreen screen, int height = 720)
    {
        using var bitmap = new SKBitmap(1280, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, height);
    }

    internal sealed class Fixture : IAsyncDisposable
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

    internal sealed class RecordingConnection : IGameSessionConnection
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
