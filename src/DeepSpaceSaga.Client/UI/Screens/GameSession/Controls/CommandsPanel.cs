using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Commands Panel (top-left) — fixed navigation groups and module-addressed weapon panels.
/// A Caption (360×32) with a Hide/Show toggle button (26×26) + a list of
/// fixed command-panel groups (Navigation, Maneuver, Engine, Space Control),
/// each caption 360×36 with a fixed-height body (same size for every panel,
/// regardless of how many commands it holds).
/// </summary>
public sealed class CommandsPanel
{
    internal ModuleDiagram? OwnModuleDiagram { get; set; }
    internal int? DestroyedModuleLimit { get; set; }
    internal long PresentationRevision { get; set; }
    private SKPicture? _cachedPicture;
    private InstalledModuleSnapshot[] _cachedModules = [];
    private object? _cachedTiles;
    private string? _cachedOwner;
    private float _cachedHeight;
    private double _cachedTime, _cachedMultiplier;
    private long _cachedRevision, _inputRevision, _cachedInputRevision;
    private int? _cachedLimit;

    internal void ReleasePresentationCache()
    {
        _cachedPicture?.Dispose();
        _cachedPicture = null;
        _cachedModules = [];
        _cachedTiles = null;
    }

    internal static IReadOnlyList<string> ModuleDetails(string panel, string? moduleId,
        IReadOnlyList<InstalledModuleSnapshot> modules, ModuleDiagram? diagram, int? destroyedLimit = null)
    {
        if (diagram is null) return [];
        List<string> lines = [];
        if (panel == "Navigation")
            lines.Add($"{Localization.Combat("Panel.DestroyedModules")}: {diagram.DestroyedCount} / {destroyedLimit?.ToString() ?? Localization.Combat("Panel.UnknownValue")}");
        foreach (var tile in diagram.Tiles)
        {
            var module = modules.FirstOrDefault(m => m.ModuleId == tile.Id);
            if (module is null) continue;
            string group = module.LauncherCombat is not null ? "Torpedo Launcher" : module.Defense is not null ? "Countermeasure Launcher" :
                !module.Commands.IsDefaultOrEmpty && module.Commands.Any(c => c.CommandTypeId.StartsWith("engine.", StringComparison.Ordinal)) ? "Engine" :
                !module.Commands.IsDefaultOrEmpty && module.Commands.Any(c => c.CommandTypeId.StartsWith("scanner.", StringComparison.Ordinal)) ? "Space Control" : "Navigation";
            if (moduleId is not null ? tile.Id != moduleId : panel != group) continue;
            lines.Add(tile.Name + (tile.Critical ? " " + Localization.Combat("Panel.CriticalIcon") : ""));
            lines.Add($"{Localization.Combat("Panel.Armor")} {HealthBar(tile.Armor, tile.ArmorMax)} {tile.Armor}   {Localization.Combat("Panel.Structure")} {HealthBar(tile.Structure, tile.StructureMax)} {tile.Structure}");
            if (tile.Destroyed) lines.Add(GameSessionScreen.CombatText("Destroyed", ""));
            else if (tile.DisabledRealSeconds > 0)
                lines.Add($"{GameSessionScreen.CombatText("Disabled", "")} {PanelTime.Format(tile.DisabledRealSeconds)} {GameSessionScreen.CombatText("Seconds", "")}");
            else if (module.LauncherCombat is null && module.Defense is null) lines.Add(GameSessionScreen.CombatText("Ready", ""));
            if (tile.HitDamage is { } damage) lines.Add($"−{damage}");
        }
        return lines;
    }

    private static string HealthBar(int current, int maximum)
    {
        int filled = maximum > 0 ? (int)Math.Round(Math.Clamp((double)current / maximum, 0, 1) * 6) : 0;
        return Localization.Combat("Panel.BarLeft") +
            string.Concat(Enumerable.Repeat(Localization.Combat("Panel.BarFull"), filled)) +
            string.Concat(Enumerable.Repeat(Localization.Combat("Panel.BarEmpty"), 6 - filled)) + Localization.Combat("Panel.BarRight");
    }

    private const string XenonAssetsPath = "Images/UI/Themes/Xenon/GameSession/CommandPanels";
    private const float XenonBodySliceInset = 12f;
    private const float XenonButtonSliceInset = 8f;
    private const float ModuleTitleOffsetY = -4f;
    private const float CaptionTitleFontSize = 16f;
    public const float MainCaptionToPanelsGap = 2f;
    public const float CollapsedPanelGap = 2f;

    public const float PanelWidth = 360f;
    public const float CaptionHeight = 32f;
    public const float PanelCaptionHeight = 36f;

