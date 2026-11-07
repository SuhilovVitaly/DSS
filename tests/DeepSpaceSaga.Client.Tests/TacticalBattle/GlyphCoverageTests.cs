using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class GlyphCoverageTests
{
    private const string Symbols = "−→·±×°≤…—";
    private static SKTypeface[] Faces => [MenuStyle.TypefaceRegular, MenuStyle.TypefaceBold, XenonStyle.TypefaceRegular, XenonStyle.TypefaceSemibold];

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void DisplayNumbersAreInvariantBankersRoundedAndNormalizeNegativeZero(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal("0", BattleFormat.Number(0.5));
            Assert.Equal("2", BattleFormat.Number(1.5));
            Assert.Equal("2", BattleFormat.Number(2.5));
            Assert.Equal("4", BattleFormat.Number(3.5));
            Assert.Equal(BattleFormat.Text("−2"), BattleFormat.Number(-2.5));
            Assert.Equal("1.2", BattleFormat.Number(1.25, 1));
            Assert.Equal("1.8", BattleFormat.Number(1.75, 1));
            Assert.Equal("1.50", BattleFormat.Number(1.5, 2));
            Assert.Equal("0", BattleFormat.Number(-0.0));
            Assert.Equal("0", BattleFormat.Signed(-0.125));
            Assert.Equal("0", BattleFormat.Signed(0.125));
            Assert.Equal("+1.2", BattleFormat.Signed(1.25, 1));
            Assert.Equal("95%", BattleFormat.Percent(95));
            Assert.Equal("12" + BattleFormat.Text("°"), BattleFormat.Degrees(12.5));
            Assert.Equal("9223372036854775807", BattleFormat.Seed(long.MaxValue));
            Assert.Equal(BattleFormat.Text("−9223372036854775808"), BattleFormat.Seed(long.MinValue));
            foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.Equal(BattleFormat.Text("—"), BattleFormat.Number(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleFormat.Number(1, 16));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void EveryRequiredSymbolOrItsDisplaySubstituteIsSupportedByAllActualControlFaces()
    {
        string safe = BattleFormat.Text(Symbols);
        Assert.Equal(safe, BattleFormat.Text(safe));
        foreach (var face in Faces)
        {
            using var paint = new SKPaint { Typeface = face };
            var glyphs = paint.GetGlyphs(safe + " Точность броня корпус 0123456789");
            Assert.NotEmpty(glyphs);
            Assert.All(glyphs, glyph => Assert.NotEqual((ushort)0, glyph));
            // Seed entry uses the existing text box, whose accepted Unicode minus must itself render.
            Assert.All(paint.GetGlyphs("−0123456789"), glyph => Assert.NotEqual((ushort)0, glyph));
        }
        var panel = new ScrollTextPanel(); panel.Layout(new(0, 0, 600, 100)); panel.SetText(Symbols);
        Assert.Equal(Symbols, panel.Text); // authoritative/source prose stays unchanged
        Assert.Equal(safe, Assert.Single(panel.Lines));
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { } root)
        {
            var evidence = Faces.Select(face =>
            {
                using var paint = new SKPaint { Typeface = face };
                return new { face.FamilyName, Original = Symbols.Select(c => new { Symbol = c.ToString(), Glyphs = paint.GetGlyphs(c.ToString()) }).ToArray(), Display = safe, DisplayGlyphs = paint.GetGlyphs(safe) };
            }).ToArray();
            File.WriteAllText(Path.Combine(root, "glyph-coverage.json"), JsonSerializer.Serialize(new { Os = RuntimeInformation.OSDescription, Faces = evidence }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    [Fact]
    public void RasterUsesActualFacesAndResolvedSymbolsWithoutMissingGlyphBoxes()
    {
        using var bitmap = new SKBitmap(1280, 500); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        int row = 0;
        foreach (var face in Faces)
        {
            using var paint = new SKPaint { Typeface = face, TextSize = 28, IsAntialias = true, Color = SKColors.White };
            canvas.DrawText(face.FamilyName + ": " + BattleFormat.Text("−5 → цель · ±2 × 3 ° ≤ 4 …"), 20, 60 + row++ * 100, paint);
        }
        Assert.NotEqual(SKColors.Black, bitmap.GetPixel(25, 50));
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { } root)
        {
            using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(root, "glyphs.png")); data.SaveTo(file);
        }
    }
}
