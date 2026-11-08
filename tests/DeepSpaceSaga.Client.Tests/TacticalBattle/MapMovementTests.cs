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
        var middle = f.View.Project(row.Path[row.Path.Length / 2]);
        Assert.True(RasterProbe.Fraction(b, SKRect.Create(middle.X - 1, middle.Y - 1, 2, 2), BattleDesign.Cyan) >= .6);
        var end = f.View.Project(row.Path[^1]);
        s.Screen.OnMouseDown(end.X * 1.5f, end.Y * 1.5f, MouseButton.Left);
        s.Screen.OnMouseMove((end.X - 30) * 1.5f, (end.Y + 20) * 1.5f);
        s.Screen.OnMouseUp((end.X - 30) * 1.5f, (end.Y + 20) * 1.5f, MouseButton.Left);
        Assert.Contains(s.Screen.BuildMoveRequest().Orders, o => o.DroneId == row.Unit.Id && o != row.Suggestion);
    }
}
