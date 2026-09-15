using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

internal static class HairAssets
{
    private static readonly string[] Labels = ["Пикси", "Прямое каре", "Волнистый лоб", "Длинные прямые", "Рыжие кудри", "Каре с чёлкой", "Низкий пучок", "Высокий хвост", "Коса вокруг головы", "Серебристые волны"];
    public static bool HasSources(string root) => File.Exists(Path.Combine(root, "Sources/hair/hair-10.png"));
    public static void Build(string root)
    {
        var assets = new PortraitAssetRepository(root);
        if (assets.Style.LibraryVersion < 6) throw new InvalidDataException("Build the eye library first.");
        var parts = assets.Parts.Where(p => p.IntroducedInVersion < 7).Select(p => p with { FaceTextures = [] }).ToList();
        for (int n = 1; n <= 10; n++)
        {
            using var source = SKBitmap.Decode(Path.Combine(root, $"Sources/hair/hair-{n:00}.png"));
            if (source is null || source.Width != 1254 || source.Height != 1254) throw new InvalidDataException($"Invalid hair source {n}.");
            using var mask = Mask(n);
            using var cut = new SKBitmap(1254, 1254, SKColorType.Rgba8888, SKAlphaType.Premul) { Pixels = source.Pixels };
            var pixels = cut.Pixels; var maskPixels = mask.Pixels;
            var bg = source.GetPixel(0, 0);
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                int distance = Math.Max(Math.Abs(c.Red - bg.Red), Math.Max(Math.Abs(c.Green - bg.Green), Math.Abs(c.Blue - bg.Blue)));
                float matte = Math.Clamp((distance - 5) / 10f, 0, 1);
                // Below the jaw, neutral/blue pixels belong to the source uniform.
                // Retain the warm strand colors and remove that garment spill.
                if (n != 4 && i / 1254 > 630) matte *= Math.Clamp((c.Red - c.Blue - 1) / 5f, 0, 1);
                pixels[i] = c.WithAlpha((byte)(maskPixels[i].Alpha * matte));
            }
            cut.Pixels = pixels;
            using var placed = cut.Resize(new SKImageInfo(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
            string path = $"Parts/hair/hair-{n:00}.png";
            Save(root, placed, path); Save(root, mask, $"Masks/hair/hair-{n:00}.png");
            parts.Add(new() { Id = $"female.frontal.hair.{n:000}", Category = "Hair", DisplayName = Labels[n - 1], Texture = path, IntroducedInVersion = 7 });
        }
        var style = assets.Style with
        {
            LibraryVersion = 7, SupportedLibraryVersions = [4, 5, 6, 7],
            Layers = [.. assets.Style.Layers.Where(l => l.Category != "Hair"), new("Hair", IntroducedInVersion: 7)]
        };
        File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
        File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
        Console.WriteLine("Built 10 hairstyle overlays; v7, 8100 combinations. Bald v4/v5/v6 retained.");
    }
    private static SKBitmap Mask(int n)
    {
        // Registration contours are measured on the generated 1254px portraits.
        // The cavity excludes facial features and the central collar. The small
        // feather at the hairline blends the original scalp with the hair roots.
        SKPoint[] hairline = n switch
        {
            1 => P(418,396, 431,325, 458,291, 505,275, 556,263, 602,241, 650,218, 700,197, 747,196, 776,241, 802,299, 815,398),
            2 => P(425,439, 439,367, 464,298, 494,231, 540,181, 590,163, 648,151, 700,178, 752,222, 782,286, 801,365, 815,439),
            3 => P(421,438, 429,382, 456,327, 497,292, 525,257, 548,193, 588,169, 644,155, 705,164, 756,212, 798,291, 819,350),
            4 => P(420,441, 439,374, 462,300, 497,230, 542,181, 584,163, 632,155, 688,169, 739,213, 781,281, 801,357, 817,442),
            5 => P(428,435, 428,370, 451,322, 500,289, 537,254, 554,198, 586,169, 646,154, 708,165, 762,212, 801,294, 820,350),
            6 => P(419,349, 444,320, 485,311, 527,309, 568,305, 603,307, 640,307, 682,307, 723,307, 765,312, 797,332, 816,353),
            7 => P(418,399, 437,317, 467,268, 507,216, 556,181, 610,159, 665,152, 708,145, 747,185, 781,239, 808,312, 820,398),
            8 => P(416,353, 433,273, 465,218, 513,180, 564,155, 615,149, 669,150, 718,167, 766,205, 796,256, 813,317, 822,351),
            9 => P(416,356, 435,283, 464,235, 500,204, 550,176, 606,161, 662,151, 711,160, 756,197, 790,251, 813,317, 822,356),
            _ => P(418,419, 433,361, 459,318, 506,299, 557,282, 608,252, 653,214, 697,169, 731,190, 769,239, 802,296, 821,349)
        };
        int bottom = n switch { 1 => 427, 2 => 654, 3 => 808, 4 => 1210, 5 => 809, 6 => 661, 7 => 630, 8 => 822, 9 => 382, _ => 842 };
        var bitmap = new SKBitmap(1254, 1254); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var white = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawRect(new SKRect(190, 0, 1010, bottom), white);
        var cavity = hairline.ToList();
        bool ears = n is 3 or 5 or 6 or 7 or 8 or 9 or 10;
        cavity.AddRange(ears ? P(847,332, 851,384, 838,454, 816,481) : P(813,477));
        cavity.AddRange(P(795,494, 786,554, 757,609, 750,670));
        if (n == 4) cavity.AddRange(P(763,753, 776,896, 779,1053, 741,1134, 699,1185, 699,1254, 541,1254, 541,1180, 499,1137, 480,1060, 480,903, 478,758));
        else cavity.AddRange(P(779,737, 795,bottom + 20, 450,bottom + 20, 470,734));
        cavity.AddRange(P(485,671, 481,617, 449,558, 439,496));
        cavity.AddRange(ears ? P(421,481, 398,451, 387,397, 390,340) : P(425,476));
        using var path = Smooth(cavity.ToArray());
        using var feather = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3);
        using var clear = new SKPaint { Color = SKColors.White, BlendMode = SKBlendMode.DstOut, IsAntialias = true, MaskFilter = feather };
        canvas.DrawPath(path, clear);
        // Exposed ear silhouettes are preserved for the short/updo styles.
        if (n is 1 or 7 or 9)
        {
            using var ear = new SKPath(); ear.AddOval(new SKRect(363, 327, 416, 502)); ear.AddOval(new SKRect(824, 327, 877, 502));
            canvas.DrawPath(ear, clear);
        }
        if (n is 1 or 7 or 9)
        {
            canvas.DrawRect(new SKRect(0, 428, 1254, 1254), clear);
        }
        if (n == 8) canvas.DrawRect(new SKRect(0, 489, 477, 1254), clear);
        if (n == 4)
        {
            using var outer = Smooth(P(180,0, 1040,0, 1040,740, 941,810, 953,900, 965,1000, 947,1090, 915,1143, 872,1170, 813,1182, 754,1180, 716,1170, 550,1170, 506,1180, 449,1183, 391,1179, 346,1154, 317,1126, 295,1077, 284,1022, 295,930, 319,818, 180,740));
            using var clip = new SKBitmap(1254, 1254); using (var cc = new SKCanvas(clip)) { cc.Clear(SKColors.Transparent); cc.DrawPath(outer, white); }
            using var keep = new SKPaint { BlendMode = SKBlendMode.DstIn }; canvas.DrawBitmap(clip, 0, 0, keep);
        }
        // Gaussian feathering must never alter the selectable eyes or the front
        // collar, even by a single translucent pixel after thumbnail filtering.
        using var protectedArea = new SKPaint { BlendMode = SKBlendMode.Clear };
        canvas.DrawRect(new SKRect(463, 321, 605, 400), protectedArea);
        canvas.DrawRect(new SKRect(643, 321, 787, 400), protectedArea);
        canvas.DrawRect(new SKRect(500, 737, 741, 852), protectedArea);
        return bitmap;
    }
    private static SKPoint[] P(params float[] values) => Enumerable.Range(0, values.Length / 2).Select(i => new SKPoint(values[i * 2], values[i * 2 + 1])).ToArray();
    private static SKPath Smooth(SKPoint[] p)
    {
        var path = new SKPath(); path.MoveTo(p[0]);
        for (int i = 0; i < p.Length; i++)
        {
            var a = p[(i + p.Length - 1) % p.Length]; var b = p[i]; var c = p[(i + 1) % p.Length]; var d = p[(i + 2) % p.Length];
            path.CubicTo(b.X + (c.X - a.X) / 6, b.Y + (c.Y - a.Y) / 6, c.X - (d.X - b.X) / 6, c.Y - (d.Y - b.Y) / 6, c.X, c.Y);
        }
        path.Close(); return path;
    }
    public static void Bake(string root)
    {
        var assets = new PortraitAssetRepository(root); using var renderer = new PortraitRenderer(assets);
        var baseline = new PortraitGenerator(assets).Generate(1, 7);
        foreach (string category in new[] { "Eyes", "Nose", "Mouth", "Chin", "Clothes" }) baseline = baseline with { Parts = baseline.Parts.SetItem(category, $"female.frontal.{category.ToLowerInvariant()}.001") };
        using var gallery = new SKBitmap(2000, 800); using var canvas = new SKCanvas(gallery); canvas.Clear(new SKColor(28, 40, 54));
        using var label = new SKPaint { Color = SKColors.White, TextSize = 18, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
        for (int n = 1; n <= 10; n++)
        {
            var appearance = baseline with { Parts = baseline.Parts.SetItem("Hair", $"female.frontal.hair.{n:000}") };
            int x = (n - 1) % 5 * 400, y = (n - 1) / 5 * 400;
            canvas.DrawImage(renderer.Render(appearance, 400), x, y);
            canvas.DrawText($"{n:00} · {Labels[n - 1]}", x + 10, y + 388, label);
            using var portrait = SKBitmap.FromImage(renderer.Render(appearance, 1024)); Save(root, portrait, $"Generated/hair/portrait-{n:00}.png");
        }
        Save(root, gallery, "Generated/hair-comparison.png");
        using var outfits = new SKBitmap(1600, 1920); using var oc = new SKCanvas(outfits); oc.Clear(new SKColor(28, 40, 54));
        for (int suit = 1; suit <= 3; suit++)
        for (int n = 1; n <= 10; n++)
        {
            var appearance = baseline with { Parts = baseline.Parts.SetItem("Hair", $"female.frontal.hair.{n:000}").SetItem("Clothes", $"female.frontal.clothes.{suit:000}") };
            int x = (n - 1) % 5 * 320, y = ((suit - 1) * 2 + (n - 1) / 5) * 320;
            oc.DrawImage(renderer.Render(appearance, 320), x, y); oc.DrawText($"{n:00} / {suit}", x + 10, y + 308, label);
        }
        Save(root, outfits, "Generated/hair-costumes.png");
    }
    private static void Save(string root, SKBitmap bitmap, string path)
    {
        string target = Path.Combine(root, path); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(target); data.SaveTo(file);
    }
}
