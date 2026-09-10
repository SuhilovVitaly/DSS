using DeepSpaceSaga.Client.UI.Portraits;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pixel-level checks for <see cref="PortraitComposer"/>: the docking-confirmation
/// portrait compositor (background + headless suited body + head portrait, cropped to
/// 250×250). Covers both sexes, the deterministic (non-<see cref="System.Random"/>)
/// body-variant pick keyed by personKey, and the missing-portrait-file failure path.
/// The dialog window that will consume this class is a separate batch, out of scope here.
/// </summary>
public class PortraitComposerTests
{
    private const string FemalePortraitPath = "Images/Persons/W/CHR-20260901-170239-JJD2U7.png";
    private const string MalePortraitPath = "Images/Persons/M/CHR-20260906-150900-IYUL3A.png";

    [Fact]
    public void Compose_female_returns_a_250x250_non_null_bitmap()
    {
        using var result = PortraitComposer.Compose(FemalePortraitPath, PersonSex.Female, "crew-1");

        Assert.NotNull(result);
        Assert.Equal(250, result!.Width);
        Assert.Equal(250, result.Height);
    }

    [Fact]
    public void Compose_male_returns_a_250x250_non_null_bitmap()
    {
        using var result = PortraitComposer.Compose(MalePortraitPath, PersonSex.Male, "crew-2");

        Assert.NotNull(result);
        Assert.Equal(250, result!.Width);
        Assert.Equal(250, result.Height);
    }

    [Fact]
    public void Compose_female_result_is_not_fully_transparent()
    {
        using var result = PortraitComposer.Compose(FemalePortraitPath, PersonSex.Female, "crew-1");

        Assert.NotNull(result);
        AssertHasVisibleCenterPixels(result!);
    }

    [Fact]
    public void Compose_male_result_is_not_fully_transparent()
    {
        using var result = PortraitComposer.Compose(MalePortraitPath, PersonSex.Male, "crew-2");

        Assert.NotNull(result);
        AssertHasVisibleCenterPixels(result!);
    }

    [Fact]
    public void Compose_same_personKey_picks_the_same_body_variant_deterministically()
    {
        using var first = PortraitComposer.Compose(FemalePortraitPath, PersonSex.Female, "same-key");
        using var second = PortraitComposer.Compose(FemalePortraitPath, PersonSex.Female, "same-key");

        Assert.NotNull(first);
        Assert.NotNull(second);
        AssertBitmapsAreIdentical(first!, second!);
    }

    [Fact]
    public void Compose_missing_portrait_file_returns_null_without_throwing()
    {
        var result = PortraitComposer.Compose("Images/Persons/W/does-not-exist.png", PersonSex.Female, "crew-1");

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
