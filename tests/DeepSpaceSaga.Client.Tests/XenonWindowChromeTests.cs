using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class XenonWindowChromeTests
{
    [Fact]
    public void Production_chrome_assets_are_loaded()
    {
        Assert.True(GenericWindowTypeA.HasAssets);
        Assert.True(GenericButtonTypeA.HasAssets);
    }

    [Fact]
    public void Draw_does_not_throw()
    {
        using var bitmap = new SKBitmap(520, 570);
        using var canvas = new SKCanvas(bitmap);
        XenonWindowChrome.Draw(canvas,
            new SKRect(10, 10, 510, 560),
            new SKRect(68, 486, 452, 542),
            "DEEP SPACE SAGA", "Version 1.0", "ESC  RESUME");
    }
}
