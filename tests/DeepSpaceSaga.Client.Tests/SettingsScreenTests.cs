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

    [Theory]
    [InlineData(640, 480)]
    [InlineData(1280, 720)]
    public void Tutorial_hints_row_fits_between_skin_row_and_exit_without_overlap(int width, int height)
    {
        float top = SettingsLayout.PanelTop(height);
        var hints = SettingsLayout.TutorialHintsField(width, height);
        var skin = SettingsLayout.SkinField(width, height);
        var exit = Button(SettingsLayout.SkinExitY);
        var window = SKRect.Create(width, height);
        // Every element the screen can draw at once: label strips and fields of each row, the restart note,
        // the two save-failure messages and the exit button.
        var elements = new (string Name, SKRect Rect)[]
        {
            ("monitor", Row(SettingsLayout.MonitorRowY)),
            ("note", new(0, top + SettingsLayout.MonitorNoteY - XenonStyle.FooterFontSize, width, top + SettingsLayout.MonitorNoteY + 4)),
            ("language", Row(SettingsLayout.LanguageRowY)),
            ("scale", Row(SettingsLayout.UiScaleRowY)),
            ("skin", new(skin.Left, skin.Top - XenonComboBox.LabelHeight, skin.Right, skin.Bottom)),
            ("skin-error", new(skin.Left, skin.Bottom + 3, skin.Right, skin.Bottom + 25)),
            ("hints", hints),
            ("hints-error", new(hints.Left, hints.Bottom + 3, hints.Right, hints.Bottom + 25)),
            ("exit", exit),
        };
        for (int i = 0; i < elements.Length; i++)
        for (int j = i + 1; j < elements.Length; j++)
            Assert.False(elements[i].Rect.IntersectsWith(elements[j].Rect), $"{elements[i].Name} overlaps {elements[j].Name}");
        foreach (var (name, rect) in elements)
            Assert.True(window.Contains(rect), $"{name} leaves the {width}x{height} window");
        Assert.Equal(XenonComboBox.NativeWidth, hints.Width);
        Assert.Equal(26f, hints.Height);
        Assert.True(hints.Top > skin.Bottom && hints.Bottom < exit.Top);

        Assert.Equal(SettingsButton.TutorialHints,
            SettingsLayout.HitTest(hints.MidX, hints.MidY, width, height, hasSkinPicker: true, hasTutorialHints: true));
        Assert.Equal(SettingsButton.None,
            SettingsLayout.HitTest(hints.MidX, hints.MidY, width, height, hasSkinPicker: true, hasTutorialHints: false));
        Assert.Equal(SettingsButton.SkinCombo,
            SettingsLayout.HitTest(skin.MidX, skin.MidY, width, height, hasSkinPicker: true, hasTutorialHints: true));
        Assert.Equal(SettingsButton.Exit,
            SettingsLayout.HitTest(width / 2f, exit.Top + 12, width, height, hasSkinPicker: true, hasTutorialHints: true));
        Assert.Equal(396f, SettingsLayout.ExitY); // the public path is unchanged

        SKRect Row(float rowY)
        {
            float left = SettingsLayout.PanelLeft(width) + SettingsLayout.RowRightX - XenonComboBox.NativeWidth;
            return new(left, top + rowY - XenonComboBox.LabelHeight, left + XenonComboBox.NativeWidth, top + rowY + XenonComboBox.FieldHeight);
        }

        SKRect Button(float buttonY)
        {
            float left = SettingsLayout.PanelLeft(width) + (SettingsLayout.PanelWidth - SettingsLayout.ButtonWidth) / 2f;
            return new(left, top + buttonY, left + SettingsLayout.ButtonWidth, top + buttonY + SettingsLayout.ButtonHeight);
        }
    }

    [Fact]
    public void Tutorial_hints_click_toggles_and_failed_save_restores()
    {
        const int width = 640, height = 480;
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        var calls = new List<bool>();
        bool accept = true;
        var screen = new SettingsScreen(["Monitor 1"], 0, _ => { }, 1, _ => { }, "English", _ => { },
            [("classic", "Classic")], "classic", _ => true,
            tutorialHints: true, trySetTutorialHints: value =>
            {
                calls.Add(value);
                return accept;
            });
        screen.Render(canvas, width, height);
        var row = SettingsLayout.TutorialHintsField(width, height);
        Assert.True(screen.TutorialHintsEnabled);

        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(row.MidX, row.MidY, MouseButton.Left));
        Assert.False(screen.TutorialHintsEnabled);
        Assert.False(screen.TutorialHintsSaveFailed);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(row.MidX, row.MidY, MouseButton.Right)); // only the left button
        Assert.False(screen.TutorialHintsEnabled);

        accept = false;
        screen.OnMouseDown(row.MidX, row.MidY, MouseButton.Left);
        Assert.False(screen.TutorialHintsEnabled); // restored
        Assert.True(screen.TutorialHintsSaveFailed);
        using (var failed = Snapshot(screen))
        {
            accept = true;
            screen.OnMouseDown(row.MidX, row.MidY, MouseButton.Left);
            Assert.True(screen.TutorialHintsEnabled);
            Assert.False(screen.TutorialHintsSaveFailed);
            using var saved = Snapshot(screen);
            Assert.NotEqual(PixelSignature(failed), PixelSignature(saved)); // the failure message is drawn under the row
        }
        Assert.Equal(new[] { false, true, true }, calls);

        // An open drop-down owns the click: the row underneath does not toggle.
        float languageY = SettingsLayout.PanelTop(height) + SettingsLayout.LanguageRowY + 10;
        screen.OnMouseDown(width / 2f, languageY, MouseButton.Left);
        screen.OnMouseDown(row.MidX, row.MidY, MouseButton.Left);
        Assert.True(screen.TutorialHintsEnabled);
        Assert.Equal(3, calls.Count);

        // Hover reports the row; without the callback there is no row at all.
        Assert.True(screen.OnMouseMove(row.MidX, row.MidY));
        var plain = new SettingsScreen(["Monitor 1"], 0, _ => { }, 1, _ => { }, "English", _ => { },
            [("classic", "Classic")], "classic", _ => true);
        plain.Render(canvas, width, height);
        Assert.False(plain.OnMouseMove(row.MidX, row.MidY));
        Assert.True(plain.TutorialHintsEnabled);

        static SKBitmap Snapshot(SettingsScreen screen)
        {
            var bitmap = new SKBitmap(width, height);
            bitmap.Erase(SKColors.Transparent);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, width, height);
            canvas.Flush();
            return bitmap;
        }
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
