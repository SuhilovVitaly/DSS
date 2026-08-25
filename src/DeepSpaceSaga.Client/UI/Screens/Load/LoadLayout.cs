using SkiaSharp;

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
/// Layout and hit-test geometry for the Generic Type A Load overlay. One full-width
/// content panel holds the selectable save rows; CLOSE, DELETE, and LOAD form one
/// horizontal action row at the bottom of the window.
/// </summary>
public static class LoadLayout
{
    public const float PanelWidth = 700f;
    public const float PanelHeight = 600f;

    public const float Margin = 40f;

    /// <summary>Breathing room between a panel's border artwork and the content it holds.</summary>
    public const float ContentPadding = 20f;

    public const float ContentPanelY = 100f;

    /// <summary>Full-width inner panel containing the selectable save list.</summary>
    public const float ContentPanelX = Margin;
    public const float ContentPanelWidth = PanelWidth - 2 * Margin;
    public const float ContentPanelHeight = 380f;

    public const float BottomButtonWidth = 200f;
    public const float BottomButtonHeight = 56f;
    public const float BottomButtonGap = 10f;
    public const float BottomButtonsY = 510f;

    public const float RowHeight = 50f;
    public const float RowSpacing = 6f;

    public static float ListTop => ContentPanelY + ContentPadding;
    public static float ListWidth => ContentPanelWidth - 2 * ContentPadding;
    public static float ListHeight => ContentPanelHeight - 2 * ContentPadding;

    public static readonly int VisibleRows = (int)((ListHeight + RowSpacing) / RowHeight);

    public const float ScrollbarWidth = 6f;
    public const float ScrollbarGap = 0f;
    public const float ScrollbarThumbMinHeight = 20f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SKRect PanelRect(int screenWidth, int screenHeight)
    {
        float left = PanelLeft(screenWidth);
        float top = PanelTop(screenHeight);
        return new SKRect(left, top, left + PanelWidth, top + PanelHeight);
    }

    public static (float X, float Y, float W, float H) ContentPanelRect() =>
        (ContentPanelX, ContentPanelY, ContentPanelWidth, ContentPanelHeight);

    /// <summary>Top-left-relative rect for a visible row (before scroll offset is applied by the caller).</summary>
    public static (float X, float Y, float W, float H) RowRect(int visibleRowIndex)
    {
        float y = ListTop + visibleRowIndex * RowHeight;
        return (ContentPanelX + ContentPadding, y, ListWidth, RowHeight - RowSpacing);
    }

    public static (float X, float Y, float W, float H) CloseButtonRect() =>
        (Margin, BottomButtonsY, BottomButtonWidth, BottomButtonHeight);

    public static (float X, float Y, float W, float H) DeleteButtonRect() =>
        (Margin + BottomButtonWidth + BottomButtonGap,
            BottomButtonsY, BottomButtonWidth, BottomButtonHeight);

    public static (float X, float Y, float W, float H) LoadButtonRect() =>
        (Margin + 2 * (BottomButtonWidth + BottomButtonGap),
            BottomButtonsY, BottomButtonWidth, BottomButtonHeight);

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
