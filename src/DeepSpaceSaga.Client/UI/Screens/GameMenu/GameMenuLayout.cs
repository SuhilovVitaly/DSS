using SkiaSharp;

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

/// <summary>All GameMenu geometry and hit testing for the centered 500×550 panel.</summary>
public static class GameMenuLayout
{
    public const float PanelWidth = 500f;
    public const float PanelHeight = 550f;
    public const float InnerPadding = 58f;

    public const float ButtonWidth = 384f;
    public const float ButtonHeight = 56f;
    public const float ResumeY = 132f;
    public const float SaveY = 198f;
    public const float LoadY = 264f;
    public const float SettingsY = 330f;
    public const float MainMenuY = 396f;

    public const float HeaderY = 24f;
    public const float HeaderHeight = 76f;
    public const float FooterY = 476f;
    public const float FooterHeight = 56f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SKRect PanelRect(int screenWidth, int screenHeight) =>
        OffsetRect(0, 0, PanelWidth, PanelHeight, screenWidth, screenHeight);

    public static SKRect HeaderRect(int screenWidth, int screenHeight) =>
        OffsetRect(InnerPadding, HeaderY, ButtonWidth, HeaderHeight, screenWidth, screenHeight);

    public static SKRect FooterRect(int screenWidth, int screenHeight) =>
        OffsetRect(InnerPadding, FooterY, ButtonWidth, FooterHeight, screenWidth, screenHeight);

    public static SKRect ButtonRect(GameMenuButton id, int screenWidth, int screenHeight)
    {
        float y = id switch
        {
            GameMenuButton.Resume => ResumeY,
            GameMenuButton.Save => SaveY,
            GameMenuButton.Load => LoadY,
            GameMenuButton.Settings => SettingsY,
            GameMenuButton.MainMenu => MainMenuY,
            _ => -ButtonHeight
        };
        return OffsetRect(InnerPadding, y, ButtonWidth, ButtonHeight, screenWidth, screenHeight);
    }

    public static GameMenuButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        if (ButtonRect(GameMenuButton.Resume, screenWidth, screenHeight).Contains(screenX, screenY)) return GameMenuButton.Resume;
        if (ButtonRect(GameMenuButton.Save, screenWidth, screenHeight).Contains(screenX, screenY)) return GameMenuButton.Save;
        if (ButtonRect(GameMenuButton.Load, screenWidth, screenHeight).Contains(screenX, screenY)) return GameMenuButton.Load;
        if (ButtonRect(GameMenuButton.Settings, screenWidth, screenHeight).Contains(screenX, screenY)) return GameMenuButton.Settings;
        if (ButtonRect(GameMenuButton.MainMenu, screenWidth, screenHeight).Contains(screenX, screenY)) return GameMenuButton.MainMenu;
        return GameMenuButton.None;
    }

    private static SKRect OffsetRect(float x, float y, float width, float height,
        int screenWidth, int screenHeight)
    {
        float left = PanelLeft(screenWidth) + x;
        float top = PanelTop(screenHeight) + y;
        return new SKRect(left, top, left + width, top + height);
    }
}
