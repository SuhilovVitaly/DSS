using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
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
    private const string XenonAssetsPath = "Images/UI/Themes/Xenon/GameSession/CommandPanels";
    private const float XenonBodySliceInset = 12f;
    private const float XenonButtonSliceInset = 8f;
    private const float ModuleTitleOffsetY = -4f;
    private const float CaptionTitleFontSize = 16f;

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
    public const float CommandButtonHeight = 48f;
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
    /// the plain text-label button. Icons are drawn bare (no button chrome) at a
    /// fixed 48×48 (before UI scaling), centered on the command's clickable area —
    /// source assets may be higher-resolution (e.g. 64×64) for crisp downscaling.
    /// Each file name matches its command type id (e.g. <see cref="ShipEngineCommandTypes.Brake"/>
    /// → "engine.brake.png"). Each entry has a matching ".active" file (e.g.
    /// "engine.brake.png" / "engine.brake.active.png") swapped in on hover, in place
    /// of a background highlight; if the active file is missing the normal icon is
    /// kept on hover. The clickable area, hover/press tracking and cursor-over-interactive
    /// signalling are unchanged from a regular button (see <see cref="OnMouseMove"/>).
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> CommandIconFileNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ShipEngineCommandTypes.Accelerate] = "engine.accelerate.png",
            [ShipEngineCommandTypes.Brake] = "engine.brake.png",
            [ShipEngineCommandTypes.MaintainCourse] = "engine.maintainCourse.png",
            [ShipEngineCommandTypes.MaintainSpeed] = "engine.maintainSpeed.png",
            [ShipEngineCommandTypes.Orbit] = "engine.orbit.png",
            [ShipEngineCommandTypes.TurnLeftStep] = "engine.turnLeftStep.png",
            [ShipEngineCommandTypes.TurnLeftUntilCancel] = "engine.turnLeftUntilCancel.png",
            [ShipEngineCommandTypes.TurnRightStep] = "engine.turnRightStep.png",
            [ShipEngineCommandTypes.TurnRightUntilCancel] = "engine.turnRightUntilCancel.png",
            [ShipEngineCommandTypes.SpeedSynchronization] = "engine.speedSynchronization.png",
            [ShipEngineCommandTypes.DirectionSynchronization] = "engine.directionSynchronization.png",
            [NavigationComputerCommandTypes.Dock] = "navigation.dock.png",
            [NavigationComputerCommandTypes.StationsList] = "navigation.stationsList.png",
            [ScannerCommandTypes.GeneralScan] = "scanner.generalScan.png",
            [ScannerCommandTypes.StructuralScan] = "scanner.structuralScan.png",
        };

    private const float CommandIconSize = 48f;

    private readonly record struct CommandIconPair(SKBitmap? Normal, SKBitmap? Active);

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
    private readonly SKBitmap? _hideHoverImage;
    private readonly SKBitmap? _hidePressedImage;
    private readonly SKBitmap? _showHoverImage;
    private readonly SKBitmap? _showPressedImage;
    private readonly SKBitmap? _commandButtonNormalImage;
    private readonly SKBitmap? _commandButtonHoverImage;
    private readonly SKBitmap? _commandButtonPressedImage;
    private readonly Dictionary<string, CommandIconPair> _commandIcons = new(StringComparer.Ordinal);

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

        var typeface = XenonStyle.TypefaceRegular;

        _panelBgPaint = new SKPaint
        {
            Color = new SKColor(2, 16, 24),
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src
        };
        _panelBorderPaint = new SKPaint { Color = new SKColor(42, 42, 42), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _titlePaint = new SKPaint
        {
            Color = XenonStyle.CyanBright,
            TextSize = CaptionTitleFontSize,
            IsAntialias = true,
            Typeface = XenonStyle.TypefaceSemibold
        };

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

        _moduleCaptionTextPaint = new SKPaint { Color = XenonStyle.CyanBright, TextSize = 14f, IsAntialias = true, Typeface = XenonStyle.TypefaceSemibold };

        _captionBackgroundImage = LoadImage($"{XenonAssetsPath}/header-strip.png");
        _moduleCaptionBackgroundImage = LoadImage($"{XenonAssetsPath}/header-strip.png");
        _moduleBodyBackgroundImage = LoadImage($"{XenonAssetsPath}/module-body.png");
        _hideImage = LoadImage($"{XenonAssetsPath}/collapse-normal.png");
        _hideHoverImage = LoadImage($"{XenonAssetsPath}/collapse-hover.png");
        _hidePressedImage = LoadImage($"{XenonAssetsPath}/collapse-pressed.png");
        _showImage = LoadImage($"{XenonAssetsPath}/expand-normal.png");
        _showHoverImage = LoadImage($"{XenonAssetsPath}/expand-hover.png");
        _showPressedImage = LoadImage($"{XenonAssetsPath}/expand-pressed.png");
        _commandButtonNormalImage = LoadImage($"{XenonAssetsPath}/command-normal.png");
        _commandButtonHoverImage = LoadImage($"{XenonAssetsPath}/command-hover.png");
        _commandButtonPressedImage = LoadImage($"{XenonAssetsPath}/command-pressed.png");

        foreach (var (commandTypeId, fileName) in CommandIconFileNames)
        {
            string activeFileName = Path.ChangeExtension(fileName, null) + ".active" + Path.GetExtension(fileName);
            _commandIcons[commandTypeId] = new CommandIconPair(
                LoadCommandIcon($"Images/UI/GameSessionScreenUI/commands-panel/{fileName}"),
                LoadCommandIcon($"Images/UI/GameSessionScreenUI/commands-panel/{activeFileName}"));
        }
    }

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>
    /// Decodes and downscales a command icon to the fixed <see cref="CommandIconSize"/>
    /// (48×48, before UI scaling) — source assets may be higher-resolution for a
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

    /// <summary>True if the normal-state icon file for <paramref name="commandTypeId"/> was found and decoded at construction time.</summary>
    internal bool HasLoadedIconFor(string commandTypeId) =>
        _commandIcons.TryGetValue(commandTypeId, out var pair) && pair.Normal is not null;

    /// <summary>True if the hover-state ("-active") icon file for <paramref name="commandTypeId"/> was found and decoded at construction time.</summary>
    internal bool HasLoadedActiveIconFor(string commandTypeId) =>
        _commandIcons.TryGetValue(commandTypeId, out var pair) && pair.Active is not null;

    /// <summary>True when the complete Xenon command-panel chrome was found.</summary>
    internal bool HasLoadedXenonChrome =>
        _captionBackgroundImage is not null &&
        _moduleCaptionBackgroundImage is not null &&
        _moduleBodyBackgroundImage is not null &&
        _hideImage is not null && _hideHoverImage is not null && _hidePressedImage is not null &&
        _showImage is not null && _showHoverImage is not null && _showPressedImage is not null &&
        _commandButtonNormalImage is not null &&
        _commandButtonHoverImage is not null &&
        _commandButtonPressedImage is not null;

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
        canvas.DrawRect(_captionRect, _panelBgPaint);
        if (_captionBackgroundImage is not null)
            NinePatch.Draw(canvas, _captionBackgroundImage, _captionRect, 6f);

        // HideShow toggle: hide icon when open, show icon when closed.
        var hideShowImage = ResolveHideShowImage();
        DrawButton(canvas, _hideShowButtonRect, 0, hideShowImage);

        float textX = _hideShowButtonRect.Right + Padding + 2f;
        float textY = _captionRect.MidY + _titlePaint.TextSize / 3f;
        canvas.DrawText("Command Panels", textX, textY, _titlePaint);
    }

    private void DrawButton(SKCanvas canvas, SKRect rect, int buttonIndex, SKBitmap? image)
    {
        if (image is not null)
        {
            canvas.DrawBitmap(image, rect);
            return;
        }

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
    }

    private SKBitmap? ResolveHideShowImage()
    {
        bool pressed = _pressedButtonIndex == 0 && _hoveredButtonIndex == 0;
        bool hovered = _hoveredButtonIndex == 0;

        if (_state == CommandsPanelState.Closed)
            return pressed ? _showPressedImage : hovered ? _showHoverImage : _showImage;

        return pressed ? _hidePressedImage : hovered ? _hideHoverImage : _hideImage;
    }

    private bool IsActiveButton(int buttonIndex) => buttonIndex == 0 && _state == CommandsPanelState.Closed;

    private void DrawCommandPanel(SKCanvas canvas, CommandPanelGeometry row)
    {
        canvas.DrawRect(row.CaptionRect, _panelBgPaint);
        if (_moduleCaptionBackgroundImage is not null)
            NinePatch.Draw(canvas, _moduleCaptionBackgroundImage, row.CaptionRect, 6f);

        float textX = row.CaptionRect.Left + 40f;
        float textY = row.CaptionRect.MidY + _moduleCaptionTextPaint.TextSize / 3f + ModuleTitleOffsetY;
        canvas.DrawText(row.Name, textX, textY, _moduleCaptionTextPaint);

        if (row.Opened && row.BodyRect.Height > 0)
        {
            canvas.DrawRect(row.BodyRect, _panelBgPaint);
            if (_moduleBodyBackgroundImage is not null)
                NinePatch.Draw(canvas, _moduleBodyBackgroundImage, row.BodyRect, XenonBodySliceInset);

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

        var iconPair = _commandIcons.GetValueOrDefault(button.CommandTypeId);
        var icon = iconPair.Normal;
        if (icon is not null)
        {
            // Icon commands render as a bare image — no button chrome, ever. On
            // hover the "-active" variant is swapped in instead of a background
            // highlight (falling back to the normal icon if no active file was
            // found). The clickable area (button.Rect) and hover/press tracking
            // are unchanged either way, so the cursor still switches to the
            // interactive glyph and clicks still land exactly as they would on a
            // regular button.
            bool isHovered = button.Enabled && index == _hoveredCommandButtonIndex;
            if (isHovered && iconPair.Active is not null)
                icon = iconPair.Active;

            float iconX = button.Rect.MidX - CommandIconSize / 2f;
            float iconY = button.Rect.MidY - CommandIconSize / 2f;
            var iconRect = new SKRect(iconX, iconY, iconX + CommandIconSize, iconY + CommandIconSize);

            byte iconAlpha = button.Enabled ? (byte)255 : (byte)110;
            _commandBtnIconPaint.Color = new SKColor(255, 255, 255, iconAlpha);
            canvas.DrawBitmap(icon, iconRect, _commandBtnIconPaint);
            return;
        }

        // The wide Xenon button frame is only a fallback for commands without a
        // dedicated square icon. Drawing it beneath icon assets leaves visible
        // rectangular wings on both sides of the square.
        DrawCommandButtonChrome(canvas, index, button);

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

        if (_commandButtonNormalImage is null)
        {
            canvas.DrawRect(button.Rect, fill);
            canvas.DrawRect(button.Rect, _commandBtnBorderPaint);
        }

        var textPaint = button.Enabled ? _commandBtnTextPaint : _commandBtnTextDisabledPaint;
        string displayLabel = TruncateLabel(label, textPaint, button.Rect.Width - 8f);
        float textY = button.Rect.MidY + textPaint.TextSize / 3f;
        canvas.DrawText(displayLabel, button.Rect.MidX, textY, textPaint);
    }

    private void DrawCommandButtonChrome(SKCanvas canvas, int index, CommandButtonGeometry button)
    {
        if (_commandButtonNormalImage is null)
            return;

        SKBitmap image = _commandButtonNormalImage;
        SKPaint? paint = null;

        if (!button.Enabled)
        {
            paint = XenonStyle.DisabledImagePaint;
        }
        else if (index == _pressedCommandButtonIndex && index == _hoveredCommandButtonIndex &&
                 _commandButtonPressedImage is not null)
        {
            image = _commandButtonPressedImage;
        }
        else if (index == _hoveredCommandButtonIndex && _commandButtonHoverImage is not null)
        {
            image = _commandButtonHoverImage;
        }

        NinePatch.Draw(canvas, image, button.Rect, XenonButtonSliceInset, paint);
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
