using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Save;

/// <summary>Which part of the Save overlay a click landed on.</summary>
public enum SaveZone
{
    None,
    Save,
    Close,
    Delete,
    Row
}

/// <summary>
/// Result of a <see cref="SaveLayout.HitTest"/>. <see cref="RowIndex"/> is only meaningful
/// for <see cref="SaveZone.Row"/> and is relative to the currently visible window.
/// </summary>
public readonly record struct SaveHit(SaveZone Zone, int RowIndex = -1)
{
    public static readonly SaveHit None = new(SaveZone.None);
}

/// <summary>
/// Layout and hit-test geometry for the Generic Type A Save overlay. A single content
/// panel owns both the always-visible name field and the selectable save list; every
/// action button is in the bottom row.
/// </summary>
public static class SaveLayout
{
    public const float PanelWidth = 900f;
    public const float PanelHeight = 600f;

    public const float Margin = 40f;

    public const float ContentPanelX = Margin;
    public const float ContentPanelY = 100f;
    public const float ContentPanelWidth = PanelWidth - 2 * Margin;
    public const float ContentPanelHeight = 380f;
    public const float ContentPadding = 20f;

    public const float NameInputWidth = 600f;
    public const float NameInputHeight = 44f;
    public const float NameInputY = ContentPanelY + ContentPadding;

    public const float NameToListGap = 36f;
    public const float ListTop = NameInputY + NameInputHeight + NameToListGap;
    public const float RowHeight = 50f;
    public const float RowSpacing = 6f;
    public const int VisibleRows = 5;

    public const float BottomButtonWidth = 200f;
    public const float BottomButtonHeight = 56f;
    public const float BottomButtonGap = 10f;
    public const float BottomButtonsX = 140f;
    public const float BottomButtonsY = 510f;

    public const float ScrollbarWidth = 6f;
    public const float ScrollbarGap = 10f;
    public const float ScrollbarThumbMinHeight = 20f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SKRect PanelRect(int screenWidth, int screenHeight)
    {
        float left = PanelLeft(screenWidth);
        float top = PanelTop(screenHeight);
        return new SKRect(left, top, left + PanelWidth, top + PanelHeight);
    }

    public static float ListWidth => ContentPanelWidth - 2 * ContentPadding;

    public static (float X, float Y, float W, float H) ContentPanelRect() =>
        (ContentPanelX, ContentPanelY, ContentPanelWidth, ContentPanelHeight);

    /// <summary>Top-left-relative rect for a visible row (before scroll offset is applied by the caller).</summary>
    public static (float X, float Y, float W, float H) RowRect(int visibleRowIndex)
    {
        float y = ListTop + visibleRowIndex * RowHeight;
        return (ContentPanelX + ContentPadding, y, ListWidth, RowHeight - RowSpacing);
    }

    public static (float X, float Y, float W, float H) SaveButtonRect() =>
        BottomButtonRect(2);

    public static (float X, float Y, float W, float H) DeleteButtonRect() =>
        BottomButtonRect(1);

    public static (float X, float Y, float W, float H) NameInputRect() =>
        ((PanelWidth - NameInputWidth) / 2f, NameInputY, NameInputWidth, NameInputHeight);

    public static (float X, float Y, float W, float H) CloseButtonRect() =>
        BottomButtonRect(0);

    /// <summary>
    /// Vertical track spanning the full visible row list, in the gap between the rows
    /// and the content panel's right edge. Only meaningful — and only drawn by the
    /// caller — when there are more slots than <see cref="VisibleRows"/>.
    /// </summary>
    public static (float X, float Y, float W, float H) ScrollbarTrackRect() =>
        (ContentPanelX + ContentPadding + ListWidth + ScrollbarGap,
            ListTop, ScrollbarWidth, VisibleRows * RowHeight - RowSpacing);

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
    public static SaveHit HitTest(
        float screenX, float screenY, int screenWidth, int screenHeight,
        int visibleSlotCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInRect(lx, ly, CloseButtonRect()))
            return new SaveHit(SaveZone.Close);
        if (IsInRect(lx, ly, DeleteButtonRect()))
            return new SaveHit(SaveZone.Delete);
        if (IsInRect(lx, ly, SaveButtonRect()))
            return new SaveHit(SaveZone.Save);

        for (int i = 0; i < visibleSlotCount; i++)
        {
            if (IsInRect(lx, ly, RowRect(i)))
                return new SaveHit(SaveZone.Row, i);
        }

        return SaveHit.None;
    }

    private static (float X, float Y, float W, float H) BottomButtonRect(int index) =>
        (BottomButtonsX + index * (BottomButtonWidth + BottomButtonGap),
            BottomButtonsY, BottomButtonWidth, BottomButtonHeight);

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
