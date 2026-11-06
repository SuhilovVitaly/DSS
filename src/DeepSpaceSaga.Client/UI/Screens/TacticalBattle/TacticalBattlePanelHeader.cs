using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public enum TacticalBattlePresentation { Current, SalvoFlight, SalvoResults }

internal sealed record TacticalBattlePanelHeader(string Title, string Phase, SKColor PhaseColor, string Losses,
    string Seed, string Outcome, string Reason, SKColor OutcomeColor, string Status, bool ShowNew)
{
    internal string PhaseRole { get; init; } = "panel.phase";
    internal string OutcomeRole { get; init; } = "panel.outcome";
    internal string StatusRole { get; init; } = "panel.status";

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
        string outcome = "", reason = "", outcomeRole = "panel.outcome";
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
            outcomeRole += "." + outcomeKey["Outcome.".Length..].ToLowerInvariant();
            reason = string.Join(" · ", result.Reasons.Select(r => Format("OutcomeReason." + r.Kind,
                r.Cap is { } cap ? BattleFormat.Number(cap) : r.CarrierId ?? "")));
        }
        return new(battle.ScenarioName + " · " + L("Turn") + " " + BattleFormat.Number(turn),
            L(phase), phaseColor, Format("LossLine", BattleFormat.Number(ownLosses), BattleFormat.Number(enemyLosses)),
            L("Seed") + " " + BattleFormat.Seed(battle.Seed), outcome, reason, outcomeColor,
            frame.Status.Length > 0 ? frame.Status.Trim() : frame.Pending ? L("Waiting") : "",
            !flight && battle.Phase == TacticalBattlePhase.Over)
        {
            PhaseRole = "panel.phase." + phase["Phase.".Length..].ToLowerInvariant(),
            OutcomeRole = outcomeRole,
            StatusRole = frame.Pending ? "panel.status.pending" : "panel.status.error",
        };
    }

    internal float Draw(SKCanvas canvas, SKRect content, float scale)
    {
        using var layout = new SKPaint { Typeface = MenuStyle.TypefaceRegular, TextSize = 14 * scale };
        using var marker = new SKPaint { IsAntialias = true };
        float y = content.Top + 12 * scale;
        DrawLines(Title, 2, "panel.summary.title");
        DrawMarker(new SKPoint(content.Left + 5 * scale, y + 9 * scale), PhaseRole + ".marker", PhaseColor);
        ControlVisuals.Label(canvas, Phase, new(content.Left + 17 * scale, y, content.Right, y + 19 * scale), true, scale,
            role: PhaseRole);
        y += 23 * scale;
        DrawLines(Losses + " · " + Seed, 2, "panel.summary.losses");
        if (Outcome.Length > 0)
        {
            y += 3 * scale;
            var banner = SKRect.Create(content.Left, y, content.Width, 29 * scale);
            SkinStyle.Box(canvas, OutcomeRole, banner, OutcomeColor.WithAlpha(48), null, scale);
            string accent = OutcomeRole + ".accent";
            marker.Color = SkinStyle.Color(accent, "fill", OutcomeColor);
            canvas.DrawRect(content.Left, y, Math.Clamp(SkinStyle.Number(accent, "size", 3), 1, 8) * scale, banner.Height, marker);
            ControlVisuals.Label(canvas, Outcome, new(content.Left + 10 * scale, y, content.Right, banner.Bottom), true, scale,
                role: OutcomeRole);
            y = banner.Bottom + 3 * scale;
            DrawLines(Reason, 2, OutcomeRole + ".reason");
        }
        if (Status.Length > 0) { y += 3 * scale; DrawLines(Status, 3, StatusRole); }
        return y + 9 * scale;

        void DrawMarker(SKPoint center, string role, SKColor fallback)
        {
            float size = Math.Clamp(SkinStyle.Number(role, "size", 8), 2, 15) * scale;
            var bounds = SKRect.Create(center.X - size / 2, center.Y - size / 2, size, size);
            if (SkinStyle.Image(canvas, role, bounds)) return;
            marker.Color = SkinStyle.Color(role, "fill", fallback);
            canvas.DrawCircle(center, size / 2, marker);
        }

        void DrawLines(string text, int maximumLines, string role)
        {
            if (text.Length == 0) return;
            int rows = Math.Min(maximumLines, ControlText.Wrap(text, content.Width, layout).Length);
            using var styled = ControlText.Paint(scale, role, text);
            styled.TextSize = Math.Min(styled.TextSize, 17 * scale);
            var lines = ControlText.Wrap(text, content.Width, styled);
            for (int i = 0; i < rows; i++)
            {
                if (i < lines.Length)
                    ControlVisuals.DrawLabel(canvas, lines[i], new(content.Left, y, content.Right, y + 19 * scale), styled);
                y += 19 * scale;
            }
        }
    }

    private static string L(string key) => BattleFormat.Localize(key);
    private static string Format(string key, params object[] args) =>
        BattleFormat.Text(string.Format(CultureInfo.InvariantCulture, L(key), args));
}
