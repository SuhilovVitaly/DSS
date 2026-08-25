using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class LoadScreenVisualTests
{
    [Fact]
    public void Render_draws_the_Generic_Type_A_shell_and_bottom_buttons()
    {
        using var bitmap = new SKBitmap(1280, 720);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        var slots = new[]
        {
            new SaveSlotInfo("slot-1", "Docked at New Hope", DateTime.UtcNow),
            new SaveSlotInfo("slot-2", "Deep Space Patrol", DateTime.UtcNow.AddHours(-2))
        };
        var screen = new LoadScreen(() => slots, _ => { });

        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        var panel = LoadLayout.PanelRect(bitmap.Width, bitmap.Height);
        Assert.True(GenericWindowTypeA.HasAssets);
        Assert.True(GenericButtonTypeA.HasAssets);
        Assert.True(bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 2).Alpha > 0);
        Assert.True(bitmap.GetPixel((int)panel.Left + 2, (int)panel.MidY).Alpha > 0);
    }

    [Fact]
    public void Bottom_buttons_fill_the_inner_width()
    {
        var close = LoadLayout.CloseButtonRect();
        var load = LoadLayout.LoadButtonRect();

        Assert.Equal(LoadLayout.Margin, close.X);
        Assert.Equal(LoadLayout.PanelWidth - LoadLayout.Margin, load.X + load.W);
    }
}
