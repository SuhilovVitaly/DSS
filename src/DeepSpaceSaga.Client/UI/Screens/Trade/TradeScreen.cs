using System.Collections.Generic;
using System.Linq;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>
/// Trade overlay (Docs/FirstRelease/Screens/Trade.md). Placeholder shell only — the
/// previous Buy/Sell/Refuel MVP was stripped down ahead of a full redesign, so the panel
/// now just shows the shared toolbar and a single "not available yet" line, the same
/// pattern as <see cref="Hire.HireScreen"/>/<see cref="Contracts.ContractsScreen"/>.
/// Opened from <see cref="Station.StationScreen"/>'s `TRADE` button (ScreenEvent.OpenTrade)
/// as a nested modal on top of it; closes via the toolbar's exit-button icon (see
/// StationToolbar), Escape, or a click outside the panel (on the dimmed background),
/// returning to <see cref="Station.StationScreen"/>. Pause-on-open/resume-on-close is
/// handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this screen has no
/// speed/pause logic of its own. Structural twin of
/// <see cref="Hire.HireScreen"/>/<see cref="Contracts.ContractsScreen"/>/
/// <see cref="Station.StationScreen"/>.
/// </summary>
public sealed class TradeScreen : IScreen
{
    private readonly SnapshotBuffer? _buffer;

    private int _screenWidth;
    private int _screenHeight;
    private bool _isStationNameHovered;
    private bool _isExitButtonHovered;
    private bool _isScrollUpHovered;
    private bool _isScrollDownHovered;

    /// <summary>Same role as <see cref="_isScrollUpHovered"/>/<see cref="_isScrollDownHovered"/>, for the Goods grid's own scrollbar arrows (see <see cref="GridPanelOriginYGoods"/>).</summary>
    private bool _isScrollUpHoveredGoods;
    private bool _isScrollDownHoveredGoods;

    /// <summary>
    /// Index of the first resource row currently shown, 0..<see cref="GridPanel.MaxScrollOffset"/>
    /// of the docked station's resource count — clamped after every arrow click/wheel tick/
    /// drag move, and again in <see cref="Render"/> in case the resource count itself
    /// changes between frames.
    /// </summary>
    private int _scrollOffset;

    /// <summary>Test seam — current resources-grid scroll offset (see <see cref="_scrollOffset"/>).</summary>
    internal int ScrollOffset => _scrollOffset;

    /// <summary>True while the scrollbar thumb is being dragged (mouse-down on it, not yet released) — see <see cref="OnMouseDown(float, float, Silk.NET.Input.MouseButton)"/>/<see cref="OnMouseUp"/>.</summary>
    private bool _isDraggingScrollThumb;

    /// <summary>
    /// Vertical distance (local coordinates) from the drag's initial click point down to
    /// the thumb's top edge at that moment — kept fixed for the whole drag so the thumb
    /// stays anchored under the pointer instead of snapping its top to it.
    /// </summary>
    private float _scrollThumbDragGrabOffsetY;

    /// <summary>
    /// Item type id of the resource row the player last clicked — null while nothing is
    /// selected. Identity, not a row index: sorting reorders <see cref="ResolveResourceRows"/>'
    /// result, so the selection is resolved back to whatever index that item currently sits
    /// at (<see cref="ResolveSelectedRowIndex"/>) every time it's needed, rather than being
    /// invalidated by a resort. Never cleared by leaving/re-docking; the item just won't be
    /// visible (index resolves to null) if it drops out of the station's trade snapshot.
    /// </summary>
    private string? _selectedResourceItemTypeId;

