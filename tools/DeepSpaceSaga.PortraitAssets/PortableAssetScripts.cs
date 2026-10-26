using System.Globalization;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

internal static class PortableAssetScripts
{
    internal static void Run(string[] args)
    {
        if (args[0] == "export-tactical") Export(args);
        else Prepare(args);
    }

    private static SKBitmap Decode(string path) => SKBitmap.Decode(path) ?? throw new InvalidDataException("Invalid image: " + path);
    private static void Save(SKBitmap bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
    private static string Relative(string path) => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
    private static SKBitmap Resize(SKBitmap image, int width, int height) => image.Resize(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High)
        ?? throw new InvalidDataException("Cannot resize image");

    private static void Export(string[] args)
    {
        if (args.Length != 4) throw new ArgumentException("export-tactical <manifest> <repo> <install:true|false>");
        string manifest = Path.GetFullPath(args[1]), assetRoot = Path.GetDirectoryName(manifest)!;
        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        var entries = document.RootElement.EnumerateArray().ToArray();
        foreach (var entry in entries)
        {
            string file = Relative(entry.GetProperty("file").GetString()!);
            using var source = Decode(Path.Combine(assetRoot, Relative(entry.GetProperty("master").GetString()!)));
            using var output = Resize(source, entry.GetProperty("width").GetInt32(), entry.GetProperty("height").GetInt32());
            Save(output, Path.Combine(assetRoot, "GameSessionScreenUI", file));
            if (bool.Parse(args[3])) Save(output, Path.Combine(args[2], "src/DeepSpaceSaga.Client/Images/UI/GameSessionScreenUI", file));
        }
        int rows = (entries.Length + 3) / 4;
        using var sheet = new SKBitmap(1328, rows * 142 + 112);
        using var canvas = new SKCanvas(sheet);
        canvas.Clear(new SKColor(9, 18, 27));
        using var text = new SKPaint { Typeface = UiFonts.Ui, TextSize = 14, Color = new SKColor(230, 239, 245), IsAntialias = true };
        using var heading = new SKPaint { Typeface = UiFonts.UiBold, TextSize = 27, Color = text.Color, IsAntialias = true };
        canvas.DrawText("DEEP SPACE SAGA / TACTICAL CONTROLS", 24, 43, heading);
        canvas.DrawText("Trade UI palette / normal + hover / original runtime sizes", 24, 68, text);
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            string file = Relative(entry.GetProperty("file").GetString()!);
            int x = 24 + i % 4 * 320, y = 100 + i / 4 * 142;
            using var png = Decode(Path.Combine(assetRoot, "GameSessionScreenUI", file));
            int size = Math.Min(64, entry.GetProperty("width").GetInt32());
            canvas.DrawBitmap(png, new SKRect(x, y, x + size, y + size));
            if (entry.GetProperty("width").GetInt32() == 64) canvas.DrawBitmap(png, new SKRect(x + 76, y + 8, x + 124, y + 56));
            canvas.DrawText(entry.GetProperty("label").GetString() ?? "", x, y + 82, text);
            text.Color = new SKColor(147, 173, 190);
            canvas.DrawText(Path.GetFileName(file), x, y + 104, text);
            text.Color = new SKColor(230, 239, 245);
        }
        canvas.Flush();
        Save(sheet, Path.Combine(assetRoot, "tactical-buttons-contact-sheet.png"));
        Console.WriteLine($"Exported {entries.Length} button images. Installed: {args[3]}");
    }

    private static void Prepare(string[] args)
    {
        if (args.Length != 9) throw new ArgumentException("prepare-portrait <source> <width> <height> <chinY> <repo> <gender> <output> <preview>");
        double width = double.Parse(args[2], CultureInfo.InvariantCulture), height = double.Parse(args[3], CultureInfo.InvariantCulture), chin = double.Parse(args[4], CultureInfo.InvariantCulture);
        bool male = args[6] == "male";
        if (args[6] is not ("male" or "female")) throw new ArgumentException("Unsupported gender");
        if (!double.IsFinite(width) || width < (male ? 350 : 355) || width > (male ? 410 : 390)) throw new ArgumentException("Anatomical head width outside pack limits");
        if (!double.IsFinite(height) || height < 470 || height > 520) throw new ArgumentException("Skull-to-chin height must be 470..520px");
        if (!double.IsFinite(chin) || chin < 540 || chin > 570) throw new ArgumentException("Lowest chin must be at y540..570");
        string pack = male ? "M4" : "W4";
        using var source = Decode(Path.GetFullPath(args[1]));
        if (source.Width != source.Height) throw new InvalidDataException("Source must be square; do not distort an image to fit the canvas");
        using var portrait = Resize(source, 1024, 1024);
        foreach (var (x, y) in new[] { (0, 0), (1023, 0), (0, 1023), (1023, 1023), (100, 500), (924, 500) })
            if (portrait.GetPixel(x, y).Alpha != 0) throw new InvalidDataException("Background is not transparent at the outer canvas");
        CheckAlpha(portrait, 430, 594, 575, 625, "Incomplete neck");
        using var gallery = new SKBitmap(1536, 512);
        using var canvas = new SKCanvas(gallery);
        canvas.Clear(new SKColor(28, 40, 54));
        for (int n = 1; n <= 3; n++)
        {
            using var costume = Decode(Path.Combine(args[5], $"src/DeepSpaceSaga.Client/Images/Persons/{pack}/Clothes/clothes-{n:00}.png"));
            using var composite = new SKBitmap(1024, 1024);
            using var cg = new SKCanvas(composite);
            cg.Clear(SKColors.Transparent);
            cg.DrawBitmap(portrait, 0, 0); cg.DrawBitmap(costume, 0, 0); cg.Flush();
            CheckAlpha(composite, 440, 580, 568, 656, $"Collar gap in costume {n}");
            using var preview = Resize(composite, 512, 512);
            canvas.DrawBitmap(preview, (n - 1) * 512, 0);
        }
        canvas.Flush();
        string output = Path.GetFullPath(args[7]), previewDirectory = Path.GetFullPath(args[8]);
        Directory.CreateDirectory(output); Directory.CreateDirectory(previewDirectory);
        string suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        string saved = Path.Combine(output, $"CHR-{DateTime.Now:yyyyMMdd-HHmmss}-{suffix}.png");
        string previewPath = Path.Combine(previewDirectory, $"character-generator-{Guid.NewGuid():N}.png");
        try { Save(portrait, saved); Save(gallery, previewPath); }
        catch { File.Delete(saved); File.Delete(previewPath); throw; }
        Console.WriteLine(JsonSerializer.Serialize(new { Portrait = saved, Preview = previewPath, Gender = args[6], Pack = pack, HeadWidth = width, HeadHeight = height, ChinY = chin, CostumesChecked = 3, Note = "Inspect preview visually; delete this exact temporary preview after inspection." }));
    }

    private static void CheckAlpha(SKBitmap image, int left, int right, int top, int bottom, string message)
    {
        for (int y = top; y <= bottom; y += 2)
            for (int x = left; x <= right; x += 2)
                if (image.GetPixel(x, y).Alpha < 254) throw new InvalidDataException($"{message} at {x},{y}");
    }
}
