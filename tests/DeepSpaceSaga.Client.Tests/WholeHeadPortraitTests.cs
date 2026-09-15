using System.Security.Cryptography;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class WholeHeadPortraitTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Images", "Persons", "W2");
    private static readonly Lazy<PortraitAssetRepository> Pack = new(() => new(Root));

    [Fact]
    public void Pack_has_complete_heads_one_separate_neck_and_five_new_haircuts()
    {
        Assert.Equal(12, Pack.Value.Style.LibraryVersion);
        Assert.Equal(new[] { "Neck", "Head", "Clothes", "Hair" }, Pack.Value.Style.Layers.Select(l => l.Category));
        Assert.Equal(12, Pack.Value.Parts.Count);
        Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == "Head"));
        Assert.Single(Pack.Value.Parts, p => p.Category == "Neck");
        Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == "Clothes"));
        Assert.Equal(5, Pack.Value.Parts.Count(p => p.Category == "Hair"));
        foreach (var part in Pack.Value.Parts.Where(p => p.Category is "Head" or "Neck"))
        {
            using var bitmap = SKBitmap.Decode(Pack.Value.TexturePath(part.Texture));
            for (int y = 0; y < 1024; y++)
            for (int x = 0; x < 1024; x++)
                if (part.Category == "Head" && y >= 537 || part.Category == "Neck" && y < 455)
                    Assert.Equal(0, bitmap.GetPixel(x, y).Alpha);
        }
        Assert.False(Directory.Exists(Path.Combine(Root, "Sources")));
        Assert.False(Directory.Exists(Path.Combine(Root, "Generated")));
        var previous = new PortraitAssetRepository(Path.GetFullPath(Path.Combine(Root, "../W1")), false);
        var oldHair = previous.Parts.Where(p => p.Category == "Hair")
            .Select(p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(previous.TexturePath(p.Texture))))).ToHashSet();
        foreach (var p in Pack.Value.Parts.Where(p => p.Category == "Hair"))
            Assert.DoesNotContain(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Pack.Value.TexturePath(p.Texture)))), oldHair);
    }

    [Fact]
    public void All_45_combinations_are_distinct_and_have_no_gap_from_chin_to_collar()
    {
        using var renderer = new PortraitRenderer(Pack.Value);
        var baseline = new PortraitGenerator(Pack.Value).Generate(1);
        var hashes = new HashSet<string>();
        foreach (var head in Pack.Value.Parts.Where(p => p.Category == "Head"))
        foreach (var hair in Pack.Value.Parts.Where(p => p.Category == "Hair"))
        foreach (var clothes in Pack.Value.Parts.Where(p => p.Category == "Clothes"))
        {
            var a = baseline with { Parts = baseline.Parts.SetItem("Head", head.Id).SetItem("Hair", hair.Id).SetItem("Clothes", clothes.Id) };
            Assert.Equal("female.w2.neck.001", a.Parts["Neck"]);
            using var bitmap = SKBitmap.FromImage(renderer.Render(a, 256));
            for (int y = 126; y <= 164; y++)
            for (int x = 110; x <= 145; x++)
                Assert.True(bitmap.GetPixel(x, y).Alpha == 255, $"Gap at {x},{y}: {head.Id}/{hair.Id}/{clothes.Id}");
            Assert.True(hashes.Add(Convert.ToHexString(SHA256.HashData(bitmap.Bytes))));
        }
        Assert.Equal(45, hashes.Count);
    }

    [Fact]
    public void W2_window_cycles_whole_heads_preserves_neck_and_rerolls_on_click()
    {
        var screen = new TempCharacterImageScreen(Root); screen.OnActivated();
        try
        {
            Assert.NotNull(screen.Appearance); Assert.Equal(12, screen.Appearance.LibraryVersion);
            using var bitmap = new SKBitmap(1120, 748); using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1120, 748);
            var before = screen.Appearance;
            screen.OnMouseDown(234, 208, MouseButton.Left);
            Assert.NotEqual(before.Parts["Head"], screen.Appearance!.Parts["Head"]);
            foreach (string category in new[] { "Neck", "Hair", "Clothes" }) Assert.Equal(before.Parts[category], screen.Appearance.Parts[category]);
            screen.OnMouseDown(100, 337, MouseButton.Left); Assert.Equal("Neck", screen.SelectedLayer);
            string old = AppearanceSerializer.Serialize(screen.Appearance);
            screen.OnMouseDown(512, 350, MouseButton.Left);
            Assert.NotEqual(old, AppearanceSerializer.Serialize(screen.Appearance!)); Assert.Null(screen.SelectedLayer);
            Assert.True(Pack.Value.IsCompatible(screen.Appearance!));
            Assert.Equal(ScreenEvent.CloseTempCharacterImage, screen.OnKeyDown(Key.Escape));
        }
        finally { screen.OnDeactivated(); }
    }

    [Fact]
    public void Preset_round_trip_preserves_the_complete_W2_selection()
    {
        var generator = new PortraitGenerator(Pack.Value);
        using var renderer = new PortraitRenderer(Pack.Value);
        var a = generator.Generate(42);
        var b = AppearanceSerializer.Deserialize(AppearanceSerializer.Serialize(a));
        Pack.Value.ValidateAppearance(b);
        Assert.Equal(AppearanceSerializer.Serialize(a), AppearanceSerializer.Serialize(b));
        Assert.Equal(AppearanceSerializer.Serialize(a), AppearanceSerializer.Serialize(generator.Generate(42)));
        using var imageA = renderer.Render(a).Encode(); using var imageB = renderer.Render(b).Encode();
        Assert.Equal(imageA.ToArray(), imageB.ToArray());
    }
}
