using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Defeat;

/// <summary>A terminal screen: only Load and Main Menu can leave it.</summary>
public sealed class DefeatScreen : IScreen
{
    private readonly float _uiScale;
    private DefeatLayout _layout;
    private ScreenEvent _hovered;
    private ScreenEvent _focused = ScreenEvent.OpenLoadWindow;
    private readonly CombatEncounterSnapshot? _encounter;
    private float _statsScroll, _statsScrollLimit;

    internal DefeatLayout Layout => _layout;
    internal float StatsScrollOffset => _statsScroll;
    internal IReadOnlyList<string> StatsLines => _encounter is null ? [] : FormatStats(_encounter);

    public DefeatScreen(float uiScale = 1, CombatEncounterSnapshot? encounter = null)
    { _uiScale = float.IsFinite(uiScale) && uiScale > 0 ? uiScale : 1; _encounter = encounter; }

    public void OnActivated() { _hovered = ScreenEvent.None; _focused = ScreenEvent.OpenLoadWindow; }
    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key)
    {
        if (key is Key.Tab or Key.Down or Key.Up or Key.Left or Key.Right)
            _focused = _focused == ScreenEvent.OpenLoadWindow ? ScreenEvent.MainMenu : ScreenEvent.OpenLoadWindow;
        return key is Key.Enter or Key.Space ? _focused : ScreenEvent.None;
    }

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button) =>
        button == MouseButton.Left ? _layout.Hit(x / _uiScale, y / _uiScale) : ScreenEvent.None;

    public bool OnMouseMove(float x, float y)
    {
        _hovered = _layout.Hit(x / _uiScale, y / _uiScale);
        if (_hovered != ScreenEvent.None) _focused = _hovered;
        return _hovered != ScreenEvent.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (_layout.Stats.Contains(x / _uiScale, y / _uiScale) && float.IsFinite(delta) && delta != 0)
            _statsScroll = Math.Clamp(_statsScroll - Math.Sign(delta) * 24, 0, _statsScrollLimit);
        return ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _layout = DefeatLayout.Create((int)(width / _uiScale), (int)(height / _uiScale), _encounter is not null);
        using var shade = new SKPaint { Color = new SKColor(0, 0, 0, 220) };
        canvas.DrawRect(0, 0, width, height, shade);
        canvas.Save();
        canvas.Scale(_uiScale);
        GenericWindowTypeA.Draw(canvas, _layout.Panel);
        using var title = new SKPaint
        {
            Color = MenuStyle.ColorText,
            TextSize = 26 * _layout.Scale,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = MenuStyle.TextTitle.Typeface
        };
        string text = Localization.Get("Combat.Defeat");
        float measured = title.MeasureText(text), available = _layout.Panel.Width - 48 * _layout.Scale;
        if (measured > available) title.TextSize *= available / measured;
        canvas.DrawText(text, _layout.Panel.MidX, _layout.Panel.Top + 70 * _layout.Scale, title);
        if (_encounter is not null)
        {
            _statsScrollLimit = DrawStats(canvas, _layout.Stats, StatsLines, Math.Max(8, 12 * _layout.Scale), _statsScroll);
            _statsScroll = Math.Clamp(_statsScroll, 0, _statsScrollLimit);
        }
        Draw(_layout.Load, "Combat.Defeat.Load", ScreenEvent.OpenLoadWindow);
        Draw(_layout.MainMenu, "Combat.Defeat.MainMenu", ScreenEvent.MainMenu);
        canvas.Restore();

        void Draw(SKRect rect, string key, ScreenEvent action)
        {
            GenericButtonTypeA.Draw(canvas, rect, Localization.Get(key),
                action == _focused || action == _hovered ? ButtonState.Hovered : ButtonState.Normal);
        }
    }

    private static string Duration(double realSeconds)
    {
        double value = double.IsFinite(realSeconds) ? Math.Clamp(realSeconds, 0, 1e12) : 0;
        return FormattableString.Invariant($"{Math.Floor(value / 3600):00}:{Math.Floor(value / 60) % 60:00}:{Math.Floor(value) % 60:00}");
    }
    private static IReadOnlyList<string> FormatStats(CombatEncounterSnapshot encounter)
    {
        string T(string key) => Localization.Combat("Stats." + key);
        var s = encounter.Stats;
        return [
            $"{Localization.Combat("Duration")}: {Duration(encounter.DurationSeconds * 4)}",
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
    private static float DrawStats(SKCanvas canvas, SKRect rect, IReadOnlyList<string> lines, float fontSize, float scroll)
    {
        using var text = new SKPaint { Color = MenuStyle.ColorText, TextSize = fontSize, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        var wrapped = lines.SelectMany(line => WrapStats(line, Math.Max(1, rect.Width - 6), text)).ToArray();
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

    private static ImmutableArray<string> WrapStats(string text, float width, SKPaint paint)
    {
        var lines = ImmutableArray.CreateBuilder<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            string rest = paragraph;
            while (rest.Length > 0)
            {
                int count = Math.Clamp((int)paint.BreakText(rest, Math.Max(1, width)), 1, rest.Length);
                if (count < rest.Length)
                {
                    int space = rest.LastIndexOf(' ', count - 1, count);
                    if (space > 0) count = space;
                    if (count > 0 && char.IsHighSurrogate(rest[count - 1]) && count < rest.Length && char.IsLowSurrogate(rest[count])) count++;
                }
                lines.Add(rest[..count].Trim());
                rest = rest[count..].TrimStart();
            }
        }
        return lines.ToImmutable();
    }
}
