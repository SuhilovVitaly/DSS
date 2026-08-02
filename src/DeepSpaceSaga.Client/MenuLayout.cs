namespace DeepSpaceSaga.Client;

public enum MenuButton
{
    None,
    NewGame,
    Load,
    Exit
}

public sealed class MenuLayout
{
    public const float PanelWidth = 480f;
    public const float PanelHeight = 345f;

    // Panel is centered on screen; all button rects are relative to panel top-left.
    public const float PanelOriginX = 0f;
    public const float PanelOriginY = 0f;

    // Title area
    public const float TitleY = 36f;
    public const float VersionY = 68f;

    // Button dimensions
    public const float ButtonWidth = 260f;
    public const float ButtonHeight = 34f;
    public const float ButtonCornerRadius = 2f;

    // Button Y positions (from top of panel)
    public const float NewGameButtonY = 108f;
    public const float LoadButtonY = 154f;
    public const float ExitButtonY = 270f;

    // Status text
    public const float StatusTextY = 218f;

    // Button X (centered in panel)
    public static float ButtonLeft => (PanelWidth - ButtonWidth) / 2f;

    // Font sizes
    public const float TitleFontSize = 30f;
    public const float VersionFontSize = 14f;
    public const float ButtonFontSize = 15f;
    public const float StatusFontSize = 12f;

    // Colors
    public static readonly ColorDesc PanelBorderColor = new(60, 60, 60);
    public static readonly ColorDesc PanelFillColor = new(0, 0, 0);
    public static readonly ColorDesc TitleColor = new(220, 220, 220);
    public static readonly ColorDesc VersionColor = new(140, 140, 140);
    public static readonly ColorDesc ButtonTextColor = new(180, 180, 180);
    public static readonly ColorDesc ButtonDisabledTextColor = new(80, 80, 80);
    public static readonly ColorDesc ButtonBorderColor = new(80, 80, 80);
    public static readonly ColorDesc ButtonDisabledBorderColor = new(50, 50, 50);
    public static readonly ColorDesc StatusTextColor = new(100, 100, 100);
    public const float ButtonBorderWidth = 1f;
    public const float PanelBorderWidth = 1f;

    /// <summary>
    /// Returns which menu button is at the given screen-space coordinate,
    /// or MenuButton.None if no button was hit.
    /// (x, y) are in screen framebuffer space.
    /// </summary>
    public static MenuButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = (screenWidth - PanelWidth) / 2f;
        float panelTop = (screenHeight - PanelHeight) / 2f;

        float localX = screenX - panelLeft;
        float localY = screenY - panelTop;

        return HitTestPanelLocal(localX, localY);
    }

    /// <summary>
    /// Hit-test using coordinates relative to panel top-left corner.
    /// </summary>
    public static MenuButton HitTestPanelLocal(float x, float y)
    {
        if (IsInButton(x, y, NewGameButtonY))
            return MenuButton.NewGame;

        if (IsInButton(x, y, LoadButtonY))
            return MenuButton.Load;

        if (IsInButton(x, y, ExitButtonY))
            return MenuButton.Exit;

        return MenuButton.None;
    }

    private static bool IsInButton(float x, float y, float buttonY)
    {
        return x >= ButtonLeft && x <= ButtonLeft + ButtonWidth
            && y >= buttonY && y <= buttonY + ButtonHeight;
    }
}

public readonly record struct ColorDesc(byte R, byte G, byte B);