    /// <summary>Test seam — current resources-grid row selection, resolved to its index in the current sort order (see <see cref="_selectedResourceItemTypeId"/>).</summary>
    internal int? SelectedResourceIndex =>
        ResolveSelectedRowIndex(ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending));

    /// <summary>Index of <see cref="_selectedResourceItemTypeId"/> within <paramref name="rows"/> (current sort order), or null if nothing is selected or the selected item isn't in <paramref name="rows"/>.</summary>
    private int? ResolveSelectedRowIndex(ResourceRow[] rows)
    {
        if (_selectedResourceItemTypeId is null)
            return null;

        int index = Array.FindIndex(rows, row => row.ItemTypeId == _selectedResourceItemTypeId);
        return index >= 0 ? index : null;
    }

    /// <summary>Column the resources grid is currently sorted by — clicking a column title (<see cref="GridPanel.HitTestColumnTitle"/>) changes this; a second click on the same column flips <see cref="_sortDescending"/> instead.</summary>
    private GridSortColumn _sortColumn = GridSortColumn.Name;
    private bool _sortDescending;

    /// <summary>Test seam — current resources-grid sort column (see <see cref="_sortColumn"/>).</summary>
    internal GridSortColumn SortColumn => _sortColumn;

    /// <summary>Test seam — current resources-grid sort direction (see <see cref="_sortDescending"/>).</summary>
    internal bool SortDescending => _sortDescending;

    /// <summary>
    /// Index of the first good row currently shown — same role as <see cref="_scrollOffset"/>
    /// but for the Goods grid drawn below the Resources grid (see
    /// <see cref="GridPanelOriginYGoods"/>).
    /// </summary>
    private int _scrollOffsetGoods;

    /// <summary>Test seam — current goods-grid scroll offset (see <see cref="_scrollOffsetGoods"/>).</summary>
    internal int ScrollOffsetGoods => _scrollOffsetGoods;

    /// <summary>True while the Goods grid's scrollbar thumb is being dragged — Goods-grid equivalent of <see cref="_isDraggingScrollThumb"/>.</summary>
    private bool _isDraggingScrollThumbGoods;

    /// <summary>Goods-grid equivalent of <see cref="_scrollThumbDragGrabOffsetY"/>.</summary>
    private float _scrollThumbDragGrabOffsetYGoods;

    /// <summary>Identity-based selection for the Goods grid — same scheme as <see cref="_selectedResourceItemTypeId"/>.</summary>
    private string? _selectedGoodItemTypeId;

    /// <summary>Test seam — current goods-grid row selection, resolved to its index in the current sort order (see <see cref="_selectedGoodItemTypeId"/>).</summary>
    internal int? SelectedGoodIndex =>
        ResolveSelectedGoodRowIndex(ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods));

    /// <summary>Index of <see cref="_selectedGoodItemTypeId"/> within <paramref name="rows"/> (current sort order), or null — Goods-grid equivalent of <see cref="ResolveSelectedRowIndex"/>.</summary>
    private int? ResolveSelectedGoodRowIndex(ResourceRow[] rows)
    {
        if (_selectedGoodItemTypeId is null)
            return null;

        int index = Array.FindIndex(rows, row => row.ItemTypeId == _selectedGoodItemTypeId);
        return index >= 0 ? index : null;
    }

    /// <summary>Column the Goods grid is currently sorted by — independent of the Resources grid's <see cref="_sortColumn"/>.</summary>
    private GridSortColumn _sortColumnGoods = GridSortColumn.Name;
    private bool _sortDescendingGoods;

    /// <summary>Test seam — current goods-grid sort column (see <see cref="_sortColumnGoods"/>).</summary>
    internal GridSortColumn SortColumnGoods => _sortColumnGoods;

    /// <summary>Test seam — current goods-grid sort direction (see <see cref="_sortDescendingGoods"/>).</summary>
    internal bool SortDescendingGoods => _sortDescendingGoods;

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

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the tokens readout.</summary>
    private long? _tokensHoverStartedAtMs;

    /// <summary>Test seam — true once the tokens-readout hover delay has elapsed.</summary>
    internal bool IsTokensTooltipVisible =>
        _tokensHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the fuel readout.</summary>
    private long? _fuelHoverStartedAtMs;

    /// <summary>Test seam — true once the fuel-readout hover delay has elapsed.</summary>
    internal bool IsFuelTooltipVisible =>
        _fuelHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    private const string PlaceholderLine = "Trade: awaiting redesign";

    /// <summary>Outline marking the future content area, ahead of the real redesign layout.</summary>
    private static readonly SKRect _contentOutlineRect = new(10f, 90f, 10f + 980f, 90f + 200f);

    private static readonly SKPaint _contentOutlinePaint = new()
    {
        Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true
    };

    /// <summary>Anchor (the header bar's top-left) for the <see cref="GridPanel"/> resources list — see that control's doc comment for how header/rows/scrollbar are laid out relative to this point.</summary>
    private const float GridPanelOriginX = 15f;
    private const float GridPanelOriginY = 76f;
    private const string ResourcesGridTitle = "Resources";

    /// <summary>
    /// Anchor for the Goods grid, same <see cref="GridPanelOriginX"/> column directly below
    /// the Resources grid: <see cref="GridPanelOriginY"/> (76) + header-to-last-row-bottom
    /// (44 + <see cref="GridPanel.MaxVisibleRows"/>×30 = 194) + a 30px gap between the two panels.
    /// </summary>
    private const float GridPanelOriginYGoods = GridPanelOriginY + 194f + 30f;
    private const string GoodsGridTitle = "Goods";

    /// <summary>Test seam — the resources grid's current row labels (see <see cref="ResolveResourceRows"/>).</summary>
    internal string[] ResourceNames => ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending).Select(row => row.Name).ToArray();

    /// <summary>Test seam — the resources grid's current "Selling price" column values (station's UnitPriceCredits), same row order as <see cref="ResourceNames"/>.</summary>
    internal string[] ResourceSellingPrices => ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending).Select(row => row.SellingPrice).ToArray();

    /// <summary>Test seam — the resources grid's current "Selling count" column values (station's StockQuantity), same row order as <see cref="ResourceNames"/>.</summary>
    internal string[] ResourceSellingCounts => ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending).Select(row => row.SellingCount).ToArray();

    /// <summary>Test seam — the resources grid's current "Buying price" column values, same row order as <see cref="ResourceNames"/>.</summary>
    internal string[] ResourceBuyingPrices => ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending).Select(row => row.BuyingPrice).ToArray();

    /// <summary>Test seam — the resources grid's current "Buying count" column values (player's ship cargo quantity), same row order as <see cref="ResourceNames"/>.</summary>
    internal string[] ResourceBuyingCounts => ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending).Select(row => row.BuyingCount).ToArray();

    /// <summary>Test seam — the goods grid's current row labels (see <see cref="ResolveGoodRows"/>).</summary>
    internal string[] GoodNames => ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods).Select(row => row.Name).ToArray();

    /// <summary>Test seam — the goods grid's current "Selling price" column values, same row order as <see cref="GoodNames"/>.</summary>
    internal string[] GoodSellingPrices => ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods).Select(row => row.SellingPrice).ToArray();

    /// <summary>Test seam — the goods grid's current "Selling count" column values, same row order as <see cref="GoodNames"/>.</summary>
    internal string[] GoodSellingCounts => ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods).Select(row => row.SellingCount).ToArray();

    /// <summary>Test seam — the goods grid's current "Buying price" column values, same row order as <see cref="GoodNames"/>.</summary>
    internal string[] GoodBuyingPrices => ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods).Select(row => row.BuyingPrice).ToArray();

    /// <summary>Test seam — the goods grid's current "Buying count" column values, same row order as <see cref="GoodNames"/>.</summary>
    internal string[] GoodBuyingCounts => ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods).Select(row => row.BuyingCount).ToArray();

    /// <summary>
    /// One resources-grid row: display name plus its Selling/Buying price+count column
    /// text. <see cref="ItemTypeId"/> is the stable identity used to carry row selection
    /// across a resort (<see cref="ResolveSelectedRowIndex"/>) — never shown, since
    /// GridPanel only ever receives the formatted display strings.
    /// </summary>
    private readonly record struct ResourceRow(string ItemTypeId, string Name, string SellingPrice, string SellingCount, string BuyingPrice, string BuyingCount);

    /// <summary>Same fields as <see cref="ResourceRow"/> but numeric — sorting must happen on these, not their formatted string form (lexicographic "100" &lt; "20" would otherwise corrupt price/count ordering).</summary>
    private readonly record struct ResourceRowData(string ItemTypeId, string Name, long SellingPrice, long SellingCount, long BuyingPrice, long BuyingCount);

    /// <summary>
    /// The docked station's <see cref="TradeItemCategories.Resource"/> items, sorted by
    /// <paramref name="sortColumn"/>/<paramref name="sortDescending"/> (set by clicking a
    /// column title — see <see cref="GridPanel.HitTestColumnTitle"/>) — empty (not null)
    /// while undocked or before the first snapshot arrives, which <see cref="GridPanel"/>
    /// renders as its dark-gray empty state. Selling price/count are the station's own
    /// <c>UnitPriceCredits</c>/<c>StockQuantity</c>; this MVP model has only one price per
    /// item (no separate buy/sell price), so Buying price reuses it too — only Buying count
    /// differs, sourced from the player's own ship cargo (<see cref="ResolvePlayerCargo"/>)
    /// rather than the station.
    /// </summary>
    private static ResourceRow[] ResolveResourceRows(AuthoritativeSnapshot? snapshot, GridSortColumn sortColumn, bool sortDescending)
    {
        var items = snapshot?.DockedStationTrade?.Items ?? default;
        if (items.IsDefaultOrEmpty)
            return Array.Empty<ResourceRow>();

        var playerCargo = ResolvePlayerCargo(snapshot);

        var rows = items
            .Where(item => item.Category == TradeItemCategories.Resource)
            .Select(item =>
            {
                long shipQuantity = playerCargo.TryGetValue(item.ItemTypeId, out long quantity) ? quantity : 0;
                return new ResourceRowData(
                    item.ItemTypeId, ItemDisplayName(item.ItemTypeId),
                    SellingPrice: item.UnitPriceCredits, SellingCount: item.StockQuantity,
                    BuyingPrice: item.UnitPriceCredits, BuyingCount: shipQuantity);
            });

        IEnumerable<ResourceRowData> sorted = (sortColumn, sortDescending) switch
        {
            (GridSortColumn.Name, false) => rows.OrderBy(row => row.Name, StringComparer.Ordinal),
            (GridSortColumn.Name, true) => rows.OrderByDescending(row => row.Name, StringComparer.Ordinal),
            (GridSortColumn.SellingPrice, false) => rows.OrderBy(row => row.SellingPrice),
            (GridSortColumn.SellingPrice, true) => rows.OrderByDescending(row => row.SellingPrice),
            (GridSortColumn.SellingCount, false) => rows.OrderBy(row => row.SellingCount),
            (GridSortColumn.SellingCount, true) => rows.OrderByDescending(row => row.SellingCount),
            (GridSortColumn.BuyingPrice, false) => rows.OrderBy(row => row.BuyingPrice),
            (GridSortColumn.BuyingPrice, true) => rows.OrderByDescending(row => row.BuyingPrice),
            (GridSortColumn.BuyingCount, false) => rows.OrderBy(row => row.BuyingCount),
            (GridSortColumn.BuyingCount, true) => rows.OrderByDescending(row => row.BuyingCount),
            _ => rows.OrderBy(row => row.Name, StringComparer.Ordinal)
        };

        return sorted
            .Select(row => new ResourceRow(
                row.ItemTypeId, row.Name, row.SellingPrice.ToString(), row.SellingCount.ToString(),
                row.BuyingPrice.ToString(), row.BuyingCount.ToString()))
            .ToArray();
    }

    /// <summary>
    /// The docked station's <see cref="TradeItemCategories.Good"/> items — same shape,
    /// sourcing and sort logic as <see cref="ResolveResourceRows"/>, just filtered to the
    /// other trade category (e.g. Fuel, Energy Cells, Food Rations) for the Goods grid.
    /// </summary>
    private static ResourceRow[] ResolveGoodRows(AuthoritativeSnapshot? snapshot, GridSortColumn sortColumn, bool sortDescending)
    {
        var items = snapshot?.DockedStationTrade?.Items ?? default;
        if (items.IsDefaultOrEmpty)
            return Array.Empty<ResourceRow>();

        var playerCargo = ResolvePlayerCargo(snapshot);

        var rows = items
            .Where(item => item.Category == TradeItemCategories.Good)
            .Select(item =>
            {
                long shipQuantity = playerCargo.TryGetValue(item.ItemTypeId, out long quantity) ? quantity : 0;
                return new ResourceRowData(
                    item.ItemTypeId, ItemDisplayName(item.ItemTypeId),
                    SellingPrice: item.UnitPriceCredits, SellingCount: item.StockQuantity,
                    BuyingPrice: item.UnitPriceCredits, BuyingCount: shipQuantity);
            });

        IEnumerable<ResourceRowData> sorted = (sortColumn, sortDescending) switch
        {
            (GridSortColumn.Name, false) => rows.OrderBy(row => row.Name, StringComparer.Ordinal),
            (GridSortColumn.Name, true) => rows.OrderByDescending(row => row.Name, StringComparer.Ordinal),
            (GridSortColumn.SellingPrice, false) => rows.OrderBy(row => row.SellingPrice),
            (GridSortColumn.SellingPrice, true) => rows.OrderByDescending(row => row.SellingPrice),
            (GridSortColumn.SellingCount, false) => rows.OrderBy(row => row.SellingCount),
            (GridSortColumn.SellingCount, true) => rows.OrderByDescending(row => row.SellingCount),
            (GridSortColumn.BuyingPrice, false) => rows.OrderBy(row => row.BuyingPrice),
            (GridSortColumn.BuyingPrice, true) => rows.OrderByDescending(row => row.BuyingPrice),
            (GridSortColumn.BuyingCount, false) => rows.OrderBy(row => row.BuyingCount),
            (GridSortColumn.BuyingCount, true) => rows.OrderByDescending(row => row.BuyingCount),
            _ => rows.OrderBy(row => row.Name, StringComparer.Ordinal)
        };

        return sorted
            .Select(row => new ResourceRow(
                row.ItemTypeId, row.Name, row.SellingPrice.ToString(), row.SellingCount.ToString(),
                row.BuyingPrice.ToString(), row.BuyingCount.ToString()))
            .ToArray();
    }

    /// <summary>
    /// Player's ship cargo, keyed by item type id — from the first installed module (by
    /// Position) whose CommandTypeIds handles Buy, the same "container module" resolution
    /// the pre-redesign TradeScreen used (story file's ResolveModuleId/FindModule).
    /// </summary>
    private static Dictionary<string, long> ResolvePlayerCargo(AuthoritativeSnapshot? snapshot)
    {
        var modules = snapshot?.InstalledModules;
        if (modules is null || modules.Value.IsDefaultOrEmpty)
            return new Dictionary<string, long>();

        var containerModule = modules.Value
            .Where(m => m.CommandTypeIds.Contains(TradeCommandTypes.Buy))
            .OrderBy(m => m.Position)
            .FirstOrDefault();

        if (containerModule is null || containerModule.Cargo.IsDefaultOrEmpty)
            return new Dictionary<string, long>();

        return containerModule.Cargo.ToDictionary(stack => stack.ItemTypeId, stack => stack.Quantity);
    }

    /// <summary>
    /// Maps a resource item type id to its localized display name — mirrors the pre-
    /// redesign TradeScreen's item-name switch (story-20260825-084409 Batch 3): the client
    /// has no item-type display-name catalog yet, so known ids are mapped by hand, falling
    /// back to the raw id for any future/unknown one.
    /// </summary>
    private static string ItemDisplayName(string itemTypeId) => itemTypeId switch
    {
        "item.ice" => Localization.Get("Trade.ItemIce"),
        "item.iron-ore" => Localization.Get("Trade.ItemIronOre"),
        "item.silicon" => Localization.Get("Trade.ItemSilicon"),
        "item.magnesium-ore" => Localization.Get("Trade.ItemMagnesiumOre"),
        "item.uranium-ore" => Localization.Get("Trade.ItemUraniumOre"),
        "item.carbon-ore" => Localization.Get("Trade.ItemCarbonOre"),
        "item.water" => Localization.Get("Trade.ItemWater"),
        "item.steel" => Localization.Get("Trade.ItemSteel"),
        "item.energy-cells" => Localization.Get("Trade.ItemEnergyCells"),
        "item.fuel" => Localization.Get("Trade.ItemFuel"),
        "item.protein-mass" => Localization.Get("Trade.ItemProteinMass"),
        "item.food-rations" => Localization.Get("Trade.ItemFoodRations"),
        _ => itemTypeId
    };

    public TradeScreen(SnapshotBuffer? buffer = null)
    {
        _buffer = buffer;
    }

    public void OnActivated()
    {
        _isStationNameHovered = false;
        _isExitButtonHovered = false;
        _isScrollUpHovered = false;
        _isScrollDownHovered = false;
        _isDraggingScrollThumb = false;
        _selectedResourceItemTypeId = null;
        _isScrollUpHoveredGoods = false;
        _isScrollDownHoveredGoods = false;
        _isDraggingScrollThumbGoods = false;
        _selectedGoodItemTypeId = null;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;
        _tokensHoverStartedAtMs = null;
        _fuelHoverStartedAtMs = null;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseTrade : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        if (IsExitButtonHit(x, y))
            return ScreenEvent.CloseTrade;

        if (IsStationNameHit(x, y))
            return ScreenEvent.NavigateToStation;

        var resourceRows = ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending);
        int resourceRowCount = resourceRows.Length;
        if (GridPanel.IsScrollbarActive(resourceRowCount))
        {
            if (IsScrollUpArrowHit(x, y))
            {
                _scrollOffset = Math.Max(0, _scrollOffset - 1);
                return ScreenEvent.None;
            }

            if (IsScrollDownArrowHit(x, y))
            {
                _scrollOffset = Math.Min(GridPanel.MaxScrollOffset(resourceRowCount), _scrollOffset + 1);
                return ScreenEvent.None;
            }

            float pt = TradeLayout.PanelTop(_screenHeight);
            var thumbLocal = GridPanel.ScrollThumbLocalRect(GridPanelOriginX, GridPanelOriginY, resourceRowCount, _scrollOffset);
            float clickLocalY = y - pt;
            if (IsScrollThumbHit(x, y))
            {
                _isDraggingScrollThumb = true;
                _scrollThumbDragGrabOffsetY = clickLocalY - thumbLocal.Top;
                return ScreenEvent.None;
            }
        }

        var hitColumnTitle = HitTestColumnTitle(x, y);
        if (hitColumnTitle is { } clickedColumn)
        {
            _sortDescending = _sortColumn == clickedColumn && !_sortDescending;
            _sortColumn = clickedColumn;
            // Selection is identity-based (_selectedResourceItemTypeId), not index-based —
            // it survives the resort and is resolved back to wherever that item now sits.
            return ScreenEvent.None;
        }

        int hitRowIndex = HitTestResourceRow(x, y, resourceRowCount);
        if (hitRowIndex >= 0)
        {
            _selectedResourceItemTypeId = resourceRows[hitRowIndex].ItemTypeId;
            return ScreenEvent.None;
        }

        var goodRows = ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods);
        int goodRowCount = goodRows.Length;
        if (GridPanel.IsScrollbarActive(goodRowCount))
        {
            if (IsScrollUpArrowHitGoods(x, y))
            {
                _scrollOffsetGoods = Math.Max(0, _scrollOffsetGoods - 1);
                return ScreenEvent.None;
            }

            if (IsScrollDownArrowHitGoods(x, y))
            {
                _scrollOffsetGoods = Math.Min(GridPanel.MaxScrollOffset(goodRowCount), _scrollOffsetGoods + 1);
                return ScreenEvent.None;
            }

            float pt = TradeLayout.PanelTop(_screenHeight);
            var thumbLocalGoods = GridPanel.ScrollThumbLocalRect(GridPanelOriginX, GridPanelOriginYGoods, goodRowCount, _scrollOffsetGoods);
            float clickLocalYGoods = y - pt;
            if (IsScrollThumbHitGoods(x, y))
            {
                _isDraggingScrollThumbGoods = true;
                _scrollThumbDragGrabOffsetYGoods = clickLocalYGoods - thumbLocalGoods.Top;
                return ScreenEvent.None;
            }
        }

        var hitGoodColumnTitle = HitTestGoodColumnTitle(x, y);
        if (hitGoodColumnTitle is { } clickedGoodColumn)
        {
            _sortDescendingGoods = _sortColumnGoods == clickedGoodColumn && !_sortDescendingGoods;
            _sortColumnGoods = clickedGoodColumn;
            // Selection is identity-based (_selectedGoodItemTypeId), not index-based — it
            // survives the resort and is resolved back to wherever that item now sits.
            return ScreenEvent.None;
        }

        int hitGoodRowIndex = HitTestGoodRow(x, y, goodRowCount);
        if (hitGoodRowIndex >= 0)
        {
            _selectedGoodItemTypeId = goodRows[hitGoodRowIndex].ItemTypeId;
            return ScreenEvent.None;
        }

        // Click on the dimmed background outside the panel also closes it.
        if (!TradeLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseTrade;

        return ScreenEvent.None;
    }

    /// <summary>Absolute resources-grid row index hit by a click at screen coordinates (x, y), or -1 — see <see cref="GridPanel.HitTestRow"/>.</summary>
    private int HitTestResourceRow(float x, float y, int resourceRowCount)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        return GridPanel.HitTestRow(GridPanelOriginX, GridPanelOriginY, resourceRowCount, _scrollOffset, x - pl, y - pt);
    }

    /// <summary>Absolute goods-grid row index hit by a click at screen coordinates (x, y), or -1 — see <see cref="GridPanel.HitTestRow"/>.</summary>
    private int HitTestGoodRow(float x, float y, int goodRowCount)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        return GridPanel.HitTestRow(GridPanelOriginX, GridPanelOriginYGoods, goodRowCount, _scrollOffsetGoods, x - pl, y - pt);
    }

    /// <summary>Sortable column title (see <see cref="GridPanel.HitTestColumnTitle"/>) at screen coordinates (x, y), or null — drives both click-to-sort and the hover cursor swap.</summary>
    private GridSortColumn? HitTestColumnTitle(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        return GridPanel.HitTestColumnTitle(GridPanelOriginX, GridPanelOriginY, ResourcesGridTitle, x - pl, y - pt);
    }

    /// <summary>Same as <see cref="HitTestColumnTitle"/> but for the Goods grid.</summary>
    private GridSortColumn? HitTestGoodColumnTitle(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        return GridPanel.HitTestColumnTitle(GridPanelOriginX, GridPanelOriginYGoods, GoodsGridTitle, x - pl, y - pt);
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    /// <summary>Ends a scrollbar-thumb drag on left-button release, wherever the pointer ends up — see <see cref="_isDraggingScrollThumb"/>/<see cref="_isDraggingScrollThumbGoods"/>.</summary>
    public void OnMouseUp(float x, float y)
    {
        _isDraggingScrollThumb = false;
        _isDraggingScrollThumbGoods = false;
    }

    public bool OnMouseMove(float x, float y)
    {
        if (_isDraggingScrollThumb)
        {
            int resourceRowCount = CurrentResourceRowCount();
            float pt = TradeLayout.PanelTop(_screenHeight);
            float desiredThumbTopLocalY = (y - pt) - _scrollThumbDragGrabOffsetY;
            _scrollOffset = GridPanel.ResolveScrollOffsetForThumbTop(GridPanelOriginX, GridPanelOriginY, resourceRowCount, desiredThumbTopLocalY);
        }

        if (_isDraggingScrollThumbGoods)
        {
            int goodRowCount = CurrentGoodRowCount();
            float pt = TradeLayout.PanelTop(_screenHeight);
            float desiredThumbTopLocalYGoods = (y - pt) - _scrollThumbDragGrabOffsetYGoods;
            _scrollOffsetGoods = GridPanel.ResolveScrollOffsetForThumbTop(GridPanelOriginX, GridPanelOriginYGoods, goodRowCount, desiredThumbTopLocalYGoods);
        }

        _isStationNameHovered = IsStationNameHit(x, y);
        _isExitButtonHovered = IsExitButtonHit(x, y);
        bool isScrollbarActive = GridPanel.IsScrollbarActive(CurrentResourceRowCount());
        _isScrollUpHovered = isScrollbarActive && IsScrollUpArrowHit(x, y);
        _isScrollDownHovered = isScrollbarActive && IsScrollDownArrowHit(x, y);
        bool isColumnTitleHovered = HitTestColumnTitle(x, y) is not null;

        bool isGoodsScrollbarActive = GridPanel.IsScrollbarActive(CurrentGoodRowCount());
        _isScrollUpHoveredGoods = isGoodsScrollbarActive && IsScrollUpArrowHitGoods(x, y);
        _isScrollDownHoveredGoods = isGoodsScrollbarActive && IsScrollDownArrowHitGoods(x, y);
        bool isGoodColumnTitleHovered = HitTestGoodColumnTitle(x, y) is not null;

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

        if (IsTokensHit(x, y))
            _tokensHoverStartedAtMs ??= Environment.TickCount64;
        else
            _tokensHoverStartedAtMs = null;

        if (IsFuelHit(x, y))
            _fuelHoverStartedAtMs ??= Environment.TickCount64;
        else
            _fuelHoverStartedAtMs = null;

        return _isStationNameHovered || _isExitButtonHovered || _isScrollUpHovered || _isScrollDownHovered
            || _isDraggingScrollThumb || isColumnTitleHovered
            || _isScrollUpHoveredGoods || _isScrollDownHoveredGoods || _isDraggingScrollThumbGoods || isGoodColumnTitleHovered;
    }

    /// <summary>
    /// Scrolls whichever grid the pointer is currently over one row per wheel tick — same
    /// direction convention as SaveScreen's slot-list wheel scroll. Defaults to the
    /// Resources grid when the pointer is over neither grid's row area, matching this
    /// method's original (position-agnostic) behavior from before the Goods grid existed.
    /// </summary>
    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (IsWithinGoodGridRows(y))
        {
            int maxOffsetGoods = GridPanel.MaxScrollOffset(CurrentGoodRowCount());
            _scrollOffsetGoods = Math.Clamp(_scrollOffsetGoods - Math.Sign(delta), 0, maxOffsetGoods);
            return ScreenEvent.None;
        }

        int maxOffset = GridPanel.MaxScrollOffset(CurrentResourceRowCount());
        _scrollOffset = Math.Clamp(_scrollOffset - Math.Sign(delta), 0, maxOffset);
        return ScreenEvent.None;
    }

    /// <summary>True when screen-space <paramref name="y"/> falls within the Goods grid's header+rows band (see <see cref="GridPanelOriginYGoods"/>) — used to route wheel scrolling to the grid under the pointer.</summary>
    private bool IsWithinGoodGridRows(float y)
    {
        float pt = TradeLayout.PanelTop(_screenHeight);
        var header = GridPanel.HeaderLocalRect(GridPanelOriginX, GridPanelOriginYGoods);
        var lastRow = GridPanel.RowLocalRect(GridPanelOriginX, GridPanelOriginYGoods, GridPanel.MaxVisibleRows - 1);
        return y >= pt + header.Top && y <= pt + lastRow.Bottom;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = TradeLayout.PanelLeft(width);
        float pt = TradeLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + TradeLayout.PanelWidth, pt + TradeLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        var snapshot = _buffer?.Latest?.Snapshot;
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: false, isHovered: _isStationNameHovered,
            windowName: "TRADE", isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot));

        float cx = pl + TradeLayout.PanelWidth / 2f;
        canvas.DrawText(PlaceholderLine, cx, pt + TradeLayout.BodyStartY, MenuStyle.TextStatus);

        var contentRect = new SKRect(pl + _contentOutlineRect.Left, pt + _contentOutlineRect.Top,
            pl + _contentOutlineRect.Right, pt + _contentOutlineRect.Bottom);
        canvas.DrawRect(contentRect, _contentOutlinePaint);

        var resourceRows = ResolveResourceRows(snapshot, _sortColumn, _sortDescending);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, GridPanel.MaxScrollOffset(resourceRows.Length));
        var resourceNames = Array.ConvertAll(resourceRows, row => row.Name);
        var resourceSellingPrices = Array.ConvertAll(resourceRows, row => row.SellingPrice);
        var resourceSellingCounts = Array.ConvertAll(resourceRows, row => row.SellingCount);
        var resourceBuyingPrices = Array.ConvertAll(resourceRows, row => row.BuyingPrice);
        var resourceBuyingCounts = Array.ConvertAll(resourceRows, row => row.BuyingCount);
        GridPanel.Draw(canvas, pl + GridPanelOriginX, pt + GridPanelOriginY, ResourcesGridTitle, resourceRows.Length,
            _scrollOffset, _isScrollUpHovered, _isScrollDownHovered, resourceNames,
            resourceSellingPrices, resourceSellingCounts, resourceBuyingPrices, resourceBuyingCounts,
            ResolveSelectedRowIndex(resourceRows), _sortColumn, _sortDescending);

        var goodRows = ResolveGoodRows(snapshot, _sortColumnGoods, _sortDescendingGoods);
        _scrollOffsetGoods = Math.Clamp(_scrollOffsetGoods, 0, GridPanel.MaxScrollOffset(goodRows.Length));
        var goodNames = Array.ConvertAll(goodRows, row => row.Name);
        var goodSellingPrices = Array.ConvertAll(goodRows, row => row.SellingPrice);
        var goodSellingCounts = Array.ConvertAll(goodRows, row => row.SellingCount);
        var goodBuyingPrices = Array.ConvertAll(goodRows, row => row.BuyingPrice);
        var goodBuyingCounts = Array.ConvertAll(goodRows, row => row.BuyingCount);
        GridPanel.Draw(canvas, pl + GridPanelOriginX, pt + GridPanelOriginYGoods, GoodsGridTitle, goodRows.Length,
            _scrollOffsetGoods, _isScrollUpHoveredGoods, _isScrollDownHoveredGoods, goodNames,
            goodSellingPrices, goodSellingCounts, goodBuyingPrices, goodBuyingCounts,
            ResolveSelectedGoodRowIndex(goodRows), _sortColumnGoods, _sortDescendingGoods);

        // Drawn last: the tooltip hangs below the toolbar into the body area and must
        // stay on top of everything the screen drew.
        StationToolbar.DrawTooltips(canvas, pl, pt,
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            isCrewHovered: IsCrewTooltipVisible,
            isTokensHovered: IsTokensTooltipVisible,
            isFuelHovered: IsFuelTooltipVisible);
    }

    /// <summary>Current resources-grid row count, from the docked station's live trade snapshot — see <see cref="ResolveResourceRows"/>.</summary>
    private int CurrentResourceRowCount() => ResolveResourceRows(_buffer?.Latest?.Snapshot, _sortColumn, _sortDescending).Length;

    /// <summary>Current goods-grid row count, from the docked station's live trade snapshot — see <see cref="ResolveGoodRows"/>.</summary>
    private int CurrentGoodRowCount() => ResolveGoodRows(_buffer?.Latest?.Snapshot, _sortColumnGoods, _sortDescendingGoods).Length;

    /// <summary>True when (x, y) lands on the resources grid's scrollbar up-arrow button (see <see cref="GridPanel.ScrollUpArrowLocalRect"/>).</summary>
    private bool IsScrollUpArrowHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollUpArrowLocalRect(GridPanelOriginX, GridPanelOriginY, CurrentResourceRowCount());
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the resources grid's scrollbar down-arrow button (see <see cref="GridPanel.ScrollDownArrowLocalRect"/>).</summary>
    private bool IsScrollDownArrowHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollDownArrowLocalRect(GridPanelOriginX, GridPanelOriginY, CurrentResourceRowCount());
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the resources grid's scrollbar thumb (see <see cref="GridPanel.ScrollThumbLocalRect"/>) — a drag-start hit-test, checked with the current (pre-drag) <see cref="_scrollOffset"/>.</summary>
    private bool IsScrollThumbHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollThumbLocalRect(GridPanelOriginX, GridPanelOriginY, CurrentResourceRowCount(), _scrollOffset);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>Same as <see cref="IsScrollUpArrowHit"/> but for the Goods grid's scrollbar.</summary>
    private bool IsScrollUpArrowHitGoods(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollUpArrowLocalRect(GridPanelOriginX, GridPanelOriginYGoods, CurrentGoodRowCount());
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>Same as <see cref="IsScrollDownArrowHit"/> but for the Goods grid's scrollbar.</summary>
    private bool IsScrollDownArrowHitGoods(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollDownArrowLocalRect(GridPanelOriginX, GridPanelOriginYGoods, CurrentGoodRowCount());
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>Same as <see cref="IsScrollThumbHit"/> but for the Goods grid's scrollbar thumb (checked against <see cref="_scrollOffsetGoods"/>).</summary>
    private bool IsScrollThumbHitGoods(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollThumbLocalRect(GridPanelOriginX, GridPanelOriginYGoods, CurrentGoodRowCount(), _scrollOffsetGoods);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's station-name link (see StationToolbar).</summary>
    private bool IsStationNameHit(float x, float y)
    {
        string? stationName = StationToolbar.ResolveDockedStationName(_buffer?.Latest?.Snapshot);
        if (string.IsNullOrEmpty(stationName))
            return false;

        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = StationToolbar.NameLocalRect(stationName);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's exit-button icon (see StationToolbar).</summary>
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

    /// <summary>True when (x, y) lands on the toolbar's tokens/credits readout (see StationToolbar).</summary>
    private bool IsTokensHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = StationToolbar.TokensLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's fuel readout (see StationToolbar).</summary>
    private bool IsFuelHit(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FuelLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }
}
