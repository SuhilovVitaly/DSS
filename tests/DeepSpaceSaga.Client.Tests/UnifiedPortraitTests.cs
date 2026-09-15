using System.Security.Cryptography;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class UnifiedPortraitTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Images", "Persons", "W4");
    private static readonly Lazy<PortraitAssetRepository> Pack = new(() => new(Root));

    [Fact]
    public void W4_contains_only_complete_portraits_and_unchanged_shared_costumes()
    {
        Assert.Equal(14, Pack.Value.Style.LibraryVersion);
        Assert.Equal(new[] { "Portrait", "Clothes" }, Pack.Value.Style.Layers.Select(l => l.Category));
        int portraitCount = Directory.EnumerateFiles(Path.Combine(Root, "Portraits"))
            .Count(f => Path.GetExtension(f).Equals(".png", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(portraitCount, Pack.Value.Parts.Count(p => p.Category == "Portrait"));
        Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == "Clothes"));
        Assert.Equal(portraitCount + 3, Pack.Value.Parts.Count);
        string[] costumeHashes = [
            "32BD456D9C3D5F36D1E012482C9309C1D44EA15D443EBF4D698D2BCC29C3989E",
            "9013A61E41B8315A6B026DDD01927BB34551A8BBF6B9B447B822F5B96C9364BC",
            "FFA1BFE4CF48B1C4A2FBA6BEDF23786FB06596D36672ECDCBADE8B5EE3F16D4B"];
        foreach (var p in Pack.Value.Parts.Where(p => p.Category == "Clothes"))
        {
            int index = int.Parse(p.Id.Split('.').Last()) - 1;
            Assert.Equal(costumeHashes[index], Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Pack.Value.TexturePath(p.Texture)))));
        }
        Assert.False(Directory.Exists(Path.Combine(Root, "Sources")));
        Assert.False(Directory.Exists(Path.Combine(Root, "Generated")));
    }

    [Fact]
    public void All_combinations_preserve_complete_head_pixels_and_cover_the_collar_socket()
    {
        using var renderer = new PortraitRenderer(Pack.Value);
        var baseline = new PortraitGenerator(Pack.Value).Generate(4);
        var hashes = new HashSet<string>();
        foreach (var portrait in Pack.Value.Parts.Where(p => p.Category == "Portrait"))
        {
            byte[]? reference = null;
            foreach (var suit in Pack.Value.Parts.Where(p => p.Category == "Clothes"))
            {
                var a = baseline with { Parts = baseline.Parts.SetItem("Portrait", portrait.Id).SetItem("Clothes", suit.Id) };
                using var bitmap = SKBitmap.FromImage(renderer.Render(a, 256));
                var headPixels = bitmap.Bytes.AsSpan(0, bitmap.RowBytes*128).ToArray();
                if (reference is not null) Assert.Equal(reference, headPixels);
                reference = headPixels;
                for (int y = 142; y <= 164; y++)
                for (int x = 110; x <= 145; x++)
                    Assert.True(bitmap.GetPixel(x, y).Alpha == 255, $"Neck gap at {x},{y}: {portrait.Id}/{suit.Id}");
                Assert.True(hashes.Add(Convert.ToHexString(SHA256.HashData(bitmap.Bytes))));
            }
        }
        Assert.Equal(Pack.Value.Parts.Count(p => p.Category == "Portrait") * 3, hashes.Count);
    }

    [Fact]
    public void Default_window_selects_portrait_cards_keeps_costume_and_rerolls_on_click()
    {
        var screen = new TempCharacterImageScreen(); screen.OnActivated();
        try
        {
            Assert.NotNull(screen.Appearance); Assert.Equal(14, screen.Appearance.LibraryVersion);
            Assert.Equal(2, screen.Appearance.Parts.Count);
            using var bitmap = new SKBitmap(1120, 748); using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1120, 748);
            string suit = screen.Appearance.Parts["Clothes"];
            for (int i = 0; i < 3; i++)
            {
                screen.OnMouseDown(980, 170 + i*147, MouseButton.Left);
                Assert.Equal($"female.w4.portrait.{i+1:000}", screen.Appearance!.Parts["Portrait"]);
                Assert.Equal(suit, screen.Appearance.Parts["Clothes"]);
            }
            screen.OnMouseDown(234, 251, MouseButton.Left);
            Assert.NotEqual(suit, screen.Appearance!.Parts["Clothes"]);
            Assert.Equal("female.w4.portrait.003", screen.Appearance.Parts["Portrait"]);
            string before = AppearanceSerializer.Serialize(screen.Appearance);
            screen.OnMouseDown(512, 350, MouseButton.Left);
            Assert.NotEqual(before, AppearanceSerializer.Serialize(screen.Appearance!));
            Assert.True(Pack.Value.IsCompatible(screen.Appearance!));
        }
        finally { screen.OnDeactivated(); }
    }

    [Fact]
    public void Folder_discovery_preserves_ids_avoids_duplicates_and_updates_after_files_change()
    {
        using var fixture = new TemporaryPack();
        fixture.Add("Zeta.PNG");
        Directory.CreateDirectory(Path.Combine(fixture.Root, "Portraits", "helpers"));
        File.Copy(Path.Combine(fixture.Root, "Portraits", "Zeta.PNG"), Path.Combine(fixture.Root, "Portraits", "helpers", "preview.png"));
        var first = new PortraitAssetRepository(fixture.Root);
        var portraits = first.Parts.Where(p => p.Category == "Portrait").ToArray();
        Assert.Equal(2, portraits.Length);
        Assert.Contains(portraits, p => p.Id == "female.w4.portrait.001");
        var added = Assert.Single(portraits, p => p.Texture.EndsWith("Zeta.PNG", StringComparison.Ordinal));
        var generator = new PortraitGenerator(first);
        var appearance = Enumerable.Range(0, 100).Select(i => generator.Generate(i))
            .First(a => a.Parts["Portrait"] == added.Id);
        string saved = AppearanceSerializer.Serialize(appearance);

        fixture.Add("Alpha.png");
        var second = new PortraitAssetRepository(fixture.Root);
        Assert.Equal(added.Id, second.Parts.Single(p => p.Texture == added.Texture).Id);
        second.ValidateAppearance(AppearanceSerializer.Deserialize(saved));
        File.Delete(Path.Combine(fixture.Root, "Portraits", "portrait-01.png"));
        var third = new PortraitAssetRepository(fixture.Root);
        Assert.DoesNotContain(third.Parts, p => p.Id == "female.w4.portrait.001");
        Assert.Equal(2, third.Parts.Count(p => p.Category == "Portrait"));
    }

    [Fact]
    public void Paged_cards_select_every_portrait_and_save_restore_discovered_characters()
    {
        using var fixture = new TemporaryPack();
        for (int i = 0; i < 6; i++) fixture.Add($"extra-{i}.png");
        var pack = new PortraitAssetRepository(fixture.Root);
        var choices = pack.Parts.Where(p => p.Category == "Portrait").OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
        var screen = new TempCharacterImageScreen(fixture.Root, Path.Combine(fixture.Root, "preset.json"));
        using var bitmap = new SKBitmap(1120, 748);
        using var canvas = new SKCanvas(bitmap);
        screen.OnActivated();
        try
        {
            Assert.NotNull(screen.Appearance);
            string costume = screen.Appearance.Parts["Clothes"];
            for (int i = 0; i < choices.Length; i++)
            {
                screen.Render(canvas, 1120, 748);
                if (i > 0 && i % 3 == 0)
                {
                    screen.OnMouseDown(1070, 599, MouseButton.Left);
                    screen.Render(canvas, 1120, 748);
                }
                screen.OnMouseDown(980, 170 + i % 3 * 147, MouseButton.Left);
                Assert.Equal(choices[i].Id, screen.Appearance!.Parts["Portrait"]);
                Assert.Equal(costume, screen.Appearance.Parts["Clothes"]);
            }
            screen.OnMouseDown(980, 317, MouseButton.Left); // Empty slot on the final page.
            Assert.Equal(choices[^1].Id, screen.Appearance!.Parts["Portrait"]);
            screen.OnMouseDown(860, 695, MouseButton.Left); // Save.
            screen.OnMouseDown(234, 208, MouseButton.Left); // Cycle portrait.
            Assert.NotEqual(choices[^1].Id, screen.Appearance.Parts["Portrait"]);
            screen.OnMouseDown(1010, 695, MouseButton.Left); // Restore.
            Assert.Equal(choices[^1].Id, screen.Appearance.Parts["Portrait"]);
            screen.OnMouseDown(1070, 599, MouseButton.Left); // Wrap to first page.
            screen.Render(canvas, 1120, 748);
            screen.OnMouseDown(980, 170, MouseButton.Left);
            Assert.Equal(choices[0].Id, screen.Appearance.Parts["Portrait"]);
            screen.OnDeactivated();
            fixture.Add("zz-new.png");
            screen.OnActivated();
            screen.Render(canvas, 1120, 748);
            screen.OnMouseDown(835, 599, MouseButton.Left); // Last page after reopening.
            screen.Render(canvas, 1120, 748);
            screen.OnMouseDown(980, 317, MouseButton.Left);
            Assert.Equal("female.w4.portrait.file.zz-new", screen.Appearance!.Parts["Portrait"]);
        }
        finally { screen.OnDeactivated(); }
    }

    private sealed class TemporaryPack : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "dss-portrait-discovery-" + Guid.NewGuid().ToString("N"));
        public TemporaryPack()
        {
            Directory.CreateDirectory(Root);
            File.Copy(Path.Combine(UnifiedPortraitTests.Root, "portrait-style.json"), Path.Combine(Root, "portrait-style.json"));
            var catalog = Pack.Value.Parts.Where(p => p.Category == "Clothes" || p.Id == "female.w4.portrait.001").ToArray();
            File.WriteAllText(Path.Combine(Root, "parts.json"), System.Text.Json.JsonSerializer.Serialize(catalog, AppearanceSerializer.Options));
            foreach (var part in catalog)
            {
                string destination = Path.Combine(Root, part.Texture);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Pack.Value.TexturePath(part.Texture), destination);
            }
        }
        public void Add(string filename) => File.Copy(Path.Combine(UnifiedPortraitTests.Root, "Portraits", "portrait-01.png"), Path.Combine(Root, "Portraits", filename));
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
