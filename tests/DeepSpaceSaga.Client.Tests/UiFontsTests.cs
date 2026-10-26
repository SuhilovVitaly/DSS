using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class UiFontsTests
{
    [Fact]
    public void Bundled_roles_are_cached_and_cover_english_and_russian()
    {
        Assert.Contains("Inter", UiFonts.Ui.FamilyName);
        Assert.Contains("Inter", UiFonts.UiBold.FamilyName);
        Assert.Contains("DSS Mono", UiFonts.Mono.FamilyName);
        Assert.Contains("Noto Sans", UiFonts.Body.FamilyName);
        Assert.Same(UiFonts.Ui, UiFonts.Ui);
        Assert.Same(UiFonts.Body, UiFonts.BodyWeight(400));
        Assert.Same(UiFonts.BodyBold, UiFonts.BodyWeight(600));
        foreach (var typeface in new[] { UiFonts.Ui, UiFonts.UiBold, UiFonts.Mono, UiFonts.Body, UiFonts.BodyBold, UiFonts.BodyItalic })
        {
            using var paint = new SKPaint { Typeface = typeface };
            Assert.All(paint.GetGlyphs("Hello Привет 0123"), glyph => Assert.NotEqual((ushort)0, glyph));
        }
    }

    [Fact]
    public void Missing_file_reports_path_and_uses_default_without_throwing()
    {
        var warnings = new List<string>();
        string path = Path.Combine(AppContext.BaseDirectory, "UI", "Fonts", "missing-" + Guid.NewGuid() + ".ttf");
        Assert.Same(SKTypeface.Default, UiFonts.LoadFile(path, warnings.Add));
        Assert.Contains(path, Assert.Single(warnings));
    }

    [Fact]
    public void Closing_panels_does_not_dispose_shared_faces()
    {
        using (var panel = new DebugTuningPanel()) { }
        using var monoPaint = new SKPaint { Typeface = UiFonts.Mono };
        using var italicPaint = new SKPaint { Typeface = UiFonts.BodyItalic };
        Assert.All(monoPaint.GetGlyphs("Броня █░▶♦"), glyph => Assert.NotEqual((ushort)0, glyph));
        Assert.All(italicPaint.GetGlyphs("Привет"), glyph => Assert.NotEqual((ushort)0, glyph));
    }
}
