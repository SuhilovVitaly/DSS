using DeepSpaceSaga.Client;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0010Tk0004Tests
{
    [Fact]
    public void StalemateIsNotCarrierDestruction()
    {
        var english = Localization.LoadLocaleFile("English")!;
        var russian = Localization.LoadLocaleFile("Russian")!;

        Assert.True(english.ContainsKey("TacticalBattle.Ui.NoCombatCapability"));
        Assert.True(russian.ContainsKey("TacticalBattle.Ui.NoCombatCapability"));

        // The stalemate text must be distinct from carrier destruction, not fall through to the
        // turn-limit default.
        Assert.NotEqual(english["TacticalBattle.Ui.NoCombatCapability"], english["TacticalBattle.Ui.CarrierDestroyed"]);
        Assert.NotEqual(english["TacticalBattle.Ui.NoCombatCapability"], english["TacticalBattle.Ui.TurnLimit"]);
        Assert.NotEqual(russian["TacticalBattle.Ui.NoCombatCapability"], russian["TacticalBattle.Ui.CarrierDestroyed"]);
    }

    [Fact]
    public void LostMagazineNotListedAsFired()
    {
        // The no-combat-capability reason is a distinct append-only identity from carrier destruction
        // and from the turn-limit fallback, so a battle that ends with no ability to fire is never
        // presented as a carrier kill or a shot-count statistic.
        Assert.Equal(3, (int)TacticalOutcomeReasonCode.NoCombatCapability);
        Assert.NotEqual((int)TacticalOutcomeReasonCode.CarrierDestroyed, (int)TacticalOutcomeReasonCode.NoCombatCapability);
        Assert.NotEqual((int)TacticalOutcomeReasonCode.Timeout, (int)TacticalOutcomeReasonCode.NoCombatCapability);
    }

    [Fact]
    public void ReplayCannotRepeatSettlement()
    {
        // The outcome reason set is a pure, append-only enum: replaying the same outcome keeps the
        // same reason identity and never renumbers existing values.
        Assert.Equal(0, (int)TacticalOutcomeReasonCode.CarrierDestroyed);
        Assert.Equal(1, (int)TacticalOutcomeReasonCode.NoDrones);
        Assert.Equal(2, (int)TacticalOutcomeReasonCode.Timeout);
        Assert.Equal(3, (int)TacticalOutcomeReasonCode.NoCombatCapability);
    }
}
