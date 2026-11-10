using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class MapMovementTests
{
    [Fact]
    public async Task Forecast_path_and_endpoint_drag_use_order_model()
    {
        await using var s = await Ep0019Scenes.Create("1-move"); using var b = s.Render();
        var f = s.Screen.CaptureFrame(1920, 1080)!; var row = f.Movement!.SelectedOrder!;
        Assert.Equal(4, s.Screen.Layout.Rects.Count(p => p.Key.StartsWith("endpoint:", StringComparison.Ordinal)));
        Assert.DoesNotContain("callout:endpoint", s.Screen.Layout.Rects.Keys);
        foreach (var order in f.Movement.Orders)
        {
            var handle = s.Screen.Layout.Rects["endpoint:" + order.Unit.Id];
            Assert.Equal(f.View.Project(order.Path[^1]), new SKPoint(handle.MidX, handle.MidY));
            Assert.InRange(handle.Width, order.Unit.Id == row.Unit.Id ? 9.99f : 7.99f, order.Unit.Id == row.Unit.Id ? 10.01f : 8.01f);
        }
        var middle = f.View.Project(row.Path[row.Path.Length / 2]);
        var sample = SKRect.Create(middle.X - 1, middle.Y - 1, 2, 2);
        Assert.True(RasterProbe.Fraction(b, sample, new SKColor(190, 190, 190)) >= .4);
        Assert.Equal(0, RasterProbe.Fraction(b, sample, BattleDesign.Cyan));
        var end = f.View.Project(row.Path[^1]);
        s.Screen.OnMouseDown(end.X * 1.5f, end.Y * 1.5f, MouseButton.Left);
        s.Screen.OnMouseMove((end.X - 30) * 1.5f, (end.Y + 20) * 1.5f);
        s.Screen.OnMouseUp((end.X - 30) * 1.5f, (end.Y + 20) * 1.5f, MouseButton.Left);
        Assert.Contains(s.Screen.BuildMoveRequest().Orders, o => o.DroneId == row.Unit.Id && o != row.Suggestion);
    }

    [Fact]
    public async Task Reachable_area_follows_selected_drone_during_move_planning()
    {
        await using var scene = await Ep0019Scenes.Create("1-move");
        var first = scene.Frame;
        var firstArea = FindFill(first);
        var next = first.Movement!.Orders.First(row => row.Unit.Id != first.Movement.Selected);
        var nextPoint = first.View.Project(next.Unit.Pose);
        scene.Screen.OnMouseDown(nextPoint.X * 1.5f, nextPoint.Y * 1.5f, MouseButton.Left);
        scene.Screen.OnMouseUp(nextPoint.X * 1.5f, nextPoint.Y * 1.5f, MouseButton.Left);
        var second = scene.Frame;
        Assert.Equal(next.Unit.Id, second.Movement!.Selected);
        Assert.NotEqual(firstArea, FindFill(second));

        using var noPlan = BattleMapRenderTests.Raster(second with { Movement = null });
        using var noSelection = BattleMapRenderTests.Raster(second with { Movement = second.Movement with { Orders = [], Selected = null } });
        Assert.Equal(noPlan.Pixels, noSelection.Pixels);

        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
        {
            foreach (var (width, height, scale) in new[] { (1280, 720, 100), (1920, 1080, 150) })
            {
                using var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);
                scene.Screen.Render(canvas, width, height);
                using var image = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(Path.Combine(root, $"movement-{scale}.png"), image.ToArray());
            }
        }
    }

    [Fact]
    public async Task Planned_fire_sector_follows_selected_drone_endpoint_and_course()
    {
        await using var scene = await Ep0019Scenes.Create("1-move");
        var initial = scene.Frame;
        AssertSector(initial);

        scene.Screen.OnKeyDown(Key.Right);
        var edited = scene.Frame;
        var oldEnd = initial.Movement!.SelectedOrder!.Path[^1];
        var newEnd = edited.Movement!.SelectedOrder!.Path[^1];
        Assert.NotEqual((oldEnd.X, oldEnd.Y), (newEnd.X, newEnd.Y));
        Assert.NotEqual(oldEnd.Course, newEnd.Course);
        AssertSector(edited);

        scene.Screen.OnKeyDown(Key.Tab);
        var next = scene.Frame;
        Assert.NotEqual(edited.Movement.Selected, next.Movement!.Selected);
        AssertSector(next);

        static void AssertSector(TacticalBattleFrame frame)
        {
            var movement = frame.Movement!;
            var order = movement.SelectedOrder!;
            var end = order.Path[^1];
            float range = (float)(order.Unit.Build!.Range * frame.View.PixelsPerUnit);
            var origin = frame.View.Project(end);
            float radians = (float)(end.Course * Math.PI / 180);
            var forward = new SKPoint(MathF.Cos(radians), MathF.Sin(radians));
            var withoutWeapon = order with { Unit = order.Unit with { Build = null } };
            using var visible = BattleMapRenderTests.Raster(frame);
            using var hidden = BattleMapRenderTests.Raster(frame with
            {
                Movement = movement with { Orders = movement.Orders.Replace(order, withoutWeapon) }
            });
            Compare(origin + Scale(forward, 10), filled: true);
            Compare(origin + Scale(forward, range * .6f), filled: true);
            Compare(origin - Scale(forward, 10), filled: false);
            Compare(origin + Scale(forward, range + 4), filled: false);

            void Compare(SKPoint p, bool filled)
            {
                var a = visible.GetPixel((int)p.X, (int)p.Y);
                var b = hidden.GetPixel((int)p.X, (int)p.Y);
                if (filled) Assert.True(a.Red > b.Red + 12);
                else Assert.Equal(b, a);
            }
        }

        static SKPoint Scale(SKPoint p, float value) => new(p.X * value, p.Y * value);
    }

    [Fact]
    public async Task Planned_fire_sector_is_hidden_without_usable_selected_order()
    {
        await using var scene = await Ep0019Scenes.Create("1-move");
        var frame = scene.Frame;
        var movement = frame.Movement!;
        var order = movement.SelectedOrder!;
        using var withoutMovement = BattleMapRenderTests.Raster(frame with { Movement = null });
        using var withoutOrder = BattleMapRenderTests.Raster(frame with
        {
            Movement = movement with { Orders = [] }
        });
        using var withoutPath = BattleMapRenderTests.Raster(frame with
        {
            Movement = movement with { Orders = [order with { Path = [] }], Reachable = [] }
        });
        Assert.Equal(withoutMovement.Pixels, withoutOrder.Pixels);
        Assert.Equal(withoutMovement.Pixels, withoutPath.Pixels);
    }

    private static (int X, int Y) FindFill(TacticalBattleFrame frame)
    {
        var movement = frame.Movement!;
        using var polygon = new SKPath();
        polygon.AddPoly(movement.Reachable.Select(frame.View.Project).ToArray(), true);
        using var full = BattleMapRenderTests.Raster(frame);
        using var empty = BattleMapRenderTests.Raster(frame with { Movement = movement with { Reachable = [] } });
        var bounds = polygon.Bounds;
        Assert.Equal(empty.GetPixel(100, 600), full.GetPixel(100, 600));
        for (int y = Math.Max(0, (int)bounds.Top + 3); y < Math.Min(full.Height, bounds.Bottom - 3); y++)
        {
            for (int x = Math.Max(0, (int)bounds.Left + 3); x < Math.Min(full.Width, bounds.Right - 3); x++)
            {
                if (polygon.Contains(x, y) && full.GetPixel(x, y).Green > empty.GetPixel(x, y).Green + 8)
                    return (x, y);
            }
        }
        throw new InvalidOperationException("No filled pixel within the projected reachable area.");
    }
}
