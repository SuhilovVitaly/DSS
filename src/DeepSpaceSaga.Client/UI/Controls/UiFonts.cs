using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>Process-owned, file-backed fonts. Consumers must not dispose shared typefaces.</summary>
public static class UiFonts
{
    private static readonly Lazy<SKTypeface> UiRegular = Font("Inter-Regular.ttf");
    private static readonly Lazy<SKTypeface> UiStrong = Font("Inter-Bold.ttf");
    private static readonly Lazy<SKTypeface> Monospace = Font("DSSMono-Regular.ttf");
    private static readonly Lazy<SKTypeface> BodyRegular = Font("NotoSans-Regular.ttf");
    private static readonly Lazy<SKTypeface> BodyStrong = Font("NotoSans-Bold.ttf");
    private static readonly Lazy<SKTypeface> BodySlanted = Font("NotoSans-Italic.ttf");

    public static SKTypeface Ui => UiRegular.Value;
    public static SKTypeface UiBold => UiStrong.Value;
    public static SKTypeface Mono => Monospace.Value;
    public static SKTypeface Body => BodyRegular.Value;
    public static SKTypeface BodyBold => BodyStrong.Value;
    public static SKTypeface BodyItalic => BodySlanted.Value;
    public static SKTypeface BodyWeight(int weight) => weight >= 600 ? BodyBold : Body;

    private static Lazy<SKTypeface> Font(string name) =>
        new(() => LoadFile(Path.Combine(AppContext.BaseDirectory, "UI", "Fonts", name)));

    internal static SKTypeface LoadFile(string path, Action<string>? warning = null)
    {
        string reason;
        try
        {
            if (File.Exists(path))
            {
                var typeface = SKTypeface.FromFile(path);
                if (typeface is not null) return typeface;
                reason = "invalid font file";
            }
            else reason = "file not found";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        { reason = error.Message; }

        try { (warning ?? InterfaceLog.Write)($"Bundled font unavailable: {path}: {reason}; using default typeface."); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return SKTypeface.Default;
    }
}
