namespace DeepSpaceSaga.Client.UI.Screens.Load;

/// <summary>Which part of the Load overlay a click landed on.</summary>
public enum LoadZone
{
    None,
    Close,
    Load,
    Delete,
    /// <summary>A save-slot row — selects it (see <see cref="LoadHit.RowIndex"/>); does not load it.</summary>
    Row
}

/// <summary>
/// Result of a <see cref="LoadLayout.HitTest"/>. <see cref="RowIndex"/> is only meaningful
/// for <see cref="LoadZone.Row"/> and is relative to the currently visible window (the
/// caller adds the scroll offset to get the absolute slot index) — slot rows are all the
/// same fixed height, unlike <see cref="ScenarioSelect.ScenarioSelectLayout"/>'s
/// description-wrapped variable-height rows, so Layout can resolve visible-row hits itself
/// without the caller precomputing positions.
/// </summary>
public readonly record struct LoadHit(LoadZone Zone, int RowIndex = -1)
{
    public static readonly LoadHit None = new(LoadZone.None);
}

/// <summary>
/// Layout and hit-test geometry for the Load overlay panel. Pure geometry — no SKCanvas
/// dependency. Structured after <see cref="ScenarioSelect.ScenarioSelectLayout"/>: a left
/// content panel holds the selectable row list, a right action panel holds LOAD/DELETE
/// (stacked vertically — the action panel is narrower than ScenarioSelect's, with no
/// scenario name/stats block to show), and CLOSE sits on its own at the very bottom of the
/// window, below both panels — mirroring <see cref="Trade.TradeLayout"/>'s bottom exit
/// button rather than a per-panel corner icon.
/// </summary>
public static class LoadLayout
{
    public const float PanelWidth = 700f;
    public const float PanelHeight = 600f;

    public const float Margin = 40f;

    /// <summary>
    /// Vertical band of the background art's title bar (sampled from
    /// window-background-700x600.png) — the title is vertically centered inside this band,
    /// not pinned to a fixed baseline.
    /// </summary>
    public const float TitleBarY = 36f;
    public const float TitleBarHeight = 72f;

    /// <summary>Breathing room between a panel's border artwork and the content it holds.</summary>
    public const float ContentPadding = 20f;

    public const float ContentPanelY = 100f;

    /// <summary>Left panel: the nine-sliced, selectable row list.</summary>
    public const float ContentPanelX = Margin;
    public const float ContentPanelWidth = 440f;

    /// <summary>Right panel: the nine-sliced LOAD/DELETE action panel — immediately right of the content panel, no gap.</summary>
    public static float ActionPanelX => ContentPanelX + ContentPanelWidth;
    public const float ActionPanelWidth = PanelWidth - 2 * Margin - ContentPanelWidth;

    public const float ActionButtonWidth = 140f;
    public const float ActionButtonHeight = 44f;
    /// <summary>Vertical gap between the stacked LOAD/DELETE buttons.</summary>
    public const float ActionButtonGap = 20f;

    public const float CloseButtonWidth = 140f;
    public const float CloseButtonHeight = 44f;
    public const float CloseButtonBottomMargin = 54f;

    /// <summary>Gap kept between the bottom of the content/action panels and the CLOSE button below them.</summary>
    public const float PanelToCloseGap = 20f;

    public static float PanelHeightForContent =>
        PanelHeight - CloseButtonBottomMargin - CloseButtonHeight - PanelToCloseGap - ContentPanelY;

    public const float RowHeight = 50f;
    public const float RowSpacing = 6f;

    public static float ListTop => ContentPanelY + ContentPadding;
    public static float ListWidth => ContentPanelWidth - 2 * ContentPadding;
    public static float ListHeight => PanelHeightForContent - 2 * ContentPadding;

    public static readonly int VisibleRows = (int)((ListHeight + RowSpacing) / RowHeight);

    public const float ScrollbarWidth = 6f;
    public const float ScrollbarGap = 0f;
    public const float ScrollbarThumbMinHeight = 20f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static (float X, float Y, float W, float H) ContentPanelRect() =>
        (ContentPanelX, ContentPanelY, ContentPanelWidth, PanelHeightForContent);

    public static (float X, float Y, float W, float H) ActionPanelRect() =>
        (ActionPanelX, ContentPanelY, ActionPanelWidth, PanelHeightForContent);

