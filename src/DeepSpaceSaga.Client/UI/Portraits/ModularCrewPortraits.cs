using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Portraits;

/// <summary>Composes one assigned portrait on demand, without loading the rest of the pack.</summary>
internal static class ModularCrewPortraits
{
    private const int BodyWidth = 1024;
    private const int BodyHeight = 1536;
    private const int PortraitSize = 300;
    private const int CropSize = 430;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static SKBitmap? Dialogue(string root, string? portraitPath)
    {
        if (portraitPath is null)
            return null;
        const string prefix = "Images/Persons/W4/Portraits/";
        string path = portraitPath.Replace('\\', '/');
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;
        return Assigned(root, "dialogue", path[prefix.Length..]);
    }

    internal static SKBitmap? Pilot(string root, string pilotId) => Assigned(root, "pilots", pilotId);

    private static SKBitmap? Assigned(string root, string surface, string key)
    {
        try
        {
            string path = Path.Combine(root, "Data", "UI", "modular-portraits.json");
            if (!File.Exists(path))
                return null;
            var assignments = Read<Dictionary<string, Dictionary<string, Assignment>>>(path);
            return assignments.TryGetValue(surface, out var entries) && entries is not null &&
                entries.TryGetValue(key, out var assignment) && assignment is { Head.Length: > 0, Outfit.Length: > 0 }
                ? Compose(root, assignment.Head, assignment.Outfit)
                : null;
        }
        catch (Exception ex) when (IsAssetError(ex))
        {
            InterfaceLog.Write($"Modular portrait assignment {surface}/{key}: {ex.Message}");
            return null;
        }
    }

