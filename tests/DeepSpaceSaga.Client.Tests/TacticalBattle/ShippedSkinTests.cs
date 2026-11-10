using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class ShippedSkinTests
{
    [Fact]
    public void XenonStar_build_output_loads_and_corrupt_pack_falls_back()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Skins");
        string pack = Path.Combine(root, "xenon-star");
        bool expected = Environment.GetEnvironmentVariable("DSS_EXPECT_XENON_STAR") == "1";
        using var catalog = TacticalBattleSkinCatalog.Load(root);
        Assert.Equal(expected, catalog.TryGet("xenon-star", out var xenon));
        Assert.Contains(("contrast", "Contrast"), catalog.Options);
        if (!expected)
        {
            Assert.Same(TacticalBattleSkin.Classic, catalog.GetOrClassic("xenon-star"));
            return;
        }

        Assert.Contains(("xenon-star", "Xenon Star"), catalog.Options);
        Assert.IsType<SKImage>(xenon.Image("ui.panel.art"));
        string validation = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
        string temp = Path.Combine(validation, "xenon-fallback-" + Guid.NewGuid().ToString("N"));
        string target = Path.Combine(temp, "xenon-star");
        Directory.CreateDirectory(target);
        try
        {
            foreach (var file in Directory.GetFiles(pack, "*", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(target, Path.GetRelativePath(pack, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination);
            }
            File.WriteAllText(Path.Combine(target, "assets", "weapons_bg.png"), "not a PNG");
            List<string> warnings = [];
            using var damaged = TacticalBattleSkinCatalog.Load(temp, warnings.Add);
            Assert.Same(TacticalBattleSkin.Classic, damaged.GetOrClassic("xenon-star"));
            Assert.Contains(warnings, warning => warning.Contains("weapons_bg.png", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void Contrast_manifest_and_field_image_are_present_in_real_output()
    {
        List<string> warnings = [];
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"), warnings.Add);

        Assert.Empty(warnings);
        Assert.Contains(("contrast", "Contrast"), catalog.Options);
        var contrast = catalog.GetOrClassic("contrast");
        var image = Assert.IsType<SKImage>(contrast.Image("field.background"));
        Assert.Equal(256, image.Width);
        Assert.Equal(160, image.Height);
        Assert.Equal("diamond", contrast.Shape("field.unit.drone.own.selected"));
        Assert.Equal("triangle", contrast.Shape("torpedo.projectile"));
        Assert.Equal(40, contrast.Metric("shot.trail", "tail-length", 0));
        Assert.NotEqual(SKColors.Black, contrast.Color("tabs.forecast", "fill", SKColors.Black));
    }
}
