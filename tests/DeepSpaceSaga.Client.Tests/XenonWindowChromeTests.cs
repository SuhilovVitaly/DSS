using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class XenonWindowChromeTests
{
    [Fact]
    public void Production_chrome_assets_are_loaded()
    {
        Assert.True(XenonWindowChrome.HasChromeAssets);
        Assert.True(XenonMenuButton.HasButtonAssets);
    }

    [Fact]
    public void Draw_does_not_throw_for_any_close_state()
    {
        using var bitmap = new SKBitmap(520, 570);
        using var canvas = new SKCanvas(bitmap);
        foreach (var state in Enum.GetValues<ButtonState>())
        {
            XenonWindowChrome.Draw(canvas,
                new SKRect(10, 10, 510, 560),
                new SKRect(68, 34, 452, 110),
                new SKRect(34, 122, 56, 464),
                new SKRect(464, 122, 486, 464),
                new SKRect(68, 486, 452, 542),
                new SKRect(460, 30, 492, 62),
                "DEEP SPACE SAGA", "GAME MENU // SIMULATION PAUSED", "ESC  RESUME", "Version 1.0", state);
        }
    }
}
