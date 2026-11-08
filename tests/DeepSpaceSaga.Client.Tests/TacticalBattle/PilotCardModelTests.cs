using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardModelTests
{
    [Theory]
    [InlineData("1-move", "Вера", "Движение: скорость 77,5 · поворот +5,5°", "ИИ")]
    [InlineData("2-fire", "Марта", "Стрельба: целей 1 · урон 50–75 · иниц. 59", "Таг-4 56%")]
    [InlineData("4-salvo-results", "Марта", "Итог: нанесено 0 · получено −41", "промах")]
    [InlineData("5-outcome", "Вера", "Итог: нанесено 73 · получено −102", "выведена из строя")]
    public async Task Cards_match_reference_values(string scene, string name, string text, string chip)
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var s = await Ep0019Scenes.Create(scene);
        var cards = PilotCardModel.Project(s.Screen.CaptureFrame(1920, 1080)!);
        var c = cards.Single(c => c.Name == name);
        Assert.Equal(text, c.PhaseText); Assert.Equal(chip, c.Chip);
        Assert.All(cards, card => { Assert.Equal("100", card.Stats[2].Value); Assert.Equal(1, card.Stats[2].Fill); });
    }
}
