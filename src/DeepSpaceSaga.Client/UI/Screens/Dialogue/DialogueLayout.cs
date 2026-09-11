using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Dialogue;

public static class DialogueLayout
{
    public const float PanelWidth = 1400, PanelHeight = 800;
    public const int VisibleChoices = 3;
    public static float Left(int width) => (width - PanelWidth) / 2;
    public static float Top(int height) => (height - PanelHeight) / 2;
    public static SKRect Panel(int width, int height) => new(Left(width), Top(height), Left(width) + PanelWidth, Top(height) + PanelHeight);
    public static SKRect Choice(int index, int width, int height) =>
        new(Left(width) + 410, Top(height) + 470 + index * 75, Left(width) + 990, Top(height) + 528 + index * 75);
    public static SKRect Cancel(int width, int height) =>
        new(Left(width) + 510, Top(height) + 725, Left(width) + 890, Top(height) + 765);
    public static SKRect Operator(int width, int height) => new(Left(width) + 70, Top(height) + 170, Left(width) + 370, Top(height) + 470);
    public static SKRect Captain(int width, int height) => new(Left(width) + 1030, Top(height) + 170, Left(width) + 1330, Top(height) + 470);
}
