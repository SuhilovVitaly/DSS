using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;
using SkiaSharp;

if (args.Length != 2 || args[0] is not ("build" or "validate" or "bake" or "preview" or "guide" or "variants" or "eyes" or "hair" or "faces" or "fit-eyes" or "features"))
    throw new ArgumentException("Usage: PortraitAssets <build|validate|bake|preview|guide|variants|eyes|hair|faces|fit-eyes|features> <pack-directory>");
string root = Path.GetFullPath(args[1]);
const int size = 1024;
if (args[0] == "fit-eyes") { FaceEyeAssets.Preview(root); return; }
void RebuildFaces()
{
    if (!FaceAssets.HasSources(root)) return;
    FaceAssets.Build(root); FaceAssets.Bake(root);
    if (ReferenceFeatureAssets.HasSources(root)) { ReferenceFeatureAssets.Build(root); ReferenceFeatureAssets.Bake(root); }
}
if (args[0] == "features") { ReferenceFeatureAssets.Build(root); ReferenceFeatureAssets.Bake(root); return; }
if (args[0] == "faces") { RebuildFaces(); return; }
if (args[0] == "hair") { HairAssets.Build(root); HairAssets.Bake(root); RebuildFaces(); return; }
if (args[0] == "guide") { VariantAssets.Guide(root); return; }
if (args[0] == "eyes")
{
    EyeAssets.Build(root); EyeAssets.Bake(root);
    if (HairAssets.HasSources(root)) { HairAssets.Build(root); HairAssets.Bake(root); }
    RebuildFaces();
    return;
}
if (args[0] == "variants")
{
    VariantAssets.Build(root); VariantAssets.Bake(root);
    if (EyeAssets.HasSources(root)) { EyeAssets.Build(root); EyeAssets.Bake(root); }
    if (HairAssets.HasSources(root)) { HairAssets.Build(root); HairAssets.Bake(root); }
    RebuildFaces();
    return;
}
if (args[0] == "build") Build();
var assets = new PortraitAssetRepository(root);
Console.WriteLine($"Validated {assets.Parts.Count} layers, library {assets.Style.LibraryVersion}.");
if (args[0] is "build" or "bake") Bake();
if (args[0] == "build" && File.Exists(Path.Combine(root, "Sources/variants/suit-03.png")))
{ VariantAssets.Build(root); VariantAssets.Bake(root); }
else if (args[0] == "bake" && assets.Style.LibraryVersion >= 5) VariantAssets.Bake(root);
if (args[0] is "build" or "bake" && EyeAssets.HasSources(root))
{
    if (args[0] == "build") EyeAssets.Build(root);
    EyeAssets.Bake(root);
}
if (args[0] is "build" or "bake" && HairAssets.HasSources(root))
{
    if (args[0] == "build") HairAssets.Build(root);
    HairAssets.Bake(root);
}
if (args[0] == "build") RebuildFaces();
else if (args[0] == "bake" && assets.Style.LibraryVersion >= 8) FaceAssets.Bake(root);
if (args[0] == "bake" && assets.Style.LibraryVersion >= 10) ReferenceFeatureAssets.Bake(root);
if (args[0] == "preview")
{
    var screen = new TempCharacterImageScreen(root); screen.OnActivated();
    try
    {
        using var bitmap = new SKBitmap(1440, 960); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(6, 12, 20)); screen.Render(canvas, 1440, 960);
        Save(bitmap, "Generated/workshop-preview.png");
    }
    finally { screen.OnDeactivated(); }
}

