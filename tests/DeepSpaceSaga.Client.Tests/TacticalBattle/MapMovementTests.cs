using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens;
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

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    public async Task Endpoint_triangle_points_along_final_course(double course)
    {
        await using var scene = await Ep0019Scenes.Create("1-move");
        var frame = scene.Frame;
        var movement = frame.Movement!;
        var order = movement.SelectedOrder!;
        var view = new TacticalBattleView(1280, 720, 0, 0, 1);
        using var raster = BattleMapRenderTests.Raster(frame with
        {
            View = view,
            Battle = frame.Battle with { Units = [], Paths = [], Anomalies = [] },
            Movement = movement with { Orders = [order with { Path = [new(0, 0, course)] }], Reachable = [] }
        });
        var center = view.Project(0, 0);
        double radians = course * Math.PI / 180;
        var axis = new SKPoint((float)Math.Cos(radians), (float)Math.Sin(radians));
        var side = new SKPoint(-axis.Y, axis.X);
        var nose = center + BattleDesign.Scale(axis, 3);
        var flank = BattleDesign.Scale(side, 2);
        Assert.True(Pixel(nose).Blue > 160);
        Assert.True(Pixel(center - BattleDesign.Scale(axis, 2) + flank).Blue > 160);
        Assert.True(Pixel(nose + flank).Blue < 100);

        SKColor Pixel(SKPoint point) => raster.GetPixel((int)Math.Floor(point.X), (int)Math.Floor(point.Y));
    }

    [Fact]
    public async Task Reachable_area_follows_selected_drone_during_move_planning()
    {
        await using var scene = await Ep0019Scenes.Create("1-move");
        var first = scene.Frame;
        using var idle = BattleMapRenderTests.Raster(first);
        using var withoutArea = BattleMapRenderTests.Raster(first with { Movement = first.Movement! with { Reachable = [] } });
        Assert.Equal(idle.Pixels, withoutArea.Pixels);
        var firstArea = FindFill(first with { AimingMovement = true });
        var next = first.Movement!.Orders.First(row => row.Unit.Id != first.Movement.Selected);
        var nextPoint = first.View.Project(next.Unit.Pose);
        scene.Screen.OnMouseDown(nextPoint.X * 1.5f, nextPoint.Y * 1.5f, MouseButton.Left);
        scene.Screen.OnMouseUp(nextPoint.X * 1.5f, nextPoint.Y * 1.5f, MouseButton.Left);
        var second = scene.Frame;
        Assert.Equal(next.Unit.Id, second.Movement!.Selected);
        var endpoint = second.View.Project(second.Movement.SelectedOrder!.Path[^1]);
        scene.Screen.OnMouseDown(endpoint.X * 1.5f, endpoint.Y * 1.5f, MouseButton.Left);
        var aiming = scene.Frame;
        Assert.True(aiming.AimingMovement);
        Assert.NotEqual(firstArea, FindFill(aiming));
        scene.Screen.OnMouseUp(endpoint.X * 1.5f, endpoint.Y * 1.5f, MouseButton.Left);
        Assert.False(scene.Frame.AimingMovement);
        scene.Screen.OnMouseDown(endpoint.X * 1.5f, endpoint.Y * 1.5f, MouseButton.Left);
        scene.Screen.OnInputReset();
        Assert.False(scene.Frame.AimingMovement);

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
    public async Task Clicking_selected_drone_hides_movement_and_fire_zones_without_resetting_paths()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        using var initialRaster = scene.Render();
        scene.Screen.OnKeyDown(Key.Right);
        var frame = scene.Frame;
        var selected = frame.Movement!.SelectedOrder!;
        var position = frame.View.Project(selected.Unit.Pose);
        scene.Screen.OnMouseDown(position.X * 1.5f, position.Y * 1.5f, MouseButton.Left);
        var cleared = scene.Frame;
        Assert.Null(cleared.SelectedDroneId);
        Assert.Null(cleared.Movement!.SelectedOrder);
        Assert.Empty(cleared.Movement.Reachable);
        Assert.Equal(frame.Movement.Orders.ToArray(), cleared.Movement.Orders.ToArray());
        AssertPhaseButton(scene, ScreenEvent.MoveTacticalBattle);
        scene.Screen.OnMouseDown(position.X * 1.5f, position.Y * 1.5f, MouseButton.Left);
        Assert.Equal(selected.Unit.Id, scene.Frame.SelectedDroneId);
        Assert.Equal(selected.Path, scene.Frame.Movement!.SelectedOrder!.Path);
    }

    [Fact]
    public async Task Clicking_selected_shooter_hides_fire_sector_without_clearing_order()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        using var initialRaster = scene.Render();
        var frame = scene.Frame;
        var shooter = frame.FirePlan!.Selected!;
        var position = frame.View.Project(shooter.Drone.Pose);
        var orders = scene.Screen.BuildFireRequest().Targets;

        scene.Screen.OnMouseDown(position.X * 1.5f, position.Y * 1.5f, MouseButton.Left);
        Assert.Null(scene.Frame.FirePlan!.Selected);
        Assert.Equal(orders.ToArray(), scene.Screen.BuildFireRequest().Targets.ToArray());
        AssertPhaseButton(scene, ScreenEvent.FireTacticalBattle);
        scene.Screen.OnMouseDown(position.X * 1.5f, position.Y * 1.5f, MouseButton.Left);
        Assert.Equal(shooter, scene.Frame.FirePlan!.Selected);
    }

    private static void AssertPhaseButton(Ep0019Scenes scene, ScreenEvent expected)
    {
        using var raster = scene.Render();
        var button = scene.Screen.Layout.Rects["button:primary"];
        Assert.Equal(expected, scene.Screen.OnMouseDown(button.MidX * 1.5f, button.MidY * 1.5f, MouseButton.Left));
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
            var build = order.Unit.Build!;
            float range = (float)(build.Range * frame.View.PixelsPerUnit);
            var origin = frame.View.Project(end);
            var withoutWeapon = order with { Unit = order.Unit with { Build = null } };
            using var visible = BattleMapRenderTests.Raster(frame);
            using var hidden = BattleMapRenderTests.Raster(frame with
            {
                Movement = movement with { Orders = movement.Orders.Replace(order, withoutWeapon) }
            });
            Compare(PointAt(0, 10), filled: true);
            Compare(PointAt(0, range * .6f), filled: true);
            Compare(PointAt(180, 10), filled: false);
            Compare(PointAt(0, range + 4), filled: false);
            foreach (int side in new[] { -1, 1 })
            {
                Compare(PointAt(side * build.ArcDegrees * .75, range * .6f), filled: true);
                Compare(PointAt(side * build.ArcDegrees * 1.25, range * .6f), filled: false);
            }

            SKPoint PointAt(double courseOffset, float distance)
            {
                float radians = (float)((end.Course + courseOffset) * Math.PI / 180);
                return origin + new SKPoint(MathF.Cos(radians) * distance, MathF.Sin(radians) * distance);
            }

            void Compare(SKPoint p, bool filled)
            {
                var a = visible.GetPixel((int)p.X, (int)p.Y);
                var b = hidden.GetPixel((int)p.X, (int)p.Y);
                if (filled)
                {
                    Assert.True(a.Green > b.Green + 12, $"Expected green fill at {p}, got {a} over {b}.");
                    Assert.True(a.Green - b.Green > a.Red - b.Red + 8);
                }
                else Assert.Equal(b, a);
            }
        }
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
