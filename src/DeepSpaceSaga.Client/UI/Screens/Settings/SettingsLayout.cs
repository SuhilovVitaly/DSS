namespace DeepSpaceSaga.Client.UI.Screens.Settings;

public enum SettingsButton
{
    None,
    MonitorCombo,
    InterfaceScaleCombo,
    Exit
}

/// <summary>
/// Layout and hit-test geometry for the Settings overlay panel.
/// Same panel size and position as MainMenu (500×550).
/// </summary>
public sealed class SettingsLayout
{
    public const float PanelWidth = 500f;
    public const float PanelHeight = 550f;

    public const float ButtonWidth = 188f;
    public const float ButtonHeight = 58f;

    public const float TitleY = 70f;
    public const float ExitY = 470f;

    public const float MonitorLabelY = 140f;
    public const float MonitorComboY = 162f;
    public const float MonitorComboWidth = 320f;
    public const float MonitorComboHeight = 40f;
    public const float MonitorOptionHeight = 36f;
    public const float MonitorNoteY = 218f;

    public const float InterfaceScaleLabelY = 270f;
    public const float InterfaceScaleComboY = 292f;
    public const float InterfaceScaleComboWidth = 320f;
    public const float InterfaceScaleComboHeight = 40f;
    public const float InterfaceScaleOptionHeight = 36f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SettingsButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInMonitorCombo(lx, ly)) return SettingsButton.MonitorCombo;
        if (IsInInterfaceScaleCombo(lx, ly)) return SettingsButton.InterfaceScaleCombo;
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

        float bx = (PanelWidth - MonitorComboWidth) / 2f;
        if (lx < bx || lx > bx + MonitorComboWidth)
            return -1;

        float listTop = MonitorComboY + MonitorComboHeight;
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
    public static int HitTestInterfaceScaleOption(
        float screenX, float screenY, int screenWidth, int screenHeight, int optionCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        float bx = (PanelWidth - InterfaceScaleComboWidth) / 2f;
        if (lx < bx || lx > bx + InterfaceScaleComboWidth)
            return -1;

        float listTop = InterfaceScaleComboY + InterfaceScaleComboHeight;
        for (int i = 0; i < optionCount; i++)
        {
            float optionTop = listTop + i * InterfaceScaleOptionHeight;
            if (ly >= optionTop && ly <= optionTop + InterfaceScaleOptionHeight)
                return i;
        }

        return -1;
    }

    private static bool IsInMonitorCombo(float localX, float localY)
    {
        float bx = (PanelWidth - MonitorComboWidth) / 2f;
        return localX >= bx && localX <= bx + MonitorComboWidth
            && localY >= MonitorComboY && localY <= MonitorComboY + MonitorComboHeight;
    }

    private static bool IsInInterfaceScaleCombo(float localX, float localY)
    {
        float bx = (PanelWidth - InterfaceScaleComboWidth) / 2f;
        return localX >= bx && localX <= bx + InterfaceScaleComboWidth
            && localY >= InterfaceScaleComboY && localY <= InterfaceScaleComboY + InterfaceScaleComboHeight;
    }

    private static bool IsInButton(float localX, float localY, float buttonY)
    {
        float bx = (PanelWidth - ButtonWidth) / 2f;
        return localX >= bx && localX <= bx + ButtonWidth
            && localY >= buttonY && localY <= buttonY + ButtonHeight;
    }
}
