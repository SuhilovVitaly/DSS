using System.Numerics;
using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleCameraTests
{
    [Fact]
    public async Task FitIncludesCarriersAndOwnReachableExtremesWithMargin()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var state = screen.Flow.State!;
        var camera = new TacticalBattleCamera();
        camera.Update(state, 880, 720);
        var view = camera.View;
        Assert.True(view.WorldWidth >= 600);
        foreach (var unit in state.Units)
        {
            if (unit.Kind != TacticalUnitKind.Carrier && unit.State != TacticalUnitState.Air) continue;
            AssertMargin(view.Project(unit.Pose));
            if (unit.Side != state.PlayerSide || unit.State != TacticalUnitState.Air || unit.Limits is not { } l) continue;
            var limits = new TacticalLimits(l.Mass, l.MassFactor, l.MinSpeed, l.MaxSpeed, l.Change, l.Turn);
            double max = limits.ClampSpeed(limits.MaxSpeed, unit.Speed);
            double t = limits.TurnLimit(max, state.Rules.TurnAtMaxSpeedFraction);
            foreach (double turn in new[] { -t, 0, t })
            {
                var end = TacticalGeometry.Integrate(new(unit.Pose.X, unit.Pose.Y, unit.Pose.Course), max, turn, state.Rules.Substeps);
                AssertMargin(view.Project(end.X, end.Y));
            }
        }

        void AssertMargin(SKPoint point)
        {
            var margin = (float)(70 * view.PixelsPerUnit) - 0.01f;
            Assert.InRange(point.X, margin, 880 - margin);
            Assert.InRange(point.Y, margin, 720 - margin);
        }
    }

    [Fact]
    public async Task ZoomIsCursorAnchoredAndBoundedAndOnlyManualFitReleasesUserView()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var state = screen.Flow.State!;
        var camera = new TacticalBattleCamera();
        camera.Update(state, 880, 720);
        var fitted = camera.View;
        var anchor = fitted.Unproject(217.5f, 465.25f);
        camera.Zoom(217.5f, 465.25f, -100);
        var actual = camera.View.Project(anchor.X, anchor.Y);
        Assert.Equal(217.5f, actual.X, 4);
        Assert.Equal(465.25f, actual.Y, 4);
        camera.Zoom(217.5f, 465.25f, -1000000);
        Assert.Equal(fitted.WorldWidth * 0.125, camera.View.WorldWidth, 8);
        camera.Zoom(217.5f, 465.25f, 1000000);
        Assert.Equal(fitted.WorldWidth * 4, camera.View.WorldWidth, 8);
        var userView = camera.View;

        var moved = MoveFirstDroneOffscreen(state);
        camera.Update(moved, 880, 720);
        Assert.Equal(userView, camera.View);
        Assert.True(camera.UserView);
        camera.Update(moved, 1320, 800);
        Assert.Equal(userView.WorldWidth, camera.View.WorldWidth, 8);
        Assert.Equal(userView.CenterX, camera.View.CenterX, 8);
        Assert.Equal(userView.CenterY, camera.View.CenterY, 8);
        camera.Fit(moved);
        Assert.False(camera.UserView);
        Assert.InRange(camera.View.Project(moved.Units.First(u => u.State == TacticalUnitState.Air).Pose).X, 1, camera.View.PixelWidth - 1);
    }

    [Fact]
    public async Task AutomaticViewRefitsOnlyWhenLiveDroneLeavesAndFrozenViewDoesNotChange()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var state = screen.Flow.State!;
        var camera = new TacticalBattleCamera();
        camera.Update(state, 880, 720);
        var frozen = camera.View;
        camera.Update(state with { Revision = state.Revision + 1, Phase = TacticalBattlePhase.Fire }, 880, 720);
        Assert.Equal(frozen, camera.View);
        var moved = MoveFirstDroneOffscreen(state);
        var unit = moved.Units.First(u => u.State == TacticalUnitState.Air);
        Assert.True(frozen.Project(unit.Pose).X > 880);
        camera.Update(moved, 880, 720);
        Assert.InRange(camera.View.Project(unit.Pose).X, 1, 879);
        Assert.True(frozen.Project(unit.Pose).X > 880);
        Assert.False(camera.UserView);
    }

    [Fact]
    public async Task ActualBattleScreenRoutesRightDragWheelAndFitWithoutMovingWorldCamera()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.Equal((880, 720), (screen.View.PixelWidth, screen.View.PixelHeight));
        Assert.NotEqual(bitmap.GetPixel(10, 10), bitmap.GetPixel(1270, 710));
        var fit = screen.View;
        var mapCamera = (fixture.Game.CameraFocusX, fixture.Game.CameraFocusY, fixture.Game.CameraPixelsPerWorldUnit);
        var mouse = DispatchProxy.Create<IMouse, MouseRoutingTests.MouseProxy>();
        Invoke("OnMouseDown", mouse, MouseButton.Right);
        Invoke("OnMouseMove", mouse, new Vector2(155.5f, 100.25f));
        Assert.Equal(fit.CenterX - 30 / fit.PixelsPerUnit, screen.View.CenterX, 8);
        Assert.Equal(fit.CenterY - 25 / fit.PixelsPerUnit, screen.View.CenterY, 8);
        Invoke("OnMouseUp", mouse, MouseButton.Right);
        var panned = screen.View;
        Invoke("OnMouseMove", mouse, new Vector2(255.5f, 150.25f));
        Assert.Equal(panned, screen.View);
        Invoke("OnMouseScroll", mouse, new ScrollWheel(0, 1));
        Assert.True(screen.View.WorldWidth < panned.WorldWidth);
        await fixture.PressKey(Key.Number0);
        Assert.Equal(fit, screen.View);
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        screen.Render(canvas, 1280, 720);
        Assert.Equal(mapCamera, (fixture.Game.CameraFocusX, fixture.Game.CameraFocusY, fixture.Game.CameraPixelsPerWorldUnit));

        void Invoke(string method, params object[] args) => typeof(SkiaWindow)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fixture.Window, args);
    }

    private static TacticalBattleSnapshot MoveFirstDroneOffscreen(TacticalBattleSnapshot state)
    {
        var unit = state.Units.First(u => u.State == TacticalUnitState.Air);
        return state with
        {
            Revision = state.Revision + 2,
            Phase = TacticalBattlePhase.Fire,
            Units = state.Units.Replace(unit, unit with { Pose = unit.Pose with { X = unit.Pose.X + 8000 } })
        };
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
