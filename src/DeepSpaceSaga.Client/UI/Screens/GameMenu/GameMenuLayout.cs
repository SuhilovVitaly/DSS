namespace DeepSpaceSaga.Client.UI.Screens.GameMenu;

public enum GameMenuButton
{
    None,
    Resume,
    Save,
    Load,
    Settings,
    MainMenu
}

/// <summary>
/// Layout and hit-test geometry for the GameMenu overlay panel.
/// Same panel size as MainMenu (500×550).
/// </summary>
public sealed class GameMenuLayout
{
    public const float PanelWidth = 500f;
    public const float PanelHeight = 550f;

    public const float ButtonWidth = 188f;
    public const float ButtonHeight = 58f;

    public const float ResumeY = 140f;
    public const float SaveY = 212f;
    public const float LoadY = 284f;
    public const float SettingsY = 356f;
    public const float MainMenuY = 470f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static GameMenuButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInButton(lx, ly, ResumeY)) return GameMenuButton.Resume;
        if (IsInButton(lx, ly, SaveY)) return GameMenuButton.Save;
        if (IsInButton(lx, ly, LoadY)) return GameMenuButton.Load;
        if (IsInButton(lx, ly, SettingsY)) return GameMenuButton.Settings;
        if (IsInButton(lx, ly, MainMenuY)) return GameMenuButton.MainMenu;
        return GameMenuButton.None;
    }

    private static bool IsInButton(float localX, float localY, float buttonY)
    {
        float bx = (PanelWidth - ButtonWidth) / 2f;
        return localX >= bx && localX <= bx + ButtonWidth
            && localY >= buttonY && localY <= buttonY + ButtonHeight;
    }
}
