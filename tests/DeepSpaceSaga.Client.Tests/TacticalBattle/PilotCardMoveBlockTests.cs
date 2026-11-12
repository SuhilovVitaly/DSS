using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;
using SkiaSharp;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardMoveBlockTests
{
    [Fact]
    public async Task Slim_commands_fit_panel_and_slider_uses_the_visible_track()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover", withoutAnomalies: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        try
        {
            screen.Render(canvas, 1280, 720);
            var panel = screen.Layout.Rects["pilot-panel:" + screen.Movement.Selected];
            foreach (string action in new[] { "speed-", "speed+", "turn-", "turn+", "slider:speed", "slider:turn", "straight", "ai" })
                Assert.True(panel.Contains(screen.CardCommandBounds(action)), action);
            double before = screen.Movement.SelectedOrder!.Order.Speed;
            var plus = screen.CardCommandBounds("speed+");
            Assert.Equal(SKRect.Create(184, 54, 11, 11), plus);
            screen.OnMouseDown(plus.MidX, plus.MidY, MouseButton.Left); screen.OnMouseUp(plus.MidX, plus.MidY, MouseButton.Left);
            Assert.Equal(Math.Min(before + 2, screen.Movement.SelectedOrder.MaximumSpeed), Assert.Single(screen.BuildMoveRequest().Orders).Speed);
            var slider = screen.CardCommandBounds("slider:speed");
            screen.OnMouseDown(slider.MidX, slider.MidY, MouseButton.Left);
            screen.OnMouseMove(slider.Right - 3, slider.MidY); screen.OnMouseUp(slider.Right - 3, slider.MidY, MouseButton.Left);
            Assert.Equal(screen.Movement.SelectedOrder.MaximumSpeed, Assert.Single(screen.BuildMoveRequest().Orders).Speed);
            screen.OnMouseDown(slider.Left + 3, slider.MidY, MouseButton.Left); screen.OnMouseUp(slider.Left + 3, slider.MidY, MouseButton.Left);
            Assert.Equal(screen.Movement.SelectedOrder.MinimumSpeed, Assert.Single(screen.BuildMoveRequest().Orders).Speed);
            var ai = screen.CardCommandBounds("ai");
            screen.OnMouseDown(ai.MidX, ai.MidY, MouseButton.Left); screen.OnMouseUp(ai.MidX, ai.MidY, MouseButton.Left);
            Assert.Equal(screen.Movement.SelectedOrder.Suggestion, Assert.Single(screen.BuildMoveRequest().Orders));
            if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } folder)
            {
                using var crop = new SKBitmap(630, 1440); using var cc = new SKCanvas(crop); cc.Scale(2);
                screen.Render(cc, 1920, 1080);
                using var png = crop.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(Path.Combine(folder, "move-panel-column-x2.png")); png.SaveTo(file);
            }
        }
        finally { screen.OnDeactivated(); }
    }

    [Fact]
    public async Task Commands_use_the_selected_drone_and_match_keyboard_edits()
    {
        await using var card = await Ep0019Scenes.Create("1-move-no-hover");
        await using var keys = await Ep0019Scenes.Create("1-move-no-hover");
        using var image = card.Render();
        var screen = card.Screen;
        Assert.Equal(new SKRect(262.5f, 141, 285, 160.5f), screen.CardCommandBounds("speed+"));
        Assert.Equal(new SKRect(154.5f, 261, 285, 285), screen.CardCommandBounds("ai"));
        Click("speed+");
        Click("turn+");
        keys.Screen.OnKeyDown(Key.Up);
        keys.Screen.OnKeyDown(Key.Right);
        Assert.Equal(Assert.Single(keys.Screen.BuildMoveRequest().Orders), Assert.Single(screen.BuildMoveRequest().Orders));

        Click("straight");
        Assert.Equal(0, Assert.Single(screen.BuildMoveRequest().Orders).Turn);
        Click("ai");
        Assert.Equal(screen.Movement.SelectedOrder!.Suggestion, Assert.Single(screen.BuildMoveRequest().Orders));

        var speed = screen.CardCommandBounds("slider:speed");
        screen.OnMouseDown(speed.MidX, speed.MidY, MouseButton.Left);
        Assert.True(screen.OnMouseMove(speed.Right + 100, speed.MidY));
        screen.OnMouseUp(speed.Right + 100, speed.MidY, MouseButton.Left);
        Assert.Equal(screen.Movement.SelectedOrder!.MaximumSpeed, Assert.Single(screen.BuildMoveRequest().Orders).Speed);
        screen.OnKeyDown(Key.Tab);
        var before = screen.BuildMoveRequest().Orders;
        Click("speed-");
        Assert.Equal(before.ToArray(), screen.BuildMoveRequest().Orders.ToArray());

        void Click(string action)
        {
            var r = screen.CardCommandBounds(action);
            Assert.False(r.IsEmpty);
            screen.OnMouseDown(r.MidX, r.MidY, MouseButton.Left);
            screen.OnMouseUp(r.MidX, r.MidY, MouseButton.Left);
        }
    }
}
