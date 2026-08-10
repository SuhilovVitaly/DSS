using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Commands Panel (top-left) — the module-addressed command widget from
/// Stories/CommandPanelPlan.md, подзадача 1 (каркас). Self-contained UI-only skeleton:
/// a Caption (360 x 40, title "Modules") + an empty Body (width 360, placeholder for
/// future modules). No buttons, no state-machine, no modules, no Engine commands in
/// this iteration — the panel only owns geometry, rendering and hit-test consumption.
/// It never talks to the Engine/connection directly; commands arrive in later
/// подзадачи (4-6 of CommandPanelPlan.md). The existing bottom-center
/// DrawEngineCommandPanel in GameSessionScreen is untouched and keeps working.
/// </summary>
public sealed class CommandsPanel
{
    /// <summary>Panel width — Caption and Body share it (CommandPanelPlan.md: 360 = 60 + 300).</summary>
    public const float PanelWidth = 360f;

    /// <summary>Panel Caption height.</summary>
    public const float CaptionHeight = 40f;

    /// <summary>
    /// Placeholder height of the Body — the height of one future module row
    /// (CommandPanelPlan.md: Module Caption 60 x 200 / Module Body 300 x 200).
    /// </summary>
    public const float ModuleRowHeight = 200f;

    private const float Margin = 8f;   // = PanelMargin of GameSessionScreen
    private const float Padding = 6f;

    private readonly SKPaint _panelBgPaint;
    private readonly SKPaint _panelBorderPaint;
    private readonly SKPaint _titlePaint;

    private CommandsPanelState _state = CommandsPanelState.Opened;
    private SKRect _captionRect;
    private SKRect _bodyRect;

    public CommandsPanel()
    {
        var typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;
        _panelBgPaint = new SKPaint { Color = new SKColor(0, 0, 0, 200), Style = SKPaintStyle.Fill };
        _panelBorderPaint = new SKPaint { Color = new SKColor(42, 42, 42), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _titlePaint = new SKPaint { Color = new SKColor(210, 210, 210), TextSize = 13f, IsAntialias = true, Typeface = typeface };
    }

    // ── Test seams ──────────────────────────────────────────────

    public CommandsPanelState State => _state;
    public SKRect CaptionRect => _captionRect;
    public SKRect BodyRect => _bodyRect;

    /// <summary>
    /// Layout + draw the panel. Screen-space, painted over the map, no world transform.
    /// </summary>
    public void Render(SKCanvas canvas)
    {
        _captionRect = new SKRect(
            Margin, Margin,
            Margin + PanelWidth, Margin + CaptionHeight);
        _bodyRect = new SKRect(
            Margin, _captionRect.Bottom,
            Margin + PanelWidth, _captionRect.Bottom + ModuleRowHeight);

        DrawCaption(canvas);
        DrawBody(canvas);
    }

    /// <summary>
    /// Hit-test. Returns <c>true</c> when the click lands on the panel (Caption or Body)
    /// — consumed, so it never falls through to map pan/selection. No commands.
    /// </summary>
    public bool OnMouseDown(float x, float y)
    {
        return _captionRect.Contains(x, y) || _bodyRect.Contains(x, y);
    }

    private void DrawCaption(SKCanvas canvas)
    {
        canvas.DrawRect(_captionRect, _panelBgPaint);
        canvas.DrawRect(_captionRect, _panelBorderPaint);

        float textY = _captionRect.MidY + _titlePaint.TextSize / 3f;
        canvas.DrawText("Modules", _captionRect.Left + Padding, textY, _titlePaint);
    }

    private void DrawBody(SKCanvas canvas)
    {
        if (_bodyRect.Height <= 0)
            return;

        canvas.DrawRect(_bodyRect, _panelBgPaint);
        canvas.DrawRect(_bodyRect, _panelBorderPaint);
    }
}

/// <summary>Panel visibility state. State switching is подзадача 2; the skeleton is always <see cref="Opened"/>.</summary>
public enum CommandsPanelState
{
    /// <summary>Caption + all modules visible.</summary>
    Opened,

    /// <summary>Caption + only modules with state Opened visible.</summary>
    ActiveModules,

    /// <summary>Caption only, Body hidden.</summary>
    Closed,
}
