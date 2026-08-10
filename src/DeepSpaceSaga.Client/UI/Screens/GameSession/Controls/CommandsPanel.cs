using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Commands Panel (top-left) — the module-addressed command widget from
/// Stories/CommandPanelPlan.md. Self-contained UI component: a Caption (360×40)
/// with state-toggle buttons (Hide / Show / Show Active, 32×32 each) + a Body
/// whose visibility depends on the panel state. No Engine commands — the panel
/// only owns geometry, rendering, hit-test consumption and in-memory state.
/// </summary>
public sealed class CommandsPanel
{
    /// <summary>Panel width — Caption and Body share it (CommandPanelPlan.md: 360 = 60 + 300).</summary>
    public const float PanelWidth = 360f;

    /// <summary>Panel Caption height.</summary>
    public const float CaptionHeight = 40f;

    /// <summary>
    /// Placeholder height of the Body — the height of one future module row
    /// (CommandPanelPlan.md: Module Caption 60×200 / Module Body 300×200).
    /// </summary>
    public const float ModuleRowHeight = 200f;

    private const float Margin = 8f;   // = PanelMargin of GameSessionScreen
    private const float Padding = 6f;

    // ── Button geometry ─────────────────────────────────────────
    public const float ButtonSize = 32f;
    public const float ButtonLeftPadding = 10f;
    private const float ButtonGap = 4f;

    private SKRect _hideButtonRect;
    private SKRect _showButtonRect;
    private SKRect _showActiveButtonRect;

    private int _hoveredButtonIndex = -1;  // 0=Hide, 1=Show, 2=ShowActive, -1=none
    private int _pressedButtonIndex = -1;

    // ── Paints ──────────────────────────────────────────────────

    private readonly SKPaint _panelBgPaint;
    private readonly SKPaint _panelBorderPaint;
    private readonly SKPaint _titlePaint;

    private readonly SKPaint _btnNormalPaint;
    private readonly SKPaint _btnHoverPaint;
    private readonly SKPaint _btnPressedPaint;
    private readonly SKPaint _btnActivePaint;
    private readonly SKPaint _btnBorderPaint;
    private readonly SKPaint _btnIconPaint;

    private CommandsPanelState _state = CommandsPanelState.Opened;
    private SKRect _captionRect;
    private SKRect _bodyRect;

