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
    private static readonly string PackRoot = Path.Combine(AppContext.BaseDirectory, "Images", "Persons", "W4");
    private static readonly Lazy<PortraitAssetRepository> Pack = new(() => new(PackRoot));
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
        Assert.Equal(new[] { "Clothes", "Portrait" }, original.Parts.Keys);
    }
    [Theory]
    [InlineData("Portrait", 208)]
    [InlineData("Clothes", 251)]
    public void Editor_cycles_all_variants_in_both_directions_without_changing_other_parts(string category, float y)
    {
        var screen = new TempCharacterImageScreen(PackRoot); screen.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(1120, 748); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1120, 748);
            Assert.NotNull(screen.Appearance);
            int count = Pack.Value.Parts.Count(p => p.Category == category);
            Assert.True(count > 1);
            var before = screen.Appearance; var seen = new HashSet<string>();
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
    public void Deleted_libraries_and_missing_or_wrong_components_are_rejected()
    {
        var generator = new PortraitGenerator(Pack.Value); var appearance = generator.Generate(1);
        foreach (int version in new[] { 1, 2, 3, 10, 11, 12, 99 }) Assert.Throws<InvalidDataException>(() => generator.Generate(1, version));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.Remove("Portrait") }));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.SetItem("Clothes", appearance.Parts["Portrait"]) }));
        Assert.False(Pack.Value.IsCompatible(appearance with { Parts = appearance.Parts.SetItem("Portrait", "female.face.001") }));
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
    public void Decoded_texture_cache_is_bounded_and_evicted_portraits_reload_identically()
    {
        using var fixture = new TemporaryPack(66);
        var assets = new PortraitAssetRepository(fixture.Root);
        var generated = new PortraitGenerator(assets).Generate(1);
        var baseline = generated with { Parts = generated.Parts.SetItem("Portrait", assets.Parts[0].Id) };
        using var renderer = new PortraitRenderer(assets);
        byte[] original;
        using (var encoded = renderer.Render(baseline, 32).Encode()) original = encoded.ToArray();
        foreach (var part in assets.Parts)
        {
            renderer.Render(baseline with { Parts = baseline.Parts.SetItem("Portrait", part.Id) }, 32);
            Assert.InRange(renderer.CachedTextureCount, 1, 64);
            Assert.InRange(renderer.CachedCount, 1, 2);
        }
        Assert.Equal(64, renderer.CachedTextureCount);
        // The first texture was evicted after walking more than 64 distinct files.
        long compositions = renderer.CompositionCount;
        using var reloaded = renderer.Render(baseline, 32).Encode();
        Assert.Equal(compositions + 1, renderer.CompositionCount);
        Assert.Equal(original, reloaded.ToArray());
        Assert.Equal(64, renderer.CachedTextureCount);
    }

    [Fact]
    public void Missing_pack_leaves_workshop_empty_and_safe_to_render_close_or_save()
    {
        using var fixture = new TemporaryPack(1);
        File.Delete(Path.Combine(fixture.Root, "portrait-style.json"));
        Assert.Throws<FileNotFoundException>(() => new PortraitAssetRepository(fixture.Root));
        string preset = Path.Combine(fixture.Root, "preset.json");
        var screen = new TempCharacterImageScreen(fixture.Root, preset);
        screen.OnActivated();
        try
        {
            Assert.Null(screen.Appearance);
            using var bitmap = new SKBitmap(1120, 748);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1120, 748);
            screen.OnMouseDown(500, 350, MouseButton.Left);
            screen.OnMouseDown(860, 690, MouseButton.Left);
            Assert.Null(screen.Appearance);
            Assert.False(File.Exists(preset));
            Assert.Equal(ScreenEvent.CloseTempCharacterImage, screen.OnKeyDown(Key.Escape));
        }
        finally { screen.OnDeactivated(); }
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
            Assert.Equal("Portrait", screen.SelectedLayer);
            float x = left + screen.PortraitRect.MidX * scale, y = top + screen.PortraitRect.MidY * scale;
            Assert.True(screen.OnMouseMove(x, y));
            screen.OnMouseDown(x, y, MouseButton.Right); Assert.Equal("Portrait", screen.SelectedLayer);
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

    // Small procedural textures test resource handling without restoring retired art packs.
    private sealed class TemporaryPack : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), $"dss-portrait-pack-{Guid.NewGuid():N}");

        public TemporaryPack(int count)
        {
            Directory.CreateDirectory(Path.Combine(Root, "Portraits"));
            var style = new PortraitStyleProfile
            {
                Width = 32,
                Height = 32,
                MaxCachedPortraits = 2,
                Layers = [new("Portrait")]
            };
            File.WriteAllText(Path.Combine(Root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
            var parts = new List<PortraitPart>();
            for (int i = 0; i < count; i++)
            {
                string texture = $"Portraits/portrait-{i:000}.png";
                using var bitmap = new SKBitmap(32, 32);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    using var paint = new SKPaint { Color = new SKColor((byte)(80 + i), 64, 120) };
                    canvas.DrawRect(1, 1, 30, 30, paint);
                }
                using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(Path.Combine(Root, texture));
                png.SaveTo(file);
                parts.Add(new PortraitPart { Id = $"test.portrait.{i:000}", Category = "Portrait", Texture = texture });
            }
            File.WriteAllText(Path.Combine(Root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
