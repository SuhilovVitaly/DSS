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
            foreach (string text in new[] { "ПОБЕДА", "носитель Логово уничтожен", "21", "40", "20", "свои 1 · враг 5" }) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
            var start = s.Screen.Layout.Rects["button:new"];
            var exit = s.Screen.Layout.Rects["button:primary"];
            Assert.Equal(ScreenEvent.NewTacticalBattle, s.Screen.OnMouseDown(start.MidX * 1.5f, start.MidY * 1.5f, MouseButton.Left));
            Assert.Equal(ScreenEvent.CloseTacticalBattle, s.Screen.OnMouseDown(exit.MidX * 1.5f, exit.MidY * 1.5f, MouseButton.Left));
        }
        await s.Fixture.Route(ScreenEvent.CloseTacticalBattle);
        Assert.Same(s.Fixture.Game, s.Fixture.Screens.Current);
        Assert.Null(s.Fixture.Engine.GetTacticalBattleState());
    }
}
