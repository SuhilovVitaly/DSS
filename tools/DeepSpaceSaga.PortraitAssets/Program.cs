using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using SkiaSharp;

if (args.Length < 2 || args[0] is not ("build" or "validate" or "bake" or "preview"))
    throw new ArgumentException("Usage: PortraitAssets <build|validate|bake|preview> <pack-directory> [reference-directory]");
string mode = args[0], root = Path.GetFullPath(args[1]);
if (mode == "build") Build();
var repository = new PortraitAssetRepository(root);
Console.WriteLine($"Validated {repository.Parts.Count} components, {repository.Style.Layers.Length} layers.");
if (mode is "build" or "bake") Bake();
if (mode == "preview") PreviewWindow();

void Build()
{
    Directory.CreateDirectory(Path.Combine(root, "Parts"));
    Directory.CreateDirectory(Path.Combine(root, "Masks"));
    using var atlas = SKBitmap.Decode(Path.Combine(root, "Sources", "face-atlas.png"));
    using var outfits = SKBitmap.Decode(Path.Combine(root, "Sources", "outfit-atlas.png"));
    var parts = new List<PortraitPart>();
    var style = new PortraitStyleProfile
    {
        LibraryVersion = 2, SupportedLibraryVersions = [1, 2],
        Layers = [new("HairBack"), new("Face"), new("Clothes"), new("Eyes"), new("Eyebrows"), new("Nose"), new("Mouth"), new("HairFront"), new("Accessory", false)],
        Resolutions = new() { ["Thumbnail"] = 128, ["Dialogue"] = 256, ["CharacterScreen"] = 512, ["HighResolution"] = 1024 },
        Anchors = new()
        {
            ["FaceCenter"] = new(.5f, .40f), ["EyeLeft"] = new(.402f, .455f), ["EyeRight"] = new(.598f, .455f),
            ["Nose"] = new(.5f, .532f), ["Mouth"] = new(.5f, .602f), ["Chin"] = new(.5f, .66f),
            ["EarLeft"] = new(.286f, .46f), ["EarRight"] = new(.714f, .46f), ["HairCenter"] = new(.5f, .12f),
            ["NeckCenter"] = new(.5f, .70f), ["ShoulderLeft"] = new(.15f, .818f), ["ShoulderRight"] = new(.85f, .818f)
        },
        Palettes = new()
        {
            ["Skin"] = ["#E9C6AD", "#D9AC8B", "#BD8B68", "#976847", "#EDCFBD", "#CA987C"],
            ["Hair"] = ["#302B2C", "#594037", "#926A46", "#BDA475", "#834637", "#ADB0B6", "#4C566E", "#643E53"],
            ["Eyes"] = ["#648C9A", "#7A8A58", "#97734C", "#626F8B", "#5C4538", "#8B9F91"]
        },
        RenderStyle = new()
        {
            ["referenceFolder"] = "Images/Persons/W", ["adaptation"] = "Semi-realistic anime; mature frontal female heads, restrained soft shading.",
            ["light"] = "Upper-left soft key, neutral ambient fill", ["outline"] = "Fine dark brown internal lines; soft external silhouette",
            ["detailLevel"] = "Medium", ["geometry"] = "Authored normalized anchors; aligned RGBA canvas. Face includes ears and neck in this pack."
        }
    };
    // Source rectangles are atlas-authoring calibration, never runtime face geometry.
    // AI atlases are normalized to a 1254-unit authoring space before extraction.
    for (int i = 0; i < 4; i++)
    {
        float x = i * 313.5f;
        Add("Face", i, atlas, new(x + 30, 0, x + 284, 315), new(98, 38, 414, 430), "Skin", "#E9C6AD");
        Add("HairBack", i, atlas, new(x, 316, x + 313.5f, 631), new(45, 30, 467, 482), "Hair");
        Add("HairFront", i, atlas, new(x + 15, 640, x + 299, 943), new(104, 25, 408, 383), "Hair");
        Add("Eyebrows", i, atlas, new(x + 52, 1016, x + 263, 1046), new(155, 194, 357, 217), "Hair", "#606060");
        Add("Eyes", i, atlas, new(x + 53, 1046, x + 262, 1095), new(155, 215, 357, 259), "Eyes", "#808080");
        Add("Nose", i, atlas, new(x + 132, 1090, x + 180, 1138), new(240, 256, 272, 289), "Skin", "#E9C6AD");
        Add("Mouth", i, atlas, new(x + 115, 1140, x + 197, 1186), new(227, 293, 285, 324));
        // Outfits source image has a 2:1 aspect ratio. Normalize each square cell to 512 units.
        float cell = outfits.Width / 4f;
        Add("Clothes", i, outfits, new(i * cell, 0, (i + 1) * cell, cell), new(20, 307, 492, 646), sourceUnits: outfits.Width);
    }
    string[] faceNames = ["Круглое", "Квадратное", "Сердцевидное", "Ромбовидное", "Вытянутое", "Грушевидное", "Высокие скулы", "Полные щёки"];
    string[] faceTags = ["round", "square", "heart", "diamond", "long", "pear", "sculpted", "full"];
    using (var diversity = SKBitmap.Decode(Path.Combine(root, "Sources", "face-diversity-atlas.png")))
    using (var socket = SKBitmap.Decode(Path.Combine(root, "Parts", "face-01.png")))
    {
        for (int i = 0; i < 8; i++)
        {
            using var normalized = NormalizeFace(diversity, socket, i);
            Add("Face", i + 4, normalized, new(0, 0, 512, 512), new(0, 0, 512, 512), "Skin", "#E9C6AD", sourceUnits: 512);
            parts[^1] = parts[^1] with { DisplayName = faceNames[i], IntroducedInVersion = 2,
                Tags = ["female", "human", "adult", "face." + faceTags[i]] };
        }
    }
    float oc = outfits.Width / 4f;
    Add("Accessory", 0, outfits, new(oc * .06f, oc * 1.34f, oc * .94f, oc * 1.64f), new(146, 218, 366, 274), sourceUnits: outfits.Width);
    Add("Accessory", 1, outfits, new(oc * 1.23f, oc * 1.35f, oc * 1.77f, oc * 1.60f), new(135, 274, 377, 302), sourceUnits: outfits.Width);
    Add("Accessory", 2, outfits, new(oc * 2.32f, oc * 1.23f, oc * 2.73f, oc * 1.74f), new(351, 243, 398, 317), sourceUnits: outfits.Width);
    Add("Accessory", 3, outfits, new(oc * 3.24f, oc * 1.17f, oc * 3.75f, oc * 1.75f), new(135, 271, 377, 337), sourceUnits: outfits.Width);
    // Small iris masks protect sclera and eyelashes. Coordinates calibrated against each eye variant.
    for (int i = 0; i < 4; i++)
    {
        using var mask = new SKBitmap(512, 512, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(mask); canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawOval(202, 237, 11, 10, paint); canvas.DrawOval(307, 237, 11, 10, paint);
        Save(mask, $"Masks/iris-{i + 1:D2}.png");
    }
    File.WriteAllText(Path.Combine(root, "portrait-style.json"), JsonSerializer.Serialize(style, AppearanceSerializer.Options));
    File.WriteAllText(Path.Combine(root, "parts.json"), JsonSerializer.Serialize(parts, AppearanceSerializer.Options));
    if (args.Length > 2)
    {
        var references = Directory.GetFiles(args[2], "*.png").Order(StringComparer.Ordinal).Select(path =>
        {
            using var bitmap = SKBitmap.Decode(path);
            return new { file = Path.GetFileName(path), width = bitmap.Width, height = bitmap.Height, alpha = bitmap.AlphaType.ToString(), sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))) };
        }).ToArray();
        File.WriteAllText(Path.Combine(root, "reference-analysis.json"), JsonSerializer.Serialize(new { references, notes = "All six available references retained as golden sources. Frontal natural human proportions, soft highlights and subdued hair. Mixed green-screen and transparent backgrounds; originals unmodified. Geometry is manually calibrated to the new component atlas, not automatically inferred landmarks." }, AppearanceSerializer.Options));
    }

    void Add(string category, int index, SKBitmap source, SKRect crop, SKRect dest, string? channel = null, string baseColor = "#808080", float sourceUnits = 1254)
    {
        float scale = source.Width / sourceUnits;
        crop = new(crop.Left * scale, crop.Top * scale, crop.Right * scale, crop.Bottom * scale);
        using var bitmap = new SKBitmap(512, 512, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            // Raise the entire shoulder/collar assembly, including its opening mask.
            // Chin stays at ~338; front necklines now sit at 372..381 instead of 414..423.
            // This halves the exposed neck without changing facial or clothing proportions.
            if (category == "Clothes") canvas.Translate(0, -42);
            // Keep the neck extending INTO the collar. Trim only the atlas's shoulder
            // flare; a horizontal cutoff at the collar's back rim leaves a floating head.
            if (category == "Face" && index < 4)
            {
                using var silhouette = new SKPath();
                silhouette.MoveTo(0, 0); silhouette.LineTo(512, 0);
                silhouette.LineTo(512, 340); silhouette.LineTo(324, 340);
                silhouette.CubicTo(322, 361, 316, 387, 316, 430);
                silhouette.LineTo(196, 430);
                silhouette.CubicTo(196, 387, 190, 361, 188, 340);
                silhouette.LineTo(0, 340); silhouette.Close();
                canvas.ClipPath(silhouette, antialias: true);
            }
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(source, crop, dest, paint);
            if (category == "Clothes")
            {
                // The atlas is painted on an empty mannequin. Its opaque rear rim and
                // cavity must sit BEHIND skin; the front rim and undershirt stay in front.
                // These per-outfit paths are authored in the same 512px coordinate space.
                float bottom = new[] { 423f, 414f, 423f, 423f }[index];
                using var opening = new SKPath();
                opening.MoveTo(195, 340); opening.LineTo(317, 340);
                opening.LineTo(317, 393);
                opening.CubicTo(312, bottom - 7, 293, bottom, 256, bottom);
                opening.CubicTo(219, bottom, 200, bottom - 7, 195, 393);
                opening.Close();
                using var erase = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Clear };
                canvas.DrawPath(opening, erase);
            }
        }
        string path = $"Parts/{category.ToLowerInvariant()}-{index + 1:D2}.png";
        Save(bitmap, path);
        parts.Add(new PortraitPart
        {
            Id = $"female.{category.ToLowerInvariant()}.{index + 1:D3}", Category = category, Texture = path,
            ColorChannel = channel, BaseColor = baseColor,
            ColorMask = category == "Eyes" ? $"Masks/iris-{index + 1:D2}.png" : null,
            Tags = ["female", "human", "adult", category == "HairBack" && index < 2 ? "hair.long" : "standard"],
            // A comms earpiece should remain visible; long hair hides its microphone mount.
            ExcludesTags = category == "Accessory" && index == 2 ? ["hair.long"] : [],
            Weight = category == "Accessory" ? .4 : 1
        });
    }
}

