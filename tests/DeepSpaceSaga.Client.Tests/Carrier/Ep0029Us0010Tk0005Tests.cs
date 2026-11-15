using DeepSpaceSaga.Client;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0010Tk0005Tests
{
    private static readonly string[] BattleAmmoKeys =
    [
        "TacticalBattle.Ui.Ammo", "TacticalBattle.Ui.Armor", "TacticalBattle.Ui.Hull", "TacticalBattle.Ui.Mind",
        "TacticalBattle.Ui.NotFiredFemale", "TacticalBattle.Ui.NotFiredMale",
        "TacticalBattle.Ui.NoCombatCapability", "TacticalBattle.Ui.CarrierDestroyed", "TacticalBattle.Ui.TurnLimit",
    ];

    private static readonly string[] FemalePilotNames = ["Evelyn Ward", "Audrey Bennett", "Claire Morgan", "Rachel Hayes"];

    [Fact]
    public void BattleAmmoLocaleParity()
    {
        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;
        foreach (var key in BattleAmmoKeys)
        {
            Assert.True(english.TryGetValue(key, out var en), $"English missing '{key}'");
            Assert.True(russian.TryGetValue(key, out var ru), $"Russian missing '{key}'");
            Assert.False(string.IsNullOrWhiteSpace(en));
            Assert.False(string.IsNullOrWhiteSpace(ru));
        }
    }

    [Fact]
    public void FemaleNamesFitCard()
    {
        Assert.Equal(4, FemalePilotNames.Length);
        Assert.Equal(4, FemalePilotNames.Distinct(StringComparer.Ordinal).Count());

        using var paint = new SKPaint { TextSize = 12 };
        const float cardWidth = 150f;
        foreach (var name in FemalePilotNames)
        {
            float width = paint.MeasureText(name);
            Assert.True(width <= cardWidth, $"{name} is {width}px wide, exceeding {cardWidth}px");
        }
    }

    [Fact]
    public void NewEnumsHaveTranslation()
    {
        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;
        foreach (var key in new[] { "TacticalBattle.Ui.NoCombatCapability", "TacticalBattle.Ui.Ammo" })
        {
            Assert.True(english.ContainsKey(key));
            Assert.True(russian.ContainsKey(key));
            Assert.DoesNotContain("NoCombatCapability", english[key], StringComparison.Ordinal);
            Assert.DoesNotContain("NoCombatCapability", russian[key], StringComparison.Ordinal);
        }
    }
}
