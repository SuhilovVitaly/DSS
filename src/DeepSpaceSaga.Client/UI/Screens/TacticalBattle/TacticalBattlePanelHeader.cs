using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public enum TacticalBattlePresentation { Current, SalvoFlight, SalvoResults }

internal sealed record TacticalBattlePanelHeader(string Title, string Phase, SKColor PhaseColor, string Losses,
    string Seed, string Outcome, string Reason, SKColor OutcomeColor, string Status, bool ShowNew)
{
    internal static TacticalBattlePanelHeader Capture(TacticalBattleFrame frame)
    {
        var battle = frame.Battle;
        bool flight = frame.Presentation == TacticalBattlePresentation.SalvoFlight;
        bool salvo = frame.Presentation != TacticalBattlePresentation.Current;
        int turn = salvo && !battle.Shots.IsDefaultOrEmpty ? battle.Shots[0].Turn : battle.Turn;
        bool IsOwn(string side) => side == battle.PlayerSide;
        int ownLosses, enemyLosses;
        if (flight && !battle.BeforeSalvoUnits.IsDefaultOrEmpty)
        {
            var lost = battle.BeforeSalvoUnits.Where(u => u.Kind == TacticalUnitKind.Drone &&
                u.State is TacticalUnitState.Disabled or TacticalUnitState.Destroyed);
            ownLosses = lost.Count(u => IsOwn(u.Side)); enemyLosses = lost.Count(u => !IsOwn(u.Side));
        }
        else
        {
            ownLosses = battle.Losses.Where(l => IsOwn(l.Side)).Sum(l => l.Count);
            enemyLosses = battle.Losses.Where(l => !IsOwn(l.Side)).Sum(l => l.Count);
        }
        string phase = salvo ? "Phase.Results" : "Phase." + battle.Phase;
        SKColor phaseColor = salvo ? new(242, 188, 91) : battle.Phase switch
        {
            TacticalBattlePhase.Move => new(100, 196, 251),
            TacticalBattlePhase.Fire => new(240, 154, 83),
            _ => new(173, 182, 197)
        };
        string outcome = "", reason = "";
        SKColor outcomeColor = new(173, 182, 197);
        if (!flight && battle.Outcome is { } result)
        {
            string outcomeKey;
            if (result.Kind is TacticalBattleOutcomeCode.A or TacticalBattleOutcomeCode.B)
            {
                bool victory = IsOwn(result.Kind.ToString());
                outcomeKey = victory ? "Outcome.Victory" : "Outcome.Defeat";
                outcomeColor = victory ? new(47, 191, 143) : new(232, 98, 58);
            }
            else outcomeKey = "Outcome." + result.Kind;
            outcome = L(outcomeKey);
            reason = string.Join(" · ", result.Reasons.Select(r => Format("OutcomeReason." + r.Kind,
                r.Cap is { } cap ? BattleFormat.Number(cap) : r.CarrierId ?? "")));
        }
        return new(battle.ScenarioName + " · " + L("Turn") + " " + BattleFormat.Number(turn),
            L(phase), phaseColor, Format("LossLine", BattleFormat.Number(ownLosses), BattleFormat.Number(enemyLosses)),
            L("Seed") + " " + BattleFormat.Seed(battle.Seed), outcome, reason, outcomeColor,
            frame.Status.Length > 0 ? frame.Status.Trim() : frame.Pending ? L("Waiting") : "",
            !flight && battle.Phase == TacticalBattlePhase.Over);
    }

    internal float Draw(SKCanvas canvas, SKRect content, float scale)
    {
        using var paint = ControlText.Paint(scale);
        using var marker = new SKPaint { Color = PhaseColor, IsAntialias = true };
        float y = content.Top + 12 * scale;
        DrawLines(Title, 2);
        canvas.DrawCircle(content.Left + 5 * scale, y + 9 * scale, 4 * scale, marker);
        ControlVisuals.Label(canvas, Phase, new(content.Left + 17 * scale, y, content.Right, y + 19 * scale), true, scale);
        y += 23 * scale;
        DrawLines(Losses + " · " + Seed, 2);
        if (Outcome.Length > 0)
        {
            y += 3 * scale;
            var banner = SKRect.Create(content.Left, y, content.Width, 29 * scale);
            marker.Color = OutcomeColor.WithAlpha(48);
            canvas.DrawRect(banner, marker);
            marker.Color = OutcomeColor;
            canvas.DrawRect(content.Left, y, 3 * scale, banner.Height, marker);
            ControlVisuals.Label(canvas, Outcome, new(content.Left + 10 * scale, y, content.Right, banner.Bottom), true, scale);
            y = banner.Bottom + 3 * scale;
            DrawLines(Reason, 2);
        }
        if (Status.Length > 0) { y += 3 * scale; DrawLines(Status, 3); }
        return y + 9 * scale;

        void DrawLines(string text, int maximumLines)
        {
            if (text.Length == 0) return;
            var lines = ControlText.Wrap(text, content.Width, paint);
            foreach (var line in lines.Take(maximumLines))
            {
                ControlVisuals.Label(canvas, line, new(content.Left, y, content.Right, y + 19 * scale), true, scale);
                y += 19 * scale;
            }
        }
    }

    private static string L(string key) => BattleFormat.Localize(key);
    private static string Format(string key, params object[] args) =>
        BattleFormat.Text(string.Format(CultureInfo.InvariantCulture, L(key), args));
}
