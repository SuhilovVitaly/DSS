using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using System.Text.Json;
using SkiaSharp;

internal static class VariantAssets
{
    internal static readonly SKRect Head = new(176, 12, 848, 684);
    public static void Guide(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        using var source = SKBitmap.FromImage(renderer.Render(new PortraitGenerator(assets).Generate(1, 4), 1024));
        using var guide = new SKBitmap(1024, 1024);
        using var canvas = new SKCanvas(guide);
        canvas.Clear(new SKColor(28, 40, 54));
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(source, Head, paint);
        Save(root, guide, "Sources/suit-guide.png");
    }
    public static void Build(string root)
    {
        var previous = new PortraitAssetRepository(root);
        var parts = previous.Parts.Where(p => p.IntroducedInVersion == 4).Select(p => p with { FaceTextures = [] }).ToList();
        using var master = SKBitmap.Decode(Path.Combine(root, "Golden/master.png"));
        foreach (var part in parts.ToArray())
        {
            using var texture = SKBitmap.Decode(Path.Combine(root, part.Texture));
            if (part.Category == "Face")
                for (int y = 735; y < 1024; y++)
                for (int x = 0; x < 1024; x++)
                {
                    var c = texture.GetPixel(x, y);
                    texture.SetPixel(x, y, c.WithAlpha((byte)(c.Alpha * Math.Clamp((755 - y) / 20f, 0, 1))));
                }
            string path = $"Parts/variants/{part.Category.ToLowerInvariant()}-01.png";
            using var placed = Place(texture); Save(root, placed, path);
            parts[parts.IndexOf(part)] = part with { TextureVersions = new() { [5] = path } };
        }
        string[] lips = ["Естественные", "Тонкие", "Полные"];
        string[] noses = ["Прямой", "Узкий", "Округлый"];
        string[] chins = ["Округлый", "Суженный", "Широкий мягкий"];
        string[] suits = ["Офицерский", "Медицинский", "Инженерный"];
        for (int variant = 1; variant <= 3; variant++)
        {
            using var editedSource = SKBitmap.Decode(Path.Combine(root, variant == 1 ? "Sources/master.png" : $"Sources/variants/face-{variant:00}.png"));
            using var cut = CutBackground(editedSource);
            using var edited = cut.Resize(new SKImageInfo(1024, 1024), SKFilterQuality.High);
            using var chin = edited.Copy();
            for (int y = 0; y < 1024; y++)
            for (int x = 0; x < 1024; x++)
            {
                // Every chin uses exactly the same lower neck. Its jaw outline is
                // retained; the upper face layer covers the feathered junction.
                float common = Math.Clamp((y - 870) / 30f, 0, 1);
                var c = Blend(edited.GetPixel(x, y), master.GetPixel(x, y), common);
                chin.SetPixel(x, y, c.WithAlpha((byte)(c.Alpha * Math.Clamp((y - 710) / 20f, 0, 1))));
            }
            Add("Chin", chins[variant - 1], chin, variant, true);
            if (variant > 1)
                foreach (string category in new[] { "Nose", "Mouth" })
                {
                    using var mask = SKBitmap.Decode(Path.Combine(root, $"Masks/{category.ToLowerInvariant()}.png"));
                    using var feature = edited.Copy();
                    for (int y = 0; y < 1024; y++)
                    for (int x = 0; x < 1024; x++)
                    {
                        var c = feature.GetPixel(x, y);
                        feature.SetPixel(x, y, c.WithAlpha((byte)(c.Alpha * mask.GetPixel(x, y).Alpha / 255)));
                    }
                    Add(category, category == "Nose" ? noses[variant - 1] : lips[variant - 1], feature, variant, true);
                }
            using var suitSource = SKBitmap.Decode(Path.Combine(root, $"Sources/variants/suit-{variant:00}.png"));
            using var suit = suitSource.Resize(new SKImageInfo(1024, 1024), SKFilterQuality.High);
            using var garmentMask = GarmentMask();
            using (var canvas = new SKCanvas(suit))
            using (var paint = new SKPaint { BlendMode = SKBlendMode.DstIn }) canvas.DrawBitmap(garmentMask, 0, 0, paint);
            Add("Clothes", suits[variant - 1], suit, variant, false);
        }
        for (int i = 0; i < parts.Count; i++)
            if (parts[i].IntroducedInVersion == 4 && parts[i].Category is "Nose" or "Mouth")
                parts[i] = parts[i] with { DisplayName = parts[i].Category == "Nose" ? noses[0] : lips[0] };
        // Read baseline landmarks afresh: repeated builds cannot accumulate scaling.
        using var proportions = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "proportions.json")));
        var baselineAnchors = proportions.RootElement.GetProperty("landmarks").Deserialize<Dictionary<string, PortraitAnchor>>(AppearanceSerializer.Options)!;
        var style = previous.Style with
        {
            LibraryVersion = 5, SupportedLibraryVersions = [4, 5],
            Layers = [new("Chin", IntroducedInVersion: 5), new("Face"), new("Clothes", IntroducedInVersion: 5), new("Eyebrows"), new("Eyes"), new("Nose"), new("Mouth")],
            Anchors = baselineAnchors.ToDictionary(a => a.Key, a => new PortraitAnchor((Head.Left + a.Value.X * Head.Width) / 1024, (Head.Top + a.Value.Y * Head.Height) / 1024)),
            RenderStyle = new()
            {
                ["mode"] = "frontal-modular-variants", ["referenceFolder"] = "Reference",
                ["description"] = "Three lips, noses, chins and suits. Uniform whole-head placement; no independent feature transforms. Common lower neck and collar opening.",
                ["baselineVersion"] = "4"
            }
        };
        Write(root, "parts.json", parts); Write(root, "portrait-style.json", style);
        Console.WriteLine($"Built {parts.Count} components; 81 face/suit combinations; baseline v4 retained.");

        void Add(string category, string label, SKBitmap texture, int number, bool place)
        {
            string path = $"Parts/variants/{category.ToLowerInvariant()}-{number:00}.png";
            using var output = place ? Place(texture) : texture.Copy(); Save(root, output, path);
            parts.Add(new() { Id = $"female.frontal.{category.ToLowerInvariant()}.{number:000}", Category = category, Texture = path, DisplayName = label, IntroducedInVersion = 5 });
        }
    }
    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        var appearance = new PortraitGenerator(assets).Generate(1, 5);
        // Keep the initial reference combination easy to compare with the accepted face.
        foreach (string category in new[] { "Eyes", "Nose", "Mouth", "Chin", "Clothes" }) appearance = Set(appearance, category, 1);
        using var portrait = SKBitmap.FromImage(renderer.Render(appearance, 1024)); Save(root, portrait, "Generated/portrait.png");
        Write(root, "Generated/appearance.json", appearance);
        using var overview = new SKBitmap(1536, 512); using var overviewCanvas = new SKCanvas(overview);
        overviewCanvas.Clear(new SKColor(28, 40, 54));
        for (int n = 1; n <= 3; n++)
        {
            var combination = appearance;
            foreach (string category in new[] { "Nose", "Mouth", "Chin", "Clothes" }) combination = Set(combination, category, n);
            overviewCanvas.DrawImage(renderer.Render(combination, 512), (n - 1) * 512, 0);
        }
        Save(root, overview, "Generated/variants-preview.png");
        using var gallery = new SKBitmap(1536, 1536); using var canvas = new SKCanvas(gallery);
        canvas.Clear(new SKColor(28, 40, 54));
        using var label = new SKPaint { Color = SKColors.White, TextSize = 20, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        for (int chin = 1; chin <= 3; chin++)
        for (int suit = 1; suit <= 3; suit++)
        {
            var combination = Set(Set(appearance, "Chin", chin), "Clothes", suit);
            float x = (suit - 1) * 512, y = (chin - 1) * 512;
            canvas.DrawImage(renderer.Render(combination, 512), x, y);
            canvas.DrawText($"Подбородок {chin} · Костюм {suit}", x + 16, y + 27, label);
        }
        Save(root, gallery, "Generated/collar-combinations.png");
        using var comparison = new SKBitmap(1536, 1536); using var cc = new SKCanvas(comparison);
        cc.Clear(new SKColor(28, 40, 54));
        for (int row = 0; row < 3; row++)
        for (int n = 1; n <= 3; n++)
        {
            string category = new[] { "Mouth", "Nose", "Chin" }[row];
            var combination = Set(appearance, category, n);
            var rendered = renderer.Render(combination, 1024);
            cc.DrawImage(rendered, new SKRect(292, 190, 720, 618), new SKRect((n - 1) * 512, row * 512, n * 512, (row + 1) * 512));
            cc.DrawText($"{new[] { "Губы", "Нос", "Подбородок" }[row]} {n}", (n - 1) * 512 + 16, row * 512 + 27, label);
        }
        Save(root, comparison, "Generated/feature-variants.png");
        Console.WriteLine("Rendered collar matrix (9) and isolated feature comparison (9).");
    }
    private static CharacterAppearance Set(CharacterAppearance a, string category, int n) => a with { Parts = a.Parts.SetItem(category, $"female.frontal.{category.ToLowerInvariant()}.{n:000}") };
    internal static SKBitmap Place(SKBitmap source)
    {
        var result = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(result); canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(source, Head, paint); return result;
    }
    private static SKBitmap GarmentMask()
    {
        // Authored contour in the 1254px suit source. The same inner opening is
        // applied to all three garments; no generated neck pixels survive.
        var mask = new SKBitmap(1024, 1024); using var canvas = new SKCanvas(mask);
        canvas.Clear(SKColors.Transparent); canvas.Scale(1024f / 1254);
        using var path = new SKPath();
        path.MoveTo(482, 645); path.CubicTo(462, 648, 473, 690, 442, 748);
        path.LineTo(374, 774); path.LineTo(247, 809); path.LineTo(177, 820); path.LineTo(171, 833);
        path.LineTo(83, 852); path.CubicTo(54, 855, 23, 946, 12, 980); path.LineTo(0, 1065);
        path.LineTo(0, 1254); path.LineTo(1254, 1254); path.LineTo(1254, 1065);
        path.CubicTo(1245, 962, 1201, 885, 1170, 860);
        path.LineTo(1062, 831); path.LineTo(1056, 818); path.LineTo(970, 805); path.LineTo(804, 751);
        path.CubicTo(783, 743, 777, 659, 752, 645);
        path.CubicTo(754, 675, 749, 696, 714, 713);
        path.CubicTo(691, 727, 665, 736, 648, 741); path.LineTo(643, 750); path.LineTo(603, 750); path.LineTo(598, 743);
        path.CubicTo(558, 732, 504, 708, 487, 682); path.CubicTo(482, 673, 482, 660, 482, 645); path.Close();
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true }; canvas.DrawPath(path, paint); return mask;
    }
    internal static SKColor Blend(SKColor a, SKColor b, float t) => new((byte)(a.Red + (b.Red - a.Red) * t), (byte)(a.Green + (b.Green - a.Green) * t), (byte)(a.Blue + (b.Blue - a.Blue) * t), (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    internal static SKBitmap CutBackground(SKBitmap source)
    {
        var pixels = source.Pixels; int w = source.Width, h = source.Height;
        var seen = new bool[w * h]; var queue = new Queue<int>();
        void Visit(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h || seen[y * w + x]) return;
            seen[y * w + x] = true; var c = pixels[y * w + x];
            if (c.Red < 70 && c.Green < 90 && c.Blue < 115 && c.Blue > c.Red + 3) queue.Enqueue(y * w + x);
        }
        for (int x = 0; x < w; x++) { Visit(x, 0); Visit(x, h - 1); }
        for (int y = 0; y < h; y++) { Visit(0, y); Visit(w - 1, y); }
        while (queue.TryDequeue(out int index))
        {
            int x = index % w, y = index / w; pixels[index] = SKColors.Transparent;
            Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
        }
        return new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul) { Pixels = pixels };
    }
    private static void Write<T>(string root, string path, T value) => File.WriteAllText(Path.Combine(root, path), JsonSerializer.Serialize(value, AppearanceSerializer.Options));
    internal static void Save(string root, SKBitmap bitmap, string path)
    {
        string target = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(target); data.SaveTo(file);
    }
}
