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
    public void Disabled_field_differs_from_normal_field_and_never_gets_the_gold_edge()
    {
        var bounds = new SKRect(4, 4, 36, 28);
        using var normal = Render(canvas => TabletopStyle.Field(canvas, bounds));
        using var disabled = Render(canvas => TabletopStyle.Field(canvas, bounds, enabled: false));
        using var disabledFocused = Render(canvas => TabletopStyle.Field(canvas, bounds, focused: true, enabled: false));

        Assert.Equal(TabletopStyle.Inset, normal.GetPixel(20, 16));
        Assert.NotEqual(normal.GetPixel(20, 16), disabled.GetPixel(20, 16));
        Assert.Equal(disabled.Pixels, disabledFocused.Pixels);
        Assert.Equal(TabletopStyle.Edge, disabledFocused.GetPixel(20, 4));

        using var frameEnabled = Render(canvas => ControlVisuals.Frame(canvas, bounds, active: false, enabled: true, scale: 1));
        using var frameDisabled = Render(canvas => ControlVisuals.Frame(canvas, bounds, active: false, enabled: false, scale: 1));
        using var frameDisabledActive = Render(canvas => ControlVisuals.Frame(canvas, bounds, active: true, enabled: false, scale: 1));
        Assert.Equal(normal.Pixels, frameEnabled.Pixels);
        Assert.Equal(disabled.Pixels, frameDisabled.Pixels);
        Assert.Equal(disabled.Pixels, frameDisabledActive.Pixels);
        Assert.NotEqual(frameEnabled.GetPixel(20, 16), frameDisabled.GetPixel(20, 16));
    }

    [Fact]
    public void Tooltip_outside_battle_skin_has_no_gold_focus_edge()
    {
        using var bitmap = Render(canvas =>
            ControlText.Tooltip(canvas, "Hint", new SKPoint(100, 100), new SKRect(0, 0, 640, 240), 1), 640, 240);

        // One-line tooltip at scale 1: left = pointer.X + 12, top = pointer.Y + 16, width 400.
        var topEdge = Enumerable.Range(112, 400).Select(x => bitmap.GetPixel(x, 116)).ToArray();
        Assert.All(topEdge, pixel => Assert.Equal(TabletopStyle.Edge, pixel));
        Assert.DoesNotContain(TabletopStyle.Gold, bitmap.Pixels);
    }

    private static SKBitmap Render(Action<SKCanvas> draw, int width = 40, int height = 32)
    {
        var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        draw(canvas);
        canvas.Flush();
        return bitmap;
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
