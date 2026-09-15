using System.Text.Json;
using DeepSpaceSaga.Client.Portraits;
using SkiaSharp;

/// <summary>Registers existing eye sprites to a reference head during asset preparation.</summary>
internal static class FaceEyeAssets
{
    private const float Scale = 1024f / 1254;
    // Pupil centers reviewed on the native 1254px reference portraits.
    // The pair is translated as one sprite; its shape and spacing are retained.
    private static readonly (SKPoint Left, SKPoint Right)[] Eyes =
    [
        (new(480, 502), new(760, 502)), (new(476, 494), new(749, 490)),
        (new(490, 482), new(760, 486)), (new(480, 482), new(751, 482)),
        (new(481, 513), new(758, 510)), (new(483, 500), new(759, 500)),
        (new(478, 489), new(750, 484)), (new(490, 482), new(766, 486)),
        (new(490, 488), new(757, 489)), (new(492, 483), new(762, 486))
    ];
    public static Dictionary<int, string> Build(string root, int face, SKBitmap target)
    {
        var result = new Dictionary<int, string>();
        SKPoint[] targetEyes = [new(Eyes[face - 7].Left.X * Scale, Eyes[face - 7].Left.Y * Scale), new(Eyes[face - 7].Right.X * Scale, Eyes[face - 7].Right.Y * Scale)];
        var registration = new List<object>();
        using var mask = SKBitmap.Decode(Path.Combine(root, "Masks/eyes.png"));
        for (int n = 2; n <= 10; n++)
        {
            using var native = SKBitmap.Decode(Path.Combine(root, $"Sources/eyes/eye-{n:00}.png"));
            using var donor = native.Resize(new SKImageInfo(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
            SKPoint[] donorEyes = [new(480 * Scale, 517 * Scale), new(760 * Scale, 517 * Scale)];
            int dx = (int)MathF.Round((targetEyes[0].X + targetEyes[1].X - donorEyes[0].X - donorEyes[1].X) / 2);
            int dy = (int)MathF.Round((targetEyes[0].Y + targetEyes[1].Y - donorEyes[0].Y - donorEyes[1].Y) / 2);
            var offsets = new SKPoint3[2];
            for (int side = 0; side < 2; side++)
            {
                // Sample only the surrounding skin, below the eye opening.
                int left = (int)((side == 0 ? 383 : 679) * Scale);
                var a = Mean(donor, left, (int)(601 * Scale), 150, 20);
                var b = Mean(target, left + dx, (int)(601 * Scale) + dy, 150, 20);
                offsets[side] = new(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            }
            using var detail = new SKBitmap(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul);
            detail.Erase(SKColors.Transparent);
            var pixels = donor.Pixels; var alpha = mask.Pixels; var output = detail.Pixels;
            for (int y = 350; y < 530; y++)
                for (int x = 270; x < 745; x++)
                {
                    int i = y * 1024 + x;
                    if (alpha[i].Alpha == 0) continue;
                    var c = pixels[i]; int side = x < 510 ? 0 : 1;
                    var pupil = donorEyes[side];
                    float ex = (x - pupil.X) / (99 * Scale), ey = (y - pupil.Y) / (38 * Scale);
                    // Keep iris, sclera and lash color. Match only the skin margin.
                    float skin = Math.Clamp((ex * ex + ey * ey - 1) / .6f, 0, 1);
                    var offset = offsets[side];
                    var color = new SKColor(Channel(c.Red + offset.X * skin), Channel(c.Green + offset.Y * skin), Channel(c.Blue + offset.Z * skin), alpha[i].Alpha);
                    output[(y + dy) * 1024 + x + dx] = color;
                }
            detail.Pixels = output;
            string path = $"Parts/faces/{face:00}/eyes-{n:00}.png";
            using var placed = VariantAssets.Place(detail); VariantAssets.Save(root, placed, path);
            result.Add(n, path);
            registration.Add(new { eye = n, dx, dy, targetEyes = targetEyes.Select(p => new { p.X, p.Y }), skinOffsets = offsets.Select(p => new { p.X, p.Y, p.Z }) });
        }
        File.WriteAllText(Path.Combine(root, $"Sources/faces/eye-registration-{face:00}.json"), JsonSerializer.Serialize(registration, AppearanceSerializer.Options));
        return result;
    }
    private static byte Channel(float value) => (byte)Math.Clamp((int)MathF.Round(value), 0, 255);
    private static SKPoint3 Mean(SKBitmap bitmap, int left, int top, int width, int height)
    {
        long r = 0, g = 0, b = 0; int count = 0;
        for (int y = top; y < top + height; y++)
            for (int x = left; x < left + width; x++)
            { var c = bitmap.GetPixel(x, y); r += c.Red; g += c.Green; b += c.Blue; count++; }
        return new(r / (float)count, g / (float)count, b / (float)count);
    }
    public static void Preview(string root)
    {
        for (int face = 7; face <= 16; face++)
        {
            using var source = SKBitmap.Decode(Path.Combine(root, $"Sources/faces/face-{face:00}-01.png"));
            using var cut = VariantAssets.CutBackground(source);
            using var target = cut.Resize(new SKImageInfo(1024, 1024, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.High);
            Build(root, face, target);
        }
        Console.WriteLine("Prepared 90 eye overlays registered to ten reference heads.");
    }
}
