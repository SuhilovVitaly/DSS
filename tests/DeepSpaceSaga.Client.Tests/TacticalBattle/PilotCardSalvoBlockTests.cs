using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardSalvoBlockTests
{
    [Theory]
    [InlineData("3-salvo-flight", 51)]
    [InlineData("4-salvo-results", 71)]
    public async Task Slim_panel_shows_flight_or_confirmed_result_without_commands(string sceneName, float height)
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var scene = await Ep0019Scenes.Create(sceneName);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(630, 1440); using var c = new SKCanvas(bitmap); c.Scale(3);
        var frame = scene.Frame;
        var column = new PilotCardColumn(); column.Arrange(frame, PilotCardModel.Project(frame));
        var layout = new BattleLayout(); BattleDesign.Recording = layout;
        try { column.Draw(c, PilotPortraitCatalog.Default); }
        finally { BattleDesign.Recording = null; }
        Assert.Equal(height, column.Layout.Panel.Height);
        Assert.DoesNotContain(layout.Rects.Keys, k => k.StartsWith("card-command:", StringComparison.Ordinal));
        if (frame.Salvo!.InFlight)
        {
            Assert.Contains(layout.Texts, t => t.Text == "ОГОНЬ");
            var flight = frame.Salvo.Flights.Single(f => f.Shot.ShooterId == PilotCardModel.ActiveShooter(frame));
            Assert.Equal(138 * flight.Progress, layout.Rects["pilot-flight-progress"].Width, .001);
            Assert.DoesNotContain(layout.Texts, t => t.Text == "ПРОМАХ");
        }
        else
        {
            var shot = PilotCardModel.ResultShot(frame)!;
            Assert.True(shot.Decoyed); Assert.False(shot.Hit);
            Assert.Contains(layout.Texts, t => t.Text == "ПРОМАХ" && t.Color == SKColor.Parse("#C8D4EC"));
            Assert.Contains(layout.Texts, t => t.Text == BattleDesign.M("Panel.ResultLine", shot.TargetId, BattleDesign.Pct(shot.Chance), 0));
            Assert.Contains(layout.Texts, t => t.Text.StartsWith("ловушка цели:", StringComparison.Ordinal));
            Assert.DoesNotContain("pilot-damage-armor:Юн", layout.Rects.Keys);
        }
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } folder)
        {
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(folder, "salvo-panel-" + sceneName + "-column-x2.png")); png.SaveTo(file);
        }
    }

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
