using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.Save;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class SaveScreenVisualTests
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
        var screen = new SaveScreen(() => slots, _ => { }, _ => { });

        screen.Render(canvas, bitmap.Width, bitmap.Height);
        canvas.Flush();

        var panel = SaveLayout.PanelRect(bitmap.Width, bitmap.Height);
        Assert.True(GenericWindowTypeA.HasAssets);
        Assert.True(GenericButtonTypeA.HasAssets);
        Assert.True(bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 2).Alpha > 0);
        Assert.True(bitmap.GetPixel((int)panel.Left + 2, (int)panel.MidY).Alpha > 0);
    }

    [Fact]
    public void Main_actions_fill_one_bottom_row()
    {
        var close = SaveLayout.CloseButtonRect();
        var delete = SaveLayout.DeleteButtonRect();
        var overwrite = SaveLayout.OverwriteButtonRect();
        var newSave = SaveLayout.NewSaveButtonRect();

        Assert.Equal(close.Y, delete.Y);
        Assert.Equal(close.Y, overwrite.Y);
        Assert.Equal(close.Y, newSave.Y);
        Assert.Equal(SaveLayout.BottomButtonsX, close.X);
        Assert.Equal(
            SaveLayout.PanelWidth - SaveLayout.BottomButtonsX,
            newSave.X + newSave.W);
    }

    [Fact]
    public void New_save_actions_fill_one_centered_bottom_row()
    {
        var close = SaveLayout.CloseButtonRect(isNewSaveActive: true);
        var cancel = SaveLayout.CancelButtonRect();
        var save = SaveLayout.ConfirmButtonRect();

        Assert.Equal(close.Y, cancel.Y);
        Assert.Equal(close.Y, save.Y);
        Assert.Equal(close.X, SaveLayout.PanelWidth - (save.X + save.W));
    }
}
