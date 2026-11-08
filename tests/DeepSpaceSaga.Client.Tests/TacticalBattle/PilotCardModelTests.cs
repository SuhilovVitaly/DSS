using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardModelTests
{
    [Fact]
    public async Task Mind_comes_from_snapshot()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        using var locale = Localization.UseLocaleForTests("Russian");
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var own = frame.Battle.Units.First(u => u.Side == frame.Battle.PlayerSide);
        frame = frame with { Battle = frame.Battle with
        { Units = [.. frame.Battle.Units.Select(u => u.Id == own.Id ? u with { Consciousness = 9 } : u)] } };
        var stat = PilotCardModel.Project(frame).Single(c => c.UnitId == own.Id).Stats[2];
        Assert.Equal("9", stat.Value);
        Assert.Equal(0.09, stat.Fill);
        Assert.Equal(BattleDesign.Red, stat.ValueTone);
        Assert.Equal(BattleDesign.Green, stat.Tone);
    }

    [Theory]
    [InlineData("1-move", "Вера", "Движение: скорость 77,5 · поворот +5,5°", "ИИ")]
    [InlineData("2-fire", "Марта", "Стрельба: целей 1 · урон 50–75 · иниц. 59", "Таг-4 56%")]
    [InlineData("4-salvo-results", "Марта", "Итог: нанесено 0 · получено −41", "промах")]
    [InlineData("5-outcome", "Вера", "Итог: нанесено 267 · получено −46", "в строю")]
    public async Task Cards_match_reference_values(string scene, string name, string text, string chip)
    {

        await using var s = await Ep0019Scenes.Create(scene); using var locale = Localization.UseLocaleForTests("Russian");
        var cards = PilotCardModel.Project(s.Screen.CaptureFrame(1920, 1080)!);
        var c = cards.Single(c => c.Name == name);
        Assert.Equal(text, c.PhaseText); Assert.Equal(chip, c.Chip);
        var battle = s.Screen.CaptureFrame(1920, 1080)!.Battle;
        Assert.All(cards, card =>
        {
            int mind = battle.Units.Single(u => u.Id == card.UnitId).Consciousness!.Value;
            Assert.Equal(mind.ToString(), card.Stats[2].Value);
            Assert.Equal(mind / 100.0, card.Stats[2].Fill);
        });
    }
}