    internal static SKBitmap? Compose(string root, string headId, string outfit)
    {
        try
        {
            string pack = Path.GetFullPath(Path.Combine(root, "Images", "Persons", "ModularCrew"));
            var manifest = Read<Pack>(Path.Combine(pack, "composition-pack.json"));
            if (manifest.SchemaVersion != 1 || manifest.Heads is null || manifest.Suits is null ||
                manifest.CasualBodies is null || !manifest.Heads.TryGetValue(headId, out var head) || head is null)
                return null;

            bool casual = outfit.StartsWith("casual:", StringComparison.Ordinal);
            Suit? suit = null;
            string bodyPath, headPath;
            if (casual)
            {
                if (head.Casual is not { } casualHead ||
                    !manifest.CasualBodies.TryGetValue(outfit[7..], out var variants) || variants is null ||
                    !variants.TryGetValue(headId, out var casualBody))
                    return null;
                bodyPath = casualBody;
                headPath = casualHead;
            }
            else
            {
                if (!manifest.Suits.TryGetValue(outfit, out suit) || suit?.HeadTransform is null ||
                    (head.CompatibleSuits is { } compatible && !compatible.Contains(outfit, StringComparer.Ordinal)))
                    return null;
                bodyPath = suit.Body;
                headPath = head.Suit;
            }

            var transform = suit?.HeadTransform;
            if (transform is { } t && (!float.IsFinite(t.Scale) || t.Scale <= 0 || t.Scale > 1 ||
                                       !float.IsFinite(t.Tx) || !float.IsFinite(t.Ty)))
                return null;
            // The exposed-neck artwork is already registered in body coordinates.
            float anchorX = transform is { } a ? a.Scale * 512 + a.Tx : 519;
            float anchorY = transform is { } b ? b.Scale * 740 + b.Ty : 438;
            using var body = Decode(pack, bodyPath, BodyHeight);
            using var face = Decode(pack, headPath, casual ? BodyHeight : BodyWidth);
            using var back = head.HairLayers?.Back is { } backPath ? Decode(pack, backPath, BodyHeight) : null;
            using var front = head.HairLayers?.Front is { } frontPath ? Decode(pack, frontPath, BodyHeight) : null;
            using var collar = suit is not null ? Decode(pack, suit.FrontCollar, BodyHeight) : null;
            using var shadow = suit is not null ? Decode(pack, suit.ShadowMask, BodyHeight) : null;
            using var placedFace = transform is not null ? PlaceFace(face, shadow!, transform) : null;

            var image = new SKBitmap(PortraitSize, PortraitSize, SKColorType.Bgra8888, SKAlphaType.Premul);
            try
            {
                using var canvas = new SKCanvas(image);
                using var paint = new SKPaint { FilterQuality = SKFilterQuality.Low };
                canvas.Clear(SKColors.Transparent);
                canvas.Scale((float)PortraitSize / CropSize);
                canvas.Translate(-(anchorX - CropSize / 2f), -(anchorY - 200));
                Draw(canvas, paint, back, anchorX - 519, anchorY - 438);
                Draw(canvas, paint, body);
                Draw(canvas, paint, placedFace ?? face);
                Draw(canvas, paint, collar);
                Draw(canvas, paint, front, anchorX - 519, anchorY - 438);
                canvas.Flush();
                return image;
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
        catch (Exception ex) when (IsAssetError(ex))
        {
            InterfaceLog.Write($"Modular portrait {headId}/{outfit}: {ex.Message}");
            return null;
        }
    }

    private static SKBitmap PlaceFace(SKBitmap face, SKBitmap shadow, Transform transform)
    {
        var placed = new SKBitmap(BodyWidth, BodyHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        try
        {
            using (var canvas = new SKCanvas(placed))
            {
                using var paint = new SKPaint { FilterQuality = SKFilterQuality.Low };
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(transform.Tx, transform.Ty);
                canvas.Scale(transform.Scale);
                Draw(canvas, paint, face);
            }
            int left = (int)Math.Clamp(MathF.Floor(transform.Tx), 0, BodyWidth);
            int right = (int)Math.Clamp(MathF.Ceiling(transform.Tx + transform.Scale * BodyWidth), 0, BodyWidth);
            int top = (int)Math.Clamp(MathF.Floor(transform.Ty), 0, BodyHeight);
            int bottom = (int)Math.Clamp(MathF.Ceiling(transform.Ty + transform.Scale * BodyWidth), 0, BodyHeight);
            for (int y = top; y < bottom; y++)
            for (int x = left; x < right; x++)
            {
                int light = 255 - shadow.GetPixel(x, y).Red;
                if (light == 255)
                    continue;
                var pixel = placed.GetPixel(x, y);
                if (pixel.Alpha != 0)
                    placed.SetPixel(x, y, new SKColor(Darken(pixel.Red, light), Darken(pixel.Green, light),
                        Darken(pixel.Blue, light), pixel.Alpha));
            }
            return placed;
        }
        catch
        {
            placed.Dispose();
            throw;
        }
    }

    private static byte Darken(byte color, int light) => (byte)((color * light + 127) / 255);

    private static void Draw(SKCanvas canvas, SKPaint paint, SKBitmap? image, float x = 0, float y = 0)
    {
        if (image is not null)
            canvas.DrawBitmap(image, new SKRect(x, y, x + image.Width, y + image.Height), paint);
    }

    private static SKBitmap Decode(string pack, string relative, int height)
    {
        string path = Path.GetFullPath(Path.Combine(pack, relative));
        if (!Path.GetRelativePath(pack, path).Equals(relative.Replace('/', Path.DirectorySeparatorChar),
                StringComparison.Ordinal) || Path.IsPathRooted(relative) ||
            relative.Split('/').Any(part => part is "" or "." or ".." || part.Contains('\\')) ||
            !relative.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid modular portrait path.");
        var bitmap = SKBitmap.Decode(path) ?? throw new InvalidDataException($"Unreadable layer: {relative}");
        if (bitmap.Width == BodyWidth && bitmap.Height == height)
            return bitmap;
        bitmap.Dispose();
        throw new InvalidDataException($"Wrong dimensions: {relative}");
    }

    private static T Read<T>(string path) where T : class =>
        JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), JsonOptions) ??
        throw new InvalidDataException($"Missing data: {Path.GetFileName(path)}");

    private static bool IsAssetError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidDataException;

    private sealed record Assignment(string Head, string Outfit);
    private sealed record Pack(int SchemaVersion, Dictionary<string, Head> Heads,
        Dictionary<string, Suit> Suits, Dictionary<string, Dictionary<string, string>> CasualBodies);
    private sealed record Head(string Suit, string? Casual, string[]? CompatibleSuits, Hair? HairLayers);
    private sealed record Hair(string? Back, string? Front);
    private sealed record Suit(string Body, string FrontCollar, string ShadowMask, Transform HeadTransform);
    private sealed record Transform(float Scale, float Tx, float Ty);
}