    public CommandsPanel()
    {
        var typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;

        _panelBgPaint = new SKPaint { Color = new SKColor(0, 0, 0, 200), Style = SKPaintStyle.Fill };
        _panelBorderPaint = new SKPaint { Color = new SKColor(42, 42, 42), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _titlePaint = new SKPaint { Color = new SKColor(210, 210, 210), TextSize = 13f, IsAntialias = true, Typeface = typeface };

        _btnNormalPaint = new SKPaint { Color = new SKColor(35, 35, 35, 220), Style = SKPaintStyle.Fill };
        _btnHoverPaint = new SKPaint { Color = new SKColor(55, 55, 55, 230), Style = SKPaintStyle.Fill };
        _btnPressedPaint = new SKPaint { Color = new SKColor(70, 70, 70, 240), Style = SKPaintStyle.Fill };
        _btnActivePaint = new SKPaint { Color = new SKColor(50, 80, 50, 230), Style = SKPaintStyle.Fill };
        _btnBorderPaint = new SKPaint { Color = new SKColor(80, 80, 80), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _btnIconPaint = new SKPaint { Color = new SKColor(200, 200, 200), TextSize = 9f, IsAntialias = true, Typeface = typeface };
    }

    // ── Test seams ──────────────────────────────────────────────

    public CommandsPanelState State => _state;
    public SKRect CaptionRect => _captionRect;
    public SKRect BodyRect => _bodyRect;

    public SKRect HideButtonRect => _hideButtonRect;
    public SKRect ShowButtonRect => _showButtonRect;
    public SKRect ShowActiveButtonRect => _showActiveButtonRect;

    public int HoveredButtonIndex => _hoveredButtonIndex;
    public int PressedButtonIndex => _pressedButtonIndex;

    // ── Layout helpers ──────────────────────────────────────────

    /// <summary>Compute button Y so they're vertically centred in the caption.</summary>
    private float ButtonTop => Margin + (CaptionHeight - ButtonSize) / 2f;

    /// <summary>Compute the three button rects from the current layout.</summary>
    private void LayoutButtons()
    {
        float x = Margin + ButtonLeftPadding;
        float y = ButtonTop;

        _hideButtonRect = new SKRect(x, y, x + ButtonSize, y + ButtonSize);
        x += ButtonSize + ButtonGap;

        _showButtonRect = new SKRect(x, y, x + ButtonSize, y + ButtonSize);
        x += ButtonSize + ButtonGap;

        _showActiveButtonRect = new SKRect(x, y, x + ButtonSize, y + ButtonSize);
    }

    // ── Input ───────────────────────────────────────────────────

    /// <summary>
    /// Hit-test. Returns <c>true</c> when the click lands on the panel (Caption or Body
    /// — or on a button, which also changes panel state). Returns <c>false</c> when the
    /// click is outside the panel and should fall through to map pan/selection.
    /// Never sends Engine commands.
    /// </summary>
    public bool OnMouseDown(float x, float y)
    {
        // Buttons in caption — check before generic caption consumption.
        if (_hideButtonRect.Contains(x, y))
        {
            _pressedButtonIndex = 0;
            _state = CommandsPanelState.Closed;
            return true;
        }

        if (_showButtonRect.Contains(x, y))
        {
            _pressedButtonIndex = 1;
            _state = CommandsPanelState.Opened;
            return true;
        }

        if (_showActiveButtonRect.Contains(x, y))
        {
            _pressedButtonIndex = 2;
            _state = CommandsPanelState.ActiveModules;
            return true;
        }

        return _captionRect.Contains(x, y) || _bodyRect.Contains(x, y);
    }

    public void OnMouseMove(float x, float y)
    {
        if (_hideButtonRect.Contains(x, y))
            _hoveredButtonIndex = 0;
        else if (_showButtonRect.Contains(x, y))
            _hoveredButtonIndex = 1;
        else if (_showActiveButtonRect.Contains(x, y))
            _hoveredButtonIndex = 2;
        else
            _hoveredButtonIndex = -1;
    }

    public void OnMouseUp(float x, float y)
    {
        _pressedButtonIndex = -1;
    }

    // ── Render ──────────────────────────────────────────────────

    /// <summary>
    /// Layout + draw the panel. Screen-space, painted over the map, no world transform.
    /// </summary>
    public void Render(SKCanvas canvas)
    {
        LayoutButtons();

        _captionRect = new SKRect(
            Margin, Margin,
            Margin + PanelWidth, Margin + CaptionHeight);

        // Body: zero-height when Closed (Caption only).
        float bodyHeight = _state == CommandsPanelState.Closed ? 0f : ModuleRowHeight;
        _bodyRect = new SKRect(
            Margin, _captionRect.Bottom,
            Margin + PanelWidth, _captionRect.Bottom + bodyHeight);

        DrawCaption(canvas);
        if (_state != CommandsPanelState.Closed)
            DrawBody(canvas);
    }

    private void DrawCaption(SKCanvas canvas)
    {
        canvas.DrawRect(_captionRect, _panelBgPaint);
        canvas.DrawRect(_captionRect, _panelBorderPaint);

        DrawButton(canvas, _hideButtonRect, 0, "—");
        DrawButton(canvas, _showButtonRect, 1, "□");
        DrawButton(canvas, _showActiveButtonRect, 2, "○");

        // "Modules" label to the right of the last button.
        float textX = _showActiveButtonRect.Right + Padding + 2f;
        float textY = _captionRect.MidY + _titlePaint.TextSize / 3f;
        canvas.DrawText("Modules", textX, textY, _titlePaint);
    }

    private void DrawButton(SKCanvas canvas, SKRect rect, int buttonIndex, string icon)
    {
        SKPaint fill;
        if (buttonIndex == _pressedButtonIndex && buttonIndex == _hoveredButtonIndex)
            fill = _btnPressedPaint;
        else if (buttonIndex == _hoveredButtonIndex)
            fill = _btnHoverPaint;
        else if (IsActiveButton(buttonIndex))
            fill = _btnActivePaint;
        else
            fill = _btnNormalPaint;

        canvas.DrawRect(rect, fill);
        canvas.DrawRect(rect, _btnBorderPaint);

        float iconWidth = _btnIconPaint.MeasureText(icon);
        float iconX = rect.MidX - iconWidth / 2f;
        float iconY = rect.MidY + _btnIconPaint.TextSize / 3f;
        canvas.DrawText(icon, iconX, iconY, _btnIconPaint);
    }

    /// <summary>Whether <paramref name="buttonIndex"/> corresponds to the current <see cref="_state"/>.</summary>
    private bool IsActiveButton(int buttonIndex) => buttonIndex switch
    {
        0 => _state == CommandsPanelState.Closed,
        1 => _state == CommandsPanelState.Opened,
        2 => _state == CommandsPanelState.ActiveModules,
        _ => false,
    };

    private void DrawBody(SKCanvas canvas)
    {
        if (_bodyRect.Height <= 0)
            return;

        canvas.DrawRect(_bodyRect, _panelBgPaint);
        canvas.DrawRect(_bodyRect, _panelBorderPaint);
    }
}

/// <summary>Panel visibility state.</summary>
public enum CommandsPanelState
{
    /// <summary>Caption + all modules visible.</summary>
    Opened,

    /// <summary>Caption + only modules with state Opened visible.</summary>
    ActiveModules,

    /// <summary>Caption only, Body hidden.</summary>
    Closed,
}
