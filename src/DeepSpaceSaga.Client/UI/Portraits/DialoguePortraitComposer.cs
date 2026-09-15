using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Portraits;

/// <summary>Whole W4/M4 heads and costumes, using the same canvas and renderer as the workshop.</summary>
public static class DialoguePortraitComposer
{
    public static PersonSex SexFor(string? path, PersonSex fallback = PersonSex.Female)
    {
        string normalized = "/" + (path ?? "").Replace('\\', '/');
        if (normalized.Contains("/M4/", StringComparison.OrdinalIgnoreCase) || normalized.Contains("/M/", StringComparison.OrdinalIgnoreCase))
            return PersonSex.Male;
        if (normalized.Contains("/W4/", StringComparison.OrdinalIgnoreCase) || normalized.Contains("/W/", StringComparison.OrdinalIgnoreCase))
            return PersonSex.Female;
        return fallback;
    }

    // A saved concrete PNG preserves the head; the stable person key determines its costume.
    // Old W/M references are resolved to the current packs so existing saves also use the new art.
    public static SKBitmap? Compose(string? portraitPath, PersonSex sex, string personKey, string? contentRoot = null)
    {
        try
        {
            string root = Path.GetFullPath(contentRoot ?? AppContext.BaseDirectory);
            string pack = sex == PersonSex.Male ? "M4" : "W4";
            var assets = new PortraitAssetRepository(Path.Combine(root, "Images", "Persons", pack), validateTextures: false);
            int seed = BinaryPrimitives.ReadInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(personKey)));
            var appearance = new PortraitGenerator(assets).Generate(seed);
            string? normalized = portraitPath?.Replace('\\', '/');
            bool legacy = normalized is null || normalized.Contains("Persons/W/", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("Persons/M/", StringComparison.OrdinalIgnoreCase);
            if (sex == PersonSex.Male && legacy) normalized = CharacterPortraits.DefaultMale;
            if (!legacy || sex == PersonSex.Male)
            {
                string requested = Path.GetFullPath(Path.Combine(root, normalized!));
                var portrait = assets.Parts.FirstOrDefault(p => p.Category == "Portrait" &&
                    assets.TexturePath(p.Texture).Equals(requested, StringComparison.OrdinalIgnoreCase));
                if (portrait is null) return null;
                appearance = appearance with { Parts = appearance.Parts.SetItem("Portrait", portrait.Id) };
            }
            using var renderer = new PortraitRenderer(assets);
            return SKBitmap.FromImage(renderer.Render(appearance, 300));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or
            System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            InterfaceLog.Write("Dialogue portrait could not be composed: " + ex.Message);
            return null;
        }
    }
}
