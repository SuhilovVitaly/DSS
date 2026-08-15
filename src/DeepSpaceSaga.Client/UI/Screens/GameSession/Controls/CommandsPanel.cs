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

    // ── Command button grid (ТЗ-04) ─────────────────────────────
    public const float CommandButtonColumns = 4f;
    public const float CommandButtonHeight = 32f;
    public const float CommandButtonGap = 4f;
    public const float BodyPaddingX = 6f;
    public const float BodyPaddingY = 6f;

    /// <summary>84 = (360 − 2×6 padding − 3×4 gap) / 4 columns.</summary>
    public const float CommandButtonWidth =
        (PanelWidth - 2 * BodyPaddingX - (CommandButtonColumns - 1) * CommandButtonGap) / CommandButtonColumns;

    private SKRect _hideShowButtonRect;

    private int _hoveredButtonIndex = -1;  // 0=toggle, -1=none
    private int _pressedButtonIndex = -1;

    private int _hoveredCommandButtonIndex = -1;  // row-major ordinal over rendered command buttons
    private int _pressedCommandButtonIndex = -1;
    private int _commandButtonDrawOrdinal;

    // ── Module state (in-memory, per session) ───────────────────
    private readonly Dictionary<string, bool> _moduleOpenedById = new(StringComparer.Ordinal);

    // ── Layout cache (built in Render, read by test seams) ──────
    private readonly List<ModuleRowGeometry> _moduleRows = [];
    private readonly List<(string ModuleId, string Label, CommandButtonGeometry Button)> _commandButtons = [];
    private readonly List<CommandButtonGeometry> _allCommandButtons = [];
    private SKRect _captionRect;
    private SKRect _bodyRect;

    // ── Hooks (ТЗ-04, injected by the screen) ───────────────────
    private readonly Func<string, bool> _isCommandEnabled;
    private readonly Action<string, string> _commandClicked;

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

    // Command button paints (mirror the bottom engine panel palette)
    private readonly SKPaint _commandBtnNormalPaint;
    private readonly SKPaint _commandBtnHoverPaint;
    private readonly SKPaint _commandBtnPressedPaint;
    private readonly SKPaint _commandBtnDisabledPaint;
    private readonly SKPaint _commandBtnTextPaint;
    private readonly SKPaint _commandBtnTextDisabledPaint;
    private readonly SKPaint _commandBtnBorderPaint;

    // ── Images ──────────────────────────────────────────────────
    private readonly SKBitmap? _captionBackgroundImage;
    private readonly SKBitmap? _moduleCaptionBackgroundImage;
    private readonly SKBitmap? _moduleBodyBackgroundImage;
    private readonly SKBitmap? _hideImage;
    private readonly SKBitmap? _showImage;

    private CommandsPanelState _state = CommandsPanelState.AllModules;
    private CommandsPanelState _previousNonClosedState = CommandsPanelState.AllModules;

    /// <summary>
    /// <paramref name="isCommandEnabled"/> decides per-command button enablement
    /// (default: all enabled); <paramref name="commandClicked"/> receives
    /// (moduleId, commandType) when an enabled button is clicked (default: no-op).
    /// </summary>
    public CommandsPanel(
        Func<string, bool>? isCommandEnabled = null,
        Action<string, string>? commandClicked = null)
    {
        _isCommandEnabled = isCommandEnabled ?? (_ => true);
        _commandClicked = commandClicked ?? ((_, _) => { });

        var typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;

        _panelBgPaint = new SKPaint { Color = new SKColor(0, 0, 0, 200), Style = SKPaintStyle.Fill };
        _panelBorderPaint = new SKPaint { Color = new SKColor(42, 42, 42), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _titlePaint = new SKPaint { Color = new SKColor(210, 210, 210), TextSize = 13f, IsAntialias = true, Typeface = typeface };

        _btnNormalPaint = new SKPaint { Color = new SKColor(35, 35, 35, 220), Style = SKPaintStyle.Fill };
        _btnHoverPaint = new SKPaint { Color = new SKColor(55, 55, 55, 230), Style = SKPaintStyle.Fill };
        _btnPressedPaint = new SKPaint { Color = new SKColor(70, 70, 70, 240), Style = SKPaintStyle.Fill };
        _btnActivePaint = new SKPaint { Color = new SKColor(50, 80, 50, 230), Style = SKPaintStyle.Fill };
        _btnBorderPaint = new SKPaint { Color = new SKColor(80, 80, 80), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

        _commandBtnNormalPaint = new SKPaint { Color = new SKColor(26, 28, 31), Style = SKPaintStyle.Fill };
        _commandBtnHoverPaint = new SKPaint { Color = new SKColor(39, 45, 51), Style = SKPaintStyle.Fill };
        _commandBtnPressedPaint = new SKPaint { Color = new SKColor(58, 75, 67), Style = SKPaintStyle.Fill };
        _commandBtnDisabledPaint = new SKPaint { Color = new SKColor(18, 18, 18, 190), Style = SKPaintStyle.Fill };
        _commandBtnTextPaint = new SKPaint { Color = new SKColor(210, 218, 214), TextSize = 10f, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };
        _commandBtnTextDisabledPaint = new SKPaint { Color = new SKColor(96, 96, 96), TextSize = 10f, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };
        _commandBtnBorderPaint = new SKPaint { Color = new SKColor(80, 80, 80), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

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

    public int HoveredCommandButtonIndex => _hoveredCommandButtonIndex;
    public int PressedCommandButtonIndex => _pressedCommandButtonIndex;

    public IReadOnlyList<ModuleRowGeometry> ModuleRows => _moduleRows;
    public IReadOnlyList<CommandButtonGeometry> AllCommandButtons => _allCommandButtons;

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

        // Command buttons (opened rows only, row-major over the render order).
        // Enabled hit → pressed + fire the command; disabled hit → consumed, no action.
        for (int i = 0; i < _commandButtons.Count; i++)
        {
            if (_commandButtons[i].Button.Rect.Contains(x, y))
            {
                if (_commandButtons[i].Button.Enabled)
                {
                    _pressedCommandButtonIndex = i;
                    _commandClicked(_commandButtons[i].ModuleId, _commandButtons[i].Button.CommandTypeId);
                }

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

        _hoveredCommandButtonIndex = -1;
        for (int i = 0; i < _commandButtons.Count; i++)
        {
            if (_commandButtons[i].Button.Rect.Contains(x, y))
            {
                _hoveredCommandButtonIndex = i;
                break;
            }
        }

        return _hoveredButtonIndex >= 0 ||
               _hoveredCommandButtonIndex >= 0 ||
               _moduleRows.Any(r => r.CaptionRect.Contains(x, y));
    }

    public void OnMouseUp(float x, float y)
    {
        _pressedButtonIndex = -1;
        _pressedCommandButtonIndex = -1;
    }

    // ── Render ──────────────────────────────────────────────────

    public void Render(SKCanvas canvas, IReadOnlyList<InstalledModuleSnapshot> modules)
    {
        LayoutButtons();

        _captionRect = new SKRect(
            Margin, Margin,
            Margin + PanelWidth, Margin + CaptionHeight);

        _moduleRows.Clear();
        _commandButtons.Clear();
        _allCommandButtons.Clear();

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

                var buttons = opened
                    ? BuildCommandButtons(mod, bodyRect)
                    : ImmutableArray<CommandButtonGeometry>.Empty;

                _moduleRows.Add(new ModuleRowGeometry(
                    mod.ModuleId, mod.DisplayName, mod.Position, opened, captionRect, bodyRect, buttons));

                rowY += opened ? ModuleRowHeight : ModuleCaptionHeight;
            }
        }

        float bodyBottom = rowY;
        _bodyRect = new SKRect(
            Margin, _captionRect.Bottom,
            Margin + PanelWidth, bodyBottom);

        _commandButtonDrawOrdinal = 0;
        DrawCaption(canvas);
        foreach (var row in _moduleRows)
            DrawModuleRow(canvas, row);
    }

    /// <summary>
    /// One button per CommandTypeIds entry, in declaration order, laid out in a
    /// top-down grid of 4 columns. The label comes from the snapshot's Commands
    /// metadata (fallback: the type id); enablement from the injected hook.
    /// </summary>
    private ImmutableArray<CommandButtonGeometry> BuildCommandButtons(
        InstalledModuleSnapshot module, SKRect bodyRect)
    {
        var builder = ImmutableArray.CreateBuilder<CommandButtonGeometry>(module.CommandTypeIds.Length);
        for (int i = 0; i < module.CommandTypeIds.Length; i++)
        {
            string commandTypeId = module.CommandTypeIds[i];
            var metadata = FindCommandMetadata(module, commandTypeId);
            string label = metadata?.DisplayName ?? commandTypeId;

            int col = i % (int)CommandButtonColumns;
            int rowIdx = i / (int)CommandButtonColumns;
            float x = bodyRect.Left + BodyPaddingX + col * (CommandButtonWidth + CommandButtonGap);
            float y = bodyRect.Top + BodyPaddingY + rowIdx * (CommandButtonHeight + CommandButtonGap);

            var geometry = new CommandButtonGeometry(
                commandTypeId,
                new SKRect(x, y, x + CommandButtonWidth, y + CommandButtonHeight),
                _isCommandEnabled(commandTypeId));

            builder.Add(geometry);
            _commandButtons.Add((module.ModuleId, label, geometry));
            _allCommandButtons.Add(geometry);
        }

        return builder.MoveToImmutable();
    }

    private static ModuleCommandSnapshot? FindCommandMetadata(InstalledModuleSnapshot module, string commandTypeId)
    {
        if (module.Commands.IsDefaultOrEmpty)
            return null;

        foreach (var command in module.Commands)
        {
            if (command.CommandTypeId == commandTypeId)
                return command;
        }

        return null;
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

            foreach (var button in row.Buttons)
            {
                // Rows are drawn in the same order their buttons were built, so the
                // running ordinal matches the flat _commandButtons index used by the
                // hover/pressed seams and the label lookup.
                DrawCommandButton(canvas, _commandButtonDrawOrdinal++);
            }
        }
    }

    private void DrawCommandButton(SKCanvas canvas, int index)
    {
        var (_, label, button) = _commandButtons[index];

        SKPaint fill;
        if (button.Enabled)
        {
            if (index == _pressedCommandButtonIndex && index == _hoveredCommandButtonIndex)
                fill = _commandBtnPressedPaint;
            else if (index == _hoveredCommandButtonIndex)
                fill = _commandBtnHoverPaint;
            else
                fill = _commandBtnNormalPaint;
        }
        else
        {
            fill = _commandBtnDisabledPaint;
        }

        canvas.DrawRect(button.Rect, fill);
        canvas.DrawRect(button.Rect, _commandBtnBorderPaint);

        var textPaint = button.Enabled ? _commandBtnTextPaint : _commandBtnTextDisabledPaint;
        string displayLabel = TruncateLabel(label, textPaint, button.Rect.Width - 8f);
        float textY = button.Rect.MidY + textPaint.TextSize / 3f;
        canvas.DrawText(displayLabel, button.Rect.MidX, textY, textPaint);
    }

    private static string TruncateLabel(string label, SKPaint paint, float maxWidth)
    {
        if (paint.MeasureText(label) <= maxWidth)
            return label;

        for (int len = label.Length - 1; len > 0; len--)
        {
            string candidate = label[..len] + "…";
            if (paint.MeasureText(candidate) <= maxWidth)
                return candidate;
        }

        return "…";
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
    SKRect BodyRect,
    ImmutableArray<CommandButtonGeometry> Buttons);

public readonly record struct CommandButtonGeometry(
    string CommandTypeId,
    SKRect Rect,
    bool Enabled);
