using System.Security.Cryptography;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleTypographyTests
{
    [Fact]
    public void Bundled_dejavu_fonts_load_from_output()
    {
        Assert.True(BattleTypography.IsBundled);
        Assert.Equal("DejaVu Sans", BattleTypography.Regular.FamilyName);
        Assert.Equal("DejaVu Sans", BattleTypography.Bold.FamilyName);
        AssertFontHash("DejaVuSans.ttf", "690243adfefe0ce154b547db6205794bd30ac4277275179517a90994f4980648");
        AssertFontHash("DejaVuSans-Bold.ttf", "d1c3ff99f1e1ce1827a33efd4dad81f40babda06bff9e43bd7591c86662a287b");
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void Battle_strings_have_glyphs_in_bundled_font(string locale)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Data", "Locale", locale + ".json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        string[] strings = document.RootElement.EnumerateObject()
            .Where(p => p.Name.StartsWith("TacticalBattle.", StringComparison.Ordinal))
            .Select(p => p.Value.GetString()!).Append("→ · – − ° %").ToArray();
        Assert.True(strings.Length > 100);
        foreach (bool bold in new[] { false, true })
        {
            using var paint = BattleTypography.TextPaint(15, bold);
            foreach (string value in strings)
                Assert.All(paint.GetGlyphs(value.Replace("\n", "").Replace("\r", "").Replace("\t", "")),
                    glyph => Assert.NotEqual((ushort)0, glyph));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Text_paint_uses_platform_neutral_settings(bool bold)
    {
        using var paint = BattleTypography.TextPaint(15, bold);
        Assert.Same(bold ? BattleTypography.Bold : BattleTypography.Regular, paint.Typeface);
        Assert.Equal(15, paint.TextSize);
        Assert.True(paint.IsAntialias);
        Assert.True(paint.SubpixelText);
        Assert.False(paint.LcdRenderText);
        Assert.Equal(SKPaintHinting.NoHinting, paint.HintingLevel);
        Assert.False(paint.IsAutohinted);
        Assert.True(paint.IsLinearText);
    }

    [Theory]
    [InlineData(false, 185.9033)]
    [InlineData(true, 205.7153)]
    public void Text_width_matches_reference_metrics(bool bold, double expected)
    {
        using var paint = BattleTypography.TextPaint(15, bold);
        Assert.InRange(paint.MeasureText("Выполнить залп [Enter]"), expected - 0.01, expected + 0.01);
    }

    private static void AssertFontHash(string file, string expected)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "UI", "Fonts", file);
        Assert.Equal(expected, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant());
    }
}
