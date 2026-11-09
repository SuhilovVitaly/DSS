using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>File-backed battle typography; all measurements use the same settings as painting.</summary>
internal static class BattleTypography
{
    public static SKTypeface Regular { get; }
    public static SKTypeface Bold { get; }
    public static bool IsBundled { get; }

    static BattleTypography()
    {
        SKTypeface? regular = null;
        SKTypeface? bold = null;
        try
        {
            regular = Load("DejaVuSans.ttf");
            bold = Load("DejaVuSans-Bold.ttf");
            Regular = regular;
            Bold = bold;
            IsBundled = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            regular?.Dispose();
            bold?.Dispose();
            InterfaceLog.Write($"TACTICAL TYPOGRAPHY: bundled DejaVu Sans unavailable: {ex.Message}");
            Regular = MenuStyle.TypefaceRegular;
            Bold = MenuStyle.TypefaceBold;
        }
    }

    private static SKTypeface Load(string file)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "UI", "Fonts", file);
        if (!File.Exists(path)) throw new FileNotFoundException("Battle font is missing", path);
        return SKTypeface.FromFile(path) ?? throw new InvalidDataException($"Invalid battle font: {file}");
    }

    public static SKPaint TextPaint(float size, bool bold) => new()
    {
        Typeface = bold ? Bold : Regular,
        TextSize = size,
        IsAntialias = true,
        SubpixelText = true,
        LcdRenderText = false,
        HintingLevel = SKPaintHinting.NoHinting,
        IsAutohinted = false,
        IsLinearText = true
    };
}
