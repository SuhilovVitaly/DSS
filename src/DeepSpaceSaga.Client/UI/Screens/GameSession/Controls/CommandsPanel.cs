using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Commands Panel (top-left) — the fixed command-group widget.
/// A Caption (360×32) with a Hide/Show toggle button (26×26) + a list of
/// fixed command-panel groups (Navigation, Maneuver, Engine, Space Control),
/// each caption 360×36 with a fixed-height body (same size for every panel,
/// regardless of how many commands it holds).
/// </summary>
public sealed class CommandsPanel
{
    public const float PanelWidth = 360f;
    public const float CaptionHeight = 32f;
    public const float PanelCaptionHeight = 36f;

    /// <summary>Fixed body height for every panel (restored pre-ТЗ-04 constant: 200f row − 36f caption).</summary>
    public const float PanelBodyHeight = 164f;

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

    /// <summary>
    /// Fixed command-panel groups, in display order. Each panel's CommandTypeIds
    /// are rendered strictly in the declared order — this is not derived from
    /// installed modules.
    /// </summary>
    public static readonly ImmutableArray<CommandPanelDefinition> Panels = ImmutableArray.Create(
        new CommandPanelDefinition("Navigation", ImmutableArray.Create(
            NavigationComputerCommandTypes.Dock,
            NavigationComputerCommandTypes.StationsList,
            ShipEngineCommandTypes.Orbit,
            ShipEngineCommandTypes.SpeedSynchronization,
            ShipEngineCommandTypes.DirectionSynchronization)),
        new CommandPanelDefinition("Maneuver", ImmutableArray.Create(
            ShipEngineCommandTypes.MaintainCourse,
            ShipEngineCommandTypes.TurnLeftStep,
            ShipEngineCommandTypes.TurnRightStep,
            ShipEngineCommandTypes.TurnLeftUntilCancel,
            ShipEngineCommandTypes.TurnRightUntilCancel)),
        new CommandPanelDefinition("Engine", ImmutableArray.Create(
            ShipEngineCommandTypes.Accelerate,
            ShipEngineCommandTypes.Brake,
            ShipEngineCommandTypes.MaintainSpeed)),
        new CommandPanelDefinition("Space Control", ImmutableArray.Create(
            ScannerCommandTypes.GeneralScan,
            ScannerCommandTypes.StructuralScan)));

