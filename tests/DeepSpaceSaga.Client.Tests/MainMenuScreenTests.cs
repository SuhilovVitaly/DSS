using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// MainMenu's Type A shell. Open/close/hover behavior is covered by ScreenEventTests.cs.
/// </summary>
public class MainMenuScreenTests
{
    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Localized_labels_fit_the_Type_A_text_area(string language)
    {
        var strings = Localization.LoadLocaleFile(language);
        Assert.NotNull(strings);

        foreach (var key in new[] { "NewGame", "Load", "Settings", "Exit" })
        {
            Assert.True(XenonStyle.ButtonText.MeasureText(strings![$"MainMenu.{key}"])
                <= MenuLayout.ButtonWidth - 112f);
        }
    }

    [Fact]
    public void Generic_type_A_shell_asset_is_loaded()
    {
        Assert.True(GenericWindowTypeA.HasAssets);
    }

    [Fact]
    public void Window_shell_draws_the_Type_A_frame_at_the_MainMenu_bounds()
    {
        using var bitmap = new SKBitmap(520, 570);
        using var canvas = new SKCanvas(bitmap);
        var panel = new SKRect(10, 10, 510, 560);

        MainMenuScreen.DrawWindowShell(canvas, panel);
        canvas.Flush();

        var borderPixel = bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 2);
        var interiorPixel = bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 30);
        Assert.NotEqual(borderPixel, interiorPixel);
    }
}
