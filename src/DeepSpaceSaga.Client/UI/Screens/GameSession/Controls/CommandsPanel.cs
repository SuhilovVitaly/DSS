using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Commands Panel (top-left) — the module-addressed command widget from
/// Stories/CommandPanelPlan.md. Self-contained UI component: a Caption (360×40)
/// with state-toggle buttons (Hide / Show / Show Active, 32×32 each) + a list of
/// module rows (60×200 caption + 300×200 body each) for active modules whose
/// <c>CommandTypeIds</c> is not empty. No Engine commands — the panel only owns
/// geometry, rendering, hit-test consumption and in-memory UI state.
/// </summary>
public sealed class CommandsPanel
{
    /// <summary>Panel width — Caption and Body share it (CommandPanelPlan.md: 360 = 60 + 300).</summary>
    public const float PanelWidth = 360f;

    /// <summary>Panel Caption height.</summary>
    public const float CaptionHeight = 32f;

    /// <summary>Height of one module row (caption + body).</summary>
    public const float ModuleRowHeight = 200f;

    /// <summary>Module Caption height (horizontal bar above the body).</summary>
    public const float ModuleCaptionHeight = 30f;

    private const float Margin = 8f;   // = PanelMargin of GameSessionScreen
    private const float Padding = 6f;

    // ── Button geometry ─────────────────────────────────────────
    public const float ButtonSize = 26f;
    public const float ButtonLeftPadding = 0f;
    private const float ButtonGap = 4f;

    private SKRect _hideShowButtonRect;
    private SKRect _showButtonRect;
    private SKRect _showActiveButtonRect;

    private int _hoveredButtonIndex = -1;  // 0=Hide, 1=Show, 2=ShowActive, -1=none
    private int _pressedButtonIndex = -1;

    // ── Module state (in-memory, per session) ───────────────────
    private readonly Dictionary<string, bool> _moduleOpenedById = new(StringComparer.Ordinal);

    // ── Layout cache (built in Render, read by test seams) ──────
    private readonly List<ModuleRowGeometry> _moduleRows = [];
    private SKRect _captionRect;
    private SKRect _bodyRect;

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

    private readonly SKPaint _moduleCaptionTextPaint;

    // ── Images ──────────────────────────────────────────────────
    private readonly SKBitmap? _captionBackgroundImage;
    private readonly SKBitmap? _hideImage;
    private readonly SKBitmap? _showImage;
    private readonly SKBitmap? _showAllModulesImage;
    private readonly SKBitmap? _showActiveImage;

    private CommandsPanelState _state = CommandsPanelState.AllModules;
    private CommandsPanelState _previousNonClosedState = CommandsPanelState.AllModules;

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

        _moduleCaptionTextPaint = new SKPaint { Color = new SKColor(180, 180, 180), TextSize = 14f, IsAntialias = true, Typeface = typeface };

