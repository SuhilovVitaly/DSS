using System.Security.Cryptography;
using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class MalePortraitTests
{
    private static readonly string Persons = Path.Combine(AppContext.BaseDirectory, "Images", "Persons");
    private static readonly Lazy<PortraitAssetRepository> Male = new(() => new(Path.Combine(Persons, "M4")));

    [Fact]
    public void Male_pack_generates_distinct_complete_heads_with_all_three_costumes()
    {
        var assets = Male.Value;
        Assert.Equal("male", assets.Style.Gender);
        var portraits = assets.Parts.Where(p => p.Category == "Portrait").ToArray();
        Assert.True(portraits.Length >= 3);
        Assert.Equal(Directory.EnumerateFiles(Path.Combine(assets.Root, "Portraits"))
            .Count(p => Path.GetExtension(p).Equals(".png", StringComparison.OrdinalIgnoreCase)), portraits.Length);
        Assert.All(assets.Parts, p => Assert.Contains("male", p.Tags));
        Assert.All(portraits, p => Assert.StartsWith("male.m4.portrait.file.", p.Id));
        Assert.False(Directory.Exists(Path.Combine(assets.Root, "Sources")));
        Assert.False(Directory.Exists(Path.Combine(assets.Root, "Generated")));
        var generator = new PortraitGenerator(assets);
        var baseline = generator.Generate(42);
        Assert.Equal("male", baseline.Gender);
        Assert.Equal(AppearanceSerializer.Serialize(baseline), AppearanceSerializer.Serialize(generator.Generate(42)));
        assets.ValidateAppearance(AppearanceSerializer.Deserialize(AppearanceSerializer.Serialize(baseline)));
        Assert.False(assets.IsCompatible(baseline with { Gender = "female" }));
        var female = new PortraitAssetRepository(Path.Combine(Persons, "W4"), false);
        Assert.False(female.IsCompatible(baseline));
        Assert.False(assets.IsCompatible(new PortraitGenerator(female).Generate(42)));

        var suits = assets.Parts.Where(p => p.Category == "Clothes").ToArray();
        Assert.Equal(3, suits.Length);
        foreach (var suit in suits)
            Assert.Equal(File.ReadAllBytes(Path.Combine(female.Root, suit.Texture)), File.ReadAllBytes(assets.TexturePath(suit.Texture)));
        using var renderer = new PortraitRenderer(assets);
        var hashes = new HashSet<string>();
        foreach (var portrait in portraits)
        {
            byte[]? reference = null;
            foreach (var suit in suits)
            {
                var appearance = baseline with { Parts = baseline.Parts.SetItem("Portrait", portrait.Id).SetItem("Clothes", suit.Id) };
                using var bitmap = SKBitmap.FromImage(renderer.Render(appearance, 256));
                var head = bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 128).ToArray();
                if (reference is not null) Assert.Equal(reference, head);
                reference = head;
                for (int y = 142; y <= 164; y++)
                for (int x = 110; x <= 145; x++)
                    Assert.True(bitmap.GetPixel(x, y).Alpha == 255, $"Neck gap: {portrait.Id}/{suit.Id} at {x},{y}");
                Assert.True(hashes.Add(Convert.ToHexString(SHA256.HashData(bitmap.Bytes))));
            }
        }
        Assert.Equal(portraits.Length * 3, hashes.Count);
    }

    [Fact]
    public void New_male_png_is_discovered_with_male_tags_and_stable_saved_identity()
    {
        using var fixture = new TemporaryPacks();
        var before = new PortraitAssetRepository(fixture.MaleRoot);
        string source = before.TexturePath(before.Parts.First(p => p.Category == "Portrait").Texture);
        File.Copy(source, Path.Combine(fixture.MaleRoot, "Portraits", "Added.PNG"));
        Directory.CreateDirectory(Path.Combine(fixture.MaleRoot, "Portraits", "helpers"));
        File.Copy(source, Path.Combine(fixture.MaleRoot, "Portraits", "helpers", "preview.png"));
        var after = new PortraitAssetRepository(fixture.MaleRoot);
        var added = after.Get("male.m4.portrait.file.added");
        Assert.Equal(before.Parts.Count + 1, after.Parts.Count);
        Assert.Contains("male", added.Tags);
        var generator = new PortraitGenerator(after);
        var selected = Enumerable.Range(0, 100).Select(i => generator.Generate(i)).First(a => a.Parts["Portrait"] == added.Id);
        File.Copy(source, Path.Combine(fixture.MaleRoot, "Portraits", "Another.png"));
        new PortraitAssetRepository(fixture.MaleRoot).ValidateAppearance(
            AppearanceSerializer.Deserialize(AppearanceSerializer.Serialize(selected)));
    }

    [Fact]
    public void Window_switches_gender_selects_men_rerolls_and_keeps_separate_presets()
    {
        using var fixture = new TemporaryPacks();
        string preset = Path.Combine(fixture.Root, "preset.json");
        var screen = new TempCharacterImageScreen(fixture.FemaleRoot, preset);
        using var bitmap = new SKBitmap(1120, 748);
        using var canvas = new SKCanvas(bitmap);
        void Click(float x, float y) { screen.Render(canvas, 1120, 748); screen.OnMouseDown(x, y, MouseButton.Left); }
        screen.OnActivated();
        try
        {
            Assert.Equal("female", screen.Appearance!.Gender);
            string femaleSaved = AppearanceSerializer.Serialize(screen.Appearance);
            Click(860, 695);
            Click(720, 40);
            Assert.Equal("male", screen.Appearance!.Gender);
            string suit = screen.Appearance.Parts["Clothes"];
            var choices = Male.Value.Parts.Where(p => p.Category == "Portrait").OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < 3; i++)
            {
                Click(980, 170 + i * 147);
                Assert.Equal(choices[i].Id, screen.Appearance.Parts["Portrait"]);
                Assert.Equal(suit, screen.Appearance.Parts["Clothes"]);
            }
            Click(234, 251);
            Assert.NotEqual(suit, screen.Appearance.Parts["Clothes"]);
            string maleSaved = AppearanceSerializer.Serialize(screen.Appearance);
            Click(860, 695);
            Assert.Equal(femaleSaved, File.ReadAllText(preset));
            Assert.Equal(maleSaved, File.ReadAllText(Path.Combine(fixture.Root, "preset.m4.json")));
            Click(512, 350);
            Assert.NotEqual(maleSaved, AppearanceSerializer.Serialize(screen.Appearance!));
            Assert.True(Male.Value.IsCompatible(screen.Appearance!));
            Click(1010, 695);
            Assert.Equal(maleSaved, AppearanceSerializer.Serialize(screen.Appearance!));
            Click(580, 40);
            Assert.Equal("female", screen.Appearance!.Gender);
            Click(1010, 695);
            Assert.Equal(femaleSaved, AppearanceSerializer.Serialize(screen.Appearance!));
        }
        finally { screen.OnDeactivated(); }
    }

    [Fact]
    public void Missing_pack_clears_previous_portrait_and_allows_switching_back()
    {
        using var fixture = new TemporaryPacks();
        Directory.Move(fixture.MaleRoot, Path.Combine(fixture.Root, "unavailable"));
        var screen = new TempCharacterImageScreen(fixture.FemaleRoot);
        using var bitmap = new SKBitmap(1120, 748);
        using var canvas = new SKCanvas(bitmap);
        screen.OnActivated();
        try
        {
            Assert.NotNull(screen.Appearance);
            screen.Render(canvas, 1120, 748);
            screen.OnMouseDown(720, 40, MouseButton.Left);
            Assert.Null(screen.Appearance);
            screen.Render(canvas, 1120, 748);
            screen.OnMouseDown(580, 40, MouseButton.Left);
            Assert.Equal("female", screen.Appearance!.Gender);
        }
        finally { screen.OnDeactivated(); }
    }

    private sealed class TemporaryPacks : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "dss-male-tests-" + Guid.NewGuid().ToString("N"));
        public string MaleRoot => Path.Combine(Root, "M4");
        public string FemaleRoot => Path.Combine(Root, "W4");
        public TemporaryPacks()
        {
            foreach (string pack in new[] { "W4", "M4" })
            {
                string target = Path.Combine(Root, pack);
                Directory.CreateDirectory(Path.Combine(target, "Portraits"));
                var source = new PortraitAssetRepository(Path.Combine(Persons, pack), false);
                var parts = source.Parts.Where(p => p.Category == "Clothes" || pack == "M4" || p.Id == "female.w4.portrait.001").ToArray();
                File.WriteAllText(Path.Combine(target, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
                File.Copy(Path.Combine(source.Root, "portrait-style.json"), Path.Combine(target, "portrait-style.json"));
                foreach (var part in parts)
                {
                    string destination = Path.Combine(target, part.Texture);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(source.TexturePath(part.Texture), destination);
                }
            }
        }
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
