using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
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

    [Fact]
    public void Skin_selector_routes_only_visible_options_and_preserves_selection_on_cancel_or_failure()
    {
        const int width = 640, height = 480;
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        var calls = new List<string>();
        var items = new[] { ("classic", "Classic") }.Concat(Enumerable.Range(1, 19)
            .Select(i => ($"pack-{i:00}", "Same name"))).ToArray();
        var screen = new SettingsScreen(["Monitor 1"], 0, _ => throw new InvalidOperationException(),
            1, _ => throw new InvalidOperationException(), "English", _ => throw new InvalidOperationException(),
            items, "classic", id =>
            {
                calls.Add(id);
                return id != "pack-19";
            });
        screen.Render(canvas, width, height);
        var field = SettingsLayout.SkinField(width, height);

        Click(field.MidY);
        screen.OnMouseWheel(field.MidX, field.MidY, 10000);
        screen.Render(canvas, width, height);
        Click(field.Top - 15);
        Assert.Equal("classic", screen.SelectedSkinId);
        Assert.True(screen.SkinSaveFailed);

        Click(field.MidY);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.Equal("classic", screen.SelectedSkinId);
        Click(field.MidY);
        screen.OnMouseWheel(field.MidX, field.MidY, 10000);
        Click(field.Top - 45);
        Assert.Equal("pack-18", screen.SelectedSkinId);
        Assert.False(screen.SkinSaveFailed);
        Assert.Equal(new[] { "pack-19", "pack-18" }, calls);

        void Click(float y) => Assert.Equal(ScreenEvent.None, screen.OnMouseDown(field.MidX, y, MouseButton.Left));
    }

    [Theory]
    [InlineData(640, 480)]
    [InlineData(1280, 720)]
    public void Skin_selector_layout_fits_and_does_not_cover_exit_hit_target_when_closed(int width, int height)
    {
        var field = SettingsLayout.SkinField(width, height);
        float exitY = SettingsLayout.PanelTop(height) + SettingsLayout.SkinExitY;
        Assert.True(SKRect.Create(width, height).Contains(field));
        Assert.True(exitY > field.Bottom);
        Assert.True(exitY + SettingsLayout.ButtonHeight <= height);
        Assert.Equal(SettingsButton.SkinCombo, SettingsLayout.HitTest(field.MidX, field.MidY, width, height, hasSkinPicker: true));
        Assert.Equal(SettingsButton.Exit, SettingsLayout.HitTest(width / 2f, exitY + 12, width, height, hasSkinPicker: true));
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
