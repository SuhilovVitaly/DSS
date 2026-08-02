namespace DeepSpaceSaga.Client.UI.Screens.MainMenu;

public enum MenuButton
{
    None,
    NewGame,
    Load,
    Exit
}

/// <summary>
/// Layout and hit-test geometry for the MainMenu panel.
/// All dimensions in logical (window) coordinates.
/// </summary>
public sealed class MenuLayout
{
    public const float PanelWidth = 500f;
    public const float PanelHeight = 550f;

    public const float ButtonWidth = 188f;
    public const float ButtonHeight = 58f;

    public const float TitleY = 70f;
    public const float VersionY = 110f;
    public const float NewGameY = 150f;
    public const float LoadY = 224f;
    public const float StatusY = 296f;
    public const float ExitY = 404f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static MenuButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInButton(lx, ly, NewGameY)) return MenuButton.NewGame;
        if (IsInButton(lx, ly, LoadY)) return MenuButton.Load;
        if (IsInButton(lx, ly, ExitY)) return MenuButton.Exit;
        return MenuButton.None;
    }

    private static bool IsInButton(float localX, float localY, float buttonY)
    {
        float bx = (PanelWidth - ButtonWidth) / 2f;
        return localX >= bx && localX <= bx + ButtonWidth
            && localY >= buttonY && localY <= buttonY + ButtonHeight;
    }
}
