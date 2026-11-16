using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.Save;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class SaveScreenVisualTests
{
    [Fact]
    public void Render_draws_an_opaque_Generic_Type_A_window_with_always_visible_input()
    {
        using var bitmap = new SKBitmap(1280, 720);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var slots = new[]
        {
            new SaveSlotInfo("slot-1", "Docked at New Hope", DateTime.UtcNow),
            new SaveSlotInfo("slot-2", "Deep Space Patrol", DateTime.UtcNow.AddHours(-2))
        };
        var screen = new SaveScreen(() => slots, _ => { }, _ => { });
        var panel = SaveLayout.PanelRect(bitmap.Width, bitmap.Height);

        // Simulates an underlying Main Menu title occupying the same header area.
        using var underlyingPaint = new SKPaint { Color = SKColors.Red };
        canvas.DrawRect(new SKRect(panel.Left + 100f, panel.Top + 20f,
            panel.Right - 100f, panel.Top + 90f), underlyingPaint);

        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        var input = SaveLayout.NameInputRect();
        Assert.True(GenericWindowTypeA.HasAssets);
        Assert.True(GenericButtonTypeA.HasAssets);
        Assert.Equal(255, bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 70).Alpha);
        Assert.NotEqual(SKColors.Red, bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 70));
        Assert.True(bitmap.GetPixel(
            (int)(panel.Left + input.X + 2),
            (int)(panel.Top + input.Y + input.H / 2f)).Alpha > 0);
    }

    [Fact]
    public void All_three_actions_fill_one_centered_bottom_row()
    {
        var close = SaveLayout.CloseButtonRect();
        var delete = SaveLayout.DeleteButtonRect();
        var save = SaveLayout.SaveButtonRect();

        Assert.Equal(close.Y, delete.Y);
        Assert.Equal(close.Y, save.Y);
        Assert.Equal(close.X, SaveLayout.PanelWidth - (save.X + save.W));
    }

    [Fact]
    public void Overwrite_label_uses_the_Xenon_orange_accent_but_NewSave_does_not()
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var slots = new[]
        {
            new SaveSlotInfo("slot-1", "Existing Save", DateTime.UtcNow)
        };
        var screen = new SaveScreen(() => slots, _ => { }, _ => { });
        var panel = SaveLayout.PanelRect(bitmap.Width, bitmap.Height);
        var localButton = SaveLayout.SaveButtonRect();
        var button = new SKRect(
            panel.Left + localButton.X, panel.Top + localButton.Y,
            panel.Left + localButton.X + localButton.W,
            panel.Top + localButton.Y + localButton.H);

        bitmap.Erase(SKColors.Transparent);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();
        Assert.False(ContainsColor(bitmap, button, XenonStyle.OrangeAccent));

        foreach (char c in "Existing Save")
            screen.OnTextInput(c);
        bitmap.Erase(SKColors.Transparent);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        var row = SaveLayout.RowRect(0);
        var selectedRow = new SKRect(
            panel.Left + row.X, panel.Top + row.Y,
            panel.Left + row.X + row.W, panel.Top + row.Y + row.H);
        Assert.Equal("OVERWRITE", screen.SaveActionLabel);
        Assert.Equal("slot-1", screen.SelectedSlotId);
        Assert.True(ContainsColor(bitmap, selectedRow, XenonStyle.OrangeAccent));
        Assert.True(ContainsColor(bitmap, button, XenonStyle.OrangeAccent));
    }

    [Fact]
    public void Delete_confirmation_label_uses_the_danger_color_like_the_load_window()
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var slots = new[]
        {
            new SaveSlotInfo("slot-1", "Existing Save", DateTime.UtcNow)
        };
        long now = 0;
        var screen = new SaveScreen(() => slots, _ => { }, _ => { }, () => now);
        var panel = SaveLayout.PanelRect(bitmap.Width, bitmap.Height);
        var delete = ScreenRect(panel, SaveLayout.DeleteButtonRect());

        bitmap.Erase(SKColors.Transparent);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();
        var row = ScreenRect(panel, SaveLayout.RowRect(0));
        screen.OnMouseDown(row.MidX, row.MidY);
        screen.OnMouseDown(delete.MidX, delete.MidY);

        bitmap.Erase(SKColors.Transparent);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        Assert.True(ContainsColor(bitmap, delete, TabletopStyle.Danger));
    }

    [Fact]
    public void Plain_delete_label_does_not_use_the_danger_color()
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var slots = new[]
        {
            new SaveSlotInfo("slot-1", "Existing Save", DateTime.UtcNow)
        };
        var screen = new SaveScreen(() => slots, _ => { }, _ => { }, () => 0);
        var panel = SaveLayout.PanelRect(bitmap.Width, bitmap.Height);
        var delete = ScreenRect(panel, SaveLayout.DeleteButtonRect());

        bitmap.Erase(SKColors.Transparent);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();
        var row = ScreenRect(panel, SaveLayout.RowRect(0));
        screen.OnMouseDown(row.MidX, row.MidY);

        bitmap.Erase(SKColors.Transparent);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        Assert.False(ContainsColor(bitmap, delete, TabletopStyle.Danger));
    }

    [Fact]
    public void Save_button_uses_the_primary_style_like_the_load_button()
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var slots = new[]
        {
            new SaveSlotInfo("slot-1", "Existing Save", DateTime.UtcNow)
        };
        var screen = new SaveScreen(() => slots, _ => { }, _ => { });
        var panel = SaveLayout.PanelRect(bitmap.Width, bitmap.Height);
        var save = ScreenRect(panel, SaveLayout.SaveButtonRect());
        var close = ScreenRect(panel, SaveLayout.CloseButtonRect());

        bitmap.Erase(SKColors.Transparent);
        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        // Primary buttons draw their marker and label in PrimaryInk on the gold fill;
        // secondary buttons (CLOSE) never use that ink.
        Assert.True(ContainsColor(bitmap, save, TabletopStyle.PrimaryInk));
        Assert.False(ContainsColor(bitmap, close, TabletopStyle.PrimaryInk));
    }

    private static SKRect ScreenRect(SKRect panel, (float X, float Y, float W, float H) local) =>
        new(panel.Left + local.X, panel.Top + local.Y,
            panel.Left + local.X + local.W, panel.Top + local.Y + local.H);

    private static bool ContainsColor(SKBitmap bitmap, SKRect bounds, SKColor color)
    {
        for (int y = (int)bounds.Top; y < (int)bounds.Bottom; y++)
        for (int x = (int)bounds.Left; x < (int)bounds.Right; x++)
        {
            if (bitmap.GetPixel(x, y) == color)
                return true;
        }

        return false;
    }

}
