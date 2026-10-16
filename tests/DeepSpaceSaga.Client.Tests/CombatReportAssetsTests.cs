using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportAssetsTests
{
    [Fact]
    public void AllEightVariantsExistInBuildOutput()
    {
        using var assets = new CombatReportAssets();
        assets.Prepare([]);
        Assert.Equal(8, assets.IllustrationCount);
        foreach (string key in CombatReportAssets.Keys)
        {
            var image = assets.Resolve(key);
            Assert.NotNull(image);
            Assert.Equal(1024, image.Width); Assert.Equal(576, image.Height);
        }
    }

    [Fact]
    public void MissingAndCorruptAssetUsesFallback()
    {
        using var missing = new CombatReportAssets(Path.GetTempPath());
        missing.Prepare([Report(11) with { Reporter = new(BattleReportReporterRole.Captain, PortraitImage: "absent.png") }]);
        Assert.Null(missing.Resolve("enemy-in-range"));
        Assert.Null(missing.ResolvePortrait("absent.png"));
        using var corrupt = new CombatReportAssets(decode: _ => throw new IOException("corrupt"));
        corrupt.Prepare([]);
        Assert.Equal(0, corrupt.IllustrationCount);
        Assert.NotEqual(default, corrupt.Accent("enemy-in-range"));
    }

    [Fact]
    public void PortraitCacheIsBoundedAndDisposed()
    {
        var decoded = new List<SKBitmap>();
        var assets = new CombatReportAssets(decode: _ => { var bitmap = new SKBitmap(16, 16); decoded.Add(bitmap); return bitmap; });
        for (int i = 0; i < 40; i++) assets.Prepare([Report(11) with { Reporter = new(BattleReportReporterRole.Captain, PortraitImage: $"portrait-{i}.png") }]);
        Assert.Equal(32, assets.PortraitCount);
        Assert.Null(assets.ResolvePortrait("portrait-0.png"));
        Assert.NotNull(assets.ResolvePortrait("portrait-39.png"));
        Assert.Equal(8, decoded.Count(b => b.Handle == IntPtr.Zero));
        assets.Dispose(); assets.Dispose();
        Assert.All(decoded, b => Assert.Equal(IntPtr.Zero, b.Handle));
        Assert.Equal(0, assets.PortraitCount);
    }

    [Fact]
    public void RepeatedDrawDoesNotReadOrDecodeFiles()
    {
        int reads = 0;
        using var assets = new CombatReportAssets(decode: _ => { reads++; return new SKBitmap(8, 8); });
        var report = Report(11) with { Reporter = new(BattleReportReporterRole.Captain, PortraitImage: "portrait.png") };
        assets.Prepare([report]);
        Assert.Equal(9, reads);
        for (int i = 0; i < 100; i++)
        {
            assets.Prepare([report]);
            Assert.NotNull(assets.Resolve("enemy-in-range"));
            Assert.NotNull(assets.ResolvePortrait("portrait.png"));
        }
        Assert.Equal(9, reads);
    }
}
