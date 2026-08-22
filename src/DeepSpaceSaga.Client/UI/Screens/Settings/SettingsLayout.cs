namespace DeepSpaceSaga.Client.UI.Screens.Settings;

public enum SettingsButton
{
    None,
    MonitorCombo,
    UiScaleCombo,
    LanguageCombo,
    Exit
}

/// <summary>
/// Layout and hit-test geometry for the Settings overlay panel.
/// Same panel size and position as MainMenu (500×550).
///
/// Each setting is a single row: its label sits left-aligned at <see cref="RowLabelX"/>
/// and its combo box is right-aligned to <see cref="RowRightX"/> — the same right edge
/// for every row, so the boxes read as one aligned column of values next to their labels.
/// </summary>
public sealed class SettingsLayout
{
    public const float PanelWidth = 500f;
    public const float PanelHeight = 550f;

    public const float ButtonWidth = 188f;
    public const float ButtonHeight = 58f;

    public const float TitleY = 70f;
    /// <summary>Same local Y as MainMenu's EXIT button (<see cref="MainMenu.MenuLayout.ExitY"/>) — same panel size/position, so this lines the two buttons up exactly.</summary>
    public const float ExitY = 404f;

    /// <summary>Left inset, from the panel's left edge, for every row's label text.</summary>
    public const float RowLabelX = 40f;
    /// <summary>Right edge, from the panel's left edge, that every row's combo box aligns to.</summary>
    public const float RowRightX = 460f;

    public const float MonitorRowY = 140f;
    public const float MonitorComboWidth = 240f;
    public const float MonitorComboHeight = 40f;
    public const float MonitorOptionHeight = 36f;
    public const float MonitorNoteY = 196f;

    public const float UiScaleRowY = 320f;
    public const float UiScaleComboWidth = 240f;
    public const float UiScaleComboHeight = 40f;
    public const float UiScaleOptionHeight = 36f;

    public const float LanguageRowY = 230f;
    public const float LanguageComboWidth = 240f;
    public const float LanguageComboHeight = 40f;
    public const float LanguageOptionHeight = 36f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SettingsButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInMonitorCombo(lx, ly)) return SettingsButton.MonitorCombo;
        if (IsInUiScaleCombo(lx, ly)) return SettingsButton.UiScaleCombo;
        if (IsInLanguageCombo(lx, ly)) return SettingsButton.LanguageCombo;
        if (IsInButton(lx, ly, ExitY)) return SettingsButton.Exit;
        return SettingsButton.None;
    }

    /// <summary>
    /// Hit-tests the open monitor dropdown's option rows (rendered directly below
    /// the combo box). Returns the option index, or -1 if none was hit.
    /// </summary>
    public static int HitTestMonitorOption(
        float screenX, float screenY, int screenWidth, int screenHeight, int monitorCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        float bx = RowRightX - MonitorComboWidth;
        if (lx < bx || lx > bx + MonitorComboWidth)
            return -1;

        float listTop = MonitorRowY + MonitorComboHeight;
        for (int i = 0; i < monitorCount; i++)
        {
            float optionTop = listTop + i * MonitorOptionHeight;
            if (ly >= optionTop && ly <= optionTop + MonitorOptionHeight)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Hit-tests the open interface-scale dropdown's option rows (rendered directly
    /// below the combo box). Returns the option index, or -1 if none was hit.
    /// </summary>
    public static int HitTestUiScaleOption(
        float screenX, float screenY, int screenWidth, int screenHeight, int optionCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        float bx = RowRightX - UiScaleComboWidth;
        if (lx < bx || lx > bx + UiScaleComboWidth)
            return -1;

        float listTop = UiScaleRowY + UiScaleComboHeight;
        for (int i = 0; i < optionCount; i++)
        {
            float optionTop = listTop + i * UiScaleOptionHeight;
            if (ly >= optionTop && ly <= optionTop + UiScaleOptionHeight)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Hit-tests the open language dropdown's option rows (rendered directly below
    /// the combo box). Returns the option index, or -1 if none was hit.
    /// </summary>
    public static int HitTestLanguageOption(
        float screenX, float screenY, int screenWidth, int screenHeight, int optionCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        float bx = RowRightX - LanguageComboWidth;
        if (lx < bx || lx > bx + LanguageComboWidth)
            return -1;

        float listTop = LanguageRowY + LanguageComboHeight;
        for (int i = 0; i < optionCount; i++)
        {
            float optionTop = listTop + i * LanguageOptionHeight;
            if (ly >= optionTop && ly <= optionTop + LanguageOptionHeight)
                return i;
        }

        return -1;
    }

    private static bool IsInMonitorCombo(float localX, float localY)
    {
        float bx = RowRightX - MonitorComboWidth;
        return localX >= bx && localX <= bx + MonitorComboWidth
            && localY >= MonitorRowY && localY <= MonitorRowY + MonitorComboHeight;
    }

    private static bool IsInUiScaleCombo(float localX, float localY)
    {
        float bx = RowRightX - UiScaleComboWidth;
        return localX >= bx && localX <= bx + UiScaleComboWidth
            && localY >= UiScaleRowY && localY <= UiScaleRowY + UiScaleComboHeight;
    }

    private static bool IsInLanguageCombo(float localX, float localY)
    {
        float bx = RowRightX - LanguageComboWidth;
        return localX >= bx && localX <= bx + LanguageComboWidth
            && localY >= LanguageRowY && localY <= LanguageRowY + LanguageComboHeight;
    }

    private static bool IsInButton(float localX, float localY, float buttonY)
    {
        float bx = (PanelWidth - ButtonWidth) / 2f;
        return localX >= bx && localX <= bx + ButtonWidth
            && localY >= buttonY && localY <= buttonY + ButtonHeight;
    }
}
