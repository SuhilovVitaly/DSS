namespace DeepSpaceSaga.Client.UI.Screens;

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
/// Visual style (colors, fonts) lives in MenuStyle — this class is pure geometry.
/// </summary>
public sealed class MenuLayout
{
    // --- Panel ---
    public const float PanelWidth = 500f;
    public const float PanelHeight = 480f;

    // --- Button geometry (single source of truth) ---
    public const float ButtonWidth = 188f;
    public const float ButtonHeight = 58f;

    // --- Vertical positions (relative to panel top) ---
    public const float TitleY = 40f;
    public const float VersionY = 80f;
    public const float NewGameY = 120f;
    public const float LoadY = 194f;
    public const float StatusY = 266f;
    public const float ExitY = 374f;

    /// <summary>Panel left edge, centered in screen.</summary>
    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;

    /// <summary>Panel top edge, centered in screen.</summary>
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    /// <summary>Hit-test: which menu button is at screen-space (x, y)?</summary>
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
