namespace DeepSpaceSaga.Client.UI.Screens.Settings;

public enum SettingsButton
{
    None,
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

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SettingsButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInButton(lx, ly, ExitY)) return SettingsButton.Exit;
        return SettingsButton.None;
    }

    private static bool IsInButton(float localX, float localY, float buttonY)
    {
        float bx = (PanelWidth - ButtonWidth) / 2f;
        return localX >= bx && localX <= bx + ButtonWidth
            && localY >= buttonY && localY <= buttonY + ButtonHeight;
    }
}
