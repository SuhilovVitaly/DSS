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
        Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == "Portrait"));
        Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == "Clothes"));
        Assert.Equal(6, Pack.Value.Parts.Count);
        var previous = new PortraitAssetRepository(Path.GetFullPath(Path.Combine(Root, "../W2")), false);
        foreach (var p in Pack.Value.Parts.Where(p => p.Category == "Clothes"))
        {
            var original = previous.Parts.Single(c => c.Category == "Clothes" && c.Id.Split('.').Last() == p.Id.Split('.').Last());
            Assert.Equal(File.ReadAllBytes(previous.TexturePath(original.Texture)), File.ReadAllBytes(Pack.Value.TexturePath(p.Texture)));
        }
        Assert.False(Directory.Exists(Path.Combine(Root, "Sources")));
        Assert.False(Directory.Exists(Path.Combine(Root, "Generated")));
    }

    [Fact]
    public void Nine_combinations_preserve_complete_head_pixels_and_cover_the_collar_socket()
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
        Assert.Equal(9, hashes.Count);
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
}
