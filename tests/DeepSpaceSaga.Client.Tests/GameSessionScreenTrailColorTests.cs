using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GameSessionScreenTrailColorTests
{
    [Theory]
    [InlineData(-1, 13)]
    [InlineData(0, 13)]
    [InlineData(.5f, 51)]
    [InlineData(1, 89)]
    [InlineData(2, 89)]
    public void TrailOpacityEndpoints_preserve_ship_relation_and_clamp(float t, byte alpha)
    {
        var enemy = new SKColor(240, 128, 136);
        Assert.Equal(enemy.WithAlpha(alpha), GameSessionScreen.GetTrailSegmentColor(t, true, enemy));
    }

    [Fact]
    public void Other_trail_has_independent_faint_endpoints()
    {
        Assert.Equal(new SKColor(151, 168, 184, 13), GameSessionScreen.GetTrailSegmentColor(0, false));
        Assert.Equal(new SKColor(151, 168, 184, 64), GameSessionScreen.GetTrailSegmentColor(1, false));
        Assert.Equal(SpaceMapColorResolver.PlayerShipColor.WithAlpha(89), GameSessionScreen.GetTrailSegmentColor(1, true));
    }
}
