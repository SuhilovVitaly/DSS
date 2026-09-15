using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

/// <summary>W2 retains each woman's complete anatomy in one head sprite.</summary>
internal static class WholeHeadAssets
{
    private static readonly string[] HeadNames = ["Мягкое круглое лицо", "Узкий овал", "Мягкая широкая челюсть"];
    private static readonly string[] HairNames = ["Французское каре", "Медный андеркат", "Две светлые косы", "Каштановые кудри", "Платиновый помпадур"];
    private static string Id(string category, int n) => $"female.w2.{category.ToLowerInvariant()}.{n:000}";
    public static void Build(string root)
    {
        var previous = new PortraitAssetRepository(Path.GetFullPath(Path.Combine(root, "../W1")), validateTextures: false);
        var parts = new List<PortraitPart>();
        using var neckSource = Read(root, "neck-01.png");
        // The shared neck is a separately authored cutout with no head anatomy.
        // Its upper edge is hidden behind every complete head; its lower edge is
        // covered by all three collars. It never contains a jaw/face fragment.
        using (var neck = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul))
        {
            using var canvas = new SKCanvas(neck); canvas.Clear(SKColors.Transparent);
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            using var path = new SKPath();
            path.MoveTo(410, 455); path.LineTo(614, 455);
            path.CubicTo(610, 510, 604, 550, 615, 610); path.LineTo(625, 680);
            path.LineTo(399, 680); path.LineTo(409, 610);
            path.CubicTo(420, 550, 414, 510, 410, 455); path.Close();
            canvas.ClipPath(path, SKClipOperation.Intersect, true);
            canvas.Translate(512 - 627 * .86f, 455 - 550 * .86f);
            canvas.Scale(.86f); canvas.DrawBitmap(neckSource, 0, 0, paint);
            Save(neck, "Neck/neck-01.png");
            Add("Neck", 1, "Neck/neck-01.png", "Общая шея");
        }
        float[] crowns = [25, 18, 7], chins = [1115, 1072, 1090];
        for (int n = 1; n <= 3; n++)
        {
            using var source = Read(root, $"head-{n:00}.png");
            using var jaw = JawMask(n);
            using (var canvas = new SKCanvas(source))
            using (var paint = new SKPaint { BlendMode = SKBlendMode.DstIn }) canvas.DrawBitmap(jaw, 0, 0, paint);
            using var output = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(output))
            using (var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true })
            {
                canvas.Clear(SKColors.Transparent);
                float scale = 500 / (chins[n - 1] - crowns[n - 1]);
                canvas.Translate(512 - 627 * scale, 35 - crowns[n - 1] * scale);
                canvas.Scale(scale); canvas.DrawBitmap(source, 0, 0, paint);
            }
            Save(output, $"Heads/head-{n:00}.png"); Add("Head", n, $"Heads/head-{n:00}.png", HeadNames[n - 1]);
        }
        for (int n = 1; n <= 5; n++)
        {
            using var source = Read(root, $"hair-{n:00}.png");
            using var output = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(output); canvas.Clear(SKColors.Transparent);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
            if (n == 1)
            {
                // Register the wig opening independently of its outer volume.
                // The fringe stays above the brows and the bob ends near the jaw.
                float[] sx = [0, 320, 934, 1254], dx = [270, 356, 668, 754];
                float[] sy = [0, 630, 1254], dy = [6, 246, 570];
                for (int y = 0; y < sy.Length - 1; y++)
                for (int x = 0; x < sx.Length - 1; x++)
                    canvas.DrawBitmap(source, new SKRect(sx[x], sy[y], sx[x + 1], sy[y + 1]), new SKRect(dx[x], dy[y], dx[x + 1], dy[y + 1]), paint);
            }
            else if (n == 3)
            {
                float[] sy = [0, 700, 1254], dy = [2, 340, 750];
                for (int y = 0; y < sy.Length - 1; y++)
                    canvas.DrawBitmap(source, new SKRect(0, sy[y], 1254, sy[y+1]), new SKRect(512-627*.55f, dy[y], 512+627*.55f, dy[y+1]), paint);
            }
            else
            {
                const float scale = .46f;
                canvas.Translate(512 - 627 * scale, 2); canvas.Scale(scale); canvas.DrawBitmap(source, 0, 0, paint);
            }
            Save(output, $"Hair/hair-{n:00}.png"); Add("Hair", n, $"Hair/hair-{n:00}.png", HairNames[n - 1]);
        }
        foreach (var part in previous.Parts.Where(p => p.Category == "Clothes"))
        {
            int n = int.Parse(part.Id.Split('.').Last()); string path = $"Clothes/clothes-{n:00}.png";
            File.Copy(previous.TexturePath(part.Texture), Path.Combine(root, path), true);
            Add("Clothes", n, path, part.DisplayName ?? "Костюм");
        }
        var style = previous.Style with
        {
            LibraryVersion = 12, SupportedLibraryVersions = [12],
            Layers = [new("Neck", IntroducedInVersion: 12), new("Head", IntroducedInVersion: 12), new("Clothes", IntroducedInVersion: 12), new("Hair", IntroducedInVersion: 12)],
            Anchors = new(previous.Style.Anchors) { ["Crown"] = new(.5f, 35/1024f), ["Chin"] = new(.5f, 535/1024f), ["Brow"] = new(.5f, 230/1024f), ["EyeLeft"] = new(440/1024f, 275/1024f), ["EyeRight"] = new(584/1024f, 275/1024f), ["NoseBase"] = new(.5f, 375/1024f), ["Mouth"] = new(.5f, 433/1024f) },
            RenderStyle = new() { ["mode"] = "whole-head-w2", ["description"] = "Three complete heads; one neck without jaw or face; three costumes; five new hair-only cutouts. 45 combinations." }
        };
        File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
        Console.WriteLine("W2 built: 3 whole heads, 1 common neck, 3 costumes, 5 new hairstyles; 45 combinations.");
        void Save(SKBitmap bitmap, string path) => VariantAssets.Save(root, bitmap, path);
        void Add(string category, int n, string path, string label) => parts.Add(new() { Id = Id(category, n), Category = category, Texture = path, DisplayName = label, IntroducedInVersion = 12 });
    }
    private static SKBitmap JawMask(int n)
    {
        var bitmap = new SKBitmap(1254, 1254); using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.Transparent);
        using var path = new SKPath(); path.MoveTo(0, 0); path.LineTo(1254, 0); path.LineTo(1254, 850);
        if (n == 1)
        {
            path.LineTo(975, 850); path.CubicTo(960, 978, 731, 1115, 627, 1115);
            path.CubicTo(524, 1115, 305, 981, 284, 850);
        }
        else if (n == 2)
        {
            path.LineTo(897, 850); path.CubicTo(869, 952, 731, 1072, 627, 1072);
            path.CubicTo(530, 1072, 386, 952, 360, 850);
        }
        else
        {
            path.LineTo(939, 850); path.CubicTo(927, 962, 744, 1090, 627, 1090);
            path.CubicTo(510, 1090, 326, 962, 313, 850);
        }
        path.LineTo(0, 850); path.Close();
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawPath(path, paint); return bitmap;
    }
    private static SKBitmap Read(string root, string name)
    {
        using var input = SKBitmap.Decode(Path.Combine(root, "Sources", name)) ?? throw new InvalidDataException($"Missing W2 source: {name}");
        var output = input.Resize(new SKImageInfo(1254, 1254, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
        var source = output.Pixels; var pixels = (SKColor[])source.Clone();
        // Remove the colored matte fringe left by generated transparency.
        for (int y = 3; y < 1251; y++)
        for (int x = 3; x < 1251; x++)
        {
            int i = y * 1254 + x;
            byte alpha = Math.Min(source[i].Alpha, Math.Min(Math.Min(source[i-3].Alpha, source[i+3].Alpha), Math.Min(source[i-3762].Alpha, source[i+3762].Alpha)));
            pixels[i] = source[i].WithAlpha(alpha >= 240 ? (byte)255 : alpha);
        }
        output.Pixels = pixels; return output;
    }
    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        var baseline = new PortraitGenerator(assets).Generate(1);
        using var labels = new SKPaint { Color = SKColors.White, TextSize = 17, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        Gallery("Generated/examples.png", 5, 3, 360, i => Set(Set(Set(baseline, "Head", i/5+1), "Hair", i%5+1), "Clothes", i/5+1));
        Gallery("Generated/all-combinations.png", 5, 9, 256, i => Set(Set(Set(baseline, "Head", i/15+1), "Hair", i%5+1), "Clothes", i/5%3+1));
        using var bald = new SKBitmap(1536, 512); using var bc = new SKCanvas(bald); bc.Clear(new SKColor(28, 40, 54));
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
        for (int n = 1; n <= 3; n++)
        {
            foreach (string path in new[] { "Neck/neck-01.png", $"Heads/head-{n:00}.png", "Clothes/clothes-01.png" })
            { using var bitmap = SKBitmap.Decode(Path.Combine(root, path)); bc.DrawBitmap(bitmap, new SKRect((n-1)*512, 0, n*512, 512), paint); }
        }
        VariantAssets.Save(root, bald, "Generated/heads-neck.png");
        void Gallery(string path, int columns, int rows, int size, Func<int, CharacterAppearance> select)
        {
            using var bitmap = new SKBitmap(columns*size, rows*size); using var canvas = new SKCanvas(bitmap); canvas.Clear(new SKColor(28, 40, 54));
            for (int i = 0; i < columns*rows; i++)
            { var a = select(i); int x=i%columns*size, y=i/columns*size; canvas.DrawImage(renderer.Render(a, size), x, y); canvas.DrawText($"Г{a.Parts["Head"].Split('.').Last()} · П{a.Parts["Hair"].Split('.').Last()} · К{a.Parts["Clothes"].Split('.').Last()}", x+8, y+22, labels); }
            VariantAssets.Save(root, bitmap, path);
        }
    }
    private static CharacterAppearance Set(CharacterAppearance a, string category, int n) => a with { Parts = a.Parts.SetItem(category, Id(category, n)) };
}
