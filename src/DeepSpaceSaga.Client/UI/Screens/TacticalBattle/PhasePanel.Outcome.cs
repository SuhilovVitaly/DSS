using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    partial void DrawOutcome(SKCanvas c)
    {
        var b = _frame.Battle; if (b.Outcome is not { } outcome) return;
        Head(c, b.ScenarioName, "OutcomeTag");
        bool victory = outcome.Kind.ToString() == b.PlayerSide;
        string title = U(outcome.Kind switch { TacticalBattleOutcomeCode.Draw => "Draw", TacticalBattleOutcomeCode.Timeout => "Timeout", _ => victory ? "Victory" : "Defeat" });
        T(c, title, 1050, 140, 22, victory ? Yellow : Red, true,
            role: victory ? "ui.outcome.victory" : "ui.outcome.defeat");
        string reason = string.Join("; ", outcome.Reasons.Select(r => r.Kind switch
        { TacticalOutcomeReasonCode.CarrierDestroyed => U("CarrierDestroyed", r.CarrierId), TacticalOutcomeReasonCode.NoDrones => U(victory ? "EnemyNoDrones" : "OwnNoDrones"), _ => U("TurnLimit", r.Cap) }));
        T(c, reason, 1050, 158, FitSize(reason, 12, 214), Text); Line(c, 1050, 1264, 167);
        int Loss(bool own) => outcome.Losses.Where(l => (l.Side == b.PlayerSide) == own).Sum(l => l.Count);
        string[] keys = ["Turns", "Shots", "Hits", "Losses"];
        string[] values = [outcome.Turns.ToString(), outcome.Statistics.Shots.ToString(), outcome.Statistics.Hits.ToString(), U("LossValue", Loss(true), Loss(false))];
        for (int i = 0; i < keys.Length; i++)
        { T(c, U(keys[i]), 1050, 185 + 18 * i, 12, Muted); T(c, values[i], 1264, 185 + 18 * i, 12, Text, true, SKTextAlign.Right); }
        float y = _db + 28;
        T(c, U("BattleOver"), 1050, y + 20, 14, Text, true); BoxText(c, new(1048, y + 28, 1266, y + 48), U("ExitHelp"));
        ButtonAt(c, "new", new(1048, y + 56, 1132, _db + 112), U("NewBattle")); ButtonAt(c, "primary", new(1138, y + 56, 1266, _db + 112), U("Exit"), true);
    }
}
