using DeepSpaceSaga.Client.UI.Assets;
using DeepSpaceSaga.Client.UI.Controls;

namespace DeepSpaceSaga.Client.Tests;

public class UiAssetLoaderTests
{
    public static TheoryData<string> ProductionAssets => new()
    {
        XenonWindowChrome.ShellPath,
        XenonWindowChrome.ClosePath,
        XenonWindowChrome.CloseActivePath,
        XenonMenuButton.NormalPath,
        XenonMenuButton.HoverPath,
        XenonMenuButton.MarkerPath,
        XenonMenuButton.MarkerActivePath,
        XenonMenuButton.MarkerDisabledPath
    };

    [Theory]
    [MemberData(nameof(ProductionAssets))]
    public void Production_png_loads_from_test_output(string path)
    {
        var bitmap = UiAssetLoader.LoadBitmap(path);
        Assert.NotNull(bitmap);
        Assert.True(bitmap!.Width > 0 && bitmap.Height > 0);
    }

    [Fact]
    public void Forward_and_back_slashes_share_the_cached_bitmap()
    {
        var forward = UiAssetLoader.LoadBitmap(XenonMenuButton.NormalPath);
        var backward = UiAssetLoader.LoadBitmap(XenonMenuButton.NormalPath.Replace('/', '\\'));
        Assert.Same(forward, backward);
    }

    [Fact]
    public void Missing_corrupt_and_traversal_paths_return_null()
    {
        var directory = Directory.CreateTempSubdirectory("dss-ui-assets-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "broken.png"), "not a png");
            Assert.Null(UiAssetLoader.LoadBitmapFrom("missing.png", directory.FullName));
            Assert.Null(UiAssetLoader.LoadBitmapFrom("broken.png", directory.FullName));
            Assert.Null(UiAssetLoader.LoadBitmapFrom("../outside.png", directory.FullName));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
