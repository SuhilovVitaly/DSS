using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>Nonmodal encounter results. All values are captured Engine facts.</summary>
internal sealed class CombatSummaryPanel
{
    internal SKRect Bounds { get; private set; }
    internal SKRect ContinueRect { get; private set; }
    internal SKRect StatsRect { get; private set; }
    internal bool CanContinue { get; private set; }
    internal IReadOnlyList<string> Lines { get; private set; } = [];
    private float _scroll, _scrollLimit;
    internal void Hide() { Bounds = ContinueRect = StatsRect = SKRect.Empty; Lines = []; _scroll = _scrollLimit = 0; CanContinue = false; }
    internal void Render(SKCanvas canvas, SKRect available, CombatEncounterSnapshot encounter, bool canContinue, string? error = null)
    {
        if (available.Width < 90 || available.Height < 100) { Hide(); return; }
        float width = Math.Min(440, available.Width - 8), height = Math.Min(300, available.Height - 8);
        Bounds = new(available.MidX - width / 2, available.Top + 4, available.MidX + width / 2, available.Top + height + 4);
        ContinueRect = new(Bounds.Left + 8, Bounds.Bottom - 38, Bounds.Right - 8, Bounds.Bottom - 8);
        StatsRect = new(Bounds.Left + 8, Bounds.Top + 34, Bounds.Right - 8, ContinueRect.Top - 8);
        Lines = error is null ? FormatStats(encounter) : FormatStats(encounter).Concat([error]).ToArray(); CanContinue = canContinue;
        using var fill = new SKPaint { Color = new(8, 25, 36, 245) };
        using var border = new SKPaint { Color = XenonStyle.CyanBright, Style = SKPaintStyle.Stroke };
        using var text = new SKPaint { Color = XenonStyle.CyanBright, TextSize = 14, IsAntialias = true };
        canvas.DrawRect(Bounds, fill); canvas.DrawRect(Bounds, border);
        canvas.Save(); canvas.ClipRect(Bounds);
        canvas.DrawText(GameSessionScreen.CombatText("BattleSummary", "Бой завершён"), Bounds.Left + 8, Bounds.Top + 22, text);
        _scrollLimit = DrawStats(canvas, StatsRect, Lines, 12, _scroll); _scroll = Math.Clamp(_scroll, 0, _scrollLimit);
        GenericButtonTypeA.Draw(canvas, ContinueRect, GameSessionScreen.CombatText("Continue", "Продолжить"), canContinue ? ButtonState.Normal : ButtonState.Disabled);
        canvas.Restore();
    }
    internal bool Scroll(float x, float y, float delta)
    {
        if (!Bounds.Contains(x, y)) return false;
        if (float.IsFinite(delta) && delta != 0) _scroll = Math.Clamp(_scroll - Math.Sign(delta) * 32, 0, _scrollLimit);
        return true;
    }
    internal static string Duration(double realSeconds)
    {
        double value = double.IsFinite(realSeconds) ? Math.Clamp(realSeconds, 0, 1e12) : 0;
        return FormattableString.Invariant($"{Math.Floor(value / 3600):00}:{Math.Floor(value / 60) % 60:00}:{Math.Floor(value) % 60:00}");
    }
    internal static IReadOnlyList<string> FormatStats(CombatEncounterSnapshot encounter)
    {
        string T(string key) => GameSessionScreen.CombatText("Stats." + key, key);
        var s = encounter.Stats;
        return [
            $"{GameSessionScreen.CombatText("Duration", "Длительность")}: {Duration(encounter.DurationSeconds * 4)}",
            $"{T("TorpedoesLaunched")}: {s.TorpedoesLaunched}",
            $"{T("CountermissilesLaunched")}: {s.CountermissilesLaunched}",
            $"{T("EnemyTorpedoesLaunched")}: {s.EnemyTorpedoesLaunched}",
            $"{T("TorpedoesIntercepted")}: {s.TorpedoesIntercepted}",
            $"{T("PlayerHitsReceived")}: {s.PlayerHitsReceived}",
            $"{T("EnemyHitsDealt")}: {s.EnemyHitsDealt}",
            $"{T("PlayerEvasions")}: {s.PlayerEvasions}",
            $"{T("EnemiesDestroyed")}: {s.EnemiesDestroyed}",
            $"{T("ArmorDamageTaken")}: {s.ArmorDamageTaken}",
            $"{T("StructureDamageTaken")}: {s.StructureDamageTaken}"
        ];
    }
    internal static float DrawStats(SKCanvas canvas, SKRect rect, IReadOnlyList<string> lines, float fontSize, float scroll)
    {
        using var text = new SKPaint { Color = MenuStyle.ColorText, TextSize = fontSize, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        var wrapped = lines.SelectMany(line => CombatReportPanelLayout.Wrap(line, Math.Max(1, rect.Width - 6), text)).ToArray();
        float step = fontSize + 4, max = Math.Max(0, wrapped.Length * step - rect.Height);
        float offset = Math.Clamp(scroll, 0, max);
        canvas.Save(); canvas.ClipRect(rect);
        for (int i = 0; i < wrapped.Length; i++) canvas.DrawText(wrapped[i], rect.Left, rect.Top + fontSize + i * step - offset, text);
        if (max > 0 && rect.Height > 0)
        {
            float height = Math.Max(8, rect.Height * rect.Height / (rect.Height + max));
            float top = rect.Top + (rect.Height - height) * offset / max;
            canvas.DrawRect(new SKRect(rect.Right - 3, top, rect.Right, top + height), text);
        }
        canvas.Restore(); return max;
    }
}
