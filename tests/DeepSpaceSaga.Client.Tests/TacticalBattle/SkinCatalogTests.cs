using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class SkinCatalogTests
{
    [Fact]
    public void Catalog_discovers_a_pack_and_loads_assets_from_its_folder()
    {
        using var fixture = new PackDirectory();
        string directory = fixture.Pack("pack-folder", "custom", "field.png");
        string path = WritePng(directory, "field.png", 19, 11);
        const string font = "font.otf";
        File.Copy(Path.Combine(AppContext.BaseDirectory, "UI", "Fonts", "humaroid.regular.otf"), Path.Combine(directory, "assets", font));
        List<string> warnings = [];
        using var catalog = TacticalBattleSkinCatalog.Load(fixture.Root, warnings.Add);

        Assert.Equal(new[] { ("classic", "Classic"), ("custom", "Skin custom") }, catalog.Options);
        Assert.Empty(warnings);
        var skin = catalog.GetOrClassic("custom");
        var image = Assert.IsType<SKImage>(skin.Image("field.background.selected"));
        Assert.Equal(19, image.Width);
        Assert.Equal(11, image.Height);
        Assert.NotNull(skin.Typeface("field.background.selected"));
        File.Delete(path);
        using var pixels = SKBitmap.FromImage(image);
        Assert.Equal(SKColors.CornflowerBlue, pixels.GetPixel(1, 1));
        Assert.Same(TacticalBattleSkin.Classic, catalog.GetOrClassic("Custom"));
        catalog.Dispose();
        catalog.Dispose();
    }

    [Fact]
    public void Bad_packs_and_duplicate_ids_leave_other_packs_and_classic_usable()
    {
        using var fixture = new PackDirectory();
        fixture.Pack("valid-b", "zulu");
        fixture.Pack("valid-a", "alpha");
        fixture.Pack("dupe-b", "duplicate");
        fixture.Pack("dupe-a", "duplicate");
        fixture.Pack("reserved", "classic");
        fixture.Pack("missing", "missing", "missing.png");
        fixture.Pack("version", "unsupported", version: 2);
        string invalidUnicode = fixture.Pack("invalid-unicode", "invalid-unicode");
        string manifestPath = Path.Combine(invalidUnicode, "skin.json");
        File.WriteAllText(manifestPath, File.ReadAllText(manifestPath).Replace("Skin invalid-unicode", @"\uD800"));
        string invalid = fixture.Pack("invalid", "invalid");
        File.WriteAllText(Path.Combine(invalid, "skin.json"), "{");
        string corrupt = fixture.Pack("corrupt", "corrupt", "invalid.png");
        Directory.CreateDirectory(Path.Combine(corrupt, "assets"));
        File.WriteAllText(Path.Combine(corrupt, "assets", "invalid.png"), "not PNG");
        string oversized = fixture.Pack("oversized", "oversized");
        using (var stream = File.OpenWrite(Path.Combine(oversized, "skin.json")))
            stream.SetLength(TacticalBattleSkin.MaxManifestBytes + 1);
        Directory.CreateDirectory(Path.Combine(fixture.Root, "no-manifest"));

        List<string> warnings = [];
        using var catalog = TacticalBattleSkinCatalog.Load(fixture.Root, warnings.Add);
        Assert.Equal(new[] { "classic", "alpha", "zulu" }, catalog.Options.Select(option => option.Id));
        Assert.Equal(9, warnings.Count);
        Assert.Contains(warnings, w => w.Contains("dupe-a") && w.Contains("duplicate ID"));
        Assert.Contains(warnings, w => w.Contains("dupe-b") && w.Contains("duplicate ID"));
        Assert.Contains(warnings, w => w.Contains("roles.field.background.image") && w.Contains("missing.png"));
        Assert.Contains(warnings, w => w.Contains("corrupt") && w.Contains("invalid PNG"));
        Assert.Contains(warnings, w => w.Contains("version") && w.Contains("schemaVersion"));
        Assert.Contains(warnings, w => w.Contains("oversized") && w.Contains("262144"));
        using var empty = TacticalBattleSkinCatalog.Load(Path.Combine(fixture.Root, "absent"));
        Assert.Same(TacticalBattleSkin.Classic, empty.GetOrClassic("alpha"));
    }

    [Fact]
    public void Catalog_rejects_escape_and_reparse_paths()
    {
        using var fixture = new PackDirectory();
        fixture.Pack("escape", "escape", "../other.png");
        fixture.Pack("device", "device", "CON.png");
        string directory = fixture.Pack("linked", "linked", "other.png");
        string target = Path.Combine(fixture.Root, "other");
        Directory.CreateDirectory(target);
        File.Move(WritePng(target, "other.png", 8, 8), Path.Combine(target, "other.png"));
        string link = Path.Combine(directory, "assets");

        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (IOException) when (OperatingSystem.IsWindows())
        {
            // A Windows test account may lack SeCreateSymbolicLinkPrivilege. A directory
            // junction exercises the same ReparsePoint guard without that privilege.
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe")
            {
                Arguments = "/d /c mklink /J \"%DSS_TEST_LINK%\" \"%DSS_TEST_TARGET%\"",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            start.Environment["DSS_TEST_LINK"] = link;
            start.Environment["DSS_TEST_TARGET"] = target;
            using var process = System.Diagnostics.Process.Start(start);
            Assert.NotNull(process);
            Assert.True(process.WaitForExit(5000));
            Assert.Equal(0, process.ExitCode);
        }

        try
        {
            List<string> warnings = [];
            using var catalog = TacticalBattleSkinCatalog.Load(fixture.Root, warnings.Add);
            Assert.Single(catalog.Options);
            Assert.Contains(warnings, w => w.Contains("escape") && w.Contains("relative path"));
            Assert.Contains(warnings, w => w.Contains("linked") && w.Contains("reparse"));
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    private static string WritePng(string folder, string name, int width, int height)
    {
        string path = Path.Combine(folder, "assets", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.Create(path);
        data.SaveTo(output);
        return path;
    }

    private sealed class PackDirectory : IDisposable
    {
        internal string Root { get; } = Path.Combine(
            Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath(),
            "skin-catalog-" + Guid.NewGuid().ToString("N"));

        internal string Pack(string folder, string id, string? image = null, int version = 1)
        {
            string directory = Path.Combine(Root, folder);
            Directory.CreateDirectory(directory);
            var role = new Dictionary<string, string>();
            if (image is not null)
                role["image"] = "assets/" + image;
            if (id == "custom")
                role["font"] = "assets/font.otf";
            File.WriteAllText(Path.Combine(directory, "skin.json"), JsonSerializer.Serialize(new
            {
                schemaVersion = version,
                id,
                displayName = $"Skin {id}",
                roles = new Dictionary<string, Dictionary<string, string>> { ["field.background"] = role },
            }));
            return directory;
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
