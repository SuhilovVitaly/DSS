using DeepSpaceSaga.Client.UI.Portraits;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pixel-level checks for <see cref="PortraitComposer"/>: the docking-confirmation
/// portrait compositor (background + headless suited body + head portrait, cropped to
/// 250×250). Covers both sexes, the deterministic (non-<see cref="System.Random"/>)
/// repeatability, and the missing-portrait-file failure path. Input portraits are
/// temporary test images, independent of retired game artwork.
/// The dialog window that will consume this class is a separate batch, out of scope here.
/// </summary>
public class PortraitComposerTests : IDisposable
{
    private readonly string _portraitPath = Path.Combine(Path.GetTempPath(), $"dss-composer-{Guid.NewGuid():N}.png");

    public PortraitComposerTests()
    {
        using var bitmap = new SKBitmap(235, 235);
        bitmap.Erase(new SKColor(180, 120, 100));
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(_portraitPath);
        png.SaveTo(file);
    }

    public void Dispose() => File.Delete(_portraitPath);

    [Fact]
    public void Compose_female_returns_a_250x250_non_null_bitmap()
    {
        using var result = PortraitComposer.Compose(_portraitPath, PersonSex.Female, "crew-1");

        Assert.NotNull(result);
        Assert.Equal(250, result!.Width);
        Assert.Equal(250, result.Height);
    }

    [Fact]
    public void Compose_male_returns_a_250x250_non_null_bitmap()
    {
        using var result = PortraitComposer.Compose(_portraitPath, PersonSex.Male, "crew-2");

        Assert.NotNull(result);
        Assert.Equal(250, result!.Width);
        Assert.Equal(250, result.Height);
    }

    [Fact]
    public void Compose_female_result_is_not_fully_transparent()
    {
        using var result = PortraitComposer.Compose(_portraitPath, PersonSex.Female, "crew-1");

        Assert.NotNull(result);
        AssertHasVisibleCenterPixels(result!);
    }

    [Fact]
    public void Compose_male_result_is_not_fully_transparent()
    {
        using var result = PortraitComposer.Compose(_portraitPath, PersonSex.Male, "crew-2");

        Assert.NotNull(result);
        AssertHasVisibleCenterPixels(result!);
    }

    [Fact]
    public void Compose_same_input_and_personKey_produce_identical_pixels()
    {
        using var first = PortraitComposer.Compose(_portraitPath, PersonSex.Female, "same-key");
        using var second = PortraitComposer.Compose(_portraitPath, PersonSex.Female, "same-key");

        Assert.NotNull(first);
        Assert.NotNull(second);
        AssertBitmapsAreIdentical(first!, second!);
    }

    [Fact]
    public void Compose_missing_portrait_file_returns_null_without_throwing()
    {
        using var result = PortraitComposer.Compose(_portraitPath + ".missing", PersonSex.Female, "crew-1");

        Assert.Null(result);
    }

    private static void AssertHasVisibleCenterPixels(SKBitmap bitmap)
    {
        bool anyVisible = false;
        for (int x = bitmap.Width / 2 - 10; x <= bitmap.Width / 2 + 10 && !anyVisible; x++)
        {
            for (int y = bitmap.Height / 2 - 10; y <= bitmap.Height / 2 + 10; y++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    anyVisible = true;
                    break;
                }
            }
        }

        Assert.True(anyVisible, "Expected at least one non-transparent pixel near the center of the composed portrait.");
    }

    private static void AssertBitmapsAreIdentical(SKBitmap a, SKBitmap b)
    {
        Assert.Equal(a.Width, b.Width);
        Assert.Equal(a.Height, b.Height);

        for (int x = 0; x < a.Width; x++)
        {
            for (int y = 0; y < a.Height; y++)
            {
                Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y));
            }
        }
    }
}
