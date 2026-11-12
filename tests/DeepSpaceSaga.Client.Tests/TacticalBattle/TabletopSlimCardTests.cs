using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopSlimCardTests
{
    [Theory]
    [InlineData("1-move-no-hover")]
    [InlineData("4-salvo-results")]
    public async Task Slim_cards_show_numeric_boxes_and_damage_without_old_phase_lines(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(630, 1440); using var canvas = new SKCanvas(bitmap); canvas.Scale(3);
        var frame = scene.Frame;
        var cards = PilotCardModel.Project(frame);
        var column = new PilotCardColumn(); column.Arrange(frame, cards);
        var layout = new BattleLayout(); BattleDesign.Recording = layout;
        try { column.Draw(canvas, PilotPortraitCatalog.Default); }
        finally { BattleDesign.Recording = null; }
        foreach (var card in cards)
        {
            var rect = layout.Rects["pilot:" + card.UnitId];
            Assert.Equal(150, rect.Width); Assert.Equal(40, rect.Height);
            var armor = layout.Rects["pilot-armor:" + card.UnitId];
            Assert.Equal(SKRect.Create(rect.Left + 41, rect.Top + 20, 24, 12), armor);
            Assert.Contains(layout.Texts, t => t.Text == card.Name);
            Assert.DoesNotContain(layout.Texts, t => t.Text == card.PhaseText && card.PhaseText.Length > 0);
            var damage = frame.Salvo is { InFlight: false } salvo ? salvo.Damage.FirstOrDefault(d => d.Unit.Id == card.UnitId) : null;
            Assert.Equal(damage?.Armor > 0, layout.Rects.ContainsKey("pilot-damage-armor:" + card.UnitId));
            Assert.Equal(damage?.Hull > 0, layout.Rects.ContainsKey("pilot-damage-hull:" + card.UnitId));
        }
        Assert.Contains(SKColor.Parse("#3A5A8A"), bitmap.Pixels);
        Assert.Contains(SKColor.Parse("#3F6A2C"), bitmap.Pixels);
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } folder)
        {
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(folder, "slim-" + name + "-x2.png")); png.SaveTo(file);
        }
    }

    [Fact]
    public async Task Destroyed_card_is_grey_and_clicking_a_live_card_selects_its_pilot()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover", withoutAnomalies: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        var frame = scene.Frame;
        var unit = frame.Battle.Units.First(u => u.Kind == TacticalUnitKind.Drone && u.Side == frame.Battle.PlayerSide);
        frame = frame with { Battle = frame.Battle with { Units = frame.Battle.Units.Replace(unit, unit with { State = TacticalUnitState.Destroyed, Armor = 0, Hull = 0 }) } };
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        var column = new PilotCardColumn(); column.Arrange(frame, PilotCardModel.Project(frame));
        column.Draw(canvas, PilotPortraitCatalog.Default);
        var color = bitmap.GetPixel(49, 28);
        Assert.Equal(color.Red, color.Green); Assert.Equal(color.Green, color.Blue); Assert.InRange(color.Red, 50, 65);
        var portrait = bitmap.GetPixel(22, 22);
        Assert.InRange(Math.Abs(portrait.Red - portrait.Green), 0, 1); Assert.InRange(Math.Abs(portrait.Green - portrait.Blue), 0, 1);
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        try
        {
            screen.Render(canvas, 1280, 720);
            var second = PilotCardModel.Project(screen.CaptureFrame(1280, 720)!)[1];
            var bounds = screen.Layout.Rects["pilot:" + second.UnitId];
            screen.OnMouseDown(bounds.MidX, bounds.MidY, MouseButton.Left);
            Assert.Equal(second.UnitId, screen.Movement.Selected);
        }
        finally { screen.OnDeactivated(); }
    }
}
