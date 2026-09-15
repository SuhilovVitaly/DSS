using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

internal static class EyeAssets
{
    private static readonly string[] Labels = ["Исходные серо-зелёные", "Удлинённые голубые", "Округлые карие", "Прикрытые серые", "Миндальные зелёные", "Мягкие тёмно-карие", "Глубокие ореховые", "Складка у ресниц", "Открытые серо-голубые", "Узкие янтарные"];
    public static bool HasSources(string root) => File.Exists(Path.Combine(root, "Sources/eyes/eye-10.png"));
    public static void Build(string root)
    {
        var assets = new PortraitAssetRepository(root);
        if (assets.Style.LibraryVersion < 5) throw new InvalidDataException("Build the clothed portrait library first.");
        var parts = assets.Parts.Where(p => p.IntroducedInVersion < 6).Select(p => p with { FaceTextures = [] }).ToList();
        using var mask = SKBitmap.Decode(Path.Combine(root, "Masks/eyes.png"));
        for (int n = 2; n <= 10; n++)
        {
            using var source = SKBitmap.Decode(Path.Combine(root, $"Sources/eyes/eye-{n:00}.png")) ?? throw new InvalidDataException($"Missing eye source {n}.");
            if (source.Width != 1254 || source.Height != 1254) throw new InvalidDataException($"Eye source {n} must retain the 1254px registration canvas.");
            using var detail = source.Resize(new SKImageInfo(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
            // Use the same eye extraction mask as the original; changing an eye
            // cannot introduce generated pixels on the rest of the face/body.
            using (var canvas = new SKCanvas(detail))
            using (var paint = new SKPaint { BlendMode = SKBlendMode.DstIn }) canvas.DrawBitmap(mask, 0, 0, paint);
            using var placed = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(placed))
            using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
            { canvas.Clear(SKColors.Transparent); canvas.DrawBitmap(detail, VariantAssets.Head, paint); }
            string path = $"Parts/eyes/eyes-{n:00}.png";
            Save(root, placed, path);
            parts.Add(new() { Id = $"female.frontal.eyes.{n:000}", Category = "Eyes", DisplayName = Labels[n - 1], Texture = path, IntroducedInVersion = 6 });
        }
        int original = parts.FindIndex(p => p.Id == "female.frontal.eyes.001");
        parts[original] = parts[original] with { DisplayName = Labels[0] };
        var style = assets.Style with { LibraryVersion = 6, SupportedLibraryVersions = [4, 5, 6], Layers = assets.Style.Layers.Where(l => l.IntroducedInVersion <= 6).ToArray() };
        File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
        Console.WriteLine("Built 10 registered eye pairs. Library v6; v4/v5 appearances retained.");
    }
    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        var baseline = new PortraitGenerator(assets).Generate(1, 6);
        foreach (string category in new[] { "Nose", "Mouth", "Chin", "Clothes" })
            baseline = baseline with { Parts = baseline.Parts.SetItem(category, $"female.frontal.{category.ToLowerInvariant()}.001") };
        using var closeups = new SKBitmap(1200, 850); using var canvas = new SKCanvas(closeups);
        using var portraits = new SKBitmap(1600, 640); using var pc = new SKCanvas(portraits);
        canvas.Clear(new SKColor(28, 40, 54)); pc.Clear(new SKColor(28, 40, 54));
        using var label = new SKPaint { Color = SKColors.White, TextSize = 21, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        for (int n = 1; n <= 10; n++)
        {
            var appearance = baseline with { Parts = baseline.Parts.SetItem("Eyes", $"female.frontal.eyes.{n:000}") };
            var image = renderer.Render(appearance, 1024);
            int x = (n - 1) % 2 * 600, y = (n - 1) / 2 * 170;
            canvas.DrawImage(image, new SKRect(350, 246, 666, 326), new SKRect(x + 47.2f, y + 36, x + 552.8f, y + 164));
            canvas.DrawText($"{n:00} · {Labels[n - 1]}", x + 16, y + 26, label);
            int px = (n - 1) % 5 * 320, py = (n - 1) / 5 * 320;
            pc.DrawImage(image, new SKRect(px, py, px + 320, py + 320));
            pc.DrawText(n.ToString("00"), px + 10, py + 24, label);
        }
        Save(root, closeups, "Generated/eyes-comparison.png"); Save(root, portraits, "Generated/eyes-portraits.png");
    }
    private static void Save(string root, SKBitmap bitmap, string path)
    {
        string target = Path.Combine(root, path); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(target); data.SaveTo(file);
    }
}
