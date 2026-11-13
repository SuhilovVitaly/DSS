using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhaseCameraTests
{
    [Theory]
    [InlineData("1-move", 50)]
    [InlineData("2-fire", 70)]
    public async Task Phase_fit_matches_formula(string name, double margin)
    {
        await using var s = await Ep0019Scenes.Create(name);
        var f = s.Screen.CaptureFrame(1920, 1080)!;
        var points = f.Battle.Units.Where(u => name == "1-move" || u.Kind == TacticalUnitKind.Drone).Select(u => u.Pose)
            .Concat(f.Battle.Paths.SelectMany(p => p.Points)).ToList();
        if (f.Movement is { } move) points.AddRange(move.Orders.Select(o => o.Path[^1]));
        double expected = Math.Min((832 - 2 * margin) / (points.Max(p => p.X) - points.Min(p => p.X)), (708 - 2 * margin) / (points.Max(p => p.Y) - points.Min(p => p.Y)));
        Assert.InRange(f.View.PixelsPerUnit, expected - .00001, expected + .00001);
        var center = f.View.Project((points.Max(p => p.X) + points.Min(p => p.X)) / 2, (points.Max(p => p.Y) + points.Min(p => p.Y)) / 2);
        Assert.InRange(center.X, 617.99, 618.01); Assert.InRange(center.Y, 359.99, 360.01);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Phase_transitions_keep_center_and_scale_through_movement_and_salvo(bool tabletop, bool adjustCamera)
    {
        using var profile = tabletop ? new TabletopTestProfile() : null;
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover", settingsPath: profile?.SettingsPath);
        scene.Fixture.ManualAnimation = true;
        var screen = scene.Screen;
        Assert.Equal(tabletop ? "tabletop" : "classic", screen.SkinId);
        using var raster = scene.Render();
        var initial = scene.Frame.View;
        if (adjustCamera)
        {
            screen.OnMouseWheel(900, 540, -100);
            screen.OnMouseDown(900, 540, MouseButton.Right);
            screen.OnMouseMove(960, 570);
            screen.OnMouseUp(960, 570, MouseButton.Right);
        }
        var expected = scene.Frame.View;
        if (adjustCamera)
        {
            Assert.NotEqual(initial.PixelsPerUnit, expected.PixelsPerUnit);
            Assert.NotEqual(initial.CenterX, expected.CenterX);
        }
        while (scene.Frame.Battle.Phase == TacticalBattlePhase.Move)
        {
            Assert.True(scene.Frame.Battle.Turn <= 4);
            await scene.Fixture.Route(screen.OnKeyDown(Key.Enter));
            Assert.True(scene.Frame.Pending);
            Assert.Equal(expected, scene.Frame.View);
            scene.Fixture.AdvanceAnimation(650);
            Assert.Equal(expected, scene.Frame.View);
            scene.Fixture.FinishPresentation();
            Assert.Equal(expected, scene.Frame.View);
        }
        Assert.Equal(TacticalBattlePhase.Fire, scene.Frame.Battle.Phase);

        await scene.Fixture.Route(screen.OnKeyDown(Key.Enter));
        var flight = scene.Frame;
        Assert.True(flight.Salvo?.InFlight);
        Assert.Equal(expected, flight.View);
        screen.OnKeyDown(Key.Enter);
        var results = scene.Frame;
        Assert.False(results.Salvo?.InFlight);
        Assert.Equal(expected, results.View);
        screen.OnKeyDown(Key.Enter);
        var next = scene.Frame;
        Assert.Null(next.Salvo);
        Assert.Equal(expected, next.View);
    }
}
