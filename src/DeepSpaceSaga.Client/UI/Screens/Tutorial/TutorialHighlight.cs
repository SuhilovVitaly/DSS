using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// "Show, don't tell": resolves a step anchor against the <see cref="BattleLayout.Rects"/> of the current frame and draws a soft
/// pulsing frame plus a pointer arrow around it. Read-only over the layout; never part of the session's input bounds.
/// </summary>
internal static class TutorialHighlight
{
    private const float Inflate = 4, StrokeWidth = 2, ArrowHeight = 12, ArrowHalfWidth = 7, ArrowGap = 2, TopLimit = 40;
    private const double PeriodMs = 1200;

    /// <summary>
    /// anchor: "&lt;prefix&gt;:{own|selected|any|signal}" | "&lt;prefix&gt;:&lt;literal&gt;" | "&lt;literal&gt;".
    /// Returns a key present in <paramref name="layout"/> or null. Units are scanned in snapshot order, without sorting.
    /// </summary>
    internal static string? ResolveKey(string? anchor, BattleLayout layout, TutorialObservation observation, string? signalUnitId)
    {
        if (string.IsNullOrEmpty(anchor)) return null;
        int colon = anchor.IndexOf(':', StringComparison.Ordinal);
        string? template = colon >= 0 && anchor.EndsWith('}') && anchor.IndexOf('{', colon) == colon + 1 ? anchor[(colon + 2)..^1] : null;
        if (template is null) return layout.Rects.ContainsKey(anchor) ? anchor : null;

        string prefix = anchor[..(colon + 1)];
        string? Key(string? unitId) => unitId is not null && layout.Rects.ContainsKey(prefix + unitId) ? prefix + unitId : null;
        var battle = observation.Battle;
        bool Own(TacticalUnitSnapshot u) => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone &&
            u.State != TacticalUnitState.Destroyed;
        switch (template)
        {
            case "own":
                if (observation.SelectedDroneId is { } selected && battle.Units.Any(u => u.Id == selected && Own(u)) && Key(selected) is { } own)
                    return own;
                return battle.Units.Where(Own).Select(u => Key(u.Id)).FirstOrDefault(k => k is not null);
            case "selected":
                return Key(observation.SelectedDroneId);
            case "any":
                return battle.Units.Select(u => Key(u.Id)).FirstOrDefault(k => k is not null);
            case "signal":
                return Key(signalUnitId);
            default:
                return null;
        }
    }

    internal static SKRect? Target(BattleLayout layout, string? key) =>
        key is not null && layout.Rects.TryGetValue(key, out var rect) && !rect.IsEmpty ? rect : null;

    /// <summary>Pulse opacity 0.55 + 0.45·sin(2π·t/1200) from the screen's injected clock only.</summary>
    internal static float PulseAlpha(long nowMs) => (float)(0.55 + 0.45 * Math.Sin(2 * Math.PI * (nowMs % (long)PeriodMs) / PeriodMs));

    /// <summary>Draws the frame (target inflated by 4) and the arrow; records tutorial:highlight and tutorial:arrow.</summary>
    internal static void Draw(SKCanvas c, BattleLayout layout, SKRect target, long nowMs)
    {
        var frame = target;
        frame.Inflate(Inflate, Inflate);
        byte alpha = (byte)Math.Round(255 * PulseAlpha(nowMs));
        var color = BattleDesign.Yellow.WithAlpha(alpha);
        using (var glow = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 6, Color = BattleDesign.Yellow.WithAlpha((byte)(alpha / 4)) })
            c.DrawRoundRect(frame, 4, 4, glow);
        using (var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, Color = color })
            c.DrawRoundRect(frame, 4, 4, stroke);

        bool below = target.Top < TopLimit;
        float tipY = below ? frame.Bottom + ArrowGap : frame.Top - ArrowGap;
        float baseY = below ? tipY + ArrowHeight : tipY - ArrowHeight;
        var arrow = new SKRect(target.MidX - ArrowHalfWidth, below ? tipY : baseY, target.MidX + ArrowHalfWidth, below ? baseY : tipY);
        BattleDesign.Polygon(c, [new(target.MidX, tipY), new(arrow.Left, baseY), new(arrow.Right, baseY)], color,
            BattleDesign.ButtonInk.WithAlpha(alpha), 1);
        layout.Rects["tutorial:highlight"] = frame;
        layout.Rects["tutorial:arrow"] = arrow;
    }
}