void Build()
{
    foreach (string folder in new[] { "Parts", "Masks", "Generated", "Golden" }) Directory.CreateDirectory(Path.Combine(root, folder));
    using var original = SKBitmap.Decode(Path.Combine(root, "Sources/master.png"));
    using var retouched = SKBitmap.Decode(Path.Combine(root, "Sources/blank-retouch.png"));
    if (original.Width != original.Height || original.Width != retouched.Width || original.Height != retouched.Height)
        throw new InvalidDataException("Master and blank retouch must have the same square canvas.");
    using var cutout = CutBackground(original);
    using var master = cutout.Resize(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
    using var blank = retouched.Resize(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
    using var oval = blank.Copy();
    // Use the entire inpainted base, not rectangular retouch islands. Its silhouette
    // comes from the master so AI retouching cannot alter head/ear/neck geometry.
    for (int y=0;y<size;y++)
    for (int x=0;x<size;x++) oval.SetPixel(x,y,oval.GetPixel(x,y).WithAlpha(master.GetPixel(x,y).Alpha));
    // Regions measured on THIS complete 1254px portrait. No component is independently
    // moved, scaled, reshaped or calibrated against a hairstyle or collar.
    var regions = new (string Category, string Label, Func<float, float, float> Distance)[]
    {
        ("Eyebrows", "Брови", (x,y) => Math.Max(Rect(x,y,330,372,604,478), Rect(x,y,643,369,911,478))),
        ("Eyes", "Глаза", (x,y) => Math.Max(Rect(x,y,346,459,610,639), Rect(x,y,642,459,905,639))),
        ("Nose", "Нос", (x,y) => Math.Max(Rect(x,y,571,458,674,681), Ellipse(x,y,618,694,120,96))),
        ("Mouth", "Рот", (x,y) => Rect(x,y,449,738,781,945))
    };
    var components = new List<PortraitPart>();
    foreach (var region in regions)
    {
        using var detail = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var mask = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        detail.Erase(SKColors.Transparent); mask.Erase(SKColors.Transparent);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = region.Distance(x * 1254f / size, y * 1254f / size);
            float coverage = Math.Clamp(distance / (region.Category == "Eyebrows" ? 12 : 24), 0, 1);
            var source = master.GetPixel(x, y);
            detail.SetPixel(x, y, source.WithAlpha((byte)Math.Round(source.Alpha * coverage)));
            mask.SetPixel(x, y, SKColors.White.WithAlpha((byte)Math.Round(255 * coverage)));
        }
        // Feature cores retain original pixels; only surrounding skin is feathered.
        string path = $"Parts/{region.Category.ToLowerInvariant()}.png";
        Save(detail, path); Save(mask, $"Masks/{region.Category.ToLowerInvariant()}.png");
        components.Add(Part(region.Category, region.Label, path));
    }
    Save(oval, "Parts/oval.png"); Save(master, "Golden/master.png");
    components.Insert(0, Part("Face", "Овал лица", "Parts/oval.png"));
    var style = new PortraitStyleProfile
    {
        LibraryVersion = 4, SupportedLibraryVersions = [4], Width = size, Height = size,
        Layers = [new("Face"), new("Eyebrows"), new("Eyes"), new("Nose"), new("Mouth")],
        Resolutions = new() { ["Thumbnail"] = 128, ["Dialogue"] = 256, ["CharacterScreen"] = 512, ["HighResolution"] = size },
        Anchors = new()
        {
            ["Crown"] = Point(617,49), ["EyeLeft"] = Point(480,517), ["EyeRight"] = Point(760,517),
            ["Brow"] = Point(618,435), ["NoseBase"] = Point(618,736), ["Mouth"] = Point(617,826), ["Chin"] = Point(620,1024),
            ["CheekLeft"] = Point(316,630), ["CheekRight"] = Point(919,630),
            ["InnerEyeLeft"] = Point(560,522), ["InnerEyeRight"] = Point(684,522),
            ["OuterEyeLeft"] = Point(398,517), ["OuterEyeRight"] = Point(846,517),
            ["NoseWingLeft"] = Point(550,707), ["NoseWingRight"] = Point(692,707),
            ["MouthLeft"] = Point(503,826), ["MouthRight"] = Point(730,826)
        },
        RenderStyle = new()
        {
            ["referenceFolder"] = "Reference", ["mode"] = "single-frontal-prototype",
            ["description"] = "One bald frontal adult female. Five registered layers from one complete master. No feature warping.",
            ["landmarks"] = "Approximate manual 2D landmarks, not 3D anthropometry or a universal beauty formula."
        }
    };
    Write("portrait-style.json", style); Write("parts.json", components);
    Write("proportions.json", new
    {
        coordinateSystem = "Normalized from newly authored 1254px master; x right, y down. Approximate manual measurements.",
        landmarks = style.Anchors,
        measured = new { eyeHeadFraction = (517.0-49)/(1024-49), middleThird = 736-435, lowerThird = 1024-736,
            mouthLowerThirdFraction = (826.0-736)/(1024-736), eyeSpacingToMeanFissure = (684.0-560)/((560-398+846-684)/2.0),
            mouthToNoseWidth = (730.0-503)/(692-550), mouthToCheekWidth = (730.0-503)/(919-316) },
        limitation = "Hairline is absent on a bald head and is not measured. Canons are guides, not constraints used to deform the image."
    });
    static PortraitPart Part(string category, string label, string path) => new()
    { Id = $"female.frontal.{category.ToLowerInvariant()}.001", Category = category, DisplayName = label, Texture = path, IntroducedInVersion = 4 };
    static PortraitAnchor Point(float x, float y) => new(x / 1254, y / 1254);
}

void Bake()
{
    Directory.CreateDirectory(Path.Combine(root, "Generated"));
    using var renderer = new PortraitRenderer(assets);
    var appearance = new PortraitGenerator(assets).Generate(1, 4);
    using var portrait = SKBitmap.FromImage(renderer.Render(appearance, size));
    Save(portrait, "Generated/portrait.png"); Write("Generated/appearance.json", appearance);
    using var master = SKBitmap.Decode(Path.Combine(root, "Golden/master.png"));
    long totalError = 0; int samples = 0;
    for (int y = 0; y < size; y++)
    for (int x = 0; x < size; x++)
    {
        var a = portrait.GetPixel(x,y); var b = master.GetPixel(x,y);
        if (a.Alpha != b.Alpha) throw new InvalidDataException("Reconstruction changed the silhouette.");
        if (b.Alpha == 255) { totalError += Math.Abs(a.Red-b.Red)+Math.Abs(a.Green-b.Green)+Math.Abs(a.Blue-b.Blue); samples+=3; }
    }
    double meanError = totalError/(double)samples;
    if (meanError > 12) throw new InvalidDataException($"Reconstruction deviates from the master: mean RGB error {meanError:F2}.");
    using var gallery = new SKBitmap(1536, 1024); using var canvas = new SKCanvas(gallery);
    canvas.Clear(new SKColor(28,40,54));
    using var label = new SKPaint { Color = SKColors.White, TextSize = 20, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
    var baselineParts = assets.Parts.Where(p => p.IntroducedInVersion == 4).ToArray();
    for (int i = 0; i <= baselineParts.Length; i++)
    {
        float x = i % 3 * 512, y = i / 3 * 512;
        if (i == baselineParts.Length) canvas.DrawBitmap(portrait, new SKRect(x,y,x+512,y+512));
        else { using var part = SKBitmap.Decode(assets.TexturePath(baselineParts[i].Texture)); canvas.DrawBitmap(part, new SKRect(x,y,x+512,y+512)); }
        canvas.DrawText(i == baselineParts.Length ? "Базовый портрет v4" : baselineParts[i].DisplayName, x+16,y+28,label);
    }
    Save(gallery, "Generated/layers.png");
    using var guides = new SKBitmap(size,size); using var guideCanvas = new SKCanvas(guides);
    guideCanvas.Clear(new SKColor(28,40,54)); guideCanvas.DrawBitmap(portrait,0,0);
    using var line = new SKPaint { Color = new SKColor(99,219,225,170), StrokeWidth = 1, IsAntialias = true };
    using var text = new SKPaint { Color = SKColors.White, TextSize = 17, IsAntialias = true, Typeface = MenuStyle.TypefaceRegular };
    foreach (string key in new[] { "Crown", "Brow", "EyeLeft", "NoseBase", "Mouth", "Chin" })
    {
        float y = assets.Style.Anchors[key].Y * size;
        if (assets.Style.LibraryVersion >= 5) y = (y - VariantAssets.Head.Top) / VariantAssets.Head.Height * size;
        guideCanvas.DrawLine(20,y,size-20,y,line); guideCanvas.DrawText(key,20,y-5,text);
    }
    guideCanvas.DrawLine(618f/1254*size,20,618f/1254*size,size-20,line);
    Save(guides,"Generated/proportion-guide.png");
    Console.WriteLine($"Reconstruction: identical silhouette, mean RGB deviation {meanError:F2}/255 (retouched skin). Feature geometry unchanged.");
}

static float Rect(float x,float y,float left,float top,float right,float bottom) => Math.Min(Math.Min(x-left,right-x),Math.Min(y-top,bottom-y));
static float Ellipse(float x,float y,float cx,float cy,float rx,float ry) => (1-MathF.Sqrt(MathF.Pow((x-cx)/rx,2)+MathF.Pow((y-cy)/ry,2)))*Math.Min(rx,ry);
static SKBitmap CutBackground(SKBitmap source)
{
    int width = source.Width, height = source.Height;
    var pixels = source.Pixels; var visited = new bool[width*height]; var queue = new Queue<int>();
    void Visit(int x,int y)
    {
        if (x<0 || y<0 || x>=width || y>=height || visited[y*width+x]) return;
        visited[y*width+x] = true; var c = source.GetPixel(x,y);
        if (c.Red<70 && c.Green<90 && c.Blue<115 && c.Blue>c.Red+3) queue.Enqueue(y*width+x);
    }
    for (int x=0;x<width;x++) { Visit(x,0); Visit(x,height-1); }
    for (int y=0;y<height;y++) { Visit(0,y); Visit(width-1,y); }
    while (queue.TryDequeue(out int index))
    {
        int x=index%width,y=index/width; pixels[index]=SKColors.Transparent;
        Visit(x-1,y); Visit(x+1,y); Visit(x,y-1); Visit(x,y+1);
    }
    var alpha = new SKBitmap(width,height,SKColorType.Rgba8888,SKAlphaType.Premul);
    alpha.Pixels = pixels; return alpha;
}
void Write<T>(string path,T value) => File.WriteAllText(Path.Combine(root,path),JsonSerializer.Serialize(value,AppearanceSerializer.Options));
void Save(SKBitmap bitmap,string path)
{
    using var image=SKImage.FromBitmap(bitmap); using var data=image.Encode(SKEncodedImageFormat.Png,100);
    using var file=File.Create(Path.Combine(root,path)); data.SaveTo(file);
}
