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
/// dependency. Redesigned after <see cref="ScenarioSelect.ScenarioSelectLayout"/>: rows are
/// selectable (click to select, does not act) and a single LOAD/DELETE button pair at the
/// panel's bottom acts on whichever row is currently selected — replacing the previous
/// design's LOAD/DELETE buttons repeated on every row.
/// </summary>
public static class LoadLayout
{
    public const float PanelWidth = 700f;
    public const float PanelHeight = 600f;

    public const float Margin = 40f;

    public const float TitleY = 50f;

    /// <summary>Breathing room between the content panel's border artwork and the row list it holds.</summary>
    public const float ContentPadding = 20f;
    public const float ContentPanelY = 90f;

    public const float ListTop = ContentPanelY + ContentPadding;
    public const float RowHeight = 50f;
    public const float RowSpacing = 6f;
    public const int VisibleRows = 8;

    public const float ActionButtonWidth = 140f;
    public const float ActionButtonHeight = 44f;
    public const float ActionButtonGap = 20f;
    public const float ActionButtonBottomMargin = 24f;

    public const float CloseButtonWidth = 100f;
    public const float CloseButtonHeight = 32f;
    public const float CloseButtonMargin = 16f;

    public const float ScrollbarWidth = 6f;
    public const float ScrollbarGap = 10f;
    public const float ScrollbarThumbMinHeight = 20f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static float ListWidth => PanelWidth - 2 * Margin;

    /// <summary>
    /// The content panel (nine-sliced background) holding the row list — spans from just
    /// below the title down to just above the LOAD/DELETE button row.
    /// </summary>
    public static (float X, float Y, float W, float H) ContentPanelRect()
    {
        float bottom = DeleteButtonRect().Y - ActionButtonGap;
        return (Margin, ContentPanelY, ListWidth, bottom - ContentPanelY);
    }

    /// <summary>Top-left-relative rect for a visible row (before scroll offset is applied by the caller).</summary>
    public static (float X, float Y, float W, float H) RowRect(int visibleRowIndex)
    {
        float y = ListTop + visibleRowIndex * RowHeight;
        return (Margin, y, ListWidth, RowHeight - RowSpacing);
    }

    /// <summary>
    /// DELETE button rect — secondary/destructive action, on the left of the pair (mirrors
    /// <see cref="ScenarioSelect.ScenarioSelectLayout.BackButtonRect"/>'s "secondary action
    /// left, primary action right" convention). Acts on whichever row is currently selected.
    /// </summary>
    public static (float X, float Y, float W, float H) DeleteButtonRect()
    {
        float totalWidth = 2 * ActionButtonWidth + ActionButtonGap;
        float x = (PanelWidth - totalWidth) / 2f;
        float y = PanelHeight - ActionButtonBottomMargin - ActionButtonHeight;
        return (x, y, ActionButtonWidth, ActionButtonHeight);
    }

    /// <summary>LOAD button rect — primary action, right of DELETE. Acts on the selected row.</summary>
    public static (float X, float Y, float W, float H) LoadButtonRect()
    {
        var delete = DeleteButtonRect();
        return (delete.X + ActionButtonWidth + ActionButtonGap, delete.Y, ActionButtonWidth, ActionButtonHeight);
    }

    /// <summary>Top-right corner of the panel, matching a title-bar close button.</summary>
    public static (float X, float Y, float W, float H) CloseButtonRect() =>
        (PanelWidth - CloseButtonMargin - CloseButtonWidth, CloseButtonMargin, CloseButtonWidth, CloseButtonHeight);

    /// <summary>
    /// Vertical track spanning the full visible row list, in the right margin strip just
    /// past where the row list ends. Only meaningful — and only drawn by the caller — when
    /// there are more slots than <see cref="VisibleRows"/>.
    /// </summary>
    public static (float X, float Y, float W, float H) ScrollbarTrackRect() =>
        (PanelWidth - Margin + ScrollbarGap, ListTop, ScrollbarWidth, VisibleRows * RowHeight - RowSpacing);

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
