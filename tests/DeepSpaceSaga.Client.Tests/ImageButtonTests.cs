using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The shared nine-sliced BACK/PLAY-style button (Images/UI/Buttons/button.png, drawn via
/// NinePatch — see NinePatchTests.cs), reusable across any screen that wants this look.
/// </summary>
public class ImageButtonTests
{
    [Fact]
    public void Button_image_is_loaded()
    {
        // Regression: the button.png asset must resolve at the client's working directory
        // and be registered in the .csproj with CopyToOutputDirectory, or every screen
        // using ImageButton.Draw silently falls back to MenuStyle's flat style.
        Assert.True(ImageButton.HasLoadedImage);
    }

    [Fact]
    public void Draw_does_not_throw_for_any_button_state()
    {
        using var bitmap = new SKBitmap(200, 80);
        using var canvas = new SKCanvas(bitmap);
        var rect = new SKRect(10, 10, 190, 60);

        foreach (var state in new[] { ButtonState.Normal, ButtonState.Hovered, ButtonState.Pressed, ButtonState.Disabled })
            ImageButton.Draw(canvas, rect, "TEST", state);
    }
}
