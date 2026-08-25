using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// ScenarioSelect's Generic Type A shell and action-button presentation. Behavior is
/// covered by ScreenEventTests.cs.
/// </summary>
public class ScenarioSelectScreenTests
{
    [Fact]
    public void Generic_Type_A_assets_are_loaded()
    {
        Assert.True(GenericWindowTypeA.HasAssets);
        Assert.True(GenericButtonTypeA.HasAssets);
    }

    [Fact]
    public void Window_shell_draws_the_Type_A_frame_at_ScenarioSelect_bounds()
    {
        using var bitmap = new SKBitmap(920, 640);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var panel = new SKRect(10, 10, 910, 630);

        ScenarioSelectScreen.DrawWindowShell(canvas, panel);
        canvas.Flush();

        Assert.True(bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 2).Alpha > 0);
        Assert.True(bitmap.GetPixel((int)panel.Left + 2, (int)panel.MidY).Alpha > 0);
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Localized_action_labels_fit_Type_A_buttons(string language)
    {
        var strings = Localization.LoadLocaleFile(language);
        Assert.NotNull(strings);
        Assert.True(XenonStyle.ButtonText.MeasureText(strings!["ScenarioSelect.Back"])
            <= ScenarioSelectLayout.ActionButtonWidth - 80f);
        Assert.True(XenonStyle.ButtonText.MeasureText(strings["ScenarioSelect.Play"])
            <= ScenarioSelectLayout.ActionButtonWidth - 80f);
    }
}
