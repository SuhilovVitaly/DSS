namespace DeepSpaceSaga.Client.UI.Screens;

public enum MenuButton
{
    None,
    NewGame,
    Load,
    Exit
}

/// <summary>
/// Layout constants matching the reference WinForms MainMenu design.
/// A centered panel (800×660) with a border contains all menu elements.
/// Y positions match reference: title at 50, NEW GAME 175, LOAD 254, EXIT 565.
/// </summary>
public sealed class MenuLayout
{
    // --- Panel ---
    public const float PanelWidth = 800f;
    public const float PanelHeight = 660f;
    public const float PanelBorderWidth = 2f;

    // --- Button dimensions (reference: 188×58) ---
    public const float ButtonWidth = 188f;
    public const float ButtonHeight = 58f;
    public const float ButtonCornerRadius = 0f;

    // --- Font sizes (pt converted to px at 96dpi) ---
    public const float TitleFontSize = 32f;       // 24pt Bold
    public const float VersionFontSize = 13f;     // 10pt Regular
    public const float ButtonFontSize = 14f;      // 10.8pt Bold
    public const float StatusFontSize = 11f;      // 8pt Italic

    // --- Vertical positions (relative to panel top, matching reference) ---
    public const float TitleY = 50f;
    public const float TitleToVersionGap = 10f;
    public const float VersionToNewGameGap = 40f;
    public const float NewGameY = 175f;
    public const float LoadY = 254f;
    public const float ButtonGapClose = 21f; // 254 - 175 - 58
    public const float ExitY = 561f;
    public const float StatusY = 330f;       // between LOAD and EXIT
    public const float BigGap = 249f;        // 561 - 254 - 58

    /// <summary>Panel left edge, centered in screen.</summary>
    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;

    /// <summary>Panel top edge, centered in screen.</summary>
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    /// <summary>Button X, centered in panel.</summary>
    public static float ButtonLeft(int screenWidth) => PanelLeft(screenWidth) + (PanelWidth - ButtonWidth) / 2f;

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
