using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class SettingsScreenTests
{
    [Fact]
    public void Generic_Type_A_and_Xenon_combo_assets_are_loaded()
    {
        Assert.True(GenericWindowTypeA.HasAssets);
        Assert.True(GenericButtonTypeA.HasAssets);
        Assert.True(XenonComboBox.HasAssets);
    }

    [Fact]
    public void Combo_boxes_keep_the_reference_asset_geometry()
    {
        Assert.Equal(XenonComboBox.NativeWidth, SettingsLayout.MonitorComboWidth);
        Assert.Equal(XenonComboBox.NativeWidth, SettingsLayout.LanguageComboWidth);
        Assert.Equal(XenonComboBox.NativeWidth, SettingsLayout.UiScaleComboWidth);
        Assert.Equal(XenonComboBox.FieldHeight, SettingsLayout.MonitorComboHeight);
        Assert.Equal(XenonComboBox.FieldHeight, SettingsLayout.LanguageComboHeight);
        Assert.Equal(XenonComboBox.FieldHeight, SettingsLayout.UiScaleComboHeight);
    }

    [Fact]
    public void Generic_button_matches_the_other_Type_A_menu_buttons()
    {
        Assert.Equal(384f, SettingsLayout.ButtonWidth);
        Assert.Equal(56f, SettingsLayout.ButtonHeight);
        Assert.Equal(396f, SettingsLayout.ExitY);
    }

    [Fact]
    public void Combo_states_draw_different_pixels()
    {
        using var normal = DrawCombo(highlighted: false);
        using var hover = DrawCombo(highlighted: true);

        Assert.NotEqual(PixelSignature(normal), PixelSignature(hover));
    }

    [Fact]
    public void Settings_render_smoke_test_draws_the_Type_A_shell()
    {
        using var bitmap = new SKBitmap(1280, 720);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var screen = new SettingsScreen(
            new[] { "Monitor 1 (1920x1080)" }, 0, _ => { },
            1.0, _ => { }, "English", _ => { });

        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        var panel = SettingsLayout.PanelRect(bitmap.Width, bitmap.Height);
        Assert.True(bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 2).Alpha > 0);
        Assert.True(bitmap.GetPixel((int)panel.Left + 2, (int)panel.MidY).Alpha > 0);
    }

    private static SKBitmap DrawCombo(bool highlighted)
    {
        var bitmap = new SKBitmap(375, 78);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        XenonComboBox.DrawClosed(canvas, new SKRect(10, 34, 365, 68),
            "MONITOR", "Monitor 1", highlighted);
        canvas.Flush();
        return bitmap;
    }

    private static uint PixelSignature(SKBitmap bitmap)
    {
        uint signature = 2166136261;
        for (int x = 0; x < bitmap.Width; x += 5)
        for (int y = 0; y < bitmap.Height; y += 5)
        {
            var color = bitmap.GetPixel(x, y);
            signature = (signature ^ color.Red) * 16777619;
            signature = (signature ^ color.Green) * 16777619;
            signature = (signature ^ color.Blue) * 16777619;
            signature = (signature ^ color.Alpha) * 16777619;
        }

        return signature;
    }
}
