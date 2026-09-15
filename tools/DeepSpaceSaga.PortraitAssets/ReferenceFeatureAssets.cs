using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

/// <summary>Extracts interchangeable features from the ten approved frontal reference heads.</summary>
internal static class ReferenceFeatureAssets
{
    private const float Scale = 1024f / 1254;
    private static readonly string[] Categories = ["Eyes", "Eyebrows", "Nose", "Mouth"];
    // Native 1254px landmarks reviewed on the approved source portraits.
    private sealed record Landmarks(float EyeX, float EyeY, float NoseY, float MouthY);
    private static readonly Landmarks[] Points =
    [
        new(620, 502, 698, 804), new(612.5f, 492, 690, 789),
        new(625, 484, 695, 787), new(615.5f, 482, 681, 781),
        new(619.5f, 511.5f, 698, 811), new(621, 500, 700, 800),
        new(614, 486.5f, 702, 795), new(628, 484, 677, 782),
        new(623.5f, 488.5f, 700, 795), new(627, 484.5f, 676, 778)
    ];
    private static Landmarks Landmark(int face) => face >= 7 ? Points[face - 7] : new(620, 517, 736, 826);
    private static int Number(string category, int reference) => reference + (category == "Eyes" ? 10 : category == "Eyebrows" ? 1 : 3);
    private static string Id(string category, int n) => $"female.frontal.{category.ToLowerInvariant()}.{n:000}";
    public static bool HasSources(string root) => Enumerable.Range(7, 10).All(f => File.Exists(Path.Combine(root, $"Sources/faces/face-{f:00}-01.png")));

