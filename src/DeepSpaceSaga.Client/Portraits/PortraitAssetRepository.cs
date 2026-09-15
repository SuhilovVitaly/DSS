using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Portraits;

/// <summary>Loads and validates the complete pack before admitting it to the renderer.</summary>
public sealed class PortraitAssetRepository
{
    public string Root { get; }
    public PortraitStyleProfile Style { get; }
    public IReadOnlyList<PortraitPart> Parts { get; }
    private readonly Dictionary<string, PortraitPart> _byId;

    public PortraitAssetRepository(string root, bool validateTextures = true)
    {
        Root = Path.GetFullPath(root);
        Style = JsonSerializer.Deserialize<PortraitStyleProfile>(File.ReadAllText(Path.Combine(Root, "portrait-style.json")), AppearanceSerializer.Options)
            ?? throw new InvalidDataException("Missing style profile.");
        Parts = JsonSerializer.Deserialize<PortraitPart[]>(File.ReadAllText(Path.Combine(Root, "parts.json")), AppearanceSerializer.Options)
            ?? throw new InvalidDataException("Missing parts catalog.");
        if (Style.Width <= 0 || Style.Height <= 0 || Style.MaxCachedPortraits is < 1 or > 256 ||
            Style.Layers.Length == 0 || Style.Layers.Select(l => l.Category).Distinct().Count() != Style.Layers.Length ||
            Style.Anchors.Values.Any(a => !float.IsFinite(a.X) || !float.IsFinite(a.Y) || a.X is < 0 or > 1 || a.Y is < 0 or > 1) ||
            Style.Palettes.Values.Any(p => p.Length == 0 || p.Any(c => !IsColor(c))))
            throw new InvalidDataException("Invalid portrait style profile.");
        _byId = new(StringComparer.Ordinal);
        foreach (var part in Parts)
        {
            if (string.IsNullOrWhiteSpace(part.Id) || !_byId.TryAdd(part.Id, part))
                throw new InvalidDataException($"Empty or duplicate portrait ID: {part.Id}");
            if (!double.IsFinite(part.Weight) || part.Weight <= 0 || !IsColor(part.BaseColor) ||
                !Style.SupportedLibraryVersions.Contains(part.IntroducedInVersion) ||
                !Style.Layers.Any(l => l.Category == part.Category) ||
                (part.ColorChannel is not null && !Style.Palettes.ContainsKey(part.ColorChannel)))
                throw new InvalidDataException($"Invalid metadata for {part.Id}.");
            ValidateTexture(part.Texture, validateTextures);
            if (part.ColorMask is not null) ValidateTexture(part.ColorMask, validateTextures);
            foreach (var texture in part.Expressions.Values) ValidateTexture(texture, validateTextures);
            foreach (var entry in part.TextureVersions.Concat(part.ColorMaskVersions).Concat(part.SkinMaskVersions))
            {
                if (!Style.SupportedLibraryVersions.Contains(entry.Key)) throw new InvalidDataException($"Unknown texture version in {part.Id}.");
                ValidateTexture(entry.Value, validateTextures);
            }
        }
        foreach (var part in Parts)
            foreach (var entry in part.FaceTextures)
            {
                if (!_byId.TryGetValue(entry.Key, out var face) || face.Category != "Face")
                    throw new InvalidDataException($"Unknown face texture dependency in {part.Id}.");
                ValidateTexture(entry.Value, validateTextures);
            }
        foreach (var part in Parts)
            if (part.RequiresComponents.Concat(part.ExcludesComponents).Any(id => !_byId.ContainsKey(id)))
                throw new InvalidDataException($"Unknown compatibility dependency in {part.Id}.");
        var knownTags = Parts.SelectMany(p => p.Tags).ToHashSet(StringComparer.Ordinal);
        foreach (var part in Parts)
            if (part.RequiresTags.Concat(part.ExcludesTags).Any(tag => !knownTags.Contains(tag)))
                throw new InvalidDataException($"Unknown compatibility tag in {part.Id}.");
        foreach (var layer in Style.Layers.Where(l => l.Required))
            if (!Parts.Any(p => p.Category == layer.Category)) throw new InvalidDataException($"Missing layer {layer.Category}.");
    }

    public PortraitPart Get(string id) => _byId.TryGetValue(id, out var part) ? part : throw new InvalidDataException($"Unknown portrait ID: {id}");
    public string TexturePath(string path)
    {
        var full = Path.GetFullPath(Path.Combine(Root, path));
        if (!full.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Portrait texture escapes its library.");
        return full;
    }
    public static bool IsColor(string? color) => color is { Length: 7 } && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit);

    private void ValidateTexture(string path, bool decode)
    {
        var full = TexturePath(path);
        if (!File.Exists(full) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"Missing PNG: {path}");
        if (!decode) return;
        using var bitmap = SKBitmap.Decode(full) ?? throw new InvalidDataException($"Unreadable PNG: {path}");
        if (bitmap.Width != Style.Width || bitmap.Height != Style.Height || bitmap.AlphaType == SKAlphaType.Opaque ||
            bitmap.GetPixel(0, 0).Alpha != 0)
            throw new InvalidDataException($"Invalid canvas/alpha: {path}");
    }

    public bool IsCompatible(CharacterAppearance appearance)
    {
        if (appearance.Version != 1 || !Style.SupportedLibraryVersions.Contains(appearance.LibraryVersion) ||
            appearance.Gender != "female" || appearance.Age != "adult" || appearance.Race != "human" || appearance.Parts is null) return false;
        var selected = new List<PortraitPart>();
        foreach (var entry in appearance.Parts)
        {
            if (!_byId.TryGetValue(entry.Value, out var p) || p.Category != entry.Key || p.IntroducedInVersion > appearance.LibraryVersion ||
                !p.Tags.Contains(appearance.Gender) || !p.Tags.Contains(appearance.Race) || !p.Tags.Contains(appearance.Age)) return false;
            selected.Add(p);
        }
        if (Style.Layers.Any(l => l.Required && l.IntroducedInVersion <= appearance.LibraryVersion && !appearance.Parts.ContainsKey(l.Category))) return false;
        var tags = selected.SelectMany(p => p.Tags).ToHashSet(StringComparer.Ordinal);
        var ids = selected.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        return selected.All(p => p.RequiresTags.All(tags.Contains) && !p.ExcludesTags.Any(tags.Contains) &&
            p.RequiresComponents.All(ids.Contains) && !p.ExcludesComponents.Any(ids.Contains));
    }

    public void ValidateAppearance(CharacterAppearance appearance)
    {
        if (!IsCompatible(appearance)) throw new InvalidDataException("Incompatible appearance or library version.");
        if (appearance.Colors is null || Style.Palettes.Keys.Any(k => !appearance.Colors.TryGetValue(k, out var color) || !IsColor(color)))
            throw new InvalidDataException("Missing or invalid appearance color.");
    }
}
