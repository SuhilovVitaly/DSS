using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardFireBlockTests
{
    [Fact]
    public async Task Fire_commands_and_defense_are_in_the_expanded_drone_card()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        using var locale = Localization.UseLocaleForTests("Russian");
        using var image = scene.Render();
        var screen = scene.Screen;
        Assert.Equal(new SKRect(6, 93, 196, 317), screen.Layout.Rects["pilot:Юн"]);
        Assert.Equal(new SKRect(18, 292.5f, 285, 319.5f), screen.CardCommandBounds("target:Таг-5"));
        Assert.Equal(new SKRect(18, 322.5f, 285, 349.5f), screen.CardCommandBounds("target:Таг-6"));
        Assert.Equal(new SKRect(18, 391.5f, 285, 418.5f), screen.CardCommandBounds("decoy"));
        Assert.Equal(new SKRect(154.5f, 424.5f, 285, 450), screen.CardCommandBounds("evade"));
        Assert.Equal(5, screen.Layout.Rects.Keys.Count(key => key.StartsWith("card-command:", StringComparison.Ordinal)));
        Assert.Contains(screen.Layout.Texts, item => item.Text == "Таг-6: броня 20/20, затем корпус 40/40");
    }

    [Theory]
    [InlineData("2-evasion-plan", "EvadeNoFire", false)]
    [InlineData("2-no-target", "NoTargetsInArc", true)]
    [InlineData("2-anomaly-dust", "DroneInDust", true)]
    [InlineData("2-decoy-empty", "DecoyEmpty", false)]
    public async Task Fire_state_exposes_eligible_commands(string name, string text, bool canDecoy)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var image = scene.Render();
        Assert.Contains(scene.Screen.Layout.Texts, item => item.Text.Contains(BattleDesign.M(text), StringComparison.Ordinal));
        Assert.Equal(canDecoy, !scene.Screen.CardCommandBounds("decoy").IsEmpty);
    }

    [Fact]
    public async Task Absorption_is_displayed_in_the_target_row()
    {
        await using var scene = await Ep0019Scenes.Create("2-anomaly-absorption");
        using var image = scene.Render();
        Assert.Contains(scene.Screen.Layout.Texts, item => item.Text == BattleDesign.M("Card.Absorption", 36) && item.Color == BattleDesign.Heat);
    }
}
