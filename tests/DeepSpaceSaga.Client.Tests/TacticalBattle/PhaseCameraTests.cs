using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;

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
}