    public static void Build(string root)
    {
        var assets = new PortraitAssetRepository(root);
        if (assets.Style.LibraryVersion < 9 || !HasSources(root)) throw new InvalidDataException("Build the sixteen reference faces first.");
        var parts = assets.Parts.Where(p => p.IntroducedInVersion < 10).ToList();
        var audit = new List<object>();
        var heads = Enumerable.Range(1, 16).Select(f => Read(root, f)).ToArray();
        try
        {
            for (int reference = 1; reference <= 10; reference++)
            {
                int donorFace = reference + 6;
                foreach (string category in Categories)
                {
                    int number = Number(category, reference);
                    var paths = new Dictionary<string, string>();
                    for (int face = 1; face <= 16; face++)
                    {
                        string path = $"Parts/reference-features/{reference:00}/{category.ToLowerInvariant()}-face-{face:00}.png";
                        using var patch = Extract(heads[donorFace - 1], heads[face - 1], Landmark(donorFace), Landmark(face), category);
                        using var placed = VariantAssets.Place(patch);
                        VariantAssets.Save(root, placed, path);
                        paths.Add(Id("Face", face), path);
                    }
                    parts.Add(new PortraitPart
                    {
                        Id = Id(category, number), Category = category, IntroducedInVersion = 10,
                        DisplayName = $"{Label(category)} · референс {reference:00}",
                        Texture = paths[Id("Face", 1)], FaceTextures = paths
                    });
                    audit.Add(new { id = Id(category, number), donorFace = Id("Face", donorFace),
                        source = $"Sources/faces/face-{donorFace:00}-01.png", originalReference = $"Sources/faces/reference-{donorFace:00}.png",
                        landmarks = Landmark(donorFace), textures = paths });
                }
                Console.WriteLine($"Extracted reference {reference}/10: eyes, brows, nose, mouth for 16 faces.");
            }
        }
        finally { foreach (var head in heads) head.Dispose(); }
        File.WriteAllText(Path.Combine(root, "Sources/reference-features.json"), JsonSerializer.Serialize(audit, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(assets.Style with
        { LibraryVersion = 10, SupportedLibraryVersions = Enumerable.Range(4, 7).ToArray() }, AppearanceSerializer.Options));
        Console.WriteLine("Added 40 selectable features, 640 registered PNGs. Library v10: 89 parts.");
    }

    private static SKBitmap Read(string root, int face)
    {
        using var source = SKBitmap.Decode(Path.Combine(root, face == 1 ? "Sources/master.png" : $"Sources/faces/face-{face:00}-01.png"));
        return source.Resize(new SKImageInfo(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
    }

    private static SKBitmap Extract(SKBitmap donor, SKBitmap target, Landmarks from, Landmarks to, string category)
    {
        float CenterY(Landmarks p) => category switch { "Eyebrows" => p.EyeY - 80, "Nose" => p.NoseY, "Mouth" => p.MouthY, _ => p.EyeY };
        int dx = (int)MathF.Round((to.EyeX - from.EyeX) * Scale), dy = (int)MathF.Round((CenterY(to) - CenterY(from)) * Scale);
        var output = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
        output.Erase(SKColors.Transparent);
        var pixels = donor.Pixels; var targetPixels = target.Pixels; var result = output.Pixels;
        foreach (float side in category is "Eyes" or "Eyebrows" ? new[] { -140f, 140f } : new[] { 0f })
        {
            float cx = from.EyeX + side, cy = CenterY(from);
            var region = category switch
            {
                "Eyes" => new SKRect(cx - 135, cy - 55, cx + 135, cy + 112),
                "Eyebrows" => new SKRect(cx - 145, cy - 60, cx + 145, cy + 38),
                "Nose" => new SKRect(cx - 125, cy - 225, cx + 125, cy + 58),
                _ => new SKRect(cx - 174, cy - 84, cx + 174, cy + 102)
            };
            float feather = 22;
            // Estimate skin color on the outer margin, never on iris, lips or brow hairs.
            double dr = 0, dg = 0, db = 0, donorLight = 0; int samples = 0;
            for (int y = (int)(region.Top * Scale); y < region.Bottom * Scale; y += 2)
                for (int x = (int)(region.Left * Scale); x < region.Right * Scale; x += 2)
                {
                    float distance = Distance(x / Scale, y / Scale);
                    if (distance < 2 || distance > 9) continue;
                    var a = pixels[y * 1024 + x]; var b = targetPixels[(y + dy) * 1024 + x + dx];
                    dr += b.Red - a.Red; dg += b.Green - a.Green; db += b.Blue - a.Blue;
                    donorLight += (a.Red + a.Green + a.Blue) / 3f; samples++;
                }
            var offset = new SKPoint3((float)(dr / Math.Max(1, samples)), (float)(dg / Math.Max(1, samples)), (float)(db / Math.Max(1, samples)));
            for (int y = (int)(region.Top * Scale); y < region.Bottom * Scale; y++)
                for (int x = (int)(region.Left * Scale); x < region.Right * Scale; x++)
                {
                    float nx = x / Scale, ny = y / Scale;
                    float alpha = Math.Clamp(Distance(nx, ny) / feather, 0, 1);
                    if (alpha <= 0) continue;
                    var c = pixels[y * 1024 + x];
                    float ex = (nx - cx) / (category == "Mouth" ? 122 : 110);
                    float ey = (ny - cy) / (category == "Eyes" ? 36 : category == "Mouth" ? 45 : 24);
                    float skin = category == "Nose" ? 1 : Math.Clamp((ex * ex + ey * ey - .7f) / .65f, 0, 1);
                    if (category == "Eyebrows")
                    {
                        float light = (c.Red + c.Green + c.Blue) / 3f, skinLight = (float)(donorLight / Math.Max(1, samples));
                        skin = Math.Clamp((light - skinLight * .55f) / Math.Max(1, skinLight * .35f), 0, 1);
                    }
                    result[(y + dy) * 1024 + x + dx] = new SKColor(Channel(c.Red + offset.X * skin), Channel(c.Green + offset.Y * skin), Channel(c.Blue + offset.Z * skin), (byte)MathF.Round(alpha * 255));
                }
            float Distance(float x, float y)
            {
                float distance = Math.Min(Math.Min(x - region.Left, region.Right - x), Math.Min(y - region.Top, region.Bottom - y));
                if (category == "Nose")
                {
                    // Narrow bridge; broad alae only near the nose base, clear of the eyes.
                    float halfWidth = 55 + 70 * Math.Clamp((y - (cy - 115)) / 70, 0, 1);
                    distance = Math.Min(distance, halfWidth - Math.Abs(x - cx));
                }
                return distance;
            }
        }
        output.Pixels = result;
        return output;
    }
    private static byte Channel(float c) => (byte)Math.Clamp((int)MathF.Round(c), 0, 255);
    private static string Label(string category) => category switch { "Eyes" => "Глаза", "Eyebrows" => "Брови", "Nose" => "Нос", _ => "Губы" };

    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        var baseline = new PortraitGenerator(assets).Generate(1, 10);
        foreach (var category in new[] { "Face", "Eyes", "Eyebrows", "Nose", "Mouth", "Chin", "Clothes" }) baseline = Set(baseline, category, 1);
        baseline = Set(baseline, "Hair", 7);
        using var paint = new SKPaint { Color = SKColors.White, TextSize = 17, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        using (var atlas = new SKBitmap(2600, 720))
        using (var canvas = new SKCanvas(atlas))
        {
            canvas.Clear(new SKColor(28, 40, 54));
            for (int row = 0; row < Categories.Length; row++)
            {
                string category = Categories[row];
                var crop = category switch
                {
                    "Eyes" => new SKRect(345, 250, 675, 365),
                    "Eyebrows" => new SKRect(340, 207, 680, 275),
                    "Nose" => new SKRect(435, 280, 590, 443),
                    _ => new SKRect(407, 402, 619, 515)
                };
                for (int i = 0; i < 10; i++)
                {
                    using var texture = SKBitmap.Decode(assets.TexturePath(assets.Get(Id(category, Number(category, i + 1))).Texture));
                    float scale = Math.Min(245 / crop.Width, 138 / crop.Height);
                    float width = crop.Width * scale, height = crop.Height * scale;
                    canvas.DrawBitmap(texture, crop, SKRect.Create(i * 260 + (260 - width) / 2, row * 180 + 32 + (138 - height) / 2, width, height));
                    canvas.DrawText($"{Label(category)} {Number(category, i + 1)} · Р{i + 1}", i * 260 + 10, row * 180 + 23, paint);
                }
            }
            VariantAssets.Save(root, atlas, "Generated/reference-feature-parts.png");
        }
        foreach (string category in Categories)
        {
            Gallery($"Generated/reference-{category.ToLowerInvariant()}-collection.png", 5, 2, 400,
                i => Set(baseline, category, Number(category, i + 1)), i => $"{Label(category)} {Number(category, i + 1)} · реф. {i + 1}");
            for (int batch = 0; batch < 2; batch++)
            {
                int offset = batch * 8 + 1;
                Gallery($"Generated/reference-{category.ToLowerInvariant()}-fit-{batch + 1}.png", 10, 8, 200,
                    i => Set(Set(baseline, "Face", offset + i / 10), category, Number(category, i % 10 + 1)),
                    i => $"Л{offset + i / 10} · Р{i % 10 + 1}");
            }
        }
        Gallery("Generated/reference-feature-portraits.png", 5, 2, 400, i =>
        {
            var a = Set(Set(Set(baseline, "Face", i + 7), "Hair", i + 1), "Clothes", i % 3 + 1);
            foreach (string category in Categories) a = Set(a, category, Number(category, i + 1));
            return a;
        }, i => $"Референс {i + 1} · четыре детали");
        void Gallery(string path, int columns, int rows, int cell, Func<int, CharacterAppearance> appearance, Func<int, string> label)
        {
            using var bitmap = new SKBitmap(columns * cell, rows * cell); using var canvas = new SKCanvas(bitmap);
            canvas.Clear(new SKColor(28, 40, 54));
            for (int i = 0; i < columns * rows; i++)
            {
                int x = i % columns * cell, y = i / columns * cell;
                canvas.DrawImage(renderer.Render(appearance(i), cell), x, y);
                canvas.DrawText(label(i), x + 8, y + 22, paint);
            }
            VariantAssets.Save(root, bitmap, path);
        }
        Console.WriteLine("Rendered four feature collections and all 640 face/feature combinations.");
    }
    private static CharacterAppearance Set(CharacterAppearance a, string category, int n) => a with { Parts = a.Parts.SetItem(category, Id(category, n)) };
}