        _captionBackgroundImage = LoadImage("Images/UI/GameSessionScreenUI/command-panel-caption-background.png");
        _hideImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-hide.png");
        _showImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-show.png");
        _showAllModulesImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-show-all.png");
        _showActiveImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-show-active.png");
    }

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    // ── Test seams ──────────────────────────────────────────────

    public CommandsPanelState State => _state;
    public CommandsPanelState PreviousNonClosedState => _previousNonClosedState;
    public SKRect CaptionRect => _captionRect;
    public SKRect BodyRect => _bodyRect;

    public SKRect HideShowButtonRect => _hideShowButtonRect;
    public SKRect ShowButtonRect => _showButtonRect;
    public SKRect ShowActiveButtonRect => _showActiveButtonRect;

    public int HoveredButtonIndex => _hoveredButtonIndex;
    public int PressedButtonIndex => _pressedButtonIndex;

    /// <summary>Module row geometry from the last <see cref="Render"/> call.</summary>
    public IReadOnlyList<ModuleRowGeometry> ModuleRows => _moduleRows;

    // ── Layout helpers ──────────────────────────────────────────

    private float ButtonTop => Margin; // top-aligned at caption origin

    private void LayoutButtons()
    {
        float x = Margin + ButtonLeftPadding;
        float y = ButtonTop;

        _hideShowButtonRect = new SKRect(x, y, x + ButtonSize, y + ButtonSize);
        x += ButtonSize + ButtonGap;

        _showButtonRect = new SKRect(x, y, x + ButtonSize, y + ButtonSize);
        x += ButtonSize + ButtonGap;

        _showActiveButtonRect = new SKRect(x, y, x + ButtonSize, y + ButtonSize);
    }

    // ── Input ───────────────────────────────────────────────────

    /// <summary>
    /// Hit-test. Returns <c>true</c> when the click lands on the panel (buttons,
    /// module captions, module bodies, or panel caption/body). Returns <c>false</c>
    /// when the click is outside and should fall through to map pan/selection.
    /// Never sends Engine commands.
    /// </summary>
    public bool OnMouseDown(float x, float y)
    {
        // 1. Panel state buttons.
        if (_hideShowButtonRect.Contains(x, y))
        {
            _pressedButtonIndex = 0;
            if (_state == CommandsPanelState.Closed)
                _state = _previousNonClosedState;
            else
            {
                _previousNonClosedState = _state;
                _state = CommandsPanelState.Closed;
            }

            return true;
        }

        if (_showButtonRect.Contains(x, y))
        {
            _pressedButtonIndex = 1;
            _state = CommandsPanelState.AllModules;
            return true;
        }

        if (_showActiveButtonRect.Contains(x, y))
        {
            _pressedButtonIndex = 2;
            _state = CommandsPanelState.ActiveModules;
            return true;
        }

        // 2. Module caption rects — toggle per-module Opened/Closed.
        foreach (var row in _moduleRows)
        {
            if (row.CaptionRect.Contains(x, y))
            {
                string id = row.ModuleId;
                bool wasOpened = !_moduleOpenedById.TryGetValue(id, out bool o) || o;
                _moduleOpenedById[id] = !wasOpened;
                return true;
            }
        }

        // 3. Module body rects (only opened rows) — consumed, no action.
        foreach (var row in _moduleRows)
        {
            if (row is { Opened: true, BodyRect: var br } && br.Contains(x, y))
                return true;
        }

        // 4. Panel caption / body — consumed, no action.
        return _captionRect.Contains(x, y) || _bodyRect.Contains(x, y);
    }

    public bool OnMouseMove(float x, float y)
    {
        if (_hideShowButtonRect.Contains(x, y))
            _hoveredButtonIndex = 0;
        else if (_showButtonRect.Contains(x, y))
            _hoveredButtonIndex = 1;
        else if (_showActiveButtonRect.Contains(x, y))
            _hoveredButtonIndex = 2;
        else
            _hoveredButtonIndex = -1;

        return _hoveredButtonIndex >= 0 || _moduleRows.Any(r => r.CaptionRect.Contains(x, y));
    }

    public void OnMouseUp(float x, float y)
    {
        _pressedButtonIndex = -1;
    }

    // ── Render ──────────────────────────────────────────────────

    /// <summary>
    /// Layout + draw the panel. Screen-space, painted over the map, no world transform.
    /// </summary>
    /// <param name="canvas">Skia canvas.</param>
    /// <param name="modules">Installed modules from the latest snapshot (may be empty).</param>
    public void Render(SKCanvas canvas, IReadOnlyList<InstalledModuleSnapshot> modules)
    {
        LayoutButtons();

        _captionRect = new SKRect(
            Margin, Margin,
            Margin + PanelWidth, Margin + CaptionHeight);

        // Build visible module rows.
        _moduleRows.Clear();

        float rowY = _captionRect.Bottom;

        if (_state != CommandsPanelState.Closed)
        {
            // Guard against default ImmutableArray from old snapshots without the field.
            var safeModules = modules is ImmutableArray<InstalledModuleSnapshot> { IsDefault: false } arr
                ? arr
                : ImmutableArray<InstalledModuleSnapshot>.Empty;

            // Data-driven: only modules with non-empty CommandTypeIds.
            // Order: module.engine.basic always first, then by Position (loaded order, stable).
            var activeModules = safeModules
                .Where(m => m.CommandTypeIds.Length > 0)
                .OrderBy(m => m.ModuleTypeId == "module.engine.basic" ? -1 : 0)
                .ThenBy(m => m.Position)
                .ToList();

            // Forget per-module state for modules no longer active.
            var activeIds = new HashSet<string>(activeModules.Select(m => m.ModuleId), StringComparer.Ordinal);
            foreach (string staleId in _moduleOpenedById.Keys.Except(activeIds).ToList())
                _moduleOpenedById.Remove(staleId);

            foreach (var mod in activeModules)
            {
                // Default opened — module starts visible with body.
                bool opened = !_moduleOpenedById.TryGetValue(mod.ModuleId, out bool o) || o;

                // In ActiveModules state, only rows with Opened state are visible.
                if (_state == CommandsPanelState.ActiveModules && !opened)
                    continue;

                var captionRect = new SKRect(
                    Margin, rowY,
                    Margin + PanelWidth, rowY + ModuleCaptionHeight);

                var bodyRect = opened
                    ? new SKRect(
                        Margin, rowY + ModuleCaptionHeight,
                        Margin + PanelWidth, rowY + ModuleRowHeight)
                    : SKRect.Empty;

                _moduleRows.Add(new ModuleRowGeometry(
                    mod.ModuleId, mod.DisplayName, mod.Position, opened, captionRect, bodyRect));

                rowY += opened ? ModuleRowHeight : ModuleCaptionHeight;
            }
        }

        // Body rect: from caption bottom to the bottom of the last module row.
        float bodyBottom = rowY;
        _bodyRect = new SKRect(
            Margin, _captionRect.Bottom,
            Margin + PanelWidth, bodyBottom);

        // Draw.
        DrawCaption(canvas);
        foreach (var row in _moduleRows)
            DrawModuleRow(canvas, row);
    }

    private void DrawCaption(SKCanvas canvas)
    {
        if (_captionBackgroundImage is not null)
            canvas.DrawBitmap(_captionBackgroundImage, _captionRect);
        else
            canvas.DrawRect(_captionRect, _panelBgPaint);

        // HideShow button: hide icon when open, show icon when closed.
        var hideShowImage = _state == CommandsPanelState.Closed ? _showImage : _hideImage;
        DrawButton(canvas, _hideShowButtonRect, 0, hideShowImage);

        DrawButton(canvas, _showButtonRect, 1, _showAllModulesImage);
        DrawButton(canvas, _showActiveButtonRect, 2, _showActiveImage);

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

    private void DrawButton(SKCanvas canvas, SKRect rect, int buttonIndex, SKBitmap? image)
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

        if (image is not null)
            canvas.DrawBitmap(image, rect);
    }

    private bool IsActiveButton(int buttonIndex) => buttonIndex switch
    {
        0 => _state == CommandsPanelState.Closed,
        1 => _state == CommandsPanelState.AllModules,
        2 => _state == CommandsPanelState.ActiveModules,
        _ => false,
    };

    private void DrawModuleRow(SKCanvas canvas, ModuleRowGeometry row)
    {
        // Caption: full width × ModuleCaptionHeight, bg + border, horizontal text.
        canvas.DrawRect(row.CaptionRect, _panelBgPaint);
        canvas.DrawRect(row.CaptionRect, _panelBorderPaint);

        float textX = row.CaptionRect.Left + Padding;
        float textY = row.CaptionRect.MidY + _moduleCaptionTextPaint.TextSize / 3f;
        canvas.DrawText(row.DisplayName, textX, textY, _moduleCaptionTextPaint);

        // Body: full width × remaining height, empty placeholder (no buttons).
        if (row.Opened && row.BodyRect.Height > 0)
        {
            canvas.DrawRect(row.BodyRect, _panelBgPaint);
            canvas.DrawRect(row.BodyRect, _panelBorderPaint);
        }
    }
}

/// <summary>Panel visibility state.</summary>
public enum CommandsPanelState
{
    /// <summary>Caption + all modules visible.</summary>
    AllModules,

    /// <summary>Caption + only modules with state Opened visible.</summary>
    ActiveModules,

    /// <summary>Caption only, Body hidden.</summary>
    Closed,
}

/// <summary>Per-module geometry produced during <see cref="CommandsPanel.Render"/>.</summary>
public readonly record struct ModuleRowGeometry(
    string ModuleId,
    string DisplayName,
    int Position,
    bool Opened,
    SKRect CaptionRect,
    SKRect BodyRect);
