using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

internal static class FaceAssets
{
    private static readonly string[] Labels = ["Классический овал", "Полное округлое", "Худое вытянутое", "Широкая челюсть", "Сердцевидное", "Выраженные скулы", "Короткое мягкое", "Атлетическая челюсть", "Удлинённое полное", "Зрелое широкое", "Нежное узкое", "Короткое прямоугольное", "Длинное угловатое", "Широкое округлое", "Узкое ромбовидное", "Широкое трапециевидное"];
    public static bool HasSources(string root) => Enumerable.Range(2, 5).All(f => Enumerable.Range(1, 3).All(v => File.Exists(Path.Combine(root, Source(f, v)))));
    private static bool HasReferenceSources(string root) => Enumerable.Range(7, 10).All(f => Enumerable.Range(1, 3).All(v => File.Exists(Path.Combine(root, Source(f, v)))));
    private static string Source(int face, int variant) => $"Sources/faces/face-{face:00}-{variant:00}.png";
    public static void Build(string root)
    {
        var assets = new PortraitAssetRepository(root);
        if (assets.Style.LibraryVersion < 7 || !HasSources(root)) throw new InvalidDataException("Build v7 and all five face sources first.");
        bool extended = HasReferenceSources(root);
        if (assets.Style.LibraryVersion >= 9 && !extended) throw new InvalidDataException("The v9 reference face sources are incomplete.");
        int count = extended ? 16 : 6, version = extended ? 9 : 8;
        var parts = assets.Parts.Where(p => p.IntroducedInVersion < 8).Select(p => p with { FaceTextures = [] }).ToList();
        int first = parts.FindIndex(p => p.Category == "Face");
        parts[first] = parts[first] with { DisplayName = Labels[0] };
        using var canonical = SKBitmap.Decode(Path.Combine(root, "Golden/master.png"));
        for (int f = 2; f <= count; f++)
        {
            string faceId = $"female.frontal.face.{f:000}";
            using var original = Read(f, 1);
            using var upper = original.Copy();
            for (int y = 735; y < 1024; y++)
                for (int x = 0; x < 1024; x++)
                {
                    var c = upper.GetPixel(x, y);
                    upper.SetPixel(x, y, c.WithAlpha((byte)(c.Alpha * Math.Clamp((755 - y) / 20f, 0, 1))));
                }
            string facePath = $"Parts/faces/{f:00}/face.png";
            using (var placed = VariantAssets.Place(upper)) VariantAssets.Save(root, placed, facePath);
            parts.Add(new() { Id = faceId, Category = "Face", Texture = facePath, DisplayName = Labels[f - 1], IntroducedInVersion = f <= 6 ? 8 : 9 });
            for (int v = 1; v <= 3; v++)
            {
                using var edited = v == 1 ? original.Copy() : Read(f, v);
                using var chin = edited.Copy();
                for (int y = 0; y < 1024; y++)
                    for (int x = 0; x < 1024; x++)
                    {
                        // Keep the new woman's jaw, then join the existing collar's
                        // canonical neck. Every identity has identical seam pixels.
                        var c = VariantAssets.Blend(edited.GetPixel(x, y), canonical.GetPixel(x, y), Math.Clamp((y - 870) / 30f, 0, 1));
                        chin.SetPixel(x, y, c.WithAlpha((byte)(c.Alpha * Math.Clamp((y - 710) / 20f, 0, 1))));
                    }
                Attach("Chin", v, chin);
                foreach (string category in v == 1 ? new[] { "Eyebrows", "Eyes", "Nose", "Mouth" } : new[] { "Nose", "Mouth" })
                {
                    using var mask = SKBitmap.Decode(Path.Combine(root, $"Masks/{category.ToLowerInvariant()}.png"));
                    using var feature = edited.Copy();
                    using (var canvas = new SKCanvas(feature))
                    using (var paint = new SKPaint { BlendMode = SKBlendMode.DstIn }) canvas.DrawBitmap(mask, 0, 0, paint);
                    Attach(category, v, feature);
                }
            }
            if (f >= 7)
                foreach (var eye in FaceEyeAssets.Build(root, f, original))
                    parts.First(p => p.Id == $"female.frontal.eyes.{eye.Key:000}").FaceTextures.Add(faceId, eye.Value);
            void Attach(string category, int variant, SKBitmap texture)
            {
                string path = $"Parts/faces/{f:00}/{category.ToLowerInvariant()}-{variant:00}.png";
                using var placed = VariantAssets.Place(texture); VariantAssets.Save(root, placed, path);
                int index = parts.FindIndex(p => p.Id == $"female.frontal.{category.ToLowerInvariant()}.{variant:000}");
                parts[index].FaceTextures.Add(faceId, path);
            }
        }
        var style = assets.Style with { LibraryVersion = version, SupportedLibraryVersions = Enumerable.Range(4, version - 3).ToArray() };
        File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
        Console.WriteLine($"Built {count} face identities with matching brows, base eyes, noses, lips and three chins each; v{version}, {count * 8100} combinations.");
        SKBitmap Read(int f, int v)
        {
            using var source = SKBitmap.Decode(Path.Combine(root, Source(f, v)));
            if (source is null || source.Width != 1254 || source.Height != 1254) throw new InvalidDataException($"Invalid face source {f}/{v}.");
            using var cut = VariantAssets.CutBackground(source);
            return cut.Resize(new SKImageInfo(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
        }
    }
    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        int count = assets.Parts.Count(p => p.Category == "Face");
        var baseline = new PortraitGenerator(assets).Generate(1);
        foreach (string category in new[] { "Face", "Eyes", "Nose", "Mouth", "Chin", "Clothes", "Hair" }) baseline = Set(baseline, category, 1);
        using var labels = new SKPaint { Color = SKColors.White, TextSize = 17, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        Gallery("Generated/faces-comparison.png", 3, 2, 512, (i) => Set(Set(baseline, "Hair", 7), "Face", i + 1), i => Labels[i]);
        Gallery("Generated/faces-examples.png", 3, 2, 512, i => Set(Set(Set(Set(baseline, "Face", i + 1), "Hair", new[] { 1, 3, 6, 7, 8, 10 }[i]), "Eyes", new[] { 1, 3, 5, 7, 9, 10 }[i]), "Clothes", i % 3 + 1), i => Labels[i]);
        Gallery("Generated/faces-hair.png", 10, count, 256, i => Set(Set(baseline, "Face", i / 10 + 1), "Hair", i % 10 + 1), i => $"Лицо {i / 10 + 1} · Волосы {i % 10 + 1}");
        Gallery("Generated/faces-collars.png", 9, count, 256, i => Set(Set(Set(Set(baseline, "Face", i / 9 + 1), "Chin", i % 9 / 3 + 1), "Clothes", i % 3 + 1), "Hair", 7), i => $"Л{i / 9 + 1} П{i % 9 / 3 + 1} К{i % 3 + 1}");
        Gallery("Generated/faces-eyes.png", 10, count, 256, i => Set(Set(Set(baseline, "Face", i / 10 + 1), "Eyes", i % 10 + 1), "Hair", 7), i => $"Лицо {i / 10 + 1} · Глаза {i % 10 + 1}");
        if (count == 16)
        {
            Gallery("Generated/reference-faces-comparison.png", 5, 2, 400, i => Set(Set(baseline, "Face", i + 7), "Hair", 7), i => $"{i + 7}. {Labels[i + 6]}");
            Gallery("Generated/reference-faces-examples.png", 5, 2, 400, i => Set(Set(Set(baseline, "Face", i + 7), "Hair", i + 1), "Clothes", i % 3 + 1), i => $"{i + 7}. {Labels[i + 6]}");
            for (int batch = 0; batch < 2; batch++)
            {
                int offset = 7 + batch * 5;
                Gallery($"Generated/reference-hair-{batch + 1}.png", 10, 5, 256, i => Set(Set(baseline, "Face", offset + i / 10), "Hair", i % 10 + 1), i => $"Л{offset + i / 10} В{i % 10 + 1}");
                Gallery($"Generated/reference-eyes-{batch + 1}.png", 10, 5, 256, i => Set(Set(Set(baseline, "Face", offset + i / 10), "Eyes", i % 10 + 1), "Hair", 7), i => $"Л{offset + i / 10} Г{i % 10 + 1}");
                Gallery($"Generated/reference-collars-{batch + 1}.png", 9, 5, 256, i => Set(Set(Set(Set(baseline, "Face", offset + i / 9), "Chin", i % 9 / 3 + 1), "Clothes", i % 3 + 1), "Hair", 7), i => $"Л{offset + i / 9} П{i % 9 / 3 + 1} К{i % 3 + 1}");
            }
        }
        for (int f = 1; f <= count; f++)
        {
            var a = Set(Set(baseline, "Face", f), "Hair", 7);
            using var bitmap = SKBitmap.FromImage(renderer.Render(a, 1024));
            VariantAssets.Save(root, bitmap, $"Generated/faces/portrait-{f:00}.png");
        }
        void Gallery(string path, int columns, int rows, int cell, Func<int, CharacterAppearance> appearance, Func<int, string> label)
        {
            using var bitmap = new SKBitmap(columns * cell, rows * cell); using var canvas = new SKCanvas(bitmap);
            canvas.Clear(new SKColor(28, 40, 54));
            for (int i = 0; i < columns * rows; i++)
            {
                int x = i % columns * cell, y = i / columns * cell;
                canvas.DrawImage(renderer.Render(appearance(i), cell), x, y);
                canvas.DrawText(label(i), x + 12, y + 25, labels);
            }
            VariantAssets.Save(root, bitmap, path);
        }
        Console.WriteLine($"Rendered face comparison, mixed examples, {count * 10} hairstyles, {count * 10} eye pairs and {count * 9} chin/collar combinations.");
    }
    private static CharacterAppearance Set(CharacterAppearance a, string category, int n) => a with { Parts = a.Parts.SetItem(category, $"female.frontal.{category.ToLowerInvariant()}.{n:000}") };
}
