using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DeepSpaceSaga.Client.Portraits;

public sealed record CharacterAppearance
{
    public int Version { get; init; } = 1;
    public int LibraryVersion { get; init; } = 1;
    public int Seed { get; init; }
    public string Gender { get; init; } = "female";
    public string Race { get; init; } = "human";
    public string Age { get; init; } = "adult";
    public ImmutableSortedDictionary<string, string> Parts { get; init; } = ImmutableSortedDictionary<string, string>.Empty;
    public ImmutableSortedDictionary<string, string> Colors { get; init; } = ImmutableSortedDictionary<string, string>.Empty;
}

public sealed record CharacterVisualState(string Expression = "neutral");
public sealed record PortraitAnchor(float X, float Y);
public sealed record PortraitLayer(string Category, bool Required = true, int IntroducedInVersion = 1);
public sealed record PortraitPart
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string Texture { get; init; }
    public string? DisplayName { get; init; }
    public int IntroducedInVersion { get; init; } = 1;
    public string? ColorChannel { get; init; }
    public string? ColorMask { get; init; }
    public Dictionary<int, string> TextureVersions { get; init; } = [];
    public Dictionary<int, string> ColorMaskVersions { get; init; } = [];
    public Dictionary<int, string> SkinMaskVersions { get; init; } = [];
    public Dictionary<string, string> FaceTextures { get; init; } = [];
    public string TextureFor(int version, string? faceId = null) =>
        faceId is not null && FaceTextures.TryGetValue(faceId, out var texture) ? texture : VersionPath(TextureVersions, version) ?? Texture;
    public string? ColorMaskFor(int version) => VersionPath(ColorMaskVersions, version) ?? ColorMask;
    public string? SkinMaskFor(int version) => VersionPath(SkinMaskVersions, version);
    private static string? VersionPath(Dictionary<int, string> paths, int version) =>
        paths.Where(p => p.Key <= version).OrderByDescending(p => p.Key).Select(p => p.Value).FirstOrDefault();
    public string BaseColor { get; init; } = "#808080";
    public double Weight { get; init; } = 1;
    public string[] Tags { get; init; } = ["female", "human", "adult"];
    public string[] RequiresTags { get; init; } = [];
    public string[] ExcludesTags { get; init; } = [];
    public string[] RequiresComponents { get; init; } = [];
    public string[] ExcludesComponents { get; init; } = [];
    public Dictionary<string, string> Expressions { get; init; } = [];
}

public sealed record PortraitStyleProfile
{
    public string Gender { get; init; } = "female";
    public string PortraitIdPrefix { get; init; } = "female.w4.portrait.file.";
    public int LibraryVersion { get; init; } = 1;
    public int[] SupportedLibraryVersions { get; init; } = [1];
    public int Width { get; init; } = 512;
    public int Height { get; init; } = 512;
    public int MaxCachedPortraits { get; init; } = 32;
    public Dictionary<string, int> Resolutions { get; init; } = [];
    public Dictionary<string, PortraitAnchor> Anchors { get; init; } = [];
    public PortraitLayer[] Layers { get; init; } = [];
    public Dictionary<string, string[]> Palettes { get; init; } = [];
    public Dictionary<string, string> RenderStyle { get; init; } = [];
}

public static class AppearanceSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(CharacterAppearance appearance) => JsonSerializer.Serialize(appearance, Options);
    public static CharacterAppearance Deserialize(string json)
    {
        var result = JsonSerializer.Deserialize<CharacterAppearance>(json, Options)
            ?? throw new InvalidDataException("Empty appearance.");
        // Version 0 is the pre-release format; its fields are identical to version 1.
        result = result.Version == 0 ? result with { Version = 1 } : result;
        if (result.Version != 1) throw new InvalidDataException($"Unsupported appearance version {result.Version}.");
        if (result.Parts is null || result.Colors is null) throw new InvalidDataException("Appearance parts and colors are required.");
        return result with
        {
            Parts = result.Parts.WithComparers(StringComparer.Ordinal),
            Colors = result.Colors.WithComparers(StringComparer.Ordinal)
        };
    }

    public static string CacheKey(CharacterAppearance appearance, CharacterVisualState state, int resolution) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            Serialize(appearance) + "\n" + state.Expression + "\n" + resolution.ToString(System.Globalization.CultureInfo.InvariantCulture))));
}

/// <summary>Explicit PRNG algorithm, independent of System.Random/runtime versions.</summary>
internal sealed class PortraitRandom(int seed)
{
    private uint _state = unchecked((uint)seed);
    public double Next()
    {
        _state = unchecked(_state + 0x9E3779B9u);
        uint z = _state;
        z = unchecked((z ^ (z >> 16)) * 0x21F0AAADu);
        z = unchecked((z ^ (z >> 15)) * 0x735A2D97u);
        return (z ^ (z >> 15)) / 4294967296.0;
    }
}

public sealed class PortraitGenerator(PortraitAssetRepository assets)
{
    public CharacterAppearance Generate(int seed, int? libraryVersion = null)
    {
        int version = libraryVersion ?? assets.Style.LibraryVersion;
        if (!assets.Style.SupportedLibraryVersions.Contains(version))
            throw new InvalidDataException($"Unsupported portrait library version {version}.");
        var random = new PortraitRandom(seed);
        // Retry whole assignments: constraints can reference components in later layers.
        for (int attempt = 0; attempt < 512; attempt++)
        {
            var parts = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
            foreach (var layer in assets.Style.Layers.Where(l => l.IntroducedInVersion <= version))
            {
                var choices = assets.Parts.Where(p => p.Category == layer.Category && p.IntroducedInVersion <= version &&
                    p.Tags.Contains(assets.Style.Gender) && p.Tags.Contains("human") && p.Tags.Contains("adult"))
                    .OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
                double total = choices.Sum(p => p.Weight) + (layer.Required ? 0 : 8);
                double pick = random.Next() * total;
                foreach (var part in choices)
                {
                    pick -= part.Weight;
                    if (pick < 0) { parts[layer.Category] = part.Id; break; }
                }
            }
            var appearance = new CharacterAppearance { Seed = seed, Gender = assets.Style.Gender, LibraryVersion = version, Parts = parts.ToImmutable() };
            if (!assets.IsCompatible(appearance)) continue;
            var colors = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
            foreach (var palette in assets.Style.Palettes.OrderBy(p => p.Key, StringComparer.Ordinal))
                colors[palette.Key] = palette.Value[(int)(random.Next() * palette.Value.Length)];
            return appearance with { Colors = colors.ToImmutable() };
        }
        throw new InvalidDataException($"No compatible {assets.Style.Gender} adult human portrait found in the library.");
    }
}