    /// <summary>Top-left-relative rect for a visible row (before scroll offset is applied by the caller).</summary>
    public static (float X, float Y, float W, float H) RowRect(int visibleRowIndex)
    {
        float y = ListTop + visibleRowIndex * RowHeight;
        return (ContentPanelX + ContentPadding, y, ListWidth, RowHeight - RowSpacing);
    }

    /// <summary>
    /// LOAD button rect — primary action, stacked above DELETE within the action panel.
    /// Acts on whichever row is currently selected.
    /// </summary>
    public static (float X, float Y, float W, float H) LoadButtonRect()
    {
        float totalHeight = 2 * ActionButtonHeight + ActionButtonGap;
        float x = ActionPanelX + (ActionPanelWidth - ActionButtonWidth) / 2f;
        float y = ContentPanelY + (PanelHeightForContent - totalHeight) / 2f;
        return (x, y, ActionButtonWidth, ActionButtonHeight);
    }

    /// <summary>DELETE button rect — secondary/destructive action, stacked below LOAD. Acts on the selected row.</summary>
    public static (float X, float Y, float W, float H) DeleteButtonRect()
    {
        var load = LoadButtonRect();
        return (load.X, load.Y + ActionButtonHeight + ActionButtonGap, ActionButtonWidth, ActionButtonHeight);
    }

    /// <summary>
    /// CLOSE button rect — a normal bottom-of-window button (mirrors
    /// <see cref="Trade.TradeLayout.ExitButtonRect"/>), centered below both panels, not a
    /// per-panel corner icon.
    /// </summary>
    public static (float X, float Y, float W, float H) CloseButtonRect()
    {
        float x = (PanelWidth - CloseButtonWidth) / 2f;
        float y = PanelHeight - CloseButtonBottomMargin - CloseButtonHeight;
        return (x, y, CloseButtonWidth, CloseButtonHeight);
    }

    /// <summary>
    /// Vertical track spanning the full visible row list, in the content panel's right
    /// margin strip just past where the row list ends. Only meaningful — and only drawn by
    /// the caller — when there are more slots than <see cref="VisibleRows"/>.
    /// </summary>
    public static (float X, float Y, float W, float H) ScrollbarTrackRect() =>
        (ContentPanelX + ContentPadding + ListWidth + ScrollbarGap, ListTop, ScrollbarWidth, VisibleRows * RowHeight - RowSpacing);

    /// <summary>
    /// Thumb position/size within <see cref="ScrollbarTrackRect"/> for the given scroll
    /// state. <paramref name="totalSlotCount"/> must be greater than <see cref="VisibleRows"/>
    /// (the caller only renders the scrollbar in that case); <paramref name="scrollOffset"/>
    /// is the same 0..(totalSlotCount-VisibleRows) value the screen clamps in OnMouseWheel.
    /// </summary>
    public static (float X, float Y, float W, float H) ScrollbarThumbRect(int scrollOffset, int totalSlotCount)
    {
        var track = ScrollbarTrackRect();
        float thumbHeight = Math.Max(ScrollbarThumbMinHeight, track.H * VisibleRows / totalSlotCount);

        int maxOffset = totalSlotCount - VisibleRows;
        float travel = track.H - thumbHeight;
        float thumbY = track.Y + (maxOffset > 0 ? travel * scrollOffset / maxOffset : 0f);

        return (track.X, thumbY, track.W, thumbHeight);
    }

    /// <summary>
    /// Hit-tests a click at screen coordinates. <paramref name="visibleSlotCount"/> is the
    /// number of slot rows actually rendered (min(total slots, VisibleRows)).
    /// </summary>
    public static LoadHit HitTest(
        float screenX, float screenY, int screenWidth, int screenHeight, int visibleSlotCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInRect(lx, ly, CloseButtonRect()))
            return new LoadHit(LoadZone.Close);
        if (IsInRect(lx, ly, DeleteButtonRect()))
            return new LoadHit(LoadZone.Delete);
        if (IsInRect(lx, ly, LoadButtonRect()))
            return new LoadHit(LoadZone.Load);

        for (int i = 0; i < visibleSlotCount; i++)
        {
            if (IsInRect(lx, ly, RowRect(i)))
                return new LoadHit(LoadZone.Row, i);
        }

        return LoadHit.None;
    }

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
