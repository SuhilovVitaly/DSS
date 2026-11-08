using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhasePanelTests
{
    [Theory]
    [InlineData("1-move", 370)]
    [InlineData("2-fire", 370)]
    [InlineData("3-salvo-flight", 382)]
    [InlineData("4-salvo-results", 366)]
    [InlineData("5-outcome", 358)]
    public async Task Header_layout_and_probes(string name, float bottom)
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var s = await Ep0019Scenes.Create(name); using var b = s.Render();
        Assert.Equal(bottom, s.Screen.Layout.Rects["phase"].Bottom);
        Assert.Contains(s.Screen.Layout.Texts, t => t.Text.StartsWith("ХОД ", StringComparison.Ordinal));
        Assert.True(RasterProbe.Fraction(b, new(1046, 62, 1270, 66), BattleDesign.Panel) >= .9);
    }
}
