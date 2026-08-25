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

}
