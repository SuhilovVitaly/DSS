using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class ShippedSkinTests
{
    [Fact]
    public void Tabletop_steel_roles_and_all_six_gradients_match_the_ui_contract()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var skin = catalog.GetOrClassic("tabletop");
        Assert.Equal(1, skin.Metric("ui.style", "steel", 0));
        Assert.Equal(1, skin.Metric("ui.pilot-column", "slim", 0));
        foreach (string role in new[] { "ui.panel", "ui.panel.build", "ui.panel.report", "ui.pilot-card" })
        {
            Assert.Equal(SKColor.Parse("#2C343E"), skin.Color(role, "fill", SKColors.Magenta));
            Assert.Equal(SKColor.Parse("#05070A"), skin.Color(role, "stroke", SKColors.Magenta));
            Assert.Equal("assets/ui/steel.png", skin.Resource(role + ".art", "image"));
        }
        foreach (var (role, property, hex) in new[] {
            ("ui.panel.bevel", "fill", "#4C5764"), ("ui.panel.bevel", "stroke", "#0A0E12"),
            ("ui.text", "text", "#E4ECF4"), ("ui.bar.armor", "fill", "#5A7CA8"), ("ui.bar.hull", "fill", "#5C9440"),
            ("ui.build.name", "text", "#E4ECF4"), ("ui.build.slot.armor", "fill", "#5A7CA8"),
            ("ui.button", "text", "#E4ECF4"), ("ui.button.primary", "text", "#2A1A08"),
            ("ui.button.pending", "text", "#6A7480"), ("ui.box.target.selected", "text", "#2A1A08"),
            ("ui.box.decoy", "text", "#B8E8A0"), ("ui.box.decoy.selected", "text", "#B8E8A0"),
            ("ui.box.evade", "text", "#E0D4FF"), ("ui.box.evade.selected", "text", "#E0D4FF"),
            ("ui.box.decoy.unavailable", "text", "#6A7480"), ("ui.box.evade.unavailable", "text", "#6A7480") })
            Assert.Equal(SKColor.Parse(hex), skin.Color(role, property, SKColors.Magenta));
        var images = skin.ResourceReferences.Where(r => r.Path.StartsWith("assets/ui/", StringComparison.Ordinal)).ToArray();
        Assert.Equal(6, images.Select(r => r.Path).Distinct().Count());
        foreach (string role in new[] { "ui.panel", "ui.panel.build", "ui.panel.report", "ui.pilot-card", "ui.pilot-card.selected",
            "ui.button", "ui.button.primary", "ui.button.pending", "ui.box.target.selected", "ui.box.decoy", "ui.box.evade" })
        {
            var image = Assert.IsType<SKImage>(skin.Image(role + ".art"));
            Assert.Equal((4, 128), (image.Width, image.Height));
        }
        Assert.InRange(skin.ResourceReferences.Select(r => r.Path).Distinct().Count(), 1, 32);
        Assert.Equal(SKColor.Parse("#A8E08A"), skin.Color("ui.box.decoy.selected", "stroke", SKColors.Magenta));
        Assert.Equal(SKColor.Parse("#C8B8F0"), skin.Color("ui.box.evade.selected", "stroke", SKColors.Magenta));
        Assert.Equal(SKColor.Parse("#20262E"), skin.Color("ui.box.decoy.unavailable", "fill", SKColors.Magenta));
        Assert.Equal(SKColor.Parse("#FFE27A"), skin.Color("field.move.handle", "fill", SKColors.Magenta));
    }

    [Fact]
    public void Tabletop_build_output_loads_all_sheets_and_corrupt_pack_falls_back()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Skins");
        List<string> warnings = [];
        using var catalog = TacticalBattleSkinCatalog.Load(root, warnings.Add);
        Assert.Empty(warnings);
        Assert.Contains(("tabletop", "Tabletop"), catalog.Options);
        var skin = catalog.GetOrClassic("tabletop");
        Assert.Equal(1, skin.Metric("field.style", "tabletop", 0));
        Assert.Equal(1600, Assert.IsType<SKImage>(skin.Image("field.background")).Width);
        foreach (string model in new[] { "light", "heavy", "thug", "carrier", "torpedo" })
        {
            foreach (string side in model is "carrier" or "torpedo" ? new[] { "own", "enemy" } : new[] { "own", "enemy", "disabled" })
            {
                string role = $"field.unit.sprite.{model}.{side}";
                var sheet = Assert.IsType<SKImage>(skin.Image(role));
                int cell = model == "carrier" ? 96 : model == "torpedo" ? 32 : 64;
                Assert.Equal(cell * 8, sheet.Width);
                Assert.Equal(cell * 4, sheet.Height);
                Assert.Equal(32, skin.Metric(role, "frames", 0));
            }
        }
        Assert.Equal(1280, Assert.IsType<SKImage>(skin.Image("field.fx.boom-kill")).Width);
        Assert.Equal(16, skin.Metric("field.fx.boom-kill", "frames", 0));
        Assert.Equal(55, skin.Metric("field.fx.boom-kill", "frame-ms", 0));
        Assert.Equal(24, skin.ResourceReferences.Select(r => r.Path).Distinct().Count());

        string validation = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
        string temp = Path.Combine(validation, "tabletop-fallback-" + Guid.NewGuid().ToString("N"));
        string pack = Path.Combine(root, "tabletop"), target = Path.Combine(temp, "tabletop");
        try
        {
            foreach (string file in Directory.GetFiles(pack, "*", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(target, Path.GetRelativePath(pack, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination);
            }
            File.WriteAllText(Path.Combine(target, "assets", "fx", "boom-kill.png"), "broken");
            warnings.Clear();
            using var damaged = TacticalBattleSkinCatalog.Load(temp, warnings.Add);
            Assert.Same(TacticalBattleSkin.Classic, damaged.GetOrClassic("tabletop"));
            Assert.Contains(warnings, w => w.Contains("boom-kill.png", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void XenonStar_build_output_loads_and_corrupt_pack_falls_back()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Skins");
        string pack = Path.Combine(root, "xenon-star");
        using var catalog = TacticalBattleSkinCatalog.Load(root);
        bool installed = catalog.TryGet("xenon-star", out var xenon);
        string? expectation = Environment.GetEnvironmentVariable("DSS_EXPECT_XENON_STAR");
        if (expectation is "0" or "1")
            Assert.Equal(expectation == "1", installed);
        Assert.Contains(("contrast", "Contrast"), catalog.Options);
        if (!installed)
        {
            Assert.False(File.Exists(Path.Combine(pack, "skin.json")), "An installed Xenon Star manifest failed to load.");
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
