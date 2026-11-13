using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopMoveTests
{
    [Fact]
    public async Task Selected_arrow_is_visibly_thinner_at_wide_zoom()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover", withoutAnomalies: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        var frame = scene.Frame;
        var row = frame.Movement!.SelectedOrder!;
        var path = row with { Path = [new(-50, 0, 0), new(0, 0, 0), new(50, 0, 0)] };
        frame = frame with
        {
            Battle = frame.Battle with { Units = [] },
            Movement = new([path], row.Unit.Id, [])
        };

        int wide = MeasureStroke(1);
        int close = MeasureStroke(6);
        Assert.InRange(wide, 4, 7);
        Assert.InRange(close, 12, 16);
        Assert.True(close >= 2 * wide);

        int MeasureStroke(double pixelsPerUnit)
        {
            var view = frame.View with { CenterX = 0, CenterY = 0, PixelsPerUnit = pixelsPerUnit };
            using var bitmap = new SKBitmap(view.PixelWidth, view.PixelHeight);
            using var canvas = new SKCanvas(bitmap);
            TacticalBattleMapRenderer.Draw(canvas, frame with { View = view });
            var sample = view.Project(-26, 0);
            var fill = SKColor.Parse("#E8D48C");
            int pixels = 0;
            // Sample both sides of the dashed centerline, clear of the drone and speed badge.
            for (int y = (int)sample.Y - 20; y <= (int)sample.Y + 20; y++)
                if (bitmap.GetPixel((int)sample.X, y) == fill) pixels++;
            return pixels;
        }
    }

    [Theory]
    [InlineData("1-move-no-hover")]
    [InlineData("1-anomaly-move-dust")]
    public async Task Movement_shows_reachable_templates_and_ghost_handles_without_weapon_sector(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap); canvas.Scale(1.5f);
        var frame = scene.Frame;
        var layout = new BattleLayout(); BattleDesign.Recording = layout;
        try { TacticalBattleMapRenderer.Draw(canvas, frame); }
        finally { BattleDesign.Recording = null; }
        Assert.False(frame.AimingMovement);
        Assert.True(layout.Rects.ContainsKey("reachable"));
        foreach (var order in frame.Movement!.Orders)
        {
            var endpoint = layout.Rects["endpoint:" + order.Unit.Id];
            Assert.Equal(12, endpoint.Width, .001f); Assert.Equal(12, endpoint.Height, .001f);
            var point = frame.View.Project(order.Path[^1]);
            Assert.Equal(point.X, endpoint.MidX, .001f); Assert.Equal(point.Y, endpoint.MidY, .001f);
            Assert.True(layout.Rects.ContainsKey("label:" + order.Unit.Id));
        }
        Assert.True(layout.Rects.ContainsKey("template-tip"));
        Assert.Contains(SKColor.Parse("#E8D48C"), bitmap.Pixels);
        Assert.DoesNotContain(SKColor.Parse("#D6CBA6"), bitmap.Pixels);
        Assert.Contains(layout.Texts, t => t.Text.StartsWith(PilotCardModel.Name(frame.Movement.SelectedOrder!.Unit) + ":", StringComparison.Ordinal) && t.Text.EndsWith("°", StringComparison.Ordinal));
        var before = bitmap.Bytes;
        var selected = frame.Movement.SelectedOrder!;
        frame = frame with { Movement = frame.Movement with { Orders = frame.Movement.Orders.Replace(selected, selected with { Unit = selected.Unit with { Build = null } }) } };
        TacticalBattleMapRenderer.Draw(canvas, frame);
        Assert.Equal(before, bitmap.Bytes);
    }

    [Fact]
    public async Task Dragging_ghost_edits_the_existing_movement_command()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover", withoutAnomalies: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        try
        {
            screen.Render(canvas, 1280, 720);
            var order = screen.Movement.SelectedOrder!;
            var endpoint = screen.Layout.Rects["endpoint:" + order.Unit.Id];
            screen.OnMouseDown(endpoint.MidX, endpoint.MidY, MouseButton.Left);
            screen.OnMouseMove(endpoint.MidX - 35, endpoint.MidY + 20);
            screen.OnMouseUp(endpoint.MidX - 35, endpoint.MidY + 20, MouseButton.Left);
            var command = Assert.Single(screen.BuildMoveRequest().Orders);
            Assert.Equal(order.Unit.Id, command.DroneId);
            Assert.NotEqual(order.Order, command);
            screen.Render(canvas, 1280, 720);
            Assert.NotEqual(endpoint, screen.Layout.Rects["endpoint:" + order.Unit.Id]);
        }
        finally { screen.OnDeactivated(); }
    }
}
