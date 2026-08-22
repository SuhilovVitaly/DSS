using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The shared nine-sliced bordered panel (Images/UI/Panels/micro-panel.png, drawn via
/// NinePatch — see NinePatchTests.cs), reusable across any screen that wants this panel
/// look.
/// </summary>
public class ImagePanelTests
{
    [Fact]
    public void Panel_image_is_loaded()
    {
        // Regression: the micro-panel.png asset must resolve at the client's working
        // directory and be registered in the .csproj with CopyToOutputDirectory, or every
        // screen using ImagePanel.Draw silently falls back to MenuStyle's plain fill.
        Assert.True(ImagePanel.HasLoadedImage);
    }

    [Fact]
    public void Draw_does_not_throw()
    {
        using var bitmap = new SKBitmap(200, 150);
        using var canvas = new SKCanvas(bitmap);
        ImagePanel.Draw(canvas, new SKRect(10, 10, 190, 140));
    }
}
