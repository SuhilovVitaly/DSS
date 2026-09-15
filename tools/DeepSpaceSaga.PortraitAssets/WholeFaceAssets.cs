using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

/// <summary>W1: a complete face is one sprite, never independently mixed features.</summary>
internal static class WholeFaceAssets
{
    private static readonly string[] OvalNames = ["Мягкий овал", "Полный овал", "Узкий овал"];
    private static readonly string[] FaceNames = ["Спокойная", "Приветливая", "Утончённая", "Уверенная", "Нежная", "Живая", "Мягкая", "Задумчивая", "Открытая", "Светлая"];
    private static string Id(string category, int n) => $"female.w1.{category.ToLowerInvariant()}.{n:000}";
    public static void Build(string root)
    {
        string legacyRoot = Path.GetFullPath(Path.Combine(root, "../W/PortraitGenerator"));
        var legacy = new PortraitAssetRepository(legacyRoot, validateTextures: false);
        var parts = new List<PortraitPart>();
        using var canonical = Read(root, "oval-01.png");
        var faceFootprint = Enumerable.Repeat((byte)255, 1254 * 1254).ToArray();
        var canonicalSkin = Mean(canonical, new SKRectI(440, 300, 800, 800));
        for (int n = 1; n <= 3; n++)
        {
            using var source = Read(root, $"oval-{n:00}.png");
            var offset = Difference(canonicalSkin, Mean(source, new SKRectI(440, 300, 800, 800)));
            var pixels = source.Pixels; var shared = canonical.Pixels;
            for (int y = 0; y < 1254; y++)
                for (int x = 0; x < 1254; x++)
                {
                    int i = y * 1254 + x;
                    var c = Tone(pixels[i], offset);
                    // All three shells share exactly the same skin below the face
                    // patch. Chin/jaw silhouette stays in the shell, not in the face.
                    float center = Smooth(Math.Clamp((PatchDistance(x, y) + 45) / 45, 0, 1));
                    c = VariantAssets.Blend(c, shared[i], center).WithAlpha(c.Alpha);
                    // The original collar socket is common across the three shells.
                    float neck = Smooth(Math.Clamp((y - 1030) / 65f, 0, 1));
                    c = VariantAssets.Blend(c, shared[i], neck);
                    if (y > 1030) c = c.WithAlpha((byte)(c.Alpha * Smooth(Math.Clamp(Math.Min(x - 432, 808 - x) / 8f, 0, 1))));
                    pixels[i] = c;
                    faceFootprint[i] = Math.Min(faceFootprint[i], c.Alpha);
                }
            source.Pixels = pixels;
            SavePlaced(root, source, $"Ovals/oval-{n:00}.png");
            Add("Oval", n, $"Ovals/oval-{n:00}.png", OvalNames[n - 1]);
        }
        for (int n = 1; n <= 10; n++)
        {
            using var raw = Read(root, $"face-{n:00}.png");
            using var registered = new SKBitmap(1254, 1254, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(registered))
            using (var paint = new SKPaint { FilterQuality = SKFilterQuality.High })
            {
                canvas.Clear(SKColors.Transparent);
                // One uniform transform of the ENTIRE face, never of individual features.
                const float scale = .90f;
                canvas.Translate(620 - 620 * scale, 517 - 535 * scale);
                canvas.Scale(scale);
                canvas.DrawBitmap(raw, 0, 0, paint);
            }
            var sourcePixels = registered.Pixels; var targetPixels = canonical.Pixels;
            // Match the common complexion using clean forehead/outer cheek skin.
            var offset = Difference(Mean(canonical, new SKRectI(425, 300, 815, 355)), Mean(registered, new SKRectI(425, 300, 815, 355)));
            using var patch = new SKBitmap(1254, 1254, SKColorType.Rgba8888, SKAlphaType.Premul);
            var output = patch.Pixels;
            for (int y = 270; y < 960; y++)
                for (int x = 300; x < 945; x++)
                {
                    float distance = PatchDistance(x, y);
                    if (distance <= 0) continue;
                    int i = y * 1254 + x;
                    float alpha = Smooth(Math.Clamp(distance / 32, 0, 1));
                    // The outer skin rim becomes exactly the shared shell skin;
                    // the central face retains its coherent authored features.
                    float rim = 1 - Smooth(Math.Clamp((distance - 28) / 42, 0, 1));
                    var c = Tone(sourcePixels[i], offset);
                    c = VariantAssets.Blend(c, targetPixels[i], rim);
                    output[i] = c.WithAlpha((byte)MathF.Round(alpha * faceFootprint[i]));
                }
            patch.Pixels = output;
            SavePlaced(root, patch, $"Faces/face-{n:00}.png");
            Add("Face", n, $"Faces/face-{n:00}.png", FaceNames[n - 1]);
        }
        foreach (string category in new[] { "Hair", "Clothes" })
            foreach (var part in legacy.Parts.Where(p => p.Category == category).OrderBy(p => p.Id))
            {
                int n = int.Parse(part.Id.Split('.').Last());
                string path = $"{category}/{category.ToLowerInvariant()}-{n:00}.png";
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, path))!);
                File.Copy(legacy.TexturePath(part.TextureFor(10)), Path.Combine(root, path), true);
                if (category == "Hair")
                {
                    // Old cutouts include a narrow strip of the former face below
                    // the eye cavity. Remove that spill only in the new copy.
                    using var hair = SKBitmap.Decode(Path.Combine(root, path));
                    using (var canvas = new SKCanvas(hair))
                    using (var clear = new SKPaint { BlendMode = SKBlendMode.Clear })
                        canvas.DrawRect(new SKRect(354, 310, 662, 350), clear);
                    VariantAssets.Save(root, hair, path);
                }
                Add(category, n, path, part.DisplayName ?? category);
            }
        var style = legacy.Style with
        {
            LibraryVersion = 11, SupportedLibraryVersions = [11],
            Layers = [new("Oval", IntroducedInVersion: 11), new("Face", IntroducedInVersion: 11), new("Clothes", IntroducedInVersion: 11), new("Hair", IntroducedInVersion: 11)],
            RenderStyle = new() { ["mode"] = "whole-face-w1", ["skin"] = "shared light peach", ["description"] = "3 shells with chin and neck; 10 complete faces; 10 copied hairstyles; no reference images." }
        };
        File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
        Console.WriteLine("W1: 3 ovals, 10 whole faces, 10 copied hairstyles, 3 copied costumes. 900 combinations.");
        void Add(string category, int n, string path, string label) => parts.Add(new() { Id = Id(category, n), Category = category, Texture = path, DisplayName = label, IntroducedInVersion = 11 });
    }
    private static float PatchDistance(float x, float y)
    {
        // Rounded broad patch includes both complete brows and all facial features.
        float qx = Math.Abs(x - 620) - 225, qy = Math.Abs(y - 617) - 238;
        float rounded = 65 - (MathF.Sqrt(MathF.Pow(Math.Max(qx, 0), 2) + MathF.Pow(Math.Max(qy, 0), 2)) + Math.Min(Math.Max(qx, qy), 0));
        float jaw = 290 - Math.Max(0, y - 750) * .5f - Math.Abs(x - 620);
        return Math.Min(rounded, jaw);
    }
    private static float Smooth(float t) => t * t * (3 - 2 * t);
    private static SKPoint3 Mean(SKBitmap bitmap, SKRectI rect)
    {
        double r = 0, g = 0, b = 0; int count = 0;
        for (int y = rect.Top; y < rect.Bottom; y += 2)
            for (int x = rect.Left; x < rect.Right; x += 2)
            { var c = bitmap.GetPixel(x, y); if (c.Alpha < 250) continue; r += c.Red; g += c.Green; b += c.Blue; count++; }
        return new((float)(r / count), (float)(g / count), (float)(b / count));
    }
    private static SKPoint3 Difference(SKPoint3 a, SKPoint3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    private static SKColor Tone(SKColor c, SKPoint3 d) => new(Channel(c.Red + d.X), Channel(c.Green + d.Y), Channel(c.Blue + d.Z), c.Alpha);
    private static byte Channel(float c) => (byte)Math.Clamp((int)MathF.Round(c), 0, 255);
    private static SKBitmap Read(string root, string file)
    {
        using var source = SKBitmap.Decode(Path.Combine(root, "Sources", file)) ?? throw new InvalidDataException($"Missing W1 source: {file}");
        var resized = source.Resize(new SKImageInfo(1254, 1254, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
        if (file.StartsWith("oval-", StringComparison.Ordinal))
        {
            var original = resized.Pixels; var cleaned = (SKColor[])original.Clone();
            for (int y = 3; y < 1251; y++)
                for (int x = 3; x < 1251; x++)
                {
                    int i = y * 1254 + x;
                    byte alpha = original[i].Alpha;
                    foreach (int delta in new[] { -3, 3, -3762, 3762 }) alpha = Math.Min(alpha, original[i + delta].Alpha);
                    cleaned[i] = original[i].WithAlpha(alpha >= 240 ? (byte)255 : alpha);
                }
            resized.Pixels = cleaned;
        }
        return resized;
    }
    private static void SavePlaced(string root, SKBitmap source, string path)
    {
        using var resized = source.Resize(new SKImageInfo(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
        using var placed = VariantAssets.Place(resized);
        VariantAssets.Save(root, placed, path);
    }
    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        var baseline = new PortraitGenerator(assets).Generate(1);
        foreach (string category in new[] { "Oval", "Face", "Clothes", "Hair" }) baseline = Set(baseline, category, 1);
        using var labels = new SKPaint { Color = SKColors.White, TextSize = 18, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        Gallery("Generated/faces.png", 5, 2, 400, i => Set(Set(baseline, "Face", i + 1), "Hair", 7), i => $"{i + 1}. {FaceNames[i]}");
        Gallery("Generated/examples.png", 5, 2, 400, i => Set(Set(Set(Set(baseline, "Face", i + 1), "Hair", i + 1), "Oval", i % 3 + 1), "Clothes", i % 3 + 1), i => $"Лицо {i + 1} · Овал {i % 3 + 1}");
        Gallery("Generated/ovals-faces.png", 10, 3, 256, i => Set(Set(Set(baseline, "Face", i % 10 + 1), "Oval", i / 10 + 1), "Hair", 7), i => $"О{i / 10 + 1} Л{i % 10 + 1}");
        for (int n = 1; n <= 3; n++)
        {
            Gallery($"Generated/hair-{n}.png", 10, 10, 200, i => Set(Set(Set(baseline, "Face", i / 10 + 1), "Oval", n), "Hair", i % 10 + 1), i => $"Л{i / 10 + 1} В{i % 10 + 1}");
        }
        void Gallery(string path, int columns, int rows, int cell, Func<int, CharacterAppearance> appearance, Func<int, string> label)
        {
            using var bitmap = new SKBitmap(columns * cell, rows * cell); using var canvas = new SKCanvas(bitmap); canvas.Clear(new SKColor(28, 40, 54));
            for (int i = 0; i < columns * rows; i++)
            { int x = i % columns * cell, y = i / columns * cell; canvas.DrawImage(renderer.Render(appearance(i), cell), x, y); canvas.DrawText(label(i), x + 9, y + 24, labels); }
            VariantAssets.Save(root, bitmap, path);
        }
    }
    private static CharacterAppearance Set(CharacterAppearance a, string category, int n) => a with { Parts = a.Parts.SetItem(category, Id(category, n)) };
}
