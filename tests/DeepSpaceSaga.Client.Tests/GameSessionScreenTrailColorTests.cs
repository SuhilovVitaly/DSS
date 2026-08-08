using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GameSessionScreenTrailColorTests
{
    [Fact]
    public void Segment_color_is_gray_at_far_end()
    {
        // t = 0: tail's far/oldest end, alpha = 40 + 120*0 = 40
        Assert.Equal(new SKColor(190, 190, 190, 40), GameSessionScreen.GetTrailSegmentColor(0f));
    }

    [Fact]
    public void Segment_color_is_gray_just_below_two_thirds()
    {
        // t = 0.6f <= 2/3: gray, alpha = (byte)(40 + 120*0.6f) = 112
        Assert.Equal(new SKColor(190, 190, 190, 112), GameSessionScreen.GetTrailSegmentColor(0.6f));
    }

    [Fact]
    public void Segment_color_is_red_just_above_two_thirds()
    {
        // t = 0.7f > 2/3: red, alpha = (byte)(40 + 120*0.7f) = 124
        Assert.Equal(new SKColor(220, 30, 20, 124), GameSessionScreen.GetTrailSegmentColor(0.7f));
    }

    [Fact]
    public void Segment_color_is_red_at_ship_end()
    {
        // t = 1: at the ship, alpha = 40 + 120*1 = 160
        Assert.Equal(new SKColor(220, 30, 20, 160), GameSessionScreen.GetTrailSegmentColor(1f));
    }

    [Fact]
    public void Segment_color_boundary_at_exactly_two_thirds_is_gray()
    {
        // Boundary is exclusive for red (strict '>'): exactly 2/3 stays gray
        Assert.Equal(new SKColor(190, 190, 190, 120), GameSessionScreen.GetTrailSegmentColor(2f / 3f));
    }
}
