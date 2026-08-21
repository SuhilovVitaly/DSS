namespace DeepSpaceSaga.Client.UI.Screens.Save;

/// <summary>Which part of the Save overlay a click landed on.</summary>
public enum SaveZone
{
    None,
    NewSave,
    ConfirmNewSave,
    CancelNewSave,
    Close,
    Overwrite,
    Delete
}

/// <summary>
/// Result of a <see cref="SaveLayout.HitTest"/>. <see cref="RowIndex"/> is only
/// meaningful for <see cref="SaveZone.Overwrite"/>/<see cref="SaveZone.Delete"/> and is
/// relative to the currently visible window (the caller adds the scroll offset to get
/// the absolute slot index).
/// </summary>
public readonly record struct SaveHit(SaveZone Zone, int RowIndex = -1)
{
    public static readonly SaveHit None = new(SaveZone.None);
}

/// <summary>
/// Layout and hit-test geometry for the Save overlay panel.
/// Pure geometry — no SKCanvas dependency — following the same pattern as
/// <see cref="Settings.SettingsLayout"/>/<see cref="GameMenu.GameMenuLayout"/>.
/// </summary>
public static class SaveLayout
{
    public const float PanelWidth = 700f;
    public const float PanelHeight = 600f;

    public const float Margin = 40f;

    public const float TitleY = 50f;

    public const float NewSaveRowY = 90f;
    public const float NewSaveRowHeight = 44f;

    public const float NameInputWidth = 300f;
    public const float NameConfirmButtonWidth = 90f;
    public const float NameCancelButtonWidth = 90f;
    public const float NameFieldGap = 10f;

    public const float ListTop = NewSaveRowY + NewSaveRowHeight + 30f;
    public const float RowHeight = 50f;
    public const float RowSpacing = 6f;
    public const int VisibleRows = 6;

    public const float RowButtonWidth = 130f;
    public const float RowButtonHeight = 36f;
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

    public static (float X, float Y, float W, float H) OverwriteButtonRect(int visibleRowIndex)
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

    public static (float X, float Y, float W, float H) NewSaveButtonRect() =>
        (Margin, NewSaveRowY + (NewSaveRowHeight - RowButtonHeight) / 2f, RowButtonWidth, RowButtonHeight);

    public static (float X, float Y, float W, float H) NameInputRect() =>
        (Margin, NewSaveRowY, NameInputWidth, NewSaveRowHeight);

    public static (float X, float Y, float W, float H) ConfirmButtonRect()
    {
        var input = NameInputRect();
        return (input.X + input.W + NameFieldGap, NewSaveRowY, NameConfirmButtonWidth, NewSaveRowHeight);
    }

    public static (float X, float Y, float W, float H) CancelButtonRect()
    {
        var confirm = ConfirmButtonRect();
        return (confirm.X + confirm.W + NameFieldGap, NewSaveRowY, NameCancelButtonWidth, NewSaveRowHeight);
    }

    /// <summary>Top-right corner of the panel, matching a title-bar close button.</summary>
    public static (float X, float Y, float W, float H) CloseButtonRect() =>
        (PanelWidth - CloseButtonMargin - CloseButtonWidth, CloseButtonMargin, CloseButtonWidth, CloseButtonHeight);

    /// <summary>
    /// Hit-tests a click at screen coordinates. <paramref name="visibleSlotCount"/> is the
    /// number of slot rows actually rendered (min(total slots, VisibleRows)).
    /// <paramref name="isNewSaveActive"/> switches the New-Save row between its collapsed
    /// button and its expanded name-entry state.
    /// </summary>
    public static SaveHit HitTest(
        float screenX, float screenY, int screenWidth, int screenHeight,
        int visibleSlotCount, bool isNewSaveActive)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (isNewSaveActive)
        {
            if (IsInRect(lx, ly, ConfirmButtonRect()))
                return new SaveHit(SaveZone.ConfirmNewSave);
            if (IsInRect(lx, ly, CancelButtonRect()))
                return new SaveHit(SaveZone.CancelNewSave);
        }
        else
        {
            if (IsInRect(lx, ly, NewSaveButtonRect()))
                return new SaveHit(SaveZone.NewSave);
        }

        if (IsInRect(lx, ly, CloseButtonRect()))
            return new SaveHit(SaveZone.Close);

        for (int i = 0; i < visibleSlotCount; i++)
        {
            if (IsInRect(lx, ly, OverwriteButtonRect(i)))
                return new SaveHit(SaveZone.Overwrite, i);
            if (IsInRect(lx, ly, DeleteButtonRect(i)))
                return new SaveHit(SaveZone.Delete, i);
        }

        return SaveHit.None;
    }

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
