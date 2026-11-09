using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhaseOutcomeTests
{
    [Fact]
    public async Task Outcome_panel_and_navigation_match_reference()
    {
        await using var s = await Ep0019Scenes.Create("5-outcome");
        using (var locale = Localization.UseLocaleForTests("Russian"))
        {
            using var b = s.Render();
            foreach (string text in new[] { "ПОБЕДА", "у противника не осталось дронов", "23", "42", "20", "свои 0 · враг 6" }) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
            Assert.Equal(ScreenEvent.NewTacticalBattle, s.Screen.OnMouseDown(1080 * 1.5f, 337 * 1.5f, MouseButton.Left));
            Assert.Equal(ScreenEvent.CloseTacticalBattle, s.Screen.OnMouseDown(1200 * 1.5f, 337 * 1.5f, MouseButton.Left));
        }
        await s.Fixture.Route(ScreenEvent.CloseTacticalBattle);
        Assert.Same(s.Fixture.Game, s.Fixture.Screens.Current);
        Assert.Null(s.Fixture.Engine.GetTacticalBattleState());
    }
}
