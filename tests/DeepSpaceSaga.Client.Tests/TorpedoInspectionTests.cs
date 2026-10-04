using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TorpedoInspectionTests
{
    private static ObjectMotionSnapshot Player => new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip);
    private static ObjectMotionSnapshot Target => new("target", 300, 100, 0, 0, DisplayName: "Pirate", RenderObjectType: SpaceObjectType.NpcShip);
    private static ObjectMotionSnapshot Torpedo => new("torpedo", 100, 100, 3, 0, RenderObjectType: SpaceObjectType.Missile,
        Torpedo: new("player", "launcher", "target", 0, 3, 90, 150, 90,
            new(1000, 1, TorpedoRoutePhase.Straight, true, [new(100, 100, 0, 3, 0, 10000)]),
            PredictedImpactMotionTimeMs: 11000));

    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void Rendered_hit_chance_value_has_clear_gap_after_label(float scale)
    {
        var panel = new ObjectInfoPanel();
        var data = new ObjectInfoPanelData("torpedo", "Torpedo", 3, 0, SpaceObjectType.Missile,
            Torpedo: new("Pirate", 9, 10, 100));
        using var bitmap = new SKBitmap((int)(800 * scale), (int)(600 * scale));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        canvas.Scale(scale);
        panel.Render(canvas, 800, 0, null, data);

        var body = panel.RowBodyRects[1];
        // The text starts after the 200px image and two 6px paddings.
        float textX = body.Left + 212;
        int row = ObjectInfoPanel.BuildLines(data).FindIndex(line => line.Label == "Hit chance");
        float baseline = body.Top + 6 + 16 - 3 + row * 16;
        using var labelPaint = new SKPaint { TextSize = 12, Typeface = XenonStyle.TypefaceRegular };
        float labelEnd = (textX + labelPaint.MeasureText("Hit chance")) * scale;

        int firstValuePixel = int.MaxValue;
        for (int y = (int)((baseline - 14) * scale); y <= (int)((baseline + 2) * scale); y++)
        {
            for (int x = (int)(textX * scale); x < (int)((body.Right - 6) * scale); x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                // Values are brighter than the grey labels (140); the background is blue.
                if (pixel.Red > 150 && Math.Abs(pixel.Red - pixel.Green) <= 2 && Math.Abs(pixel.Green - pixel.Blue) <= 2)
                    firstValuePixel = Math.Min(firstValuePixel, x);
            }
        }

        Assert.NotEqual(int.MaxValue, firstValuePixel);
        Assert.True(firstValuePixel >= labelEnd + 4 * scale,
            $"Hit chance overlaps its value at scale {scale}: label ends at {labelEnd}, value starts at {firstValuePixel}.");
    }

    [Fact]
    public void Selected_torpedo_shows_target_speed_distance_eta_and_100_percent()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        // Calendar time deliberately differs from physical time by many orders of magnitude.
        var snapshot = new AuthoritativeSnapshot(1, 9000000000, SimulationSpeed.Speed1,
            [Player, Target, Torpedo], "player", SimulationTimeMs: 1000);
        buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        Render(screen);
        screen.OnMouseDown(1060, 640);
        Assert.Equal("torpedo", screen.SelectedObjectId);
        var lines = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
        Assert.Contains(("Target", "Pirate"), lines);
        Assert.Contains(("Speed", "3 km/s"), lines);
        Assert.Contains(("Travelled", "9 km"), lines);
        Assert.Contains(("ETA", "10 s"), lines);
        Assert.Contains(("Hit chance", "100%"), lines);
        now = Stopwatch.Frequency;
        Render(screen);
        var info = screen.SelectedOrActiveObjectInfo!.Value.Torpedo!;
        Assert.Equal(12, info.TravelledKm, 8);
        Assert.Equal(9, info.EtaSeconds);
        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        now += Stopwatch.Frequency * 50;
        Render(screen);
        Assert.Equal(info, screen.SelectedOrActiveObjectInfo!.Value.Torpedo);
        // Hover and selection may change; original flight target remains authoritative.
        screen.OnMouseDown(1260, 640);
        screen.OnMouseMove(1060, 610);
        Assert.Equal("target", screen.SelectedObjectId);
        Assert.Equal("torpedo", screen.ActiveObjectId);
        Assert.Equal("Pirate", screen.SelectedOrActiveObjectInfo!.Value.Torpedo!.Target);
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Wreck_is_selectable_and_can_be_targeted()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [Player, new("wreck", 100, 100, 0, 0, RenderObjectType: SpaceObjectType.Wreck)], "player",
            InstalledModules: [new("launcher", "module.torpedo.launcher.basic", "Launcher", 1, [CombatCommandTypes.Fire],
                "On", "Ready", 60, Commands: [new(CombatCommandTypes.Fire, "Fire", "object")], LauncherCombat: new(null, 3, 90, 150, new("crew", "Operator", WeaponSkillType.TorpedoAttack, 50, 30, 30)))]));
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        Render(screen);
        // 30px screen hit radius, unrelated to the 2.5px drawn radius or collision world units.
        screen.OnMouseDown(1089, 640);
        Assert.Equal("wreck", screen.SelectedObjectId);
        Render(screen);
        var button = Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.Fire);
        Assert.True(button.Enabled);
        screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        var command = Assert.Single(connection.Commands);
        Assert.Equal("wreck", command.TargetObjectId);
        Assert.Equal(CombatCommandTypes.Fire, command.CommandType);
        Assert.Equal(5, TacticalMapMarkerPolicy.GetMarkerSizePx(SpaceObjectType.Wreck));
        Assert.Equal(5, TacticalMapMarkerPolicy.GetMarkerSizePx(SpaceObjectType.Missile));
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unknown_target_name_is_not_leaked(bool missing)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            missing ? [Player, Torpedo] : [Player, Target with { RenderObjectType = SpaceObjectType.UnknownSpaceObject, DisplayName = "SECRET" }, Torpedo], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        Render(screen);
        screen.OnMouseDown(1060, 640);
        var lines = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
        Assert.Contains(("Target", "target"), lines);
        Assert.DoesNotContain(lines, l => l.Value.Contains("SECRET"));
        screen.OnDeactivated();
    }

    [Fact]
    public void Unreachable_target_has_no_eta_and_does_not_invent_impact()
    {
        var buffer = new SnapshotBuffer(() => 0);
        var torpedo = Torpedo with { Torpedo = Torpedo.Torpedo! with { PredictedImpactMotionTimeMs = null, Route = Torpedo.Torpedo.Route with { HasIntercept = false } } };
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [Player, Target, torpedo], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        Render(screen);
        screen.OnMouseDown(1060, 640);
        Assert.Contains(("ETA", "—"), ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo));
        screen.OnDeactivated();
    }

    [Fact]
    public void Destroyed_projectile_does_not_leave_stale_info()
    {
        var buffer = new SnapshotBuffer(() => 0);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [Player, Target, Torpedo], "player");
        buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        Render(screen);
        screen.OnMouseDown(1060, 640);
        screen.OnMouseMove(1060, 640);
        Assert.NotNull(screen.SelectedOrActiveObjectInfo!.Value.Torpedo);
        buffer.Update(snapshot with { SnapshotSequence = 2, Objects = [Player, Target] });
        Render(screen);
        Assert.Null(screen.SelectedObjectId);
        Assert.Null(screen.ActiveObjectId);
        Assert.Null(screen.SelectedOrActiveObjectInfo);
        screen.OnDeactivated();
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
    }

    private sealed class Connection : IGameSessionConnection
    {
        internal List<PlayerCommand> Commands { get; } = [];
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return ValueTask.CompletedTask;
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
