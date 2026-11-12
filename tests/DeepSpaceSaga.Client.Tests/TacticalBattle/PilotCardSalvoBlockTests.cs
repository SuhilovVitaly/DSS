using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardSalvoBlockTests
{
    [Fact]
    public async Task Result_card_shows_shooter_and_read_only_details()
    {
        await using var scene = await Ep0019Scenes.Create("4-salvo-results");
        using var locale = Localization.UseLocaleForTests("Russian");
        using var image = scene.Render();
        var layout = scene.Screen.Layout;
        Assert.Equal(new SKRect(6, 180, 196, 302), layout.Rects["pilot:Марта"]);
        Assert.Equal(new SKRect(6, 307, 196, 389), layout.Rects["pilot:Олег"]);
        var shot = PilotCardModel.ResultShot(scene.Frame)!;
        Assert.Equal("Таг-1", shot.TargetId);
        Assert.True(shot.Decoyed);
        Assert.Contains(layout.Texts, item => item.Text == "→ Таг-1 · " + BattleDesign.Pct(shot.Chance));
        Assert.Contains(layout.Texts, item => item.Text == "промах");
        Assert.Contains(layout.Texts, item => item.Text == "урон 0");
        Assert.DoesNotContain(layout.Rects.Keys, key => key.StartsWith("card-command:", StringComparison.Ordinal));

        var before = scene.Screen.Flow.State;
        scene.Screen.OnMouseDown(50, 290 * 1.5f, MouseButton.Left);
        Assert.Same(before, scene.Screen.Flow.State);
        Assert.True(scene.Screen.HasSalvoPresentation);
    }

    [Fact]
    public async Task Own_active_shooter_expands_during_flight_without_result_details()
    {
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight");
        using var image = scene.Render();
        var active = PilotCardModel.ActiveShooter(scene.Frame)!;
        Assert.Equal(108, scene.Screen.Layout.Rects["pilot:" + active].Height);
        Assert.DoesNotContain(scene.Screen.Layout.Rects.Keys, key => key.StartsWith("card-command:", StringComparison.Ordinal));
        Assert.DoesNotContain(scene.Screen.Layout.Texts, item => item.Text == BattleDesign.M("Card.Dealt", 0));
    }
}
