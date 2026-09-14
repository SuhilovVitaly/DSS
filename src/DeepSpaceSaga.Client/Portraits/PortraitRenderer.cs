using SkiaSharp;

namespace DeepSpaceSaga.Client.Portraits;

/// <summary>UI-independent raster compositor. Returned images are borrowed until eviction/disposal.</summary>
public sealed class PortraitRenderer(PortraitAssetRepository assets) : IDisposable
{
    private readonly Dictionary<string, SKBitmap> _textures = new(StringComparer.Ordinal);
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
            var texture = Texture(part.Expressions.GetValueOrDefault(state.Expression, part.Texture));
            if (part.ColorChannel is null) { canvas.DrawBitmap(texture, destination, paint); continue; }
            var color = SKColor.Parse(appearance.Colors[part.ColorChannel]);
            var original = SKColor.Parse(part.BaseColor);
            using var filter = SKColorFilter.CreateColorMatrix([
                color.Red / (float)Math.Max(1, (int)original.Red), 0, 0, 0, 0,
                0, color.Green / (float)Math.Max(1, (int)original.Green), 0, 0, 0,
                0, 0, color.Blue / (float)Math.Max(1, (int)original.Blue), 0, 0,
                0, 0, 0, 1, 0]);
            if (part.ColorMask is null)
            {
                paint.ColorFilter = filter; canvas.DrawBitmap(texture, destination, paint); paint.ColorFilter = null;
            }
            else
            {
                // Color only the authored region (iris), retaining the white, lashes and highlights.
                canvas.DrawBitmap(texture, destination, paint);
                canvas.SaveLayer();
                paint.ColorFilter = filter; canvas.DrawBitmap(texture, destination, paint); paint.ColorFilter = null;
                paint.BlendMode = SKBlendMode.DstIn; canvas.DrawBitmap(Texture(part.ColorMask), destination, paint);
                paint.BlendMode = SKBlendMode.SrcOver; canvas.Restore();
            }
        }
        var image = surface.Snapshot();
        var node = _lru.AddFirst((key, image)); _cache.Add(key, node); CompositionCount++;
        while (_cache.Count > assets.Style.MaxCachedPortraits)
        {
            var last = _lru.Last!; _cache.Remove(last.Value.Key); last.Value.Image.Dispose(); _lru.RemoveLast();
        }
        return image;
    }

    private SKBitmap Texture(string path)
    {
        if (!_textures.TryGetValue(path, out var image))
            _textures[path] = image = SKBitmap.Decode(assets.TexturePath(path)) ?? throw new InvalidDataException($"Cannot decode {path}.");
        return image;
    }

    public void Dispose()
    {
        foreach (var item in _lru) item.Image.Dispose();
        foreach (var item in _textures.Values) item.Dispose();
        _lru.Clear(); _cache.Clear(); _textures.Clear();
    }
}
