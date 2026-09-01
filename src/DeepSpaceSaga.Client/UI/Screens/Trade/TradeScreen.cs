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

    /// <summary>
    /// Mockup scroll position (0..<see cref="ScrollbarMockStepCount"/>) driving the
    /// <see cref="GridPanel"/> thumb's position within the track — the resources grid
    /// itself is static (no real list to scroll yet), this only demonstrates the arrow
    /// buttons' click behavior ahead of the real redesign.
    /// </summary>
    private int _scrollPosition;

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
    private const int ScrollbarMockStepCount = 4;

    /// <summary>Test seam — the resources grid's current row labels (see <see cref="ResolveResourceNames"/>).</summary>
    internal string[] ResourceNames => ResolveResourceNames(_buffer?.Latest?.Snapshot);

    /// <summary>
    /// Display names of the docked station's <see cref="TradeItemCategories.Resource"/>
    /// items, alphabetically sorted — empty (not null) while undocked or before the first
    /// snapshot arrives, which <see cref="GridPanel"/> renders as its dark-gray empty state.
    /// </summary>
    private static string[] ResolveResourceNames(AuthoritativeSnapshot? snapshot)
    {
        var items = snapshot?.DockedStationTrade?.Items ?? default;
        if (items.IsDefaultOrEmpty)
            return Array.Empty<string>();

        return items
            .Where(item => item.Category == TradeItemCategories.Resource)
            .Select(item => ItemDisplayName(item.ItemTypeId))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
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

        if (GridPanel.IsScrollbarActive(CurrentResourceRowCount()))
        {
            if (IsScrollUpArrowHit(x, y))
            {
                _scrollPosition = Math.Max(0, _scrollPosition - 1);
                return ScreenEvent.None;
            }

            if (IsScrollDownArrowHit(x, y))
            {
                _scrollPosition = Math.Min(ScrollbarMockStepCount, _scrollPosition + 1);
                return ScreenEvent.None;
            }
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
        _isStationNameHovered = IsStationNameHit(x, y);
        _isExitButtonHovered = IsExitButtonHit(x, y);
        bool isScrollbarActive = GridPanel.IsScrollbarActive(CurrentResourceRowCount());
        _isScrollUpHovered = isScrollbarActive && IsScrollUpArrowHit(x, y);
        _isScrollDownHovered = isScrollbarActive && IsScrollDownArrowHit(x, y);

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

        return _isStationNameHovered || _isExitButtonHovered || _isScrollUpHovered || _isScrollDownHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

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

        var resourceNames = ResolveResourceNames(snapshot);
        GridPanel.Draw(canvas, pl + GridPanelOriginX, pt + GridPanelOriginY, ResourcesGridTitle, resourceNames.Length,
            _scrollPosition, ScrollbarMockStepCount, _isScrollUpHovered, _isScrollDownHovered, resourceNames);

        // Drawn last: the tooltip hangs below the toolbar into the body area and must
        // stay on top of everything the screen drew.
        StationToolbar.DrawTooltips(canvas, pl, pt,
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            isCrewHovered: IsCrewTooltipVisible,
            isTokensHovered: IsTokensTooltipVisible,
            isFuelHovered: IsFuelTooltipVisible);
    }

    /// <summary>Current resources-grid row count, from the docked station's live trade snapshot — see <see cref="ResolveResourceNames"/>.</summary>
    private int CurrentResourceRowCount() => ResolveResourceNames(_buffer?.Latest?.Snapshot).Length;

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
