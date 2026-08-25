using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

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
/// Each setting uses the native 355px Xenon Star drop-down width. Its label strip sits
/// directly above the closed field, matching the source Options UI slices.
/// </summary>
public sealed class SettingsLayout
{
    public const float PanelWidth = 500f;
    public const float PanelHeight = 550f;

    public const float ButtonWidth = 384f;
    public const float ButtonHeight = 56f;

    /// <summary>Same local Y as MainMenu's EXIT button.</summary>
    public const float ExitY = 396f;

    /// <summary>Right edge, from the panel's left edge, that every row's combo box aligns to.</summary>
    public const float RowRightX = (PanelWidth + XenonComboBox.NativeWidth) / 2f;

    public const float MonitorRowY = 124f;
    public const float MonitorComboWidth = XenonComboBox.NativeWidth;
    public const float MonitorComboHeight = XenonComboBox.FieldHeight;
    public const float MonitorOptionHeight = XenonComboBox.OptionHeight;
    public const float MonitorNoteY = 181f;

    public const float UiScaleRowY = 312f;
    public const float UiScaleComboWidth = XenonComboBox.NativeWidth;
    public const float UiScaleComboHeight = XenonComboBox.FieldHeight;
    public const float UiScaleOptionHeight = XenonComboBox.OptionHeight;

    public const float LanguageRowY = 229f;
    public const float LanguageComboWidth = XenonComboBox.NativeWidth;
    public const float LanguageComboHeight = XenonComboBox.FieldHeight;
    public const float LanguageOptionHeight = XenonComboBox.OptionHeight;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SKRect PanelRect(int screenWidth, int screenHeight)
    {
        float left = PanelLeft(screenWidth);
        float top = PanelTop(screenHeight);
        return new SKRect(left, top, left + PanelWidth, top + PanelHeight);
    }

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
