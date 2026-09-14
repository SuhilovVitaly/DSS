using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PortraitGeneratorTests
{
    private static readonly string PackRoot = Path.Combine(AppContext.BaseDirectory, "Images", "PortraitGenerator");
    private static readonly Lazy<PortraitAssetRepository> Pack = new(() => new(PackRoot));

    [Theory]
    [InlineData(1)] [InlineData(10)] [InlineData(100)] [InlineData(1000)] [InlineData(123456)] [InlineData(987654321)]
    public void Expanded_library_preserves_version_one_seeds_and_saved_appearances(int seed)
    {
        var original = AppearanceSerializer.Deserialize(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GoldenPortraits", "v1", $"seed-{seed}.json")));
        var recreated = new PortraitGenerator(Pack.Value).Generate(seed, 1);
        Pack.Value.ValidateAppearance(original);
        Assert.Equal(AppearanceSerializer.Serialize(original), AppearanceSerializer.Serialize(recreated));
        Assert.False(Pack.Value.IsCompatible(original with { Parts = original.Parts.SetItem("Face", "female.face.005") }));
    }

    [Fact]
    public void New_faces_have_distinct_jaws_and_keep_the_crown_inside_the_hair_envelope()
    {
        using var template = SKBitmap.Decode(Pack.Value.TexturePath("Parts/face-01.png"));
        var jawWidths = new List<int>();
        for (int id = 5; id <= 12; id++)
        {
            using var face = SKBitmap.Decode(Pack.Value.TexturePath($"Parts/face-{id:D2}.png"));
            jawWidths.Add(Enumerable.Range(0, 512).Count(x => face.GetPixel(x, 306).Alpha > 220));
            for (int y = 0; y < 150; y++)
            for (int x = 0; x < 512; x++)
                Assert.True(face.GetPixel(x, y).Alpha <= template.GetPixel(x, y).Alpha,
                    $"Face {id} protrudes beyond hairstyle envelope at ({x},{y}).");
        }
        Assert.True(jawWidths.Max() - jawWidths.Min() >= 25, "Face shapes must differ visibly, not only in name.");
    }

    [Fact]
    public void Every_face_enters_every_collar_without_a_transparent_seam()
    {
        var basis = new PortraitGenerator(Pack.Value).Generate(1000);
        using var renderer = new PortraitRenderer(Pack.Value);
        for (int face = 1; face <= Pack.Value.Parts.Count(p => p.Category == "Face"); face++)
        {
            using var skin = SKBitmap.Decode(Pack.Value.TexturePath($"Parts/face-{face:D2}.png"));
            for (int outfit = 1; outfit <= 4; outfit++)
            {
                var appearance = basis with
                {
                    Parts = basis.Parts.SetItem("Face", $"female.face.{face:D3}")
                        .SetItem("Clothes", $"female.clothes.{outfit:D3}")
                        .SetItem("HairBack", "female.hairback.004")
                        .SetItem("HairFront", "female.hairfront.003").Remove("Accessory"),
                    Colors = basis.Colors.SetItem("Skin", "#E9C6AD")
                };
                using var actual = SKBitmap.FromImage(renderer.Render(appearance));
                // Below the rear collar rim we must see skin, not the empty mannequin cavity.
                var expected = skin.GetPixel(250, 357); var visible = actual.GetPixel(250, 357);
                Assert.True(expected.Alpha >= 240, $"Face {face} stops above its collar.");
                Assert.InRange(Math.Abs(expected.Red - visible.Red), 0, 3);
                Assert.InRange(Math.Abs(expected.Green - visible.Green), 0, 3);
                Assert.InRange(Math.Abs(expected.Blue - visible.Blue), 0, 3);
                for (int y = 340; y <= 403; y++)
                for (int x = 237; x <= 275; x++)
                    Assert.True(actual.GetPixel(x, y).Alpha >= 240, $"Gap: face {face}, outfit {outfit}, ({x},{y}).");
            }
        }
    }

    [Fact]
    public void All_collars_limit_exposed_neck_to_natural_portrait_proportions()
    {
        float chin = Pack.Value.Style.Anchors["Chin"].Y * 512;
        foreach (var part in Pack.Value.Parts.Where(p => p.Category == "Clothes"))
        {
            using var clothes = SKBitmap.Decode(Pack.Value.TexturePath(part.Texture));
            int frontRim = Enumerable.Range((int)Math.Ceiling(chin), 110)
                .First(y => clothes.GetPixel(256, y).Alpha >= 240);
            Assert.InRange(frontRim - chin, 25, 48);
        }
    }

    [Theory]
    [InlineData(1)] [InlineData(10)] [InlineData(100)] [InlineData(1000)] [InlineData(123456)] [InlineData(987654321)]
    public void Fixed_seed_matches_reviewed_golden_portrait(int seed)
    {
        using var golden = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "GoldenPortraits", $"seed-{seed}.png"));
        Assert.NotNull(golden);
        using var renderer = new PortraitRenderer(Pack.Value);
        using var actual = SKBitmap.FromImage(renderer.Render(new PortraitGenerator(Pack.Value).Generate(seed)));
        Assert.Equal(golden.Width, actual.Width); Assert.Equal(golden.Height, actual.Height);
        Assert.Equal(golden.Pixels, actual.Pixels);
    }

    [Theory]
    [InlineData(1)] [InlineData(10)] [InlineData(100)] [InlineData(1000)] [InlineData(123456)] [InlineData(987654321)] [InlineData(int.MinValue)]
    public void GivenSeed_WhenGenerateMultipleTimes_ResultMustBeIdentical(int seed)
    {
        var first = new PortraitGenerator(Pack.Value).Generate(seed);
        var second = new PortraitGenerator(new PortraitAssetRepository(PackRoot, false)).Generate(seed);
        Assert.Equal(AppearanceSerializer.Serialize(first), AppearanceSerializer.Serialize(second));
    }

    [Fact]
    public void Serialization_preserves_identity_and_pixels()
    {
        var appearance = new PortraitGenerator(Pack.Value).Generate(123456);
        var restored = AppearanceSerializer.Deserialize(AppearanceSerializer.Serialize(appearance));
        Assert.Equal(AppearanceSerializer.Serialize(appearance), AppearanceSerializer.Serialize(restored));
        using var first = new PortraitRenderer(Pack.Value); using var second = new PortraitRenderer(Pack.Value);
        using var a = first.Render(appearance).Encode(); using var b = second.Render(restored).Encode();
        Assert.Equal(a.ToArray(), b.ToArray());
    }

    [Fact]
    public void Version_zero_migrates_but_unknown_future_version_is_rejected()
    {
        var appearance = new PortraitGenerator(Pack.Value).Generate(1);
        var old = AppearanceSerializer.Deserialize(AppearanceSerializer.Serialize(appearance with { Version = 0 }));
        Assert.Equal(1, old.Version);
        Assert.Throws<InvalidDataException>(() => AppearanceSerializer.Deserialize(AppearanceSerializer.Serialize(appearance with { Version = 99 })));
    }

    [Fact]
    public void Ten_thousand_seeds_are_compatible_and_rare_accessories_remain_rare()
    {
        var generator = new PortraitGenerator(Pack.Value); int accessories = 0;
        var faces = new HashSet<string>(); var hairstyles = new HashSet<string>();
        for (int seed = 0; seed < 10_000; seed++)
        {
            var appearance = generator.Generate(seed); Pack.Value.ValidateAppearance(appearance);
            Assert.True(Pack.Value.IsCompatible(appearance));
            faces.Add(appearance.Parts["Face"]); hairstyles.Add(appearance.Parts["HairFront"]);
            if (appearance.Parts.ContainsKey("Accessory")) accessories++;
        }
        Assert.Equal(12, faces.Count); Assert.Equal(4, hairstyles.Count);
        Assert.InRange(accessories, 900, 2000);
    }

    [Fact]
    public void Compatibility_rejects_hidden_earpiece_missing_layers_and_wrong_categories()
    {
        var appearance = new PortraitGenerator(Pack.Value).Generate(1);
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.Remove("Face") }));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.SetItem("Eyes", appearance.Parts["Face"]) }));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.SetItem("HairBack", "female.hairback.001").SetItem("Accessory", "female.accessory.003") }));
        Assert.False(Pack.Value.IsCompatible(appearance with { LibraryVersion = 99 }));
    }

    [Fact]
    public void Cache_reuses_identical_images_and_evicts_least_recently_used()
    {
        var generator = new PortraitGenerator(Pack.Value); using var renderer = new PortraitRenderer(Pack.Value);
        var appearance = generator.Generate(1);
        var image = renderer.Render(appearance, 128);
        Assert.Same(image, renderer.Render(appearance, 128));
        for (int seed = 2; seed <= 32; seed++) renderer.Render(generator.Generate(seed), 128);
        Assert.Same(image, renderer.Render(appearance, 128)); // make seed 1 most recent
        renderer.Render(generator.Generate(33), 128);
        Assert.Same(image, renderer.Render(appearance, 128));
        Assert.Equal(32, renderer.CachedCount);
        long before = renderer.CompositionCount;
        renderer.Render(generator.Generate(2), 128); // seed 2 was the least recently used
        Assert.Equal(before + 1, renderer.CompositionCount);
    }

    [Fact]
    public void Cache_key_includes_every_color_expression_and_resolution()
    {
        var appearance = new PortraitGenerator(Pack.Value).Generate(1);
        string Key(CharacterAppearance a, string expression = "neutral", int resolution = 512) => AppearanceSerializer.CacheKey(a, new(expression), resolution);
        Assert.NotEqual(Key(appearance), Key(appearance, "happy"));
        Assert.NotEqual(Key(appearance), Key(appearance, resolution: 128));
        Assert.NotEqual(Key(appearance), Key(appearance with { Colors = appearance.Colors.SetItem("Eyes", "#123456") }));
        var reordered = appearance.Colors.Reverse().ToImmutableSortedDictionary(p => p.Key, p => p.Value);
        Assert.Equal(Key(appearance), Key(appearance with { Colors = reordered }));
    }

    [Fact]
    public void Iris_recolor_preserves_sclera_and_transparent_background()
    {
        var appearance = new PortraitGenerator(Pack.Value).Generate(1);
        using var renderer = new PortraitRenderer(Pack.Value);
        using var first = SKBitmap.FromImage(renderer.Render(appearance));
        using var second = SKBitmap.FromImage(renderer.Render(appearance with { Colors = appearance.Colors.SetItem("Eyes", "#FFCC22") }));
        Assert.Equal((byte)0, first.GetPixel(0, 0).Alpha);
        Assert.Equal(first.GetPixel(185, 238), second.GetPixel(185, 238));
        Assert.NotEqual(first.GetPixel(202, 237), second.GetPixel(202, 237));
    }

    [Fact]
    public void Invalid_catalog_is_rejected_before_rendering()
    {
        var root = Path.Combine(Path.GetTempPath(), "dss-portrait-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.Copy(Path.Combine(PackRoot, "portrait-style.json"), Path.Combine(root, "portrait-style.json"));
            var first = Pack.Value.Parts[0];
            // Path traversal must be rejected, even when a target happens to exist.
            File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(new[] { first with { Texture = "../escape.png" } }, AppearanceSerializer.Options));
            Assert.Throws<InvalidDataException>(() => new PortraitAssetRepository(root, false));
            File.Copy(Pack.Value.TexturePath(first.Texture), Path.Combine(root, "part.png"));
            File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(new[] { first with { Texture = "part.png" }, first with { Texture = "part.png" } }, AppearanceSerializer.Options));
            Assert.Contains("duplicate", Assert.Throws<InvalidDataException>(() => new PortraitAssetRepository(root, false)).Message);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(1120, 748)] [InlineData(800, 600)] [InlineData(1920, 1080)]
    public void Portrait_click_generates_new_character_and_escape_closes_at_every_scale(int width, int height)
    {
        var screen = new TempCharacterImageScreen(PackRoot); screen.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, width, height); Assert.NotNull(screen.Appearance);
            var before = AppearanceSerializer.Serialize(screen.Appearance!);
            float scale = Math.Min(1, Math.Min(width / 1120f, height / 748f));
            float x = (width - 1120 * scale) / 2 + screen.PortraitRect.MidX * scale;
            float y = (height - 748 * scale) / 2 + screen.PortraitRect.MidY * scale;
            Assert.True(screen.OnMouseMove(x, y));
            Assert.Equal(ScreenEvent.None, screen.OnMouseDown(x, y, MouseButton.Right));
            Assert.Equal(before, AppearanceSerializer.Serialize(screen.Appearance!));
            screen.OnMouseDown(x, y, MouseButton.Left);
            Assert.NotEqual(before, AppearanceSerializer.Serialize(screen.Appearance!));
            Assert.Equal(ScreenEvent.CloseTempCharacterImage, screen.OnKeyDown(Key.Escape));
        }
        finally { screen.OnDeactivated(); }
    }

    [Fact]
    public void Editor_locks_face_undo_restores_and_json_round_trips()
    {
        string preset = Path.Combine(Path.GetTempPath(), "dss-portrait-" + Guid.NewGuid().ToString("N") + ".json");
        var screen = new TempCharacterImageScreen(PackRoot, preset); screen.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(1120, 748); using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1120, 748);
            var original = screen.Appearance!;
            screen.OnMouseDown(225, 182, MouseButton.Left); // face lock
            screen.OnMouseDown(500, 400, MouseButton.Left);
            Assert.Equal(original.Parts["Face"], screen.Appearance!.Parts["Face"]);
            screen.OnMouseDown(70, 597, MouseButton.Left); // undo
            Assert.Equal(AppearanceSerializer.Serialize(original), AppearanceSerializer.Serialize(screen.Appearance!));
            screen.OnMouseDown(860, 690, MouseButton.Left); // save JSON
            Assert.True(File.Exists(preset));
            screen.OnMouseDown(500, 400, MouseButton.Left);
            screen.OnMouseDown(1000, 690, MouseButton.Left); // load JSON
            Assert.Equal(AppearanceSerializer.Serialize(original), AppearanceSerializer.Serialize(screen.Appearance!));
        }
        finally { screen.OnDeactivated(); if (File.Exists(preset)) File.Delete(preset); }
    }

    [Fact]
    public void Game_screen_button_opens_portrait_workshop_without_panning()
    {
        var screen = new GameSessionScreen(new SnapshotBuffer(), new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var rect = screen.LastTempCharacterImageButtonRect;
        Assert.True(rect.Width >= 180);
        double before = screen.CameraFocusX;
        Assert.Equal(ScreenEvent.OpenTempCharacterImage, screen.OnMouseDown(rect.MidX, rect.MidY));
        Assert.Equal(before, screen.CameraFocusX);
        Assert.True(screen.OnMouseMove(rect.MidX, rect.MidY));
    }
}