// Import normalization: the source atlas has a neutral checker matte rather than alpha.
// Find the warm connected skin span on each scanline, then place the authored silhouette
// using measured crown/chin landmarks. No eye, mouth, hair, or outfit transforms are involved.
SKBitmap NormalizeFace(SKBitmap atlas, SKBitmap socket, int index)
{
    int left = (int)Math.Round(index % 4 * atlas.Width / 4.0);
    int right = (int)Math.Round((index % 4 + 1) * atlas.Width / 4.0);
    int top = (int)Math.Round(index / 4 * atlas.Height / 2.0);
    int bottom = (int)Math.Round((index / 4 + 1) * atlas.Height / 2.0);
    using var cutout = new SKBitmap(right - left, bottom - top, SKColorType.Rgba8888, SKAlphaType.Premul);
    cutout.Erase(SKColors.Transparent);
    for (int y = 0; y < cutout.Height; y++)
    {
        int start = -1, end = -1;
        for (int x = 0; x < cutout.Width; x++)
        {
            var color = atlas.GetPixel(left + x, top + y);
            if (color.Red - color.Green >= 6 && color.Red - color.Blue >= 16)
            { if (start < 0) start = x; end = x; }
        }
        if (end - start < 3) continue;
        for (int x = start + 1; x < end; x++)
        {
            var color = atlas.GetPixel(left + x, top + y);
            cutout.SetPixel(x, y, color.WithAlpha(255));
        }
    }
    float[] crowns = [40, 29, 27, 37, 16, 31, 29, 30];
    float[] chins = [375, 365, 375, 375, 363, 373, 363, 363];
    float[] targetCrowns = [58, 58, 56, 60, 56, 62, 58, 57];
    float[] targetChins = [332, 338, 338, 338, 344, 336, 340, 333];
    float[] widths = [.83f, .86f, .84f, .86f, .90f, .84f, .88f, .87f];
    float sy = (targetChins[index] - targetCrowns[index]) / (chins[index] - crowns[index]);
    float dx = 256 - cutout.Width * widths[index] / 2;
    float dy = targetCrowns[index] - crowns[index] * sy;
    using var head = new SKBitmap(512, 512, SKColorType.Rgba8888, SKAlphaType.Premul);
    using (var canvas = new SKCanvas(head))
    {
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(cutout, new SKRect(dx, dy, dx + cutout.Width * widths[index], dy + cutout.Height * sy), paint);
    }
    // Keep the crown within the existing hairstyle envelope. Forehead/temple contours
    // can narrow and cheekbones can widen, but bald skin must not protrude above a wig.
    for (int y = 0; y < 150; y++)
    for (int x = 0; x < 512; x++)
    {
        var p = head.GetPixel(x, y);
        head.SetPixel(x, y, p.WithAlpha(Math.Min(p.Alpha, socket.GetPixel(x, y).Alpha)));
    }
    // Preserve the proven neck socket for EVERY new jaw. Fade only the lower neck
    // into it, below the lowest chin, avoiding seams or renewed neck-length changes.
    for (int y = 346; y < 512; y++)
    for (int x = 0; x < 512; x++)
    {
        var p = head.GetPixel(x, y);
        head.SetPixel(x, y, p.WithAlpha((byte)(p.Alpha * Math.Clamp((362 - y) / 16f, 0, 1))));
    }
    var result = new SKBitmap(512, 512, SKColorType.Rgba8888, SKAlphaType.Premul);
    using (var canvas = new SKCanvas(result))
    {
        canvas.Clear(SKColors.Transparent);
        canvas.Save(); canvas.ClipRect(new SKRect(0, 326, 512, 512)); canvas.DrawBitmap(socket, 0, 0); canvas.Restore();
        canvas.DrawBitmap(head, 0, 0);
    }
    return result;
}

