using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameMenu;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GameMenuVisualTests
{
    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Localized_labels_fit_precomputed_text_areas(string language)
    {
        var strings = Localization.LoadLocaleFile(language);
        Assert.NotNull(strings);
        Assert.True(XenonStyle.SubtitleText.MeasureText(strings!["GameMenu.Status"])
            <= GameMenuLayout.ButtonWidth - 24f);

        foreach (var key in new[] { "Resume", "Save", "Load", "Settings", "MainMenu" })
            Assert.True(XenonStyle.ButtonText.MeasureText(strings[$"GameMenu.{key}"])
                <= GameMenuLayout.ButtonWidth - 112f);
    }

    [Fact]
    public void Render_smoke_test_produces_non_empty_panel()
    {
        using var bitmap = new SKBitmap(1280, 720);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var screen = new GameMenuScreen();
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        var resume = GameMenuLayout.ButtonRect(GameMenuButton.Resume, bitmap.Width, bitmap.Height);
        screen.OnMouseMove(resume.MidX, resume.MidY);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        var panel = GameMenuLayout.PanelRect(bitmap.Width, bitmap.Height);
        Assert.NotEqual(SKColors.Transparent, bitmap.GetPixel((int)panel.MidX, (int)panel.MidY));
        Assert.True(bitmap.GetPixel((int)panel.Left + 3, (int)panel.MidY).Alpha > 0);
    }

    [Fact]
    public void Every_button_state_draws_without_exception_and_changes_pixels()
    {
        var signatures = new HashSet<uint>();
        foreach (var state in Enum.GetValues<ButtonState>())
        {
            using var bitmap = new SKBitmap(400, 72);
            bitmap.Erase(SKColors.Transparent);
            using var canvas = new SKCanvas(bitmap);
            XenonMenuButton.Draw(canvas, new SKRect(8, 8, 392, 64), "ПРОДОЛЖИТЬ", state);
            canvas.Flush();

            uint signature = 2166136261;
            for (int x = 0; x < bitmap.Width; x += 8)
            for (int y = 0; y < bitmap.Height; y += 8)
            {
                var color = bitmap.GetPixel(x, y);
                signature = (signature ^ color.Red) * 16777619;
                signature = (signature ^ color.Green) * 16777619;
                signature = (signature ^ color.Blue) * 16777619;
                signature = (signature ^ color.Alpha) * 16777619;
            }
            signatures.Add(signature);
        }

        Assert.Equal(4, signatures.Count);
    }
}
