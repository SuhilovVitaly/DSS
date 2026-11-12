using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhaseFireInputTests
{
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1080)]
    public async Task Carrier_edge_marker_assigns_only_admitted_target(int width, int height)
    {
        await using var s = await Ep0019Scenes.Create("2-fire");
        var frame = s.Screen.CaptureFrame(width, height)!;
        var carrier = frame.Battle.Units.First(u => u.Kind == TacticalUnitKind.Carrier && u.Side != frame.Battle.PlayerSide);
        var shooter = frame.FirePlan!.Selected!.Drone;
        var view = frame.View with { CenterX = carrier.Pose.X + 100000 };
        var marker = TacticalBattleMapRenderer.MarkerPoint(view, carrier);
        Assert.True(SKPoint.Distance(marker, view.Project(carrier.Pose)) > 100);
        Assert.Equal(236, marker.X);
        var orders = new TacticalBattleFireOrders();
        var state = frame.Battle with { Targets = [new(shooter.Id, [new(carrier.Id, 100, 0, false, 0.5, 0.5)], null)] };
        orders.Observe(state, shooter.Id); orders.Select(shooter.Id);
        Assert.True(orders.ChooseAt(view, marker, 1));
        Assert.Equal(carrier.Id, orders.BuildTargets().Single(t => t.DroneId == shooter.Id).TargetId);
        orders = new TacticalBattleFireOrders();
        orders.Observe(state with { Targets = [] }, shooter.Id); orders.Select(shooter.Id);
        Assert.False(orders.ChooseAt(view, marker, 1));
        Assert.Null(orders.BuildTargets().Single(t => t.DroneId == shooter.Id).TargetId);
    }

    [Fact]
    public async Task Fire_details_and_candidate_click_hold_fire()
    {

        await using var s = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests("Russian"); using var b = s.Render();
        foreach (string text in new[] { "Таг-6 73%", "73%", "73,4 у.е. · 0,4°", "3 стреляют · 1 без цели" })
            Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
        var f = s.Screen.CaptureFrame(1920, 1080)!;
        var point = f.View.Project(f.Battle.Units.Single(u => u.Id == "Таг-2").Pose);
        s.Screen.OnMouseDown(point.X * 1.5f, point.Y * 1.5f, MouseButton.Left);
        Assert.Equal("Таг-6", s.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected!.TargetId);
        var hold = s.Screen.CardCommandBounds("hold");
        Assert.False(hold.IsEmpty);
        s.Screen.OnMouseDown(hold.MidX, hold.MidY, MouseButton.Left);
        Assert.Null(s.Screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected!.TargetId);
    }
}
