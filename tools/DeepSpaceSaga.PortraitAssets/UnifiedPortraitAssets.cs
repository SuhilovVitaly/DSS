using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

/// <summary>W4 has only two layers: a complete painted person and a costume.</summary>
internal static class UnifiedPortraitAssets
{
    private static readonly string[] Names = ["Брюнетка", "Блондинка", "Рыжая"];
    private static string Id(string category, int n) => $"female.w4.{category.ToLowerInvariant()}.{n:000}";
    public static void Build(string root)
    {
        var previous = new PortraitAssetRepository(Path.GetFullPath(Path.Combine(root, "../W2")), false);
        var parts = new List<PortraitPart>();
        for (int n = 1; n <= 3; n++)
        {
            using var source = SKBitmap.Decode(Path.Combine(root, $"Sources/portrait-{n:00}.png")) ?? throw new InvalidDataException($"Missing W4 portrait {n}");
            using var normalized = source.Resize(new SKImageInfo(1254, 1254, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
            var original = normalized.Pixels; var pixels = (SKColor[])original.Clone();
            // Matte cleanup only. The head, face, hair and neck are never split,
            // recolored, warped or assembled from other character parts.
            for (int y = 3; y < 1251; y++)
            for (int x = 3; x < 1251; x++)
            {
                int i = y*1254+x;
                byte alpha = Math.Min(original[i].Alpha, Math.Min(Math.Min(original[i-3].Alpha, original[i+3].Alpha), Math.Min(original[i-3762].Alpha, original[i+3762].Alpha)));
                pixels[i] = original[i].WithAlpha(alpha >= 240 ? (byte)255 : alpha);
            }
            normalized.Pixels = pixels;
            using var output = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(output))
            using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
            {
                canvas.Clear(SKColors.Transparent);
                // One uniform transform for the entire original portrait.
                canvas.Translate(512 - (n == 1 ? 640 : 627) * .5f, n == 2 ? 10 : 0);
                canvas.Scale(.5f); canvas.DrawBitmap(normalized, 0, 0, paint);
            }
            string path = $"Portraits/portrait-{n:00}.png";
            VariantAssets.Save(root, output, path); Add("Portrait", n, path, Names[n-1]);
        }
        foreach (var part in previous.Parts.Where(p => p.Category == "Clothes"))
        {
            int n = int.Parse(part.Id.Split('.').Last()); string path = $"Clothes/clothes-{n:00}.png";
            File.Copy(previous.TexturePath(part.Texture), Path.Combine(root, path), true);
            Add("Clothes", n, path, part.DisplayName ?? "Костюм");
        }
        var style = previous.Style with
        {
            LibraryVersion = 14, SupportedLibraryVersions = [14],
            Layers = [new("Portrait", IntroducedInVersion: 14), new("Clothes", IntroducedInVersion: 14)],
            RenderStyle = new() { ["mode"] = "unified-portrait-w4", ["description"] = "Three complete painted heads including their own neck and hairstyle. Only the costume is shared. Nine combinations." }
        };
        File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
        Console.WriteLine("W4 built: 3 complete head/neck/hair portraits, 3 shared costumes, 9 combinations.");
        void Add(string category, int n, string path, string label) => parts.Add(new() { Id = Id(category, n), Category = category, Texture = path, DisplayName = label, IntroducedInVersion = 14 });
    }
    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        var baseline = new PortraitGenerator(assets).Generate(1);
        using var labels = new SKPaint { Color = SKColors.White, TextSize = 20, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        Gallery("Generated/examples.png", 1, 512);
        Gallery("Generated/all-combinations.png", 3, 400);
        void Gallery(string path, int rows, int size)
        {
            using var bitmap = new SKBitmap(3*size, rows*size); using var canvas = new SKCanvas(bitmap); canvas.Clear(new SKColor(28, 40, 54));
            for (int row = 0; row < rows; row++)
            for (int i = 0; i < 3; i++)
            {
                int suit = rows == 1 ? i+1 : row+1;
                var a = baseline with { Parts = baseline.Parts.SetItem("Portrait", Id("Portrait", i+1)).SetItem("Clothes", Id("Clothes", suit)) };
                canvas.DrawImage(renderer.Render(a, size), i*size, row*size);
                canvas.DrawText($"{Names[i]} · Костюм {suit}", i*size+12, row*size+28, labels);
            }
            VariantAssets.Save(root, bitmap, path);
        }
    }
}
