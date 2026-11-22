using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    /// <summary>Tutorial outcome offers no new battle; its only button returns to the map.</summary>
    internal bool Tutorial { get; set; }
    internal bool Campaign { get; set; }
    internal bool RequireBattleResults { get; set; }
    internal static string OutcomeTitle(TacticalBattleSnapshot battle, bool campaign) =>
        battle.Outcome!.Kind.ToString() == battle.PlayerSide ? "Victory" : campaign ? "Defeat" :
        battle.Outcome.Kind switch { TacticalBattleOutcomeCode.Draw => "Draw", TacticalBattleOutcomeCode.Timeout => "Timeout", _ => "Defeat" };

    partial void DrawOutcome(SKCanvas c)
    {
        var b = _frame.Battle; if (b.Outcome is not { } outcome) return;
        Head(c, b.ScenarioName, "OutcomeTag");
        bool victory = outcome.Kind.ToString() == b.PlayerSide;
        string title = U(OutcomeTitle(b, Campaign && !RequireBattleResults));
        T(c, title, 1050, 140, 22, victory ? Yellow : Red, true,
            role: victory ? "ui.outcome.victory" : "ui.outcome.defeat");
        string reason = string.Join("; ", outcome.Reasons.Select(r => r.Kind switch
        { TacticalOutcomeReasonCode.CarrierDestroyed => U("CarrierDestroyed", r.CarrierId), TacticalOutcomeReasonCode.NoDrones => U(victory ? "EnemyNoDrones" : "OwnNoDrones"), TacticalOutcomeReasonCode.NoCombatCapability => U("NoCombatCapability"), _ => U("TurnLimit", r.Cap) }));
        T(c, reason, 1050, 158, FitSize(reason, 12, 214), Text); Line(c, 1050, 1264, 167);
        int Loss(bool own) => outcome.Losses.Where(l => (l.Side == b.PlayerSide) == own).Sum(l => l.Count);
        string[] keys = ["Turns", "Shots", "Hits", "Losses"];
        string[] values = [outcome.Turns.ToString(), outcome.Statistics.Shots.ToString(), outcome.Statistics.Hits.ToString(), U("LossValue", Loss(true), Loss(false))];
        for (int i = 0; i < keys.Length; i++)
        { T(c, U(keys[i]), 1050, 185 + 18 * i, 12, Muted); T(c, values[i], 1264, 185 + 18 * i, 12, Text, true, SKTextAlign.Right); }
        float carrierY = 270;
        foreach (var carrier in b.Units.Where(u => u.Kind == TacticalUnitKind.Carrier))
        {
            string row = PilotCardModel.Name(carrier) + "  " + carrier.Hull + "/" + carrier.MaxHull;
            T(c, row, 1050, carrierY, FitSize(row, 12, 214), carrier.Hull <= 0 ? Red : Text);
            carrierY += 18;
        }
        float y = _db + 28;
        if (RequireBattleResults)
        {
            T(c, U("BattleOver"), 1050, y + 20, 14, Text, true);
            ButtonAt(c, "primary", new(1048, y + 56, 1266, _db + 112), Localization.Get("TacticalBattle.Results"), true);
            return;
        }
        T(c, U("BattleOver"), 1050, y + 20, 14, Text, true); BoxText(c, new(1048, y + 28, 1266, y + 48), U("ExitHelp"));
        if (Tutorial || Campaign)
        {
            ButtonAt(c, "primary", new(1048, y + 56, 1266, _db + 112), Localization.Get("TacticalBattle.ToMap"), true);
            return;
        }
        ButtonAt(c, "new", new(1048, y + 56, 1132, _db + 112), U("NewBattle")); ButtonAt(c, "primary", new(1138, y + 56, 1266, _db + 112), U("Exit"), true);
    }
}
