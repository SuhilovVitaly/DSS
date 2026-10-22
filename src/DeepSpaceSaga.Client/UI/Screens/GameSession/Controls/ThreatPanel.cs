using System.Globalization;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

internal sealed class ThreatPanel
{
    internal enum Action { Select, Countermissile, EvadeLeft, EvadeRight }
    internal sealed record Row(string Id, string Profile, double? EntryRealSeconds, bool Dangerous, bool PredictedIntercept,
        bool Selected, bool CounterEnabled, bool EvadeEnabled);
    internal readonly record struct Hit(string Id, Action Action, SKRect Rect, bool Enabled);
    internal SKRect Bounds { get; private set; }
    internal IReadOnlyList<Row> Rows { get; private set; } = [];
    internal readonly List<Row> VisibleRows = [];
    internal readonly List<Hit> Actions = [];
    private int _offset;
    private string[] _ids = [];
    private CombatInputSettings? _bindings;
    internal string[] ButtonLabels { get; private set; } = [];
    internal void Clear() { Bounds = SKRect.Empty; Rows = []; VisibleRows.Clear(); Actions.Clear(); _offset = 0; _ids = []; }

    internal void Render(SKCanvas canvas, SKRect available, IReadOnlyList<Row> rows, PointCombatVisualSettings styles, CombatInputSettings? bindings = null)
    {
        bindings ??= CombatInputSettings.Default;
        if (!ReferenceEquals(_bindings, bindings))
        {
            _bindings = bindings;
            ButtonLabels = [T("Counter", "CM") + " [" + DebugTuningPanel.KeyLabel(bindings.CountermissileAim) + "]",
                "← [" + DebugTuningPanel.KeyLabel(bindings.EvadeLeft) + "]", "→ [" + DebugTuningPanel.KeyLabel(bindings.EvadeRight) + "]"];
        }
        if (rows.Count == 0) { Clear(); return; }
        var ids = rows.Select(r => r.Id).ToArray();
        if (!_ids.SequenceEqual(ids)) _offset = 0;
        _ids = ids; Rows = rows; VisibleRows.Clear(); Actions.Clear();
        float width = Math.Max(1, Math.Min(440, available.Width - 8));
        bool compact = width < 330;
        float rowHeight = compact ? 64 : 40;
        int capacity = Math.Clamp((int)((available.Height * .4f - 24) / rowHeight), 1, 6);
        int visible = Math.Min(capacity, rows.Count);
        _offset = Math.Clamp(_offset, 0, Math.Max(0, rows.Count - visible));
        Bounds = new(available.Left + 4, available.Top + 4, available.Left + 4 + width, available.Top + 28 + rowHeight * visible);
        using var fill = new SKPaint { Color = TacticalMapPalette.LabelBackground };
        using var line = new SKPaint { Color = styles.Threat, StrokeWidth = (float)styles.LineWidthPx, Style = SKPaintStyle.Stroke, IsAntialias = true };
        using var text = new SKPaint { Color = styles.ForeignProfileText, TextSize = (float)styles.TextSizePx, IsAntialias = true };
        canvas.DrawRect(Bounds, fill); canvas.DrawRect(Bounds, line);
        canvas.Save(); canvas.ClipRect(Bounds);
        canvas.DrawText(T("Title", "Threats") + $" · {rows.Count}" + (rows.Count > visible ? $" · {_offset + 1}–{_offset + visible}" : ""), Bounds.Left + 8, Bounds.Top + 17, text);
        for (int i = 0; i < visible; i++)
        {
            var row = rows[_offset + i]; VisibleRows.Add(row);
            float top = Bounds.Top + 24 + i * rowHeight;
            var rect = new SKRect(Bounds.Left + 2, top, Bounds.Right - 2, top + rowHeight);
            Actions.Add(new(row.Id, Action.Select, rect, true));
            if (row.Selected)
            {
                fill.Color = styles.Threat.WithAlpha(35); canvas.DrawRect(rect, fill);
                fill.Color = styles.Threat; canvas.DrawRect(rect.Left, rect.Top, 3, rect.Height, fill);
            }
            text.Color = row.Dangerous ? styles.Danger : styles.ForeignProfileText;
            string profile = GameSessionScreen.CombatText("Profile." + row.Profile, row.Profile);
            canvas.DrawText(profile + (row.PredictedIntercept ? " · ~" + T("Counter", "CM") : ""), rect.Left + 6, top + 14, text);
            string deadline = row.EntryRealSeconds is { } seconds ? seconds.ToString("0.0", CultureInfo.InvariantCulture) + " " + GameSessionScreen.CombatText("Seconds", "s") : "—";
            canvas.DrawText(T("Boundary", "Boundary") + " " + deadline, rect.Left + 6, top + 30, text);
            float buttonWidth = Math.Min(70, Math.Max(1, (rect.Width - 20) / 3));
            float x = compact ? rect.Left + 6 : rect.Right - (buttonWidth + 4) * 3 - 2;
            float y = compact ? top + 40 : top + 10;
            DrawButton(Action.Countermissile, ButtonLabels[0], row.CounterEnabled);
            DrawButton(Action.EvadeLeft, ButtonLabels[1], row.EvadeEnabled);
            DrawButton(Action.EvadeRight, ButtonLabels[2], row.EvadeEnabled);
            void DrawButton(Action action, string label, bool enabled)
            {
                var button = new SKRect(x, y, x + buttonWidth, y + 20);
                line.Color = enabled ? styles.Threat : styles.TargetShadow.WithAlpha(90);
                canvas.DrawRect(button, line); text.Color = line.Color;
                canvas.DrawText(label, button.MidX - text.MeasureText(label) / 2, button.Top + 14, text);
                Actions.Add(new(row.Id, action, button, enabled)); x += buttonWidth + 4;
            }
        }
        canvas.Restore();
    }

    internal Hit? HitTest(float x, float y)
    {
        if (!Bounds.Contains(x, y)) return null;
        // Action buttons take precedence over the containing selectable row.
        foreach (var action in Actions.AsEnumerable().Reverse()) if (action.Rect.Contains(x, y)) return action;
        return null;
    }
    internal bool Scroll(float x, float y, float delta)
    {
        if (!Bounds.Contains(x, y)) return false;
        if (float.IsFinite(delta) && delta != 0) _offset = Math.Clamp(_offset + (delta > 0 ? -1 : 1), 0, Math.Max(0, Rows.Count - VisibleRows.Count));
        return true;
    }
    private static string T(string key, string fallback) => GameSessionScreen.CombatText("Threats." + key, fallback);
}