void Save(SKBitmap bitmap, string path)
{
    using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var file = File.Create(Path.Combine(root, path)); data.SaveTo(file);
}

void Bake()
{
    Directory.CreateDirectory(Path.Combine(root, "Generated"));
    var generator = new PortraitGenerator(repository);
    using var renderer = new PortraitRenderer(repository);
    int[] seeds = [1, 10, 100, 1000, 123456, 987654321];
    using var contact = new SKBitmap(512 * 3, 512 * 2);
    using var canvas = new SKCanvas(contact); canvas.Clear(new SKColor(24, 32, 44));
    for (int i = 0; i < seeds.Length; i++)
    {
        var appearance = generator.Generate(seeds[i]);
        var portrait = renderer.Render(appearance);
        using var data = portrait.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(root, "Generated", $"seed-{seeds[i]}.png"), data.ToArray());
        File.WriteAllText(Path.Combine(root, "Generated", $"seed-{seeds[i]}.json"), AppearanceSerializer.Serialize(appearance));
        canvas.DrawImage(portrait, (i % 3) * 512, (i / 3) * 512);
    }
    Save(contact, "Generated/contact-sheet.png");
    // Review every face/outfit pairing with swept-back hair and a contrasting backdrop.
    using (var matrix = new SKBitmap(512 * 4, 512 * repository.Parts.Count(p => p.Category == "Face")))
    using (var review = new SKCanvas(matrix))
    {
        review.Clear(new SKColor(53, 74, 87));
        var basis = generator.Generate(1000, 1) with { LibraryVersion = 2 };
        for (int face = 1; face <= repository.Parts.Count(p => p.Category == "Face"); face++)
        for (int outfit = 1; outfit <= 4; outfit++)
        {
            var appearance = basis with
            {
                Parts = basis.Parts.SetItem("Face", $"female.face.{face:D3}")
                    .SetItem("Clothes", $"female.clothes.{outfit:D3}")
                    .SetItem("HairBack", "female.hairback.004")
                    .SetItem("HairFront", "female.hairfront.003").Remove("Accessory")
            };
            review.DrawImage(renderer.Render(appearance), (outfit - 1) * 512, (face - 1) * 512);
        }
        Save(matrix, "Generated/neck-collar-combinations.png");
    }
    // Same hair, features, skin and uniform: differences here come ONLY from face geometry.
    using (var comparison = new SKBitmap(512 * 4, 512 * 2))
    using (var display = new SKCanvas(comparison))
    {
        display.Clear(new SKColor(24, 32, 44));
        var basis = generator.Generate(1000, 1) with { LibraryVersion = 2 };
        using var label = new SKPaint { Color = new SKColor(228, 232, 237), TextSize = 17, IsAntialias = true,
            Typeface = DeepSpaceSaga.Client.UI.Controls.MenuStyle.TypefaceRegular };
        for (int i = 0; i < 8; i++)
        {
            var appearance = basis with { Parts = basis.Parts.SetItem("Face", $"female.face.{i + 5:D3}")
                .SetItem("HairBack", "female.hairback.004").SetItem("HairFront", "female.hairfront.003")
                .SetItem("Clothes", "female.clothes.003").Remove("Accessory") };
            float x = i % 4 * 512, y = i / 4 * 512;
            display.DrawImage(renderer.Render(appearance), x, y);
            display.DrawText(repository.Get(appearance.Parts["Face"]).DisplayName, x + 16, y + 26, label);
        }
        Save(comparison, "Generated/face-diversity.png");
    }
    var clock = System.Diagnostics.Stopwatch.StartNew();
    for (int seed = 0; seed < 10_000; seed++)
    {
        var appearance = generator.Generate(seed); repository.ValidateAppearance(appearance);
        var portrait = renderer.Render(appearance, 128);
        if (portrait.Width != 128 || portrait.Height != 128 || portrait.AlphaType == SKAlphaType.Opaque)
            throw new InvalidDataException($"Bad rendered portrait at seed {seed}.");
    }
    Console.WriteLine($"10,000 generated and rendered combinations passed in {clock.Elapsed.TotalSeconds:F1}s. Cache bounded at {renderer.CachedCount}.");
}

void PreviewWindow()
{
    var screen = new DeepSpaceSaga.Client.UI.Screens.TempCharacterImage.TempCharacterImageScreen(root);
    screen.OnActivated();
    try
    {
        using var bitmap = new SKBitmap(1440, 960); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(6, 12, 20));
        screen.Render(canvas, 1440, 960);
        screen.OnMouseDown(1050, 240, Silk.NET.Input.MouseButton.Left);
        for (int i = 0; i < 11; i++) screen.OnKeyDown(Silk.NET.Input.Key.Backspace);
        foreach (char c in "123456") screen.OnTextInput(c);
        screen.OnKeyDown(Silk.NET.Input.Key.Enter);
        canvas.Clear(new SKColor(6, 12, 20)); screen.Render(canvas, 1440, 960);
        Save(bitmap, "Generated/workshop-preview.png");
    }
    finally { screen.OnDeactivated(); }
}
