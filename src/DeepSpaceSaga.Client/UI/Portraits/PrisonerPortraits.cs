using System.Text.Json;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Portraits;

/// <summary>Owns one selected prisoner's image; never invents a head or reuses another person's face.</summary>
internal sealed class PrisonerPortraits(string root) : IDisposable
{
    private string? _key;
    private SKBitmap? _image;
    internal bool Kneeling { get; private set; }
    internal SKBitmap? Get(LootPilotSnapshot pilot, string? assignedOutfit = null)
    {
        string? asset = pilot.PortraitAssetId;
        string key = pilot.SourcePilotId + "|" + asset + "|" + assignedOutfit + "|" + pilot.Captured + "|" + pilot.Gender + "|kneeling-v3-repaired";
        if (_key == key) return _image;
        Dispose(); _key = key;
        if (string.IsNullOrWhiteSpace(asset)) return null;
        string outfit = assignedOutfit ?? ModularCrewPortraits.AssignedOutfit(root, asset) ?? "pilot";
        try
        {
            // Validate the authoritative asset even when an assembled variant is available.
            string original = SafePath(root, asset);
            if (pilot.Captured && pilot.Gender == CrewGender.Female)
            {
                string pack = Path.Combine(root, "Images", "Persons", "ModularCrew", "kneeling");
                using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(pack, "assembled-portraits.json")));
                if (index.RootElement.GetProperty("calibration").GetString() != "kneeling-v3-proportions-neck-repaired") throw new InvalidDataException("Unsupported prisoner calibration.");
                var entry = index.RootElement.GetProperty("portraits").EnumerateObject().FirstOrDefault(p => p.Value.GetProperty("portraitAssetId").GetString() == asset);
                if (entry.Value.ValueKind == JsonValueKind.Object && entry.Value.GetProperty("outfits").TryGetProperty(outfit, out var relative))
                {
                    using var profile = JsonDocument.Parse(File.ReadAllText(SafePath(pack, outfit + "/profile.json", ".json")));
                    if (profile.RootElement.GetProperty("proportions").GetProperty("calibration").GetString() != "kneeling-v3-20261117") throw new InvalidDataException("Unsupported prisoner pose.");
                    _image = SKBitmap.Decode(SafePath(pack, relative.GetString()!));
                    if (_image is not { Width: 1024, Height: 1536 }) { _image?.Dispose(); _image = null; throw new InvalidDataException("Invalid prisoner image size."); }
                    Kneeling = true;
                }
            }
            _image ??= SKBitmap.Decode(original);
            _image?.SetImmutable();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            InterfaceLog.Write("Prisoner portrait unavailable: " + ex.Message);
            _image?.Dispose(); _image = null; Kneeling = false;
            // A malformed/missing pose must still preserve the original identity if readable.
            try { _image = SKBitmap.Decode(SafePath(root, asset)); _image?.SetImmutable(); }
            catch (Exception fallback) when (fallback is IOException or UnauthorizedAccessException or ArgumentException) { }
        }
        return _image;
    }
    private static string SafePath(string rootPath, string relative, string extension = ".png")
    {
        if (Path.IsPathRooted(relative) || relative.Contains(':') || relative.Split('/', '\\').Any(p => p is "" or "." or "..") ||
            !relative.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid prisoner asset path.");
        string path = Path.GetFullPath(Path.Combine(rootPath, relative));
        if (Path.GetRelativePath(Path.GetFullPath(rootPath), path).StartsWith("..", StringComparison.Ordinal)) throw new InvalidDataException("Prisoner asset escaped content.");
        return path;
    }
    public void Dispose() { _image?.Dispose(); _image = null; _key = null; Kneeling = false; }
}