    /// <summary>
    /// Per-command icon files under Images/UI/GameSessionScreenUI/commands-panel/.
    /// A command without an entry (or whose file is missing on disk) falls back to
    /// the plain text-label button. Icons are drawn at a fixed 32×32 (before UI
    /// scaling), centered in the button — source assets may be higher-resolution
    /// (e.g. 64×64) for crisp downscaling.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> CommandIconFileNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ShipEngineCommandTypes.TurnLeftStep] = "command-panel-button-turn-left.png",
            [ShipEngineCommandTypes.TurnLeftUntilCancel] = "command-panel-button-turn-left-continuous.png",
            [ShipEngineCommandTypes.TurnRightStep] = "command-panel-button-turn-right.png",
            [ShipEngineCommandTypes.TurnRightUntilCancel] = "command-panel-button-turn-right-continuous.png",
        };

    private const float CommandIconSize = 32f;

    private SKRect _hideShowButtonRect;

    private int _hoveredButtonIndex = -1;  // 0=toggle, -1=none
    private int _pressedButtonIndex = -1;

    private int _hoveredCommandButtonIndex = -1;  // row-major ordinal over rendered command buttons
    private int _pressedCommandButtonIndex = -1;
    private int _commandButtonDrawOrdinal;

    // ── Panel state (in-memory, per session) ─────────────────────
    private readonly Dictionary<string, bool> _panelOpenedByName = new(StringComparer.Ordinal);

    // ── Layout cache (built in Render, read by test seams) ──────
    private readonly List<CommandPanelGeometry> _panelRows = [];
    private readonly List<(string Label, CommandButtonGeometry Button)> _commandButtons = [];
    private readonly List<CommandButtonGeometry> _allCommandButtons = [];
    private SKRect _captionRect;
    private SKRect _bodyRect;

    // ── Hooks (ТЗ-04, injected by the screen) ───────────────────
    private readonly Func<string, bool> _isCommandEnabled;
    private readonly Action<string> _commandClicked;

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
    private readonly SKPaint _commandBtnIconPaint;

    // ── Images ──────────────────────────────────────────────────
    private readonly SKBitmap? _captionBackgroundImage;
    private readonly SKBitmap? _moduleCaptionBackgroundImage;
    private readonly SKBitmap? _moduleBodyBackgroundImage;
    private readonly SKBitmap? _hideImage;
    private readonly SKBitmap? _showImage;
    private readonly Dictionary<string, SKBitmap?> _commandIcons = new(StringComparer.Ordinal);

    private CommandsPanelState _state = CommandsPanelState.AllPanels;
    private CommandsPanelState _previousNonClosedState = CommandsPanelState.AllPanels;

    /// <summary>
    /// <paramref name="isCommandEnabled"/> decides per-command button enablement
    /// (default: all enabled); <paramref name="commandClicked"/> receives the
    /// commandType when an enabled button is clicked (default: no-op).
    /// </summary>
    public CommandsPanel(
        Func<string, bool>? isCommandEnabled = null,
        Action<string>? commandClicked = null)
    {
        _isCommandEnabled = isCommandEnabled ?? (_ => true);
        _commandClicked = commandClicked ?? (_ => { });

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
        _commandBtnIconPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };

        var boldTypeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) ?? typeface;
        _moduleCaptionTextPaint = new SKPaint { Color = new SKColor(180, 180, 180), TextSize = 14f, IsAntialias = true, Typeface = boldTypeface };

        _captionBackgroundImage = LoadImage("Images/UI/GameSessionScreenUI/command-panel-caption-background.png");
        _moduleCaptionBackgroundImage = LoadImage("Images/UI/GameSessionScreenUI/module-panel-caption-background.png");
        _moduleBodyBackgroundImage = LoadImage("Images/UI/GameSessionScreenUI/module-panel-body-background.png");
        _hideImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-hide.png");
        _showImage = LoadImage("Images/UI/GameSessionScreenUI/title-bar/title-bar-button-show.png");

        foreach (var (commandTypeId, fileName) in CommandIconFileNames)
            _commandIcons[commandTypeId] = LoadCommandIcon($"Images/UI/GameSessionScreenUI/commands-panel/{fileName}");
    }

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>
    /// Decodes and downscales a command icon to the fixed <see cref="CommandIconSize"/>
    /// (32×32, before UI scaling) — source assets may be higher-resolution for a
    /// crisp result. Mirrors <c>GameSessionScreen.LoadButtonIcon</c>.
    /// </summary>
    private static SKBitmap? LoadCommandIcon(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            using var bitmap = SKBitmap.Decode(path);
            return bitmap?.Resize(new SKSizeI((int)CommandIconSize, (int)CommandIconSize), SKFilterQuality.High);
        }
        catch (IOException)
        {
            return null;
        }
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

    /// <summary>True if an icon file for <paramref name="commandTypeId"/> was found and decoded at construction time.</summary>
    internal bool HasLoadedIconFor(string commandTypeId) =>
        _commandIcons.TryGetValue(commandTypeId, out var bitmap) && bitmap is not null;

    public IReadOnlyList<CommandPanelGeometry> CommandPanelRows => _panelRows;
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

        // Panel captions — toggle per-panel Opened/Closed.
        foreach (var row in _panelRows)
        {
            if (row.CaptionRect.Contains(x, y))
            {
                string name = row.Name;
                bool wasOpened = !_panelOpenedByName.TryGetValue(name, out bool o) || o;
                _panelOpenedByName[name] = !wasOpened;
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
                    _commandClicked(_commandButtons[i].Button.CommandTypeId);
                }

                return true;
            }
        }

        // Panel bodies (only opened rows) — consumed, no action.
        foreach (var row in _panelRows)
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
               _panelRows.Any(r => r.CaptionRect.Contains(x, y));
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

        _panelRows.Clear();
        _commandButtons.Clear();
        _allCommandButtons.Clear();

        float rowY = _captionRect.Bottom;

        if (_state != CommandsPanelState.Closed)
        {
            var safeModules = modules is ImmutableArray<InstalledModuleSnapshot> { IsDefault: false } arr
                ? arr
                : ImmutableArray<InstalledModuleSnapshot>.Empty;

            foreach (var panel in Panels)
            {
                bool opened = !_panelOpenedByName.TryGetValue(panel.Name, out bool o) || o;

                if (_state == CommandsPanelState.ActivePanels && !opened)
                    continue;

                var captionRect = new SKRect(
                    Margin, rowY,
                    Margin + PanelWidth, rowY + PanelCaptionHeight);

                var bodyRect = opened
                    ? new SKRect(
                        Margin, rowY + PanelCaptionHeight,
                        Margin + PanelWidth, rowY + PanelCaptionHeight + PanelBodyHeight)
                    : SKRect.Empty;

                var buttons = opened
                    ? BuildCommandButtons(panel, safeModules, bodyRect)
                    : ImmutableArray<CommandButtonGeometry>.Empty;

                _panelRows.Add(new CommandPanelGeometry(
                    panel.Name, opened, captionRect, bodyRect, buttons));

                rowY += opened ? (PanelCaptionHeight + PanelBodyHeight) : PanelCaptionHeight;
            }
        }

        float bodyBottom = rowY;
        _bodyRect = new SKRect(
            Margin, _captionRect.Bottom,
            Margin + PanelWidth, bodyBottom);

        _commandButtonDrawOrdinal = 0;
        DrawCaption(canvas);
        foreach (var row in _panelRows)
            DrawCommandPanel(canvas, row);
    }

    /// <summary>
    /// One button per the panel's fixed CommandTypeIds, in declaration order, laid
    /// out in a top-down grid of 4 columns. The label comes from the first
    /// installed module (ordered by Position) exposing matching Commands metadata
    /// (fallback: the type id); enablement from the injected hook.
    /// </summary>
    private ImmutableArray<CommandButtonGeometry> BuildCommandButtons(
        CommandPanelDefinition panel, IReadOnlyList<InstalledModuleSnapshot> modules, SKRect bodyRect)
    {
        var builder = ImmutableArray.CreateBuilder<CommandButtonGeometry>(panel.CommandTypeIds.Length);
        for (int i = 0; i < panel.CommandTypeIds.Length; i++)
        {
            string commandTypeId = panel.CommandTypeIds[i];
            var metadata = FindCommandMetadataAcrossModules(modules, commandTypeId);
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
            _commandButtons.Add((label, geometry));
            _allCommandButtons.Add(geometry);
        }

        return builder.MoveToImmutable();
    }

    /// <summary>
    /// Finds command metadata for <paramref name="commandTypeId"/> across all
    /// installed modules, ordered by Position ascending, returning the first
    /// match — consistent with the click-time module resolution.
    /// </summary>
    private static ModuleCommandSnapshot? FindCommandMetadataAcrossModules(
        IReadOnlyList<InstalledModuleSnapshot> modules, string commandTypeId)
    {
        foreach (var module in modules.OrderBy(m => m.Position))
        {
            if (module.Commands.IsDefaultOrEmpty)
                continue;

            foreach (var command in module.Commands)
            {
                if (command.CommandTypeId == commandTypeId)
                    return command;
            }
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
        canvas.DrawText("Command Panels", textX, textY, _titlePaint);
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

    private void DrawCommandPanel(SKCanvas canvas, CommandPanelGeometry row)
    {
        if (_moduleCaptionBackgroundImage is not null)
            canvas.DrawBitmap(_moduleCaptionBackgroundImage, row.CaptionRect);
        else
            canvas.DrawRect(row.CaptionRect, _panelBgPaint);

        float textX = row.CaptionRect.Left + 40f;
        float textY = row.CaptionRect.MidY + _moduleCaptionTextPaint.TextSize / 3f;
        canvas.DrawText(row.Name, textX, textY, _moduleCaptionTextPaint);

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
        var (label, button) = _commandButtons[index];

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

        var icon = _commandIcons.GetValueOrDefault(button.CommandTypeId);
        if (icon is not null)
        {
            float iconX = button.Rect.MidX - CommandIconSize / 2f;
            float iconY = button.Rect.MidY - CommandIconSize / 2f;
            var iconRect = new SKRect(iconX, iconY, iconX + CommandIconSize, iconY + CommandIconSize);

            byte iconAlpha = button.Enabled ? (byte)255 : (byte)110;
            _commandBtnIconPaint.Color = new SKColor(255, 255, 255, iconAlpha);
            canvas.DrawBitmap(icon, iconRect, _commandBtnIconPaint);
        }
        else
        {
            var textPaint = button.Enabled ? _commandBtnTextPaint : _commandBtnTextDisabledPaint;
            string displayLabel = TruncateLabel(label, textPaint, button.Rect.Width - 8f);
            float textY = button.Rect.MidY + textPaint.TextSize / 3f;
            canvas.DrawText(displayLabel, button.Rect.MidX, textY, textPaint);
        }
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
    AllPanels,
    ActivePanels,
    Closed,
}

/// <summary>
/// A fixed, gameplay-grouped set of command types shown as one Commands Panel
/// row. Not derived from installed modules — the panel/command order is fixed
/// by design (see ТЗ "Command Panels по наборам команд").
/// </summary>
public sealed record CommandPanelDefinition(string Name, ImmutableArray<string> CommandTypeIds);

public readonly record struct CommandPanelGeometry(
    string Name,
    bool Opened,
    SKRect CaptionRect,
    SKRect BodyRect,
    ImmutableArray<CommandButtonGeometry> Buttons);

public readonly record struct CommandButtonGeometry(
    string CommandTypeId,
    SKRect Rect,
    bool Enabled);
