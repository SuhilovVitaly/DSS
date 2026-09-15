using SkiaSharp;

namespace DeepSpaceSaga.Client.Portraits;

/// <summary>UI-independent raster compositor. Returned images are borrowed until eviction/disposal.</summary>
public sealed class PortraitRenderer(PortraitAssetRepository assets) : IDisposable
{
    private readonly Dictionary<string, LinkedListNode<(string Path, SKBitmap Bitmap)>> _textures = new(StringComparer.Ordinal);
    private readonly LinkedList<(string Path, SKBitmap Bitmap)> _textureLru = new();
    internal int CachedTextureCount => _textures.Count;
    private readonly Dictionary<string, LinkedListNode<(string Key, SKImage Image)>> _cache = new(StringComparer.Ordinal);
    private readonly LinkedList<(string Key, SKImage Image)> _lru = new();
    public int CachedCount => _cache.Count;
    public long CompositionCount { get; private set; }

    public SKImage Render(CharacterAppearance appearance, int resolution = 512, CharacterVisualState? state = null)
    {
        assets.ValidateAppearance(appearance);
        if (resolution is < 32 or > 2048) throw new ArgumentOutOfRangeException(nameof(resolution));
        state ??= new();
        var key = AppearanceSerializer.CacheKey(appearance, state, resolution);
        if (_cache.TryGetValue(key, out var hit))
        {
            _lru.Remove(hit); _lru.AddFirst(hit); return hit.Value.Image;
        }
        using var surface = SKSurface.Create(new SKImageInfo(resolution, resolution, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var destination = SKRect.Create(resolution, resolution);
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        foreach (var layer in assets.Style.Layers)
        {
            if (!appearance.Parts.TryGetValue(layer.Category, out var id)) continue;
            var part = assets.Get(id);
            var texture = Texture(part.Expressions.GetValueOrDefault(state.Expression, part.TextureFor(appearance.LibraryVersion, appearance.Parts.GetValueOrDefault("Face"))));
            string? skinMask = part.SkinMaskFor(appearance.LibraryVersion);
            if (skinMask is not null) canvas.SaveLayer();
            if (part.ColorChannel is null) canvas.DrawBitmap(texture, destination, paint);
            else
            {
                var color = SKColor.Parse(appearance.Colors[part.ColorChannel]);
                var original = SKColor.Parse(part.BaseColor);
                using var filter = SKColorFilter.CreateColorMatrix([
                    color.Red / (float)Math.Max(1, (int)original.Red), 0, 0, 0, 0,
                    0, color.Green / (float)Math.Max(1, (int)original.Green), 0, 0, 0,
                    0, 0, color.Blue / (float)Math.Max(1, (int)original.Blue), 0, 0,
                    0, 0, 0, 1, 0]);
                string? colorMask = part.ColorMaskFor(appearance.LibraryVersion);
                if (colorMask is null)
                {
                    paint.ColorFilter = filter; canvas.DrawBitmap(texture, destination, paint); paint.ColorFilter = null;
                }
                else
                {
                    // Color only the authored region (iris), retaining the white, lashes and highlights.
                    canvas.DrawBitmap(texture, destination, paint);
                    canvas.SaveLayer();
                    paint.ColorFilter = filter; canvas.DrawBitmap(texture, destination, paint); paint.ColorFilter = null;
                    paint.BlendMode = SKBlendMode.DstIn; canvas.DrawBitmap(Texture(colorMask), destination, paint);
                    paint.BlendMode = SKBlendMode.SrcOver; canvas.Restore();
                }
            }
            if (skinMask is not null)
            {
                var skin = SKColor.Parse(appearance.Colors["Skin"]);
                using var skinFilter = SKColorFilter.CreateColorMatrix([
                    skin.Red / 233f, 0, 0, 0, 0,
                    0, skin.Green / 198f, 0, 0, 0,
                    0, 0, skin.Blue / 173f, 0, 0,
                    0, 0, 0, 0, 255]);
                // Replace masked RGB inside this component while retaining its original alpha.
                // SrcOver would apply soft edge alpha twice and produce visible pale seams.
                using var replace = new SKPaint { BlendMode = SKBlendMode.SrcATop };
                canvas.SaveLayer(replace); paint.ColorFilter = skinFilter; canvas.DrawBitmap(texture, destination, paint);
                paint.ColorFilter = null; paint.BlendMode = SKBlendMode.DstIn;
                canvas.DrawBitmap(Texture(skinMask), destination, paint);
                paint.BlendMode = SKBlendMode.SrcOver; canvas.Restore();
                canvas.Restore();
            }
        }
        var image = surface.Snapshot();
        // A growing face library must not retain every decoded 1024px layer.
        // Evict after composition, when no layer still holds a borrowed bitmap.
        while (_textures.Count > 64)
        {
            var last = _textureLru.Last!;
            _textures.Remove(last.Value.Path); last.Value.Bitmap.Dispose(); _textureLru.RemoveLast();
        }
        var node = _lru.AddFirst((key, image)); _cache.Add(key, node); CompositionCount++;
        while (_cache.Count > assets.Style.MaxCachedPortraits)
        {
            var last = _lru.Last!; _cache.Remove(last.Value.Key); last.Value.Image.Dispose(); _lru.RemoveLast();
        }
        return image;
    }

    private SKBitmap Texture(string path)
    {
        if (_textures.TryGetValue(path, out var node))
        {
            _textureLru.Remove(node); _textureLru.AddFirst(node); return node.Value.Bitmap;
        }
        var image = SKBitmap.Decode(assets.TexturePath(path)) ?? throw new InvalidDataException($"Cannot decode {path}.");
        _textures.Add(path, _textureLru.AddFirst((path, image)));
        return image;
    }

    public void Dispose()
    {
        foreach (var item in _lru) item.Image.Dispose();
        foreach (var item in _textureLru) item.Bitmap.Dispose();
        _lru.Clear(); _cache.Clear(); _textures.Clear(); _textureLru.Clear();
    }
}
