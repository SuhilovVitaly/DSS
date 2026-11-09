using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class MapDesignTests
{
    [Fact]
    public async Task Markers_labels_and_carrier_edges_follow_design()
    {
        await using var s = await Ep0019Scenes.Create("2-fire"); using var b = s.Render(); var f = s.Screen.CaptureFrame(1920, 1080)!;
        var inside = new SKRect(236, 40, 1000, 680);
        int edges = f.Battle.Units.Count(u => u.Kind == TacticalUnitKind.Carrier && !inside.Contains(f.View.Project(u.Pose)));
        Assert.Equal(12 - edges, s.Screen.Layout.Rects.Count(p => p.Key.StartsWith("label:", StringComparison.Ordinal)));
        Assert.Equal(edges, s.Screen.Layout.Rects.Count(p => p.Key.StartsWith("edge:", StringComparison.Ordinal)));
        foreach (var u in f.Battle.Units.Where(u => u.Kind == TacticalUnitKind.Drone))
        {
            var p = f.View.Project(u.Pose);
            Assert.True(RasterProbe.Fraction(b, SKRect.Create(p.X - 1.5f, p.Y - 1.5f, 3, 3), u.Side == f.Battle.PlayerSide ? BattleDesign.Cyan : BattleDesign.Orange) >= .5);
            var r = s.Screen.Layout.Rects["label:" + u.Id]; Assert.False(r.Contains(p));
        }
    }
}
