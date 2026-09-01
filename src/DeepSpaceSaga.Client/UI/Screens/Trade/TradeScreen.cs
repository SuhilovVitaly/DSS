using System.Collections.Immutable;
using System.Linq;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>
/// Trade overlay (Docs/FirstRelease/Screens/Trade.md) — Buy/Sell station cargo goods and
/// Refuel the engine module, all authoritative (Engine confirms every mutation; this
/// screen never mutates Credits/stock/cargo/fuel itself). Opened from
/// <see cref="Station.StationScreen"/>'s `TRADE` button (ScreenEvent.OpenTrade) as a
/// nested modal on top of it; closes via the EXIT button, Escape, or a click outside the
/// panel, returning to <see cref="Station.StationScreen"/>. Pause-on-open/resume-on-close
/// is handled generically by SkiaWindow's PushModalAsync/PopModalAsync.
///
/// Unlike the other overlay screens, this one needs live session access (it reads
/// PlayerCredits/DockedStationTrade/Cargo/FuelAmountKg from the buffered snapshot every
/// frame, and sends trade commands through the handle) — the same shape GameSessionScreen
/// already uses for (buffer, handle).
///
/// Design decisions made where the contract left them to the implementer (see the story
/// file, Unit 6.3):
/// <list type="bullet">
/// <item>Item display names: the client has no item-type display-name catalog yet, so
/// the known ids (src/DeepSpaceSaga.Client/Data/item-types.json — 10 as of
/// story-20260825-084409 Batch 3) are mapped via a small switch to Trade.Item* localization
/// keys, falling back to the raw id for any future/unknown id.</item>
/// <item>FUEL display: FuelCapacityKg is Engine-internal content, never projected to the
/// client, so there is no denominator for a percentage fill — the FUEL panel shows
/// <see cref="InstalledModuleSnapshot.FuelAmountKg"/> as a plain "X kg" number, no
/// ProgressBar (which stays unused in this first version rather than being fed a made-up
/// fraction).</item>
/// <item>Quantity stepper upper bound: the client has no authoritative capacity limit
/// (CargoCapacityKg is Engine-internal too), so the stepper just clamps to a generous,
/// non-authoritative ceiling (station stock or the player's own cargo of that item,
/// whichever is larger) purely to keep the UI number sane — the Engine is still the only
/// real authority and can reject/partially execute regardless of what the UI allowed.</item>
/// <item>Refuel quantity: kept as its own <see cref="_refuelQuantity"/> field/stepper in
/// the FUEL panel, decoupled from the STATION INVENTORY/TRANSACTION selection — clicking
/// REFUEL never requires first selecting the "Fuel" row, and selecting cargo goods never
/// changes the refuel amount.</item>
/// </list>
/// </summary>
public sealed class TradeScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly GameSessionHandle? _handle;

    private int _screenWidth;
    private int _screenHeight;

    private TradeButton _hoveredButton = TradeButton.None;
    private int _hoveredInventoryRow = -1;
    private int _hoveredCargoRow = -1;
    private bool _isStationNameHovered;
    private bool _isExitButtonHovered;

    /// <summary>
    /// Real-time (Environment.TickCount64) timestamp the pointer first entered the
    /// food-rations readout, or null while not hovering it — the tooltip only appears
    /// once MenuStyle.TooltipHoverDelaySeconds has elapsed since this moment (checked in
    /// Render every frame, since OnMouseMove alone would never fire again while the
    /// pointer sits still).
    /// </summary>
    private long? _foodRationsHoverStartedAtMs;

    /// <summary>Test seam — true once the food-rations hover delay has elapsed.</summary>
    internal bool IsFoodRationsTooltipVisible =>
        _foodRationsHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the crew readout.</summary>
    private long? _crewHoverStartedAtMs;

    /// <summary>Test seam — true once the crew-readout hover delay has elapsed.</summary>
    internal bool IsCrewTooltipVisible =>
        _crewHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Currently selected station item for Buy/Sell — null means "nothing selected yet".</summary>
    private string? _selectedItemTypeId;

    /// <summary>
    /// Quantity for the currently selected Buy/Sell transaction. On selection change (station
    /// row, cargo row, or <see cref="OnActivated"/>'s preselect) reset to exactly one sell
    /// package for that item (<see cref="ResolveQuantityStep"/> — 10 for Good, 100 for
    /// Resource) rather than a hardcoded 1: a bare 1 is never a multiple of the package size,
    /// so it would always be rejected by the Engine's authoritative Sell package-size check
    /// (CommandReasonCodes.InvalidPackageQuantity) regardless of how many times the stepper is
    /// clicked afterwards — Buy has no package restriction, so this floor never breaks Buy,
    /// it only narrows the reachable minimum. Reset to 1 on Cancel (no selection).
    /// </summary>
    private long _quantity = 1;

    /// <summary>Quantity for Refuel — independent of <see cref="_quantity"/> (see type doc comment).</summary>
    private long _refuelQuantity = 1;

    /// <summary>CommandId of the last Buy/Sell/Refuel sent, awaiting a CommandResult in a future snapshot.</summary>
    private string? _pendingCommandId;
    private string? _pendingCommandType;
    private long _pendingQuantity;

    /// <summary>
    /// Last resolved status message (success/partial-fill/rejection reason) — kept visible
    /// until the next command is sent and resolved, so it doesn't disappear the instant
    /// the triggering CommandResult is consumed.
    /// </summary>
    private string? _lastStatusMessage;

    /// <summary>Test seam — mirrors GameSessionScreen's internal getters for testability.</summary>
    internal string? SelectedItemTypeId => _selectedItemTypeId;
    internal long Quantity => _quantity;
    internal long RefuelQuantity => _refuelQuantity;
    internal string? LastStatusMessage => _lastStatusMessage;

    public TradeScreen(SnapshotBuffer buffer, GameSessionHandle? handle)
    {
        GenericWindowTypeA.Preload();
        GenericButtonTypeA.Preload();

        _buffer = buffer;
        _handle = handle;
    }

    public void OnActivated()
    {
        _hoveredButton = TradeButton.None;
        _hoveredInventoryRow = -1;
        _hoveredCargoRow = -1;
        _isStationNameHovered = false;
        _isExitButtonHovered = false;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;

        var trade = _buffer.Latest?.Snapshot?.DockedStationTrade;
        if (trade is not null && !trade.Items.IsDefaultOrEmpty)
        {
            _selectedItemTypeId = trade.Items[0].ItemTypeId;
            // Start at exactly one sell package (§59) — see the "Quantity defaults to a whole
            // sell package" note on the _quantity field: a hardcoded 1 here is never a valid
            // Sell quantity for a Resource/Good item once package-size validation applies.
            _quantity = ResolveQuantityStep(trade, _selectedItemTypeId);
        }
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseTrade : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = TradeLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == TradeButton.Close)
            return ScreenEvent.CloseTrade;

        if (IsExitButtonHit(x, y))
            return ScreenEvent.CloseTrade;

        var snapshot = _buffer.Latest?.Snapshot;

        if (IsStationNameHit(x, y, snapshot))
            return ScreenEvent.NavigateToStation;

        var trade = snapshot?.DockedStationTrade;

        if (trade is not null && !trade.Items.IsDefaultOrEmpty)
        {
            int rowIndex = TradeLayout.HitTestInventoryRow(x, y, _screenWidth, _screenHeight, trade.Items.Length);
            if (rowIndex >= 0)
            {
                _selectedItemTypeId = trade.Items[rowIndex].ItemTypeId;
                // §59: start at exactly one sell package, not a hardcoded 1 — see the
                // _quantity field doc comment.
                _quantity = ResolveQuantityStep(trade, _selectedItemTypeId);
                return ScreenEvent.None;
            }
        }

        var containerModule = FindModule(snapshot, ResolveModuleId(snapshot, TradeCommandTypes.Buy));
        if (containerModule is { Cargo.IsDefaultOrEmpty: false })
        {
            int cargoRowIndex = TradeLayout.HitTestCargoRow(x, y, _screenWidth, _screenHeight, containerModule.Cargo.Length);
            if (cargoRowIndex >= 0)
            {
                _selectedItemTypeId = containerModule.Cargo[cargoRowIndex].ItemTypeId;
                _quantity = ResolveQuantityStep(trade, _selectedItemTypeId);
                return ScreenEvent.None;
            }
        }

        switch (hit)
        {
            case TradeButton.QuantityMinus:
                {
                    // Floor is one sell package (§59), not a bare 1 — Minus must never leave
                    // _quantity at a value the authoritative Sell package-size check would
                    // reject (InvalidPackageQuantity). Harmless for Buy, which has no package
                    // restriction and accepts any package-multiple quantity too.
                    long step = ResolveQuantityStep(trade, _selectedItemTypeId);
                    _quantity = Math.Max(step, _quantity - step);
                }
                return ScreenEvent.None;

            case TradeButton.QuantityPlus:
                _quantity = Math.Min(
                    ResolveQuantityUpperBound(trade, containerModule, _selectedItemTypeId),
                    _quantity + ResolveQuantityStep(trade, _selectedItemTypeId));
                return ScreenEvent.None;

            case TradeButton.RefuelQuantityMinus:
                _refuelQuantity = Math.Max(1, _refuelQuantity - 1);
                return ScreenEvent.None;

            case TradeButton.RefuelQuantityPlus:
                _refuelQuantity = Math.Min(ResolveQuantityUpperBound(trade, containerModule, "item.fuel"), _refuelQuantity + 1);
                return ScreenEvent.None;

            case TradeButton.Buy:
                SendTrade(snapshot, TradeCommandTypes.Buy, _selectedItemTypeId, _quantity);
                return ScreenEvent.None;

            case TradeButton.Sell:
                SendTrade(snapshot, TradeCommandTypes.Sell, _selectedItemTypeId, _quantity);
                return ScreenEvent.None;

            case TradeButton.Refuel:
                SendTrade(snapshot, TradeCommandTypes.Refuel, "item.fuel", _refuelQuantity, forRefuel: true);
                return ScreenEvent.None;

            case TradeButton.Cancel:
                _selectedItemTypeId = null;
                _quantity = 1;
                return ScreenEvent.None;
        }

        // Click on the dimmed background outside the panel also closes it.
        if (!TradeLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseTrade;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        _hoveredButton = TradeLayout.HitTest(x, y, _screenWidth, _screenHeight);

        var snapshot = _buffer.Latest?.Snapshot;
        var trade = snapshot?.DockedStationTrade;
        _hoveredInventoryRow = trade is not null && !trade.Items.IsDefaultOrEmpty
            ? TradeLayout.HitTestInventoryRow(x, y, _screenWidth, _screenHeight, trade.Items.Length)
            : -1;

        var containerModule = FindModule(snapshot, ResolveModuleId(snapshot, TradeCommandTypes.Buy));
        _hoveredCargoRow = containerModule is { Cargo.IsDefaultOrEmpty: false }
            ? TradeLayout.HitTestCargoRow(x, y, _screenWidth, _screenHeight, containerModule.Cargo.Length)
            : -1;

        _isStationNameHovered = IsStationNameHit(x, y, snapshot);
        _isExitButtonHovered = IsExitButtonHit(x, y);

        // Not a button — hovering it only shows a delayed tooltip (see Render), so it must
        // not affect the interactive-cursor swap the way the name link / exit button do.
        if (IsFoodRationsHit(x, y))
            _foodRationsHoverStartedAtMs ??= Environment.TickCount64;
        else
            _foodRationsHoverStartedAtMs = null;

        if (IsCrewHit(x, y))
            _crewHoverStartedAtMs ??= Environment.TickCount64;
        else
            _crewHoverStartedAtMs = null;

        return _hoveredButton != TradeButton.None || _hoveredInventoryRow >= 0 || _hoveredCargoRow >= 0
            || _isStationNameHovered || _isExitButtonHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    // ── Command sending ─────────────────────────────────────────────────────────

    private void SendTrade(AuthoritativeSnapshot? snapshot, string commandType, string? itemTypeId, long quantity, bool forRefuel = false)
    {
        if (snapshot is null || _handle is null || string.IsNullOrWhiteSpace(snapshot.PlayerShipObjectId) || itemTypeId is null)
            return;
        if (!forRefuel && _selectedItemTypeId is null)
            return;

        string? moduleId = ResolveModuleId(snapshot, commandType);
        if (moduleId is null)
            return;

        _pendingCommandId = _handle.SendTradeCommand(snapshot.PlayerShipObjectId, moduleId, commandType, itemTypeId, quantity);
        _pendingCommandType = commandType;
        _pendingQuantity = quantity;
    }

    /// <summary>
    /// Resolves the installed module that should receive <paramref name="commandType"/>:
    /// the first module (ordered by Position) whose CommandTypeIds contains it. Mirrors
    /// GameSessionScreen.ResolveModuleId exactly (see the story file's grounding note).
    /// </summary>
    private static string? ResolveModuleId(AuthoritativeSnapshot? snapshot, string commandType)
    {
        var modules = snapshot?.InstalledModules;
        if (modules is null || modules.Value.IsDefaultOrEmpty)
            return null;

        return modules.Value
            .Where(m => m.CommandTypeIds.Contains(commandType))
            .OrderBy(m => m.Position)
            .Select(m => m.ModuleId)
            .FirstOrDefault();
    }

    private static InstalledModuleSnapshot? FindModule(AuthoritativeSnapshot? snapshot, string? moduleId)
    {
        if (snapshot is null || moduleId is null || snapshot.InstalledModules.IsDefaultOrEmpty)
            return null;

        foreach (var module in snapshot.InstalledModules)
        {
            if (module.ModuleId == moduleId)
                return module;
        }

        return null;
    }

    /// <summary>
    /// Buy/Sell quantity stepper step size for the currently selected station item (§59,
    /// story-20260825-084409 Batch 3, U10): 100 for <see cref="TradeItemCategories.Resource"/>,
    /// 10 for <see cref="TradeItemCategories.Good"/> (including Fuel sold as cargo — the Refuel
    /// panel's own stepper is unaffected, see <see cref="_refuelQuantity"/>). Read from
    /// <see cref="StationInventoryItemSnapshot.Category"/> as published by the Engine — never
    /// re-derived from the item id on the client. Falls back to 1 when nothing is selected or
    /// the selected id isn't (yet) present in the station's trade snapshot.
    /// </summary>
    private static long ResolveQuantityStep(StationTradeSnapshot? trade, string? itemTypeId)
    {
        if (trade is null || itemTypeId is null || trade.Items.IsDefaultOrEmpty)
            return 1;

        foreach (var item in trade.Items)
        {
            if (item.ItemTypeId != itemTypeId)
                continue;

            return item.Category == TradeItemCategories.Resource ? 100 : 10;
        }

        return 1;
    }

    /// <summary>
    /// Non-authoritative UI ceiling for the quantity stepper — see the type doc comment's
    /// "Quantity stepper upper bound" decision.
    /// </summary>
    private static long ResolveQuantityUpperBound(StationTradeSnapshot? trade, InstalledModuleSnapshot? containerModule, string? itemTypeId)
    {
        if (itemTypeId is null)
            return 1;

        long stationStock = 0;
        if (trade is not null && !trade.Items.IsDefaultOrEmpty)
        {
            foreach (var item in trade.Items)
            {
                if (item.ItemTypeId == itemTypeId)
                {
                    stationStock = item.StockQuantity;
                    break;
                }
            }
        }

        long playerCargoQuantity = 0;
        if (containerModule is { Cargo.IsDefaultOrEmpty: false })
        {
            foreach (var stack in containerModule.Cargo)
            {
                if (stack.ItemTypeId == itemTypeId)
                {
                    playerCargoQuantity = stack.Quantity;
                    break;
                }
            }
        }

        return Math.Max(1, Math.Max(stationStock, playerCargoQuantity));
    }

    // ── Command result → status message ────────────────────────────────────────

    private void ProcessPendingCommandResult(AuthoritativeSnapshot? snapshot)
    {
        if (_pendingCommandId is null || snapshot is null || snapshot.CommandResults.IsDefaultOrEmpty)
            return;

        foreach (var result in snapshot.CommandResults)
        {
            if (result.CommandId != _pendingCommandId)
                continue;

            if (result.Status == CommandResultStatus.Deferred)
                return; // still pending — keep waiting for the final disposition

            _lastStatusMessage = BuildStatusMessage(result);
            _pendingCommandId = null;
            _pendingCommandType = null;
            return;
        }
    }

    private string BuildStatusMessage(CommandResult result)
    {
        if (result.Status is CommandResultStatus.Rejected or CommandResultStatus.Failed or CommandResultStatus.Cancelled)
            return ReasonMessage(result.ReasonCode);

        return _pendingCommandType switch
        {
            TradeCommandTypes.Buy => Localization.Get("Trade.StatusBuySuccess"),
            TradeCommandTypes.Sell => result.ExecutedQuantity is { } executed && executed < _pendingQuantity
                ? string.Format(Localization.Get("Trade.StatusSellPartial"), executed, _pendingQuantity)
                : Localization.Get("Trade.StatusSellSuccess"),
            TradeCommandTypes.Refuel => Localization.Get("Trade.StatusRefuelSuccess"),
            _ => string.Empty
        };
    }

    private static string ReasonMessage(string? reasonCode) => reasonCode switch
    {
        CommandReasonCodes.InsufficientPlayerCredits => Localization.Get("Trade.ReasonInsufficientPlayerCredits"),
        CommandReasonCodes.InsufficientStationStock => Localization.Get("Trade.ReasonInsufficientStationStock"),
        CommandReasonCodes.CargoCapacityExceeded => Localization.Get("Trade.ReasonCargoCapacityExceeded"),
        CommandReasonCodes.FuelCapacityExceeded => Localization.Get("Trade.ReasonFuelCapacityExceeded"),
        CommandReasonCodes.UnknownItemType => Localization.Get("Trade.ReasonUnknownItemType"),
        CommandReasonCodes.NotDocked => Localization.Get("Trade.ReasonNotDocked"),
        CommandReasonCodes.InsufficientCargoQuantity => Localization.Get("Trade.ReasonInsufficientCargoQuantity"),
        CommandReasonCodes.InvalidQuantity => Localization.Get("Trade.ReasonInvalidQuantity"),
        CommandReasonCodes.InvalidPackageQuantity => Localization.Get("Trade.ReasonInvalidPackageQuantity"),
        _ => reasonCode ?? string.Empty
    };

    private static string ItemDisplayName(string itemTypeId) => itemTypeId switch
    {
        "item.energy-cells" => Localization.Get("Trade.ItemEnergyCells"),
        "item.fuel" => Localization.Get("Trade.ItemFuel"),
        "item.ice" => Localization.Get("Trade.ItemIce"),
        "item.iron-ore" => Localization.Get("Trade.ItemIronOre"),
        "item.silicon" => Localization.Get("Trade.ItemSilicon"),
        "item.magnesium-ore" => Localization.Get("Trade.ItemMagnesiumOre"),
        "item.water" => Localization.Get("Trade.ItemWater"),
        "item.steel" => Localization.Get("Trade.ItemSteel"),
        "item.protein-mass" => Localization.Get("Trade.ItemProteinMass"),
        "item.food-rations" => Localization.Get("Trade.ItemFoodRations"),
        _ => itemTypeId
    };

    // ── Rendering ────────────────────────────────────────────────────────────────

    private static readonly SKPaint _subtitlePaint = new()
    {
        Color = MenuStyle.ColorTextDim, TextSize = MenuStyle.StatusFontSize, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _columnTitlePaint = new()
    {
        Color = MenuStyle.ColorText, TextSize = MenuStyle.ButtonFontSize, IsAntialias = true,
        TextAlign = SKTextAlign.Center, Typeface = MenuStyle.TypefaceHumaroid
    };

    private static readonly SKPaint _statLabelPaint = new()
    {
        Color = new SKColor(0x00, 0xFF, 0xFF), TextSize = MenuStyle.StatusFontSize, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceHumaroid
    };

    private static readonly SKPaint _statValuePaint = new()
    {
        Color = MenuStyle.ColorText, TextSize = MenuStyle.ButtonFontSize, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceHumaroid
    };

    private static readonly SKPaint _rowNamePaint = new()
    {
        Color = MenuStyle.ColorText, TextSize = MenuStyle.StatusFontSize + 2f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _rowDetailPaint = new()
    {
        Color = MenuStyle.ColorTextDim, TextSize = MenuStyle.StatusFontSize, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _statusPaint = new()
    {
        Color = MenuStyle.ColorText, TextSize = MenuStyle.StatusFontSize, IsAntialias = true,
        TextAlign = SKTextAlign.Center, Typeface = MenuStyle.TypefaceRegular
    };

    /// <summary>Accent color for the selected row/highlighted values — same orange used elsewhere (ScenarioSelect, ProgressBar default fill).</summary>
    private static readonly SKColor _accentColor = XenonStyle.OrangeAccent;

    private static readonly SKPaint _selectedRowIndicator = new()
    {
        Color = _accentColor, Style = SKPaintStyle.Stroke, StrokeWidth = 2f
    };

    private static readonly SKPaint _hoveredRowBorder = new()
    {
        Color = MenuStyle.ColorTextDim, Style = SKPaintStyle.Stroke, StrokeWidth = 1f
    };

    /// <summary>Centered variant of <see cref="_statValuePaint"/>, for values drawn under a centered column title.</summary>
    private static readonly SKPaint _statValueCenteredPaint = new()
    {
        Color = MenuStyle.ColorText, TextSize = MenuStyle.ButtonFontSize, IsAntialias = true,
        TextAlign = SKTextAlign.Center, Typeface = MenuStyle.TypefaceHumaroid
    };

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        var snapshot = _buffer.Latest?.Snapshot;
        ProcessPendingCommandResult(snapshot);

        float pl = TradeLayout.PanelLeft(width);
        float pt = TradeLayout.PanelTop(height);
        var panelRect = TradeLayout.PanelRect(width, height);
        GenericWindowTypeA.DrawOpaque(canvas, panelRect);
        string tradeTitle = Localization.Get("Trade.Title");
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: false, isHovered: _isStationNameHovered,
            windowName: tradeTitle, isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            isCrewHovered: IsCrewTooltipVisible);

        DrawHeader(canvas, pl, pt, snapshot);

        var trade = snapshot?.DockedStationTrade;
        if (snapshot is null || trade is null)
        {
            DrawNotDockedStatus(canvas, pl, pt);
            DrawExitButton(canvas, pl, pt);
            return;
        }

        string? containerModuleId = ResolveModuleId(snapshot, TradeCommandTypes.Buy);
        string? engineModuleId = ResolveModuleId(snapshot, TradeCommandTypes.Refuel);
        var containerModule = FindModule(snapshot, containerModuleId);
        var engineModule = FindModule(snapshot, engineModuleId);

        DrawStatsRow(canvas, pl, pt, snapshot, containerModule, engineModule);
        DrawStationInventoryColumn(canvas, pl, pt, trade);
        DrawTransactionColumn(canvas, pl, pt, trade, containerModule);
        DrawCargoColumn(canvas, pl, pt, containerModule);
        DrawFuelPanel(canvas, pl, pt, engineModule);
        DrawSummaryRow(canvas, pl, pt, snapshot, trade);
        DrawExitButton(canvas, pl, pt); // drawn last: DrawSummaryRow's ImagePanel would otherwise paint over it
    }

    /// <summary>True when (x, y) lands on the toolbar's station-name link (see StationToolbar).</summary>
    private bool IsStationNameHit(float x, float y, AuthoritativeSnapshot? snapshot)
    {
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        if (string.IsNullOrEmpty(stationName))
            return false;

        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = StationToolbar.NameLocalRect(stationName);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>
    /// True when (x, y) lands on the toolbar's exit-button icon (see StationToolbar) — an
    /// additional way to close Trade alongside the existing bottom-row EXIT button
    /// (<see cref="TradeButton.Close"/>/<see cref="TradeLayout.ExitButtonRect"/>), both
    /// mapped to the same ScreenEvent.CloseTrade.
    /// </summary>
    private bool IsExitButtonHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = StationToolbar.ExitButtonLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's food-rations readout (see StationToolbar).</summary>
    private bool IsFoodRationsHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FoodRationsLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's crew readout (see StationToolbar).</summary>
    private bool IsCrewHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = StationToolbar.CrewLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    private void DrawHeader(SKCanvas canvas, float pl, float pt, AuthoritativeSnapshot? snapshot)
    {
        string subtitle;
        if (snapshot?.DockedStationTrade is { } trade)
        {
            string? stationName = snapshot.Objects.IsDefaultOrEmpty
                ? null
                : snapshot.Objects.FirstOrDefault(o => o.ObjectId == trade.StationObjectId)?.DisplayName;
            subtitle = $"{stationName ?? trade.StationObjectId} — {Localization.Get("Trade.Docked")}";
        }
        else
        {
            subtitle = Localization.Get("Trade.NotDocked");
        }

        canvas.DrawText(subtitle, pl + TradeLayout.HeaderLeftX, pt + TradeLayout.SubtitleBaselineY, _subtitlePaint);
    }

    private void DrawExitButton(SKCanvas canvas, float pl, float pt)
    {
        var rect = CombinedRect(pl, pt, TradeLayout.ExitButtonRect());
        var state = _hoveredButton == TradeButton.Close ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, rect, Localization.Get("Trade.Exit"), state);
    }

    private void DrawNotDockedStatus(SKCanvas canvas, float pl, float pt)
    {
        var rect = new SKRect(pl, pt + TradeLayout.ColumnsTopY, pl + TradeLayout.PanelWidth, pt + TradeLayout.ColumnsTopY + 60f);
        canvas.DrawText(Localization.Get("Trade.NotDocked"), rect.MidX, rect.Top, _statusPaint);
    }

    private void DrawStatsRow(SKCanvas canvas, float pl, float pt, AuthoritativeSnapshot snapshot,
        InstalledModuleSnapshot? containerModule, InstalledModuleSnapshot? engineModule)
    {
        long cargoTotal = 0;
        if (containerModule is { Cargo.IsDefaultOrEmpty: false })
        {
            foreach (var stack in containerModule.Cargo)
                cargoTotal += stack.Quantity;
        }

        DrawStatBlock(canvas, pl, pt, TradeLayout.CreditsStatRect(), Localization.Get("Trade.Credits"), snapshot.PlayerCredits.ToString());
        DrawStatBlock(canvas, pl, pt, TradeLayout.CargoStatRect(), Localization.Get("Trade.Cargo"), cargoTotal.ToString());

        string fuelValue = engineModule?.FuelAmountKg is { } fuelKg ? $"{fuelKg} kg" : "—";
        DrawStatBlock(canvas, pl, pt, TradeLayout.FuelStatRect(), Localization.Get("Trade.Fuel"), fuelValue);
    }

    private void DrawStatBlock(SKCanvas canvas, float pl, float pt, (float X, float Y, float W, float H) local, string label, string value)
    {
        var rect = CombinedRect(pl, pt, local);
        canvas.DrawText(label, rect.Left, rect.Top + 18f, _statLabelPaint);
        canvas.DrawText(value, rect.Left, rect.Top + 40f, _statValuePaint);
    }

    private void DrawStationInventoryColumn(SKCanvas canvas, float pl, float pt, StationTradeSnapshot trade)
    {
        var columnRect = CombinedRect(pl, pt, TradeLayout.StationColumnRect());
        ImagePanel.Draw(canvas, columnRect);
        canvas.DrawText(Localization.Get("Trade.StationInventory"), columnRect.MidX, columnRect.Top + TradeLayout.ColumnTitleBaselineY, _columnTitlePaint);

        if (trade.Items.IsDefaultOrEmpty)
            return;

        for (int i = 0; i < trade.Items.Length; i++)
        {
            var item = trade.Items[i];
            var rowRect = CombinedRect(pl, pt, TradeLayout.InventoryRowRect(i));

            bool isSelected = item.ItemTypeId == _selectedItemTypeId;
            bool isHovered = _hoveredInventoryRow == i;
            if (isSelected)
                canvas.DrawLine(rowRect.Left, rowRect.Top, rowRect.Left, rowRect.Bottom, _selectedRowIndicator);
            else if (isHovered)
                canvas.DrawRect(rowRect, _hoveredRowBorder);

            canvas.Save();
            canvas.ClipRect(rowRect);
            canvas.DrawText(ItemDisplayName(item.ItemTypeId), rowRect.Left + 10f, rowRect.Top + 22f, _rowNamePaint);
            canvas.DrawText(
                $"{Localization.Get("Trade.UnitPrice")}: {item.UnitPriceCredits}   STOCK: {item.StockQuantity}",
                rowRect.Left + 10f, rowRect.Top + 44f, _rowDetailPaint);
            canvas.Restore();
        }
    }

    private void DrawTransactionColumn(SKCanvas canvas, float pl, float pt, StationTradeSnapshot trade, InstalledModuleSnapshot? containerModule)
    {
        var columnRect = CombinedRect(pl, pt, TradeLayout.TransactionColumnRect());
        ImagePanel.Draw(canvas, columnRect);
        canvas.DrawText(Localization.Get("Trade.Transaction"), columnRect.MidX, columnRect.Top + TradeLayout.ColumnTitleBaselineY, _columnTitlePaint);

        var selectedItem = _selectedItemTypeId is null
            ? (StationInventoryItemSnapshot?)null
            : trade.Items.IsDefaultOrEmpty ? null : trade.Items.FirstOrDefault(i => i.ItemTypeId == _selectedItemTypeId);

        if (selectedItem is null)
        {
            canvas.DrawText(Localization.Get("Trade.SelectItemPrompt"), columnRect.MidX,
                columnRect.Top + TradeLayout.TransactionItemNameBaselineY, _statusPaint);
        }
        else
        {
            canvas.DrawText(ItemDisplayName(selectedItem.ItemTypeId), columnRect.MidX,
                columnRect.Top + TradeLayout.TransactionItemNameBaselineY, _columnTitlePaint);

            canvas.DrawText($"{Localization.Get("Trade.UnitPrice")}: {selectedItem.UnitPriceCredits}", columnRect.MidX,
                columnRect.Top + TradeLayout.TransactionUnitPriceBaselineY, _statValueCenteredPaint);
        }

        var stepperRect = CombinedRect(pl, pt, TradeLayout.QuantityStepperRect());
        var stepperHover = _hoveredButton switch
        {
            TradeButton.QuantityMinus => QuantityStepperButton.Minus,
            TradeButton.QuantityPlus => QuantityStepperButton.Plus,
            _ => QuantityStepperButton.None
        };
        QuantityStepper.Draw(canvas, stepperRect, _quantity, stepperHover);

        long total = selectedItem?.UnitPriceCredits * _quantity ?? 0;
        canvas.DrawText($"{Localization.Get("Trade.TotalPrice")}: {total}", columnRect.MidX,
            columnRect.Top + TradeLayout.TransactionTotalBaselineY, _statValueCenteredPaint);

        bool canTrade = selectedItem is not null && containerModule is not null;
        var buyRect = CombinedRect(pl, pt, TradeLayout.BuyButtonRect());
        var buyState = !canTrade ? ButtonState.Disabled : _hoveredButton == TradeButton.Buy ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, buyRect, Localization.Get("Trade.Buy"), buyState);

        var sellRect = CombinedRect(pl, pt, TradeLayout.SellButtonRect());
        var sellState = !canTrade ? ButtonState.Disabled : _hoveredButton == TradeButton.Sell ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, sellRect, Localization.Get("Trade.Sell"), sellState);

        if (!string.IsNullOrEmpty(_lastStatusMessage))
        {
            canvas.DrawText(_lastStatusMessage, columnRect.MidX, columnRect.Bottom - 12f, _statusPaint);
        }
    }

    private void DrawCargoColumn(SKCanvas canvas, float pl, float pt, InstalledModuleSnapshot? containerModule)
    {
        var columnRect = CombinedRect(pl, pt, TradeLayout.CargoListRect());
        ImagePanel.Draw(canvas, columnRect);
        canvas.DrawText(Localization.Get("Trade.YourCargo"), columnRect.MidX, columnRect.Top + TradeLayout.ColumnTitleBaselineY, _columnTitlePaint);

        var cargo = containerModule?.Cargo ?? default;
        if (cargo.IsDefaultOrEmpty)
            return;

        for (int i = 0; i < cargo.Length; i++)
        {
            var stack = cargo[i];
            var rowRect = CombinedRect(pl, pt, TradeLayout.CargoRowRect(i));

            bool isSelected = stack.ItemTypeId == _selectedItemTypeId;
            bool isHovered = _hoveredCargoRow == i;
            if (isSelected)
                canvas.DrawLine(rowRect.Left, rowRect.Top, rowRect.Left, rowRect.Bottom, _selectedRowIndicator);
            else if (isHovered)
                canvas.DrawRect(rowRect, _hoveredRowBorder);

            canvas.Save();
            canvas.ClipRect(rowRect);
            canvas.DrawText($"{ItemDisplayName(stack.ItemTypeId)} × {stack.Quantity}", rowRect.Left + 10f, rowRect.Top + 26f, _rowNamePaint);
            canvas.Restore();
        }
    }

    private void DrawFuelPanel(SKCanvas canvas, float pl, float pt, InstalledModuleSnapshot? engineModule)
    {
        var panelRect = CombinedRect(pl, pt, TradeLayout.FuelPanelRect());
        ImagePanel.Draw(canvas, panelRect);
        canvas.DrawText(Localization.Get("Trade.Fuel"), panelRect.MidX, panelRect.Top + TradeLayout.FuelLabelBaselineY, _columnTitlePaint);

        string fuelValue = engineModule?.FuelAmountKg is { } fuelKg ? $"{fuelKg} kg" : "—";
        canvas.DrawText(fuelValue, panelRect.MidX, panelRect.Top + TradeLayout.FuelValueBaselineY, _statValueCenteredPaint);

        var stepperRect = CombinedRect(pl, pt, TradeLayout.RefuelQuantityStepperRect());
        var stepperHover = _hoveredButton switch
        {
            TradeButton.RefuelQuantityMinus => QuantityStepperButton.Minus,
            TradeButton.RefuelQuantityPlus => QuantityStepperButton.Plus,
            _ => QuantityStepperButton.None
        };
        QuantityStepper.Draw(canvas, stepperRect, _refuelQuantity, stepperHover);

        var refuelRect = CombinedRect(pl, pt, TradeLayout.RefuelButtonRect());
        var refuelState = engineModule is null ? ButtonState.Disabled : _hoveredButton == TradeButton.Refuel ? ButtonState.Hovered : ButtonState.Normal;
        GenericButtonTypeA.Draw(canvas, refuelRect, Localization.Get("Trade.Refuel"), refuelState);
    }

    private void DrawSummaryRow(SKCanvas canvas, float pl, float pt, AuthoritativeSnapshot snapshot, StationTradeSnapshot trade)
    {
        var summaryRect = CombinedRect(pl, pt, TradeLayout.SummaryRect());
        ImagePanel.Draw(canvas, summaryRect);

        canvas.DrawText(Localization.Get("Trade.AccountSummary"), summaryRect.Left + TradeLayout.SummaryPadding,
            summaryRect.Top + TradeLayout.SummaryTitleBaselineY, _statLabelPaint);

        var selectedItem = _selectedItemTypeId is null || trade.Items.IsDefaultOrEmpty
            ? (StationInventoryItemSnapshot?)null
            : trade.Items.FirstOrDefault(i => i.ItemTypeId == _selectedItemTypeId);

        long transactionTotal = selectedItem?.UnitPriceCredits * _quantity ?? 0;
        long projectedBalance = snapshot.PlayerCredits - transactionTotal;

        float blockWidth = (summaryRect.Width - TradeLayout.CancelButtonWidth - TradeLayout.ExitButtonWidth - TradeLayout.ExitButtonGap
            - TradeLayout.CancelButtonMargin - 2 * TradeLayout.SummaryPadding) / 3f;
        float x0 = summaryRect.Left + TradeLayout.SummaryPadding;

        DrawSummaryValue(canvas, x0, summaryRect.Top + TradeLayout.SummaryValuesBaselineY, Localization.Get("Trade.CurrentCredits"), snapshot.PlayerCredits.ToString());
        DrawSummaryValue(canvas, x0 + blockWidth, summaryRect.Top + TradeLayout.SummaryValuesBaselineY, Localization.Get("Trade.TransactionTotal"), transactionTotal.ToString());
        DrawSummaryValue(canvas, x0 + 2 * blockWidth, summaryRect.Top + TradeLayout.SummaryValuesBaselineY, Localization.Get("Trade.ProjectedBalance"), projectedBalance.ToString());

        var cancelRect = CombinedRect(pl, pt, TradeLayout.CancelButtonRect());
        GenericButtonTypeA.Draw(canvas, cancelRect, Localization.Get("Trade.Cancel"),
            _hoveredButton == TradeButton.Cancel ? ButtonState.Hovered : ButtonState.Normal);
    }

    private void DrawSummaryValue(SKCanvas canvas, float x, float baselineY, string label, string value)
    {
        canvas.DrawText(label, x, baselineY - 20f, _statLabelPaint);
        canvas.DrawText(value, x, baselineY, _statValuePaint);
    }

    private static SKRect CombinedRect(float panelLeft, float panelTop, (float X, float Y, float W, float H) local) =>
        new(panelLeft + local.X, panelTop + local.Y, panelLeft + local.X + local.W, panelTop + local.Y + local.H);
}
