using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TabletopStyleTests
{
    [Fact]
    public void Plate_and_field_reverse_the_bevel_without_drawing_outside_bounds()
    {
        using var bitmap = new SKBitmap(40, 32);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var bounds = new SKRect(4, 4, 36, 28);

        TabletopStyle.Panel(canvas, bounds);
        var raisedTop = bitmap.GetPixel(20, 5);
        var raisedBottom = bitmap.GetPixel(20, 26);
        Assert.Equal(TabletopStyle.Steel, bitmap.GetPixel(20, 16));

        TabletopStyle.Field(canvas, bounds, focused: true);
        Assert.Equal(raisedBottom, bitmap.GetPixel(20, 5));
        Assert.Equal(raisedTop, bitmap.GetPixel(20, 26));
        Assert.Equal(TabletopStyle.Inset, bitmap.GetPixel(20, 16));
        Assert.Equal(TabletopStyle.Gold, bitmap.GetPixel(20, 4));
        Assert.Equal(0, bitmap.GetPixel(20, 3).Alpha);
        Assert.Equal(0, bitmap.GetPixel(36, 16).Alpha);
    }

    [Fact]
    public void Primary_button_states_have_distinct_fills_and_disabled_text()
    {
        using var bitmap = new SKBitmap(40, 32);
        using var canvas = new SKCanvas(bitmap);
        var bounds = new SKRect(4, 4, 36, 28);
        var fills = new HashSet<SKColor>();
        foreach (var state in Enum.GetValues<ButtonState>())
        {
            TabletopStyle.Button(canvas, bounds, state, primary: true);
            Assert.True(fills.Add(bitmap.GetPixel(20, 16)));
        }

        Assert.Equal(TabletopStyle.PrimaryInk, TabletopStyle.ButtonInk(ButtonState.Normal, primary: true));
        Assert.Equal(TabletopStyle.Secondary, TabletopStyle.ButtonInk(ButtonState.Disabled, primary: true));
    }
}
