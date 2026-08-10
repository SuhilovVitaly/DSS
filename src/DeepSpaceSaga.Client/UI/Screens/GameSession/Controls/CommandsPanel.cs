using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Commands Panel (top-left) — the module-addressed command widget.
/// A Caption (360×32) with a Hide/Show toggle button (26×26) + a list of
/// module rows (caption 360×36 + body 360×164) for active modules.
/// </summary>
public sealed class CommandsPanel
{
    public const float PanelWidth = 360f;
    public const float CaptionHeight = 32f;
    public const float ModuleRowHeight = 200f;
    public const float ModuleCaptionHeight = 36f;

    private const float Margin = 8f;
    private const float Padding = 6f;

    // ── Button geometry ─────────────────────────────────────────
    public const float ButtonSize = 26f;
    public const float ButtonLeftPadding = 2f;

    private SKRect _hideShowButtonRect;

    private int _hoveredButtonIndex = -1;  // 0=toggle, -1=none
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

    private readonly SKPaint _moduleCaptionTextPaint;

    // ── Images ──────────────────────────────────────────────────
    private readonly SKBitmap? _captionBackgroundImage;
    private readonly SKBitmap? _moduleCaptionBackgroundImage;
    private readonly SKBitmap? _moduleBodyBackgroundImage;
    private readonly SKBitmap? _hideImage;
    private readonly SKBitmap? _showImage;

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

        var boldTypeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) ?? typeface;
        _moduleCaptionTextPaint = new SKPaint { Color = new SKColor(180, 180, 180), TextSize = 14f, IsAntialias = true, Typeface = boldTypeface };

        _captionBackgroundImage = LoadImage("Images/UI/GameSessionScreenUI/command-panel-caption-background.png");
        _moduleCaptionBackgroundImage = LoadImage("Images/UI/GameSessionScreenUI/module-panel-caption-background.png");
        _moduleBodyBackgroundImage = LoadImage("Images/UI/GameSessionScreenUI/module-panel-body-background.png");
        _hideImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-hide.png");
        _showImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-show.png");
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

    public int HoveredButtonIndex => _hoveredButtonIndex;
    public int PressedButtonIndex => _pressedButtonIndex;

    public IReadOnlyList<ModuleRowGeometry> ModuleRows => _moduleRows;

    // ── Layout helpers ──────────────────────────────────────────

    private float ButtonTop => Margin + 2f;

    private void LayoutButtons()
    {
        float x = Margin + ButtonLeftPadding;
        float y = ButtonTop;
        _hideShowButtonRect = new SKRect(x, y, x + ButtonSize, y + ButtonSize);
    }

    // ── Input ───────────────────────────────────────────────────

    public bool OnMouseDown(float x, float y)
    {
        // Toggle button — hide (save state) or show (restore).
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

        // Module captions — toggle per-module Opened/Closed.
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

        // Module bodies (only opened rows) — consumed, no action.
        foreach (var row in _moduleRows)
        {
            if (row is { Opened: true, BodyRect: var br } && br.Contains(x, y))
                return true;
        }

        // Panel caption / body — consumed, no action.
        return _captionRect.Contains(x, y) || _bodyRect.Contains(x, y);
    }

    public bool OnMouseMove(float x, float y)
    {
        _hoveredButtonIndex = _hideShowButtonRect.Contains(x, y) ? 0 : -1;
        return _hoveredButtonIndex >= 0 || _moduleRows.Any(r => r.CaptionRect.Contains(x, y));
    }

    public void OnMouseUp(float x, float y)
    {
        _pressedButtonIndex = -1;
    }

    // ── Render ──────────────────────────────────────────────────

    public void Render(SKCanvas canvas, IReadOnlyList<InstalledModuleSnapshot> modules)
    {
        LayoutButtons();

        _captionRect = new SKRect(
            Margin, Margin,
            Margin + PanelWidth, Margin + CaptionHeight);

        _moduleRows.Clear();

        float rowY = _captionRect.Bottom;

        if (_state != CommandsPanelState.Closed)
        {
            var safeModules = modules is ImmutableArray<InstalledModuleSnapshot> { IsDefault: false } arr
                ? arr
                : ImmutableArray<InstalledModuleSnapshot>.Empty;

            var activeModules = safeModules
                .Where(m => m.CommandTypeIds.Length > 0)
                .OrderBy(m => m.ModuleTypeId == "module.engine.basic" ? -1 : 0)
                .ThenBy(m => m.Position)
                .ToList();

            var activeIds = new HashSet<string>(activeModules.Select(m => m.ModuleId), StringComparer.Ordinal);
            foreach (string staleId in _moduleOpenedById.Keys.Except(activeIds).ToList())
                _moduleOpenedById.Remove(staleId);

            foreach (var mod in activeModules)
            {
                bool opened = !_moduleOpenedById.TryGetValue(mod.ModuleId, out bool o) || o;

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

        float bodyBottom = rowY;
        _bodyRect = new SKRect(
            Margin, _captionRect.Bottom,
            Margin + PanelWidth, bodyBottom);

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

        // HideShow toggle: hide icon when open, show icon when closed.
        var hideShowImage = _state == CommandsPanelState.Closed ? _showImage : _hideImage;
        DrawButton(canvas, _hideShowButtonRect, 0, hideShowImage);

        float textX = _hideShowButtonRect.Right + Padding + 2f;
        float textY = _captionRect.MidY + _titlePaint.TextSize / 3f;
        canvas.DrawText("Modules", textX, textY, _titlePaint);
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

    private bool IsActiveButton(int buttonIndex) => buttonIndex == 0 && _state == CommandsPanelState.Closed;

    private void DrawModuleRow(SKCanvas canvas, ModuleRowGeometry row)
    {
        if (_moduleCaptionBackgroundImage is not null)
            canvas.DrawBitmap(_moduleCaptionBackgroundImage, row.CaptionRect);
        else
            canvas.DrawRect(row.CaptionRect, _panelBgPaint);

        float textX = row.CaptionRect.Left + 40f;
        float textY = row.CaptionRect.MidY + _moduleCaptionTextPaint.TextSize / 3f;
        canvas.DrawText(row.DisplayName, textX, textY, _moduleCaptionTextPaint);

        if (row.Opened && row.BodyRect.Height > 0)
        {
            if (_moduleBodyBackgroundImage is not null)
                canvas.DrawBitmap(_moduleBodyBackgroundImage, row.BodyRect);
            else
                canvas.DrawRect(row.BodyRect, _panelBgPaint);
        }
    }
}

public enum CommandsPanelState
{
    AllModules,
    ActiveModules,
    Closed,
}

public readonly record struct ModuleRowGeometry(
    string ModuleId,
    string DisplayName,
    int Position,
    bool Opened,
    SKRect CaptionRect,
    SKRect BodyRect);