    /// <summary>Fixed body height for every panel (restored pre-ТЗ-04 constant: 200f row − 36f caption).</summary>
    public const float PanelBodyHeight = 164f;
    internal static float MinimumExpandedHeight => Margin + CaptionHeight + MainCaptionToPanelsGap +
        Panels.Length * PanelCaptionHeight + (Panels.Length - 1) * CollapsedPanelGap + PanelBodyHeight;

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
            ShipEngineCommandTypes.DirectionSynchronization,
            NavigationComputerCommandTypes.Approach)),
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
            ScannerCommandTypes.StructuralScan)),
        new CommandPanelDefinition("Torpedo Launcher", ImmutableArray.Create(CombatCommandTypes.Launch, CombatCommandTypes.Detonate)),
        new CommandPanelDefinition("Countermeasure Launcher", ImmutableArray.Create(DefenseCommandTypes.Launch)));

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
            [NavigationComputerCommandTypes.Approach] = "navigation.approach.png",
            [NavigationComputerCommandTypes.StationsList] = "navigation.stationsList.png",
            [ScannerCommandTypes.GeneralScan] = "scanner.generalScan.png",
            [ScannerCommandTypes.StructuralScan] = "scanner.structuralScan.png",
        };

    private const float CommandIconSize = 48f;

    /// <summary>Height of the strip overlaid at the bottom of each panel's own body (not adding to it) that shows the hovered command's name, centered.</summary>
    public const float StatusBarHeight = 24f;

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
    private readonly Func<WeaponPanelInstance, string, bool>? _weaponCommandEnabled;
    private readonly Action<WeaponPanelInstance, string>? _weaponCommandClicked;
    private readonly Func<WeaponPanelInstance, string?>? _weaponStatus;
    private readonly Func<WeaponPanelInstance, string?>? _weaponOperatorText;
    private readonly Func<WeaponPanelInstance, IReadOnlyList<string>>? _weaponDetails;
    private readonly Func<WeaponPanelInstance, string, string?>? _weaponCommandLabel;
    private readonly Func<double> _motionMultiplier;
    private readonly Func<double> _motionTime;
    private readonly HashSet<string> _visibleWeaponRanges = new(StringComparer.Ordinal);
    private readonly HashSet<string> _initializedPointRanges = new(StringComparer.Ordinal);
    private Guid? _pointRangeSession;
    private bool _hasPointRangeSession;
    internal bool IsWeaponRangeVisible(string moduleId) => _visibleWeaponRanges.Contains(moduleId);
    internal void InitializePointWeaponRanges(Guid? session, IEnumerable<string> moduleIds)
    {
        if (!_hasPointRangeSession || _pointRangeSession != session)
        {
            foreach (string id in _initializedPointRanges) _visibleWeaponRanges.Remove(id);
            _initializedPointRanges.Clear();
            _pointRangeSession = session;
            _hasPointRangeSession = true;
        }
        foreach (string id in moduleIds)
            if (_initializedPointRanges.Add(id)) _visibleWeaponRanges.Add(id);
    }
    private float _viewportHeight = float.PositiveInfinity;
    private float _scrollOffset;
    private float _maxScroll;
    private string? _preferredOpenedPanel;
    internal string? HoveredCommandTypeId => _hoveredCommandButtonIndex >= 0 &&
        _hoveredCommandButtonIndex < _allCommandButtons.Count
            ? _allCommandButtons[_hoveredCommandButtonIndex].CommandTypeId : null;

    /// <summary>Read-only hit-test against a visible command, with current enablement.</summary>
    internal string? EnabledCommandAt(float x, float y)
    {
        if (_state == CommandsPanelState.Closed) return null;
        foreach (var row in _panelRows)
        {
            if (!RowsContain(x, y) || !row.Opened || (_panelOpenedByName.TryGetValue(row.Key, out bool opened) && !opened)) continue;
            foreach (var button in row.Buttons)
                if (button.Rect.Contains(x, y) && button.Enabled && ButtonEnabled(button))
                    return button.CommandTypeId;
        }
        return null;
    }

    internal CommandPanelGeometry? PanelAt(float x, float y)
    {
        if (!RowsContain(x, y)) return null;
        foreach (var row in _panelRows)
            if (row.CaptionRect.Contains(x, y) || row.Opened && row.BodyRect.Contains(x, y)) return row;
        return null;
    }

    // ── Paints ──────────────────────────────────────────────────
    private readonly SKPaint _panelBgPaint;
    private readonly SKPaint _mainCaptionBgPaint;
    private readonly SKPaint _moduleCaptionBgPaint;
    private readonly SKPaint _mainCaptionHighlightPaint;
    private readonly SKPaint _mainCaptionShadowPaint;
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

    // Status bar paints (hovered command name, bottom of the whole widget)
    private readonly SKPaint _statusBarBgPaint;
    private readonly SKPaint _statusBarBorderPaint;
    private readonly SKPaint _statusBarTextPaint;

    // ── Images ──────────────────────────────────────────────────
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
        Action<string>? commandClicked = null,
        Func<WeaponPanelInstance, string, bool>? weaponCommandEnabled = null,
        Action<WeaponPanelInstance, string>? weaponCommandClicked = null,
        Func<WeaponPanelInstance, string?>? weaponStatus = null,
        Func<WeaponPanelInstance, string?>? weaponOperatorText = null,
        Func<WeaponPanelInstance, IReadOnlyList<string>>? weaponDetails = null,
        Func<double>? motionTime = null, Func<double>? motionMultiplier = null,
        Func<WeaponPanelInstance, string, string?>? weaponCommandLabel = null)
    {
        _isCommandEnabled = isCommandEnabled ?? (_ => true);
        _commandClicked = commandClicked ?? (_ => { });
        _weaponCommandEnabled = weaponCommandEnabled;
        _weaponCommandClicked = weaponCommandClicked;
        _weaponStatus = weaponStatus;
        _weaponOperatorText = weaponOperatorText;
        _weaponDetails = weaponDetails;
        _motionTime = motionTime ?? (() => 0);
        _motionMultiplier = motionMultiplier ?? (() => .25);
        _weaponCommandLabel = weaponCommandLabel;

        var typeface = XenonStyle.TypefaceRegular;

        _panelBgPaint = new SKPaint
        {
            Color = new SKColor(2, 16, 24),
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src
        };
        _mainCaptionBgPaint = new SKPaint
        {
            Color = new SKColor(6, 30, 43),
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src
        };
        _moduleCaptionBgPaint = new SKPaint
        {
            Color = new SKColor(8, 35, 50),
            Style = SKPaintStyle.Fill,
            BlendMode = SKBlendMode.Src
        };
        _mainCaptionHighlightPaint = new SKPaint
        {
            Color = new SKColor(24, 76, 96),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = false
        };
        _mainCaptionShadowPaint = new SKPaint
        {
            Color = new SKColor(0, 6, 11),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = false
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

        _statusBarBgPaint = new SKPaint { Color = new SKColor(8, 35, 50), Style = SKPaintStyle.Fill };
        _statusBarBorderPaint = new SKPaint { Color = new SKColor(42, 42, 42), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _statusBarTextPaint = new SKPaint { Color = XenonStyle.CyanBright, TextSize = 12f, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };

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
        _inputRevision++;
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

        if (!RowsContain(x, y)) return _captionRect.Contains(x, y);

        // Panel captions — toggle per-panel Opened/Closed.
        foreach (var row in _panelRows)
        {
            if (row.CaptionRect.Contains(x, y))
            {
                if (row.RangeModuleId is { } moduleId && row.RangeToggleRect.Contains(x, y))
                {
                    if (!_visibleWeaponRanges.Add(moduleId)) _visibleWeaponRanges.Remove(moduleId);
                    return true;
                }
                string name = row.Key;
                bool wasOpened = row.Opened;
                _panelOpenedByName[name] = !wasOpened;
                if (!wasOpened) _preferredOpenedPanel = name;
                return true;
            }
        }

        // Command buttons (opened rows only, row-major over the render order).
        // Enabled hit → pressed + fire the command; disabled hit → consumed, no action.
        for (int i = 0; i < _commandButtons.Count; i++)
        {
            if (_commandButtons[i].Button.Rect.Contains(x, y))
            {
                if (_commandButtons[i].Button.Enabled && ButtonEnabled(_commandButtons[i].Button))
                {
                    _pressedCommandButtonIndex = i;
                    var button = _commandButtons[i].Button;
                    if (button.Weapon is { } weapon) _weaponCommandClicked?.Invoke(weapon, button.CommandTypeId);
                    else _commandClicked(button.CommandTypeId);
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
        if (_pointerX != x || _pointerY != y) _inputRevision++;
        _pointerX = x;
        _pointerY = y;
        _hoveredButtonIndex = _hideShowButtonRect.Contains(x, y) ? 0 : -1;

        _hoveredCommandButtonIndex = -1;
        for (int i = 0; i < _commandButtons.Count; i++)
        {
            if (RowsContain(x, y) && _commandButtons[i].Button.Rect.Contains(x, y))
            {
                _hoveredCommandButtonIndex = i;
                break;
            }
        }

        return _hoveredButtonIndex >= 0 ||
               _hoveredCommandButtonIndex >= 0 ||
               (RowsContain(x, y) && _panelRows.Any(r => r.CaptionRect.Contains(x, y)));
    }

    public void OnMouseUp(float x, float y)
    {
        _inputRevision++;
        _pressedButtonIndex = -1;
        _pressedCommandButtonIndex = -1;
    }

    private float _pointerX = -1, _pointerY = -1;
    internal string? CurrentTimeTooltip { get; private set; }
    internal string? CurrentHealthTooltip { get; private set; }

    // ── Render ──────────────────────────────────────────────────

    public void Render(SKCanvas canvas, IReadOnlyList<InstalledModuleSnapshot> modules, float viewportHeight = float.PositiveInfinity,
        string? ownerObjectId = null)
    {
        if (modules is ImmutableArray<InstalledModuleSnapshot> { IsDefault: true }) modules = Array.Empty<InstalledModuleSnapshot>();
        double time = _motionTime(), multiplier = _motionMultiplier();
        bool same = _cachedPicture is not null && _cachedModules.Length == modules.Count &&
            _cachedHeight == viewportHeight && _cachedOwner == ownerObjectId && _cachedTime == time &&
            _cachedMultiplier == multiplier && _cachedRevision == PresentationRevision &&
            _cachedInputRevision == _inputRevision && _cachedLimit == DestroyedModuleLimit &&
            ReferenceEquals(_cachedTiles, OwnModuleDiagram?.Tiles);
        if (same)
            for (int i = 0; i < modules.Count; i++)
                if (!ReferenceEquals(_cachedModules[i], modules[i])) { same = false; break; }
        if (!same)
        {
            _cachedPicture?.Dispose();
            _cachedPicture = null;
            using var recorder = new SKPictureRecorder();
            var recording = recorder.BeginRecording(new SKRect(0, 0, PanelWidth + Margin * 2,
                float.IsFinite(viewportHeight) ? Math.Max(1, viewportHeight) : 100000));
            RenderCore(recording, modules, viewportHeight, ownerObjectId);
            _cachedPicture = recorder.EndRecording();
            _cachedModules = modules.ToArray();
            _cachedTiles = OwnModuleDiagram?.Tiles;
            _cachedHeight = viewportHeight; _cachedOwner = ownerObjectId;
            _cachedTime = time; _cachedMultiplier = multiplier; _cachedRevision = PresentationRevision;
            _cachedInputRevision = _inputRevision; _cachedLimit = DestroyedModuleLimit;
        }
        canvas.DrawPicture(_cachedPicture!);
    }

    private void RenderCore(SKCanvas canvas, IReadOnlyList<InstalledModuleSnapshot> modules, float viewportHeight,
        string? ownerObjectId)
    {
        if (OwnModuleDiagram is null && (modules is ImmutableArray<InstalledModuleSnapshot> { IsDefault: true } ||
            !modules.Any(m => m.LauncherCombat is not null || m.Defense is not null))) viewportHeight = float.PositiveInfinity;
        _viewportHeight = viewportHeight;
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
            rowY += MainCaptionToPanelsGap;
            var safeModules = modules is ImmutableArray<InstalledModuleSnapshot> { IsDefault: true }
                ? ImmutableArray<InstalledModuleSnapshot>.Empty : modules;
            var panels = BuildPanels(safeModules, ownerObjectId);
            using var detailPaint = new SKPaint { TextSize = 12f, IsAntialias = true };
            var details = panels.ToDictionary(p => p.Key, p => WrapDetails(
                (p.Weapon is { } w ? _weaponDetails?.Invoke(w) ?? [] : [])
                    .Concat(ModuleDetails(p.Name, p.Weapon?.ModuleId, safeModules, OwnModuleDiagram, DestroyedModuleLimit)).ToArray(), detailPaint), StringComparer.Ordinal);

            // Reserve every caption so all five groups remain reachable. Give an
            // explicitly opened group priority, then the installed launcher.
            HashSet<string>? visibleBodies = null;
            bool hasLauncher = OwnModuleDiagram is not null || safeModules.Any(m => m.LauncherCombat is not null || m.Defense is not null);
            float captionHeight = hasLauncher && viewportHeight < MinimumExpandedHeight
                ? Math.Clamp((viewportHeight - rowY - 128 - (panels.Length - 1) * CollapsedPanelGap) / panels.Length, 18f, 28f)
                : PanelCaptionHeight;
            float BodyHeight(string name)
            {
                if (!hasLauncher) return PanelBodyHeight;
                var definition = panels.First(p => p.Key == name);
                int rows = (definition.CommandTypeIds.Length + 3) / 4;
                return BodyPaddingY * 2 + rows * CommandButtonHeight + (rows - 1) * CommandButtonGap +
                    StatusBarHeight + XenonBodySliceInset + (definition.Weapon is not null || name is "Torpedo Launcher" or "Countermeasure Launcher" ? 32f : 0)
                    + details[name].Length * 16f;
            }
            if (hasLauncher)
            {
                visibleBodies = new(StringComparer.Ordinal);
                float remaining = viewportHeight - rowY - panels.Length * captionHeight -
                    (panels.Length - 1) * CollapsedPanelGap;
                foreach (string name in panels.OrderBy(p => p.Key == _preferredOpenedPanel ? -1 : ExpansionPriority(p))
                    .Select(p => p.Key))
                {
                    if (!panels.Any(p => p.Key == name)) continue;
                    if (_panelOpenedByName.TryGetValue(name, out bool requested) && !requested) continue;
                    if (remaining < BodyHeight(name) && name != _preferredOpenedPanel && (visibleBodies.Count > 0 || OwnModuleDiagram is null)) continue;
                    visibleBodies.Add(name);
                    remaining -= BodyHeight(name);
                }
                if (visibleBodies.Count == 0 && _preferredOpenedPanel is { } preferred && panels.Any(p => p.Key == preferred) &&
                    (!_panelOpenedByName.TryGetValue(preferred, out bool preferredRequested) || preferredRequested)) visibleBodies.Add(preferred);
            }

            rowY -= _scrollOffset;
            for (int panelIndex = 0; panelIndex < panels.Length; panelIndex++)
            {
                var panel = panels[panelIndex];
                bool opened = !_panelOpenedByName.TryGetValue(panel.Key, out bool o) || o;
                if (visibleBodies is not null) opened &= visibleBodies.Contains(panel.Key);


                var captionRect = new SKRect(
                    Margin, rowY,
                    Margin + PanelWidth, rowY + captionHeight);

                var bodyRect = opened
                    ? new SKRect(
                        Margin, rowY + captionHeight,
                        Margin + PanelWidth, rowY + captionHeight + BodyHeight(panel.Key))
                    : SKRect.Empty;

                int rowStartOrdinal = _commandButtons.Count;
                var buttons = opened
                    ? BuildCommandButtons(panel, safeModules, bodyRect)
                    : ImmutableArray<CommandButtonGeometry>.Empty;
                int rowEndOrdinalExclusive = _commandButtons.Count;

                // The per-panel status bar overlays the bottom of the panel's own
                // fixed-height body (it never grows the body) and only shows a
                // name when the hovered command button belongs to this panel.
                // Inset by the nine-patch body border (XenonBodySliceInset) on
                // every side so it sits inside the panel's frame rather than
                // painting over it.
                var statusBarRect = opened
                    ? new SKRect(
                        bodyRect.Left + XenonBodySliceInset,
                        bodyRect.Bottom - XenonBodySliceInset - StatusBarHeight,
                        bodyRect.Right - XenonBodySliceInset,
                        bodyRect.Bottom - XenonBodySliceInset)
                    : SKRect.Empty;
                string? statusBarText = opened &&
                    _hoveredCommandButtonIndex >= rowStartOrdinal &&
                    _hoveredCommandButtonIndex < rowEndOrdinalExclusive
                        ? _commandButtons[_hoveredCommandButtonIndex].Label
                        : null;
                if (opened && statusBarText is null && panel.Weapon is { } weapon) statusBarText = _weaponStatus?.Invoke(weapon);

                var weaponModule = panel.Weapon is { } instance
                    ? safeModules.FirstOrDefault(m => m.ModuleId == instance.ModuleId)
                    : panel.Name == "Torpedo Launcher" ? safeModules.OrderBy(m => m.Position).FirstOrDefault(m => m.LauncherCombat is not null)
                    : panel.Name == "Countermeasure Launcher" ? safeModules.OrderBy(m => m.Position).FirstOrDefault(m => m.Defense is not null) : null;
                float toggleSize = Math.Min(18, captionRect.Height - 4);
                var toggle = weaponModule is null ? SKRect.Empty : new SKRect(captionRect.Right - toggleSize - 8,
                    captionRect.MidY - toggleSize / 2, captionRect.Right - 8, captionRect.MidY + toggleSize / 2);
                var reloadBar = opened && weaponModule is not null
                    ? new SKRect(bodyRect.Left + 12, bodyRect.Bottom - 9, bodyRect.Right - 12, bodyRect.Bottom - 3) : SKRect.Empty;
                double? readyAt = weaponModule?.LauncherCombat?.ReadyAtMotionTimeMs ??
                    weaponModule?.Defense?.ReadyAtMotionTimeMs ?? weaponModule?.Defense?.ReloadDueMotionTimeMs;
                double? reloadDuration = weaponModule?.LauncherCombat?.ReloadDurationMs ?? weaponModule?.Defense?.ReloadDurationMs;
                double remainingReload = Math.Max(0, (readyAt ?? 0) - _motionTime());
                double reloadFraction = remainingReload <= 0 ? 1 : reloadDuration is > 0 ? Math.Clamp(1 - remainingReload / reloadDuration.Value, 0, 1) : 0;
                if (weaponModule is { StructureMax: > 0, StructureCurrent: <= 0 } || weaponModule?.LauncherCombat?.IsBrokenUntilDock == true) reloadFraction = 0;
                bool pointWeapon = weaponModule?.LauncherCombat?.Profiles is { IsDefaultOrEmpty: false } || weaponModule?.Defense?.Profiles is { IsDefaultOrEmpty: false };
                var reloadRing = pointWeapon ? new SKRect(captionRect.Left + 8, captionRect.MidY - 12, captionRect.Left + 32, captionRect.MidY + 12) : SKRect.Empty;
                double multiplier = _motionMultiplier();
                double remainingReal = remainingReload / 1000 / (multiplier > 0 ? multiplier : .25);
                _panelRows.Add(new CommandPanelGeometry(
                    panel.Name, opened, captionRect, bodyRect, buttons, statusBarRect, statusBarText, panel.Weapon, details[panel.Key],
                    weaponModule?.ModuleId, toggle, weaponModule is not null && IsWeaponRangeVisible(weaponModule.ModuleId), pointWeapon ? SKRect.Empty : reloadBar, reloadFraction, reloadRing, remainingReal,
                    panel.Weapon is { } operatorWeapon ? _weaponOperatorText?.Invoke(operatorWeapon) : null,
                    remainingReload > 0 ? PanelTime.Tooltip(remainingReload / 1000,
                        (SimulationSpeed)(int)Math.Round((multiplier > 0 ? multiplier : .25) * 4)) : null));

                rowY += opened ? (captionHeight + BodyHeight(panel.Key)) : captionHeight;
                if (!opened && panelIndex < panels.Length - 1)
                    rowY += CollapsedPanelGap;
            }
        }

        _maxScroll = Math.Max(0, rowY + _scrollOffset - viewportHeight);
        if (_scrollOffset > _maxScroll)
        {
            _scrollOffset = _maxScroll;
            RenderCore(canvas, modules, viewportHeight, ownerObjectId);
            return;
        }
        float bodyBottom = Math.Min(rowY, viewportHeight);
        _bodyRect = new SKRect(
            Margin, _captionRect.Bottom,
            Margin + PanelWidth, bodyBottom);

        _commandButtonDrawOrdinal = 0;
        DrawCaption(canvas);
        canvas.Save();
        canvas.ClipRect(new SKRect(Margin, _captionRect.Bottom, Margin + PanelWidth, float.IsFinite(viewportHeight) ? viewportHeight : float.MaxValue));
        foreach (var row in _panelRows)
            DrawCommandPanel(canvas, row);
        CurrentTimeTooltip = null;
        CurrentHealthTooltip = null;
        foreach (var row in _panelRows)
        {
            if (row.TimeTooltip is not { } tooltip || !row.ReloadRingRect.Contains(_pointerX, _pointerY)) continue;
            CurrentTimeTooltip = tooltip;
            float top = Math.Min(_pointerY + 18, viewportHeight - 24);
            canvas.DrawRect(new SKRect(Margin, top, Margin + PanelWidth, top + 24), _panelBgPaint);
            canvas.DrawText(tooltip, Margin + 4, top + 16, _moduleCaptionTextPaint);
        }
        foreach (var row in _panelRows)
        {
            if (!row.Opened || !row.BodyRect.Contains(_pointerX, _pointerY) || OwnModuleDiagram is null) continue;
            var tooltips = new List<string>();
            foreach (var tile in OwnModuleDiagram.Tiles)
            {
                if (row.Weapon is { } weapon && weapon.ModuleId != tile.Id) continue;
                if (!row.DetailLines.Any(line => line.StartsWith(tile.Name, StringComparison.Ordinal))) continue;
                tooltips.Add($"{tile.Name}: {Localization.Combat("Panel.Armor")} {tile.Armor}/{tile.ArmorMax}; {Localization.Combat("Panel.Structure")} {tile.Structure}/{tile.StructureMax}");
                if (tile.Critical) tooltips.Add(Localization.Combat("Panel.CriticalTooltip"));
                if (tile.DisabledRealSeconds > 0) tooltips.Add(PanelTime.Tooltip(tile.DisabledRealSeconds * _motionMultiplier(),
                    (SimulationSpeed)(int)Math.Round(_motionMultiplier() * 4)));
            }
            CurrentHealthTooltip = tooltips.Count == 0 ? null : string.Join("\n", tooltips);
            float top = Math.Min(_pointerY + 18, viewportHeight - tooltips.Count * 18 - 4);
            if (tooltips.Count > 0)
                canvas.DrawRect(new SKRect(Margin, top, Margin + PanelWidth, top + tooltips.Count * 18 + 4), _panelBgPaint);
            for (int i = 0; i < tooltips.Count; i++)
                canvas.DrawText(tooltips[i], Margin + 4, top + 16 + i * 18, _moduleCaptionTextPaint);
        }
        canvas.Restore();
    }

    private bool RowsContain(float x, float y) => _state != CommandsPanelState.Closed && x >= Margin && x <= Margin + PanelWidth &&
        y >= _captionRect.Bottom && y < _viewportHeight;

    private static ImmutableArray<string> WrapDetails(IReadOnlyList<string> source, SKPaint paint)
    {
        var lines = ImmutableArray.CreateBuilder<string>();
        foreach (string line in source)
        {
            if (line.Contains('\t')) { lines.Add(line); continue; }
            string remaining = line;
            while (paint.MeasureText(remaining) > PanelWidth - 20)
            {
                int count = Math.Max(1, (int)paint.BreakText(remaining, PanelWidth - 20));
                int space = remaining.LastIndexOf(' ', count - 1, count);
                if (space > 0) count = space;
                lines.Add(remaining[..count].TrimEnd());
                remaining = remaining[count..].TrimStart();
            }
            lines.Add(remaining);
        }
        return lines.ToImmutable();
    }

    public bool OnMouseWheel(float x, float y, float delta)
    {
        _inputRevision++;
        if (!float.IsFinite(delta) || delta == 0 || _state == CommandsPanelState.Closed || !RowsContain(x, y) || _maxScroll == 0) return false;
        _scrollOffset = Math.Clamp(_scrollOffset - delta * 48, 0, _maxScroll);
        return true;
    }

    private bool ButtonEnabled(CommandButtonGeometry button) => button.Weapon is { } weapon
        ? _weaponCommandEnabled?.Invoke(weapon, button.CommandTypeId) == true
        : _isCommandEnabled(button.CommandTypeId);

    private static ImmutableArray<CommandPanelDefinition> BuildPanels(IReadOnlyList<InstalledModuleSnapshot> modules, string? owner)
    {
        if (string.IsNullOrWhiteSpace(owner)) return Panels;
        var builder = ImmutableArray.CreateBuilder<CommandPanelDefinition>();
        foreach (var panel in Panels)
        {
            bool torpedo = panel.Name == "Torpedo Launcher", counter = panel.Name == "Countermeasure Launcher";
            if (!torpedo && !counter) { builder.Add(panel); continue; }
            var weapons = modules.Where(m => torpedo ? m.LauncherCombat?.Profiles is { IsDefaultOrEmpty: false }
                : m.Defense?.Profiles is { IsDefaultOrEmpty: false }).OrderBy(m => m.Position).ThenBy(m => m.ModuleId, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < weapons.Length; i++)
            {
                string caption = weapons.Length == 1 ? panel.Name : panel.Name + " " + (i + 1);
                builder.Add(new(caption, torpedo ? [CombatCommandTypes.Launch, CombatCommandTypes.Detonate] : [DefenseCommandTypes.Launch],
                    new(owner, weapons[i].ModuleId, caption)));
            }
        }
        return builder.ToImmutable();
    }

    internal static int ExpansionPriority(CommandPanelDefinition panel) =>
        panel.CommandTypeIds.Contains(CombatCommandTypes.Launch) ? 0 :
        panel.CommandTypeIds.Contains(DefenseCommandTypes.Launch) ? 1 :
        panel.Name == "Navigation" ? 2 : panel.Name == "Engine" ? 3 : panel.Name == "Space Control" ? 4 : 5;

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
            var metadata = panel.Weapon is { } weapon
                ? FindCommandMetadataAcrossModules(modules.Where(m => m.ModuleId == weapon.ModuleId).ToArray(), commandTypeId)
                : FindCommandMetadataAcrossModules(modules, commandTypeId);
            string label = panel.Weapon is { } labelWeapon ? _weaponCommandLabel?.Invoke(labelWeapon, commandTypeId) ?? metadata?.DisplayName ?? commandTypeId : metadata?.DisplayName ?? commandTypeId;

            int col = i % (int)CommandButtonColumns;
            int rowIdx = i / (int)CommandButtonColumns;
            float x = bodyRect.Left + BodyPaddingX + col * (CommandButtonWidth + CommandButtonGap);
            float y = bodyRect.Top + BodyPaddingY + rowIdx * (CommandButtonHeight + CommandButtonGap);

            var geometry = new CommandButtonGeometry(
                commandTypeId,
                new SKRect(x, y, x + CommandButtonWidth, y + CommandButtonHeight),
                panel.Weapon is { } instance ? _weaponCommandEnabled?.Invoke(instance, commandTypeId) == true : _isCommandEnabled(commandTypeId), panel.Weapon, label);

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
        ModuleCommandSnapshot? result = null;
        int firstPosition = int.MaxValue;
        for (int i = 0; i < modules.Count; i++)
        {
            var module = modules[i];
            if (module.Commands.IsDefaultOrEmpty || (result is not null && module.Position >= firstPosition))
                continue;

            foreach (var command in module.Commands)
            {
                if (command.CommandTypeId != commandTypeId) continue;
                result = command;
                firstPosition = module.Position;
                break;
            }
        }

        return result;
    }

    private void DrawCaption(SKCanvas canvas)
    {
        DrawBeveledCaption(canvas, _captionRect, _mainCaptionBgPaint);

        // HideShow toggle: hide icon when open, show icon when closed.
        var hideShowImage = ResolveHideShowImage();
        DrawButton(canvas, _hideShowButtonRect, 0, hideShowImage);

        float textX = _hideShowButtonRect.Right + Padding + 2f;
        float textY = _captionRect.MidY + _titlePaint.TextSize / 3f;
        canvas.DrawText(Localization.Combat("Panel.Commands"), textX, textY, _titlePaint);
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
        DrawBeveledCaption(canvas, row.CaptionRect, _moduleCaptionBgPaint);

        if (!row.ReloadRingRect.IsEmpty)
        {
            using var ring = new SKPaint { Color = XenonStyle.CyanBright.WithAlpha(60), Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true };
            canvas.DrawOval(row.ReloadRingRect, ring);
            ring.Color = XenonStyle.CyanBright;
            using var arc = new SKPath(); arc.AddArc(row.ReloadRingRect, -90, (float)(360 * row.ReloadFraction));
            canvas.DrawPath(arc, ring);
        }
        float textX = row.CaptionRect.Left + 40f;
        float textY = row.CaptionRect.MidY + _moduleCaptionTextPaint.TextSize / 3f + ModuleTitleOffsetY;
        canvas.DrawText(TruncateLabel(Localization.CombatPanelCaption(row.Name), _moduleCaptionTextPaint,
            (row.RangeToggleRect.IsEmpty ? row.CaptionRect.Right - 6 : row.RangeToggleRect.Left - 6) - textX), textX, textY, _moduleCaptionTextPaint);
        if (!row.RangeToggleRect.IsEmpty)
        {
            canvas.DrawRect(row.RangeToggleRect, row.RangeVisible ? _btnActivePaint : _btnNormalPaint);
            canvas.DrawRect(row.RangeToggleRect, _btnBorderPaint);
            if (row.RangeVisible)
            {
                using var check = new SKPaint { Color = XenonStyle.CyanBright, StrokeWidth = 2, IsAntialias = true };
                var r = row.RangeToggleRect;
                canvas.DrawLine(r.Left + 3, r.MidY, r.MidX - 1, r.Bottom - 4, check);
                canvas.DrawLine(r.MidX - 1, r.Bottom - 4, r.Right - 3, r.Top + 3, check);
            }
        }

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

            if (row.OperatorText is { } operatorText)
            {
                using var paint = new SKPaint { Color = XenonStyle.CyanBright, TextSize = 12f, IsAntialias = true };
                var rect = row.BodyRect;
                canvas.Save();
                canvas.ClipRect(new SKRect(rect.Left + 8, rect.Top + 58, rect.Right - 8, row.StatusBarRect.Top));
                canvas.DrawText(operatorText, rect.Left + 10, rect.Top + 80, paint);
                canvas.Restore();
            }
            DrawPanelStatusBar(canvas, row);
            if (!row.DetailLines.IsDefaultOrEmpty)
            {
                using var paint = new SKPaint { Color = XenonStyle.CyanBright, TextSize = 12f, IsAntialias = true };
                canvas.Save();
                canvas.ClipRect(new SKRect(row.BodyRect.Left + 8, row.BodyRect.Top + 4, row.BodyRect.Right - 8, row.StatusBarRect.Top));
                for (int i = 0; i < row.DetailLines.Length; i++)
                {
                    paint.Color = row.Name == "Navigation" && i == 0 && DestroyedModuleLimit is { } limit &&
                        OwnModuleDiagram is { } diagram && diagram.DestroyedCount >= limit - 1 ? SKColors.IndianRed : XenonStyle.CyanBright;
                    float y = Math.Max(row.BodyRect.Top + (row.Weapon is null ? 16 : 96), row.Buttons.IsDefaultOrEmpty ? 0 : row.Buttons.Max(b => b.Rect.Bottom) + 16) + i * 16;
                    if (row.DetailLines[i].Contains('\t'))
                    {
                        float[] widths = [24, 65, 45, 39, 57, 44, 54];
                        var columns = row.DetailLines[i].Split('\t');
                        float x = row.BodyRect.Left + 10;
                        paint.TextSize = 10;
                        for (int column = 0; column < columns.Length; column++)
                        {
                            canvas.DrawText(columns[column], x, y, paint);
                            x += widths[Math.Min(column, widths.Length - 1)];
                        }
                        paint.TextSize = 12;
                    }
                    else canvas.DrawText(row.DetailLines[i], row.BodyRect.Left + 10, y, paint);
                }
                canvas.Restore();
            }
            if (!row.ReloadBarRect.IsEmpty)
            {
                canvas.DrawRect(row.ReloadBarRect, _statusBarBgPaint);
                using var progress = new SKPaint { Color = XenonStyle.CyanBright };
                var fill = row.ReloadBarRect;
                fill.Right = fill.Left + fill.Width * (float)row.ReloadFraction;
                canvas.DrawRect(fill, progress);
                canvas.DrawRect(row.ReloadBarRect, _btnBorderPaint);
            }
        }
    }

    /// <summary>
    /// Draws the hovered command's name centered in a thin strip at the bottom
    /// of this panel's own body (overlaid, not adding height — see the fixed
    /// <see cref="StatusBarHeight"/> reserved within <see cref="PanelBodyHeight"/>
    /// by every current panel's button grid). No-op when nothing in this panel
    /// is hovered.
    /// </summary>
    private void DrawPanelStatusBar(SKCanvas canvas, CommandPanelGeometry row)
    {
        if (row.StatusBarText is null)
            return;

        canvas.DrawRect(row.StatusBarRect, _statusBarBgPaint);
        canvas.DrawRect(row.StatusBarRect, _statusBarBorderPaint);

        float textY = row.StatusBarRect.MidY + _statusBarTextPaint.TextSize / 3f;
        canvas.DrawText(TruncateLabel(row.StatusBarText, _statusBarTextPaint, row.StatusBarRect.Width - 12), row.StatusBarRect.MidX, textY, _statusBarTextPaint);
    }

    private void DrawBeveledCaption(SKCanvas canvas, SKRect rect, SKPaint backgroundPaint)
    {
        canvas.DrawRect(rect, backgroundPaint);

        float left = rect.Left + 0.5f;
        float top = rect.Top + 0.5f;
        float right = rect.Right - 0.5f;
        float bottom = rect.Bottom - 0.5f;
        canvas.DrawLine(left, top, right, top, _mainCaptionHighlightPaint);
        canvas.DrawLine(left, top, left, bottom, _mainCaptionHighlightPaint);
        canvas.DrawLine(right, top, right, bottom, _mainCaptionShadowPaint);
        canvas.DrawLine(left, bottom, right, bottom, _mainCaptionShadowPaint);
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
    Closed = 2,
}

/// <summary>
/// A fixed, gameplay-grouped set of command types shown as one Commands Panel
/// row. Not derived from installed modules — the panel/command order is fixed
/// by design (see ТЗ "Command Panels по наборам команд").
/// </summary>
public sealed record CommandPanelDefinition(string Name, ImmutableArray<string> CommandTypeIds, WeaponPanelInstance? Weapon = null)
{
    public string Key => Weapon?.Key ?? Name;
}

public readonly record struct CommandPanelGeometry(
    string Name,
    bool Opened,
    SKRect CaptionRect,
    SKRect BodyRect,
    ImmutableArray<CommandButtonGeometry> Buttons,
    SKRect StatusBarRect,
    string? StatusBarText,
    WeaponPanelInstance? Weapon = null,
    ImmutableArray<string> DetailLines = default,
    string? RangeModuleId = null, SKRect RangeToggleRect = default, bool RangeVisible = false,
    SKRect ReloadBarRect = default, double ReloadFraction = 1, SKRect ReloadRingRect = default, double RemainingReloadRealSeconds = 0, string? OperatorText = null,
    string? TimeTooltip = null)
{
    public string Key => Weapon?.Key ?? Name;
}

public readonly record struct CommandButtonGeometry(
    string CommandTypeId,
    SKRect Rect,
    bool Enabled,
    WeaponPanelInstance? Weapon = null, string? Label = null);
