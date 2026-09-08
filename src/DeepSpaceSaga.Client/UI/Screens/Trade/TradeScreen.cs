using System.Collections.Generic;
using System.Linq;
using DeepSpaceSaga.Client.UI.Assets;
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

    /// <summary>
    /// Session handle used to send Buy/Sell/Refuel trade commands (Docs/FirstRelease/Screens/Trade.md,
    /// "UI-решение: панель действия") — same fire-and-forget pattern as
    /// <see cref="Station.StationScreen"/>/<see cref="GameSession.GameSessionScreen"/>, null in
    /// tests that construct this screen directly against a SnapshotBuffer without a live session.
    /// </summary>
    private readonly GameSessionHandle? _handle;

    private int _screenWidth;
    private int _screenHeight;
    private bool _isStationNameHovered;
    private bool _isExitButtonHovered;
    private bool _isScrollUpHovered;
    private bool _isScrollDownHovered;

    /// <summary>Same role as <see cref="_isScrollUpHovered"/>/<see cref="_isScrollDownHovered"/>, for the Goods grid's own scrollbar arrows (see <see cref="GridPanelOriginYGoods"/>).</summary>
    private bool _isScrollUpHoveredGoods;
    private bool _isScrollDownHoveredGoods;

    /// <summary>Same role as <see cref="_isScrollUpHovered"/>/<see cref="_isScrollDownHovered"/>, for the Modules grid's own scrollbar arrows (see <see cref="GridPanelOriginYModules"/>).</summary>
    private bool _isScrollUpHoveredModules;
    private bool _isScrollDownHoveredModules;

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
    /// Index of the first module row currently shown — same role as <see cref="_scrollOffset"/>
    /// but for the Modules grid drawn below the Goods grid (see
    /// <see cref="GridPanelOriginYModules"/>).
    /// </summary>
    private int _scrollOffsetModules;

    /// <summary>Test seam — current modules-grid scroll offset (see <see cref="_scrollOffsetModules"/>).</summary>
    internal int ScrollOffsetModules => _scrollOffsetModules;

    /// <summary>True while the Modules grid's scrollbar thumb is being dragged — Modules-grid equivalent of <see cref="_isDraggingScrollThumb"/>.</summary>
    private bool _isDraggingScrollThumbModules;

    /// <summary>Modules-grid equivalent of <see cref="_scrollThumbDragGrabOffsetY"/>.</summary>
    private float _scrollThumbDragGrabOffsetYModules;

    /// <summary>Identity-based selection for the Modules grid — same scheme as <see cref="_selectedResourceItemTypeId"/>.</summary>
    private string? _selectedModuleItemTypeId;

    /// <summary>Test seam — current modules-grid row selection, resolved to its index in the current sort order (see <see cref="_selectedModuleItemTypeId"/>).</summary>
    internal int? SelectedModuleIndex =>
        ResolveSelectedModuleRowIndex(ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules));

    /// <summary>Index of <see cref="_selectedModuleItemTypeId"/> within <paramref name="rows"/> (current sort order), or null — Modules-grid equivalent of <see cref="ResolveSelectedRowIndex"/>.</summary>
    private int? ResolveSelectedModuleRowIndex(ResourceRow[] rows)
    {
        if (_selectedModuleItemTypeId is null)
            return null;

        int index = Array.FindIndex(rows, row => row.ItemTypeId == _selectedModuleItemTypeId);
        return index >= 0 ? index : null;
    }

    /// <summary>Column the Modules grid is currently sorted by — independent of the other two grids' sort state.</summary>
    private GridSortColumn _sortColumnModules = GridSortColumn.Name;
    private bool _sortDescendingModules;

    /// <summary>Test seam — current modules-grid sort column (see <see cref="_sortColumnModules"/>).</summary>
    internal GridSortColumn SortColumnModules => _sortColumnModules;

    /// <summary>Test seam — current modules-grid sort direction (see <see cref="_sortDescendingModules"/>).</summary>
    internal bool SortDescendingModules => _sortDescendingModules;

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

    /// <summary>
    /// Outline marking the future content area around the Resources grid, ahead of the real
    /// redesign layout. Left edge sits 5px left of <see cref="GridPanelOriginX"/> (215) — the
    /// same 980-wide, 200-tall outline as the pre-widening layout, just shifted right by the
    /// same 200px the whole grid column moved to make room for the ship-compartment
    /// illustration in the newly added panel width (<see cref="TradeLayout.PanelWidth"/>).
    /// </summary>
    private static readonly SKRect _contentOutlineRect = new(210f, 90f, 210f + 980f, 90f + 200f);

    /// <summary>Same outline as <see cref="_contentOutlineRect"/>, shifted down by the same offset as <see cref="GridPanelOriginYGoods"/> is from <see cref="GridPanelOriginY"/>, to frame the Goods grid below it.</summary>
    private static readonly SKRect _contentOutlineRectGoods = new(
        _contentOutlineRect.Left, _contentOutlineRect.Top + (GridPanelOriginYGoods - GridPanelOriginY),
        _contentOutlineRect.Right, _contentOutlineRect.Bottom + (GridPanelOriginYGoods - GridPanelOriginY));

    /// <summary>Same outline as <see cref="_contentOutlineRect"/>, shifted down by the same offset as <see cref="GridPanelOriginYModules"/> is from <see cref="GridPanelOriginY"/>, to frame the Modules grid below the Goods grid.</summary>
    private static readonly SKRect _contentOutlineRectModules = new(
        _contentOutlineRect.Left, _contentOutlineRect.Top + (GridPanelOriginYModules - GridPanelOriginY),
        _contentOutlineRect.Right, _contentOutlineRect.Bottom + (GridPanelOriginYModules - GridPanelOriginY));

    private static readonly SKPaint _contentOutlinePaint = new()
    {
        Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true
    };

    /// <summary>
    /// Ship-compartment illustration shown to the left of the three stacked grids, in the
    /// 200px strip reserved by widening <see cref="TradeLayout.PanelWidth"/> — loaded once via
    /// the shared, cached <see cref="UiAssetLoader"/> (returns null, cached, if the file is
    /// missing/corrupt, in which case <see cref="DrawCompartmentImage"/> simply no-ops and
    /// leaves the strip blank rather than throwing or drawing a placeholder).
    /// </summary>
    private static readonly SKBitmap? _compartmentImage =
        UiAssetLoader.LoadBitmap("Images/UI/TradeScreen/trade-androids.png");

    /// <summary>Test seam — true if the compartment illustration PNG was found and decoded at startup.</summary>
    internal static bool HasLoadedCompartmentImage => _compartmentImage is not null;

    /// <summary>
    /// Local rect (panel-relative) the compartment illustration is drawn into — spans the
    /// same vertical extent as the three stacked grids' combined white-outline block
    /// (<see cref="_contentOutlineRect"/>'s top down to <see cref="_contentOutlineRectModules"/>'s
    /// bottom) and the full width reserved for it, from the panel's own left margin
    /// (<see cref="PanelContentMargin"/>) to where the grid column starts (<see cref="GridPanelOriginX"/>).
    /// </summary>
    private static readonly SKRect _compartmentImageRect = new(
        PanelContentMargin, _contentOutlineRect.Top, GridPanelOriginX, _contentOutlineRectModules.Bottom);

    /// <summary>
    /// Draws <see cref="_compartmentImage"/> filling <see cref="_compartmentImageRect"/> with a
    /// "cover" crop — scaled up to whichever of width/height needs the larger factor to fill
    /// the rect, then center-cropped on the other axis — so the illustration fills the
    /// reserved strip edge-to-edge with no letterboxing and no stretch distortion, regardless
    /// of the source PNG's own aspect ratio. No-ops if the asset failed to load.
    /// </summary>
    private static void DrawCompartmentImage(SKCanvas canvas, float pl, float pt)
    {
        if (_compartmentImage is not { } image)
            return;

        var destRect = new SKRect(
            pl + _compartmentImageRect.Left, pt + _compartmentImageRect.Top,
            pl + _compartmentImageRect.Right, pt + _compartmentImageRect.Bottom);

        float scale = Math.Max(destRect.Width / image.Width, destRect.Height / image.Height);
        float srcCropWidth = destRect.Width / scale;
        float srcCropHeight = destRect.Height / scale;
        float srcLeft = (image.Width - srcCropWidth) / 2f;
        float srcTop = (image.Height - srcCropHeight) / 2f;
        var srcRect = new SKRect(srcLeft, srcTop, srcLeft + srcCropWidth, srcTop + srcCropHeight);

        canvas.DrawBitmap(image, srcRect, destRect);
    }

    /// <summary>
    /// Anchor (the header bar's top-left) for the <see cref="GridPanel"/> resources list — see
    /// that control's doc comment for how header/rows/scrollbar are laid out relative to this
    /// point. Shifted right from the original 15 by the 200px <see cref="TradeLayout.PanelWidth"/>
    /// was widened by, reserving a 15..215 strip at the panel's left edge for the
    /// ship-compartment illustration (<see cref="_compartmentImage"/>) — everything else in
    /// the grid/right-panel column keeps its original size, just moved over by that same 200px.
    /// </summary>
    private const float GridPanelOriginX = 215f;
    private const float GridPanelOriginY = 76f;
    private const string ResourcesGridTitle = "Resources";

    /// <summary>Grid height: header-to-last-row-bottom, 44 + <see cref="GridPanel.MaxVisibleRows"/>×30.</summary>
    private const float GridRowsAreaHeight = 194f;

    /// <summary><see cref="GridRowsAreaHeight"/> plus the 30px gap left between two stacked grid panels.</summary>
    private const float GridPanelPitch = GridRowsAreaHeight + 30f;

    /// <summary>Extra manual downward nudge for the Goods grid, on top of its base position directly below the Resources grid.</summary>
    private const float GoodsGridExtraOffsetY = 25f;

    /// <summary>
    /// Anchor for the Goods grid, same <see cref="GridPanelOriginX"/> column directly below
    /// the Resources grid: <see cref="GridPanelOriginY"/> (76) + <see cref="GridPanelPitch"/>
    /// (224) + <see cref="GoodsGridExtraOffsetY"/> (25).
    /// </summary>
    private const float GridPanelOriginYGoods = GridPanelOriginY + GridPanelPitch + GoodsGridExtraOffsetY;
    private const string GoodsGridTitle = "Goods";

    /// <summary>Extra manual downward nudge for the Modules grid, on top of its base position directly below the Goods grid — independent of <see cref="GoodsGridExtraOffsetY"/>.</summary>
    private const float ModulesGridExtraOffsetY = 50f;

    /// <summary>
    /// Anchor for the Modules grid, same <see cref="GridPanelOriginX"/> column directly below
    /// the Goods grid's base position (<see cref="GridPanelOriginY"/> + 2×<see cref="GridPanelPitch"/>),
    /// plus its own <see cref="ModulesGridExtraOffsetY"/> (50) — deliberately not stacked on
    /// top of <see cref="GoodsGridExtraOffsetY"/>, since the two nudges were requested as
    /// independent adjustments to each panel's original position.
    /// </summary>
    private const float GridPanelOriginYModules = GridPanelOriginY + 2 * GridPanelPitch + ModulesGridExtraOffsetY;
    private const string ModulesGridTitle = "Modules";

    /// <summary>Right edge (panel-local x) of the three grids — their left margin plus header+scrollbar width.</summary>
    private const float GridRightEdge = GridPanelOriginX + GridPanel.HeaderWidth + GridPanel.ScrollbarWidth;

    /// <summary>
    /// Generic gap kept from the Trade panel's own edges — what <see cref="GridPanelOriginX"/>
    /// used to double as before the grid column was shifted right to free up room for a
    /// ship-compartment illustration. Kept as its own constant so the right-hand panels'
    /// positioning below stays anchored to the panel's actual 15px edge margin rather than
    /// following the grid's own (now larger) left offset.
    /// </summary>
    private const float PanelContentMargin = 15f;

    /// <summary>Extra rightward nudge applied to both right-hand panels' left edge — their width shrinks by the same amount, their right edge staying put.</summary>
    private const float RightPanelExtraLeftInset = 25f;

    /// <summary>Extra width added to both right-hand panels — their left edge moves this much further left, their right edge staying put.</summary>
    private const float RightPanelExtraWidth = 5f;

    /// <summary>Left edge of the two right-hand info panels (no grid) — as far from the grids' right edge as the Trade panel's own left edge margin (<see cref="PanelContentMargin"/>), plus <see cref="RightPanelExtraLeftInset"/>, minus <see cref="RightPanelExtraWidth"/>.</summary>
    private const float RightPanelLeft = GridRightEdge + PanelContentMargin + RightPanelExtraLeftInset - RightPanelExtraWidth;

    /// <summary>Right edge of the two right-hand info panels — mirrors <see cref="PanelContentMargin"/> as the gap kept from the Trade panel's own right edge.</summary>
    private const float RightPanelRight = TradeLayout.PanelWidth - PanelContentMargin;

    /// <summary>Gap kept between the titlebar's left edge and its panel's own white outline left edge — same gap <see cref="GridPanelOriginX"/> (15) keeps from the Resources white outline's left edge (<see cref="_contentOutlineRect"/>, 10): 15-10=5.</summary>
    private static readonly float RightPanelTitleBarLeftInset = GridPanelOriginX - _contentOutlineRect.Left;

    /// <summary>Gap kept between the titlebar's right edge and its panel's own white outline right edge — mirrors <see cref="RightPanelTitleBarLeftInset"/> (these panels have no scrollbar to justify a wider right margin the way the Resources grid header's does).</summary>
    private static readonly float RightPanelTitleBarRightInset = RightPanelTitleBarLeftInset;

    /// <summary>Height of the gray rounded-corner titlebar drawn at the top of each right-hand panel — same style as <see cref="GridPanel"/>'s own header bar.</summary>
    private const float RightPanelTitleBarHeight = GridPanel.HeaderHeight;

    /// <summary>Corner radius for the right-hand panels' titlebar — mirrors GridPanel's own (private) header corner radius.</summary>
    private const float RightPanelTitleBarCornerRadius = 12f;

    /// <summary>Same fill color as <see cref="GridPanel"/>'s own header bar, for the right-hand panels' titlebar.</summary>
    private static readonly SKPaint _rightPanelTitleBarPaint = new()
    {
        Color = new SKColor(0x5E, 0x5E, 0x5E), Style = SKPaintStyle.Fill, IsAntialias = true
    };

    /// <summary>
    /// Upper right-hand panel (no grid) — same top and bottom as the Resources grid's own
    /// white outline (<see cref="_contentOutlineRect"/>), so the two frames line up on screen
    /// instead of the panel's height being an independent fraction of the right column.
    /// </summary>
    private static readonly SKRect _rightPanelUpper = new(
        RightPanelLeft, _contentOutlineRect.Top, RightPanelRight, _contentOutlineRect.Bottom);

    /// <summary>
    /// Lower right-hand panel (no grid) — top edge matches the Goods grid's own white outline
    /// (<see cref="_contentOutlineRectGoods"/>), the same way <see cref="_rightPanelUpper"/>'s
    /// matches the Resources grid, and bottom edge matches the Modules grid's own white outline
    /// (<see cref="_contentOutlineRectModules"/>); also hosts the Trade action panel (<see cref="TradeActionContentTop"/> onward).
    /// </summary>
    private static readonly SKRect _rightPanelLower = new(
        RightPanelLeft, _contentOutlineRectGoods.Top, RightPanelRight, _contentOutlineRectModules.Bottom);

    /// <summary>How far each right-hand panel's titlebar sits above its own white outline's top — same overlap the Resources grid's own header bar keeps above its white outline on the left (<see cref="GridPanelOriginY"/> vs. <see cref="_contentOutlineRect"/>'s top: 90-76=14).</summary>
    private static readonly float RightPanelTitleBarOverlap = _contentOutlineRect.Top - GridPanelOriginY;

    /// <summary>
    /// Upper right-hand panel's titlebar — sits <see cref="RightPanelTitleBarOverlap"/> above
    /// its own panel's top, left/right edges inset from the panel's own white outline by
    /// <see cref="RightPanelTitleBarLeftInset"/>/<see cref="RightPanelTitleBarRightInset"/> —
    /// the same horizontal margins the Resources grid's header keeps from its white outline.
    /// </summary>
    private static readonly SKRect _rightPanelUpperTitleBar = new(
        RightPanelLeft + RightPanelTitleBarLeftInset, _rightPanelUpper.Top - RightPanelTitleBarOverlap,
        RightPanelRight - RightPanelTitleBarRightInset, _rightPanelUpper.Top - RightPanelTitleBarOverlap + RightPanelTitleBarHeight);

    /// <summary>Same idea as <see cref="_rightPanelUpperTitleBar"/>, for the lower panel's own top.</summary>
    private static readonly SKRect _rightPanelLowerTitleBar = new(
        RightPanelLeft + RightPanelTitleBarLeftInset, _rightPanelLower.Top - RightPanelTitleBarOverlap,
        RightPanelRight - RightPanelTitleBarRightInset, _rightPanelLower.Top - RightPanelTitleBarOverlap + RightPanelTitleBarHeight);

    /// <summary>Test seam — the two right-hand info panels' geometry, in the same panel-local coordinate space as <see cref="_contentOutlineRect"/>.</summary>
    internal (SKRect Upper, SKRect Lower) RightPanels => (_rightPanelUpper, _rightPanelLower);

    /// <summary>Test seam — the two right-hand panels' titlebar geometry, same coordinate space as <see cref="RightPanels"/>.</summary>
    internal (SKRect Upper, SKRect Lower) RightPanelTitleBars => (_rightPanelUpperTitleBar, _rightPanelLowerTitleBar);

    // ── Trade action panel (lower right-hand frame) — Docs/FirstRelease/Screens/Trade.md
    // "UI-решение: панель действия". Shows Buy/Sell/Refuel controls for whichever item is
    // currently selected across the three grids above (identity-based selection, already
    // mutually exclusive — see _selectedResourceItemTypeId/_selectedGoodItemTypeId/
    // _selectedModuleItemTypeId).

    private const string FuelItemTypeId = "item.fuel";

    /// <summary>True while the action panel is in Buy mode, false for Sell — always true (and locked) while <see cref="FuelItemTypeId"/> is selected, since refueling has no Sell counterpart.</summary>
    private bool _isTradeBuyMode = true;

    /// <summary>Test seam — current Buy/Sell toggle state (see <see cref="_isTradeBuyMode"/>).</summary>
    internal bool IsTradeBuyMode => _isTradeBuyMode;

    /// <summary>
    /// Current quantity chosen by the stepper (`-`/`+`/`Max`) or the quantity slider — reset
    /// to 1 (trading is fully per-unit — Docs/FirstRelease/Screens/Trade.md, "UI-решение:
    /// панель действия") whenever the selection or the Buy/Sell mode changes (see
    /// <see cref="ResetTradeActionPanelState"/>).
    /// </summary>
    private long _tradeQuantity;

    /// <summary>Test seam — current stepper quantity (see <see cref="_tradeQuantity"/>).</summary>
    internal long TradeQuantity => _tradeQuantity;

    /// <summary>True while the quantity slider's track is being dragged (mouse-down on it, not yet released) — same drag-state shape as <see cref="_isDraggingScrollThumb"/>, updated in <see cref="OnMouseMove(float, float)"/>, cleared in <see cref="OnMouseUp"/>.</summary>
    private bool _isDraggingTradeSlider;

    /// <summary>True while the pointer is over the confirm button AND it's currently clickable (a disabled button gets no hover feedback — same "not a button unless it does something" rule the toolbar's plain readouts already follow) — drives both the cursor swap (<see cref="OnMouseMove(float, float)"/>'s return) and the button's <see cref="ButtonState.Hovered"/> visual.</summary>
    private bool _isTradeConfirmHovered;

    /// <summary>True while the pointer is over the trade-result panel's "Return to item" button (see <see cref="HandleTradeResultReturnButtonMouseDown"/>) — same role as <see cref="_isTradeConfirmHovered"/>, for the button shown in the same bottom-anchored slot once nothing is selected.</summary>
    private bool _isReturnToItemHovered;

    /// <summary>True while the pointer is over the quantity stepper's `-` button — same hover-feedback role as <see cref="_isTradeConfirmHovered"/>, only meaningful while an item is selected (the stepper isn't drawn otherwise).</summary>
    private bool _isTradeMinusHovered;

    /// <summary>`+` counterpart to <see cref="_isTradeMinusHovered"/>.</summary>
    private bool _isTradePlusHovered;

    /// <summary>`Max` counterpart to <see cref="_isTradeMinusHovered"/>.</summary>
    private bool _isTradeMaxHovered;

    /// <summary>
    /// CommandId of the last trade command sent via <see cref="OnConfirmTradeClicked"/>, kept
    /// until its disposition is observed in a later snapshot's CommandResults (see
    /// <see cref="UpdateTradeCommandResult"/>) — a rejection produces no other observable state
    /// change (GameSessionHandle.SendTradeCommand's doc comment), so this is the only way the
    /// panel learns a Buy/Sell/Refuel was rejected.
    /// </summary>
    private string? _lastSentTradeCommandId;

    /// <summary>CommandType of the command <see cref="_lastSentTradeCommandId"/> refers to — captured at send time (not re-derived from current UI state) so <see cref="UpdateTradeCommandResult"/> reports the right outcome message even if the player has since changed the Buy/Sell toggle or selection while the result was still in flight.</summary>
    private string? _lastSentTradeCommandType;

    /// <summary>Quantity the in-flight command in <see cref="_lastSentTradeCommandId"/> was sent with — same "captured at send time" rationale as <see cref="_lastSentTradeCommandType"/>, used to detect/describe a partial Sell fill.</summary>
    private long _lastSentTradeQuantity;

    /// <summary>Station unit price at the moment the in-flight command was sent — same "captured at send time" rationale as <see cref="_lastSentTradeCommandType"/>/<see cref="_lastSentTradeQuantity"/>, used to show what the trade actually cost/paid in the result message.</summary>
    private long _lastSentTradeUnitPriceCredits;

    /// <summary>Item type id of the in-flight command in <see cref="_lastSentTradeCommandId"/> — captured at send time since the grid selection is cleared right away (<see cref="OnConfirmTradeClicked"/>), so <see cref="UpdateTradeCommandResult"/> has it available to resolve the post-trade Container module.</summary>
    private string? _lastSentTradeItemTypeId;

    /// <summary>Display name of the item being traded — captured at send time for the same reason as <see cref="_lastSentTradeItemTypeId"/>, shown as the first line of the trade-result detail block.</summary>
    private string? _lastSentTradeItemDisplayName;

    /// <summary>True when the in-flight command is a Refuel — determines whether the trade-result before/after line reads from the player's Fuel tank or the Container module's free cargo capacity.</summary>
    private bool _lastSentTradeWasFuel;

    /// <summary>Container module's available cargo capacity in kg ("free cargo"), or the player's Fuel amount in kg for a Refuel, immediately before the command was sent — paired with the post-trade amount resolved in <see cref="UpdateTradeCommandResult"/> to show the before/after change.</summary>
    private long _lastSentAmountBeforeTrade;

    /// <summary>Player's Credits balance immediately before the command was sent — same before/after pairing as <see cref="_lastSentAmountBeforeTrade"/>, for Credits instead of cargo/Fuel.</summary>
    private long _lastSentCreditsBeforeTrade;

    /// <summary>
    /// Localization key for the last observed trade-command rejection reason, or null — shown
    /// near the confirm button until a new command supersedes it or the selection/mode changes
    /// (<see cref="ResetTradeActionPanelState"/>).
    /// </summary>
    private string? _tradeRejectionReasonKey;

    /// <summary>Test seam — current trade-command rejection reason key (see <see cref="_tradeRejectionReasonKey"/>), null once resolved/cleared.</summary>
    internal string? TradeRejectionReasonKey => _tradeRejectionReasonKey;

    /// <summary>
    /// Fully-resolved (already localized/formatted) message for the last trade command that
    /// executed successfully, or null — shown in the same spot as <see cref="_tradeRejectionReasonKey"/>
    /// (directly above the confirm button) when there's no disabled/rejection reason to show
    /// instead, so a successful Buy/Sell/Refuel is visibly acknowledged rather than only
    /// showing up a second later as updated numbers in the grid (see <see cref="UpdateTradeCommandResult"/>).
    /// </summary>
    private string? _tradeResultMessage;

    /// <summary>Test seam — current trade-result message (see <see cref="_tradeResultMessage"/>), null once cleared.</summary>
    internal string? TradeResultMessage => _tradeResultMessage;

    /// <summary>Display name of the item the last successfully-executed trade was for — set alongside <see cref="_tradeResultMessage"/> in <see cref="UpdateTradeCommandResult"/>, null once cleared.</summary>
    private string? _tradeResultItemDisplayName;

    /// <summary>Test seam — see <see cref="_tradeResultItemDisplayName"/>.</summary>
    internal string? TradeResultItemDisplayName => _tradeResultItemDisplayName;

    /// <summary>Item type id of the last successfully-executed trade — set alongside <see cref="_tradeResultItemDisplayName"/>, used by the "Return to item" button (<see cref="HandleTradeResultReturnButtonMouseDown"/>) to re-select it once the grid selection has been cleared.</summary>
    private string? _tradeResultItemTypeId;

    /// <summary>Test seam — see <see cref="_tradeResultItemTypeId"/>.</summary>
    internal string? TradeResultItemTypeId => _tradeResultItemTypeId;

    /// <summary>True when the last successfully-executed trade was a Refuel — see <see cref="_lastSentTradeWasFuel"/>.</summary>
    private bool _tradeResultWasFuel;

    /// <summary>Player's cargo quantity (or Fuel amount in kg for a Refuel) of the traded item just before the last successfully-executed trade — set alongside <see cref="_tradeResultMessage"/>.</summary>
    private long? _tradeResultAmountBefore;

    /// <summary>Test seam — see <see cref="_tradeResultAmountBefore"/>.</summary>
    internal long? TradeResultAmountBefore => _tradeResultAmountBefore;

    /// <summary>Same amount as <see cref="_tradeResultAmountBefore"/>, resolved from the snapshot that reported the trade as Executed — the after side of the before/after pair.</summary>
    private long? _tradeResultAmountAfter;

    /// <summary>Test seam — see <see cref="_tradeResultAmountAfter"/>.</summary>
    internal long? TradeResultAmountAfter => _tradeResultAmountAfter;

    /// <summary>Player's Credits balance just before the last successfully-executed trade — Credits counterpart to <see cref="_tradeResultAmountBefore"/>.</summary>
    private long? _tradeResultCreditsBefore;

    /// <summary>Test seam — see <see cref="_tradeResultCreditsBefore"/>.</summary>
    internal long? TradeResultCreditsBefore => _tradeResultCreditsBefore;

    /// <summary>Player's Credits balance from the snapshot that reported the trade as Executed — Credits counterpart to <see cref="_tradeResultAmountAfter"/>.</summary>
    private long? _tradeResultCreditsAfter;

    /// <summary>Test seam — see <see cref="_tradeResultCreditsAfter"/>.</summary>
    internal long? TradeResultCreditsAfter => _tradeResultCreditsAfter;

    /// <summary>Test seam — the item type id currently selected across the three grids (Resources/Goods/Modules), or null.</summary>
    internal string? SelectedTradeItemTypeId =>
        _selectedResourceItemTypeId ?? _selectedGoodItemTypeId ?? _selectedModuleItemTypeId;

    /// <summary>
    /// Called whenever the selected trade item or the Buy/Sell mode changes: forces Buy mode
    /// back on for Fuel (Sell is unreachable for it), resets the quantity to 1 (trading is
    /// fully per-unit), and clears any stale command-rejection state from a previous item/mode.
    /// </summary>
    private void ResetTradeActionPanelState(AuthoritativeSnapshot? snapshot)
    {
        string? itemTypeId = SelectedTradeItemTypeId;
        if (itemTypeId == FuelItemTypeId)
            _isTradeBuyMode = true;

        _tradeQuantity = itemTypeId is null ? 0 : 1;
        _lastSentTradeCommandId = null;
        _tradeRejectionReasonKey = null;
        _tradeResultMessage = null;
        _tradeResultItemDisplayName = null;
        _tradeResultItemTypeId = null;
        _tradeResultAmountBefore = null;
        _tradeResultAmountAfter = null;
        _tradeResultCreditsBefore = null;
        _tradeResultCreditsAfter = null;
    }

    /// <summary>
    /// The first installed module (by <see cref="InstalledModuleSnapshot.Position"/>) whose
    /// CommandTypeIds handles <see cref="TradeCommandTypes.Buy"/> — the cargo container
    /// Buy/Sell of non-Fuel items is addressed to (see <see cref="ResolvePlayerCargo"/>, which
    /// reuses this same resolution for the player-cargo dictionary it returns).
    /// </summary>
    private static InstalledModuleSnapshot? ResolveContainerModule(AuthoritativeSnapshot? snapshot)
    {
        var modules = snapshot?.InstalledModules;
        if (modules is null || modules.Value.IsDefaultOrEmpty)
            return null;

        return modules.Value
            .Where(m => m.CommandTypeIds.Contains(TradeCommandTypes.Buy))
            .OrderBy(m => m.Position)
            .FirstOrDefault();
    }

    /// <summary>
    /// The first installed module (by Position) whose CommandTypeIds handles
    /// <see cref="TradeCommandTypes.Refuel"/> — buying Fuel is always routed here, never to the
    /// container module (Docs/FirstRelease/Screens/Trade.md's command-routing rule).
    /// </summary>
    private static InstalledModuleSnapshot? ResolveEngineModule(AuthoritativeSnapshot? snapshot)
    {
        var modules = snapshot?.InstalledModules;
        if (modules is null || modules.Value.IsDefaultOrEmpty)
            return null;

        return modules.Value
            .Where(m => m.CommandTypeIds.Contains(TradeCommandTypes.Refuel))
            .OrderBy(m => m.Position)
            .FirstOrDefault();
    }

    /// <summary>
    /// Resolved market/eligibility data for the action panel's currently selected item — null
    /// while nothing is selected or the selected item can't be found in the docked station's
    /// current inventory (e.g. it dropped out between snapshots).
    /// </summary>
    private readonly record struct TradeActionInfo(
        string ItemTypeId, string DisplayName, string Category, bool IsFuel,
        long StationPriceCredits, long StationStockQuantity, long MaxSellableQuantity,
        long PlayerCargoQuantity, long PlayerCredits,
        string? ContainerModuleId, long? ContainerAvailableCapacityKg,
        string? EngineModuleId, long FuelAmountKg, long FuelCapacityKg);

    private static TradeActionInfo? ResolveTradeActionInfo(AuthoritativeSnapshot? snapshot, string? itemTypeId)
    {
        if (itemTypeId is null)
            return null;

        var item = snapshot?.DockedStationTrade?.Items.FirstOrDefault(i => i.ItemTypeId == itemTypeId);
        if (item is null)
            return null;

        var containerModule = ResolveContainerModule(snapshot);
        var engineModule = ResolveEngineModule(snapshot);
        var playerCargo = ResolvePlayerCargo(snapshot);
        long cargoQuantity = playerCargo.TryGetValue(itemTypeId, out long qty) ? qty : 0;

        return new TradeActionInfo(
            ItemTypeId: itemTypeId,
            DisplayName: ItemDisplayName(itemTypeId),
            Category: item.Category,
            IsFuel: itemTypeId == FuelItemTypeId,
            StationPriceCredits: item.UnitPriceCredits,
            StationStockQuantity: item.StockQuantity,
            MaxSellableQuantity: item.MaxSellableQuantity,
            PlayerCargoQuantity: cargoQuantity,
            PlayerCredits: snapshot?.PlayerCredits ?? 0,
            ContainerModuleId: containerModule?.ModuleId,
            ContainerAvailableCapacityKg: containerModule?.AvailableCapacityKg,
            EngineModuleId: engineModule?.ModuleId,
            FuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            FuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot));
    }

    /// <summary>
    /// Max quantity the stepper's `Max` button (and the quantity slider's upper bound) resolves
    /// to (Docs/FirstRelease/Screens/Trade.md batch spec): Buy non-Fuel is bounded by
    /// affordability/station stock, and additionally resolves to 0 (button becomes a no-op)
    /// when the container has no cargo space left at all; Buy Fuel (Refuel) is additionally
    /// bounded by remaining tank capacity; Sell is bounded by cargo-on-hand/station's
    /// MaxSellableQuantity — fully per-unit, no package rounding.
    /// </summary>
    private static long ResolveMaxQuantity(TradeActionInfo info, bool isBuyMode)
    {
        if (isBuyMode)
        {
            long affordable = info.StationPriceCredits > 0 ? info.PlayerCredits / info.StationPriceCredits : 0;

            if (info.IsFuel)
            {
                long remainingFuelCapacity = Math.Max(0, info.FuelCapacityKg - info.FuelAmountKg);
                return Math.Max(0, Math.Min(affordable, Math.Min(info.StationStockQuantity, remainingFuelCapacity)));
            }

            if (info.ContainerAvailableCapacityKg is <= 0)
                return 0;

            return Math.Max(0, Math.Min(affordable, info.StationStockQuantity));
        }

        return Math.Max(0, Math.Min(info.PlayerCargoQuantity, info.MaxSellableQuantity));
    }

    /// <summary>
    /// Maps a screen-space x position within the quantity slider's track to a quantity in
    /// `[minQuantity, maxQuantity]`, linearly interpolated and rounded to the nearest integer —
    /// used both for the initial click-to-set jump and every subsequent drag move (Docs/
    /// FirstRelease/Screens/Trade.md, "UI-решение: панель действия", step 5).
    /// `minQuantity` is 1 when something is tradeable (<paramref name="maxQuantity"/> &gt; 0),
    /// 0 otherwise — matching the stepper's own floor.
    /// </summary>
    private static long ResolveSliderQuantity(float x, SKRect trackRectScreen, long maxQuantity)
    {
        if (maxQuantity <= 0)
            return 0;

        long minQuantity = 1;
        if (trackRectScreen.Width <= 0)
            return minQuantity;

        float fraction = Math.Clamp((x - trackRectScreen.Left) / trackRectScreen.Width, 0f, 1f);
        long value = minQuantity + (long)Math.Round(fraction * (maxQuantity - minQuantity), MidpointRounding.AwayFromZero);
        return Math.Clamp(value, minQuantity, maxQuantity);
    }

    /// <summary>
    /// Proactive confirm-button eligibility (Docs/FirstRelease/Screens/Trade.md's two documented
    /// disabled states — the panel does not try to predict every possible Engine rejection, e.g.
    /// cargo mass overflow on Buy, which is a documented client-side limitation surfaced
    /// reactively instead via <see cref="UpdateTradeCommandResult"/>).
    /// </summary>
    private static string? ResolveConfirmDisabledReasonKey(TradeActionInfo info, bool isBuyMode, long quantity)
    {
        if (isBuyMode)
        {
            if (!info.IsFuel && info.ContainerAvailableCapacityKg is <= 0)
                return "Trade.NoCargoSpace";

            long totalPrice = info.StationPriceCredits * quantity;
            if (totalPrice > info.PlayerCredits)
                return "Trade.ReasonInsufficientPlayerCredits";

            return null;
        }

        if (info.PlayerCargoQuantity <= 0)
            return "Trade.NoneInCargo";

        return null;
    }

    /// <summary>Maps an Engine <see cref="CommandReasonCodes"/> value to its localization key (Docs/FirstRelease/Screens/Trade.md's CommandResult-correlation rule) — falls back to the raw code for any reason not expected on a trade command.</summary>
    private static string ResolveRejectionReasonKey(string reasonCode) => reasonCode switch
    {
        CommandReasonCodes.InsufficientPlayerCredits => "Trade.ReasonInsufficientPlayerCredits",
        CommandReasonCodes.InsufficientStationStock => "Trade.ReasonInsufficientStationStock",
        CommandReasonCodes.CargoCapacityExceeded => "Trade.ReasonCargoCapacityExceeded",
        CommandReasonCodes.FuelCapacityExceeded => "Trade.ReasonFuelCapacityExceeded",
        CommandReasonCodes.InsufficientCargoQuantity => "Trade.ReasonInsufficientCargoQuantity",
        CommandReasonCodes.InvalidQuantity => "Trade.ReasonInvalidQuantity",
        CommandReasonCodes.NotDocked => "Trade.ReasonNotDocked",
        CommandReasonCodes.UnknownItemType => "Trade.ReasonUnknownItemType",
        _ => reasonCode
    };

    /// <summary>
    /// Success-case counterpart to <see cref="ResolveRejectionReasonKey"/> — the transaction
    /// confirmation shown above the confirm button when a command actually executed (Docs/
    /// FirstRelease/Screens/Trade.md's UI-решение: панель действия), reusing the leftover
    /// Trade.Status* keys from the pre-redesign MVP, extended with the unit price paid/received
    /// and the resulting total. Sell reports the partial-fill wording — and its total is priced
    /// off the actually-executed quantity, not the originally requested one — when the
    /// station's hidden Credits balance capped how much it actually bought.
    /// </summary>
    private static string ResolveTradeResultMessage(
        string commandType, long? executedQuantity, long requestedQuantity, long unitPriceCredits)
    {
        if (commandType == TradeCommandTypes.Sell && executedQuantity is { } executed && executed < requestedQuantity)
        {
            return string.Format(
                Localization.Get("Trade.StatusSellPartial"), executed, requestedQuantity, unitPriceCredits, executed * unitPriceCredits);
        }

        long quantity = commandType == TradeCommandTypes.Sell && executedQuantity is { } executedInFull
            ? executedInFull
            : requestedQuantity;
        long total = quantity * unitPriceCredits;

        string key = commandType switch
        {
            TradeCommandTypes.Refuel => "Trade.StatusRefuelSuccess",
            TradeCommandTypes.Sell => "Trade.StatusSellSuccess",
            _ => "Trade.StatusBuySuccess"
        };
        return string.Format(Localization.Get(key), quantity, unitPriceCredits, total);
    }

    /// <summary>
    /// Correlates <see cref="_lastSentTradeCommandId"/> against the latest snapshot's
    /// CommandResults (see GameSessionHandle.SendTradeCommand's doc comment) — sets
    /// <see cref="_tradeRejectionReasonKey"/> on a Rejected disposition, <see cref="_tradeResultMessage"/>
    /// on an Executed one, or clears the pending id on any other disposition (the command
    /// resolved with an observable state change, so there's nothing more to show). Called once
    /// per Render.
    /// </summary>
    private void UpdateTradeCommandResult(AuthoritativeSnapshot? snapshot)
    {
        if (_lastSentTradeCommandId is null || snapshot is null || snapshot.CommandResults.IsDefaultOrEmpty)
            return;

        foreach (var result in snapshot.CommandResults)
        {
            if (result.CommandId != _lastSentTradeCommandId)
                continue;

            // Deferred (module busy) is not a final disposition — trade commands are not
            // expected to defer, but if one does, keep tracking it across future snapshots
            // rather than treating "no result yet" as resolved.
            if (result.Status == CommandResultStatus.Deferred)
                return;

            if (result.Status == CommandResultStatus.Rejected)
                _tradeRejectionReasonKey = ResolveRejectionReasonKey(result.ReasonCode ?? string.Empty);
            else if (result.Status == CommandResultStatus.Executed && _lastSentTradeCommandType is not null)
            {
                _tradeResultMessage = ResolveTradeResultMessage(
                    _lastSentTradeCommandType, result.ExecutedQuantity, _lastSentTradeQuantity, _lastSentTradeUnitPriceCredits);

                _tradeResultItemDisplayName = _lastSentTradeItemDisplayName;
                _tradeResultItemTypeId = _lastSentTradeItemTypeId;
                _tradeResultWasFuel = _lastSentTradeWasFuel;
                _tradeResultAmountBefore = _lastSentAmountBeforeTrade;
                _tradeResultAmountAfter = _lastSentTradeWasFuel
                    ? StationToolbar.ResolveFuelAmountKg(snapshot)
                    : ResolveContainerModule(snapshot)?.AvailableCapacityKg ?? 0;
                _tradeResultCreditsBefore = _lastSentCreditsBeforeTrade;
                _tradeResultCreditsAfter = snapshot.PlayerCredits;
            }

            _lastSentTradeCommandId = null;
            return;
        }
    }

    // ── Trade action panel layout (panel-local coordinates, same space as _rightPanelLower —
    // screen coordinates are obtained by adding TradeLayout.PanelLeft/PanelTop, exactly like
    // every other rect in this file).

    // RightPanelTitleBarLeftInset/RightInset are `static readonly` (computed from _contentOutlineRect),
    // not compile-time constants, so these derived positions must be `static readonly` too.
    private static readonly float TradeActionContentLeft = RightPanelLeft + RightPanelTitleBarLeftInset;
    private static readonly float TradeActionContentRight = RightPanelRight - RightPanelTitleBarRightInset;
    private static readonly float TradeActionContentTop = _rightPanelLowerTitleBar.Bottom + 8f;

    private const float TradeMarketLineHeight = 14f;
    private const float TradeToggleHeight = 22f;
    private const float TradeStepperHeight = 22f;
    private const float TradeSummaryLineHeight = 14f;
    private const float TradeReasonLineHeight = 14f;
    private const float TradeConfirmHeight = 28f;

    private static readonly SKRect _tradeMarketLine1Rect = new(
        TradeActionContentLeft, TradeActionContentTop, TradeActionContentRight, TradeActionContentTop + TradeMarketLineHeight);
    private static readonly SKRect _tradeMarketLine2Rect = new(
        TradeActionContentLeft, _tradeMarketLine1Rect.Bottom + 4f, TradeActionContentRight, _tradeMarketLine1Rect.Bottom + 4f + TradeMarketLineHeight);

    /// <summary>Third/fourth line of the trade-result detail block drawn in the action panel's empty state after a confirm (see <see cref="DrawTradeResultDetails"/>) — same left-aligned rhythm as <see cref="_tradeMarketLine1Rect"/>/<see cref="_tradeMarketLine2Rect"/>, which hold that block's first two lines (item name, then the existing success/partial-fill message).</summary>
    private static readonly SKRect _tradeResultLine3Rect = new(
        TradeActionContentLeft, _tradeMarketLine2Rect.Bottom + 4f, TradeActionContentRight, _tradeMarketLine2Rect.Bottom + 4f + TradeMarketLineHeight);
    private static readonly SKRect _tradeResultLine4Rect = new(
        TradeActionContentLeft, _tradeResultLine3Rect.Bottom + 4f, TradeActionContentRight, _tradeResultLine3Rect.Bottom + 4f + TradeMarketLineHeight);

    private const float TradeToggleGap = 8f;
    private static readonly float _tradeToggleRowTop = _tradeMarketLine2Rect.Bottom + 6f;
    private static readonly float _tradeToggleButtonWidth = (TradeActionContentRight - TradeActionContentLeft - TradeToggleGap) / 2f;
    private static readonly SKRect _tradeBuyButtonRect = new(
        TradeActionContentLeft, _tradeToggleRowTop, TradeActionContentLeft + _tradeToggleButtonWidth, _tradeToggleRowTop + TradeToggleHeight);
    private static readonly SKRect _tradeSellButtonRect = new(
        _tradeBuyButtonRect.Right + TradeToggleGap, _tradeToggleRowTop, TradeActionContentRight, _tradeToggleRowTop + TradeToggleHeight);

    /// <summary>Test seam — the Buy/Sell toggle buttons' geometry, panel-local (see <see cref="RightPanels"/>'s coordinate space).</summary>
    internal (SKRect Buy, SKRect Sell) TradeModeToggleRects => (_tradeBuyButtonRect, _tradeSellButtonRect);

    private const float TradeStepperGap = 8f;
    private const float TradeStepperButtonWidth = 36f;
    private const float TradeMaxButtonWidth = 70f;
    private static readonly float _tradeStepperRowTop = _tradeBuyButtonRect.Bottom + 6f;
    private static readonly SKRect _tradeMinusButtonRect = new(
        TradeActionContentLeft, _tradeStepperRowTop, TradeActionContentLeft + TradeStepperButtonWidth, _tradeStepperRowTop + TradeStepperHeight);
    private static readonly SKRect _tradeMaxButtonRect = new(
        TradeActionContentRight - TradeMaxButtonWidth, _tradeStepperRowTop, TradeActionContentRight, _tradeStepperRowTop + TradeStepperHeight);
    private static readonly SKRect _tradePlusButtonRect = new(
        _tradeMaxButtonRect.Left - TradeStepperGap - TradeStepperButtonWidth, _tradeStepperRowTop,
        _tradeMaxButtonRect.Left - TradeStepperGap, _tradeStepperRowTop + TradeStepperHeight);
    private static readonly SKRect _tradeQuantityFieldRect = new(
        _tradeMinusButtonRect.Right + TradeStepperGap, _tradeStepperRowTop,
        _tradePlusButtonRect.Left - TradeStepperGap, _tradeStepperRowTop + TradeStepperHeight);

    /// <summary>Test seam — the quantity stepper's `-`/`+`/`Max` button geometry, panel-local.</summary>
    internal (SKRect Minus, SKRect Plus, SKRect Max) TradeStepperRects => (_tradeMinusButtonRect, _tradePlusButtonRect, _tradeMaxButtonRect);

    /// <summary>
    /// Quantity slider row (Docs/FirstRelease/Screens/Trade.md, "UI-решение: панель действия",
    /// step 5) — directly below the `-`/`+`/`Max` stepper row, spanning only the `-`/field/`+`
    /// group's width (<see cref="_tradeMinusButtonRect"/>.Left to <see cref="_tradePlusButtonRect"/>.Right),
    /// not the `Max` button — narrower than the full frame width so its track visually lines up
    /// with the quantity controls it drives, not the wider row above it. Dragging or clicking
    /// anywhere on the track sets <see cref="_tradeQuantity"/> to the position clicked, same
    /// underlying field as the stepper/Max — see <see cref="ResolveSliderQuantity"/>.
    /// </summary>
    private const float TradeSliderRowHeight = 16f;
    private static readonly SKRect _tradeSliderRowRect = new(
        _tradeMinusButtonRect.Left, _tradeMinusButtonRect.Bottom + 6f, _tradePlusButtonRect.Right, _tradeMinusButtonRect.Bottom + 6f + TradeSliderRowHeight);

    /// <summary>Test seam — the quantity slider's track geometry, panel-local.</summary>
    internal SKRect TradeSliderRect => _tradeSliderRowRect;

    private static readonly SKRect _tradeSummaryLine1Rect = new(
        TradeActionContentLeft, _tradeSliderRowRect.Bottom + 6f, TradeActionContentRight, _tradeSliderRowRect.Bottom + 6f + TradeSummaryLineHeight);
    private static readonly SKRect _tradeSummaryLine2Rect = new(
        TradeActionContentLeft, _tradeSummaryLine1Rect.Bottom, TradeActionContentRight, _tradeSummaryLine1Rect.Bottom + TradeSummaryLineHeight);

    /// <summary>
    /// Gap kept between the confirm button and its own panel's bottom edge — the lower
    /// right-hand panel (spanning the Goods+Modules grids' combined height, see
    /// <see cref="_rightPanelLower"/>) is far taller than the action panel's content needs, so
    /// the confirm button is deliberately pinned near the panel's bottom instead of cascading
    /// directly below the summary lines, so it reads as a distinct, harder-to-miss final step
    /// rather than blending into the block of text above it.
    /// </summary>
    private const float TradeConfirmBottomMargin = 16f;
    private static readonly SKRect _tradeConfirmButtonRect = new(
        TradeActionContentLeft, _rightPanelLower.Bottom - TradeConfirmBottomMargin - TradeConfirmHeight,
        TradeActionContentRight, _rightPanelLower.Bottom - TradeConfirmBottomMargin);

    /// <summary>Test seam — the confirm button's geometry, panel-local.</summary>
    internal SKRect TradeConfirmButtonRect => _tradeConfirmButtonRect;

    /// <summary>Reason/result message line — sits directly above the (now bottom-anchored) confirm button, whatever that ends up being (see <see cref="TradeConfirmBottomMargin"/>), not cascaded from the summary lines above.</summary>
    private static readonly SKRect _tradeReasonLineRect = new(
        TradeActionContentLeft, _tradeConfirmButtonRect.Top - 6f - TradeReasonLineHeight,
        TradeActionContentRight, _tradeConfirmButtonRect.Top - 6f);

    /// <summary>Screen-space rect for a panel-local rect, using the current frame's panel position — mirrors every other `pl + local.Left, pt + local.Top` pattern in this file.</summary>
    private SKRect ToScreenRect(SKRect local)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        return new SKRect(pl + local.Left, pt + local.Top, pl + local.Right, pt + local.Bottom);
    }

    private static bool Contains(SKRect rect, float x, float y) =>
        x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom;

    /// <summary>
    /// Handles a left click against the trade action panel's own controls (Buy/Sell toggle,
    /// `-`/`+`/`Max` stepper, confirm) — returns true (and has already acted) if the click hit
    /// one of them, so the caller (OnMouseDown) skips the grid hit-testing below it. Requires
    /// an item to be selected; the panel has no clickable controls in its empty "Select item" state.
    /// </summary>
    private bool HandleTradeActionPanelMouseDown(float x, float y)
    {
        var snapshot = _buffer?.Latest?.Snapshot;
        string? itemTypeId = SelectedTradeItemTypeId;
        if (itemTypeId is null)
            return false;

        var info = ResolveTradeActionInfo(snapshot, itemTypeId);
        if (info is null)
            return false;

        if (Contains(ToScreenRect(_tradeBuyButtonRect), x, y))
        {
            if (!_isTradeBuyMode)
            {
                _isTradeBuyMode = true;
                ResetTradeActionPanelState(snapshot);
            }
            return true;
        }

        if (Contains(ToScreenRect(_tradeSellButtonRect), x, y))
        {
            // Fuel has no Sell counterpart (no trade.refuel-reverse command exists) — the
            // Sell side of the toggle is unreachable while it's selected.
            if (_isTradeBuyMode && !info.Value.IsFuel)
            {
                _isTradeBuyMode = false;
                ResetTradeActionPanelState(snapshot);
            }
            return true;
        }

        // Trading is fully per-unit (Docs/FirstRelease/Screens/Trade.md, "UI-решение: панель
        // действия") — the `-`/`+` stepper always moves by 1, for every category.
        const long step = 1;
        long max = ResolveMaxQuantity(info.Value, _isTradeBuyMode);

        if (Contains(ToScreenRect(_tradeMinusButtonRect), x, y))
        {
            _tradeQuantity = Math.Clamp(_tradeQuantity - step, step, Math.Max(step, max));
            return true;
        }

        if (Contains(ToScreenRect(_tradePlusButtonRect), x, y))
        {
            _tradeQuantity = Math.Clamp(_tradeQuantity + step, step, Math.Max(step, max));
            return true;
        }

        if (Contains(ToScreenRect(_tradeMaxButtonRect), x, y))
        {
            // No-op when Max resolves to 0 (e.g. container completely full on a non-Fuel Buy) —
            // there is nothing meaningful to set the quantity to.
            if (max > 0)
                _tradeQuantity = max;
            return true;
        }

        if (Contains(ToScreenRect(_tradeSliderRowRect), x, y))
        {
            // Click-to-set: jump the quantity straight to the position clicked, then continue
            // tracking the drag in OnMouseMove (mirrors the grid scrollbar-thumb-drag pattern —
            // _isDraggingScrollThumb/OnMouseMove/OnMouseUp).
            _isDraggingTradeSlider = true;
            _tradeQuantity = ResolveSliderQuantity(x, ToScreenRect(_tradeSliderRowRect), max);
            return true;
        }

        if (Contains(ToScreenRect(_tradeConfirmButtonRect), x, y))
        {
            OnConfirmTradeClicked(snapshot, info.Value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles a left click on the trade-result panel's "Return to item" button — only relevant
    /// once a trade has executed and the grid selection was cleared (see
    /// <see cref="OnConfirmTradeClicked"/>/<see cref="DrawTradeResultDetails"/>). Re-selects the
    /// traded item in whichever grid (Resources/Goods) it belongs to, which puts both the grid
    /// row and the action panel back on that item exactly like a fresh row click would. No-ops
    /// (but still consumes the click) if the item has since dropped out of the docked station's
    /// inventory — there's nothing left to select it as.
    /// </summary>
    private bool HandleTradeResultReturnButtonMouseDown(float x, float y)
    {
        if (_tradeResultItemTypeId is null || SelectedTradeItemTypeId is not null)
            return false;

        if (!Contains(ToScreenRect(_tradeConfirmButtonRect), x, y))
            return false;

        var snapshot = _buffer?.Latest?.Snapshot;
        var item = snapshot?.DockedStationTrade?.Items.FirstOrDefault(i => i.ItemTypeId == _tradeResultItemTypeId);
        if (item is not null)
        {
            if (item.Category == TradeItemCategories.Resource)
                _selectedResourceItemTypeId = _tradeResultItemTypeId;
            else
                _selectedGoodItemTypeId = _tradeResultItemTypeId;

            ResetTradeActionPanelState(snapshot);
        }

        return true;
    }

    /// <summary>
    /// Sends the Buy/Sell/Refuel command for the current selection/mode/quantity (Docs/
    /// FirstRelease/Screens/Trade.md's command-routing rule: Fuel always goes to the engine
    /// module as trade.refuel, everything else goes to the container module as trade.buy/
    /// trade.sell). No-ops if the confirm button is currently disabled, or if there's no live
    /// session/target module to address (tests constructing this screen without a
    /// GameSessionHandle).
    /// </summary>
    private void OnConfirmTradeClicked(AuthoritativeSnapshot? snapshot, TradeActionInfo info)
    {
        if (ResolveConfirmDisabledReasonKey(info, _isTradeBuyMode, _tradeQuantity) is not null)
            return;

        string? playerShipObjectId = snapshot?.PlayerShipObjectId;
        if (_handle is null || string.IsNullOrWhiteSpace(playerShipObjectId))
            return;

        string commandType;
        string? moduleId;
        if (info.IsFuel)
        {
            commandType = TradeCommandTypes.Refuel;
            moduleId = info.EngineModuleId;
        }
        else if (_isTradeBuyMode)
        {
            commandType = TradeCommandTypes.Buy;
            moduleId = info.ContainerModuleId;
        }
        else
        {
            commandType = TradeCommandTypes.Sell;
            moduleId = info.ContainerModuleId;
        }

        if (moduleId is null)
            return;

        _lastSentTradeCommandId = _handle.SendTradeCommand(
            playerShipObjectId, moduleId, commandType, info.ItemTypeId, _tradeQuantity);
        _lastSentTradeCommandType = commandType;
        _lastSentTradeQuantity = _tradeQuantity;
        _lastSentTradeUnitPriceCredits = info.StationPriceCredits;
        _lastSentTradeItemTypeId = info.ItemTypeId;
        _lastSentTradeItemDisplayName = info.DisplayName;
        _lastSentTradeWasFuel = info.IsFuel;
        _lastSentAmountBeforeTrade = info.IsFuel ? info.FuelAmountKg : info.ContainerAvailableCapacityKg ?? 0;
        _lastSentCreditsBeforeTrade = info.PlayerCredits;
        _tradeRejectionReasonKey = null;
        _tradeResultMessage = null;

        // Clear the grid selection right away so the action panel drops back to its empty
        // state (Docs/FirstRelease/Screens/Trade.md's action panel), which now doubles as the
        // trade-result view (see DrawTradeActionPanel) once UpdateTradeCommandResult populates
        // _tradeResultMessage/_tradeRejectionReasonKey from a later snapshot — deliberately not
        // routed through ResetTradeActionPanelState, which would wipe those two fields along
        // with _lastSentTradeCommandId before the result has a chance to arrive.
        _selectedResourceItemTypeId = null;
        _selectedGoodItemTypeId = null;
        _selectedModuleItemTypeId = null;
    }

    /// <summary>Currently resolved <see cref="TradeActionInfo"/> for the selected item, or null while nothing is selected/resolvable — shared by the test seams below and <see cref="DrawTradeActionPanel"/>.</summary>
    private TradeActionInfo? ResolveCurrentTradeActionInfo() =>
        ResolveTradeActionInfo(_buffer?.Latest?.Snapshot, SelectedTradeItemTypeId);

    /// <summary>Test seam — whether the confirm button is currently enabled (proactive checks only — see <see cref="ResolveConfirmDisabledReasonKey"/>), false while nothing is selected.</summary>
    internal bool CanConfirmTrade =>
        ResolveCurrentTradeActionInfo() is { } info
        && ResolveConfirmDisabledReasonKey(info, _isTradeBuyMode, _tradeQuantity) is null;

    /// <summary>Test seam — the localization key for why the confirm button is currently disabled (proactive reason takes priority over a stale reactive rejection reason — see <see cref="ResolveConfirmDisabledReasonKey"/>/<see cref="_tradeRejectionReasonKey"/>), or null when it's enabled.</summary>
    internal string? TradeDisabledReasonKey =>
        ResolveCurrentTradeActionInfo() is { } info
            ? ResolveConfirmDisabledReasonKey(info, _isTradeBuyMode, _tradeQuantity) ?? _tradeRejectionReasonKey
            : null;

    /// <summary>Test seam — the resolved Max quantity the stepper's `Max` button would set (see <see cref="ResolveMaxQuantity"/>), or 0 while nothing is selected.</summary>
    internal long TradeMaxQuantity =>
        ResolveCurrentTradeActionInfo() is { } info ? ResolveMaxQuantity(info, _isTradeBuyMode) : 0;

    // ── Trade action panel drawing.

    private static readonly SKPaint _tradeTitleBarTextPaint = new()
    {
        Color = SKColors.White, TextSize = 16f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceHumaroid
    };

    private static readonly SKPaint _tradeBodyTextPaint = new()
    {
        Color = SKColors.White, TextSize = 13f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceRegular
    };

    private static readonly SKPaint _tradeBodyTextPaintCentered = new()
    {
        Color = SKColors.White, TextSize = 13f, IsAntialias = true,
        TextAlign = SKTextAlign.Center, Typeface = MenuStyle.TypefaceRegular
    };

    /// <summary>Reason text (proactive disable or reactive rejection) — dim, distinct from the normal body text.</summary>
    private static readonly SKPaint _tradeReasonTextPaint = new()
    {
        Color = MenuStyle.ColorTextDim, TextSize = 12f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceRegular
    };

    /// <summary>
    /// Result message text (<see cref="_tradeResultMessage"/>) — a distinct green, unlike the
    /// dim <see cref="_tradeReasonTextPaint"/> used for disabled/rejection reasons, so a
    /// successful Buy/Sell/Refuel actually reads as a positive confirmation rather than
    /// blending in with the same muted styling used for "something's wrong" text.
    /// </summary>
    private static readonly SKPaint _tradeResultMessagePaint = new()
    {
        Color = new SKColor(0x5A, 0xD6, 0x6D), TextSize = 12f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceBold
    };

    /// <summary>
    /// Colors the after-value in a trade-result before/after line (<see cref="DrawBeforeAfterLine"/>)
    /// when it improved on the before-value (free cargo or Credits went up) — green, same
    /// intent as <see cref="_tradeResultMessagePaint"/> but a separate paint so this coloring
    /// (which follows the sign of the change) stays decoupled from that unconditional
    /// success-message styling.
    /// </summary>
    private static readonly SKPaint _tradeResultPositivePaint = new()
    {
        Color = new SKColor(0x5A, 0xD6, 0x6D), TextSize = 13f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceBold
    };

    /// <summary>Counterpart to <see cref="_tradeResultPositivePaint"/> — orange, used when the after-value dropped from the before-value.</summary>
    private static readonly SKPaint _tradeResultNegativePaint = new()
    {
        Color = new SKColor(0xF2, 0x9A, 0x3C), TextSize = 13f, IsAntialias = true,
        TextAlign = SKTextAlign.Left, Typeface = MenuStyle.TypefaceBold
    };

    /// <summary>Vertical baseline for a single line of text centered within <paramref name="rect"/>, matching <see cref="MenuStyle.VerticalCenterBaseline"/>'s convention.</summary>
    private static float LineBaselineY(SKRect rect, SKPaint paint) => MenuStyle.VerticalCenterBaseline(rect, paint);

    /// <summary>Quantity slider track — a thin filled bar, same border color as the other action-panel controls.</summary>
    private static readonly SKPaint _tradeSliderTrackPaint = new()
    {
        Color = MenuStyle.ButtonBorder.Color, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true
    };

    /// <summary>Quantity slider thumb — a small filled circle positioned by the current quantity's fraction of `[min, Max]`.</summary>
    private static readonly SKPaint _tradeSliderThumbPaint = new()
    {
        Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true
    };

    /// <summary>
    /// Full trade-result detail block shown in the action panel's empty state once a trade has
    /// executed (see <see cref="OnConfirmTradeClicked"/>/<see cref="UpdateTradeCommandResult"/>):
    /// the traded item's name and the existing success/partial-fill summary (unit price ×
    /// quantity = total) in white, then the free-cargo-or-Fuel amount and the Credits balance
    /// each as a "before - after" line (<see cref="DrawBeforeAfterLine"/>) whose after-value is
    /// colored by the sign of the change, and finally a "Return to item" button
    /// (<see cref="HandleTradeResultReturnButtonMouseDown"/>) that re-selects the traded item.
    /// </summary>
    private void DrawTradeResultDetails(SKCanvas canvas)
    {
        var line1 = ToScreenRect(_tradeMarketLine1Rect);
        canvas.DrawText(_tradeResultItemDisplayName!, line1.Left, LineBaselineY(line1, _tradeBodyTextPaint), _tradeBodyTextPaint);

        var line2 = ToScreenRect(_tradeMarketLine2Rect);
        canvas.DrawText(_tradeResultMessage!, line2.Left, LineBaselineY(line2, _tradeBodyTextPaint), _tradeBodyTextPaint);

        string amountLabel = Localization.Get(_tradeResultWasFuel ? "Trade.Fuel" : "Trade.FreeCargo");
        DrawBeforeAfterLine(canvas, ToScreenRect(_tradeResultLine3Rect), amountLabel, _tradeResultAmountBefore ?? 0, _tradeResultAmountAfter ?? 0);

        string creditsLabel = Localization.Get("Trade.Credits");
        DrawBeforeAfterLine(canvas, ToScreenRect(_tradeResultLine4Rect), creditsLabel, _tradeResultCreditsBefore ?? 0, _tradeResultCreditsAfter ?? 0);

        // Same bottom-anchored slot the confirm button occupies while an item is selected
        // (the two states are mutually exclusive, so there's no layout conflict reusing it).
        var returnButtonRect = ToScreenRect(_tradeConfirmButtonRect);
        MenuStyle.DrawButton(canvas, returnButtonRect, Localization.Get("Trade.ReturnToItem"),
            _isReturnToItemHovered ? ButtonState.Hovered : ButtonState.Normal);
    }

    /// <summary>
    /// Draws one "{label}: {before} - {after}" trade-result line: label and before-value in the
    /// panel's normal white body text, plain "-" as the before/after separator (not an arrow
    /// glyph — the panel's font has no glyph for it and renders a tofu box instead), and the
    /// after-value colored green when it's higher than the before-value, orange when it's
    /// lower, left white when unchanged.
    /// </summary>
    private void DrawBeforeAfterLine(SKCanvas canvas, SKRect lineRectScreen, string label, long before, long after)
    {
        string prefix = string.Format(Localization.Get("Trade.ResultLinePrefix"), label, before);
        float baselineY = LineBaselineY(lineRectScreen, _tradeBodyTextPaint);
        canvas.DrawText(prefix, lineRectScreen.Left, baselineY, _tradeBodyTextPaint);

        SKPaint afterPaint = after > before ? _tradeResultPositivePaint : after < before ? _tradeResultNegativePaint : _tradeBodyTextPaint;
        float prefixWidth = _tradeBodyTextPaint.MeasureText(prefix);
        canvas.DrawText(after.ToString(), lineRectScreen.Left + prefixWidth, baselineY, afterPaint);
    }

    /// <summary>
    /// Draws the lower right-hand panel's title (item name/category, or the empty-state
    /// title) and, once an item is selected, its full Buy/Sell transaction content — the
    /// panel's white outline and gray titlebar background are already drawn by the caller.
    /// </summary>
    private void DrawTradeActionPanel(SKCanvas canvas, float pl, float pt, AuthoritativeSnapshot? snapshot)
    {
        string? itemTypeId = SelectedTradeItemTypeId;
        var info = ResolveTradeActionInfo(snapshot, itemTypeId);

        // Once a trade has been sent (selection is cleared right on confirm — see
        // OnConfirmTradeClicked), the empty state below doubles as the trade-result view until
        // the player picks a new item: its title and body switch from the plain "select an
        // item" prompt to the outcome of that trade.
        bool hasTradeOutcome = _tradeResultMessage is not null || _tradeRejectionReasonKey is not null;

        var titleBarScreen = ToScreenRect(_rightPanelLowerTitleBar);
        string titleText = info is { } selected
            ? $"{selected.DisplayName} ({selected.Category})"
            : Localization.Get(hasTradeOutcome ? "Trade.ResultTitle" : "Trade.SelectItemTitle");
        canvas.DrawText(titleText, titleBarScreen.Left + 10f, LineBaselineY(titleBarScreen, _tradeTitleBarTextPaint), _tradeTitleBarTextPaint);

        if (info is not { } tradeInfo)
        {
            if (_tradeResultMessage is not null && _tradeResultItemDisplayName is not null)
            {
                DrawTradeResultDetails(canvas);
                return;
            }

            var emptyRect = ToScreenRect(new SKRect(
                TradeActionContentLeft, TradeActionContentTop, TradeActionContentRight, _rightPanelLower.Bottom - 10f));
            string emptyText = hasTradeOutcome
                ? Localization.Get(_tradeRejectionReasonKey!)
                : Localization.Get("Trade.SelectItemPrompt");
            canvas.DrawText(emptyText, emptyRect.MidX, emptyRect.MidY, _tradeBodyTextPaintCentered);
            return;
        }

        // Market info — two lines (Docs/FirstRelease/Screens/Trade.md: station price, then
        // station stock / player's own holding of the item).
        var marketLine1 = ToScreenRect(_tradeMarketLine1Rect);
        canvas.DrawText($"{Localization.Get("Trade.UnitPrice")}: {tradeInfo.StationPriceCredits}",
            marketLine1.Left, LineBaselineY(marketLine1, _tradeBodyTextPaint), _tradeBodyTextPaint);

        var marketLine2 = ToScreenRect(_tradeMarketLine2Rect);
        string marketLine2Text = tradeInfo.IsFuel
            ? $"{Localization.Get("Trade.StationInventory")}: {tradeInfo.StationStockQuantity}   {Localization.Get("Trade.Fuel")}: {tradeInfo.FuelAmountKg}/{tradeInfo.FuelCapacityKg}"
            : $"{Localization.Get("Trade.StationInventory")}: {tradeInfo.StationStockQuantity}   {Localization.Get("Trade.Cargo")}: {tradeInfo.PlayerCargoQuantity}";
        canvas.DrawText(marketLine2Text, marketLine2.Left, LineBaselineY(marketLine2, _tradeBodyTextPaint), _tradeBodyTextPaint);

        // Buy/Sell toggle.
        var buyRect = ToScreenRect(_tradeBuyButtonRect);
        MenuStyle.DrawButton(canvas, buyRect, Localization.Get("Trade.Buy"),
            _isTradeBuyMode ? ButtonState.Pressed : ButtonState.Normal);
        var sellRect = ToScreenRect(_tradeSellButtonRect);
        MenuStyle.DrawButton(canvas, sellRect, Localization.Get("Trade.Sell"),
            tradeInfo.IsFuel ? ButtonState.Disabled : (!_isTradeBuyMode ? ButtonState.Pressed : ButtonState.Normal));

        // Quantity stepper.
        var minusRect = ToScreenRect(_tradeMinusButtonRect);
        MenuStyle.DrawButton(canvas, minusRect, "-", _isTradeMinusHovered ? ButtonState.Hovered : ButtonState.Normal);
        var plusRect = ToScreenRect(_tradePlusButtonRect);
        MenuStyle.DrawButton(canvas, plusRect, "+", _isTradePlusHovered ? ButtonState.Hovered : ButtonState.Normal);
        var maxRect = ToScreenRect(_tradeMaxButtonRect);
        MenuStyle.DrawButton(canvas, maxRect, Localization.Get("Trade.Max"), _isTradeMaxHovered ? ButtonState.Hovered : ButtonState.Normal);
        var quantityRect = ToScreenRect(_tradeQuantityFieldRect);
        canvas.DrawRect(quantityRect, MenuStyle.ButtonBorder);
        canvas.DrawText(_tradeQuantity.ToString(), quantityRect.MidX, LineBaselineY(quantityRect, _tradeBodyTextPaintCentered), _tradeBodyTextPaintCentered);

        // Quantity slider (Docs/FirstRelease/Screens/Trade.md, "UI-решение: панель действия",
        // step 5) — full-width track, thumb positioned by the current quantity's fraction of
        // [min, Max].
        var sliderRect = ToScreenRect(_tradeSliderRowRect);
        long sliderMax = ResolveMaxQuantity(tradeInfo, _isTradeBuyMode);
        long sliderMin = sliderMax > 0 ? 1 : 0;
        float sliderTrackY = sliderRect.MidY;
        canvas.DrawLine(sliderRect.Left, sliderTrackY, sliderRect.Right, sliderTrackY, _tradeSliderTrackPaint);
        float sliderFraction = sliderMax > sliderMin
            ? (float)(Math.Clamp(_tradeQuantity, sliderMin, sliderMax) - sliderMin) / (sliderMax - sliderMin)
            : 0f;
        float sliderThumbX = sliderRect.Left + sliderFraction * sliderRect.Width;
        canvas.DrawCircle(sliderThumbX, sliderTrackY, TradeSliderRowHeight / 2f - 2f, _tradeSliderThumbPaint);

        // Transaction summary.
        long totalPrice = tradeInfo.StationPriceCredits * _tradeQuantity;
        long creditsChange = _isTradeBuyMode ? -totalPrice : totalPrice;
        long cargoChange = _isTradeBuyMode ? _tradeQuantity : -_tradeQuantity;
        var summaryLine1 = ToScreenRect(_tradeSummaryLine1Rect);
        canvas.DrawText($"{Localization.Get("Trade.TotalPrice")}: {totalPrice}",
            summaryLine1.Left, LineBaselineY(summaryLine1, _tradeBodyTextPaint), _tradeBodyTextPaint);
        var summaryLine2 = ToScreenRect(_tradeSummaryLine2Rect);
        string changeLabel = tradeInfo.IsFuel ? Localization.Get("Trade.Fuel") : Localization.Get("Trade.Cargo");
        canvas.DrawText($"{Localization.Get("Trade.Credits")}: {(creditsChange >= 0 ? "+" : string.Empty)}{creditsChange}   {changeLabel}: {(cargoChange >= 0 ? "+" : string.Empty)}{cargoChange}",
            summaryLine2.Left, LineBaselineY(summaryLine2, _tradeBodyTextPaint), _tradeBodyTextPaint);

        // Message line above the confirm button — proactive disable reason takes priority over
        // a stale reactive rejection, which in turn takes priority over a past success message
        // (Docs/FirstRelease/Screens/Trade.md: makes a Buy/Sell/Refuel visibly acknowledged
        // instead of only showing up a second later as updated grid numbers).
        string? reasonKey = ResolveConfirmDisabledReasonKey(tradeInfo, _isTradeBuyMode, _tradeQuantity) ?? _tradeRejectionReasonKey;
        if (reasonKey is not null)
        {
            var reasonRect = ToScreenRect(_tradeReasonLineRect);
            canvas.DrawText(Localization.Get(reasonKey), reasonRect.Left, LineBaselineY(reasonRect, _tradeReasonTextPaint), _tradeReasonTextPaint);
        }
        else if (_tradeResultMessage is not null)
        {
            var resultRect = ToScreenRect(_tradeReasonLineRect);
            canvas.DrawText(_tradeResultMessage, resultRect.Left, LineBaselineY(resultRect, _tradeResultMessagePaint), _tradeResultMessagePaint);
        }

        // Confirm button.
        bool canConfirm = ResolveConfirmDisabledReasonKey(tradeInfo, _isTradeBuyMode, _tradeQuantity) is null;
        string confirmLabel = tradeInfo.IsFuel
            ? Localization.Get("Trade.Refuel")
            : (_isTradeBuyMode ? Localization.Get("Trade.Buy") : Localization.Get("Trade.Sell"));
        var confirmRect = ToScreenRect(_tradeConfirmButtonRect);
        var confirmState = !canConfirm ? ButtonState.Disabled : (_isTradeConfirmHovered ? ButtonState.Hovered : ButtonState.Normal);
        MenuStyle.DrawButton(canvas, confirmRect, confirmLabel, confirmState);
    }

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

    /// <summary>Test seam — the modules grid's current row labels (see <see cref="ResolveModuleRows"/>) — always empty until real module-trade data exists.</summary>
    internal string[] ModuleNames => ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules).Select(row => row.Name).ToArray();

    /// <summary>Test seam — the modules grid's current "Selling price" column values, same row order as <see cref="ModuleNames"/>.</summary>
    internal string[] ModuleSellingPrices => ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules).Select(row => row.SellingPrice).ToArray();

    /// <summary>Test seam — the modules grid's current "Selling count" column values, same row order as <see cref="ModuleNames"/>.</summary>
    internal string[] ModuleSellingCounts => ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules).Select(row => row.SellingCount).ToArray();

    /// <summary>Test seam — the modules grid's current "Buying price" column values, same row order as <see cref="ModuleNames"/>.</summary>
    internal string[] ModuleBuyingPrices => ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules).Select(row => row.BuyingPrice).ToArray();

    /// <summary>Test seam — the modules grid's current "Buying count" column values, same row order as <see cref="ModuleNames"/>.</summary>
    internal string[] ModuleBuyingCounts => ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules).Select(row => row.BuyingCount).ToArray();

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
    /// The Modules grid's rows — placeholder always returning no rows, same shape/scroll/
    /// sort mechanics as <see cref="ResolveResourceRows"/>/<see cref="ResolveGoodRows"/> so
    /// the grid renders and behaves identically. No station-module-for-sale data model
    /// exists yet (<see cref="TradeItemCategories"/> only has Resource/Good — see
    /// <see cref="StationTradeSnapshot"/>); wire this up to real data once one does, the
    /// same way the other two grids were.
    /// </summary>
    private static ResourceRow[] ResolveModuleRows(AuthoritativeSnapshot? snapshot, GridSortColumn sortColumn, bool sortDescending) =>
        Array.Empty<ResourceRow>();

    /// <summary>
    /// Player's ship cargo, keyed by item type id — from the first installed module (by
    /// Position) whose CommandTypeIds handles Buy, the same "container module" resolution
    /// the pre-redesign TradeScreen used (story file's ResolveModuleId/FindModule).
    /// </summary>
    private static Dictionary<string, long> ResolvePlayerCargo(AuthoritativeSnapshot? snapshot)
    {
        var containerModule = ResolveContainerModule(snapshot);
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

    public TradeScreen(SnapshotBuffer? buffer = null, GameSessionHandle? handle = null)
    {
        _buffer = buffer;
        _handle = handle;
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
        _isScrollUpHoveredModules = false;
        _isScrollDownHoveredModules = false;
        _isDraggingScrollThumbModules = false;
        _selectedModuleItemTypeId = null;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;
        _tokensHoverStartedAtMs = null;
        _fuelHoverStartedAtMs = null;
        _isDraggingTradeSlider = false;
        _isTradeConfirmHovered = false;
        _isReturnToItemHovered = false;
        _isTradeMinusHovered = false;
        _isTradePlusHovered = false;
        _isTradeMaxHovered = false;
        ResetTradeActionPanelState(_buffer?.Latest?.Snapshot);
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

        if (HandleTradeActionPanelMouseDown(x, y))
            return ScreenEvent.None;

        if (HandleTradeResultReturnButtonMouseDown(x, y))
            return ScreenEvent.None;

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
            string clickedResourceItemTypeId = resourceRows[hitRowIndex].ItemTypeId;
            // Clicking the already-selected row toggles the selection off instead of re-selecting it.
            _selectedResourceItemTypeId = _selectedResourceItemTypeId == clickedResourceItemTypeId
                ? null : clickedResourceItemTypeId;
            // The three grids share a single selection — picking a row in one clears the others.
            _selectedGoodItemTypeId = null;
            _selectedModuleItemTypeId = null;
            ResetTradeActionPanelState(_buffer?.Latest?.Snapshot);
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
            string clickedGoodItemTypeId = goodRows[hitGoodRowIndex].ItemTypeId;
            // Clicking the already-selected row toggles the selection off instead of re-selecting it.
            _selectedGoodItemTypeId = _selectedGoodItemTypeId == clickedGoodItemTypeId
                ? null : clickedGoodItemTypeId;
            // The three grids share a single selection — picking a row in one clears the others.
            _selectedResourceItemTypeId = null;
            _selectedModuleItemTypeId = null;
            ResetTradeActionPanelState(_buffer?.Latest?.Snapshot);
            return ScreenEvent.None;
        }

        var moduleRows = ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules);
        int moduleRowCount = moduleRows.Length;
        if (GridPanel.IsScrollbarActive(moduleRowCount))
        {
            if (IsScrollUpArrowHitModules(x, y))
            {
                _scrollOffsetModules = Math.Max(0, _scrollOffsetModules - 1);
                return ScreenEvent.None;
            }

            if (IsScrollDownArrowHitModules(x, y))
            {
                _scrollOffsetModules = Math.Min(GridPanel.MaxScrollOffset(moduleRowCount), _scrollOffsetModules + 1);
                return ScreenEvent.None;
            }

            float pt = TradeLayout.PanelTop(_screenHeight);
            var thumbLocalModules = GridPanel.ScrollThumbLocalRect(GridPanelOriginX, GridPanelOriginYModules, moduleRowCount, _scrollOffsetModules);
            float clickLocalYModules = y - pt;
            if (IsScrollThumbHitModules(x, y))
            {
                _isDraggingScrollThumbModules = true;
                _scrollThumbDragGrabOffsetYModules = clickLocalYModules - thumbLocalModules.Top;
                return ScreenEvent.None;
            }
        }

        var hitModuleColumnTitle = HitTestModuleColumnTitle(x, y);
        if (hitModuleColumnTitle is { } clickedModuleColumn)
        {
            _sortDescendingModules = _sortColumnModules == clickedModuleColumn && !_sortDescendingModules;
            _sortColumnModules = clickedModuleColumn;
            // Selection is identity-based (_selectedModuleItemTypeId), not index-based — it
            // survives the resort and is resolved back to wherever that item now sits.
            return ScreenEvent.None;
        }

        int hitModuleRowIndex = HitTestModuleRow(x, y, moduleRowCount);
        if (hitModuleRowIndex >= 0)
        {
            string clickedModuleItemTypeId = moduleRows[hitModuleRowIndex].ItemTypeId;
            // Clicking the already-selected row toggles the selection off instead of re-selecting it.
            _selectedModuleItemTypeId = _selectedModuleItemTypeId == clickedModuleItemTypeId
                ? null : clickedModuleItemTypeId;
            // The three grids share a single selection — picking a row in one clears the others.
            _selectedResourceItemTypeId = null;
            _selectedGoodItemTypeId = null;
            ResetTradeActionPanelState(_buffer?.Latest?.Snapshot);
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

    /// <summary>Absolute modules-grid row index hit by a click at screen coordinates (x, y), or -1 — see <see cref="GridPanel.HitTestRow"/>. Always -1 today since the placeholder grid has no rows (see <see cref="ResolveModuleRows"/>).</summary>
    private int HitTestModuleRow(float x, float y, int moduleRowCount)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        return GridPanel.HitTestRow(GridPanelOriginX, GridPanelOriginYModules, moduleRowCount, _scrollOffsetModules, x - pl, y - pt);
    }

    /// <summary>Same as <see cref="HitTestColumnTitle"/> but for the Modules grid.</summary>
    private GridSortColumn? HitTestModuleColumnTitle(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        return GridPanel.HitTestColumnTitle(GridPanelOriginX, GridPanelOriginYModules, ModulesGridTitle, x - pl, y - pt);
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    /// <summary>Ends a scrollbar-thumb drag or quantity-slider drag on left-button release, wherever the pointer ends up — see <see cref="_isDraggingScrollThumb"/>/<see cref="_isDraggingScrollThumbGoods"/>/<see cref="_isDraggingScrollThumbModules"/>/<see cref="_isDraggingTradeSlider"/>.</summary>
    public void OnMouseUp(float x, float y)
    {
        _isDraggingScrollThumb = false;
        _isDraggingScrollThumbGoods = false;
        _isDraggingScrollThumbModules = false;
        _isDraggingTradeSlider = false;
    }

    public bool OnMouseMove(float x, float y)
    {
        if (_isDraggingTradeSlider)
        {
            var draggedInfo = ResolveCurrentTradeActionInfo();
            if (draggedInfo is { } info)
            {
                long max = ResolveMaxQuantity(info, _isTradeBuyMode);
                _tradeQuantity = ResolveSliderQuantity(x, ToScreenRect(_tradeSliderRowRect), max);
            }
        }

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

        if (_isDraggingScrollThumbModules)
        {
            int moduleRowCount = CurrentModuleRowCount();
            float pt = TradeLayout.PanelTop(_screenHeight);
            float desiredThumbTopLocalYModules = (y - pt) - _scrollThumbDragGrabOffsetYModules;
            _scrollOffsetModules = GridPanel.ResolveScrollOffsetForThumbTop(GridPanelOriginX, GridPanelOriginYModules, moduleRowCount, desiredThumbTopLocalYModules);
        }

        var hoveredTradeInfo = ResolveCurrentTradeActionInfo();
        _isTradeConfirmHovered = hoveredTradeInfo is { } hti
            && Contains(ToScreenRect(_tradeConfirmButtonRect), x, y)
            && ResolveConfirmDisabledReasonKey(hti, _isTradeBuyMode, _tradeQuantity) is null;

        _isReturnToItemHovered = hoveredTradeInfo is null && _tradeResultItemTypeId is not null
            && Contains(ToScreenRect(_tradeConfirmButtonRect), x, y);

        _isTradeMinusHovered = hoveredTradeInfo is not null && Contains(ToScreenRect(_tradeMinusButtonRect), x, y);
        _isTradePlusHovered = hoveredTradeInfo is not null && Contains(ToScreenRect(_tradePlusButtonRect), x, y);
        _isTradeMaxHovered = hoveredTradeInfo is not null && Contains(ToScreenRect(_tradeMaxButtonRect), x, y);

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

        bool isModulesScrollbarActive = GridPanel.IsScrollbarActive(CurrentModuleRowCount());
        _isScrollUpHoveredModules = isModulesScrollbarActive && IsScrollUpArrowHitModules(x, y);
        _isScrollDownHoveredModules = isModulesScrollbarActive && IsScrollDownArrowHitModules(x, y);
        bool isModuleColumnTitleHovered = HitTestModuleColumnTitle(x, y) is not null;

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
            || _isScrollUpHoveredGoods || _isScrollDownHoveredGoods || _isDraggingScrollThumbGoods || isGoodColumnTitleHovered
            || _isScrollUpHoveredModules || _isScrollDownHoveredModules || _isDraggingScrollThumbModules || isModuleColumnTitleHovered
            || _isDraggingTradeSlider || _isTradeConfirmHovered || _isReturnToItemHovered
            || _isTradeMinusHovered || _isTradePlusHovered || _isTradeMaxHovered;
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

        if (IsWithinModuleGridRows(y))
        {
            int maxOffsetModules = GridPanel.MaxScrollOffset(CurrentModuleRowCount());
            _scrollOffsetModules = Math.Clamp(_scrollOffsetModules - Math.Sign(delta), 0, maxOffsetModules);
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

    /// <summary>Same as <see cref="IsWithinGoodGridRows"/> but for the Modules grid's header+rows band (see <see cref="GridPanelOriginYModules"/>).</summary>
    private bool IsWithinModuleGridRows(float y)
    {
        float pt = TradeLayout.PanelTop(_screenHeight);
        var header = GridPanel.HeaderLocalRect(GridPanelOriginX, GridPanelOriginYModules);
        var lastRow = GridPanel.RowLocalRect(GridPanelOriginX, GridPanelOriginYModules, GridPanel.MaxVisibleRows - 1);
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
        UpdateTradeCommandResult(snapshot);
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: false, isHovered: _isStationNameHovered,
            windowName: "TRADE", isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot));

        DrawCompartmentImage(canvas, pl, pt);

        var contentRect = new SKRect(pl + _contentOutlineRect.Left, pt + _contentOutlineRect.Top,
            pl + _contentOutlineRect.Right, pt + _contentOutlineRect.Bottom);
        canvas.DrawRect(contentRect, _contentOutlinePaint);

        var contentRectGoods = new SKRect(pl + _contentOutlineRectGoods.Left, pt + _contentOutlineRectGoods.Top,
            pl + _contentOutlineRectGoods.Right, pt + _contentOutlineRectGoods.Bottom);
        canvas.DrawRect(contentRectGoods, _contentOutlinePaint);

        var contentRectModules = new SKRect(pl + _contentOutlineRectModules.Left, pt + _contentOutlineRectModules.Top,
            pl + _contentOutlineRectModules.Right, pt + _contentOutlineRectModules.Bottom);
        canvas.DrawRect(contentRectModules, _contentOutlinePaint);

        var rightPanelUpper = new SKRect(pl + _rightPanelUpper.Left, pt + _rightPanelUpper.Top,
            pl + _rightPanelUpper.Right, pt + _rightPanelUpper.Bottom);
        canvas.DrawRect(rightPanelUpper, _contentOutlinePaint);
        var rightPanelUpperTitleBar = new SKRect(pl + _rightPanelUpperTitleBar.Left, pt + _rightPanelUpperTitleBar.Top,
            pl + _rightPanelUpperTitleBar.Right, pt + _rightPanelUpperTitleBar.Bottom);
        canvas.DrawRoundRect(rightPanelUpperTitleBar, RightPanelTitleBarCornerRadius, RightPanelTitleBarCornerRadius, _rightPanelTitleBarPaint);

        var rightPanelLower = new SKRect(pl + _rightPanelLower.Left, pt + _rightPanelLower.Top,
            pl + _rightPanelLower.Right, pt + _rightPanelLower.Bottom);
        canvas.DrawRect(rightPanelLower, _contentOutlinePaint);
        var rightPanelLowerTitleBar = new SKRect(pl + _rightPanelLowerTitleBar.Left, pt + _rightPanelLowerTitleBar.Top,
            pl + _rightPanelLowerTitleBar.Right, pt + _rightPanelLowerTitleBar.Bottom);
        canvas.DrawRoundRect(rightPanelLowerTitleBar, RightPanelTitleBarCornerRadius, RightPanelTitleBarCornerRadius, _rightPanelTitleBarPaint);

        DrawTradeActionPanel(canvas, pl, pt, snapshot);

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

        var moduleRows = ResolveModuleRows(snapshot, _sortColumnModules, _sortDescendingModules);
        _scrollOffsetModules = Math.Clamp(_scrollOffsetModules, 0, GridPanel.MaxScrollOffset(moduleRows.Length));
        var moduleNames = Array.ConvertAll(moduleRows, row => row.Name);
        var moduleSellingPrices = Array.ConvertAll(moduleRows, row => row.SellingPrice);
        var moduleSellingCounts = Array.ConvertAll(moduleRows, row => row.SellingCount);
        var moduleBuyingPrices = Array.ConvertAll(moduleRows, row => row.BuyingPrice);
        var moduleBuyingCounts = Array.ConvertAll(moduleRows, row => row.BuyingCount);
        GridPanel.Draw(canvas, pl + GridPanelOriginX, pt + GridPanelOriginYModules, ModulesGridTitle, moduleRows.Length,
            _scrollOffsetModules, _isScrollUpHoveredModules, _isScrollDownHoveredModules, moduleNames,
            moduleSellingPrices, moduleSellingCounts, moduleBuyingPrices, moduleBuyingCounts,
            ResolveSelectedModuleRowIndex(moduleRows), _sortColumnModules, _sortDescendingModules);

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

    /// <summary>Current modules-grid row count — always 0 until real module-trade data exists, see <see cref="ResolveModuleRows"/>.</summary>
    private int CurrentModuleRowCount() => ResolveModuleRows(_buffer?.Latest?.Snapshot, _sortColumnModules, _sortDescendingModules).Length;

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

    /// <summary>Same as <see cref="IsScrollUpArrowHit"/> but for the Modules grid's scrollbar.</summary>
    private bool IsScrollUpArrowHitModules(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollUpArrowLocalRect(GridPanelOriginX, GridPanelOriginYModules, CurrentModuleRowCount());
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>Same as <see cref="IsScrollDownArrowHit"/> but for the Modules grid's scrollbar.</summary>
    private bool IsScrollDownArrowHitModules(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollDownArrowLocalRect(GridPanelOriginX, GridPanelOriginYModules, CurrentModuleRowCount());
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>Same as <see cref="IsScrollThumbHit"/> but for the Modules grid's scrollbar thumb (checked against <see cref="_scrollOffsetModules"/>).</summary>
    private bool IsScrollThumbHitModules(float x, float y)
    {
        float pl = TradeLayout.PanelLeft(_screenWidth);
        float pt = TradeLayout.PanelTop(_screenHeight);
        var local = GridPanel.ScrollThumbLocalRect(GridPanelOriginX, GridPanelOriginYModules, CurrentModuleRowCount(), _scrollOffsetModules);
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
