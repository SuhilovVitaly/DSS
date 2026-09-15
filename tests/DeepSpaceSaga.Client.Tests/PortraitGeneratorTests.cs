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
    private static readonly string PackRoot = Path.Combine(AppContext.BaseDirectory, "Images", "Persons", "W", "PortraitGenerator");
    private static readonly Lazy<PortraitAssetRepository> Pack = new(() => new(PackRoot));
    [Fact]
    public void Library_has_three_requested_variants_and_preserves_the_bald_baseline()
    {
        Assert.Equal(10, Pack.Value.Style.LibraryVersion);
        Assert.Equal(new[] { 4, 5, 6, 7, 8, 9, 10 }, Pack.Value.Style.SupportedLibraryVersions);
        Assert.Equal(89, Pack.Value.Parts.Count);
        Assert.Equal(16, Pack.Value.Parts.Count(p => p.Category == "Face"));
        Assert.Equal(10, Pack.Value.Parts.Count(p => p.Category == "Face" && p.IntroducedInVersion == 9));
        Assert.Equal(11, Pack.Value.Parts.Count(p => p.Category == "Eyebrows"));
        Assert.Equal(20, Pack.Value.Parts.Count(p => p.Category == "Eyes"));
        foreach (string category in new[] { "Nose", "Mouth" }) Assert.Equal(13, Pack.Value.Parts.Count(p => p.Category == category));
        foreach (string category in new[] { "Chin", "Clothes" }) Assert.Equal(3, Pack.Value.Parts.Count(p => p.Category == category));
        Assert.Equal(10, Pack.Value.Parts.Count(p => p.Category == "Hair"));
        Assert.All(Pack.Value.Parts, p => Assert.StartsWith("female.frontal.", p.Id));
        Assert.Empty(Pack.Value.Style.Palettes);
    }
    [Fact]
    public void Reassembly_preserves_master_silhouette_and_feature_core_pixels()
    {
        using var renderer = new PortraitRenderer(Pack.Value);
        using var actual = SKBitmap.FromImage(renderer.Render(new PortraitGenerator(Pack.Value).Generate(1, 4), 1024));
        using var master = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "GoldenPortraits", "master.png"));
        long error = 0; int samples = 0;
        for (int y = 0; y < 1024; y++)
            for (int x = 0; x < 1024; x++)
            {
                var a = actual.GetPixel(x, y); var b = master.GetPixel(x, y);
                Assert.Equal(b.Alpha, a.Alpha);
                if (b.Alpha == 255) { error += Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue); samples += 3; }
            }
        Assert.InRange(error / (double)samples, 0, 12);
        foreach (var point in new[] { (480, 517), (760, 517), (477, 438), (765, 438), (618, 710), (617, 826) })
        {
            int cx = (int)(point.Item1 * 1024f / 1254), cy = (int)(point.Item2 * 1024f / 1254);
            for (int y = cy - 4; y <= cy + 4; y++)
                for (int x = cx - 4; x <= cx + 4; x++) Assert.Equal(master.GetPixel(x, y), actual.GetPixel(x, y));
        }
    }
    [Fact]
    public void Reviewed_landmarks_have_frontal_alignment_and_adult_vertical_proportions()
    {
        var a = Pack.Value.Style.Anchors;
        float eyes = (a["EyeLeft"].Y + a["EyeRight"].Y) / 2;
        Assert.InRange((eyes - a["Crown"].Y) / (a["Chin"].Y - a["Crown"].Y), .45f, .55f);
        Assert.InRange((a["NoseBase"].Y - a["Brow"].Y) / (a["Chin"].Y - a["NoseBase"].Y), .85f, 1.15f);
        Assert.InRange((a["Mouth"].Y - a["NoseBase"].Y) / (a["Chin"].Y - a["NoseBase"].Y), .25f, .4f);
        Assert.InRange(Math.Abs(a["EyeLeft"].Y - a["EyeRight"].Y), 0, .005f);
        Assert.InRange(Math.Abs(a["NoseBase"].X - (a["EyeLeft"].X + a["EyeRight"].X) / 2), 0, .01f);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MinValue)]
    public void Seed_and_json_round_trip_preserve_selected_variants(int seed)
    {
        var generator = new PortraitGenerator(Pack.Value);
        var original = generator.Generate(seed);
        var restored = AppearanceSerializer.Deserialize(AppearanceSerializer.Serialize(original));
        Assert.Equal(AppearanceSerializer.Serialize(original), AppearanceSerializer.Serialize(generator.Generate(seed)));
        Assert.Equal(AppearanceSerializer.Serialize(original), AppearanceSerializer.Serialize(restored));
        using var renderer = new PortraitRenderer(Pack.Value);
        using var a = renderer.Render(original).Encode(); using var b = renderer.Render(restored).Encode();
        Assert.Equal(a.ToArray(), b.ToArray());
        Assert.Equal(8, original.Parts.Count);
    }
    [Fact]
    public void All_810_combinations_render_distinctly_without_a_hole_between_neck_and_collar()
    {
        using var renderer = new PortraitRenderer(Pack.Value);
        var baseline = new PortraitGenerator(Pack.Value).Generate(1, 6);
        var hashes = new HashSet<string>();
        foreach (var eyes in Pack.Value.Parts.Where(p => p.Category == "Eyes" && p.IntroducedInVersion <= 6))
            foreach (var lips in Pack.Value.Parts.Where(p => p.Category == "Mouth" && p.IntroducedInVersion <= 6))
                foreach (var nose in Pack.Value.Parts.Where(p => p.Category == "Nose" && p.IntroducedInVersion <= 6))
                    foreach (var chin in Pack.Value.Parts.Where(p => p.Category == "Chin"))
                        foreach (var suit in Pack.Value.Parts.Where(p => p.Category == "Clothes"))
                        {
                            var a = baseline with { Parts = baseline.Parts.SetItem("Eyes", eyes.Id).SetItem("Mouth", lips.Id).SetItem("Nose", nose.Id).SetItem("Chin", chin.Id).SetItem("Clothes", suit.Id) };
                            Assert.True(Pack.Value.IsCompatible(a));
                            foreach (var hair in Pack.Value.Parts.Where(p => p.Category == "Hair"))
                            {
                                Assert.True(Pack.Value.IsCompatible(a with { LibraryVersion = 7, Parts = a.Parts.SetItem("Hair", hair.Id) }));
                                foreach (var face in Pack.Value.Parts.Where(p => p.Category == "Face"))
                                    Assert.True(Pack.Value.IsCompatible(a with { LibraryVersion = 9, Parts = a.Parts.SetItem("Hair", hair.Id).SetItem("Face", face.Id) }));
                            }
                            var rendered = renderer.Render(a, 256);
                            using var bitmap = SKBitmap.FromImage(rendered);
                            for (int y = 142; y <= 164; y++)
                                for (int x = 106; x <= 148; x++) Assert.Equal(255, bitmap.GetPixel(x, y).Alpha);
                            using var encoded = rendered.Encode();
                            Assert.True(hashes.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(encoded.ToArray()))));
                        }
        Assert.Equal(810, hashes.Count);
    }
    [Fact]
    public void Eye_changes_preserve_pixels_outside_the_eye_region_and_legacy_selection()
    {
        var generator = new PortraitGenerator(Pack.Value); using var renderer = new PortraitRenderer(Pack.Value);
        var appearance = generator.Generate(1, 9) with { LibraryVersion = 10 };
        using var original = SKBitmap.FromImage(renderer.Render(appearance, 1024));
        byte[] reference = original.Bytes;
        foreach (var eyes in Pack.Value.Parts.Where(p => p.Category == "Eyes"))
        {
            using var changed = SKBitmap.FromImage(renderer.Render(appearance with { Parts = appearance.Parts.SetItem("Eyes", eyes.Id) }, 1024));
            byte[] bytes = changed.Bytes;
            Assert.True(reference.AsSpan(0, original.RowBytes * 250).SequenceEqual(bytes.AsSpan(0, changed.RowBytes * 250)));
            Assert.True(reference.AsSpan(original.RowBytes * 360).SequenceEqual(bytes.AsSpan(changed.RowBytes * 360)));
        }
        for (int seed = 0; seed < 20; seed++)
        {
            var legacy = generator.Generate(seed, 5);
            Assert.Equal("female.frontal.eyes.001", legacy.Parts["Eyes"]);
            Assert.False(Pack.Value.IsCompatible(legacy with { Parts = legacy.Parts.SetItem("Eyes", "female.frontal.eyes.002") }));
            using var oldImage = renderer.Render(legacy).Encode();
            using var upgradedImage = renderer.Render(legacy with { LibraryVersion = 6 }).Encode();
            Assert.Equal(oldImage.ToArray(), upgradedImage.ToArray());
        }
    }
    [Theory]
    [InlineData("Eyes", 293, 20)]
    [InlineData("Eyebrows", 251, 11)]
    [InlineData("Nose", 337, 13)]
    [InlineData("Mouth", 380, 13)]
    [InlineData("Hair", 508, 10)]
    [InlineData("Face", 208, 16)]
    public void Editor_cycles_all_variants_in_both_directions_without_changing_other_parts(string category, float y, int count)
    {
        var screen = new TempCharacterImageScreen(PackRoot); screen.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(1120, 748); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1120, 748);
            var before = screen.Appearance!; var seen = new HashSet<string>();
            for (int n = 0; n < count; n++)
            {
                seen.Add(screen.Appearance!.Parts[category]);
                screen.OnMouseDown(234, y, MouseButton.Left);
                foreach (var part in before.Parts.Where(p => p.Key != category)) Assert.Equal(part.Value, screen.Appearance!.Parts[part.Key]);
            }
            Assert.Equal(count, seen.Count); Assert.Equal(before.Parts[category], screen.Appearance!.Parts[category]);
            screen.OnMouseDown(198, y, MouseButton.Left); Assert.NotEqual(before.Parts[category], screen.Appearance!.Parts[category]);
            screen.OnMouseDown(234, y, MouseButton.Left); Assert.Equal(before.Parts[category], screen.Appearance!.Parts[category]);
        }
        finally { screen.OnDeactivated(); }
    }
    [Fact]
    public void Hairstyles_preserve_all_eye_pairs_and_the_central_collar()
    {
        var generator = new PortraitGenerator(Pack.Value); using var renderer = new PortraitRenderer(Pack.Value);
        var baseline = generator.Generate(1, 6);
        Assert.DoesNotContain("Hair", baseline.Parts.Keys);
        foreach (var eyes in Pack.Value.Parts.Where(p => p.Category == "Eyes" && p.IntroducedInVersion <= 6))
        {
            var bald = baseline with { Parts = baseline.Parts.SetItem("Eyes", eyes.Id) };
            using var reference = SKBitmap.FromImage(renderer.Render(bald, 256));
            foreach (var hair in Pack.Value.Parts.Where(p => p.Category == "Hair"))
            {
                var selected = bald with { LibraryVersion = 7, Parts = bald.Parts.SetItem("Hair", hair.Id) };
                using var portrait = SKBitmap.FromImage(renderer.Render(selected, 256));
                foreach (var point in new[] { (108, 72), (146, 72), (127, 154), (127, 166) })
                    for (int y = point.Item2 - 2; y <= point.Item2 + 2; y++)
                        for (int x = point.Item1 - 2; x <= point.Item1 + 2; x++) Assert.Equal(reference.GetPixel(x, y), portrait.GetPixel(x, y));
            }
        }
        Assert.False(Pack.Value.IsCompatible(baseline with { LibraryVersion = 7 }));
        foreach (var hair in Pack.Value.Parts.Where(p => p.Category == "Hair"))
        {
            using var texture = SKBitmap.Decode(Pack.Value.TexturePath(hair.Texture));
            Assert.Equal(0, texture.GetPixel(512, 700).Alpha);
            Assert.Equal(255, texture.GetPixel(500, 80).Alpha);
        }
    }
    [Fact]
    public void Distinct_faces_have_their_own_jaws_and_preserve_all_three_collar_seams()
    {
        var assets = Pack.Value; var generator = new PortraitGenerator(assets);
        using var renderer = new PortraitRenderer(assets);
        var baseline = generator.Generate(1, 9);
        baseline = baseline with { Parts = baseline.Parts.SetItem("Hair", "female.frontal.hair.007") };
        var chinHashes = new HashSet<string>();
        foreach (var face in assets.Parts.Where(p => p.Category == "Face"))
            foreach (var chin in assets.Parts.Where(p => p.Category == "Chin"))
            {
                string path = chin.TextureFor(9, face.Id);
                using var texture = SKBitmap.Decode(assets.TexturePath(path));
                using var original = SKBitmap.Decode(assets.TexturePath(chin.Texture));
                Assert.True(chinHashes.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(texture.Bytes))));
                for (int y = 607; y < 684; y++)
                    for (int x = 350; x < 670; x++) Assert.Equal(original.GetPixel(x, y), texture.GetPixel(x, y));
                foreach (var suit in assets.Parts.Where(p => p.Category == "Clothes"))
                {
                    var a = baseline with { Parts = baseline.Parts.SetItem("Face", face.Id).SetItem("Chin", chin.Id).SetItem("Clothes", suit.Id) };
                    using var portrait = SKBitmap.FromImage(renderer.Render(a, 256));
                    for (int y = 142; y <= 164; y++)
                        for (int x = 106; x <= 148; x++) Assert.Equal(255, portrait.GetPixel(x, y).Alpha);
                }
            }
        Assert.Equal(48, chinHashes.Count);
        using var fuller = SKBitmap.Decode(assets.TexturePath(assets.Get("female.frontal.face.002").Texture));
        using var slender = SKBitmap.Decode(assets.TexturePath(assets.Get("female.frontal.face.003").Texture));
        int Width(SKBitmap bitmap) => Enumerable.Range(0, 1024).Count(x => bitmap.GetPixel(x, 475).Alpha > 240);
        Assert.True(Width(fuller) > Width(slender) + 20, $"Cheek widths: full={Width(fuller)}, slender={Width(slender)}");
        for (int version = 4; version <= 7; version++)
        {
            var legacy = generator.Generate(1, version);
            Assert.Equal("female.frontal.face.001", legacy.Parts["Face"]);
            Assert.False(assets.IsCompatible(legacy with { Parts = legacy.Parts.SetItem("Face", "female.frontal.face.002") }));
            if (version != 7) continue;
            using var before = renderer.Render(legacy).Encode();
            using var upgraded = renderer.Render(legacy with { LibraryVersion = 8 }).Encode();
            Assert.Equal(before.ToArray(), upgraded.ToArray());
        }
    }
    [Fact]
    public void Reference_face_library_preserves_v8_portraits_and_bounds_decoded_texture_memory()
    {
        var assets = Pack.Value; var generator = new PortraitGenerator(assets);
        using var renderer = new PortraitRenderer(assets);
        for (int f = 1; f <= 6; f++)
        {
            var legacy = generator.Generate(f, 8);
            legacy = legacy with { Parts = legacy.Parts.SetItem("Face", $"female.frontal.face.{f:000}") };
            using var original = renderer.Render(legacy, 256).Encode();
            using var current = renderer.Render(legacy with { LibraryVersion = 9 }, 256).Encode();
            Assert.Equal(original.ToArray(), current.ToArray());
            Assert.False(assets.IsCompatible(legacy with { Parts = legacy.Parts.SetItem("Face", "female.frontal.face.007") }));
        }
        var baseline = generator.Generate(1, 9);
        byte[] first;
        using (var image = renderer.Render(baseline, 256).Encode()) first = image.ToArray();
        for (int f = 1; f <= 16; f++)
            for (int v = 1; v <= 3; v++)
            {
                var selected = baseline with { Parts = baseline.Parts.SetItem("Face", $"female.frontal.face.{f:000}") };
                foreach (string category in new[] { "Chin", "Nose", "Mouth" }) selected = selected with { Parts = selected.Parts.SetItem(category, $"female.frontal.{category.ToLowerInvariant()}.{v:000}") };
                renderer.Render(selected, 256);
                Assert.InRange(renderer.CachedTextureCount, 1, 64);
            }
        using var reloaded = renderer.Render(baseline, 256).Encode();
        Assert.Equal(first, reloaded.ToArray());
    }
    [Fact]
    public void Reference_features_are_independent_on_every_face_and_preserve_neck_hair_and_legacy_portraits()
    {
        var assets = Pack.Value; var generator = new PortraitGenerator(assets);
        using var renderer = new PortraitRenderer(assets);
        var additions = assets.Parts.Where(p => p.IntroducedInVersion == 10).ToArray();
        Assert.Equal(40, additions.Length);
        Assert.All(additions, p => Assert.Equal(16, p.FaceTextures.Count));
        var baseline = generator.Generate(1, 9);
        for (int face = 1; face <= 16; face++)
        {
            var legacy = baseline with { Parts = baseline.Parts.SetItem("Face", $"female.frontal.face.{face:000}") };
            using var oldImage = renderer.Render(legacy, 256).Encode();
            var current = legacy with { LibraryVersion = 10 };
            using var original = SKBitmap.FromImage(renderer.Render(current, 256));
            using var upgraded = original.Encode(SKEncodedImageFormat.Png, 100);
            Assert.Equal(oldImage.ToArray(), upgraded.ToArray());
            foreach (var group in additions.GroupBy(p => p.Category))
            {
                var hashes = new HashSet<string>();
                foreach (var part in group)
                {
                    Assert.False(assets.IsCompatible(legacy with { Parts = legacy.Parts.SetItem(part.Category, part.Id) }));
                    var selected = current with { Parts = current.Parts.SetItem(part.Category, part.Id) };
                    Assert.True(assets.IsCompatible(selected));
                    using var changed = SKBitmap.FromImage(renderer.Render(selected, 256));
                    for (int y = 0; y < 256; y++)
                        for (int x = 0; x < 256; x++)
                            if (x < 85 || x > 170 || y < 45 || y > 145)
                                Assert.True(original.GetPixel(x, y) == changed.GetPixel(x, y), $"Face {face}, {part.Id}, pixel ({x},{y}) changed outside the feature region.");
                    Assert.True(hashes.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(changed.Bytes))));
                    Assert.InRange(renderer.CachedTextureCount, 1, 64);
                }
                Assert.Equal(10, hashes.Count);
            }
        }
    }
    [Fact]
    public void Every_chin_keeps_exactly_the_same_neck_at_the_front_collar_seam()
    {
        var chins = Pack.Value.Parts.Where(p => p.Category == "Chin").ToArray();
        using var first = SKBitmap.Decode(Pack.Value.TexturePath(chins[0].Texture));
        foreach (var chin in chins.Skip(1))
        {
            using var other = SKBitmap.Decode(Pack.Value.TexturePath(chin.Texture));
            for (int y = 607; y < 684; y++)
                for (int x = 350; x < 670; x++) Assert.Equal(first.GetPixel(x, y), other.GetPixel(x, y));
        }
    }
    [Fact]
    public void Editor_cycles_only_the_requested_part_and_randomizes_on_portrait_click()
    {
        var screen = new TempCharacterImageScreen(PackRoot); screen.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(1120, 748); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1120, 748);
            var before = screen.Appearance!;
            screen.OnMouseDown(234, 337, MouseButton.Left); // Nose, next.
            var after = screen.Appearance!;
            Assert.NotEqual(before.Parts["Nose"], after.Parts["Nose"]);
            foreach (var part in before.Parts.Where(p => p.Key != "Nose")) Assert.Equal(part.Value, after.Parts[part.Key]);
            screen.OnMouseDown(198, 337, MouseButton.Left); // Previous restores the same nose, regardless of library size.
            Assert.Equal(before.Parts["Nose"], screen.Appearance!.Parts["Nose"]);
            screen.OnMouseDown(500, 350, MouseButton.Left);
            Assert.False(before.Parts.SequenceEqual(screen.Appearance!.Parts));
        }
        finally { screen.OnDeactivated(); }
    }
    [Fact]
    public void Deleted_libraries_and_missing_or_wrong_components_are_rejected()
    {
        var generator = new PortraitGenerator(Pack.Value); var appearance = generator.Generate(1);
        foreach (int version in new[] { 1, 2, 3, 99 }) Assert.Throws<InvalidDataException>(() => generator.Generate(1, version));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.Remove("Eyes") }));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.SetItem("Nose", appearance.Parts["Face"]) }));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.SetItem("Face", "female.face.001") }));
    }
    [Fact]
    public void Cache_is_bounded_and_reuses_a_rendered_portrait()
    {
        var generator = new PortraitGenerator(Pack.Value); using var renderer = new PortraitRenderer(Pack.Value);
        var first = generator.Generate(1); var image = renderer.Render(first, 128);
        Assert.Same(image, renderer.Render(first, 128));
        for (int i = 2; i <= 33; i++) renderer.Render(generator.Generate(i), 128);
        Assert.Equal(32, renderer.CachedCount);
        long before = renderer.CompositionCount; renderer.Render(first, 128);
        Assert.Equal(before + 1, renderer.CompositionCount);
    }
    [Fact]
    public void Invalid_catalog_paths_are_rejected()
    {
        string root = Path.Combine(Path.GetTempPath(), "dss-portrait-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.Copy(Path.Combine(PackRoot, "portrait-style.json"), Path.Combine(root, "portrait-style.json"));
            File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(new[] { Pack.Value.Parts[0] with { Texture = "../escape.png" } }, AppearanceSerializer.Options));
            Assert.Throws<InvalidDataException>(() => new PortraitAssetRepository(root, false));
        }
        finally { Directory.Delete(root, true); }
    }
    [Theory]
    [InlineData(1120, 748)]
    [InlineData(800, 600)]
    [InlineData(1920, 1080)]
    public void Layers_can_be_inspected_and_portrait_reassembled_at_every_scale(int width, int height)
    {
        var screen = new TempCharacterImageScreen(PackRoot); screen.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, width, height);
            Assert.NotNull(screen.Appearance);
            float scale = Math.Min(1, Math.Min(width / 1120f, height / 748f));
            float left = (width - 1120 * scale) / 2, top = (height - 748 * scale) / 2;
            screen.OnMouseDown(left + 120 * scale, top + 209 * scale, MouseButton.Left);
            Assert.Equal("Face", screen.SelectedLayer);
            float x = left + screen.PortraitRect.MidX * scale, y = top + screen.PortraitRect.MidY * scale;
            Assert.True(screen.OnMouseMove(x, y));
            screen.OnMouseDown(x, y, MouseButton.Right); Assert.Equal("Face", screen.SelectedLayer);
            screen.OnMouseDown(x, y, MouseButton.Left); Assert.Null(screen.SelectedLayer);
            Assert.Equal(ScreenEvent.CloseTempCharacterImage, screen.OnKeyDown(Key.Escape));
        }
        finally { screen.OnDeactivated(); }
    }
    [Fact]
    public void Editor_saves_current_portrait_and_rejects_an_old_preset_without_losing_it()
    {
        string path = Path.Combine(Path.GetTempPath(), "dss-portrait-" + Guid.NewGuid().ToString("N") + ".json");
        var screen = new TempCharacterImageScreen(PackRoot, path); screen.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(1120, 748); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1120, 748);
            string original = AppearanceSerializer.Serialize(screen.Appearance!);
            screen.OnMouseDown(860, 690, MouseButton.Left); Assert.Equal(original, File.ReadAllText(path));
            screen.OnMouseDown(1000, 690, MouseButton.Left); Assert.Equal(original, AppearanceSerializer.Serialize(screen.Appearance!));
            File.WriteAllText(path, AppearanceSerializer.Serialize(screen.Appearance! with { LibraryVersion = 3 }));
            screen.OnMouseDown(1000, 690, MouseButton.Left); Assert.Equal(original, AppearanceSerializer.Serialize(screen.Appearance!));
        }
        finally { screen.OnDeactivated(); if (File.Exists(path)) File.Delete(path); }
    }
    [Fact]
    public void Workshop_opened_from_game_survives_queued_clicks_and_loads_the_shipped_pack()
    {
        var screen = new TempCharacterImageScreen();
        screen.OnActivated();
        try
        {
            Assert.NotNull(screen.Appearance);
            // The bottom game-screen button is outside the centered workshop.
            // A second queued click must not dismiss the newly opened modal.
            Assert.Equal(ScreenEvent.None, screen.OnMouseDown(1720, 1300, MouseButton.Left));
            using var bitmap = new SKBitmap(3440, 1380);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 3440, 1380);
            Assert.Equal(ScreenEvent.None, screen.OnMouseDown(1720, 1300, MouseButton.Left));
            Assert.Equal(ScreenEvent.CloseTempCharacterImage, screen.OnMouseDown(1160 + 1060, 316 + 40, MouseButton.Left));
            Assert.Equal(ScreenEvent.CloseTempCharacterImage, screen.OnKeyDown(Key.Escape));
        }
        finally { screen.OnDeactivated(); }
    }
    [Fact]
    public void Game_screen_button_opens_portrait_workshop_without_panning()
    {
        var screen = new GameSessionScreen(new SnapshotBuffer(), new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1080);
        var rect = screen.LastTempCharacterImageButtonRect; Assert.True(rect.Width >= 180);
        double before = screen.CameraFocusX;
        Assert.Equal(ScreenEvent.OpenTempCharacterImage, screen.OnMouseDown(rect.MidX, rect.MidY));
        Assert.Equal(before, screen.CameraFocusX); Assert.True(screen.OnMouseMove(rect.MidX, rect.MidY));
    }
}
