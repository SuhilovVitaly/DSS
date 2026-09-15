using System.Security.Cryptography;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class WholeFacePortraitTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Images", "Persons", "W1");
    private static readonly Lazy<PortraitAssetRepository> Pack = new(() => new(Root));

    [Fact]
    public void Pack_contains_three_shells_ten_whole_faces_and_the_existing_outfits_and_hair()
    {
        Assert.Equal(11, Pack.Value.Style.LibraryVersion);
        Assert.Equal(new[] { "Oval", "Face", "Clothes", "Hair" }, Pack.Value.Style.Layers.Select(l => l.Category));
        Assert.Equal(26, Pack.Value.Parts.Count);
        Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == "Oval"));
        Assert.Equal(10, Pack.Value.Parts.Count(p => p.Category == "Face"));
        Assert.Equal(10, Pack.Value.Parts.Count(p => p.Category == "Hair"));
        Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == "Clothes"));
        Assert.All(Pack.Value.Parts, p => Assert.Empty(p.FaceTextures));
        Assert.False(Directory.Exists(Path.Combine(Root, "Sources")));
        Assert.False(Directory.Exists(Path.Combine(Root, "Generated")));
    }

    [Fact]
    public void All_900_combinations_render_uniquely_with_an_opaque_neck_socket()
    {
        using var renderer = new PortraitRenderer(Pack.Value);
        var baseline = new PortraitGenerator(Pack.Value).Generate(1);
        var hashes = new HashSet<string>();
        foreach (var oval in Pack.Value.Parts.Where(p => p.Category == "Oval"))
        foreach (var face in Pack.Value.Parts.Where(p => p.Category == "Face"))
        foreach (var hair in Pack.Value.Parts.Where(p => p.Category == "Hair"))
        foreach (var clothes in Pack.Value.Parts.Where(p => p.Category == "Clothes"))
        {
            var a = baseline with { Parts = baseline.Parts.SetItem("Oval", oval.Id).SetItem("Face", face.Id).SetItem("Hair", hair.Id).SetItem("Clothes", clothes.Id) };
            var rendered = renderer.Render(a, 256);
            using var bitmap = SKBitmap.FromImage(rendered);
            for (int y = 142; y <= 164; y++)
                for (int x = 110; x <= 147; x++) Assert.True(bitmap.GetPixel(x, y).Alpha == 255, $"Neck gap at {x},{y}: {bitmap.GetPixel(x, y).Alpha}; {oval.Id}/{clothes.Id}");
            Assert.True(hashes.Add(Convert.ToHexString(SHA256.HashData(bitmap.Bytes))));
        }
        Assert.Equal(900, hashes.Count);
    }

    [Fact]
    public void Switching_oval_preserves_the_whole_face_and_common_neck_skin()
    {
        var ovals = Pack.Value.Parts.Where(p => p.Category == "Oval").ToArray();
        using var first = SKBitmap.Decode(Pack.Value.TexturePath(ovals[0].Texture));
        foreach (var part in ovals.Skip(1))
        {
            using var other = SKBitmap.Decode(Pack.Value.TexturePath(part.Texture));
            foreach (var (x, y) in new[] { (512, 300), (470, 420), (512, 602) })
                Assert.Equal(first.GetPixel(x, y), other.GetPixel(x, y));
        }
        using var renderer = new PortraitRenderer(Pack.Value);
        var baseline = new PortraitGenerator(Pack.Value).Generate(12);
        foreach (var face in Pack.Value.Parts.Where(p => p.Category == "Face"))
        {
            var a = baseline with { Parts = baseline.Parts.SetItem("Face", face.Id).SetItem("Oval", ovals[0].Id) };
            using var original = SKBitmap.FromImage(renderer.Render(a, 1024));
            foreach (var oval in ovals.Skip(1))
            {
                using var changed = SKBitmap.FromImage(renderer.Render(a with { Parts = a.Parts.SetItem("Oval", oval.Id) }, 1024));
                foreach (var (x, y) in new[] { (440, 295), (580, 295), (512, 390), (512, 440) })
                    Assert.Equal(original.GetPixel(x, y), changed.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void W1_workshop_still_generates_a_new_complete_face_portrait()
    {
        var screen = new TempCharacterImageScreen(Root);
        screen.OnActivated();
        try
        {
            Assert.NotNull(screen.Appearance);
            Assert.Equal(11, screen.Appearance.LibraryVersion);
            Assert.Equal(4, screen.Appearance.Parts.Count);
            string before = AppearanceSerializer.Serialize(screen.Appearance);
            using var bitmap = new SKBitmap(1120, 748);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1120, 748);
            screen.OnMouseDown(500, 350, MouseButton.Left);
            Assert.NotEqual(before, AppearanceSerializer.Serialize(screen.Appearance!));
            Assert.True(Pack.Value.IsCompatible(screen.Appearance!));
        }
        finally { screen.OnDeactivated(); }
    }
}
