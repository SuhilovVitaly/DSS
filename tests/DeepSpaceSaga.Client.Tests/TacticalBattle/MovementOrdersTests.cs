using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class MovementOrdersTests
{
    [Theory]
    [InlineData(0.8f)]
    [InlineData(1.5f)]
    public async Task Movement_panel_skin_changes_pixels_and_preserves_issued_order(float scale)
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(uiScale: scale);
        var classic = await Start(fixture);
        var skin = TacticalBattleSkin.Parse("""
            {"schemaVersion":1,"id":"move","displayName":"Move","roles":{
              "ui.panel":{"fill":"#342B5A"},
              "ui.slider.speed":{"fill":"#FF45AA"},
              "ui.slider.turn":{"fill":"#29DDBA"}
            }}
            """);
        var themed = new TacticalBattleScreen(classic.Flow, scale, skin);
        using var classicPixels = Draw(classic);
        using var themedPixels = Draw(themed);
        Assert.Equal(classic.Layout.Rects["slider:speed"], themed.Layout.Rects["slider:speed"]);
        Assert.Contains(new SKColor(255, 69, 170), themedPixels.Pixels);
        Assert.Contains(new SKColor(41, 221, 186), themedPixels.Pixels);
        Assert.DoesNotContain(new SKColor(255, 69, 170), classicPixels.Pixels);

        var slider = classic.Layout.Rects["slider:speed"]; slider = new(slider.Left * 1000 / 720, slider.Top * 1000 / 720, slider.Right * 1000 / 720, slider.Bottom * 1000 / 720);
        float x = slider.Left + slider.Width * 0.85f;
        classic.OnMouseDown(x, slider.MidY, MouseButton.Left);
        themed.OnMouseDown(x, slider.MidY, MouseButton.Left);
        classic.OnMouseUp(x, slider.MidY, MouseButton.Left);
        themed.OnMouseUp(x, slider.MidY, MouseButton.Left);
        Assert.Equal(Assert.Single(classic.BuildMoveRequest().Orders), Assert.Single(themed.BuildMoveRequest().Orders));

        static SKBitmap Draw(TacticalBattleScreen screen)
        {
            var bitmap = new SKBitmap(1600, 1000);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, bitmap.Width, bitmap.Height);
            return bitmap;
        }
    }

    [Fact]
    public async Task KeyboardEditsOnlySelectedDroneAndEngineUsesSuggestionsForUntouchedDrones()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var state = screen.Flow.State!;
        Assert.Empty(screen.BuildMoveRequest().Orders);
        var initial = screen.Movement;
        Assert.Equal(state.Suggestions, initial.Orders.Select(o => o.Order));
        Assert.All(initial.Orders, order => Assert.False(order.Touched));
        Assert.NotEmpty(initial.Reachable);

        screen.OnKeyDown(Key.Tab);
        Assert.Equal(initial.Orders[1].Unit.Id, screen.Movement.Selected);
        screen.OnKeyDown(Key.Tab, KeyModifiers.Shift, false);
        Assert.Equal(initial.Orders[0].Unit.Id, screen.Movement.Selected);
        Assert.Empty(screen.BuildMoveRequest().Orders);
        screen.OnKeyDown(Key.Up);
        screen.OnKeyDown(Key.Right, KeyModifiers.None, true);
        var edited = screen.Movement.SelectedOrder!;
        Assert.True(edited.Touched);
        var requested = Assert.Single(screen.BuildMoveRequest().Orders);
        Assert.Equal(edited.Order, requested);
        Assert.Equal(Math.Min(initial.Orders[0].Order.Speed + 2, edited.MaximumSpeed), requested.Speed, 10);
        Assert.Equal(Math.Clamp(initial.Orders[0].Order.Turn + 5, -edited.MaximumTurn, edited.MaximumTurn), requested.Turn, 10);

        await fixture.Route(screen.OnKeyDown(Key.Enter));
        var after = screen.Flow.State!;
        Assert.True(after.Revision > state.Revision);
        foreach (var row in initial.Orders)
        {
            var path = after.Paths.Single(p => p.DroneId == row.Unit.Id);
            Assert.Equal(row.Unit.Id == edited.Unit.Id ? edited.Path[^1] : row.Path[^1], path.Points[^1]);
        }
        Assert.Empty(screen.BuildMoveRequest().Orders);
    }

    [Fact]
    public async Task CursorMeshFindsAnInteriorExactPointAndKeepsStableFirstTie()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var state = screen.Flow.State!;
        var orders = new TacticalBattleOrders();
        orders.Observe(state);
        var first = orders.Capture().SelectedOrder!;
        var l = first.Unit.Limits!;
        var limits = new TacticalLimits(l.Mass, l.MassFactor, l.MinSpeed, l.MaxSpeed, l.Change, l.Turn);
        double speed = first.MinimumSpeed + (first.MaximumSpeed - first.MinimumSpeed) * (7 / 12.0);
        double cap = limits.TurnLimit(speed, state.Rules.TurnAtMaxSpeedFraction);
        double turn = -cap + 2 * cap * (39 / 60.0);
        var target = TacticalGeometry.Integrate(new(first.Unit.Pose.X, first.Unit.Pose.Y, first.Unit.Pose.Course), speed, turn, state.Rules.Substeps);
        Assert.True(orders.AimAt(target.X, target.Y));
        var actual = Assert.Single(orders.EditedOrders());
        Assert.Equal(speed, actual.Speed, 10);
        Assert.Equal(turn, actual.Turn, 10);
        Assert.Equal(new TacticalPoseSnapshot(target.X, target.Y, target.Course), orders.Capture().SelectedOrder!.Path[^1]);

        // Distinct commands with identical endpoints: the first speed/turn remains selected.
        var unit = first.Unit with { Speed = 0, Limits = l with { MinSpeed = 0, MaxSpeed = 0, Change = 0, Turn = 45 } };
        var tie = new TacticalBattleOrders();
        tie.Observe(state with { Units = [unit], Suggestions = [new(unit.Id, 0, 0)] });
        Assert.True(tie.AimAt(unit.Pose.X, unit.Pose.Y));
        Assert.Equal(new TacticalDroneOrder(unit.Id, 0, -45), Assert.Single(tie.EditedOrders()));
    }





    private static void Save(SKBitmap bitmap)
    {
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root) return;
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(Path.Combine(root, "battle-movement.png"));
        png.SaveTo(file);
    }

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }
}
