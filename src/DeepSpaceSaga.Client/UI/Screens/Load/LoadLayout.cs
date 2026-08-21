namespace DeepSpaceSaga.Client.UI.Screens.Load;

/// <summary>Which part of the Load overlay a click landed on.</summary>
public enum LoadZone
{
    None,
    Close,
    Load,
    Delete
}

/// <summary>
/// Result of a <see cref="LoadLayout.HitTest"/>. <see cref="RowIndex"/> is only
/// meaningful for <see cref="LoadZone.Load"/>/<see cref="LoadZone.Delete"/> and is
/// relative to the currently visible window (the caller adds the scroll offset to get
/// the absolute slot index).
/// </summary>
public readonly record struct LoadHit(LoadZone Zone, int RowIndex = -1)
{
    public static readonly LoadHit None = new(LoadZone.None);
}

/// <summary>
/// Layout and hit-test geometry for the Load overlay panel. Pure geometry — no SKCanvas
/// dependency — following the same pattern as <see cref="Save.SaveLayout"/>. Structurally
/// simpler than Save: no New Save/Overwrite row, so the slot list starts right below the
/// title and each row shows a LOAD button plus the same two-stage in-place DELETE button.
/// </summary>
public static class LoadLayout
{
    public const float PanelWidth = 700f;
    public const float PanelHeight = 600f;

    public const float Margin = 40f;

    public const float TitleY = 50f;

    public const float ListTop = 110f;
    public const float RowHeight = 50f;
    public const float RowSpacing = 6f;
    public const int VisibleRows = 8;

    // Fixed regardless of label: wide enough for the longest row label ("CONFIRM?",
    // measured ~83.5px at ButtonFontSize 14 bold) plus icon and padding.
    public const float RowButtonWidth = 150f;
    public const float RowButtonHeight = 40f;
    public const float RowButtonGap = 10f;

    public const float CloseButtonWidth = 100f;
    public const float CloseButtonHeight = 32f;
    public const float CloseButtonMargin = 16f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static float ListWidth => PanelWidth - 2 * Margin;

    /// <summary>Top-left-relative rect for a visible row (before scroll offset is applied by the caller).</summary>
    public static (float X, float Y, float W, float H) RowRect(int visibleRowIndex)
    {
        float y = ListTop + visibleRowIndex * RowHeight;
        return (Margin, y, ListWidth, RowHeight - RowSpacing);
    }

    public static (float X, float Y, float W, float H) LoadButtonRect(int visibleRowIndex)
    {
        var row = RowRect(visibleRowIndex);
        float x = row.X + row.W - (RowButtonWidth * 2 + RowButtonGap);
        float y = row.Y + (row.H - RowButtonHeight) / 2f;
        return (x, y, RowButtonWidth, RowButtonHeight);
    }

    public static (float X, float Y, float W, float H) DeleteButtonRect(int visibleRowIndex)
    {
        var row = RowRect(visibleRowIndex);
        float x = row.X + row.W - RowButtonWidth;
        float y = row.Y + (row.H - RowButtonHeight) / 2f;
        return (x, y, RowButtonWidth, RowButtonHeight);
    }

    /// <summary>Top-right corner of the panel, matching a title-bar close button.</summary>
    public static (float X, float Y, float W, float H) CloseButtonRect() =>
        (PanelWidth - CloseButtonMargin - CloseButtonWidth, CloseButtonMargin, CloseButtonWidth, CloseButtonHeight);

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

        for (int i = 0; i < visibleSlotCount; i++)
        {
            if (IsInRect(lx, ly, LoadButtonRect(i)))
                return new LoadHit(LoadZone.Load, i);
            if (IsInRect(lx, ly, DeleteButtonRect(i)))
                return new LoadHit(LoadZone.Delete, i);
        }

        return LoadHit.None;
    }

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
